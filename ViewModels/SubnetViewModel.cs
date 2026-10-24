using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.Implementation;
using PingMyNetwork.Models;
using PingMyNetwork.Resources.Strings;

namespace PingMyNetwork.ViewModels
{
    /// <summary>
    /// Works out a subnet's range, mask and size as the address is typed, starting from this network.
    /// </summary>
    public sealed partial class SubnetViewModel : ObservableObject
    {
        #region Variables

        /// <summary>
        /// How long the "Copied" confirmation stays up.
        /// </summary>
        private const int COPIED_VISIBLE_MS = 2000;

        private CancellationTokenSource? _CopiedCancel;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the view model with this network's address and prefix.
        /// </summary>
        /// <param name="monitor">The connection monitor.</param>
        public SubnetViewModel(NetworkMonitor monitor)
        {
            NetworkSnapshot snapshot = monitor.Current;

            Input = snapshot.IpAddress is string address
                ? $"{address}/{snapshot.PrefixLength ?? 24}"
                : "192.168.1.10/24";
        }

        #endregion

        #region Properties

        /// <summary>
        /// The address and prefix or mask, e.g. "192.168.1.10/24".
        /// </summary>
        [ObservableProperty]
        public partial string Input { get; set; }

        /// <summary>
        /// The worked-out values, or empty when the input is not valid.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsInvalid))]
        public partial IReadOnlyList<DetailSection> Sections { get; set; } = [];

        /// <summary>
        /// Whether the input cannot be read.
        /// </summary>
        public bool IsInvalid => Sections.Count == 0;

        /// <summary>
        /// Confirmation after a copy, or empty.
        /// </summary>
        [ObservableProperty]
        public partial string CopiedText { get; set; } = string.Empty;

        /// <summary>
        /// Whether the copy confirmation is showing.
        /// </summary>
        [ObservableProperty]
        public partial bool IsCopiedVisible { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Recalculates on every keystroke; it is a few integer operations.
        /// </summary>
        /// <param name="value">The new input.</param>
        partial void OnInputChanged(string value)
        {
            SubnetInfo? subnet = SubnetCalculator.Calculate(value);

            Sections = subnet is null
                ? []
                :
                [
                    DetailSection.From(
                        AppResources.SubnetSectionRange,
                        (AppResources.SubnetNetwork, $"{subnet.Network}/{subnet.PrefixLength}"),
                        (AppResources.SubnetFirstHost, subnet.FirstHost),
                        (AppResources.SubnetLastHost, subnet.LastHost),
                        (AppResources.SubnetBroadcast, subnet.Broadcast),
                        (AppResources.SubnetHosts, subnet.UsableHosts.ToString("N0"))),
                    DetailSection.From(
                        AppResources.SubnetSectionMask,
                        (AppResources.SubnetMask, subnet.Mask),
                        (AppResources.SubnetWildcard, subnet.Wildcard),
                        (AppResources.SubnetPrefix, $"/{subnet.PrefixLength}"),
                        (AppResources.SubnetScope, subnet.IsPrivate ? AppResources.SubnetPrivate : AppResources.SubnetPublic)),
                ];
        }

        /// <summary>
        /// Copies one value and confirms it briefly.
        /// </summary>
        /// <param name="row">The row that was tapped.</param>
        /// <returns>A task that completes when the confirmation has gone.</returns>
        [RelayCommand]
        private async Task CopyAsync(DetailRow row)
        {
            await Clipboard.Default.SetTextAsync(row.Value);

            _CopiedCancel?.Cancel();
            _CopiedCancel = new CancellationTokenSource();

            CopiedText = NetworkFormat.Text(AppResources.DetailsCopied, row.Title.ToLowerInvariant());
            IsCopiedVisible = true;

            try
            {
                await Task.Delay(COPIED_VISIBLE_MS, _CopiedCancel.Token);
                IsCopiedVisible = false;
            }
            catch (TaskCanceledException)
            {
                // A newer copy is showing its own confirmation.
            }
        }

        #endregion
    }
}
