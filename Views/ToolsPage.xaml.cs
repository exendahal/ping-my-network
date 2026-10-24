using PingMyNetwork.Helper.Enums;
using PingMyNetwork.Helper.Utilities;

namespace PingMyNetwork.Views
{
    /// <summary>
    /// The Tools destination: every tool as a card, grouped, opening its own screen.
    /// </summary>
    public partial class ToolsPage : ContentPage
    {
        #region Variables

        /// <summary>
        /// Space under the content on narrow windows, so the last card clears the floating tab bar.
        /// </summary>
        private const double TAB_BAR_CLEARANCE = 112;

        private LayoutWidth? _AppliedWidth;

        private bool _IsOpening;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the page.
        /// </summary>
        public ToolsPage()
        {
            InitializeComponent();

            AppShell.ApplyTransition(this);

            SizeChanged += (_, _) => ApplyLayout();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Opens the tapped tool's screen.
        /// </summary>
        /// <param name="sender">The tool's card, whose ClassId is its route.</param>
        /// <param name="e">The event data.</param>
        private async void OnToolTapped(object? sender, TappedEventArgs e)
        {
            // Taps arriving while a tool is still opening would each push another copy.
            if (_IsOpening || (sender is not Element { ClassId: string route }))
            {
                return;
            }

            _IsOpening = true;

            try
            {
                await Shell.Current.GoToAsync(route);
            }
            finally
            {
                _IsOpening = false;
            }
        }

        /// <summary>
        /// Stacks the two groups on narrow windows and sets them side by side on wide ones.
        /// </summary>
        private void ApplyLayout()
        {
            double windowWidth = Window?.Width ?? Width;
            LayoutWidth width = LayoutWidthUtility.From(windowWidth);
            Thickness padding = LayoutWidthUtility.PagePadding(windowWidth);

            PageGrid.Padding = width == LayoutWidth.Compact
                ? new Thickness(padding.Left, padding.Top, padding.Right, TAB_BAR_CLEARANCE)
                : padding;

            // The scroll view measures with unlimited width on Windows; a definite width lets the
            // descriptions wrap inside their rows.
            PageGrid.WidthRequest = Math.Min(Width, PageGrid.MaximumWidthRequest);

            if (width == _AppliedWidth)
            {
                return;
            }

            _AppliedWidth = width;

            bool isWide = width >= LayoutWidth.Expanded;

            PageGrid.ColumnDefinitions = isWide
                ? [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)]
                : [new ColumnDefinition(GridLength.Star)];

            Grid.SetColumnSpan(Header, isWide ? 2 : 1);
            Grid.SetRow(DiagnoseGroup, 1);
            Grid.SetRow(UtilityGroup, isWide ? 1 : 2);
            Grid.SetColumn(UtilityGroup, isWide ? 1 : 0);
        }

        #endregion
    }
}
