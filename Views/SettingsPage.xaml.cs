using PingMyNetwork.ViewModels;

namespace PingMyNetwork.Views
{
    /// <summary>
    /// Settings, pushed on top of Home.
    /// </summary>
    public partial class SettingsPage : ContentPage
    {
        #region Constructor

        /// <summary>
        /// Creates the page.
        /// </summary>
        /// <param name="viewModel">The settings view model.</param>
        public SettingsPage(SettingsViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = viewModel;

            AppShell.ApplyTransition(this);
        }

        #endregion
    }
}
