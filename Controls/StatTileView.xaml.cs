namespace PingMyNetwork.Controls
{
    /// <summary>
    /// One figure with its caption and an optional icon, e.g. "Router latency · 2 ms".
    /// </summary>
    public partial class StatTileView : ContentView
    {
        #region Variables

        /// <summary>
        /// What the figure measures.
        /// </summary>
        public static readonly BindableProperty CaptionProperty = BindableProperty.Create(
            nameof(Caption),
            typeof(string),
            typeof(StatTileView),
            string.Empty,
            propertyChanged: OnTextChanged);

        /// <summary>
        /// The figure itself, already formatted.
        /// </summary>
        public static readonly BindableProperty ValueProperty = BindableProperty.Create(
            nameof(Value),
            typeof(string),
            typeof(StatTileView),
            string.Empty,
            propertyChanged: OnTextChanged);

        /// <summary>
        /// Optional icon glyph shown before the caption.
        /// </summary>
        public static readonly BindableProperty IconProperty = BindableProperty.Create(
            nameof(Icon),
            typeof(string),
            typeof(StatTileView),
            string.Empty,
            propertyChanged: OnTextChanged);

        /// <summary>
        /// Replaces the figure with a shimmering placeholder.
        /// </summary>
        public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(
            nameof(IsLoading),
            typeof(bool),
            typeof(StatTileView),
            false,
            propertyChanged: OnLoadingChanged);

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the stat tile.
        /// </summary>
        public StatTileView()
        {
            InitializeComponent();

            Skeleton.Blocks = SkeletonView.STAT_TILE;
        }

        #endregion

        #region Properties

        /// <summary>
        /// What the figure measures.
        /// </summary>
        public string Caption
        {
            get => (string)GetValue(CaptionProperty);
            set => SetValue(CaptionProperty, value);
        }

        /// <summary>
        /// The figure itself, already formatted.
        /// </summary>
        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>
        /// Optional icon glyph shown before the caption.
        /// </summary>
        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>
        /// Whether the tile is showing a placeholder instead of a figure.
        /// </summary>
        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Shows or hides the placeholder.
        /// </summary>
        /// <param name="bindable">The stat tile.</param>
        /// <param name="oldValue">Unused.</param>
        /// <param name="newValue">Unused.</param>
        private static void OnLoadingChanged(BindableObject bindable, object oldValue, object newValue)
        {
            StatTileView tile = (StatTileView)bindable;

            tile.Skeleton.IsVisible = tile.IsLoading;
            tile.StatLayout.Opacity = tile.IsLoading ? 0 : 1;
        }

        /// <summary>
        /// Repaints the labels, and keeps the pair readable as one phrase to a screen reader.
        /// </summary>
        /// <param name="bindable">The stat tile.</param>
        /// <param name="oldValue">Unused.</param>
        /// <param name="newValue">Unused.</param>
        private static void OnTextChanged(BindableObject bindable, object oldValue, object newValue)
        {
            StatTileView tile = (StatTileView)bindable;

            tile.IconLabel.Text = tile.Icon;
            tile.IconLabel.IsVisible = !string.IsNullOrEmpty(tile.Icon);
            tile.CaptionLabel.Text = tile.Caption;
            tile.ValueLabel.Text = tile.Value;

            SemanticProperties.SetDescription(tile, $"{tile.Caption}, {tile.Value}");
        }

        #endregion
    }
}
