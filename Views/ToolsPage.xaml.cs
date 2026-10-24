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
        /// One card per row on phones, two on medium windows, three on wide ones.
        /// </summary>
        private void ApplyLayout()
        {
            double windowWidth = Window?.Width ?? Width;
            LayoutWidth width = LayoutWidthUtility.From(windowWidth);
            Thickness padding = LayoutWidthUtility.PagePadding(windowWidth);

            PageStack.Padding = width == LayoutWidth.Compact
                ? new Thickness(padding.Left, padding.Top, padding.Right, TAB_BAR_CLEARANCE)
                : padding;

            // The scroll view measures with unlimited width on Windows; a definite width lets the
            // descriptions wrap inside their cards.
            PageStack.WidthRequest = Math.Min(Width, PageStack.MaximumWidthRequest);

            if (width == _AppliedWidth)
            {
                return;
            }

            _AppliedWidth = width;

            int columns = width switch
            {
                LayoutWidth.Compact => 1,
                LayoutWidth.Medium => 2,
                _ => 3,
            };

            Flow(DiagnoseGrid, columns);
            Flow(UtilityGrid, columns);
        }

        /// <summary>
        /// Places a grid's cards left to right, top to bottom, in equal columns.
        /// </summary>
        /// <param name="grid">The grid.</param>
        /// <param name="columns">Cards per row.</param>
        private static void Flow(Grid grid, int columns)
        {
            int rows = (grid.Children.Count + columns - 1) / columns;

            grid.ColumnDefinitions = [.. Enumerable.Range(0, columns).Select(_ => new ColumnDefinition(GridLength.Star))];
            grid.RowDefinitions = [.. Enumerable.Range(0, rows).Select(_ => new RowDefinition(GridLength.Auto))];

            for (var i = 0; i < grid.Children.Count; i++)
            {
                BindableObject card = (BindableObject)grid.Children[i];

                Grid.SetColumn(card, i % columns);
                Grid.SetRow(card, i / columns);
            }
        }

        #endregion
    }
}
