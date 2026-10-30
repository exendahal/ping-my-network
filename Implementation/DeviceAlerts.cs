using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.Models;
using PingMyNetwork.Resources.Strings;

namespace PingMyNetwork.Implementation
{
    /// <summary>
    /// Notifies the user when a device they have not marked as known joins the network.
    /// </summary>
    /// <remarks>
    /// While the app is open it scans every two minutes; every scan, whoever started it, announces
    /// what it found. In the background Android runs the same scan about every 15 minutes
    /// (BackgroundCheck); iOS gives apps no reliable background time, so there the check runs
    /// whenever the app is opened.
    /// </remarks>
    public sealed class DeviceAlerts
    {
        #region Variables

        /// <summary>
        /// How often the network is checked while the app is open.
        /// </summary>
        private static readonly TimeSpan CHECK_INTERVAL = TimeSpan.FromMinutes(2);

        private readonly DeviceManager _Manager;
        private readonly NetworkMonitor _Monitor;

        private PeriodicTimer? _Timer;
        private bool _IsBackgroundScheduled;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the alerts and listens for new devices.
        /// </summary>
        /// <param name="manager">The device manager, whose scans report new devices.</param>
        /// <param name="monitor">The connection monitor, for the network's name.</param>
        public DeviceAlerts(DeviceManager manager, NetworkMonitor monitor)
        {
            _Manager = manager;
            _Monitor = monitor;

            _Manager.NewDeviceFound += OnNewDeviceFound;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Starts checking every two minutes, when alerts are on.
        /// </summary>
        public void Start()
        {
            if (!AppSettings.AreAlertsOn || (_Timer is not null))
            {
                return;
            }

            // The background job does not survive a reboot; put it back once per launch.
            if (!_IsBackgroundScheduled)
            {
                _IsBackgroundScheduled = true;
                BackgroundCheck.Schedule(true);
            }

            _Timer = new PeriodicTimer(CHECK_INTERVAL);
            _ = RunAsync(_Timer);
        }

        /// <summary>
        /// Stops checking, when the app goes to the background or alerts are turned off.
        /// </summary>
        public void Stop()
        {
            _Timer?.Dispose();
            _Timer = null;
        }

        /// <summary>
        /// Turns alerts on or off, asking for permission to notify when turning them on.
        /// </summary>
        /// <param name="isOn">Whether alerts should be on.</param>
        /// <returns>Whether alerts are on afterwards; false when notifications were refused.</returns>
        public async Task<bool> SetEnabledAsync(bool isOn)
        {
            if (isOn && !await Notifier.RequestPermissionAsync())
            {
                isOn = false;
            }

            // Refused is fine: alerts still work while the app is open.
            if (isOn)
            {
                await LocationAccess.RequestBackgroundAsync();
            }

            AppSettings.AreAlertsOn = isOn;
            BackgroundCheck.Schedule(isOn);

            if (isOn)
            {
                Start();
            }
            else
            {
                Stop();
            }

            return isOn;
        }

        /// <summary>
        /// Scans on every tick until stopped.
        /// </summary>
        /// <param name="timer">The timer to follow.</param>
        /// <returns>A task that completes once stopped.</returns>
        private async Task RunAsync(PeriodicTimer timer)
        {
            while (await timer.WaitForNextTickAsync())
            {
                if (_Monitor.Current.IsConnected)
                {
                    await _Manager.ScanAsync();
                }
            }
        }

        /// <summary>
        /// Posts one notification for a device that just joined, or came back when return
        /// reminders are on.
        /// </summary>
        /// <param name="sender">The device manager.</param>
        /// <param name="device">The device.</param>
        private void OnNewDeviceFound(object? sender, NetworkDevice device)
        {
            if (!AppSettings.AreAlertsOn || (!device.IsNew && !AppSettings.AreReturnAlertsOn))
            {
                return;
            }

            string network = _Manager.NetworkName ?? _Monitor.Current.Ssid ?? AppResources.AppName;

            Notifier.Show(
                device.Id.GetHashCode(),
                NetworkFormat.Text(device.IsNew ? AppResources.AlertNewDeviceTitle : AppResources.AlertDeviceBackTitle, network),
                NetworkFormat.Text(
                    device.IsNew ? AppResources.AlertNewDeviceBody : AppResources.AlertDeviceBackBody,
                    DeviceDescriber.DisplayName(device),
                    device.IpAddress));
        }

        #endregion
    }
}
