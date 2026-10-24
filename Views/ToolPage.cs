using PingMyNetwork.ViewModels;

namespace PingMyNetwork.Views
{
    /// <summary>
    /// A tool's screen, pushed on top of Tools. Whatever the tool is doing stops when it closes.
    /// </summary>
    public abstract class ToolPage : ContentPage
    {
        #region Variables

        private readonly ToolViewModel? _Tool;

        #endregion

        #region Constructor

        /// <summary>
        /// Binds the page to its view model and gives it the push transition.
        /// </summary>
        /// <param name="viewModel">The tool's view model.</param>
        protected ToolPage(object viewModel)
        {
            BindingContext = viewModel;
            _Tool = viewModel as ToolViewModel;

            AppShell.ApplyTransition(this);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Stops a ping, trace or check still running, so it does not carry on unseen.
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            _Tool?.Stop();

            // A field still focused as the page leaves keeps Android's keyboard open over the
            // screen underneath.
            foreach (InputView field in this.GetVisualTreeDescendants().OfType<InputView>().Where(view => view.IsFocused))
            {
                _ = field.HideSoftInputAsync(CancellationToken.None);
                field.Unfocus();
            }
        }

        #endregion
    }
}
