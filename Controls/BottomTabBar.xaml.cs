namespace PingMyNetwork.Controls
{
    /// <summary>
    /// The four-destination tab bar along the bottom of narrow windows.
    /// </summary>
    /// <remarks>
    /// Declared once in the shell rather than on each page, so switching tabs never rebuilds it.
    /// This is sill_app's fix for tab-switch flicker.
    /// </remarks>
    public partial class BottomTabBar : ContentView
    {
        #region Variables

        /// <summary>
        /// Route of the tab that is currently showing.
        /// </summary>
        public static readonly BindableProperty ActiveRouteProperty = BindableProperty.Create(
            nameof(ActiveRoute),
            typeof(string),
            typeof(BottomTabBar),
            string.Empty,
            propertyChanged: OnVisualChanged);

        /// <summary>
        /// Colour of the active tab.
        /// </summary>
        public static readonly BindableProperty AccentColorProperty = BindableProperty.Create(
            nameof(AccentColor),
            typeof(Color),
            typeof(BottomTabBar),
            Colors.Green,
            propertyChanged: OnVisualChanged);

        /// <summary>
        /// Colour of the inactive tabs.
        /// </summary>
        public static readonly BindableProperty InactiveColorProperty = BindableProperty.Create(
            nameof(InactiveColor),
            typeof(Color),
            typeof(BottomTabBar),
            Colors.Gray,
            propertyChanged: OnVisualChanged);

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the tab bar.
        /// </summary>
        public BottomTabBar()
        {
            InitializeComponent();

            Apply();
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised with the route of a tab the user picked, other than the one already showing.
        /// </summary>
        public event EventHandler<string>? RouteRequested;

        #endregion

        #region Properties

        /// <summary>
        /// Route of the tab that is currently showing.
        /// </summary>
        public string ActiveRoute
        {
            get => (string)GetValue(ActiveRouteProperty);
            set => SetValue(ActiveRouteProperty, value);
        }

        /// <summary>
        /// Colour of the active tab.
        /// </summary>
        public Color AccentColor
        {
            get => (Color)GetValue(AccentColorProperty);
            set => SetValue(AccentColorProperty, value);
        }

        /// <summary>
        /// Colour of the inactive tabs.
        /// </summary>
        public Color InactiveColor
        {
            get => (Color)GetValue(InactiveColorProperty);
            set => SetValue(InactiveColorProperty, value);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Repaints when the active route or either colour changes.
        /// </summary>
        /// <param name="bindable">The tab bar.</param>
        /// <param name="oldValue">Unused.</param>
        /// <param name="newValue">Unused.</param>
        private static void OnVisualChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((BottomTabBar)bindable).Apply();
        }

        /// <summary>
        /// Colours each tab according to whether it is the active route.
        /// </summary>
        private void Apply()
        {
            foreach (IView child in Items.Children)
            {
                if (child is not Layout item)
                {
                    continue;
                }

                bool isActive = item.ClassId == ActiveRoute;
                Color colour = isActive ? AccentColor : InactiveColor;
                Label icon = (Label)item.Children[0];
                Label caption = (Label)item.Children[1];

                icon.TextColor = colour;
                caption.TextColor = colour;
                caption.FontAttributes = isActive ? FontAttributes.Bold : FontAttributes.None;
            }
        }

        /// <summary>
        /// Asks the shell to switch to the tapped tab.
        /// </summary>
        /// <param name="sender">The tapped tab.</param>
        /// <param name="e">Unused.</param>
        private void OnItemTapped(object? sender, TappedEventArgs e)
        {
            if ((sender is VisualElement item) && (item.ClassId != ActiveRoute))
            {
                RouteRequested?.Invoke(this, item.ClassId);
            }
        }

        #endregion
    }
}
