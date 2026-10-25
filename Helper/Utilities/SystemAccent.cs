namespace PingMyNetwork.Helper.Utilities
{
    /// <summary>
    /// The system's own accent colour, for the "Use system colors" setting: Material You on
    /// Android 12+, the user's accent colour on Windows, and system blue, the default tint, on
    /// iOS and Mac.
    /// </summary>
    /// <remarks>
    /// Screens use the live resources "Accent" and "AccentTint" (DynamicResource), which hold the
    /// colour for the current theme. <see cref="Apply"/> refreshes them whenever the theme or the
    /// setting changes, so every screen follows straight away. Code that paints the accent itself
    /// listens to <see cref="Changed"/>.
    /// </remarks>
    public static class SystemAccent
    {
        #region Variables

        private static readonly string[] KEYS = ["AccentLight", "AccentDark", "AccentTintLight", "AccentTintDark"];

        #endregion

        #region Events

        /// <summary>
        /// Raised after the accent changed, for code that paints it rather than binding to it.
        /// </summary>
        public static event EventHandler? Changed;

        #endregion

        #region Methods

        /// <summary>
        /// Settles the accent: the system's when "Use system colors" is on and the system has one,
        /// otherwise the app's green, in the shade for the current theme.
        /// </summary>
        /// <param name="app">The application, whose resources are set.</param>
        public static void Apply(Application app)
        {
            // Without overrides, the green from Colors.xaml shows through.
            foreach (string key in KEYS)
            {
                app.Resources.Remove(key);
            }

            if (AppSettings.UseSystemColors && (Read() is (Color light, Color dark)))
            {
                app.Resources["AccentLight"] = light;
                app.Resources["AccentDark"] = dark;

                // The soft fills behind accent icons are the accent faintly over the card colour.
                app.Resources["AccentTintLight"] = Blend(light, ResourceLookup.Get("SurfaceLight", Colors.White), 0.12f);
                app.Resources["AccentTintDark"] = Blend(dark, ResourceLookup.Get("SurfaceDark", Colors.Black), 0.18f);
            }

            bool isDark = app.RequestedTheme == AppTheme.Dark;

            app.Resources["Accent"] = ResourceLookup.Get(isDark ? "AccentDark" : "AccentLight", Colors.Green);
            app.Resources["AccentTint"] = ResourceLookup.Get(isDark ? "AccentTintDark" : "AccentTintLight", Colors.Green);

            Changed?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Whether this device has a system accent to follow.
        /// </summary>
        /// <returns>True when Apply would change anything.</returns>
        public static bool IsAvailable()
        {
            return Read() is not null;
        }

        /// <summary>
        /// The system accent for light and dark backgrounds. The light one is dark enough for
        /// white text on it, and the dark one light enough for black text, as primary buttons need.
        /// </summary>
        /// <returns>The pair, or null when the system has no accent of its own.</returns>
        private static (Color Light, Color Dark)? Read()
        {
#if ANDROID
            // Material You: the wallpaper-based palette, Android 12 and later.
            if (!OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                return null;
            }

            Android.Content.Context context = Platform.AppContext;

            return (
                FromAndroid(context.GetColor(Android.Resource.Color.SystemAccent1600)),
                FromAndroid(context.GetColor(Android.Resource.Color.SystemAccent1200)));
#elif WINDOWS
            Windows.UI.ViewManagement.UISettings settings = new();

            return (
                FromWindows(settings.GetColorValue(Windows.UI.ViewManagement.UIColorType.AccentDark1)),
                FromWindows(settings.GetColorValue(Windows.UI.ViewManagement.UIColorType.AccentLight2)));
#elif IOS || MACCATALYST
            // iOS lets users pick no accent; the system's own tint is system blue.
            UIKit.UIColor tint = UIKit.UIColor.SystemBlue;

            return (
                FromApple(tint.GetResolvedColor(UIKit.UITraitCollection.FromUserInterfaceStyle(UIKit.UIUserInterfaceStyle.Light))),
                FromApple(tint.GetResolvedColor(UIKit.UITraitCollection.FromUserInterfaceStyle(UIKit.UIUserInterfaceStyle.Dark))));
#else
            return null;
#endif
        }

        /// <summary>
        /// Mixes a colour over a background.
        /// </summary>
        /// <param name="colour">The colour.</param>
        /// <param name="background">The background.</param>
        /// <param name="amount">How much of the colour, 0 to 1.</param>
        /// <returns>The mix.</returns>
        private static Color Blend(Color colour, Color background, float amount)
        {
            return new Color(
                background.Red + ((colour.Red - background.Red) * amount),
                background.Green + ((colour.Green - background.Green) * amount),
                background.Blue + ((colour.Blue - background.Blue) * amount));
        }

#if ANDROID
        /// <summary>
        /// Converts a packed Android colour.
        /// </summary>
        /// <param name="argb">The colour as ARGB.</param>
        /// <returns>The colour.</returns>
        private static Color FromAndroid(int argb)
        {
            return Color.FromUint(unchecked((uint)argb));
        }
#elif WINDOWS
        /// <summary>
        /// Converts a Windows colour.
        /// </summary>
        /// <param name="colour">The colour.</param>
        /// <returns>The colour.</returns>
        private static Color FromWindows(Windows.UI.Color colour)
        {
            return Color.FromRgb(colour.R, colour.G, colour.B);
        }
#elif IOS || MACCATALYST
        /// <summary>
        /// Converts a UIKit colour.
        /// </summary>
        /// <param name="colour">The colour.</param>
        /// <returns>The colour.</returns>
        private static Color FromApple(UIKit.UIColor colour)
        {
            colour.GetRGBA(out nfloat red, out nfloat green, out nfloat blue, out _);

            return new Color((float)red, (float)green, (float)blue);
        }
#endif

        #endregion
    }
}
