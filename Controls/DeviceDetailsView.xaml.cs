using System.ComponentModel;
using PingMyNetwork.Helper.Enums;
using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.ViewModels;

namespace PingMyNetwork.Controls
{
    /// <summary>
    /// One device in full. Code-behind only paints the status colours and commits the name edit.
    /// </summary>
    public partial class DeviceDetailsView : ContentView
    {
        #region Variables

        private DeviceDetailsViewModel? _ViewModel;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the view.
        /// </summary>
        public DeviceDetailsView()
        {
            InitializeComponent();

            if (Application.Current is not null)
            {
                Application.Current.RequestedThemeChanged += (_, _) => Paint();
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Shows the "pick a device" message when nothing is selected, which only the wide pane
        /// needs; the pushed page always has a device.
        /// </summary>
        public bool ShowEmptyState { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Follows the view model the view is given.
        /// </summary>
        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            if (_ViewModel is not null)
            {
                _ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            _ViewModel = BindingContext as DeviceDetailsViewModel;

            if (_ViewModel is not null)
            {
                _ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            }

            Paint();
        }

        /// <summary>
        /// Repaints when the Known status or the selection changes.
        /// </summary>
        /// <param name="sender">The view model.</param>
        /// <param name="e">Which property changed.</param>
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(DeviceDetailsViewModel.IsKnown) or nameof(DeviceDetailsViewModel.HasDevice))
            {
                Paint();
            }
        }

        /// <summary>
        /// Colours the Known line and makes "Mark as known" the primary action for an unknown device.
        /// Never colour alone: the icon and the sentence change too.
        /// </summary>
        private void Paint()
        {
            bool isKnown = _ViewModel?.IsKnown == true;

            EmptyState.IsVisible = ShowEmptyState && (_ViewModel?.HasDevice != true);

            KnownIcon.Text = isKnown ? MaterialIcon.VERIFIED : MaterialIcon.HELP;
            KnownIcon.TextColor = StatusPalette.For(isKnown ? StatusLevel.Good : StatusLevel.Notice);
            ToggleKnownButton.Style = ResourceLookup.Get<Style?>(isKnown ? "QuietButton" : "PrimaryButton", null);
            ForgetButton.TextColor = StatusPalette.For(StatusLevel.Bad);
        }

        /// <summary>
        /// Saves the name when the user presses Enter or leaves the field.
        /// </summary>
        /// <param name="sender">The name field.</param>
        /// <param name="e">Unused.</param>
        private void OnNameCommitted(object? sender, EventArgs e)
        {
            _ViewModel?.SaveNameCommand.Execute(null);
        }

        #endregion
    }
}
