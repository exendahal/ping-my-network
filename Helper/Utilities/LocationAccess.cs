using PingMyNetwork.Helper.Enums;

namespace PingMyNetwork.Helper.Utilities
{
    /// <summary>
    /// Whether the app may read the Wi-Fi name, and the system page that fixes it when not.
    /// </summary>
    /// <remarks>
    /// Android, iOS and Windows only share the Wi-Fi name and nearby networks with apps that have
    /// location access. On Android and iOS, location services (GPS) must also be switched on,
    /// even with the permission granted.
    /// </remarks>
    public static class LocationAccess
    {
        #region Methods

        /// <summary>
        /// Finds out what, if anything, keeps the Wi-Fi name hidden.
        /// </summary>
        /// <returns>The problem, or None.</returns>
        public static async Task<LocationProblem> CheckAsync()
        {
            if (!(OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsWindows()))
            {
                return LocationProblem.None;
            }

            // Windows desktop apps have no runtime prompt; the privacy page is the only switch.
            if (OperatingSystem.IsWindows())
            {
                return LocationProblem.PermissionMissing;
            }

            PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                return LocationProblem.PermissionMissing;
            }

            return IsServiceOn() ? LocationProblem.None : LocationProblem.ServicesOff;
        }

        /// <summary>
        /// Whether location services (GPS) are switched on for the whole device.
        /// </summary>
        /// <returns>True when on, or on platforms without such a switch.</returns>
        public static bool IsServiceOn()
        {
#if ANDROID
            if (Platform.AppContext.GetSystemService(Android.Content.Context.LocationService) is not Android.Locations.LocationManager manager)
            {
                return true;
            }

            return OperatingSystem.IsAndroidVersionAtLeast(28)
                ? manager.IsLocationEnabled
                : manager.IsProviderEnabled(Android.Locations.LocationManager.GpsProvider)
                    || manager.IsProviderEnabled(Android.Locations.LocationManager.NetworkProvider);
#elif IOS || MACCATALYST
            return CoreLocation.CLLocationManager.LocationServicesEnabled;
#else
            return true;
#endif
        }

        /// <summary>
        /// Opens the system page where location services are switched on.
        /// </summary>
        /// <returns>A task that completes once handed over.</returns>
        public static async Task OpenServiceSettingsAsync()
        {
#if ANDROID
            Android.Content.Intent intent = new(Android.Provider.Settings.ActionLocationSourceSettings);

            intent.AddFlags(Android.Content.ActivityFlags.NewTask);
            Platform.AppContext.StartActivity(intent);
            await Task.CompletedTask;
#elif WINDOWS
            await Launcher.Default.OpenAsync(PlatformLinks.WindowsLocationSettings);
#else
            // iOS does not let apps open the Location Services page itself; the app's own page is
            // the closest, one level away.
            AppInfo.Current.ShowSettingsUI();
            await Task.CompletedTask;
#endif
        }

        /// <summary>
        /// Asks for location access. When the system will not ask again (the user said "Don't ask
        /// again"), opens the app's settings page instead.
        /// </summary>
        /// <returns>True once granted.</returns>
        public static async Task<bool> RequestAsync()
        {
            if (OperatingSystem.IsWindows())
            {
                await Launcher.Default.OpenAsync(PlatformLinks.WindowsLocationSettings);
                return false;
            }

            PermissionStatus status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status == PermissionStatus.Granted)
            {
                return true;
            }

            if (!Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>())
            {
                AppInfo.Current.ShowSettingsUI();
            }

            return false;
        }

        #endregion
    }
}
