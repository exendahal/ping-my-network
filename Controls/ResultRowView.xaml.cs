using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.Models;

namespace PingMyNetwork.Controls
{
    /// <summary>
    /// Shows one <see cref="ResultRow"/>, with the trailing figure in its status colour.
    /// </summary>
    public partial class ResultRowView : ContentView
    {
        #region Constructor

        /// <summary>
        /// Creates the row.
        /// </summary>
        public ResultRowView()
        {
            InitializeComponent();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Colours the trailing figure for the row's level; a row without one keeps the neutral style.
        /// </summary>
        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            if (BindingContext is ResultRow { Level: { } level })
            {
                TrailingLabel.TextColor = StatusPalette.For(level);
            }
            else
            {
                // Setting a colour above replaced the theme binding, so a recycled row restores it.
                TrailingLabel.SetAppThemeColor(
                    Label.TextColorProperty,
                    ResourceLookup.Get("CaptionLight", Colors.Gray),
                    ResourceLookup.Get("CaptionDark", Colors.Gray));
            }
        }

        #endregion
    }
}
