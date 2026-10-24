namespace PingMyNetwork.Controls
{
    /// <summary>
    /// A value gauge whose sweep, colour and knob position all come from the value it shows.
    /// </summary>
    /// <remarks>
    /// Copied from sill_app's air quality ring: a 270 degree arc with the gap at the bottom, and a
    /// ring-shaped knob riding the end of the filled section. Two arcs and a knob per frame sit
    /// well inside the 16 ms budget.
    ///
    /// Text is deliberately not drawn here. It lives as real labels on top, so it stays available
    /// to the screen reader and scales with dynamic type.
    /// </remarks>
    public sealed class RingGaugeDrawable : IDrawable
    {
        #region Variables

        /// <summary>
        /// Where the arc starts, in degrees counterclockwise from three o'clock. 225 puts the
        /// opening symmetrically at the bottom.
        /// </summary>
        private const float START_ANGLE = 225f;

        /// <summary>
        /// How far the arc sweeps clockwise, leaving a 90 degree gap at the bottom. A full circle
        /// would make "empty" and "full" meet, which reads as ambiguous on a value gauge.
        /// </summary>
        private const float SWEEP_DEGREES = 270f;

        /// <summary>
        /// Radius of the knob that marks the current value, as a multiple of the stroke width.
        /// </summary>
        private const float KNOB_RADIUS_RATIO = 0.78f;

        /// <summary>
        /// Stroke width of the knob outline, as a multiple of the arc stroke width.
        /// </summary>
        private const float KNOB_STROKE_RATIO = 0.34f;

        #endregion

        #region Properties

        /// <summary>
        /// Position of the value across its range, from 0 to 1.
        /// </summary>
        public float Fraction { get; set; }

        /// <summary>
        /// The status colour, cross-faded by the page rather than snapped.
        /// </summary>
        public Color ArcColor { get; set; } = Colors.Gray;

        /// <summary>
        /// The unfilled remainder of the gauge.
        /// </summary>
        public Color TrackColor { get; set; } = Colors.LightGray;

        /// <summary>
        /// Fill colour inside the knob, which is the page surface so the knob reads as a hole.
        /// </summary>
        public Color KnobFill { get; set; } = Colors.White;

        /// <summary>
        /// Stroke width of both arcs.
        /// </summary>
        public float StrokeWidth { get; set; } = 16f;

        /// <summary>
        /// Hides the knob while there is no value to mark.
        /// </summary>
        public bool ShowKnob { get; set; } = true;

        #endregion

        #region Methods

        /// <summary>
        /// Draws the track, the filled section and the knob.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="dirtyRect">The area being redrawn.</param>
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            float knobRadius = StrokeWidth * KNOB_RADIUS_RATIO;

            // The knob overhangs the arc, so inset by whichever is wider.
            float inset = Math.Max(StrokeWidth / 2f, knobRadius) + 2f;
            float size = Math.Min(dirtyRect.Width, dirtyRect.Height) - (inset * 2f);

            if (size <= 0)
            {
                return;
            }

            float radius = size / 2f;
            PointF centre = new(dirtyRect.Center.X, dirtyRect.Center.Y);
            RectF bounds = new(centre.X - radius, centre.Y - radius, size, size);
            float clamped = Math.Clamp(Fraction, 0f, 1f);
            float swept = SWEEP_DEGREES * clamped;

            canvas.SaveState();

            canvas.StrokeSize = StrokeWidth;
            canvas.StrokeLineCap = LineCap.Round;

            canvas.StrokeColor = TrackColor;
            canvas.DrawArc(
                bounds,
                START_ANGLE,
                START_ANGLE - SWEEP_DEGREES,
                clockwise: true,
                closed: false);

            if (swept > 0.5f)
            {
                canvas.StrokeColor = ArcColor;
                canvas.DrawArc(
                    bounds,
                    START_ANGLE,
                    START_ANGLE - swept,
                    clockwise: true,
                    closed: false);
            }

            if (ShowKnob)
            {
                DrawKnob(canvas, centre, radius, START_ANGLE - swept, knobRadius);
            }

            canvas.RestoreState();
        }

        /// <summary>
        /// Draws the ring-shaped knob that marks the current value.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="centre">Centre of the gauge.</param>
        /// <param name="radius">Radius of the arc the knob rides.</param>
        /// <param name="angleDegrees">Where on the arc the knob sits.</param>
        /// <param name="knobRadius">Radius of the knob itself.</param>
        private void DrawKnob(
            ICanvas canvas,
            PointF centre,
            float radius,
            float angleDegrees,
            float knobRadius)
        {
            double radians = angleDegrees * Math.PI / 180.0;

            // Canvas y grows downward, so the sine term is subtracted to keep angles conventional.
            float x = centre.X + (radius * (float)Math.Cos(radians));
            float y = centre.Y - (radius * (float)Math.Sin(radians));

            canvas.FillColor = KnobFill;
            canvas.FillCircle(x, y, knobRadius);

            canvas.StrokeSize = StrokeWidth * KNOB_STROKE_RATIO;
            canvas.StrokeColor = ArcColor;
            canvas.DrawCircle(x, y, knobRadius);
        }

        #endregion
    }
}
