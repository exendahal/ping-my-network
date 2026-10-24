using MauiWifiManager;
using MauiWifiManager.Abstractions;

namespace PingMyNetwork.Implementation
{
    /// <summary>
    /// Keeps a live list of the Wi-Fi networks around, while the analyzer is on screen.
    /// </summary>
    /// <remarks>
    /// Built on the library's scan session, which already paces itself to the platform: Android
    /// allows four fresh scans per two minutes, so the session asks for one every 30 s and reads the
    /// OS's cached results in between; Windows scans every couple of seconds. Networks appearing and
    /// leaving arrive as DeviceDiscovered / DeviceLost; ScanResultsAvailable brings the whole list
    /// with current signal strengths, including scans the OS made by itself (which cost no quota).
    /// </remarks>
    public sealed class WifiScanner
    {
        #region Variables

        private readonly IWifiNetworkService _Wifi;
        private readonly object _Gate = new();
        private readonly Dictionary<string, NetworkData> _Networks = new(StringComparer.OrdinalIgnoreCase);

        private CancellationTokenSource? _SessionCancel;
        private bool _IsRunning;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the scanner.
        /// </summary>
        /// <param name="wifi">The Wi-Fi manager.</param>
        public WifiScanner(IWifiNetworkService wifi)
        {
            _Wifi = wifi;
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised, on any thread, when the list or the scan readiness changes.
        /// </summary>
        public event EventHandler? Changed;

        #endregion

        #region Properties

        /// <summary>
        /// Whether this platform can scan for networks at all. iOS cannot.
        /// </summary>
        public static bool IsSupported => OperatingSystem.IsAndroid() || OperatingSystem.IsWindows();

        /// <summary>
        /// Whether a scan can run now, or why not.
        /// </summary>
        public WifiScanAvailability Availability { get; private set; } = WifiScanAvailability.Ready;

        /// <summary>
        /// When the list last changed.
        /// </summary>
        public DateTimeOffset? LastUpdate { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// A copy of the networks seen in the latest scan.
        /// </summary>
        /// <returns>The networks.</returns>
        public List<NetworkData> GetNetworks()
        {
            lock (_Gate)
            {
                return [.. _Networks.Values];
            }
        }

        /// <summary>
        /// Starts scanning. Called when the analyzer appears.
        /// </summary>
        /// <returns>A task that completes once the session has started.</returns>
        public async Task StartAsync()
        {
            if (!IsSupported || _IsRunning)
            {
                return;
            }

            _IsRunning = true;

            _Wifi.ScanResultsAvailable += OnScanResults;
            _Wifi.DeviceDiscovered += OnDiscovered;
            _Wifi.DeviceLost += OnLost;

            await RefreshAvailabilityAsync();

            CancellationTokenSource cancel = new();

            _SessionCancel = cancel;
            await _Wifi.StartScanningForDevicesAsync(cancel.Token);

            // The session reports changes from here on; one scan now fills the list straight away.
            WifiManagerResponse<List<NetworkData>> first = await _Wifi.ScanWifiNetworksAsync(cancel.Token);

            if (first.Data is { } networks)
            {
                Replace(networks);
            }
        }

        /// <summary>
        /// Stops scanning, so the radio is left alone once the analyzer is closed.
        /// </summary>
        /// <returns>A task that completes once the session has stopped.</returns>
        public async Task StopAsync()
        {
            if (!_IsRunning)
            {
                return;
            }

            _IsRunning = false;

            _Wifi.ScanResultsAvailable -= OnScanResults;
            _Wifi.DeviceDiscovered -= OnDiscovered;
            _Wifi.DeviceLost -= OnLost;

            _SessionCancel?.Cancel();
            _SessionCancel = null;

            await _Wifi.StopScanningAsync();
        }

        /// <summary>
        /// Asks for the permissions scanning needs, then checks readiness again.
        /// </summary>
        /// <returns>A task that completes once answered.</returns>
        public async Task RequestPermissionsAsync()
        {
            await _Wifi.RequestPermissionsAsync();
            await RefreshAvailabilityAsync();
        }

        /// <summary>
        /// Checks whether a scan can run now.
        /// </summary>
        /// <returns>A task that completes once checked.</returns>
        private async Task RefreshAvailabilityAsync()
        {
            WifiManagerResponse<WifiScanAvailability> ready = await _Wifi.CanStartScanAsync();

            Availability = ready.ErrorCode == WifiErrorCodes.Success ? ready.Data : WifiScanAvailability.Ready;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Takes a full scan result as the new list.
        /// </summary>
        /// <param name="sender">The Wi-Fi manager.</param>
        /// <param name="e">The scan result.</param>
        private void OnScanResults(object? sender, WifiScanResultsEventArgs e)
        {
            Replace(e.Networks);
        }

        /// <summary>
        /// Adds a network that came into range.
        /// </summary>
        /// <param name="sender">The Wi-Fi manager.</param>
        /// <param name="network">The network.</param>
        private void OnDiscovered(object? sender, NetworkData network)
        {
            lock (_Gate)
            {
                _Networks[KeyOf(network)] = network;
            }

            Touch();
        }

        /// <summary>
        /// Removes a network that went out of range.
        /// </summary>
        /// <param name="sender">The Wi-Fi manager.</param>
        /// <param name="network">The network.</param>
        private void OnLost(object? sender, NetworkData network)
        {
            lock (_Gate)
            {
                _Networks.Remove(KeyOf(network));
            }

            Touch();
        }

        /// <summary>
        /// Replaces the list with a full scan.
        /// </summary>
        /// <param name="networks">The networks.</param>
        private void Replace(IEnumerable<NetworkData> networks)
        {
            lock (_Gate)
            {
                _Networks.Clear();

                foreach (NetworkData network in networks)
                {
                    _Networks[KeyOf(network)] = network;
                }
            }

            Touch();
        }

        /// <summary>
        /// Notes the change and tells listeners.
        /// </summary>
        private void Touch()
        {
            LastUpdate = DateTimeOffset.Now;

            // A result arriving means scanning works, whatever the last check said.
            if (Availability is WifiScanAvailability.Throttled)
            {
                Availability = WifiScanAvailability.Ready;
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// One entry per access point: the same name on two radios is two entries.
        /// </summary>
        /// <param name="network">The network.</param>
        /// <returns>The key.</returns>
        private static string KeyOf(NetworkData network)
        {
            return $"{network.Ssid}|{network.Bssid}";
        }

        #endregion
    }
}
