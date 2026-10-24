namespace PingMyNetwork.Controls
{
    /// <summary>
    /// Back button and title for a pushed screen.
    /// </summary>
    public partial class PageHeader : ContentView
    {
        #region Variables

        /// <summary>
        /// The screen's title.
        /// </summary>
        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title), typeof(string), typeof(PageHeader), string.Empty,
            propertyChanged: static (bindable, _, value) => ((PageHeader)bindable).TitleLabel.Text = value as string);

        private bool _IsLeaving;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the header.
        /// </summary>
        public PageHeader()
        {
            InitializeComponent();
        }

        #endregion

        #region Properties

        /// <summary>
        /// The screen's title.
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Goes back to the screen underneath, once however often it is tapped.
        /// </summary>
        /// <param name="sender">The back button.</param>
        /// <param name="e">The event data.</param>
        private async void OnBackClicked(object? sender, EventArgs e)
        {
            if (_IsLeaving)
            {
                return;
            }

            _IsLeaving = true;

            try
            {
                await Shell.Current.GoToAsync("..");
            }
            finally
            {
                _IsLeaving = false;
            }
        }

        #endregion
    }
}
