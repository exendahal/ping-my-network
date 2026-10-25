using PingMyNetwork.ViewModels;

namespace PingMyNetwork.Views
{
    /// <summary>
    /// Settings, pushed on top of Home.
    /// </summary>
    public partial class SettingsPage : ContentPage
    {
        #region Variables

        private readonly SettingsViewModel _ViewModel;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the page.
        /// </summary>
        /// <param name="viewModel">The settings view model.</param>
        public SettingsPage(SettingsViewModel viewModel)
        {
            InitializeComponent();

            _ViewModel = viewModel;
            BindingContext = viewModel;

            AppShell.ApplyTransition(this);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Opens the permissions screen. The row raises an event rather than taking a command.
        /// </summary>
        /// <param name="sender">The row.</param>
        /// <param name="e">The event data.</param>
        private void OnPermissionsTapped(object? sender, EventArgs e)
        {
            _ViewModel.OpenPermissionsCommand.Execute(null);
        }

        /// <summary>
        /// Opens a bug report.
        /// </summary>
        /// <param name="sender">The row.</param>
        /// <param name="e">The event data.</param>
        private void OnReportBugTapped(object? sender, EventArgs e)
        {
            _ViewModel.ReportBugCommand.Execute(null);
        }

        /// <summary>
        /// Opens a feature request.
        /// </summary>
        /// <param name="sender">The row.</param>
        /// <param name="e">The event data.</param>
        private void OnRequestFeatureTapped(object? sender, EventArgs e)
        {
            _ViewModel.RequestFeatureCommand.Execute(null);
        }

        #endregion
    }
}
