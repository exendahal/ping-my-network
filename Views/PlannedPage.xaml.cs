using PingMyNetwork.Helper.Utilities;

namespace PingMyNetwork.Views
{
    /// <summary>
    /// Stands in for a destination that is still being built, describing what it will do.
    /// </summary>
    public partial class PlannedPage : ContentPage
    {
        #region Variables

        /// <summary>
        /// The destination's name.
        /// </summary>
        public static readonly BindableProperty HeadingProperty = BindableProperty.Create(
            nameof(Heading), typeof(string), typeof(PlannedPage), string.Empty, propertyChanged: OnTextChanged);

        /// <summary>
        /// One sentence on what the destination will do.
        /// </summary>
        public static readonly BindableProperty BodyProperty = BindableProperty.Create(
            nameof(Body), typeof(string), typeof(PlannedPage), string.Empty, propertyChanged: OnTextChanged);

        /// <summary>
        /// The destination's icon glyph.
        /// </summary>
        public static readonly BindableProperty IconProperty = BindableProperty.Create(
            nameof(Icon), typeof(string), typeof(PlannedPage), string.Empty, propertyChanged: OnTextChanged);

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the page.
        /// </summary>
        public PlannedPage()
        {
            InitializeComponent();

            AppShell.ApplyTransition(this);

            SizeChanged += (_, _) => PageGrid.Padding = LayoutWidthUtility.PagePadding(Window?.Width ?? Width);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The destination's name.
        /// </summary>
        public string Heading
        {
            get => (string)GetValue(HeadingProperty);
            set => SetValue(HeadingProperty, value);
        }

        /// <summary>
        /// One sentence on what the destination will do.
        /// </summary>
        public string Body
        {
            get => (string)GetValue(BodyProperty);
            set => SetValue(BodyProperty, value);
        }

        /// <summary>
        /// The destination's icon glyph.
        /// </summary>
        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Copies the texts onto the labels.
        /// </summary>
        /// <param name="bindable">The page.</param>
        /// <param name="oldValue">Unused.</param>
        /// <param name="newValue">Unused.</param>
        private static void OnTextChanged(BindableObject bindable, object oldValue, object newValue)
        {
            PlannedPage page = (PlannedPage)bindable;

            page.HeadingLabel.Text = page.Heading;
            page.BodyLabel.Text = page.Body;
            page.IconLabel.Text = page.Icon;
        }

        #endregion
    }
}
