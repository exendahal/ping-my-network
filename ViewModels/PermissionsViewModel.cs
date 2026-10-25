using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiWifiManager;
using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.Implementation;
using PingMyNetwork.Resources.Strings;

namespace PingMyNetwork.ViewModels
{
    /// <summary>
    /// The permissions screen: shown on first launch, before any screen needs them, and from
    /// Settings. Each item says why the app needs it, so nothing is asked for out of the blue.
    /// </summary>
    public sealed partial class PermissionsViewModel : ObservableObject
    {
        #region Variables

        private readonly NetworkMonitor _Monitor;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the view model with the items this platform asks for.
        /// </summary>
        /// <param name="monitor">The connection monitor, read again once access changes.</param>
        /// <param name="wifi">The Wi-Fi manager, which asks for everything scanning needs.</param>
        /// <param name="alerts">New-device alerts, turned on with the notification permission.</param>
        public PermissionsViewModel(NetworkMonitor monitor, IWifiNetworkService wifi, DeviceAlerts alerts)
        {
            _Monitor = monitor;

            List<PermissionItem> items = [];

            if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
            {
                items.Add(new PermissionItem(
                    MaterialIcon.LOCATION,
                    AppResources.PermissionLocationTitle,
                    AppResources.PermissionLocationBody,
                    AppResources.ActionAllow,
                    async () => await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>() == PermissionStatus.Granted,
                    async () =>
                    {
                        if (await LocationAccess.RequestAsync())
                        {
                            // Android 13+ also needs nearby Wi-Fi access to scan; the library asks for both.
                            await wifi.RequestPermissionsAsync();
                        }
                    }));

                items.Add(new PermissionItem(
                    MaterialIcon.WIFI_FIND,
                    AppResources.PermissionLocationServiceTitle,
                    AppResources.PermissionLocationServiceBody,
                    AppResources.ActionTurnOn,
                    () => Task.FromResult(LocationAccess.IsServiceOn()),
                    LocationAccess.OpenServiceSettingsAsync,
                    AppResources.PermissionOn));
            }
            else if (OperatingSystem.IsWindows())
            {
                // Windows desktop apps have no prompt; the Wi-Fi name showing up is the only sign.
                items.Add(new PermissionItem(
                    MaterialIcon.LOCATION,
                    AppResources.PermissionLocationTitle,
                    AppResources.PermissionLocationBodyWindows,
                    AppResources.ActionOpenSettings,
                    () => Task.FromResult(!_Monitor.Current.IsSsidHidden),
                    () => Launcher.Default.OpenAsync(PlatformLinks.WindowsLocationSettings)));
            }

            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                items.Add(new PermissionItem(
                    MaterialIcon.NEARBY,
                    AppResources.PermissionNearbyTitle,
                    AppResources.PermissionNearbyBody,
                    AppResources.ActionAllow,
                    async () => await Permissions.CheckStatusAsync<Permissions.NearbyWifiDevices>() == PermissionStatus.Granted,
                    async () => await Permissions.RequestAsync<Permissions.NearbyWifiDevices>()));
            }

            if (Notifier.IsSupported)
            {
                items.Add(new PermissionItem(
                    MaterialIcon.NOTIFICATIONS,
                    AppResources.PermissionAlertsTitle,
                    AppResources.PermissionAlertsBody,
                    AppResources.ActionTurnOn,
                    async () => AppSettings.AreAlertsOn && await Notifier.IsAllowedAsync(),
                    () => alerts.SetEnabledAsync(true),
                    AppResources.PermissionOn));
            }

            Items = items;
        }

        #endregion

        #region Properties

        /// <summary>
        /// What the app asks for on this platform.
        /// </summary>
        public IReadOnlyList<PermissionItem> Items { get; }

        /// <summary>
        /// Whether this is the first launch, which shows "Continue" instead of a back button.
        /// </summary>
        public bool IsFirstRun { get; } = !AppSettings.IsOnboarded;

        #endregion

        #region Methods

        /// <summary>
        /// Checks every item again. Called when the screen appears and when the app comes back from
        /// system settings.
        /// </summary>
        /// <returns>A task that completes once checked.</returns>
        public async Task RefreshAsync()
        {
            foreach (PermissionItem item in Items)
            {
                await item.RefreshAsync();
            }

            // A new permission can reveal the Wi-Fi name; Home should show it.
            _Monitor.Refresh();
        }

        /// <summary>
        /// Finishes the first-run screen and goes on to Home.
        /// </summary>
        /// <returns>A task that completes once navigation finishes.</returns>
        [RelayCommand]
        private async Task ContinueAsync()
        {
            AppSettings.IsOnboarded = true;
            await Shell.Current.GoToAsync("..");
        }

        #endregion
    }
}
