using PingMyNetwork.Helper.Enums;
using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.ViewModels;

namespace PingMyNetwork.Views
{
    /// <summary>
    /// One device, pushed on top of Devices on narrow windows. Wide windows show the same view in a
    /// pane beside the list instead.
    /// </summary>
    public partial class DeviceDetailsPage : ContentPage, IQueryAttributable
    {
        #region Variables

        private readonly DeviceDetailsViewModel _ViewModel;

        private bool _IsClosing;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the page.
        /// </summary>
        /// <param name="viewModel">The details view model.</param>
        public DeviceDetailsPage(DeviceDetailsViewModel viewModel)
        {
            InitializeComponent();

            _ViewModel = viewModel;
            BindingContext = viewModel;

            AppShell.ApplyTransition(this);

            _ViewModel.Forgotten += (_, _) => Close();
            SizeChanged += OnSizeChanged;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Shows the device named in the route.
        /// </summary>
        /// <param name="query">Route parameters; "id" is the device's id.</param>
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            _ViewModel.Show(query.TryGetValue("id", out object? id) ? id as string : null);
        }

        /// <summary>
        /// Follows live changes while showing.
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();

            _ViewModel.Attach();
        }

        /// <summary>
        /// Stops following changes once gone.
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            _ViewModel.Detach();
        }

        /// <summary>
        /// Hands over to the list's pane when the window becomes wide enough for one, so the same
        /// device stays open: no state is lost on resize.
        /// </summary>
        /// <param name="sender">The page.</param>
        /// <param name="e">Unused.</param>
        private void OnSizeChanged(object? sender, EventArgs e)
        {
            if (LayoutWidthUtility.From(Window?.Width ?? Width) != LayoutWidth.Compact)
            {
                Close();
            }
        }

        /// <summary>
        /// Goes back to the list, once.
        /// </summary>
        private void Close()
        {
            if (_IsClosing)
            {
                return;
            }

            _IsClosing = true;
            Dispatcher.Dispatch(async () => await Shell.Current.GoToAsync(".."));
        }

        #endregion
    }
}
