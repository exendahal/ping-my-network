using MauiWifiManager.Abstractions;

namespace PingMyNetwork.Controls
{
    /// <summary>
    /// One network on the channel chart.
    /// </summary>
    /// <param name="Channel">Its channel.</param>
    /// <param name="RssiDbm">How strong it is here.</param>
    /// <param name="IsOwn">Whether it is one of the user's own access points.</param>
    /// <param name="Label">Name drawn over the peak, or null for none.</param>
    public sealed record ChannelCurve(int Channel, int RssiDbm, bool IsOwn, string? Label);

    /// <summary>
    /// Every network on one band, drawn as a curve across the channels it occupies with its height
    /// set by signal strength: the classic channel analyzer view.
    /// </summary>
    /// <remarks>
    /// On 2.4 GHz channels are 5 MHz apart and a network is 20 MHz wide, so each curve spans five
    /// channel numbers and overlaps its neighbours, which is exactly what the chart is for. On 5 and
    /// 6 GHz channels are listed side by side (only those in use), since they do not overlap that way.
    /// Text other than axis labels lives in real labels outside the chart, for screen readers.
    /// </remarks>
    public sealed class ChannelChartDrawable : IDrawable
    {
        #region Variables

        /// <summary>
        /// Weakest signal on the axis.
        /// </summary>
        private const float MIN_DBM = -100f;

        /// <summary>
        /// Strongest signal on the axis.
        /// </summary>
        private const float MAX_DBM = -30f;

        /// <summary>
        /// Room for the dBm labels on the left.
        /// </summary>
        private const float LEFT_AXIS = 36f;

        /// <summary>
        /// Room for the channel labels underneath.
        /// </summary>
        private const float BOTTOM_AXIS = 22f;

        /// <summary>
        /// Room above the highest curve for its label.
        /// </summary>
        private const float TOP_MARGIN = 18f;

        /// <summary>
        /// Gridlines, in dBm.
        /// </summary>
        private static readonly float[] GRID_DBM = [-50f, -70f, -90f];

        #endregion

        #region Properties

        /// <summary>
        /// The band being drawn.
        /// </summary>
        public WifiFrequencyBand Band { get; set; } = WifiFrequencyBand.Band2_4GHz;

        /// <summary>
        /// The networks to draw.
        /// </summary>
        public IReadOnlyList<ChannelCurve> Curves { get; set; } = [];

        /// <summary>
        /// Channel suggested as quietest, marked on the axis; null for none.
        /// </summary>
        public int? SuggestedChannel { get; set; }

        /// <summary>
        /// Colour of the user's own network.
        /// </summary>
        public Color OwnColor { get; set; } = Colors.Green;

        /// <summary>
        /// Colour of everybody else's networks.
        /// </summary>
        public Color OtherColor { get; set; } = Colors.Gray;

        /// <summary>
        /// Colour of axis text and gridlines.
        /// </summary>
        public Color AxisColor { get; set; } = Colors.Gray;

        /// <summary>
        /// Growth of the curves from 0 to 1, for the entrance animation.
        /// </summary>
        public float Progress { get; set; } = 1f;

        #endregion

        #region Methods

        /// <summary>
        /// Draws the gridlines, the curves and the axes.
        /// </summary>
        /// <param name="canvas">The canvas.</param>
        /// <param name="dirtyRect">The area to draw in.</param>
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            RectF plot = new(
                dirtyRect.X + LEFT_AXIS,
                dirtyRect.Y + TOP_MARGIN,
                dirtyRect.Width - LEFT_AXIS - 8f,
                dirtyRect.Height - TOP_MARGIN - BOTTOM_AXIS);

            if ((plot.Width <= 0) || (plot.Height <= 0))
            {
                return;
            }

            List<int> slots = Slots();

            canvas.SaveState();
            DrawGrid(canvas, dirtyRect, plot);

            // Others first, so the user's own network is drawn on top.
            foreach (ChannelCurve curve in Curves.OrderBy(curve => curve.IsOwn).ThenBy(curve => curve.RssiDbm))
            {
                DrawCurve(canvas, plot, slots, curve);
            }

            DrawChannelAxis(canvas, plot, slots);
            canvas.RestoreState();
        }

        /// <summary>
        /// The channel positions along the x axis.
        /// </summary>
        /// <returns>On 2.4 GHz every channel from 1 to 13; otherwise the channels in use.</returns>
        private List<int> Slots()
        {
            if (Band == WifiFrequencyBand.Band2_4GHz)
            {
                return Enumerable.Range(1, 13).ToList();
            }

            IEnumerable<int> used = Curves.Select(curve => curve.Channel);

            if (SuggestedChannel is int suggested)
            {
                used = used.Append(suggested);
            }

            return used.Distinct().Order().ToList();
        }

