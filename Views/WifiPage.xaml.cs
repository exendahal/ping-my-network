using System.ComponentModel;
using MauiWifiManager.Abstractions;
using PingMyNetwork.Controls;
using PingMyNetwork.Helper.Enums;
using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.ViewModels;

namespace PingMyNetwork.Views
{
    /// <summary>
    /// The Wi-Fi analyzer. Code-behind holds view concerns only: drawing the charts, the chart's
    /// entrance, and which sections show at the window's width.
    /// </summary>
    public partial class WifiPage : ContentPage
    {
        #region Variables

        private const string CHART_ANIMATION = "ChartGrow";

        /// <summary>
        /// The chart's entrance: curves grow from the axis (PRD 9.7).
        /// </summary>
        private const uint CHART_GROW_MS = 400;

        /// <summary>
        /// Space under the content on narrow windows, so the last item clears the floating tab bar.
        /// </summary>
        private const double TAB_BAR_CLEARANCE = 112;

        private readonly WifiViewModel _ViewModel;
        private readonly ChannelChartDrawable _Chart = new();
        private readonly SignalHistoryDrawable _History = new();

        private bool? _IsWide;

        private bool _IsShowing;
        private WifiFrequencyBand? _GrownBand;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the page.
        /// </summary>
        /// <param name="viewModel">The Wi-Fi view model.</param>
        public WifiPage(WifiViewModel viewModel)
        {
            InitializeComponent();

            _ViewModel = viewModel;
            BindingContext = viewModel;

            ChannelChart.Drawable = _Chart;
            WalkChart.Drawable = _History;
            InsightsSkeletonFirst.Blocks = SkeletonView.INSIGHT;
            InsightsSkeletonSecond.Blocks = SkeletonView.INSIGHT;

            AppShell.ApplyTransition(this);

            SizeChanged += (_, _) => ApplyLayout();

            if (Application.Current is not null)
            {
                Application.Current.RequestedThemeChanged += (_, _) => PaintCharts();
            }

            PaintCharts();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Starts scanning while the analyzer is showing.
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Subscribed per appearance, not in the constructor: the view model outlives the page,
            // and a second page instance must not leave a stale handler behind.
            _IsShowing = true;
            _ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _ViewModel.PropertyChanged += OnViewModelPropertyChanged;

            // The chart's entrance and the scan wait for the slide, so neither costs it frames.
            await AppShell.WhenSettledAsync();

            if (_IsShowing)
            {
                UpdateChart();
                await _ViewModel.AppearAsync();
            }
        }

        /// <summary>
        /// Stops scanning and the walk once the analyzer is left.
        /// </summary>
        protected override async void OnDisappearing()
        {
            base.OnDisappearing();

            _IsShowing = false;
            this.AbortAnimation(CHART_ANIMATION);
            _ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            await _ViewModel.DisappearAsync();
        }

        /// <summary>
        /// Redraws what a changed value affects.
        /// </summary>
        /// <param name="sender">The view model.</param>
        /// <param name="e">Which property changed.</param>
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(WifiViewModel.Curves):
                case nameof(WifiViewModel.SuggestedChannel):
                case nameof(WifiViewModel.ChartBand):
                    UpdateChart();
                    break;

                case nameof(WifiViewModel.WalkSamples):
                case nameof(WifiViewModel.WalkLevel):
                    _History.Samples = _ViewModel.WalkSamples;
                    _History.LineColor = StatusPalette.For(_ViewModel.WalkLevel);
                    WalkChart.Invalidate();
                    break;

