using PingMyNetwork.Helper.Enums;
using PingMyNetwork.Helper.Utilities;

namespace PingMyNetwork.Controls
{
    /// <summary>
    /// The most recent pings as bars, newest on the right. A lost ping is a full-height faint bar
    /// in the "bad" colour, so gaps stand out instead of reading as a very fast reply.
    /// </summary>
    /// <remarks>
    /// The scale follows the slowest reply on screen, never dropping below <see cref="MIN_SCALE_MS"/>,
    /// so a quiet LAN still shows variation without one spike flattening everything for good.
    /// </remarks>
    public sealed class LatencyChartDrawable : IDrawable
    {
        #region Variables

        /// <summary>
        /// Pings kept on screen.
        /// </summary>
        public const int CAPACITY = 60;

        /// <summary>
        /// Fewest bar slots, so a short run of 4 pings still has reasonably slim bars.
        /// </summary>
        private const int MIN_SLOTS = 20;

        /// <summary>
        /// The smallest top of the scale, in milliseconds.
        /// </summary>
        private const float MIN_SCALE_MS = 20f;

        /// <summary>
        /// Space between bars, as a share of each slot.
        /// </summary>
        private const float GAP_SHARE = 0.3f;

        #endregion

        #region Properties

        /// <summary>
        /// Round trips in milliseconds, oldest first; null for a lost ping.
        /// </summary>
        public IReadOnlyList<long?> Samples { get; set; } = [];

        /// <summary>
        /// Colour of the baseline.
        /// </summary>
        public Color AxisColor { get; set; } = Colors.Gray;

        #endregion

        #region Methods

        /// <summary>
        /// Draws the baseline and one bar per ping.
        /// </summary>
        /// <param name="canvas">The canvas.</param>
        /// <param name="dirtyRect">The area to draw in.</param>
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if ((dirtyRect.Width <= 0) || (dirtyRect.Height <= 0))
            {
                return;
            }

            canvas.SaveState();

            canvas.StrokeColor = AxisColor.WithAlpha(0.4f);
            canvas.StrokeSize = 1f;
            canvas.DrawLine(dirtyRect.X, dirtyRect.Bottom - 0.5f, dirtyRect.Right, dirtyRect.Bottom - 0.5f);

            float scale = Math.Max(MIN_SCALE_MS, Samples.Select(sample => sample ?? 0).DefaultIfEmpty().Max() * 1.15f);
            float slot = dirtyRect.Width / Math.Clamp(Samples.Count, MIN_SLOTS, CAPACITY);
            float barWidth = Math.Max(1f, slot * (1 - GAP_SHARE));
            float startX = dirtyRect.Right - (slot * Samples.Count);

            for (var i = 0; i < Samples.Count; i++)
            {
                float x = startX + (slot * i) + ((slot - barWidth) / 2);

                if (Samples[i] is long milliseconds)
                {
                    // Even a 0 ms reply gets a sliver, so it reads as "answered".
                    float height = Math.Max(2f, (dirtyRect.Height - 2) * milliseconds / scale);

                    canvas.FillColor = StatusPalette.For(NetworkFormat.LatencyLevel(milliseconds));
                    canvas.FillRoundedRectangle(x, dirtyRect.Bottom - 1 - height, barWidth, height, Math.Min(2f, barWidth / 2));
                }
                else
                {
                    canvas.FillColor = StatusPalette.For(StatusLevel.Bad).WithAlpha(0.22f);
                    canvas.FillRectangle(x, dirtyRect.Y, barWidth, dirtyRect.Height - 1);
                }
            }

            canvas.RestoreState();
        }

        #endregion
    }
}
