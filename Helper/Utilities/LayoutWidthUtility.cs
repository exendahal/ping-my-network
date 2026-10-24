using PingMyNetwork.Helper.Enums;

namespace PingMyNetwork.Helper.Utilities
{
    /// <summary>
    /// Maps a width in device-independent units to its width class.
    /// </summary>
    public static class LayoutWidthUtility
    {
        #region Variables

        /// <summary>
        /// Below this is Compact: bottom tab bar, one pane. Matches sill_app's WIDE_BREAKPOINT.
        /// </summary>
        public const double MEDIUM_MIN = 600;

        /// <summary>
        /// From here, Expanded: multi-column dashboard.
        /// </summary>
        public const double EXPANDED_MIN = 1024;

        /// <summary>
        /// From here, Large: content capped and centred.
        /// </summary>
        public const double LARGE_MIN = 1440;

        #endregion

        #region Methods

        /// <summary>
        /// The width class for a width.
        /// </summary>
        /// <param name="width">Width in device-independent units.</param>
        /// <returns>The class.</returns>
        public static LayoutWidth From(double width)
        {
            if (width >= LARGE_MIN)
            {
                return LayoutWidth.Large;
            }

            if (width >= EXPANDED_MIN)
            {
                return LayoutWidth.Expanded;
            }

            return width >= MEDIUM_MIN ? LayoutWidth.Medium : LayoutWidth.Compact;
        }

        /// <summary>
        /// Page padding for a width. Narrow pages leave room at the bottom for the floating tab bar;
        /// wide ones have the rail instead.
        /// </summary>
        /// <param name="width">The window's width, which decides whether the rail or the tab bar shows.</param>
        /// <returns>The padding token from Styles.xaml.</returns>
        public static Thickness PagePadding(double width)
        {
            string key = From(width) == LayoutWidth.Compact ? "PagePaddingCompact" : "PagePaddingWide";

            return ResourceLookup.Get(key, new Thickness(16));
        }

        #endregion
    }
}