                case nameof(WifiViewModel.Section):
                    ApplySections();
                    break;
            }
        }

        /// <summary>
        /// Hands the chart its data, and grows the curves in the first time a band is shown.
        /// </summary>
        private void UpdateChart()
        {
            _Chart.Band = _ViewModel.ChartBand;
            _Chart.Curves = _ViewModel.Curves;
            _Chart.SuggestedChannel = _ViewModel.SuggestedChannel;

            bool isNewBand = (_GrownBand != _ViewModel.ChartBand) && (_ViewModel.Curves.Count > 0);

            if (!isNewBand || MotionSettings.PrefersReducedMotion)
            {
                if (isNewBand)
                {
                    _GrownBand = _ViewModel.ChartBand;
                }

                ChannelChart.Invalidate();
                return;
            }

            // Only a band's first appearance animates; live updates just redraw.
            _GrownBand = _ViewModel.ChartBand;
            this.AbortAnimation(CHART_ANIMATION);

            new Animation(
                progress =>
                {
                    _Chart.Progress = (float)progress;
                    ChannelChart.Invalidate();
                },
                0,
                1,
                Easing.CubicOut)
                .Commit(this, CHART_ANIMATION, rate: 16, length: CHART_GROW_MS, finished: (_, _) =>
                {
                    _Chart.Progress = 1f;
                    ChannelChart.Invalidate();
                });
        }

        /// <summary>
        /// Applies theme colours to both charts.
        /// </summary>
        private void PaintCharts()
        {
            _Chart.OwnColor = StatusPalette.Accent;
            _Chart.OtherColor = StatusPalette.Neutral;
            _Chart.AxisColor = StatusPalette.Neutral;
            _History.AxisColor = StatusPalette.Neutral;
            _History.LineColor = StatusPalette.For(_ViewModel.WalkLevel);

            ChannelChart.Invalidate();
            WalkChart.Invalidate();
        }

        /// <summary>
        /// Lays the page out for the window's width: one section at a time on narrow windows, all
        /// four as a two-column dashboard on wide ones.
        /// </summary>
        private void ApplyLayout()
        {
            double windowWidth = Window?.Width ?? Width;
            bool isWide = LayoutWidthUtility.From(windowWidth) >= LayoutWidth.Expanded;
            Thickness padding = LayoutWidthUtility.PagePadding(windowWidth);

            // The lists scroll to the bottom edge; their last item leaves room for the tab bar.
            PageGrid.Padding = new Thickness(padding.Left, padding.Top, padding.Right, 0);
            double clearance = LayoutWidthUtility.From(windowWidth) == LayoutWidth.Compact ? TAB_BAR_CLEARANCE : padding.Bottom;
            LeftStack.Padding = new Thickness(0, 0, 0, clearance);
            ListClearance.HeightRequest = clearance;

            if (isWide == _IsWide)
            {
                return;
            }

            _IsWide = isWide;

            Body.ColumnDefinitions = isWide
                ? [new ColumnDefinition(new GridLength(3, GridUnitType.Star)), new ColumnDefinition(new GridLength(2, GridUnitType.Star))]
                : [new ColumnDefinition(GridLength.Star)];
            Grid.SetColumn(RightGrid, isWide ? 1 : 0);

            ApplySections();
        }

        /// <summary>
        /// Shows every section on wide windows, or the chosen one on narrow ones, and marks its chip.
        /// </summary>
        private void ApplySections()
        {
            bool isWide = _IsWide == true;
            WifiSection section = _ViewModel.Section;

            SectionBar.IsVisible = !isWide && _ViewModel.IsSupported;

            InsightsView.IsVisible = isWide || (section == WifiSection.Insights);
            WalkView.IsVisible = isWide || (section == WifiSection.SignalWalk);
            ChannelsView.IsVisible = isWide || (section == WifiSection.Channels);
            NetworksView.IsVisible = isWide || (section == WifiSection.Networks);

            LeftScroll.IsVisible = InsightsView.IsVisible || WalkView.IsVisible || ChannelsView.IsVisible;
            RightGrid.IsVisible = NetworksView.IsVisible;

            // Narrow: the chip already names the section, so the heading would repeat it.
            InsightsHeading.IsVisible = isWide;
            WalkHeading.IsVisible = isWide;
            ChannelsHeading.IsVisible = isWide;
            NetworksHeading.IsVisible = isWide;

            Style? chip = ResourceLookup.Get<Style?>("Chip", null);
            Style? selected = ResourceLookup.Get<Style?>("ChipSelected", null);

            foreach (IView child in SectionChips.Children)
            {
                if (child is Button button)
                {
                    button.Style = button.ClassId == section.ToString() ? selected : chip;
                }
            }
        }

        /// <summary>
        /// Saves the spot when Enter is pressed in the place name.
        /// </summary>
        /// <param name="sender">The place name field.</param>
        /// <param name="e">Unused.</param>
        private void OnPlaceCompleted(object? sender, EventArgs e)
        {
            _ViewModel.SaveSpotCommand.Execute(null);
        }

        #endregion
    }
}
