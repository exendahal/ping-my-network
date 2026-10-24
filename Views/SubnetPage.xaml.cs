using PingMyNetwork.ViewModels;

namespace PingMyNetwork.Views
{
    /// <summary>
    /// Subnet calculator: a subnet's range, mask and size.
    /// </summary>
    public partial class SubnetPage : ToolPage
    {
        #region Constructor

        /// <summary>
        /// Creates the page.
        /// </summary>
        /// <param name="viewModel">The tool's view model.</param>
        public SubnetPage(SubnetViewModel viewModel)
            : base(viewModel)
        {
            InitializeComponent();
        }

        #endregion
    }
}
