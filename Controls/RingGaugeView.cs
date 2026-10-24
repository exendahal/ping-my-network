using PingMyNetwork.Helper.Enums;
using PingMyNetwork.Helper.Utilities;

namespace PingMyNetwork.Controls
{
    /// <summary>
    /// The ring gauge with sill_app's animation: the arc sweeps to each new value while its colour
    /// cross-fades to the new status level, as one motion, so crossing a level never snaps.
    /// </summary>
    /// <remarks>
    /// Built in code: one GraphicsView and an animation. Reduced motion swaps the sweep for a short
    /// cross-fade. Values arriving faster than the sweep (several bindings changing at once) are
    /// coalesced into one animation per dispatcher pass, so the sweep is never restarted from its
    /// own target.
    /// </remarks>
    public sealed class RingGaugeView : ContentView
    {
        #region Variables

        private const string RING_ANIMATION = "RingSweep";

        /// <summary>
        /// Sweep length.
        /// </summary>
        private const uint SWEEP_MS = 600;

        /// <summary>
        /// Colour cross-fade length, inside the sweep.
        /// </summary>
        private const uint COLOUR_MS = 500;

        /// <summary>
        /// Cross-fade length under reduced motion.
        /// </summary>
        private const uint REDUCED_MS = 150;

        /// <summary>
        /// One frame at 60 fps.
        /// </summary>
        private const uint FRAME_RATE_MS = 16;

        /// <summary>
        /// The value, from 0 to 1.
        /// </summary>
        public static readonly BindableProperty FractionProperty = BindableProperty.Create(
            nameof(Fraction), typeof(float), typeof(RingGaugeView), 0f, propertyChanged: OnValueChanged);

        /// <summary>
        /// The status level, which picks the arc colour.
        /// </summary>
        public static readonly BindableProperty LevelProperty = BindableProperty.Create(
            nameof(Level), typeof(StatusLevel), typeof(RingGaugeView), StatusLevel.Good, propertyChanged: OnValueChanged);

        /// <summary>
        /// Arc thickness.
        /// </summary>
        public static readonly BindableProperty StrokeWidthProperty = BindableProperty.Create(
            nameof(StrokeWidth), typeof(float), typeof(RingGaugeView), 12f, propertyChanged: OnValueChanged);

        private readonly RingGaugeDrawable _Drawable = new();
        private readonly GraphicsView _Canvas = new();

        private float _ShownFraction;
        private Color _ShownColor = StatusPalette.Neutral;
        private bool _IsQueued;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the gauge.
        /// </summary>
        public RingGaugeView()
        {
            _Canvas.Drawable = _Drawable;
            Content = _Canvas;

            if (Application.Current is not null)
            {
                Application.Current.RequestedThemeChanged += (_, _) => Paint(_ShownFraction, StatusPalette.For(Level));
            }

            Paint(0f, StatusPalette.For(Level));
        }

        #endregion

        #region Properties

        /// <summary>
        /// The value, from 0 to 1.
        /// </summary>
        public float Fraction
        {
            get => (float)GetValue(FractionProperty);
            set => SetValue(FractionProperty, value);
        }

        /// <summary>
        /// The status level, which picks the arc colour.
        /// </summary>
        public StatusLevel Level
        {
            get => (StatusLevel)GetValue(LevelProperty);
            set => SetValue(LevelProperty, value);
        }

        /// <summary>
        /// Arc thickness.
        /// </summary>
        public float StrokeWidth
        {
            get => (float)GetValue(StrokeWidthProperty);
            set => SetValue(StrokeWidthProperty, value);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Queues one animation for however many values changed together.
        /// </summary>
        /// <param name="bindable">The gauge.</param>
        /// <param name="oldValue">Unused.</param>
        /// <param name="newValue">Unused.</param>
        private static void OnValueChanged(BindableObject bindable, object oldValue, object newValue)
        {
            RingGaugeView gauge = (RingGaugeView)bindable;

            if (gauge._IsQueued)
            {
                return;
            }

            gauge._IsQueued = true;
            gauge.Dispatcher.Dispatch(() =>
            {
                gauge._IsQueued = false;
                gauge.Animate();
            });
        }

        /// <summary>
        /// Sweeps from the value on screen to the bound one, cross-fading the colour on the way.
        /// </summary>
        private void Animate()
        {
            this.AbortAnimation(RING_ANIMATION);

            float from = _ShownFraction;
            float to = Fraction;
            Color fromColor = _ShownColor;
            Color toColor = StatusPalette.For(Level);
            Color current = fromColor;

            _Drawable.StrokeWidth = StrokeWidth;

            // Not on screen yet: nothing to animate from.
            if (Handler is null)
            {
                Paint(to, toColor);
                return;
            }

            bool reduced = MotionSettings.PrefersReducedMotion;
            uint length = reduced ? REDUCED_MS : SWEEP_MS;
            double colourEnd = reduced ? 1.0 : (double)COLOUR_MS / SWEEP_MS;

            Animation parent = new();

            parent.Add(0, 1, new Animation(
                progress => Paint((float)progress, current),
                reduced ? to : from,
                to,
                reduced ? Easing.Linear : Easing.CubicOut));

            parent.Add(0, colourEnd, new Animation(
                progress => current = StatusPalette.Lerp(fromColor, toColor, (float)progress),
                0,
                1,
                Easing.Linear));

            parent.Commit(this, RING_ANIMATION, rate: FRAME_RATE_MS, length: length);
        }

        /// <summary>
        /// Draws the gauge at a value and colour, and remembers them as what is on screen.
        /// </summary>
        /// <param name="fraction">How far round the arc runs.</param>
        /// <param name="color">The arc colour.</param>
        private void Paint(float fraction, Color color)
        {
            _ShownFraction = fraction;
            _ShownColor = color;

            _Drawable.Fraction = fraction;
            _Drawable.ArcColor = color;
            _Drawable.TrackColor = StatusPalette.Track;
            _Drawable.KnobFill = StatusPalette.CardBackground;

            _Canvas.Invalidate();
        }

        #endregion
    }
}
