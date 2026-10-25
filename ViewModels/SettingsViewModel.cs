using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PingMyNetwork.Helper.Enums;
using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.Models;
using PingMyNetwork.Resources.Strings;

namespace PingMyNetwork.ViewModels
{
    /// <summary>
    /// Settings: theme and the app's version.
    /// </summary>
    public sealed partial class SettingsViewModel : ObservableObject
    {
        #region Constructor

        /// <summary>
        /// Creates the view model from the saved settings.
        /// </summary>
        public SettingsViewModel()
        {
            ThemeOptions = BuildThemeOptions(AppSettings.Theme);
        }

        #endregion

        #region Properties

        /// <summary>
        /// System, Light and Dark, with the current one selected.
        /// </summary>
        [ObservableProperty]
        public partial IReadOnlyList<ChoiceOption> ThemeOptions { get; set; }

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
