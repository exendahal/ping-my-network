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
