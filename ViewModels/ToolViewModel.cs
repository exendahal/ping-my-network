using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiWifiManager.Abstractions;
using PingMyNetwork.Resources.Strings;

namespace PingMyNetwork.ViewModels
{
    /// <summary>
    /// What every tool shares: one button that starts the tool and stops it while it runs, an error
    /// line, and cancelling whatever is running when the screen closes.
    /// </summary>
    public abstract partial class ToolViewModel : ObservableObject
    {
        #region Variables

        private CancellationTokenSource? _Cancel;

        #endregion

        #region Properties

        /// <summary>
        /// Whether the tool is running.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RunButtonText))]
        public partial bool IsRunning { get; set; }

        /// <summary>
        /// Why the last run failed, or empty.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        public partial string ErrorText { get; set; } = string.Empty;

        /// <summary>
        /// Whether there is an error to show.
        /// </summary>
        public bool HasError => ErrorText.Length > 0;

        /// <summary>
        /// The run button's label: the tool's action, or "Stop" while it runs.
        /// </summary>
        public string RunButtonText => IsRunning ? AppResources.ToolStop : ActionText;

        /// <summary>
        /// What the run button says while idle, e.g. "Start" or "Look up".
        /// </summary>
        protected virtual string ActionText => AppResources.ToolStart;

        #endregion

        #region Methods

        /// <summary>
        /// Cancels a run in progress. The button reads "Start" again straight away, even if the
        /// platform takes a moment to abandon an in-flight request. Called when the screen closes.
        /// </summary>
        public void Stop()
        {
            _Cancel?.Cancel();
            _Cancel = null;
            IsRunning = false;
        }

        /// <summary>
        /// Runs the tool, or stops it when it is already running. Concurrent so the same button can
        /// stop the run it started; a toolkit async command otherwise disables itself while running.
        /// </summary>
        /// <returns>A task that completes when the run ends.</returns>
        [RelayCommand(AllowConcurrentExecutions = true)]
        private async Task RunAsync()
        {
            if (IsRunning)
            {
                Stop();
                return;
            }

            using CancellationTokenSource cancel = new();

            _Cancel = cancel;
            ErrorText = string.Empty;
            IsRunning = true;

            try
            {
                await RunCoreAsync(cancel.Token);
            }
            catch (OperationCanceledException)
            {
                // Stopped by the user or by leaving the screen; what was found so far stays.
            }
            catch (Exception ex)
            {
                // A tool failing must never take the app down with it; say why and stay usable.
                ErrorText = string.IsNullOrWhiteSpace(ex.Message) ? AppResources.ToolFailed : ex.Message;
            }
            finally
            {
                // A run stopped early may finish after a new one started; it must not end that one.
                if (_Cancel == cancel)
                {
                    _Cancel = null;
                    IsRunning = false;
                }
            }
        }

        /// <summary>
        /// Does the tool's work.
        /// </summary>
        /// <param name="token">Cancelled when the user stops the tool or leaves.</param>
        /// <returns>A task that completes when the work is done.</returns>
        protected abstract Task RunCoreAsync(CancellationToken token);

        /// <summary>
        /// Shows a failed library call's message, unless it failed only because it was cancelled.
        /// </summary>
        /// <typeparam name="T">The response's data type.</typeparam>
        /// <param name="response">The library's response.</param>
        /// <returns>True when the call succeeded.</returns>
        protected bool Succeeded<T>(WifiManagerResponse<T> response)
        {
            if (response.ErrorCode is WifiErrorCodes.Success)
            {
                return true;
            }

            if (response.ErrorCode is not WifiErrorCodes.OperationCanceled)
            {
                ErrorText = response.ErrorMessage ?? AppResources.ToolFailed;
            }

            return false;
        }

        #endregion
    }
}
