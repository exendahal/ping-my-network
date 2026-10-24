using PingMyNetwork.ViewModels;

namespace PingMyNetwork.Views
{
    /// <summary>
    /// Port check: which TCP ports a host accepts connections on.
    /// </summary>
    public partial class PortCheckPage : ToolPage
    {
        #region Constructor

        /// <summary>
        /// Creates the page.
        /// </summary>
        /// <param name="viewModel">The tool's view model.</param>
        public PortCheckPage(PortCheckViewModel viewModel)
            : base(viewModel)
        {
            InitializeComponent();
        }

        #endregion
    }
}
