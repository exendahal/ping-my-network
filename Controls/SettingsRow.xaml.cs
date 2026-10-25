using PingMyNetwork.Helper.Utilities;

namespace PingMyNetwork.Controls
{
    /// <summary>
    /// One row of an inset grouped list, used by the device and settings screens.
    /// </summary>
    public partial class SettingsRow : ContentView
    {
        #region Variables

        /// <summary>
        /// The row label.
        /// </summary>
        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title), typeof(string), typeof(SettingsRow), string.Empty, propertyChanged: Refresh);

        /// <summary>
        /// Trailing detail text, such as the current value of a preference.
        /// </summary>
        public static readonly BindableProperty ValueProperty = BindableProperty.Create(
            nameof(Value), typeof(string), typeof(SettingsRow), string.Empty, propertyChanged: Refresh);

        /// <summary>
        /// Leading Material Icons glyph.
        /// </summary>
        public static readonly BindableProperty IconProperty = BindableProperty.Create(
            nameof(Icon), typeof(string), typeof(SettingsRow), string.Empty, propertyChanged: Refresh);

        /// <summary>
        /// Shows the chevron. Set only when tapping the row actually leads somewhere.
        /// </summary>
        public static readonly BindableProperty ShowChevronProperty = BindableProperty.Create(
            nameof(ShowChevron), typeof(bool), typeof(SettingsRow), false, propertyChanged: Refresh);

        /// <summary>
        /// Turns the row into a switch row.
        /// </summary>
        public static readonly BindableProperty ShowSwitchProperty = BindableProperty.Create(
            nameof(ShowSwitch), typeof(bool), typeof(SettingsRow), false, propertyChanged: Refresh);

        /// <summary>
        /// State of the switch, when there is one.
        /// </summary>
        public static readonly BindableProperty IsToggledProperty = BindableProperty.Create(
            nameof(IsToggled), typeof(bool), typeof(SettingsRow), false,
            defaultBindingMode: BindingMode.TwoWay, propertyChanged: Refresh);

        /// <summary>
        /// Paints the row red, for actions that destroy something.
        /// </summary>
        public static readonly BindableProperty IsDestructiveProperty = BindableProperty.Create(
            nameof(IsDestructive), typeof(bool), typeof(SettingsRow), false, propertyChanged: Refresh);

        /// <summary>
        /// Greys the row out and stops it responding, for anything not built yet.
        /// </summary>
        public static readonly BindableProperty IsPendingProperty = BindableProperty.Create(
            nameof(IsPending), typeof(bool), typeof(SettingsRow), false, propertyChanged: Refresh);

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the row.
        /// </summary>
        public SettingsRow()
        {
            InitializeComponent();

            Apply();
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised when the row is tapped.
        /// </summary>
        public event EventHandler? Tapped;

        /// <summary>
        /// Raised when the row's switch changes.
        /// </summary>
        public event EventHandler<bool>? Toggled;

        #endregion

        #region Properties

        /// <summary>The row label.</summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>Trailing detail text.</summary>
        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>Leading glyph.</summary>
        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>Whether the chevron shows.</summary>
        public bool ShowChevron
        {
            get => (bool)GetValue(ShowChevronProperty);
            set => SetValue(ShowChevronProperty, value);
        }

        /// <summary>Whether the row carries a switch.</summary>
        public bool ShowSwitch
        {
            get => (bool)GetValue(ShowSwitchProperty);
            set => SetValue(ShowSwitchProperty, value);
        }

        /// <summary>State of the switch.</summary>
        public bool IsToggled
        {
            get => (bool)GetValue(IsToggledProperty);
            set => SetValue(IsToggledProperty, value);
        }

        /// <summary>Whether the row is destructive.</summary>
        public bool IsDestructive
        {
            get => (bool)GetValue(IsDestructiveProperty);
            set => SetValue(IsDestructiveProperty, value);
        }

        /// <summary>Whether the row is not built yet.</summary>
        public bool IsPending
        {
            get => (bool)GetValue(IsPendingProperty);
            set => SetValue(IsPendingProperty, value);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Repaints when any displayed property changes.
        /// </summary>
        /// <param name="bindable">The row.</param>
        /// <param name="oldValue">Unused.</param>
        /// <param name="newValue">Unused.</param>
        private static void Refresh(BindableObject bindable, object oldValue, object newValue)
        {
            ((SettingsRow)bindable).Apply();
        }

        /// <summary>
        /// Pushes the current properties onto the row's parts.
        /// </summary>
        private void Apply()
        {
            TitleLabel.Text = Title;

            ValueLabel.Text = Value;
            ValueLabel.IsVisible = !string.IsNullOrEmpty(Value);

            IconLabel.Text = Icon;
            IconLabel.IsVisible = !string.IsNullOrEmpty(Icon);

            ChevronLabel.IsVisible = ShowChevron && !ShowSwitch;
            RowSwitch.IsVisible = ShowSwitch;
            RowSwitch.IsToggled = IsToggled;

            TitleLabel.TextColor = IsDestructive
                ? StatusPalette.For(Helper.Enums.StatusLevel.Bad)
                : (Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Colors.White
                    : Colors.Black);

            // The live accent, so the icons change as soon as "Use system colors" is flipped.
            if (IsDestructive)
            {
                IconLabel.TextColor = StatusPalette.For(Helper.Enums.StatusLevel.Bad);
            }
            else
            {
                IconLabel.SetDynamicResource(Label.TextColorProperty, "Accent");
            }

            // Anything still to be built says so by looking unavailable, rather than by doing
            // nothing when tapped.
            Opacity = IsPending ? 0.45 : 1.0;

            // The whole row is one screen-reader stop: label, then value, then its state.
            SemanticProperties.SetDescription(
                this,
                ShowSwitch
                    ? $"{Title}, {(IsToggled ? "on" : "off")}"
                    : string.IsNullOrEmpty(Value) ? Title : $"{Title}, {Value}");
        }

        /// <summary>
        /// Forwards a tap, unless the row is not built yet.
        /// </summary>
        /// <param name="sender">The row.</param>
        /// <param name="e">Unused.</param>
        private void OnTapped(object? sender, TappedEventArgs e)
        {
            if (IsPending)
            {
                return;
            }

            if (ShowSwitch)
            {
                RowSwitch.IsToggled = !RowSwitch.IsToggled;
                return;
            }

            Tapped?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Forwards a switch change.
        /// </summary>
        /// <param name="sender">The switch.</param>
        /// <param name="e">The new state.</param>
        private void OnToggled(object? sender, ToggledEventArgs e)
        {
            if (IsToggled == e.Value)
            {
                return;
            }

            IsToggled = e.Value;
            Toggled?.Invoke(this, e.Value);
        }

        #endregion
    }
}
