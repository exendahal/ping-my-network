using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using MauiWifiManager;
using MauiWifiManager.Abstractions;
using PingMyNetwork.Helper.Enums;
using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.Models;

namespace PingMyNetwork.Implementation
{
    /// <summary>
    /// Keeps one up-to-date picture of the current connection for every screen that shows it.
    /// </summary>
    /// <remarks>
    /// A refresh publishes in stages, so the screen fills in as answers arrive instead of waiting
    /// for the slowest one: the connection itself first (no network round trip), then whether the
    /// internet is reachable, then router latency and, where the platform only reports band and
    /// channel from a scan, the scan result. A newer refresh cancels an older one, so a stale
    /// answer never overwrites a fresh one.
    /// </remarks>
    public sealed class NetworkMonitor
    {
        #region Variables

        /// <summary>
        /// How long to wait for a burst of change events to settle before refreshing once.
        /// </summary>
        private const int DEBOUNCE_MS = 300;

        /// <summary>
        /// Echo requests sent to the router per refresh.
        /// </summary>
        private const int GATEWAY_PING_COUNT = 3;

        /// <summary>
        /// Cloudflare's own description of the caller: provider, public address, city. The speed
        /// test already uses Cloudflare, so the provider lookup adds no new third party.
        /// </summary>
        private static readonly Uri PROVIDER_URI = new("https://speed.cloudflare.com/meta");

        /// <summary>
        /// For the provider lookup. One client for the app's life, as HttpClient intends.
        /// </summary>
        private static readonly HttpClient _Http = CreateHttpClient();

        private readonly IWifiNetworkService _Wifi;
        private readonly object _Gate = new();

        /// <summary>
        /// The last provider lookup and the network it was for, so a refresh on the same network
        /// does not ask again.
        /// </summary>
        private (string Network, string? Provider, string? PublicIp)? _ProviderCache;

