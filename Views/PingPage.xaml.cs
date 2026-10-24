using System.ComponentModel;
using PingMyNetwork.Controls;
using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.ViewModels;

namespace PingMyNetwork.Views
{
    /// <summary>
    /// Ping: live round trips as a chart, with average, jitter, range and loss.
    /// </summary>
    public partial class PingPage : ToolPage
    {
        #region Variables

        private readonly PingViewModel _ViewModel;

        private readonly LatencyChartDrawable _Chart = new();

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the page.
        /// </summary>
        /// <param name="viewModel">The tool's view model.</param>
        public PingPage(PingViewModel viewModel)
            : base(viewModel)
        {
            InitializeComponent();

            _ViewModel = viewModel;
            Chart.Drawable = _Chart;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Follows new pings while showing.
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();

            _ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            Redraw();
        }

        /// <summary>
        /// Stops following once the page is gone.
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            _ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        /// <summary>
        /// Redraws the chart when a ping lands.
        /// </summary>
        /// <param name="sender">The view model.</param>
        /// <param name="e">Which property changed.</param>
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PingViewModel.Samples))
            {
                Redraw();
            }
        }

        /// <summary>
        /// Hands the chart the samples and the theme's axis colour.
        /// </summary>
        private void Redraw()
        {
            _Chart.Samples = _ViewModel.Samples;
            _Chart.AxisColor = StatusPalette.Neutral;
            Chart.Invalidate();
        }

        #endregion
    }
}
