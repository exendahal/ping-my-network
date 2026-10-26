using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiWifiManager;
using MauiWifiManager.Abstractions;
using PingMyNetwork.Helper.Enums;
using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.Implementation;
using PingMyNetwork.Models;
using PingMyNetwork.Resources.Strings;

namespace PingMyNetwork.ViewModels
{
    /// <summary>
    /// The Home screen: whether the internet works, and the few facts about the connection people
    /// check most.
    /// </summary>
    /// <remarks>
    /// A singleton, so the screen comes back showing what it last knew instead of a skeleton.
    /// Every value is formatted here once per snapshot, so the page binds plain strings.
    /// </remarks>
    public sealed partial class HomeViewModel : ObservableObject
    {
        #region Variables

        private readonly NetworkMonitor _Monitor;
        private readonly IWifiNetworkService _Wifi;
        private readonly DeviceManager _Devices;

        private bool _IsDevicesQueued;
        private LocationProblem _LocationProblem;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the view model and shows whatever the monitor already knows.
        /// </summary>
        /// <param name="monitor">The connection monitor.</param>
        /// <param name="wifi">The Wi-Fi manager, for handing over to system settings.</param>
        /// <param name="devices">The device manager, for the devices summary.</param>
        public HomeViewModel(NetworkMonitor monitor, IWifiNetworkService wifi, DeviceManager devices)
        {
            _Devices = devices;
            _Devices.Changed += (_, _) => QueueDevicesSummary();

            _Monitor = monitor;
            _Wifi = wifi;

            _Monitor.SnapshotChanged += OnSnapshotChanged;

            Apply(_Monitor.Current);
        }

        #endregion

        #region Properties

        /// <summary>
        /// True until the first connection read finishes.
        /// </summary>
        [ObservableProperty]
        public partial bool IsLoading { get; set; } = true;

        /// <summary>
        /// Whether the internet is reachable, or why not.
        /// </summary>
        [ObservableProperty]
        public partial ConnectionStatus Status { get; set; }

        /// <summary>
        /// How good things are overall, which drives the status colour.
        /// </summary>
        [ObservableProperty]
        public partial StatusLevel Level { get; set; }

        /// <summary>
        /// The status in a word or two.
        /// </summary>
        [ObservableProperty]
        public partial string StatusTitle { get; set; } = AppResources.StatusChecking;

        /// <summary>
        /// One sentence on what the status means.
        /// </summary>
        [ObservableProperty]
        public partial string StatusDetail { get; set; } = string.Empty;

        /// <summary>
        /// Whether there is a status sentence to show. "Online" needs no explanation; problems do.
        /// </summary>
        [ObservableProperty]
        public partial bool HasStatusDetail { get; set; }

        /// <summary>
        /// Devices card heading, e.g. "8 devices".
        /// </summary>
        [ObservableProperty]
        public partial string DevicesTitle { get; set; } = AppResources.HomeDevicesNotScanned;

        /// <summary>
        /// Devices card line, e.g. "5 to review · 1 new".
        /// </summary>
        [ObservableProperty]
        public partial string DevicesSubtitle { get; set; } = AppResources.HomeDevicesNotScannedBody;

        /// <summary>
        /// Whether some devices still need the user's review, which shows the attention dot.
        /// </summary>
        [ObservableProperty]
        public partial bool HasDevicesToReview { get; set; }

        /// <summary>
        /// Icon for the status.
        /// </summary>
        [ObservableProperty]
        public partial string StatusIcon { get; set; } = MaterialIcon.WIFI_FIND;

        /// <summary>
        /// Network name, or the transport when there is no name.
        /// </summary>
        [ObservableProperty]
        public partial string NetworkName { get; set; } = string.Empty;

        /// <summary>
        /// Icon for the transport.
        /// </summary>
        [ObservableProperty]
        public partial string ConnectionIcon { get; set; } = MaterialIcon.WIFI;

        /// <summary>
        /// True when on Wi-Fi but the system withheld the network name.
        /// </summary>
        [ObservableProperty]
        public partial bool IsNameHidden { get; set; }

