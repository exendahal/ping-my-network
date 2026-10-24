namespace PingMyNetwork.Controls
{
    /// <summary>
    /// Placeholder blocks with a highlight sweeping across them, drawn while real content loads.
    /// </summary>
    /// <remarks>
    /// The blocks are given in normalised coordinates so one drawable serves tiles of any size,
    /// and the sweep gradient is built across the whole canvas rather than per block. That keeps
    /// the highlight travelling as one continuous band over a group of blocks, the way a real
    /// skeleton reads, instead of each block shimmering on its own.
    ///
    /// Nothing is allocated per frame except the paint, which the gradient needs.
    /// </remarks>
    public sealed class SkeletonDrawable : IDrawable
    {
        #region Variables

        /// <summary>
        /// Half-width of the highlight band, as a fraction of the canvas.
        /// </summary>
        private const float HIGHLIGHT_WIDTH = 0.18f;

        /// <summary>
        /// Corner radius of a placeholder block.
        /// </summary>
        private const float BLOCK_RADIUS = 5f;

        #endregion

        #region Properties

        /// <summary>
        /// Blocks to draw, in normalised coordinates where 0,0 is the top left of the canvas and
        /// 1,1 the bottom right.
        /// </summary>
        public IReadOnlyList<RectF> Blocks { get; set; } = [];

        /// <summary>
        /// Where the highlight currently sits, from just off the left to just off the right.
        /// </summary>
        public float Progress { get; set; }

        /// <summary>
        /// Resting colour of a placeholder block.
        /// </summary>
        public Color BaseColor { get; set; } = Colors.Gray;

        /// <summary>
        /// Colour of the travelling highlight.
        /// </summary>
        public Color HighlightColor { get; set; } = Colors.LightGray;

        /// <summary>
        /// Whether the highlight is drawn at all. Reduce-motion leaves flat blocks instead.
        /// </summary>
        public bool ShowHighlight { get; set; } = true;

        #endregion

        #region Methods

        /// <summary>
        /// Draws the placeholder blocks.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="dirtyRect">The area being redrawn.</param>
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (Blocks.Count == 0 || dirtyRect.Width <= 0 || dirtyRect.Height <= 0)
            {
                return;
            }

            canvas.SaveState();

            if (ShowHighlight)
            {
                canvas.SetFillPaint(BuildSweep(), dirtyRect);
            }
            else
            {
                canvas.FillColor = BaseColor;
            }

            foreach (RectF block in Blocks)
            {
                canvas.FillRoundedRectangle(
                    dirtyRect.X + (block.X * dirtyRect.Width),
                    dirtyRect.Y + (block.Y * dirtyRect.Height),
                    block.Width * dirtyRect.Width,
                    block.Height * dirtyRect.Height,
                    BLOCK_RADIUS);
            }

            canvas.RestoreState();
        }

        /// <summary>
        /// Builds the gradient that carries the highlight across the canvas.
        /// </summary>
        /// <returns>The paint to fill blocks with.</returns>
        private Paint BuildSweep()
        {
            float centre = Progress;
            float left = centre - HIGHLIGHT_WIDTH;
            float right = centre + HIGHLIGHT_WIDTH;

            List<PaintGradientStop> stops = [new PaintGradientStop(0f, BaseColor)];

            if (left > 0f)
            {
                stops.Add(new PaintGradientStop(left, BaseColor));
            }

            if (centre is > 0f and < 1f)
            {
                stops.Add(new PaintGradientStop(centre, HighlightColor));
            }

            if (right < 1f)
            {
                stops.Add(new PaintGradientStop(right, BaseColor));
            }

            stops.Add(new PaintGradientStop(1f, BaseColor));

            return new LinearGradientPaint
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0),
                GradientStops = [.. stops.OrderBy(stop => stop.Offset)],
            };
        }

        #endregion
    }
}