        /// <summary>
        /// Where a channel sits horizontally.
        /// </summary>
        /// <param name="plot">The plot area.</param>
        /// <param name="slots">The channel positions.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>The x coordinate.</returns>
        private float XOf(RectF plot, List<int> slots, float channel)
        {
            if (Band == WifiFrequencyBand.Band2_4GHz)
            {
                // Two extra channels each side, so the outer curves are not cut off.
                return plot.X + (plot.Width * (channel + 1f) / 16f);
            }

            int index = slots.IndexOf((int)channel);
            float slot = plot.Width / Math.Max(1, slots.Count);

            return plot.X + (slot * (index + 0.5f));
        }

        /// <summary>
        /// Where a signal strength sits vertically.
        /// </summary>
        /// <param name="plot">The plot area.</param>
        /// <param name="dbm">The strength.</param>
        /// <returns>The y coordinate.</returns>
        private static float YOf(RectF plot, float dbm)
        {
            float clamped = Math.Clamp(dbm, MIN_DBM, MAX_DBM);

            return plot.Bottom - (plot.Height * (clamped - MIN_DBM) / (MAX_DBM - MIN_DBM));
        }

        /// <summary>
        /// Horizontal gridlines with their dBm labels.
        /// </summary>
        /// <param name="canvas">The canvas.</param>
        /// <param name="bounds">The whole area.</param>
        /// <param name="plot">The plot area.</param>
        private void DrawGrid(ICanvas canvas, RectF bounds, RectF plot)
        {
            canvas.StrokeColor = AxisColor.WithAlpha(0.25f);
            canvas.StrokeSize = 1f;
            canvas.FontColor = AxisColor;
            canvas.FontSize = 10f;

            foreach (float dbm in GRID_DBM)
            {
                float y = YOf(plot, dbm);

                canvas.DrawLine(plot.X, y, plot.Right, y);
                canvas.DrawString($"{dbm:0}", bounds.X, y - 7f, LEFT_AXIS - 6f, 14f, HorizontalAlignment.Right, VerticalAlignment.Center);
            }

            canvas.StrokeColor = AxisColor.WithAlpha(0.5f);
            canvas.DrawLine(plot.X, plot.Bottom, plot.Right, plot.Bottom);
        }

        /// <summary>
        /// One network: a filled bump across its channels, peaking at its signal strength.
        /// </summary>
        /// <param name="canvas">The canvas.</param>
        /// <param name="plot">The plot area.</param>
        /// <param name="slots">The channel positions.</param>
        /// <param name="curve">The network.</param>
        private void DrawCurve(ICanvas canvas, RectF plot, List<int> slots, ChannelCurve curve)
        {
            float centre = XOf(plot, slots, curve.Channel);
            float halfWidth = Band == WifiFrequencyBand.Band2_4GHz
                ? XOf(plot, slots, curve.Channel + 2f) - centre
                : (plot.Width / Math.Max(1, slots.Count)) * 0.45f;
            float peak = YOf(plot, MIN_DBM + ((curve.RssiDbm - MIN_DBM) * Progress));
            Color colour = curve.IsOwn ? OwnColor : OtherColor;

            PathF path = new();

            path.MoveTo(centre - halfWidth, plot.Bottom);
            path.CurveTo(
                centre - (halfWidth * 0.55f), peak,
                centre + (halfWidth * 0.55f), peak,
                centre + halfWidth, plot.Bottom);

            canvas.FillColor = colour.WithAlpha(curve.IsOwn ? 0.22f : 0.08f);
            canvas.FillPath(path);

            canvas.StrokeColor = colour.WithAlpha(curve.IsOwn ? 1f : 0.55f);
            canvas.StrokeSize = curve.IsOwn ? 2.5f : 1.5f;
            canvas.DrawPath(path);

            if (curve.Label is string label && (Progress >= 1f))
            {
                canvas.FontColor = colour;
                canvas.FontSize = 11f;

                // The bezier peaks at three quarters of its control height.
                float labelY = plot.Bottom - ((plot.Bottom - peak) * 0.75f) - 16f;

                canvas.DrawString(label, centre - 80f, labelY, 160f, 14f, HorizontalAlignment.Center, VerticalAlignment.Bottom);
            }
        }

        /// <summary>
        /// Channel numbers under the plot, with the suggested channel emphasised.
        /// </summary>
        /// <param name="canvas">The canvas.</param>
        /// <param name="plot">The plot area.</param>
        /// <param name="slots">The channel positions.</param>
        private void DrawChannelAxis(ICanvas canvas, RectF plot, List<int> slots)
        {
            canvas.FontSize = 10f;

            foreach (int channel in slots)
            {
                float x = XOf(plot, slots, channel);
                bool isSuggested = channel == SuggestedChannel;

                canvas.FontColor = isSuggested ? OwnColor : AxisColor;
                canvas.Font = isSuggested ? Microsoft.Maui.Graphics.Font.DefaultBold : Microsoft.Maui.Graphics.Font.Default;
                canvas.DrawString(channel.ToString(), x - 16f, plot.Bottom + 4f, 32f, 14f, HorizontalAlignment.Center, VerticalAlignment.Top);
            }
        }

        #endregion
    }
}