        private NetworkSnapshot _Current = NetworkSnapshot.Initial;
        private CancellationTokenSource? _RefreshCancel;
        private bool _IsRunning;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the monitor.
        /// </summary>
        /// <param name="wifi">The Wi-Fi manager.</param>
        public NetworkMonitor(IWifiNetworkService wifi)
        {
            _Wifi = wifi;
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised, on a background thread, whenever the snapshot changes.
        /// </summary>
        public event EventHandler<NetworkSnapshot>? SnapshotChanged;

        #endregion

        #region Properties

        /// <summary>
        /// The latest snapshot.
        /// </summary>
        public NetworkSnapshot Current
        {
            get
            {
                lock (_Gate)
                {
                    return _Current;
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Starts listening for changes and takes a fresh snapshot.
        /// </summary>
        public void Start()
        {
            if (_IsRunning)
            {
                return;
            }

            _IsRunning = true;

            _Wifi.ConnectionTypeChanged += OnConnectionChanged;
            _Wifi.WifiNetworkChanged += OnConnectionChanged;
            _Wifi.WifiStateChanged += OnConnectionChanged;

            // Location decides whether the Wi-Fi name and access point can be read at all.
            LocationAccess.Changed += OnConnectionChanged;

            // Signal is the one reading that changes constantly, so it updates in place rather than
            // triggering a full refresh. On Windows subscribing starts a 5 s poll, which is why the
            // monitor stops when the app goes to the background.
            _Wifi.SignalStrengthChanged += OnSignalChanged;

            Refresh();
        }

        /// <summary>
        /// Stops listening, so nothing polls while the app is in the background.
        /// </summary>
        public void Stop()
        {
            if (!_IsRunning)
            {
                return;
            }

            _IsRunning = false;

            _Wifi.ConnectionTypeChanged -= OnConnectionChanged;
            _Wifi.WifiNetworkChanged -= OnConnectionChanged;
            _Wifi.WifiStateChanged -= OnConnectionChanged;
            LocationAccess.Changed -= OnConnectionChanged;
            _Wifi.SignalStrengthChanged -= OnSignalChanged;

            lock (_Gate)
            {
                _RefreshCancel?.Cancel();
                _RefreshCancel = null;
            }
        }

        /// <summary>
        /// Takes a fresh snapshot now.
        /// </summary>
        public void Refresh()
        {
            _ = RefreshAsync(debounce: false);
        }

        /// <summary>
        /// Takes a fresh snapshot and waits for every stage, for callers with no screen to fill in
        /// as answers arrive, such as the background check.
        /// </summary>
        /// <returns>A task that completes once the snapshot is complete.</returns>
        public Task RefreshNowAsync()
        {
            return RefreshAsync(debounce: false);
        }

        /// <summary>
        /// Refreshes after a change event, once the burst it belongs to has settled.
        /// </summary>
        /// <param name="sender">The Wi-Fi manager, or null for a location change.</param>
        /// <param name="e">Unused.</param>
        private void OnConnectionChanged(object? sender, EventArgs e)
        {
            _ = RefreshAsync(debounce: true);
        }

        /// <summary>
        /// Updates the signal reading without a full refresh.
        /// </summary>
        /// <param name="sender">The Wi-Fi manager.</param>
        /// <param name="e">The new reading.</param>
        private void OnSignalChanged(object? sender, WifiSignalChangedEventArgs e)
        {
            Update(
                snapshot => snapshot.ConnectionType == NetworkConnectionType.WiFi
                    ? snapshot with
                    {
                        SignalPercent = e.SignalPercentage,
                        RssiDbm = e.RssiDbm ?? snapshot.RssiDbm,
                    }
                    : snapshot,
                CancellationToken.None);
        }

        /// <summary>
        /// Runs one staged refresh, cancelling whichever refresh was still running.
        /// </summary>
        /// <param name="debounce">Whether to wait for related events to settle first.</param>
        /// <returns>A task that completes when every stage has published or been cancelled.</returns>
        private async Task RefreshAsync(bool debounce)
        {
            CancellationTokenSource cancel = new();
            CancellationToken token = cancel.Token;

            // ponytail: superseded sources are cancelled but not disposed. Without a timeout they
            // hold nothing unmanaged, and disposing one a running stage still reads would throw.
            lock (_Gate)
            {
                _RefreshCancel?.Cancel();
                _RefreshCancel = cancel;
            }

            try
            {
                if (debounce)
                {
                    await Task.Delay(DEBOUNCE_MS, token);
                }

                // Off the UI thread: on Windows the library reads the connection synchronously.
                NetworkSnapshot snapshot = await Task.Run(() => ReadConnectionAsync(token), token);

                Update(_ => snapshot, token);

                if (!snapshot.IsConnected)
                {
                    return;
                }

                ConnectionStatus status = await CheckInternetAsync(token);

                Update(current => current with { Status = status }, token);

                await Task.WhenAll(
                    MeasureGatewayAsync(snapshot.Gateway, token),
                    FillFromScanAsync(snapshot, token),
                    status == ConnectionStatus.Online ? FillProviderAsync(snapshot, token) : Task.CompletedTask);
            }
            catch (OperationCanceledException)
            {
                // A newer refresh took over; it publishes instead.
            }
        }

        /// <summary>
        /// Reads the connection itself: transport, addresses and Wi-Fi details. No network traffic.
        /// </summary>
        /// <param name="token">Cancels the read.</param>
        /// <returns>A snapshot whose status is still Checking.</returns>
        private async Task<NetworkSnapshot> ReadConnectionAsync(CancellationToken token)
        {
            WifiManagerResponse<List<NetworkConnectionType>> types = await _Wifi.GetConnectionTypesAsync(token);
            List<NetworkConnectionType> transports = types.Data ?? [];
            NetworkSnapshot snapshot;

            if (transports.Contains(NetworkConnectionType.WiFi))
            {
                WifiManagerResponse<NetworkData> info = await _Wifi.GetNetworkInfoAsync(token);

                snapshot = info.ErrorCode == WifiErrorCodes.Success && info.Data is not null
                    ? FromWifi(info.Data)
                    : FromInterface(NetworkConnectionType.WiFi);
            }
            else if (transports.Contains(NetworkConnectionType.Ethernet))
            {
                snapshot = FromInterface(NetworkConnectionType.Ethernet);
            }
            else if (transports.Contains(NetworkConnectionType.Cellular))
            {
                snapshot = FromInterface(NetworkConnectionType.Cellular);
            }
            else
            {
                WifiManagerResponse<bool> enabled = await _Wifi.IsWifiEnabledAsync(token);
                bool isWifiOff = (enabled.ErrorCode == WifiErrorCodes.Success) && !enabled.Data;

                return new NetworkSnapshot
                {
                    Status = isWifiOff ? ConnectionStatus.WifiOff : ConnectionStatus.Offline,
                };
            }

            WifiManagerResponse<bool> metered = await _Wifi.IsMeteredConnectionAsync(token);

            return snapshot with
            {
                IsMetered = (metered.ErrorCode == WifiErrorCodes.Success) && metered.Data,
            };
        }

        /// <summary>
        /// Builds a snapshot from what the library reports for the Wi-Fi connection.
        /// </summary>
        /// <param name="data">The library's network data.</param>
        /// <returns>The snapshot.</returns>
        private static NetworkSnapshot FromWifi(NetworkData data)
        {
            // Android reports "<unknown ssid>" when location access or location services are off.
            string? ssid = string.IsNullOrWhiteSpace(data.Ssid) || data.Ssid.Equals("<unknown ssid>", StringComparison.OrdinalIgnoreCase)
                ? null
                : data.Ssid.Trim('"');

            return new NetworkSnapshot
            {
                ConnectionType = NetworkConnectionType.WiFi,
                Ssid = ssid,
                IsSsidHidden = ssid is null,
                Bssid = NetworkFormat.Bssid(data.Bssid),
                Security = data.SecurityType?.ToString(),
                SignalPercent = NetworkFormat.SignalPercent(data.RssiDbm, data.SignalStrength),
                RssiDbm = data.RssiDbm,
                Band = data.FrequencyBand,
                Channel = data.ChannelNumber,
                ChannelWidthMhz = data.ChannelWidthMhz,
                Standard = data.WifiStandard,
                LinkSpeedMbps = data.LinkSpeedMbps ?? WirelessLinkSpeed(),
                IpAddress = NetworkFormat.Ipv4(data.IpAddress),
                SubnetMask = data.SubnetMask,
                PrefixLength = data.NetworkPrefixLength,
                BroadcastAddress = data.BroadcastAddress,
                Gateway = NetworkFormat.Ipv4(data.GatewayAddress),
                DhcpServer = NetworkFormat.Ipv4(data.DhcpServerAddress),
                Ipv6Address = data.IPv6Address,
                DnsServers = data.DnsAddresses ?? [],
            };
        }

        /// <summary>
        /// Builds a snapshot from the operating system's interface list, for links the library
        /// does not describe (Ethernet and mobile data) or when it could not read the Wi-Fi link.
        /// </summary>
        /// <remarks>
        /// ponytail: app-side because the library's GetNetworkInfoAsync only covers Wi-Fi. Move it
        /// into the library when it learns to describe Ethernet, so every app gets it.
        /// </remarks>
        /// <param name="type">The transport that is up.</param>
        /// <returns>The snapshot, with addresses when an interface with a router was found.</returns>
        private static NetworkSnapshot FromInterface(NetworkConnectionType type)
        {
            NetworkSnapshot snapshot = new() { ConnectionType = type };
            NetworkInterface? adapter;

            try
            {
                NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();

                adapter = adapters.FirstOrDefault(candidate => IsRoutable(candidate) && MatchesType(candidate, type))
                    ?? adapters.FirstOrDefault(IsRoutable);
            }
            catch (NetworkInformationException)
            {
                // Some Android versions refuse the interface list; the transport alone still shows.
                return snapshot;
            }

            if (adapter is null)
            {
                return snapshot;
            }

            IPInterfaceProperties properties = adapter.GetIPProperties();
            UnicastIPAddressInformation? ipv4 = properties.UnicastAddresses
                .FirstOrDefault(address => address.Address.AddressFamily == AddressFamily.InterNetwork);
            IPAddress? gateway = properties.GatewayAddresses
                .Select(address => address.Address)
                .FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork);
            IPAddress? dhcp = OperatingSystem.IsWindows() || OperatingSystem.IsLinux()
                ? properties.DhcpServerAddresses.FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork)
                : null;
            string? ipv6 = properties.UnicastAddresses
                .Select(address => address.Address)
                .Where(address => (address.AddressFamily == AddressFamily.InterNetworkV6) && !address.IsIPv6LinkLocal)
                .Select(address => address.ToString())
                .FirstOrDefault();
            int? prefix = ipv4?.PrefixLength > 0 ? ipv4.PrefixLength : null;

            return snapshot with
            {
                IpAddress = ipv4?.Address.ToString(),
                SubnetMask = ipv4?.IPv4Mask?.ToString(),
                PrefixLength = prefix,
                BroadcastAddress = Broadcast(ipv4),
                Gateway = gateway?.ToString(),
                DhcpServer = dhcp?.ToString(),
                Ipv6Address = ipv6,
                DnsServers = properties.DnsAddresses
                    .Where(address => !IPAddress.IsLoopback(address))
                    .Select(address => address.ToString())
                    .ToList(),
                LinkSpeedMbps = adapter.Speed > 0 ? (int)(adapter.Speed / 1_000_000) : null,
            };
        }

        /// <summary>
        /// The Wi-Fi adapter's negotiated speed from the operating system, for platforms where the
        /// library does not report it (Windows).
        /// </summary>
        /// <remarks>
        /// ponytail: app-side for now; belongs in the library's Windows GetNetworkInfoAsync.
        /// </remarks>
        /// <returns>Speed in Mbps, or null when unknown.</returns>
        private static int? WirelessLinkSpeed()
        {
            try
            {
                NetworkInterface? adapter = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(candidate => (candidate.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                        && (candidate.OperationalStatus == OperationalStatus.Up));

                return adapter?.Speed > 0 ? (int)(adapter.Speed / 1_000_000) : null;
            }
            catch (NetworkInformationException)
            {
                return null;
            }
        }

        /// <summary>
        /// Whether an interface is up and has a router, which is what makes it the active link.
        /// </summary>
        /// <param name="adapter">The interface.</param>
        /// <returns>True when it carries traffic off the device.</returns>
        private static bool IsRoutable(NetworkInterface adapter)
        {
            return (adapter.OperationalStatus == OperationalStatus.Up)
                && (adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                && adapter.GetIPProperties().GatewayAddresses.Any(
                    address => (address.Address.AddressFamily == AddressFamily.InterNetwork)
                        && !address.Address.Equals(IPAddress.Any));
        }

        /// <summary>
        /// Whether an interface is of the kind the platform says is connected.
        /// </summary>
        /// <param name="adapter">The interface.</param>
        /// <param name="type">The transport reported as connected.</param>
        /// <returns>True when they match.</returns>
        private static bool MatchesType(NetworkInterface adapter, NetworkConnectionType type)
        {
            return type switch
            {
                NetworkConnectionType.WiFi => adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211,
                NetworkConnectionType.Ethernet => adapter.NetworkInterfaceType
                    is NetworkInterfaceType.Ethernet
                    or NetworkInterfaceType.GigabitEthernet
                    or NetworkInterfaceType.FastEthernetT
                    or NetworkInterfaceType.FastEthernetFx,
                _ => true,
            };
        }

        /// <summary>
        /// Calculates the broadcast address of an IPv4 interface address.
        /// </summary>
        /// <param name="address">The interface address and its mask.</param>
        /// <returns>The broadcast address, or null without a mask.</returns>
        private static string? Broadcast(UnicastIPAddressInformation? address)
        {
            byte[]? ip = address?.Address.GetAddressBytes();
            byte[]? mask = address?.IPv4Mask?.GetAddressBytes();

            if ((ip is null) || (mask is null) || (ip.Length != 4) || (mask.Length != 4))
            {
                return null;
            }

            byte[] broadcast = new byte[4];

            for (var i = 0; i < 4; i++)
            {
                broadcast[i] = (byte)(ip[i] | ~mask[i]);
            }

            return new IPAddress(broadcast).ToString();
        }

        /// <summary>
        /// Works out whether the internet is reachable, and if not, whether a sign-in page is why.
        /// </summary>
        /// <param name="token">Cancels the check.</param>
        /// <returns>Online, CaptivePortal or NoInternet.</returns>
        private async Task<ConnectionStatus> CheckInternetAsync(CancellationToken token)
        {
            WifiManagerResponse<bool> internet = await _Wifi.IsInternetAvailableAsync(token);

            if ((internet.ErrorCode == WifiErrorCodes.Success) && internet.Data)
            {
                return ConnectionStatus.Online;
            }

            WifiManagerResponse<bool> captive = await _Wifi.IsCaptivePortalDetectedAsync(token);

            return (captive.ErrorCode == WifiErrorCodes.Success) && captive.Data
                ? ConnectionStatus.CaptivePortal
                : ConnectionStatus.NoInternet;
        }

        /// <summary>
        /// Measures the round trip to the router and publishes it.
        /// </summary>
        /// <param name="gateway">The router's address.</param>
        /// <param name="token">Cancels the measurement.</param>
        /// <returns>A task that completes once published.</returns>
        private async Task MeasureGatewayAsync(string? gateway, CancellationToken token)
        {
            double? average = null;

            if (gateway is not null)
            {
                WifiManagerResponse<PingStatistics> ping = await _Wifi.PingAsync(gateway, GATEWAY_PING_COUNT, token);

                // A router that ignores ping is common and not a fault; the tile then shows a dash.
                average = ping.Data is { Received: > 0 } ? ping.Data.AverageMs : null;
            }

            Update(current => current with { GatewayLatencyMs = average, IsGatewayMeasured = true }, token);
        }

        /// <summary>
        /// Fills in band, channel, standard and strength from a scan, on platforms that only report
        /// them there (Windows). The strongest access point with the same name is taken as the one
        /// this device is on.
        /// </summary>
        /// <param name="snapshot">The connection as first read.</param>
        /// <param name="token">Cancels the scan.</param>
        /// <returns>A task that completes once published.</returns>
        private async Task FillFromScanAsync(NetworkSnapshot snapshot, CancellationToken token)
        {
            if ((snapshot.ConnectionType != NetworkConnectionType.WiFi)
                || (snapshot.Ssid is null)
                || (snapshot.Band != WifiFrequencyBand.Unknown)
                || !(OperatingSystem.IsWindows() || OperatingSystem.IsAndroid()))
            {
                return;
            }

            WifiManagerResponse<List<NetworkData>> scan = await _Wifi.ScanWifiNetworksAsync(token);
            NetworkData? match = scan.Data?
                .Where(network => string.Equals(network.Ssid, snapshot.Ssid, StringComparison.Ordinal))
                .OrderByDescending(network => network.RssiDbm ?? int.MinValue)
                .FirstOrDefault();

            if (match is null)
            {
                return;
            }

            Update(
                current => current with
                {
                    Band = match.FrequencyBand,
                    Channel = match.ChannelNumber ?? current.Channel,
                    ChannelWidthMhz = match.ChannelWidthMhz ?? current.ChannelWidthMhz,
                    Standard = match.WifiStandard != WifiStandard.Unknown ? match.WifiStandard : current.Standard,
                    RssiDbm = match.RssiDbm ?? current.RssiDbm,
                    SignalPercent = NetworkFormat.SignalPercent(match.RssiDbm, null) ?? current.SignalPercent,
                },
                token);
        }

        /// <summary>
        /// Looks up the internet provider and public address, once per network.
        /// </summary>
        /// <param name="snapshot">The connection as first read.</param>
        /// <param name="token">Cancels the lookup.</param>
        /// <returns>A task that completes once published.</returns>
        private async Task FillProviderAsync(NetworkSnapshot snapshot, CancellationToken token)
        {
            string network = $"{snapshot.ConnectionType}|{snapshot.Ssid}|{snapshot.Gateway}";

            if (_ProviderCache is not { } cached || (cached.Network != network))
            {
                try
                {
                    using HttpResponseMessage response = await _Http.GetAsync(PROVIDER_URI, token);
                    using System.Text.Json.JsonDocument json = System.Text.Json.JsonDocument.Parse(
                        await response.Content.ReadAsStringAsync(token));
                    System.Text.Json.JsonElement root = json.RootElement;

                    cached = (
                        network,
                        root.TryGetProperty("asOrganization", out System.Text.Json.JsonElement provider) ? NetworkFormat.Provider(provider.GetString()) : null,
                        root.TryGetProperty("clientIp", out System.Text.Json.JsonElement ip) ? ip.GetString() : null);
                    _ProviderCache = cached;
                }
                catch (Exception exception) when (exception is HttpRequestException or System.Text.Json.JsonException or TaskCanceledException)
                {
                    // Nothing to show; the tile stays hidden. A cancelled refresh lands here too.
                    return;
                }
            }

            Update(current => current with { Provider = cached.Provider, PublicIp = cached.PublicIp }, token);
        }

        /// <summary>
        /// The client for the provider lookup. Cloudflare answers with an empty object unless the
        /// request says it comes from its speed test page.
        /// </summary>
        /// <returns>The client.</returns>
        private static HttpClient CreateHttpClient()
        {
            HttpClient client = new() { Timeout = TimeSpan.FromSeconds(8) };

            client.DefaultRequestHeaders.Referrer = new Uri("https://speed.cloudflare.com/");

            return client;
        }

        /// <summary>
        /// Replaces the current snapshot and tells listeners, unless the refresh was superseded.
        /// </summary>
        /// <param name="change">Builds the new snapshot from the current one.</param>
        /// <param name="token">The refresh's token; a cancelled refresh publishes nothing.</param>
        private void Update(Func<NetworkSnapshot, NetworkSnapshot> change, CancellationToken token)
        {
            NetworkSnapshot next;

            lock (_Gate)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                next = change(_Current) with { CheckedAt = DateTimeOffset.Now };
                _Current = next;
            }

            SnapshotChanged?.Invoke(this, next);
        }

        #endregion
    }
}
