using CommunityToolkit.Mvvm.ComponentModel;
using MauiWifiManager;
using MauiWifiManager.Abstractions;
using PingMyNetwork.Helper.Enums;
using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.Implementation;
using PingMyNetwork.Resources.Strings;

namespace PingMyNetwork.ViewModels
{
    /// <summary>
    /// Measures download and upload speed and latency to the internet.
    /// </summary>
    public sealed partial class SpeedTestViewModel : ToolViewModel
    {
        #region Variables

        /// <summary>
        /// The gauge is full at this speed. Logarithmic, so 10 and 100 Mbps both read clearly.
        /// </summary>
        private const double GAUGE_FULL_MBPS = 1000;

        /// <summary>
        /// Below this download speed, video calls and streaming struggle.
        /// </summary>
        private const double SLOW_MBPS = 10;

        /// <summary>
        /// At or above this, a household rarely waits on the connection.
        /// </summary>
        private const double FAST_MBPS = 50;

        private readonly IWifiNetworkService _Wifi;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the view model.
        /// </summary>
        /// <param name="wifi">The library.</param>
        /// <param name="monitor">The connection monitor, for whether data is metered.</param>
        public SpeedTestViewModel(IWifiNetworkService wifi, NetworkMonitor monitor)
        {
            _Wifi = wifi;

            IsMetered = monitor.Current.IsMetered;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Whether the connection is metered, so the data warning stands out.
        /// </summary>
        public bool IsMetered { get; }

        /// <summary>
        /// Download speed on the gauge, 0 to 1.
        /// </summary>
        [ObservableProperty]
        public partial double Fraction { get; set; }

        /// <summary>
        /// The gauge's colour.
        /// </summary>
        [ObservableProperty]
        public partial StatusLevel Level { get; set; } = StatusLevel.Good;

        /// <summary>
        /// The figure in the middle of the gauge.
        /// </summary>
        [ObservableProperty]
        public partial string DownloadValue { get; set; } = AppResources.ValueMissing;

        /// <summary>
        /// Upload speed.
        /// </summary>
        [ObservableProperty]
        public partial string UploadText { get; set; } = AppResources.ValueMissing;

        /// <summary>
        /// Latency under load.
        /// </summary>
        [ObservableProperty]
        public partial string LatencyText { get; set; } = AppResources.ValueMissing;

        /// <summary>
        /// What the result means, or what is happening while it runs.
        /// </summary>
        [ObservableProperty]
        public partial string Verdict { get; set; } = AppResources.SpeedIdle;

        /// <inheritdoc />
        protected override string ActionText => Fraction > 0 ? AppResources.SpeedAgain : AppResources.ToolStart;

        #endregion

        #region Methods

        /// <summary>
        /// Runs the test.
        /// </summary>
        /// <param name="token">Cancelled on stop.</param>
        /// <returns>A task that completes with the result.</returns>
        protected override async Task RunCoreAsync(CancellationToken token)
        {
            Verdict = AppResources.SpeedRunning;

            WifiManagerResponse<SpeedTestResult> response = await _Wifi.MeasureSpeedAsync(null, token);

            if (token.IsCancellationRequested || !Succeeded(response) || (response.Data is not SpeedTestResult result))
            {
                Verdict = AppResources.SpeedIdle;
                return;
            }

            double download = result.DownloadMbps;

            Level = download >= FAST_MBPS ? StatusLevel.Good : download >= SLOW_MBPS ? StatusLevel.Fair : StatusLevel.Poor;
            Fraction = Math.Clamp(Math.Log10(1 + download) / Math.Log10(1 + GAUGE_FULL_MBPS), 0.02, 1);
            DownloadValue = Mbps(download);
            UploadText = result.UploadMbps is double upload ? NetworkFormat.Text(AppResources.SpeedMbpsFormat, Mbps(upload)) : AppResources.ValueMissing;
            LatencyText = NetworkFormat.Latency(result.LatencyMs);
            Verdict = Level switch
            {
                StatusLevel.Good => AppResources.SpeedVerdictFast,
                StatusLevel.Fair => AppResources.SpeedVerdictOk,
                _ => AppResources.SpeedVerdictSlow,
            };
        }

        /// <summary>
        /// A speed with sensible precision: "8.4" under 10, whole numbers above.
        /// </summary>
        /// <param name="mbps">Megabits per second.</param>
        /// <returns>The number.</returns>
        private static string Mbps(double mbps)
        {
            return mbps < 10 ? mbps.ToString("0.0") : mbps.ToString("0");
        }

        #endregion
    }
}