        /// <summary>
        /// Why the name is hidden, e.g. "Location is turned off".
        /// </summary>
        [ObservableProperty]
        public partial string HiddenNameDetail { get; set; } = string.Empty;

        /// <summary>
        /// The button that fixes it, e.g. "Turn on location".
        /// </summary>
        [ObservableProperty]
        public partial string HiddenNameAction { get; set; } = string.Empty;

        /// <summary>
        /// Whether there is a fix to offer.
        /// </summary>
        [ObservableProperty]
        public partial bool HasHiddenNameAction { get; set; }

        /// <summary>
        /// The internet provider, e.g. "WorldLink Communications".
        /// </summary>
        [ObservableProperty]
        public partial string ProviderText { get; set; } = AppResources.ValueMissing;

        /// <summary>
        /// Whether the provider is known, which decides whether its tile shows.
        /// </summary>
        [ObservableProperty]
        public partial bool HasProvider { get; set; }

        /// <summary>
        /// Whether a signal reading exists for the gauge.
        /// </summary>
        [ObservableProperty]
        public partial bool HasSignal { get; set; }

        /// <summary>
        /// Whether to show the transport icon in place of the gauge.
        /// </summary>
        [ObservableProperty]
        public partial bool ShowTransportIcon { get; set; } = true;

        /// <summary>
        /// Signal as 0 to 1, for the gauge.
        /// </summary>
        [ObservableProperty]
        public partial float SignalFraction { get; set; }

        /// <summary>
        /// Signal as a percentage.
        /// </summary>
        [ObservableProperty]
        public partial string SignalText { get; set; } = string.Empty;

        /// <summary>
        /// Signal in a word, with dBm where the platform reports it.
        /// </summary>
        [ObservableProperty]
        public partial string SignalCaption { get; set; } = string.Empty;

        /// <summary>
        /// Status level of the signal, which colours the gauge.
        /// </summary>
        [ObservableProperty]
        public partial StatusLevel SignalLevel { get; set; }

        /// <summary>
        /// Whether to show the metered badge.
        /// </summary>
        [ObservableProperty]
        public partial bool IsMetered { get; set; }

        /// <summary>
        /// Whether to show the tiles that only make sense on Wi-Fi.
        /// </summary>
        [ObservableProperty]
        public partial bool IsWifi { get; set; }

        /// <summary>
        /// Whether there is a connection to show tiles for.
        /// </summary>
        [ObservableProperty]
        public partial bool IsConnected { get; set; }

        /// <summary>
        /// Router round trip.
        /// </summary>
        [ObservableProperty]
        public partial string LatencyText { get; set; } = AppResources.ValueMissing;

        /// <summary>
        /// True while router latency is still being measured.
        /// </summary>
        [ObservableProperty]
        public partial bool IsLatencyLoading { get; set; } = true;

        /// <summary>
        /// Band and channel.
        /// </summary>
        [ObservableProperty]
        public partial string BandChannelText { get; set; } = AppResources.ValueMissing;

        /// <summary>
        /// Wi-Fi generation.
        /// </summary>
        [ObservableProperty]
        public partial string StandardText { get; set; } = AppResources.ValueMissing;

        /// <summary>
        /// Link speed.
        /// </summary>
        [ObservableProperty]
        public partial string LinkSpeedText { get; set; } = AppResources.ValueMissing;

        /// <summary>
        /// Whether a link speed is known, which decides whether its tile shows.
        /// </summary>
        [ObservableProperty]
        public partial bool HasLinkSpeed { get; set; }

        /// <summary>
        /// This device's address.
        /// </summary>
        [ObservableProperty]
        public partial string IpText { get; set; } = AppResources.ValueMissing;

        /// <summary>
        /// The one action the status calls for, or empty when none.
        /// </summary>
        [ObservableProperty]
        public partial string PrimaryActionText { get; set; } = string.Empty;

        /// <summary>
        /// Whether there is an action to offer.
        /// </summary>
        [ObservableProperty]
        public partial bool HasPrimaryAction { get; set; }

        /// <summary>
        /// When the connection was last checked.
        /// </summary>
        [ObservableProperty]
        public partial string LastCheckedText { get; set; } = string.Empty;

        #endregion

