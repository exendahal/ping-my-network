using PingMyNetwork.Helper.Enums;

namespace PingMyNetwork.Helper.Utilities
{
    /// <summary>
    /// The user's choices from Settings, kept in Preferences so Android Auto Backup and the iOS
    /// backup restore them with everything else.
    /// </summary>
    public static class AppSettings
    {
        #region Variables

        private const string THEME_KEY = "settings.theme";
        private const string SYSTEM_COLORS_KEY = "settings.systemColors";
        private const string ALERTS_KEY = "settings.alerts";
        private const string RETURN_ALERTS_KEY = "settings.returnAlerts";
        private const string ONBOARDED_KEY = "settings.onboarded";

        #endregion

        #region Properties

        /// <summary>
        /// Light, dark, or following the system.
        /// </summary>
        public static ThemeChoice Theme
        {
            get => (ThemeChoice)Preferences.Default.Get(THEME_KEY, (int)ThemeChoice.System);
            set => Preferences.Default.Set(THEME_KEY, (int)value);
        }

        /// <summary>
        /// Whether to use the system's accent colour (Material You, the Windows accent) instead
        /// of the app's green.
        /// </summary>
        public static bool UseSystemColors
        {
            get => Preferences.Default.Get(SYSTEM_COLORS_KEY, false);
            set => Preferences.Default.Set(SYSTEM_COLORS_KEY, value);
        }

        /// <summary>
        /// Whether to notify when an unknown device joins the network. Off until the user turns it
        /// on, for privacy.
        /// </summary>
        public static bool AreAlertsOn
        {
            get => Preferences.Default.Get(ALERTS_KEY, false);
            set => Preferences.Default.Set(ALERTS_KEY, value);
        }

        /// <summary>
        /// Whether to notify again when a device still not marked as known comes back after a
        /// while away. Off by default: a family phone would alert every time it got home.
        /// </summary>
        public static bool AreReturnAlertsOn
        {
            get => Preferences.Default.Get(RETURN_ALERTS_KEY, false);
            set => Preferences.Default.Set(RETURN_ALERTS_KEY, value);
        }

        /// <summary>
        /// Whether the first-run permissions screen has been through.
        /// </summary>
        public static bool IsOnboarded
        {
            get => Preferences.Default.Get(ONBOARDED_KEY, false);
            set => Preferences.Default.Set(ONBOARDED_KEY, value);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Puts the chosen theme into effect.
        /// </summary>
        /// <param name="app">The application; the current one when null.</param>
        public static void ApplyTheme(Application? app = null)
        {
            app ??= Application.Current;

            if (app is null)
            {
                return;
            }

            app.UserAppTheme = Theme switch
            {
                ThemeChoice.Light => AppTheme.Light,
                ThemeChoice.Dark => AppTheme.Dark,
                _ => AppTheme.Unspecified,
            };
        }

        #endregion
    }
}
