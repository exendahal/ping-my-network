using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PingMyNetwork.Helper.Enums;
using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.Implementation;
using PingMyNetwork.Models;
using PingMyNetwork.Resources.Strings;

namespace PingMyNetwork.ViewModels
{
    /// <summary>
    /// Settings: theme, new-device alerts, permissions, backup and the app's version.
    /// </summary>
    public sealed partial class SettingsViewModel : ObservableObject
    {
        #region Variables

        private readonly DeviceAlerts _Alerts;

        private bool _IsApplyingAlerts;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the view model from the saved settings.
        /// </summary>
        /// <param name="alerts">New-device alerts.</param>
        public SettingsViewModel(DeviceAlerts alerts)
        {
            _Alerts = alerts;

            ThemeOptions = BuildThemeOptions(AppSettings.Theme);

            // Showing the saved state is not the user flipping the switch.
            _IsApplyingAlerts = true;
            AreAlertsOn = AppSettings.AreAlertsOn;
            _IsApplyingAlerts = false;
        }

        #endregion

        #region Properties

        /// <summary>
        /// System, Light and Dark, with the current one selected.
        /// </summary>
        [ObservableProperty]
        public partial IReadOnlyList<ChoiceOption> ThemeOptions { get; set; }

        /// <summary>
        /// Whether the app follows the system's accent colour.
        /// </summary>
        [ObservableProperty]
        public partial bool UseSystemColors { get; set; } = AppSettings.UseSystemColors;

        /// <summary>
        /// Whether this device has a system accent to follow.
        /// </summary>
        public bool CanUseSystemColors { get; } = SystemAccent.IsAvailable();

        /// <summary>
        /// What "system colors" means on this platform.
        /// </summary>
        public string SystemColorsFooter { get; } = OperatingSystem.IsAndroid() ? AppResources.SettingsSystemColorsAndroid
            : OperatingSystem.IsWindows() ? AppResources.SettingsSystemColorsWindows
            : AppResources.SettingsSystemColorsApple;

        /// <summary>
        /// Whether new-device alerts are on.
        /// </summary>
        [ObservableProperty]
        public partial bool AreAlertsOn { get; set; }

        /// <summary>
        /// Whether unknown devices are announced again when they come back.
        /// </summary>
        [ObservableProperty]
        public partial bool AreReturnAlertsOn { get; set; } = AppSettings.AreReturnAlertsOn;

        /// <summary>
        /// Whether this platform can show alerts.
        /// </summary>
        public bool CanAlert { get; } = Notifier.IsSupported;

        /// <summary>
        /// When alerts are checked on this platform.
        /// </summary>
        public string AlertsFooter { get; } = OperatingSystem.IsAndroid()
            ? AppResources.SettingsAlertsFooterAndroid
            : AppResources.SettingsAlertsFooterApple;

        /// <summary>
        /// Where this platform backs up the app's data.
        /// </summary>
        public string BackupText { get; } = OperatingSystem.IsAndroid() ? AppResources.SettingsBackupAndroid
            : OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst() ? AppResources.SettingsBackupApple
            : AppResources.SettingsBackupWindows;

        /// <summary>
        /// The app's version, e.g. "1.0 (1)".
        /// </summary>
        public string VersionText { get; } = $"{DisplayVersion(AppInfo.Current.Version)} ({AppInfo.Current.BuildString})";

        #endregion

        #region Methods

        /// <summary>
        /// Switches the theme straight away and remembers it.
        /// </summary>
        /// <param name="key">The theme's name.</param>
        [RelayCommand]
        private void SelectTheme(string key)
        {
            ThemeChoice theme = Enum.Parse<ThemeChoice>(key);

            AppSettings.Theme = theme;
            AppSettings.ApplyTheme();
            ThemeOptions = BuildThemeOptions(theme);
        }

        /// <summary>
        /// Saves whether unknown devices are announced again when they come back.
        /// </summary>
        /// <param name="value">The switch's new state.</param>
        partial void OnAreReturnAlertsOnChanged(bool value)
        {
            AppSettings.AreReturnAlertsOn = value;
        }

        /// <summary>
        /// Switches between the system's colours and the app's green, straight away.
        /// </summary>
        /// <param name="value">The switch's new state.</param>
        partial void OnUseSystemColorsChanged(bool value)
        {
            AppSettings.UseSystemColors = value;

            if (Application.Current is Application app)
            {
                SystemAccent.Apply(app);
            }
        }

        /// <summary>
        /// Opens a new bug report on GitHub with a template and the details a fix usually needs:
        /// the app's version, the device and its system.
        /// </summary>
        /// <returns>A task that completes once the browser opens.</returns>
        [RelayCommand]
        private async Task ReportBugAsync()
        {
            string environment = NetworkFormat.Text(
                AppResources.FeedbackEnvironmentFormat,
                VersionText,
                $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}",
                $"{DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}");

            await Launcher.Default.OpenAsync(PlatformLinks.NewIssue("bug", $"{AppResources.FeedbackBugTemplate}\n\n{environment}"));
        }

        /// <summary>
        /// Opens a new feature request on GitHub.
        /// </summary>
        /// <returns>A task that completes once the browser opens.</returns>
        [RelayCommand]
        private async Task RequestFeatureAsync()
        {
            await Launcher.Default.OpenAsync(PlatformLinks.NewIssue("enhancement", AppResources.FeedbackFeatureTemplate));
        }

        /// <summary>
        /// Opens the permissions screen.
        /// </summary>
        /// <returns>A task that completes once navigation finishes.</returns>
        [RelayCommand]
        private async Task OpenPermissionsAsync()
        {
            await Shell.Current.GoToAsync(Routes.PERMISSIONS);
        }

        /// <summary>
        /// Turns alerts on or off; turning them on asks for permission to notify, and the switch
        /// goes back off if that is refused.
        /// </summary>
        /// <param name="value">The switch's new state.</param>
        partial void OnAreAlertsOnChanged(bool value)
        {
            if (_IsApplyingAlerts)
            {
                return;
            }

            _ = ApplyAlertsAsync(value);
        }

        /// <summary>
        /// Applies the alerts switch.
        /// </summary>
        /// <param name="value">The switch's new state.</param>
        /// <returns>A task that completes once applied.</returns>
        private async Task ApplyAlertsAsync(bool value)
        {
            bool isOn = await _Alerts.SetEnabledAsync(value);

            _IsApplyingAlerts = true;
            AreAlertsOn = isOn;
            _IsApplyingAlerts = false;
        }

        /// <summary>
        /// The version as set in the project, e.g. "1.0". Windows reports four parts (1.0.0.1),
        /// the last being the build number, which is shown on its own.
        /// </summary>
        /// <param name="version">The app's version.</param>
        /// <returns>Major and minor, plus the patch number when there is one.</returns>
        private static string DisplayVersion(Version version)
        {
            return version.Build > 0 ? version.ToString(3) : version.ToString(2);
        }

        /// <summary>
        /// The theme chips.
        /// </summary>
        /// <param name="current">The chosen theme.</param>
        /// <returns>One chip per theme.</returns>
        private static IReadOnlyList<ChoiceOption> BuildThemeOptions(ThemeChoice current)
        {
            return
            [
                new(nameof(ThemeChoice.System), AppResources.ThemeSystem, current == ThemeChoice.System),
                new(nameof(ThemeChoice.Light), AppResources.ThemeLight, current == ThemeChoice.Light),
                new(nameof(ThemeChoice.Dark), AppResources.ThemeDark, current == ThemeChoice.Dark),
            ];
        }

        #endregion
    }
}