        #region Methods

        /// <summary>
        /// Starts monitoring. Called when the screen appears.
        /// </summary>
        public void Start()
        {
            _Monitor.Start();
            _ = LoadDevicesAsync();
        }

        /// <summary>
        /// Checks the connection again.
        /// </summary>
        [RelayCommand]
        private void Refresh()
        {
            _Monitor.Refresh();
        }

        /// <summary>
        /// Does whatever the current status calls for.
        /// </summary>
        /// <returns>A task that completes once handed over.</returns>
        [RelayCommand]
        private async Task PrimaryActionAsync()
        {
            switch (Status)
            {
                case ConnectionStatus.CaptivePortal:
                    await Launcher.Default.OpenAsync(PlatformLinks.SignInProbe);
                    break;

                case ConnectionStatus.NoInternet:
                    Refresh();
                    break;

                case ConnectionStatus.Offline:
                case ConnectionStatus.WifiOff:
                    await _Wifi.OpenWifiSetting();
                    break;
            }
        }

        /// <summary>
        /// Fixes whatever hides the Wi-Fi name: asks for location access, or opens the page where
        /// location is switched on.
        /// </summary>
        /// <returns>A task that completes once handed over.</returns>
        [RelayCommand]
        private async Task RevealNameAsync()
        {
            if (_LocationProblem == LocationProblem.ServicesOff)
            {
                await LocationAccess.OpenServiceSettingsAsync();
                return;
            }

            if (await LocationAccess.RequestAsync())
            {
                // Android 13+ also wants nearby Wi-Fi access for scanning; the library asks for both.
                await _Wifi.RequestPermissionsAsync();
                _Monitor.Refresh();
            }
        }

        /// <summary>
        /// Opens Settings.
        /// </summary>
        /// <returns>A task that completes once navigation finishes.</returns>
        [RelayCommand]
        private async Task OpenSettingsAsync()
        {
            await Shell.Current.GoToAsync(Routes.SETTINGS);
        }

        /// <summary>
        /// Opens the device list.
        /// </summary>
        /// <returns>A task that completes once navigation finishes.</returns>
        [RelayCommand]
        private async Task OpenDevicesAsync()
        {
            await Shell.Current.GoToAsync($"//{Routes.DEVICES}", animate: false);
        }

        /// <summary>
        /// Opens network details.
        /// </summary>
        /// <returns>A task that completes once navigation finishes.</returns>
        [RelayCommand]
        private async Task OpenDetailsAsync()
        {
            await Shell.Current.GoToAsync(Routes.NETWORK_DETAILS);
        }

        /// <summary>
        /// Opens a tool on top of Home, so back comes straight here.
        /// </summary>
        /// <param name="route">The tool's route.</param>
        /// <returns>A task that completes once navigation finishes.</returns>
        [RelayCommand]
        private async Task OpenToolAsync(string route)
        {
            await Shell.Current.GoToAsync(route);
        }

        /// <summary>
        /// Moves each new snapshot onto the UI thread.
        /// </summary>
        /// <param name="sender">The monitor.</param>
        /// <param name="snapshot">The new snapshot.</param>
        private void OnSnapshotChanged(object? sender, NetworkSnapshot snapshot)
        {
            MainThread.BeginInvokeOnMainThread(() => Apply(snapshot));
        }

