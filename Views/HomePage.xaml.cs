using System.ComponentModel;
using PingMyNetwork.Controls;
using PingMyNetwork.Helper.Enums;
using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.ViewModels;

namespace PingMyNetwork.Views
{
    /// <summary>
    /// The Home screen. Code-behind holds only view concerns: the gauge animation, status colours
    /// and the layout for the current width.
    /// </summary>
    public partial class HomePage : ContentPage
    {
        #region Variables

        private const string RING_ANIMATION = "RingSweep";

        /// <summary>
        /// Gauge sweep length, as in sill_app.
        /// </summary>
        private const uint RING_SWEEP_MS = 600;

        /// <summary>
        /// Colour cross-fade length, inside the sweep so crossing a level never snaps.
        /// </summary>
        private const uint COLOUR_CROSS_FADE_MS = 500;

        /// <summary>
        /// Cross-fade used instead of the sweep when the system asks for reduced motion.
        /// </summary>
        private const uint REDUCED_MOTION_MS = 150;

        /// <summary>
        /// One frame at 60 fps.
        /// </summary>
        private const uint FRAME_RATE_MS = 16;

        /// <summary>
        /// Gauge stroke width.
        /// </summary>
        private const float RING_STROKE = 12f;

        private readonly HomeViewModel _ViewModel;
        private readonly RingGaugeDrawable _Ring = new() { StrokeWidth = RING_STROKE };

        private float _RingFraction;
        private Color _RingColor = StatusPalette.Neutral;
        private LayoutWidth? _AppliedWidth;
        private bool _IsShowing;
        private bool _IsRingQueued;
        private bool _IsTilesQueued;
        private int _TileColumns = 2;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the page.
        /// </summary>
        /// <param name="viewModel">The Home view model.</param>
        public HomePage(HomeViewModel viewModel)
        {
            InitializeComponent();

            _ViewModel = viewModel;
            BindingContext = viewModel;

            Ring.Drawable = _Ring;
            HeroSkeleton.Blocks = SkeletonView.HERO;

            AppShell.ApplyTransition(this);

            _ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            SizeChanged += (_, _) => ApplyLayout();

            // A tile appearing or disappearing (Wi-Fi only tiles, unknown link speed) re-flows the rest.
            foreach (IView tile in Tiles.Children)
            {
                ((BindableObject)tile).PropertyChanged += OnTilePropertyChanged;
            }

            if (Application.Current is not null)
            {
                Application.Current.RequestedThemeChanged += (_, _) => PaintAll(animated: false);
            }

            PaintAll(animated: false);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Starts monitoring and brings the gauge up to date.
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();

            _IsShowing = true;
            _ViewModel.Start();

            PaintAll(animated: false);
        }

        /// <summary>
        /// Stops the gauge animating while the page is hidden.
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            _IsShowing = false;
            this.AbortAnimation(RING_ANIMATION);
        }

        /// <summary>
        /// Repaints only what a changed value affects.
        /// </summary>
        /// <param name="sender">The view model.</param>
        /// <param name="e">Which property changed.</param>
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(HomeViewModel.SignalFraction):
                case nameof(HomeViewModel.SignalLevel):
                    QueueRing();
                    break;

                case nameof(HomeViewModel.Level):
                case nameof(HomeViewModel.Status):
                    PaintStatus();
                    break;

