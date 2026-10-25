using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.ViewModels;

namespace PingMyNetwork.Views
{
    /// <summary>
    /// What the app asks for and why, pushed on top of Home on first launch and from Settings.
    /// </summary>
    public partial class PermissionsPage : ContentPage
    {
        #region Variables

        private readonly PermissionsViewModel _ViewModel;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the page.
        /// </summary>
        /// <param name="viewModel">The permissions view model.</param>
        public PermissionsPage(PermissionsViewModel viewModel)
        {
            InitializeComponent();

            _ViewModel = viewModel;
            BindingContext = viewModel;

            AppShell.ApplyTransition(this);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Checks every item, and again whenever the user comes back from system settings.
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();

            _ = _ViewModel.RefreshAsync();

            if (Window is not null)
            {
                Window.Resumed += OnResumed;
            }
        }

        /// <summary>
        /// Stops listening once the page is gone. Leaving counts as having seen the screen, so it
        /// is not shown again on the next launch.
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            AppSettings.IsOnboarded = true;

            if (Window is not null)
            {
                Window.Resumed -= OnResumed;
            }
        }

        /// <summary>
        /// Checks again after a trip to system settings.
        /// </summary>
        /// <param name="sender">The window.</param>
        /// <param name="e">The event data.</param>
        private void OnResumed(object? sender, EventArgs e)
        {
            _ = _ViewModel.RefreshAsync();
        }

        #endregion
    }
}
