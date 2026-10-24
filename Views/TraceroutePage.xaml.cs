using PingMyNetwork.ViewModels;

namespace PingMyNetwork.Views
{
    /// <summary>
    /// Traceroute: the route to a host, hop by hop.
    /// </summary>
    public partial class TraceroutePage : ToolPage
    {
        #region Constructor

        /// <summary>
        /// Creates the page.
        /// </summary>
        /// <param name="viewModel">The tool's view model.</param>
        public TraceroutePage(TracerouteViewModel viewModel)
            : base(viewModel)
        {
            InitializeComponent();
        }

        #endregion
    }
}