        /// <summary>
        /// Formats a snapshot into the screen's values.
        /// </summary>
        /// <param name="snapshot">The snapshot to show.</param>
        private void Apply(NetworkSnapshot snapshot)
        {
            bool isFirstRead = ReferenceEquals(snapshot, NetworkSnapshot.Initial);

            IsLoading = isFirstRead;
            Status = snapshot.Status;
            IsConnected = snapshot.IsConnected;
            IsWifi = snapshot.ConnectionType == NetworkConnectionType.WiFi;
            IsMetered = snapshot.IsMetered;
            IsNameHidden = snapshot.IsSsidHidden;

            if (snapshot.IsSsidHidden)
            {
                _ = ExplainHiddenNameAsync();
            }

            ApplyStatus(snapshot.Status);
            ApplyName(snapshot);
            ApplySignal(snapshot);

            IsLatencyLoading = snapshot.IsConnected && !snapshot.IsGatewayMeasured;
            LatencyText = snapshot.GatewayLatencyMs is double latency
                ? NetworkFormat.Latency(latency)
                : AppResources.ValueMissing;

            string? band = NetworkFormat.Band(snapshot.Band);

            BandChannelText = (band, snapshot.Channel) switch
            {
                (not null, int channel) => NetworkFormat.Text(AppResources.BandChannelFormat, band, channel),
                (not null, null) => band,
                _ => AppResources.ValueMissing,
            };
            StandardText = NetworkFormat.StandardShort(snapshot.Standard) ?? AppResources.ValueMissing;
            HasLinkSpeed = snapshot.LinkSpeedMbps is not null;
            LinkSpeedText = snapshot.LinkSpeedMbps is int speed
                ? NetworkFormat.Text(AppResources.LinkSpeedFormat, speed)
                : AppResources.ValueMissing;
            IpText = snapshot.IpAddress ?? AppResources.ValueMissing;
            HasProvider = snapshot.Provider is not null;
            ProviderText = snapshot.Provider ?? AppResources.ValueMissing;
            LastCheckedText = isFirstRead
                ? string.Empty
                : NetworkFormat.Text(AppResources.LastCheckedFormat, snapshot.CheckedAt.LocalDateTime);
        }

        /// <summary>
        /// Works out why the Wi-Fi name is hidden and offers the fix.
        /// </summary>
        /// <returns>A task that completes once shown.</returns>
        private async Task ExplainHiddenNameAsync()
        {
            _LocationProblem = await LocationAccess.CheckAsync();

            (HiddenNameDetail, HiddenNameAction) = _LocationProblem switch
            {
                LocationProblem.ServicesOff => (AppResources.HiddenNameLocationOff, AppResources.ActionTurnOnLocation),
                LocationProblem.PermissionMissing => (
                    OperatingSystem.IsWindows() ? AppResources.HiddenNetworkNameDetail : AppResources.HiddenNameNoPermission,
                    AppResources.ActionAllowLocation),
                _ => (AppResources.HiddenNameUnknown, string.Empty),
            };

            HasHiddenNameAction = HiddenNameAction.Length > 0;
        }

        /// <summary>
        /// Sets the status words, icon, colour and action.
        /// </summary>
        /// <param name="status">The connection status.</param>
        private void ApplyStatus(ConnectionStatus status)
        {
            (StatusTitle, StatusDetail, StatusIcon, Level, PrimaryActionText) = status switch
            {
                ConnectionStatus.Online => (
                    AppResources.StatusOnline,
                    AppResources.StatusOnlineDetail,
                    MaterialIcon.CHECK_CIRCLE,
                    StatusLevel.Good,
                    string.Empty),
                ConnectionStatus.CaptivePortal => (
                    AppResources.StatusCaptivePortal,
                    AppResources.StatusCaptivePortalDetail,
                    MaterialIcon.LOGIN,
                    StatusLevel.Poor,
                    AppResources.ActionOpenSignInPage),
                ConnectionStatus.NoInternet => (
                    AppResources.StatusNoInternet,
                    AppResources.StatusNoInternetDetail,
                    MaterialIcon.CLOUD_OFF,
                    StatusLevel.Bad,
                    AppResources.ActionRetry),
                ConnectionStatus.Offline => (
                    AppResources.StatusOffline,
                    AppResources.StatusOfflineDetail,
                    MaterialIcon.WIFI_OFF,
                    StatusLevel.Bad,
                    AppResources.ActionOpenWifiSettings),
                ConnectionStatus.WifiOff => (
                    AppResources.StatusWifiOff,
                    AppResources.StatusWifiOffDetail,
                    MaterialIcon.WIFI_OFF,
                    StatusLevel.Bad,
                    AppResources.ActionOpenWifiSettings),
                _ => (
                    AppResources.StatusChecking,
                    string.Empty,
                    MaterialIcon.WIFI_FIND,
                    StatusLevel.Notice,
                    string.Empty),
            };

            HasPrimaryAction = !string.IsNullOrEmpty(PrimaryActionText);
            HasStatusDetail = (status != ConnectionStatus.Online) && !string.IsNullOrEmpty(StatusDetail);
        }

