using PingMyNetwork.Helper.Utilities;

namespace PingMyNetwork.Controls
{
    /// <summary>
    /// The vertical navigation rail used by wide windows, in place of the phone tab bar.
    /// </summary>
    public partial class SideRail : ContentView
    {
        #region Variables

        /// <summary>
        /// How much of the accent colour the active pill's background carries.
        /// </summary>
        private const float PILL_TINT = 0.12f;

        /// <summary>
        /// Route of the destination currently showing.
        /// </summary>
        public static readonly BindableProperty ActiveRouteProperty = BindableProperty.Create(
            nameof(ActiveRoute),
            typeof(string),
            typeof(SideRail),
            string.Empty,
            propertyChanged: OnVisualChanged);

        /// <summary>
        /// Colour of the active destination.
        /// </summary>
        public static readonly BindableProperty AccentColorProperty = BindableProperty.Create(
            nameof(AccentColor),
            typeof(Color),
            typeof(SideRail),
            Colors.Green,
            propertyChanged: OnVisualChanged);

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the rail.
        /// </summary>
        public SideRail()
        {
            InitializeComponent();

            Apply();
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised with the route of a destination the user picked, other than the one showing.
        /// </summary>
        public event EventHandler<string>? RouteRequested;

        #endregion

        #region Properties

        /// <summary>
        /// Route of the destination currently showing.
        /// </summary>
        public string ActiveRoute
        {
            get => (string)GetValue(ActiveRouteProperty);
            set => SetValue(ActiveRouteProperty, value);
        }

        /// <summary>
        /// Colour of the active destination.
        /// </summary>
        public Color AccentColor
        {
            get => (Color)GetValue(AccentColorProperty);
            set => SetValue(AccentColorProperty, value);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Repaints when the active route or the accent changes.
        /// </summary>
        /// <param name="bindable">The rail.</param>
        /// <param name="oldValue">Unused.</param>
        /// <param name="newValue">Unused.</param>
        private static void OnVisualChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((SideRail)bindable).Apply();
        }

        /// <summary>
        /// Gives the active destination a tinted pill and accent text, and the rest neutral text.
        /// </summary>
        private void Apply()
        {
            foreach (IView child in Items.Children)
            {
                if ((child is not Border pill) || (pill.Content is not Layout content))
                {
                    continue;
                }

                bool isActive = pill.ClassId == ActiveRoute;
                Color colour = isActive ? AccentColor : StatusPalette.Neutral;
                Label icon = (Label)content.Children[0];
                Label caption = (Label)content.Children[1];

                pill.BackgroundColor = isActive
                    ? StatusPalette.Lerp(StatusPalette.PageBackground, AccentColor, PILL_TINT)
                    : Colors.Transparent;

                icon.TextColor = colour;
                caption.TextColor = colour;
                caption.FontAttributes = isActive ? FontAttributes.Bold : FontAttributes.None;
            }
        }

        /// <summary>
        /// Asks the shell to switch to the tapped destination.
        /// </summary>
        /// <param name="sender">The tapped pill.</param>
        /// <param name="e">Unused.</param>
        private void OnItemTapped(object? sender, TappedEventArgs e)
        {
            if ((sender is VisualElement pill) && (pill.ClassId != ActiveRoute))
            {
                RouteRequested?.Invoke(this, pill.ClassId);
            }
        }

        #endregion
    }
}
