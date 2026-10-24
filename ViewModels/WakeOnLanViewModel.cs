using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiWifiManager;
using MauiWifiManager.Abstractions;
using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.Implementation;
using PingMyNetwork.Models;
using PingMyNetwork.Resources.Strings;

namespace PingMyNetwork.ViewModels
{
    /// <summary>
    /// Wakes a sleeping computer by sending it a magic packet, typed in or picked from the devices
    /// the app has seen.
    /// </summary>
    public sealed partial class WakeOnLanViewModel : ToolViewModel
    {
        #region Variables

        private readonly IWifiNetworkService _Wifi;

        private readonly NetworkMonitor _Monitor;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the view model with the devices whose MAC address is known.
        /// </summary>
        /// <param name="wifi">The library.</param>
        /// <param name="monitor">The connection monitor, for the broadcast address.</param>
        /// <param name="devices">The device list.</param>
        public WakeOnLanViewModel(IWifiNetworkService wifi, NetworkMonitor monitor, DeviceManager devices)
        {
            _Wifi = wifi;
            _Monitor = monitor;

            // A randomised (private) MAC changes over time, so waking by it rarely works; and the
            // device the app runs on cannot be asleep.
            Devices =
            [
                .. devices.GetDevices()
                    .Where(device => !string.IsNullOrEmpty(device.MacAddress) && (device.IsRandomizedMac != true) && !device.IsThisDevice)
                    .OrderBy(DeviceDescriber.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                    .Select(device => new ResultRow(
                        string.Empty,
                        DeviceDescriber.DisplayName(device),
                        device.MacAddress,
                        device.IsOnline ? AppResources.WakeAwake : null,
                        Key: device.MacAddress)),
            ];
        }

        #endregion

        #region Properties

        /// <summary>
        /// The MAC address to wake.
        /// </summary>
        [ObservableProperty]
        public partial string MacAddress { get; set; } = string.Empty;

        /// <summary>
        /// Devices the app knows the MAC address of.
        /// </summary>
        public IReadOnlyList<ResultRow> Devices { get; }

        /// <summary>
        /// Whether there are devices to pick from.
        /// </summary>
        public bool HasDevices => Devices.Count > 0;

        /// <summary>
        /// Confirmation once the packet is sent, or empty.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSent))]
        public partial string SentText { get; set; } = string.Empty;

        /// <summary>
        /// Whether the confirmation shows.
        /// </summary>
        public bool HasSent => SentText.Length > 0;

        /// <inheritdoc />
        protected override string ActionText => AppResources.WakeSend;

        #endregion

        #region Methods

        /// <summary>
        /// Fills in a device's MAC address.
        /// </summary>
        /// <param name="device">The device's row.</param>
        [RelayCommand]
        private void Pick(ResultRow device)
        {
            MacAddress = device.Key ?? string.Empty;
            SentText = string.Empty;
        }

        /// <summary>
        /// Sends the magic packet.
        /// </summary>
        /// <param name="token">Cancelled on stop.</param>
        /// <returns>A task that completes once it is sent.</returns>
        protected override async Task RunCoreAsync(CancellationToken token)
        {
            SentText = string.Empty;

            WifiManagerResponse<bool> response = await _Wifi.WakeOnLanAsync(MacAddress.Trim(), _Monitor.Current.BroadcastAddress, token);

            token.ThrowIfCancellationRequested();

            if (Succeeded(response))
            {
                SentText = NetworkFormat.Text(AppResources.WakeSentFormat, MacAddress.Trim().ToUpperInvariant());
            }
        }

        #endregion
    }
}
