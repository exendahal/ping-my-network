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
            Toolbar.SizeChanged += (_, _) => FitListToToolbar();

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

            // The list shows what it last knew straight away; loading and scanning wait for the slide.
            await AppShell.WhenSettledAsync();

            if (_IsShowing)
            {
                // async void: a scan that throws must not take the app down with it.
                try
                {
                    await _ViewModel.AppearAsync();
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Debug.WriteLine($"Devices page failed to start: {exception}");
                }

#if DEBUG
                // Lets store screenshots open a device: set PMN_SELECT=Front Door Camera.
                if (Environment.GetEnvironmentVariable("PMN_SELECT") is { Length: > 0 } name)
                {
                    _ViewModel.Devices.FirstOrDefault(device => device.Name == name)?.OpenCommand.Execute(null);
                }
#endif
            }
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

            // A shorter list may not scroll at all, which would leave no way to bring it back.
            if (e.PropertyName is nameof(DevicesViewModel.Filter) or nameof(DevicesViewModel.HasRows))
            {
                ShowToolbar();
            }
        }

        /// <summary>
        /// Moves the toolbar with the list, finger for finger: up and away while scrolling down,
        /// back down on the way up, and always fully shown at the top of the list.
        /// </summary>
        /// <remarks>
        /// ponytail: it can stop half shown when a scroll ends there, as Chrome's address bar can;
        /// snap to the nearer end once scrolling settles if that bothers anyone.
        /// </remarks>
        /// <param name="sender">The list.</param>
        /// <param name="e">The scroll position and step.</param>
        private void OnListScrolled(object? sender, ItemsViewScrolledEventArgs e)
        {
            // Height is -1 until the toolbar is first measured, and the list scrolls during its own
            // first layout, before that.
            if (_IsWide || SearchEntry.IsFocused || (Toolbar.Height <= 0))
            {
                return;
            }

            double shift = Math.Clamp(Toolbar.TranslationY - e.VerticalDelta, -Toolbar.Height, 0);

            // Never further up than the list has scrolled: at the top, the toolbar is all there.
            Toolbar.TranslationY = Math.Max(shift, -Math.Max(e.VerticalOffset, 0));
        }

        /// <summary>
        /// Brings the toolbar fully back, for when the list may no longer scroll far enough to.
        /// </summary>
        private void ShowToolbar()
        {
            Toolbar.TranslationY = 0;
        }

        /// <summary>
        /// Leaves room at the top of the list and its stand-in states for the floating toolbar.
        /// </summary>
        private void FitListToToolbar()
        {
            double inset = Toolbar.Height;

            ListInset.HeightRequest = inset;
            SkeletonRows.Margin = new Thickness(0, inset, 0, 0);
            EmptyState.Margin = new Thickness(0, inset, 0, 0);
            NoMatchState.Margin = new Thickness(0, inset, 0, 0);
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
            ShowToolbar();

            Split.ColumnDefinitions = isWide
                ? [new ColumnDefinition(new GridLength(5, GridUnitType.Star)), new ColumnDefinition(new GridLength(6, GridUnitType.Star))]
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