        /// <summary>
        /// Sets what the connection is called and its icon.
        /// </summary>
        /// <param name="snapshot">The snapshot to show.</param>
        private void ApplyName(NetworkSnapshot snapshot)
        {
            ConnectionIcon = snapshot.ConnectionType switch
            {
                NetworkConnectionType.Ethernet => MaterialIcon.ETHERNET,
                NetworkConnectionType.Cellular => MaterialIcon.CELLULAR,
                NetworkConnectionType.WiFi => MaterialIcon.WIFI,
                _ => MaterialIcon.WIFI_OFF,
            };

            NetworkName = snapshot.ConnectionType switch
            {
                NetworkConnectionType.WiFi => snapshot.Ssid ?? AppResources.HiddenNetworkName,
                NetworkConnectionType.Unknown => string.Empty,
                _ => NetworkFormat.ConnectionType(snapshot.ConnectionType),
            };
        }

        /// <summary>
        /// Sets the gauge and its caption.
        /// </summary>
        /// <param name="snapshot">The snapshot to show.</param>
        private void ApplySignal(NetworkSnapshot snapshot)
        {
            if ((snapshot.ConnectionType != NetworkConnectionType.WiFi) || (snapshot.SignalPercent is not int percent))
            {
                HasSignal = false;
                ShowTransportIcon = true;
                SignalFraction = 0;
                SignalText = string.Empty;
                SignalCaption = string.Empty;
                return;
            }

            string word = NetworkFormat.SignalWord(percent);

            HasSignal = true;
            ShowTransportIcon = false;
            SignalFraction = percent / 100f;
            SignalLevel = NetworkFormat.SignalLevel(percent);
            SignalText = NetworkFormat.Text(AppResources.PercentFormat, percent);
            SignalCaption = snapshot.RssiDbm is int rssi
                ? $"{word} · {NetworkFormat.Text(AppResources.DbmFormat, rssi)}"
                : word;
        }

        /// <summary>
        /// Shows the remembered devices of this network, so the card has numbers before any scan.
        /// </summary>
        /// <returns>A task that completes once loaded.</returns>
        private async Task LoadDevicesAsync()
        {
            await _Devices.LoadCurrentNetworkAsync();
            QueueDevicesSummary();
        }

        /// <summary>
        /// Updates the devices card once per burst of changes; a scan reports many.
        /// </summary>
        private void QueueDevicesSummary()
        {
            if (_IsDevicesQueued)
            {
                return;
            }

            _IsDevicesQueued = true;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(250);
                _IsDevicesQueued = false;
                ApplyDevicesSummary();
            });
        }

        /// <summary>
        /// Counts the devices for the card.
        /// </summary>
        private void ApplyDevicesSummary()
        {
            List<NetworkDevice> devices = _Devices.GetDevices();

            if (devices.Count == 0)
            {
                DevicesTitle = AppResources.HomeDevicesNotScanned;
                DevicesSubtitle = AppResources.HomeDevicesNotScannedBody;
                HasDevicesToReview = false;
                return;
            }

            // A guest network is only looked at, never reviewed.
            int toReview = _Devices.IsMine == false ? 0 : devices.Count(device => !device.IsKnown && !device.IsThisDevice);
            int fresh = devices.Count(device => device.IsNew);
            List<string> parts = [];

            if (toReview > 0)
            {
                parts.Add(NetworkFormat.Text(AppResources.HomeDevicesReviewFormat, toReview));
            }

            if (fresh > 0)
            {
                parts.Add(NetworkFormat.Text(AppResources.HomeDevicesNewFormat, fresh));
            }

            DevicesTitle = NetworkFormat.Text(
                devices.Count == 1 ? AppResources.HomeDevicesOneFormat : AppResources.HomeDevicesFormat,
                devices.Count);
            DevicesSubtitle = parts.Count > 0 ? string.Join(" · ", parts) : AppResources.HomeDevicesAllKnown;
            HasDevicesToReview = toReview > 0;
        }

        #endregion
    }
}
