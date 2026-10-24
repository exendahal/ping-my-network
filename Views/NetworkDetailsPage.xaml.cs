using PingMyNetwork.ViewModels;

namespace PingMyNetwork.Views
{
    /// <summary>
    /// Every value about the current network, pushed on top of Home.
    /// </summary>
    public partial class NetworkDetailsPage : ContentPage
    {
        #region Variables

        private readonly NetworkDetailsViewModel _ViewModel;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the page.
        /// </summary>
        /// <param name="viewModel">The details view model.</param>
        public NetworkDetailsPage(NetworkDetailsViewModel viewModel)
        {
            InitializeComponent();

            _ViewModel = viewModel;
            BindingContext = viewModel;

            AppShell.ApplyTransition(this);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Follows live changes while the page is showing.
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();

            _ViewModel.Attach();
        }

        /// <summary>
        /// Stops following changes once the page is gone.
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            _ViewModel.Detach();
        }

        #endregion
    }
}
