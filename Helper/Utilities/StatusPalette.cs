using PingMyNetwork.Helper.Enums;

namespace PingMyNetwork.Helper.Utilities
{
    /// <summary>
    /// The brand and status colours, for code that paints rather than binds: drawables, animated
    /// tints and the navigation chrome.
    /// </summary>
    /// <remarks>
    /// Every value comes from Colors.xaml, so the palette has one source; the fallback only shows
    /// if a key goes missing.
    /// </remarks>
    public static class StatusPalette
    {
        #region Properties

        /// <summary>
        /// Whether the app is currently drawing in dark mode.
        /// </summary>
        public static bool IsDark => Application.Current?.RequestedTheme == AppTheme.Dark;

        /// <summary>
        /// Inactive icons, captions and the "no data" state.
        /// </summary>
        public static Color Neutral => Themed("Caption");

        /// <summary>
        /// The page background.
        /// </summary>
        public static Color PageBackground => Themed("Page");

        /// <summary>
        /// The accent, used for selection and interactive elements: the app's green, or the
        /// system's colour when "Use system colors" is on (SystemAccent).
        /// </summary>
        public static Color Accent => Themed("Accent");

        /// <summary>
        /// The surface cards sit on.
        /// </summary>
        public static Color CardBackground => Themed("Surface");

        /// <summary>
        /// The unfilled part of a gauge or meter.
        /// </summary>
        public static Color Track => Themed("Track");

        /// <summary>
        /// Primary text.
        /// </summary>
        public static Color Text => Themed("OnSurface");

        #endregion

        #region Methods

        /// <summary>
        /// The colour for a status level in the current theme.
        /// </summary>
        /// <param name="level">The level to colour.</param>
        /// <returns>The status colour, e.g. StatusGoodDark.</returns>
        public static Color For(StatusLevel level)
        {
            return Themed($"Status{(Enum.IsDefined(level) ? level : StatusLevel.Notice)}");
        }

        /// <summary>
        /// A Colors.xaml colour in the current theme.
        /// </summary>
        /// <param name="name">The key without its Light or Dark suffix, e.g. "Surface".</param>
        /// <returns>The colour.</returns>
        public static Color Themed(string name)
        {
            return ResourceLookup.Get(name + (IsDark ? "Dark" : "Light"), Colors.Gray);
        }

        /// <summary>
        /// A soft card background tinted towards a status colour.
        /// </summary>
        /// <param name="level">The level to tint towards.</param>
        /// <returns>The tinted colour.</returns>
        public static Color SurfaceTint(StatusLevel level)
        {
            return Lerp(CardBackground, For(level), IsDark ? 0.14f : 0.10f);
        }

        /// <summary>
        /// A card outline tinted towards a status colour.
        /// </summary>
        /// <param name="level">The level to tint towards.</param>
        /// <returns>The tinted colour.</returns>
        public static Color OutlineTint(StatusLevel level)
        {
            return Lerp(CardBackground, For(level), IsDark ? 0.36f : 0.28f);
        }

        /// <summary>
        /// Interpolates between two colours, alpha included.
        /// </summary>
        /// <param name="from">The colour at progress 0.</param>
        /// <param name="to">The colour at progress 1.</param>
        /// <param name="progress">How far along, from 0 to 1.</param>
        /// <returns>The blended colour.</returns>
        public static Color Lerp(Color from, Color to, float progress)
        {
            return new Color(
                from.Red + ((to.Red - from.Red) * progress),
                from.Green + ((to.Green - from.Green) * progress),
                from.Blue + ((to.Blue - from.Blue) * progress),
                from.Alpha + ((to.Alpha - from.Alpha) * progress));
        }

        #endregion
    }
}
