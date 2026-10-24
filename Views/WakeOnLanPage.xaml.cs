using PingMyNetwork.ViewModels;

namespace PingMyNetwork.Views
{
    /// <summary>
    /// Wake-on-LAN: sends a magic packet to wake a sleeping computer.
    /// </summary>
    public partial class WakeOnLanPage : ToolPage
    {
        #region Constructor

        /// <summary>
        /// Creates the page.
        /// </summary>
        /// <param name="viewModel">The tool's view model.</param>
        public WakeOnLanPage(WakeOnLanViewModel viewModel)
            : base(viewModel)
        {
            InitializeComponent();
        }

        #endregion
    }
}
