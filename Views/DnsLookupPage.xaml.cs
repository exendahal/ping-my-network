using PingMyNetwork.ViewModels;

namespace PingMyNetwork.Views
{
    /// <summary>
    /// DNS lookup: one record type, straight from a DNS server.
    /// </summary>
    public partial class DnsLookupPage : ToolPage
    {
        #region Constructor

        /// <summary>
        /// Creates the page.
        /// </summary>
        /// <param name="viewModel">The tool's view model.</param>
        public DnsLookupPage(DnsLookupViewModel viewModel)
            : base(viewModel)
        {
            InitializeComponent();
        }

        #endregion
    }
}
