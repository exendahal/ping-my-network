namespace PingMyNetwork.Controls
{
    /// <summary>
    /// The last minute or so of signal readings during a signal walk, as a line with a soft fill.
    /// </summary>
    /// <remarks>
    /// Readings are added and the view redrawn; points are never animated one by one, which keeps a
    /// live chart inside the frame budget.
    /// </remarks>
    public sealed class SignalHistoryDrawable : IDrawable
    {
        #region Variables

        /// <summary>
        /// Readings kept on screen.
        /// </summary>
        public const int CAPACITY = 60;

        /// <summary>
        /// Signal below this is weak; drawn as a dashed line for reference.
        /// </summary>
        private const float WEAK_PERCENT = 40f;

        #endregion

        #region Properties

        /// <summary>
        /// Signal readings from 0 to 100, oldest first.
        /// </summary>
        public IReadOnlyList<int> Samples { get; set; } = [];

        /// <summary>
        /// Line colour, which follows the current signal level.
        /// </summary>
        public Color LineColor { get; set; } = Colors.Green;

        /// <summary>
        /// Colour of the reference line.
        /// </summary>
        public Color AxisColor { get; set; } = Colors.Gray;

        #endregion

        #region Methods

        /// <summary>
        /// Draws the weak-signal reference and the readings.
        /// </summary>
        /// <param name="canvas">The canvas.</param>
        /// <param name="dirtyRect">The area to draw in.</param>
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (dirtyRect.Width <= 0 || dirtyRect.Height <= 0)
            {
                return;
            }

            canvas.SaveState();

            float weakY = YOf(dirtyRect, WEAK_PERCENT);

            canvas.StrokeColor = AxisColor.WithAlpha(0.4f);
            canvas.StrokeSize = 1f;
            canvas.StrokeDashPattern = [4f, 4f];
            canvas.DrawLine(dirtyRect.X, weakY, dirtyRect.Right, weakY);
            canvas.StrokeDashPattern = null;

            if (Samples.Count > 1)
            {
                // Readings fill the chart from the right, so the newest is always at the edge.
                float step = dirtyRect.Width / (CAPACITY - 1);
                float startX = dirtyRect.Right - (step * (Samples.Count - 1));
                PathF line = new();
                PathF area = new();

                area.MoveTo(startX, dirtyRect.Bottom);

                for (var i = 0; i < Samples.Count; i++)
                {
                    float x = startX + (step * i);
                    float y = YOf(dirtyRect, Samples[i]);

                    if (i == 0)
                    {
                        line.MoveTo(x, y);
                    }
                    else
                    {
                        line.LineTo(x, y);
                    }

                    area.LineTo(x, y);
                }

                area.LineTo(dirtyRect.Right, dirtyRect.Bottom);
                area.Close();

                canvas.FillColor = LineColor.WithAlpha(0.16f);
                canvas.FillPath(area);

                canvas.StrokeColor = LineColor;
                canvas.StrokeSize = 2f;
                canvas.StrokeLineJoin = LineJoin.Round;
                canvas.DrawPath(line);
            }

            canvas.RestoreState();
        }

        /// <summary>
        /// Where a percentage sits vertically, with a little room top and bottom.
        /// </summary>
        /// <param name="bounds">The area.</param>
        /// <param name="percent">The reading.</param>
        /// <returns>The y coordinate.</returns>
        private static float YOf(RectF bounds, float percent)
        {
            float inset = 4f;

            return bounds.Bottom - inset - ((bounds.Height - (inset * 2)) * Math.Clamp(percent, 0f, 100f) / 100f);
        }

        #endregion
    }
}
