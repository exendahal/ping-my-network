using CommunityToolkit.Mvvm.ComponentModel;
using MauiWifiManager;
using MauiWifiManager.Abstractions;
using PingMyNetwork.Helper.Enums;
using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.Implementation;
using PingMyNetwork.Models;
using PingMyNetwork.Resources.Strings;

namespace PingMyNetwork.ViewModels
{
    /// <summary>
    /// Measures download and upload speed and latency, with a meter that follows the test live.
    /// </summary>
    /// <remarks>
    /// The meter's scale (see <see cref="SpeedScale"/>) is logarithmic and steps its top up past a
    /// gigabit, so neither a slow line nor a multi-gigabit one leaves the needle meaningless.
    /// </remarks>
    public sealed partial class SpeedTestViewModel : ToolViewModel
    {
        #region Variables

        /// <summary>
        /// Below this download speed, video calls and streaming struggle.
        /// </summary>
        private const double SLOW_MBPS = 10;

        /// <summary>
        /// At or above this, a household rarely waits on the connection.
        /// </summary>
        private const double FAST_MBPS = 50;

        private readonly IWifiNetworkService _Wifi;

        /// <summary>
        /// The top of the meter's scale right now, in Mbps.
        /// </summary>
        private double _Range = SpeedScale.DefaultRange;

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
            Ticks = SpeedScale.Ticks(_Range);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Whether the connection is metered, so the data warning stands out.
        /// </summary>
        public bool IsMetered { get; }

        /// <summary>
        /// The meter's labelled marks, which change when the scale ranges up.
        /// </summary>
        [ObservableProperty]
        public partial IReadOnlyList<GaugeTick> Ticks { get; set; }

        /// <summary>
        /// The speed on the meter, 0 to 1.
        /// </summary>
        [ObservableProperty]
        public partial double Fraction { get; set; }

        /// <summary>
        /// The meter's colour: green while downloading, purple while uploading, then the verdict.
        /// </summary>
        [ObservableProperty]
        public partial StatusLevel Level { get; set; } = StatusLevel.Good;

        /// <summary>
        /// What the big number is, e.g. "Download"; empty before a test.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPhase))]
        public partial string PhaseText { get; set; } = string.Empty;

        /// <summary>
        /// Whether the phase label shows.
        /// </summary>
        public bool HasPhase => PhaseText.Length > 0;

        /// <summary>
        /// The big number in the meter.
        /// </summary>
        [ObservableProperty]
        public partial string LiveValue { get; set; } = AppResources.ValueMissing;

        /// <summary>
        /// Download speed once measured.
        /// </summary>
        [ObservableProperty]
        public partial string DownloadText { get; set; } = AppResources.ValueMissing;

        /// <summary>
        /// Upload speed once measured.
        /// </summary>
        [ObservableProperty]
        public partial string UploadText { get; set; } = AppResources.ValueMissing;

        /// <summary>
        /// Latency once measured.
        /// </summary>
        [ObservableProperty]
        public partial string LatencyText { get; set; } = AppResources.ValueMissing;

        /// <summary>
        /// What the result means, or what is happening while it runs.
        /// </summary>
        [ObservableProperty]
        public partial string Verdict { get; set; } = AppResources.SpeedIdle;

        /// <inheritdoc />
        protected override string ActionText => DownloadText != AppResources.ValueMissing ? AppResources.SpeedAgain : AppResources.ToolStart;

        #endregion

        #region Methods

        /// <summary>
        /// Runs the test, moving the meter as progress arrives.
        /// </summary>
        /// <param name="token">Cancelled on stop.</param>
        /// <returns>A task that completes with the result.</returns>
        protected override async Task RunCoreAsync(CancellationToken token)
        {
            SetRange(SpeedScale.DefaultRange);
            Fraction = 0;
            Level = StatusLevel.Good;
            LiveValue = AppResources.ValueMissing;
            DownloadText = AppResources.ValueMissing;
            UploadText = AppResources.ValueMissing;
            LatencyText = AppResources.ValueMissing;
            PhaseText = AppResources.SpeedPhaseLatency;
            Verdict = AppResources.SpeedRunning;

            // Created on the UI thread, so each report arrives on it.
            Progress<SpeedTestProgress> progress = new(update =>
            {
                if (!token.IsCancellationRequested)
                {
                    Show(update);
                }
            });

            WifiManagerResponse<SpeedTestResult> response = await _Wifi.MeasureSpeedAsync(null, progress, token);

            if (token.IsCancellationRequested || !Succeeded(response) || (response.Data is not SpeedTestResult result))
            {
                Fraction = 0;
                PhaseText = string.Empty;
                LiveValue = AppResources.ValueMissing;
                Verdict = AppResources.SpeedIdle;
                return;
            }

            // The meter settles on the download speed, coloured by what it means.
            double download = result.DownloadMbps;

            Level = download >= FAST_MBPS ? StatusLevel.Good : download >= SLOW_MBPS ? StatusLevel.Fair : StatusLevel.Poor;
            SetRange(SpeedScale.RangeFor(download, _Range));
            Fraction = SpeedScale.FractionOf(download, _Range);
            PhaseText = AppResources.SpeedPhaseDownload;
            LiveValue = SpeedScale.Format(download);
            DownloadText = NetworkFormat.Text(AppResources.SpeedMbpsFormat, SpeedScale.Format(download));
            UploadText = result.UploadMbps is double upload
                ? NetworkFormat.Text(AppResources.SpeedMbpsFormat, SpeedScale.Format(upload))
                : AppResources.ValueMissing;
            LatencyText = NetworkFormat.Latency(result.LatencyMs);
            Verdict = Level switch
            {
                StatusLevel.Good => AppResources.SpeedVerdictFast,
                StatusLevel.Fair => AppResources.SpeedVerdictOk,
                _ => AppResources.SpeedVerdictSlow,
            };
        }

        /// <summary>
        /// Moves the meter and fills in each card as its phase finishes.
        /// </summary>
        /// <param name="update">The latest progress.</param>
        private void Show(SpeedTestProgress update)
        {
            if (update.LatencyMs is double latency)
            {
                LatencyText = NetworkFormat.Latency(latency);
            }

            switch (update.Phase)
            {
                case SpeedTestPhase.Download:
                    PhaseText = AppResources.SpeedPhaseDownload;
                    Level = StatusLevel.Good;
                    break;

                case SpeedTestPhase.Upload:
                    // The download card keeps the last download reading until the final result.
                    if (PhaseText == AppResources.SpeedPhaseDownload)
                    {
                        DownloadText = NetworkFormat.Text(AppResources.SpeedMbpsFormat, LiveValue);
                    }

                    PhaseText = AppResources.SpeedPhaseUpload;
                    Level = StatusLevel.Notice;
                    break;

                default:
                    return;
            }

            SetRange(SpeedScale.RangeFor(update.Mbps, _Range));
            Fraction = SpeedScale.FractionOf(update.Mbps, _Range);
            LiveValue = SpeedScale.Format(update.Mbps);
        }

        /// <summary>
        /// Moves the scale's top, relabelling the marks when it changes.
        /// </summary>
        /// <param name="range">The new top in Mbps.</param>
        private void SetRange(double range)
        {
            if (range != _Range)
            {
                _Range = range;
                Ticks = SpeedScale.Ticks(range);
            }
        }

        #endregion
    }
}
