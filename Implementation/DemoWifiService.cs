#if DEBUG
using System.Reflection;
using MauiWifiManager;
using MauiWifiManager.Abstractions;

namespace PingMyNetwork.Implementation
{
    /// <summary>
    /// Debug builds only: a made-up home network for store screenshots, so no real Wi-Fi name,
    /// device or address ends up in a public listing. It stands in for the Wi-Fi manager on the
    /// calls the screens read, and passes everything else (speed test, traceroute, DNS) through.
    /// </summary>
    /// <remarks>
    /// On with PMN_DEMO=1 (Windows), or with an empty "demo" file in the app's data folder
    /// (Android: adb shell run-as com.exendahal.pingmynetwork touch files/demo). Demo devices are
    /// saved apart from real ones (DeviceStore), so turning it off leaves the user's data as it was.
    /// </remarks>
    public class DemoWifiService : DispatchProxy
    {
        #region Variables

        private const string GATEWAY = "192.168.1.1";
        private const string OWN_ADDRESS = "192.168.1.23";

        /// <summary>
        /// The household: address, host name, UPnP name, maker, model, page title and open ports.
        /// The last two are left out of the seeded store (see the README in store/), so they show
        /// as new.
        /// </summary>
        private static readonly (string Ip, string? Host, string? Friendly, string? Maker, string? Model, string? Title, int[] Ports)[] DEVICES =
        [
            (GATEWAY, null, "Home Router", "TP-Link", "Archer AX55", "Archer AX55", [53, 80, 443]),
            (OWN_ADDRESS, null, null, null, null, null, []),
            ("192.168.1.10", null, "[TV] Living Room", "Samsung Electronics", "QE55Q80C", null, [8001, 8002, 9197]),
            ("192.168.1.11", "Bedroom-Chromecast.lan", null, null, null, null, [8008, 8009]),
            ("192.168.1.12", "Office-Printer.lan", null, "HP", "LaserJet Pro M404", null, [80, 631, 9100]),
            ("192.168.1.14", null, "Front Door Camera", "Reolink", "Video Doorbell", null, [80, 443, 554]),
            ("192.168.1.15", "Kitchen-Sonos.lan", null, null, null, null, [1400]),
            ("192.168.1.16", "Alexs-iPhone.lan", null, null, null, null, [62078]),
            ("192.168.1.17", "Sams-MacBook-Air.lan", null, null, null, null, [22, 445]),
            ("192.168.1.19", null, "Family NAS", "Synology", "DS224+", null, [80, 443, 445, 5000]),
            ("192.168.1.20", "tasmota-plug-4F2A.lan", null, null, null, null, [80, 1883]),
            ("192.168.1.31", null, null, null, null, null, []),
            ("192.168.1.42", null, null, null, null, null, [80]),
        ];

        /// <summary>
        /// Nearby networks: SSID, BSSID, band, channel, RSSI and security.
        /// </summary>
        private static readonly (string Ssid, string Bssid, WifiFrequencyBand Band, int Channel, int Rssi, string Security)[] NEARBY =
        [
            ("Home Wi-Fi", "a4:2b:b0:11:22:34", WifiFrequencyBand.Band5GHz, 36, -48, "WPA3"),
            ("Home Wi-Fi", "a4:2b:b0:11:22:30", WifiFrequencyBand.Band2_4GHz, 6, -57, "WPA3"),
            ("Neighbour 5G", "3c:84:6a:90:12:01", WifiFrequencyBand.Band5GHz, 44, -71, "WPA2"),
            ("Neighbour", "3c:84:6a:90:12:00", WifiFrequencyBand.Band2_4GHz, 1, -66, "WPA2"),
            ("CoffeeShop Guest", "f0:9f:c2:33:44:55", WifiFrequencyBand.Band2_4GHz, 11, -78, "Open"),
            ("Apartment 4B", "b0:be:76:aa:10:20", WifiFrequencyBand.Band2_4GHz, 6, -74, "WPA2"),
            ("Apartment 4B 5G", "b0:be:76:aa:10:21", WifiFrequencyBand.Band5GHz, 149, -80, "WPA2"),
            ("DIRECT-Printer", "02:1a:11:f0:00:01", WifiFrequencyBand.Band2_4GHz, 11, -83, "WPA2"),
        ];

        private IWifiNetworkService _Real = null!;

        #endregion

        #region Properties

        /// <summary>
        /// Whether demo mode is on.
        /// </summary>
        public static bool IsOn { get; } = (Environment.GetEnvironmentVariable("PMN_DEMO") == "1")
            || File.Exists(Path.Combine(FileSystem.AppDataDirectory, "demo"));

        /// <summary>
        /// What this device calls itself, instead of its real name.
        /// </summary>
        public static string ThisDeviceName => DeviceInfo.Current.Idiom == DeviceIdiom.Phone ? "This phone" : "This PC";

        #endregion

        #region Methods

