using System.ComponentModel;
using PingMyNetwork.Controls;
using PingMyNetwork.Helper.Enums;
using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.ViewModels;

namespace PingMyNetwork.Views
{
    /// <summary>
    /// The Home screen. Code-behind holds only view concerns: status colours and the layout for the
    /// current width. The gauge animates itself (RingGaugeView).
    /// </summary>
    public partial class HomePage : ContentPage
    {
        #region Variables

        private readonly HomeViewModel _ViewModel;

        private LayoutWidth? _AppliedWidth;
        private bool _IsTilesQueued;
        private int _TileColumns = 2;

        private bool _IsShowing;

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

            HeroSkeleton.Blocks = SkeletonView.HERO;

            AppShell.ApplyTransition(this);

            SizeChanged += (_, _) => ApplyLayout();

            // A tile appearing or disappearing (Wi-Fi only tiles, unknown link speed) re-flows the rest.
            foreach (IView tile in Tiles.Children)
            {
                ((BindableObject)tile).PropertyChanged += OnTilePropertyChanged;
            }

            if (Application.Current is not null)
            {
                Application.Current.RequestedThemeChanged += (_, _) => PaintAll();
            }

            PaintAll();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Starts monitoring.
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            _IsShowing = true;

            // Subscribed per appearance: the view model outlives the page.
            _ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            PaintAll();

            // The page shows what it last knew straight away; fresh data waits for the slide.
            await AppShell.WhenSettledAsync();

            if (_IsShowing)
            {
                _ViewModel.Start();
            }
        }

        /// <summary>
        /// Stops following the view model while hidden.
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            _IsShowing = false;
            _ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
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
        /// Paints every coloured element, for first show and theme changes.
        /// </summary>
        private void PaintAll()
        {
            TransportBadge.BackgroundColor = StatusPalette.Lerp(StatusPalette.CardBackground, StatusPalette.Accent, 0.12f);
            TransportIcon.TextColor = StatusPalette.Accent;
            HeroContent.Opacity = _ViewModel.IsLoading ? 0 : 1;

            PaintStatus();
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
                bool isAloneOnLastRow = (i == visible.Count - 1) && (i % _TileColumns == 0);

                Grid.SetColumn(visible[i], i % _TileColumns);
                Grid.SetRow(visible[i], i / _TileColumns);

                // A tile alone on the last row spans it, which also gives long values room.
                Grid.SetColumnSpan(visible[i], isAloneOnLastRow ? _TileColumns : 1);
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