                case nameof(HomeViewModel.IsLoading):
                    HeroContent.Opacity = _ViewModel.IsLoading ? 0 : 1;
                    break;
            }
        }

        /// <summary>
        /// Animates the gauge once for a snapshot, however many of its values changed. Fraction and
        /// level arrive as separate notifications; animating on each would restart the sweep from
        /// its own target and lose the motion.
        /// </summary>
        private void QueueRing()
        {
            if (_IsRingQueued)
            {
                return;
            }

            _IsRingQueued = true;

            Dispatcher.Dispatch(() =>
            {
                _IsRingQueued = false;
                AnimateRing(_ViewModel.SignalFraction, StatusPalette.For(_ViewModel.SignalLevel), _IsShowing);
            });
        }

        /// <summary>
        /// Paints every coloured element, for first show and theme changes.
        /// </summary>
        /// <param name="animated">Whether the gauge should sweep to its value.</param>
        private void PaintAll(bool animated)
        {
            _Ring.TrackColor = StatusPalette.Track;
            _Ring.KnobFill = StatusPalette.CardBackground;
            TransportBadge.BackgroundColor = StatusPalette.Lerp(StatusPalette.CardBackground, StatusPalette.Accent, 0.12f);
            TransportIcon.TextColor = StatusPalette.Accent;
            HeroContent.Opacity = _ViewModel.IsLoading ? 0 : 1;

            PaintStatus();
            AnimateRing(_ViewModel.SignalFraction, StatusPalette.For(_ViewModel.SignalLevel), animated);
        }

        /// <summary>
        /// Tints the status chip with the status colour. Text stays readable on the tint because
        /// the tint is mostly the card colour.
        /// </summary>
        private void PaintStatus()
        {
            StatusLevel level = _ViewModel.Level;
            Color colour = level == StatusLevel.Notice ? StatusPalette.Neutral : StatusPalette.For(level);

            StatusChip.BackgroundColor = level == StatusLevel.Notice
                ? Colors.Transparent
                : StatusPalette.SurfaceTint(level);
            StatusChip.Stroke = level == StatusLevel.Notice ? StatusPalette.Track : StatusPalette.OutlineTint(level);
            StatusIconLabel.TextColor = colour;
            StatusTitleLabel.TextColor = colour;
        }

        /// <summary>
        /// Moves the sweep and the colour together as one motion, so crossing a level never snaps
        /// (sill_app's ring animation).
        /// </summary>
        /// <param name="targetFraction">Where the arc should end up.</param>
        /// <param name="targetColor">The colour it should end up.</param>
        /// <param name="animated">False to jump straight to the target.</param>
        private void AnimateRing(float targetFraction, Color targetColor, bool animated)
        {
            this.AbortAnimation(RING_ANIMATION);

            float fromFraction = _RingFraction;
            Color fromColor = _RingColor;

            _RingFraction = targetFraction;
            _RingColor = targetColor;

            if (!animated)
            {
                PaintRing(targetFraction, targetColor);
                return;
            }

            // Reduced motion: the value still cross-fades so the change is visible, without the sweep.
            bool reduced = MotionSettings.PrefersReducedMotion;
            uint length = reduced ? REDUCED_MOTION_MS : RING_SWEEP_MS;
            double colourEnd = reduced ? 1.0 : (double)COLOUR_CROSS_FADE_MS / RING_SWEEP_MS;
            Color current = fromColor;

            Animation parent = new();

            parent.Add(0, 1, new Animation(
                progress => PaintRing((float)progress, current),
                reduced ? targetFraction : fromFraction,
                targetFraction,
                reduced ? Easing.Linear : Easing.CubicOut));

            parent.Add(0, colourEnd, new Animation(
                progress => current = StatusPalette.Lerp(fromColor, targetColor, (float)progress),
                0,
                1,
                Easing.Linear));

            parent.Commit(this, RING_ANIMATION, rate: FRAME_RATE_MS, length: length);
        }

        /// <summary>
        /// Pushes the arc state onto the drawable and redraws only the gauge.
        /// </summary>
        /// <param name="fraction">How far round the arc runs.</param>
        /// <param name="color">The arc colour.</param>
        private void PaintRing(float fraction, Color color)
        {
            _Ring.Fraction = fraction;
            _Ring.ArcColor = color;

            Ring.Invalidate();
        }

        /// <summary>
        /// Lays the page out for its width class: tiles under the hero on narrow windows, beside it
        /// on wide ones, with more tiles per row as space allows.
        /// </summary>
        private void ApplyLayout()
        {
            double windowWidth = Window?.Width ?? Width;
            LayoutWidth width = LayoutWidthUtility.From(windowWidth);

            PageGrid.Padding = LayoutWidthUtility.PagePadding(windowWidth);

            // The scroll view measures its content with unlimited width on Windows, so labels never
            // learn how much room they have and a long value pushes its tile wider than the column.
            // A definite width gives every child a real constraint to truncate against.
            PageGrid.WidthRequest = Math.Min(Width, PageGrid.MaximumWidthRequest);

            if (width == _AppliedWidth)
            {
                return;
            }

            _AppliedWidth = width;

            bool isWide = width >= LayoutWidth.Expanded;

            _TileColumns = width == LayoutWidth.Medium ? 3 : 2;

            PageGrid.ColumnDefinitions = isWide
                ? [new ColumnDefinition(new GridLength(3, GridUnitType.Star)), new ColumnDefinition(new GridLength(2, GridUnitType.Star))]
                : [new ColumnDefinition(GridLength.Star)];

            Grid.SetColumnSpan(Header, isWide ? 2 : 1);

            // Wide: the two stacks side by side. Narrow: the network first, then its facts.
            Grid.SetRow(RightColumn, isWide ? 1 : 2);
            Grid.SetColumn(RightColumn, isWide ? 1 : 0);

            ArrangeTiles();
        }

        /// <summary>
        /// Re-flows the tiles once the visibility bindings of this pass have all applied.
        /// </summary>
        /// <param name="sender">A tile.</param>
        /// <param name="e">Which property changed.</param>
        private void OnTilePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if ((e.PropertyName != nameof(IsVisible)) || _IsTilesQueued)
            {
                return;
            }

            _IsTilesQueued = true;

            Dispatcher.Dispatch(() =>
            {
                _IsTilesQueued = false;
                ArrangeTiles();
            });
        }

        /// <summary>
        /// Places the visible tiles left to right, top to bottom, in equal columns, so there is
        /// never a gap where a hidden tile would have been.
        /// </summary>
        private void ArrangeTiles()
        {
            List<View> visible = Tiles.Children.OfType<View>().Where(tile => tile.IsVisible).ToList();
            int rows = (visible.Count + _TileColumns - 1) / _TileColumns;

            Tiles.ColumnDefinitions.Clear();
            Tiles.RowDefinitions.Clear();

            for (var i = 0; i < _TileColumns; i++)
            {
                Tiles.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            }

            for (var i = 0; i < rows; i++)
            {
                Tiles.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }

            for (var i = 0; i < visible.Count; i++)
            {
                Grid.SetColumn(visible[i], i % _TileColumns);
                Grid.SetRow(visible[i], i / _TileColumns);
            }
        }

        /// <summary>
        /// Opens network details.
        /// </summary>
        /// <param name="sender">The details row.</param>
        /// <param name="e">Unused.</param>
        private void OnDetailsTapped(object? sender, EventArgs e)
        {
            _ViewModel.OpenDetailsCommand.Execute(null);
        }

        #endregion
    }
}
