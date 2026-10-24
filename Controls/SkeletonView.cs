using PingMyNetwork.Helper.Utilities;

namespace PingMyNetwork.Controls
{
    /// <summary>
    /// A skeleton placeholder that shimmers while real content loads.
    /// </summary>
    /// <remarks>
    /// Built in code rather than XAML because it is one child and a loop; a markup file would add
    /// a second place to look for no benefit.
    ///
    /// The sweep runs only while the view is actually visible, so a skeleton left behind on a page
    /// the user navigated away from cannot keep a 60 fps animation alive in the background.
    /// </remarks>
    public sealed class SkeletonView : ContentView
    {
        #region Variables

        private const string SWEEP_ANIMATION = "SkeletonSweep";

        /// <summary>
        /// One pass of the highlight, matching the PRD's 1200 ms shimmer loop.
        /// </summary>
        private const uint SWEEP_MS = 1200;

        /// <summary>
        /// One frame at 60 fps.
        /// </summary>
        private const uint FRAME_RATE_MS = 16;

        /// <summary>
        /// Blocks laid out like a summary figure: caption over value.
        /// </summary>
        public static readonly IReadOnlyList<RectF> STAT_TILE =
        [
            new RectF(0f, 0.16f, 0.55f, 0.16f),
            new RectF(0f, 0.50f, 0.62f, 0.30f),
        ];

        /// <summary>
        /// The Home hero card: the gauge on the left, the name, status and a sentence on the right.
        /// </summary>
        public static readonly IReadOnlyList<RectF> HERO =
        [
            new RectF(0f, 0.05f, 0.30f, 0.90f),
            new RectF(0.36f, 0.12f, 0.46f, 0.16f),
            new RectF(0.36f, 0.40f, 0.26f, 0.13f),
            new RectF(0.36f, 0.66f, 0.62f, 0.10f),
            new RectF(0.36f, 0.82f, 0.48f, 0.10f),
        ];

        /// <summary>
        /// A device row: icon, name over address, and a tag at the end.
        /// </summary>
        public static readonly IReadOnlyList<RectF> DEVICE_ROW =
        [
            new RectF(0f, 0.15f, 0.09f, 0.70f),
            new RectF(0.13f, 0.18f, 0.42f, 0.24f),
            new RectF(0.13f, 0.58f, 0.28f, 0.20f),
            new RectF(0.84f, 0.34f, 0.16f, 0.30f),
        ];

        /// <summary>
        /// A single block filling the canvas, for a chart area.
        /// </summary>
        public static readonly IReadOnlyList<RectF> BLOCK =
        [
            new RectF(0f, 0f, 1f, 1f),
        ];

        private readonly SkeletonDrawable _Drawable = new();
        private readonly GraphicsView _Canvas = new();

        private bool _Running;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the skeleton.
        /// </summary>
        public SkeletonView()
        {
            _Canvas.Drawable = _Drawable;

            Content = _Canvas;
            InputTransparent = true;

            PropertyChanged += OnSelfPropertyChanged;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Which set of placeholder blocks to draw.
        /// </summary>
        public IReadOnlyList<RectF> Blocks
        {
            get => _Drawable.Blocks;

            set
            {
                _Drawable.Blocks = value;
                _Canvas.Invalidate();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Starts or stops the sweep as the skeleton is shown and hidden.
        /// </summary>
        /// <param name="sender">This view.</param>
        /// <param name="e">The property that changed.</param>
        private void OnSelfPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(IsVisible))
            {
                return;
            }

            if (IsVisible)
            {
                Start();
            }
            else
            {
                Stop();
            }
        }

        /// <summary>
        /// Begins the sweep, unless the operating system has asked for reduced motion.
        /// </summary>
        private void Start()
        {
            ApplyTheme();

            if (_Running)
            {
                return;
            }

            // Reduce motion: flat blocks still communicate "content is coming", without the
            // looping highlight the PRD says to drop.
            if (MotionSettings.PrefersReducedMotion)
            {
                _Drawable.ShowHighlight = false;
                _Canvas.Invalidate();
                return;
            }

            _Running = true;
            _Drawable.ShowHighlight = true;

            new Animation(
                progress =>
                {
                    _Drawable.Progress = (float)progress;
                    _Canvas.Invalidate();
                },
                -HIGHLIGHT_START,
                1 + HIGHLIGHT_START,
                Easing.Linear)
                .Commit(this, SWEEP_ANIMATION, rate: FRAME_RATE_MS, length: SWEEP_MS, repeat: () => _Running);
        }

        /// <summary>
        /// Stops the sweep.
        /// </summary>
        private void Stop()
        {
            if (!_Running)
            {
                return;
            }

            _Running = false;
            this.AbortAnimation(SWEEP_ANIMATION);
        }

        /// <summary>
        /// Picks placeholder colours that sit just above the card they cover.
        /// </summary>
        private void ApplyTheme()
        {
            bool dark = StatusPalette.IsDark;

            _Drawable.BaseColor = dark
                ? Color.FromArgb("#2C2C2E")
                : Color.FromArgb("#E5E5EA");

            _Drawable.HighlightColor = dark
                ? Color.FromArgb("#3A3A3C")
                : Color.FromArgb("#F2F2F7");
        }

        /// <summary>
        /// How far past each edge the highlight starts and ends, so it enters and leaves cleanly.
        /// </summary>
        private const float HIGHLIGHT_START = 0.25f;

        #endregion
    }
}
