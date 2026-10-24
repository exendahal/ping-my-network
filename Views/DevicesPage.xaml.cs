using System.ComponentModel;
using PingMyNetwork.Controls;
using PingMyNetwork.Helper.Enums;
using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.ViewModels;

namespace PingMyNetwork.Views
{
    /// <summary>
    /// The Devices screen. Code-behind holds view concerns only: the list and pane layout for the
    /// window's width, the chip highlight, and where an opened device is shown.
    /// </summary>
    public partial class DevicesPage : ContentPage
    {
        #region Variables

        /// <summary>
        /// Placeholder rows shown while the first results arrive.
        /// </summary>
        private const int SKELETON_ROWS = 6;

        /// <summary>
        /// Height of a device row, shared by the real rows and their placeholders.
        /// </summary>
        private const double ROW_HEIGHT = 68;

        /// <summary>
        /// Space under the list on narrow windows, so the last row clears the floating tab bar.
        /// </summary>
        private const double TAB_BAR_CLEARANCE = 112;

        private readonly DevicesViewModel _ViewModel;
        private readonly DeviceDetailsViewModel _PaneViewModel;

        private bool _IsWide;
        private bool _IsShowing;
        private bool _IsOpening;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the page.
        /// </summary>
        /// <param name="viewModel">The Devices view model.</param>
        /// <param name="paneViewModel">Drives the details pane on wide windows.</param>
        public DevicesPage(DevicesViewModel viewModel, DeviceDetailsViewModel paneViewModel)
        {
            InitializeComponent();

            _ViewModel = viewModel;
            _PaneViewModel = paneViewModel;
            BindingContext = viewModel;
            PaneDetails.BindingContext = paneViewModel;

            for (var i = 0; i < SKELETON_ROWS; i++)
            {
                SkeletonRows.Children.Add(new SkeletonView { Blocks = SkeletonView.DEVICE_ROW, HeightRequest = ROW_HEIGHT });
            }

            AppShell.ApplyTransition(this);

            _PaneViewModel.Forgotten += (_, _) => _ViewModel.SelectedDevice = null;
            SizeChanged += (_, _) => ApplyLayout();

            PaintChips();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Shows remembered devices and starts the session's first scan.
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            _IsShowing = true;

            // Subscribed per appearance, not in the constructor: the view model outlives the page,
            // and a second page instance must not leave a handler that opens the device again.
            _ViewModel.DeviceOpened -= OnDeviceOpened;
            _ViewModel.DeviceOpened += OnDeviceOpened;
            _ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            PaintChips();

            if (_IsWide)
            {
                _PaneViewModel.Attach();
            }

            await _ViewModel.AppearAsync();
        }

        /// <summary>
        /// Stops the pane following changes while the page is hidden.
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            _IsShowing = false;
            _ViewModel.DeviceOpened -= OnDeviceOpened;
            _ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _PaneViewModel.Detach();
        }

        /// <summary>
        /// Shows an opened device in the pane, or pushes its page on narrow windows.
        /// </summary>
        /// <param name="sender">The view model.</param>
        /// <param name="item">The opened row.</param>
        private async void OnDeviceOpened(object? sender, DeviceItem item)
        {
            if (_IsWide)
            {
                _PaneViewModel.Show(item.Id);
                return;
            }

            // Taps arriving while the details page is still opening would each push another copy.
            if (_IsOpening)
            {
                return;
            }

            _IsOpening = true;

            try
            {
                await Shell.Current.GoToAsync($"{Routes.DEVICE_DETAILS}?id={item.Id}");
            }
            finally
            {
                _IsOpening = false;
            }
        }

        /// <summary>
        /// Moves the chip highlight when the filter changes.
        /// </summary>
        /// <param name="sender">The view model.</param>
        /// <param name="e">Which property changed.</param>
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DevicesViewModel.Filter))
            {
                PaintChips();
            }
        }

        /// <summary>
        /// Fills the chip of the active filter.
        /// </summary>
        private void PaintChips()
        {
            Style? chip = ResourceLookup.Get<Style?>("Chip", null);
            Style? selected = ResourceLookup.Get<Style?>("ChipSelected", null);

            foreach (IView child in Chips.Children)
            {
                if ((child is Button button) && !string.IsNullOrEmpty(button.ClassId))
                {
                    button.Style = button.ClassId == _ViewModel.Filter.ToString() ? selected : chip;
                }
            }
        }

        /// <summary>
        /// Lays the page out for the window's width: the list alone on narrow windows, the list and
        /// the details pane side by side on wider ones. An open device carries over both ways.
        /// </summary>
        private void ApplyLayout()
        {
            double windowWidth = Window?.Width ?? Width;
            bool isWide = LayoutWidthUtility.From(windowWidth) != LayoutWidth.Compact;
            Thickness padding = LayoutWidthUtility.PagePadding(windowWidth);

            // The list scrolls to the bottom edge; its footer leaves room for the tab bar instead.
            ListColumn.Padding = new Thickness(padding.Left, padding.Top, padding.Right, 0);
            // The last row already carries an 8-unit bottom margin.
            LimitsNote.Margin = new Thickness(4, 8, 4, isWide ? padding.Bottom : TAB_BAR_CLEARANCE);

            if (isWide == _IsWide)
            {
                return;
            }

            _IsWide = isWide;
            _ViewModel.IsSelectionShown = isWide;

            Split.ColumnDefinitions = isWide
                ? [new ColumnDefinition(new GridLength(2, GridUnitType.Star)), new ColumnDefinition(new GridLength(3, GridUnitType.Star))]
                : [new ColumnDefinition(GridLength.Star)];
            Pane.IsVisible = isWide;

            if (isWide)
            {
                if (_IsShowing)
                {
                    _PaneViewModel.Attach();
                }

                _PaneViewModel.Show(_ViewModel.SelectedDevice?.Id);
                return;
            }

            _PaneViewModel.Detach();

            // A device open in the pane stays open, now as its own page.
            if (_IsShowing && (_ViewModel.SelectedDevice is DeviceItem open))
            {
                Dispatcher.Dispatch(async () => await Shell.Current.GoToAsync($"{Routes.DEVICE_DETAILS}?id={open.Id}"));
            }
        }

        #endregion
    }
}
