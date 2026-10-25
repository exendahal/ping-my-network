namespace PingMyNetwork.Resources.Styles
{
    /// <summary>
    /// The app's styles. A class rather than a Source reference, so App can load them after it has
    /// settled the accent colour they read.
    /// </summary>
    public partial class AppStyles : ResourceDictionary
    {
        #region Constructor

        /// <summary>
        /// Loads the styles.
        /// </summary>
        public AppStyles()
        {
            InitializeComponent();
        }

        #endregion
    }
}