        /// <summary>
        /// The real service, wrapped in demo data.
        /// </summary>
        /// <param name="real">The Wi-Fi manager.</param>
        /// <returns>The wrapper.</returns>
        public static IWifiNetworkService Wrap(IWifiNetworkService real)
        {
            IWifiNetworkService proxy = Create<IWifiNetworkService, DemoWifiService>();

            ((DemoWifiService)(object)proxy)._Real = real;

            return proxy;
        }

        /// <inheritdoc/>
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            object? demo = method!.Name switch
            {
                nameof(IWifiNetworkService.GetConnectionTypesAsync) => Ok(new List<NetworkConnectionType> { NetworkConnectionType.WiFi }),
                nameof(IWifiNetworkService.GetNetworkInfoAsync) => Ok(Network()),
                nameof(IWifiNetworkService.IsWifiEnabledAsync) => Ok(true),
                nameof(IWifiNetworkService.IsMeteredConnectionAsync) => Ok(false),
                nameof(IWifiNetworkService.IsInternetAvailableAsync) => Ok(true),
                nameof(IWifiNetworkService.IsCaptivePortalDetectedAsync) => Ok(false),
                nameof(IWifiNetworkService.CanStartScanAsync) => Ok(WifiScanAvailability.Ready),
                nameof(IWifiNetworkService.ScanWifiNetworksAsync) => Ok(NEARBY.Select(Nearby).ToList()),
                nameof(IWifiNetworkService.BrowseServicesAsync) => Ok(new List<NetworkServiceInfo>()),
                nameof(IWifiNetworkService.DiscoverDevicesAsync) => DiscoverAsync(args?.OfType<IProgress<LanDevice>>().FirstOrDefault()),
                nameof(IWifiNetworkService.GetDeviceDetailsAsync) => DetailsAsync((string)args![0]!),
                nameof(IWifiNetworkService.PingAsync) => Ok(Ping((string)args![0]!)),
                _ => null,
            };

            return demo ?? method.Invoke(_Real, args);
        }

        private static Task<WifiManagerResponse<T>> Ok<T>(T data)
        {
            return Task.FromResult(WifiManagerResponse<T>.SuccessResponse(data, string.Empty));
        }

        /// <summary>
        /// An address as Android packs it: first octet in the lowest byte (NetworkFormat.Ipv4).
        /// </summary>
        private static int Ip(string address)
        {
            return BitConverter.ToInt32(System.Net.IPAddress.Parse(address).GetAddressBytes());
        }

        private static NetworkData Network()
        {
            return new NetworkData
            {
                Ssid = "Home Wi-Fi",
                Bssid = "a4:2b:b0:11:22:34",
                IpAddress = Ip(OWN_ADDRESS),
                GatewayAddress = Ip(GATEWAY),
                DhcpServerAddress = Ip(GATEWAY),
                SubnetMask = "255.255.255.0",
                DnsAddresses = [GATEWAY],
                RssiDbm = -48,
                FrequencyBand = WifiFrequencyBand.Band5GHz,
                ChannelNumber = 36,
                ChannelWidthMhz = 80,
                LinkSpeedMbps = 1201,
                WifiStandard = WifiStandard.Ieee80211ax,
                SecurityType = "WPA3",
            };
        }

        private static NetworkData Nearby((string Ssid, string Bssid, WifiFrequencyBand Band, int Channel, int Rssi, string Security) network)
        {
            return new NetworkData
            {
                Ssid = network.Ssid,
                Bssid = network.Bssid,
                FrequencyBand = network.Band,
                ChannelNumber = network.Channel,
                RssiDbm = network.Rssi,
                SecurityType = network.Security,
            };
        }

        private static async Task<WifiManagerResponse<List<LanDevice>>> DiscoverAsync(IProgress<LanDevice>? progress)
        {
            List<LanDevice> found = [];

            // Answers trickle in, as they do on a real sweep.
            foreach (var device in DEVICES)
            {
                await Task.Delay(120);

                LanDevice answer = new() { IpAddress = device.Ip, HostName = device.Host, RoundTripMs = 3 + (device.Ip.Length % 9L) };

                found.Add(answer);
                progress?.Report(answer);
            }

            return WifiManagerResponse<List<LanDevice>>.SuccessResponse(found, string.Empty);
        }

        private static async Task<WifiManagerResponse<LanDeviceDetails>> DetailsAsync(string address)
        {
            await Task.Delay(300);

            var device = DEVICES.FirstOrDefault(candidate => candidate.Ip == address);

            return WifiManagerResponse<LanDeviceDetails>.SuccessResponse(new LanDeviceDetails
            {
                IpAddress = address,
                HostName = device.Host,
                FriendlyName = device.Friendly,
                Manufacturer = device.Maker,
                ModelName = device.Model,
                HttpTitle = device.Title,
                OpenPorts = [.. device.Ports ?? []],
                Ping = Ping(address),
            }, string.Empty);
        }

        private static PingStatistics Ping(string host)
        {
            long average = 2 + (host.Length % 7);

            return new PingStatistics { Host = host, Sent = 4, Received = 4, MinMs = average - 1, AverageMs = average, MaxMs = average + 3 };
        }

        #endregion
    }
}
#endif
