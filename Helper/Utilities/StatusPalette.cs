using PingMyNetwork.Helper.Enums;

namespace PingMyNetwork.Helper.Utilities
{
    /// <summary>
    /// The brand and status colours, for code that paints rather than binds: drawables, animated
    /// tints and the navigation chrome.
    /// </summary>
    /// <remarks>
    /// Carried over from sill_app's BandPalette. Light values are Apple's accessible variants, which
    /// reach at least 3:1 on a light surface; dark values are the standard system colours.
    /// </remarks>
    public static class StatusPalette
    {
        #region Variables

        private static readonly Color[] LIGHT_LEVELS =
        [
            // accessibleSystemGreen
            Color.FromArgb("#248A3D"),

            // A darkened systemYellow; the standard one cannot reach 3:1 on a light surface.
            Color.FromArgb("#8A6D00"),

            // accessibleSystemOrange
            Color.FromArgb("#B25000"),

            // accessibleSystemRed
            Color.FromArgb("#D70015"),

            // accessibleSystemPurple
            Color.FromArgb("#8944AB"),
        ];

        private static readonly Color[] DARK_LEVELS =
        [
            Color.FromArgb("#30D158"),
            Color.FromArgb("#FFD60A"),
            Color.FromArgb("#FF9F0A"),
            Color.FromArgb("#FF453A"),
            Color.FromArgb("#BF5AF2"),
        ];

        private static readonly Color NEUTRAL = Color.FromArgb("#8E8E93");

        private static readonly Color LIGHT_PAGE = Color.FromArgb("#F2F2F7");

        private static readonly Color DARK_PAGE = Color.FromArgb("#000000");

        private static readonly Color LIGHT_CARD = Color.FromArgb("#FFFFFF");

        private static readonly Color DARK_CARD = Color.FromArgb("#1C1C1E");

        private static readonly Color LIGHT_TRACK = Color.FromArgb("#E5E5EA");

        private static readonly Color DARK_TRACK = Color.FromArgb("#2C2C2E");

        private static readonly Color LIGHT_ACCENT = Color.FromArgb("#248A3D");

        private static readonly Color DARK_ACCENT = Color.FromArgb("#30D158");

        #endregion

        #region Properties

        /// <summary>
        /// Whether the app is currently drawing in dark mode.
        /// </summary>
        public static bool IsDark => Application.Current?.RequestedTheme == AppTheme.Dark;

        /// <summary>
        /// Inactive icons, captions and the "no data" state.
        /// </summary>
        public static Color Neutral => NEUTRAL;

        /// <summary>
        /// The page background.
        /// </summary>
        public static Color PageBackground => IsDark ? DARK_PAGE : LIGHT_PAGE;

        /// <summary>
        /// The accent, used for selection and interactive elements: the app's green, or the
        /// system's colour when "Use system colors" is on (SystemAccent).
        /// </summary>
        public static Color Accent => IsDark
            ? ResourceLookup.Get("AccentDark", DARK_ACCENT)
            : ResourceLookup.Get("AccentLight", LIGHT_ACCENT);

        /// <summary>
        /// The surface cards sit on.
        /// </summary>
        public static Color CardBackground => IsDark ? DARK_CARD : LIGHT_CARD;

        /// <summary>
        /// The unfilled part of a gauge or meter.
        /// </summary>
        public static Color Track => IsDark ? DARK_TRACK : LIGHT_TRACK;

        #endregion

        #region Methods

        /// <summary>
        /// The colour for a status level in the current theme.
        /// </summary>
        /// <param name="level">The level to colour.</param>
        /// <returns>The status colour.</returns>
        public static Color For(StatusLevel level)
        {
            int index = Math.Clamp((int)level, 0, LIGHT_LEVELS.Length - 1);

            return IsDark ? DARK_LEVELS[index] : LIGHT_LEVELS[index];
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
