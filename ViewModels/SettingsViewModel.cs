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
    /// Settings: theme, new-device alerts, permissions and the app's version.
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
        /// Whether new-device alerts are on.
        /// </summary>
        [ObservableProperty]
        public partial bool AreAlertsOn { get; set; }

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
        /// The app's version, e.g. "1.0 (1)".
        /// </summary>
        public string VersionText { get; } = $"{AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})";

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
