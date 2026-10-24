using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    /// Checks which TCP ports a host accepts connections on.
    /// </summary>
    public sealed partial class PortCheckViewModel : ToolViewModel
    {
        #region Variables

        /// <summary>
        /// Key of the well-known ports choice.
        /// </summary>
        private const string COMMON = "common";

        /// <summary>
        /// Key of the typed-in list choice.
        /// </summary>
        private const string CUSTOM = "custom";

        private readonly IWifiNetworkService _Wifi;

        private int _Total;

        private int _Checked;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the view model, suggesting the router as the host.
        /// </summary>
        /// <param name="wifi">The library.</param>
        /// <param name="monitor">The connection monitor, for the router's address.</param>
        public PortCheckViewModel(IWifiNetworkService wifi, NetworkMonitor monitor)
        {
            _Wifi = wifi;

            Host = monitor.Current.Gateway ?? string.Empty;
            Presets =
            [
                new ChoiceOption(COMMON, NetworkFormat.Text(AppResources.PortsCommonFormat, LanDeviceDetails.KnownPorts.Count), true),
                new ChoiceOption(CUSTOM, AppResources.PortsCustom, false),
            ];
        }

        #endregion

        #region Properties

        /// <summary>
        /// The host to check.
        /// </summary>
        [ObservableProperty]
        public partial string Host { get; set; }

        /// <summary>
        /// Which ports to check.
        /// </summary>
        public ObservableCollection<ChoiceOption> Presets { get; }

        /// <summary>
        /// Whether the typed-in list is in use, so its field shows.
        /// </summary>
        [ObservableProperty]
        public partial bool IsCustom { get; set; }

        /// <summary>
        /// The typed-in ports, e.g. "22, 80, 8000-8100".
        /// </summary>
        [ObservableProperty]
        public partial string CustomPorts { get; set; } = "22, 80, 443, 3389, 8080";

        /// <summary>
        /// The open ports found so far, in port order.
        /// </summary>
        public ObservableCollection<ResultRow> OpenPorts { get; } = [];

        /// <summary>
        /// Share of ports checked, 0 to 1.
        /// </summary>
        [ObservableProperty]
        public partial double Progress { get; set; }

        /// <summary>
        /// How many are open out of how many checked.
        /// </summary>
        [ObservableProperty]
        public partial string Summary { get; set; } = string.Empty;

        /// <summary>
        /// Whether a check has started, so the results card shows.
        /// </summary>
        [ObservableProperty]
        public partial bool HasResults { get; set; }

        /// <inheritdoc />
        protected override string ActionText => AppResources.PortsCheck;

        #endregion

        #region Methods

        /// <summary>
        /// Picks the well-known ports or the typed-in list.
        /// </summary>
        /// <param name="key">The choice's key.</param>
        [RelayCommand]
        private void SelectPreset(string key)
        {
            for (var i = 0; i < Presets.Count; i++)
            {
                Presets[i] = Presets[i] with { IsSelected = Presets[i].Key == key };
            }

            IsCustom = key == CUSTOM;
        }

        /// <summary>
        /// Checks every port, listing open ones as they are found.
        /// </summary>
        /// <param name="token">Cancelled on stop.</param>
        /// <returns>A task that completes when every port has been tried.</returns>
        protected override async Task RunCoreAsync(CancellationToken token)
        {
            string host = Host.Trim();
            List<int>? ports = IsCustom ? PortList.Parse(CustomPorts) : [.. LanDeviceDetails.KnownPorts.Keys.Order()];

            if (host.Length == 0)
            {
                ErrorText = AppResources.ToolHostRequired;
                return;
            }

            if (ports is null)
            {
                ErrorText = NetworkFormat.Text(AppResources.PortsInvalidFormat, PortList.MAX_PORTS);
                return;
            }

            OpenPorts.Clear();
            _Total = ports.Count;
            _Checked = 0;
            Progress = 0;
            HasResults = true;
            UpdateSummary();

            // Created on the UI thread, so results arrive on it.
            Progress<PortCheckResult> progress = new(result =>
            {
                if (!token.IsCancellationRequested)
                {
                    OnPortChecked(result);
                }
            });
            WifiManagerResponse<List<PortCheckResult>> response = await _Wifi.CheckPortsAsync(host, ports, progress, token);

            token.ThrowIfCancellationRequested();
            Succeeded(response);
        }

        /// <summary>
        /// Counts a checked port and lists it, in order, when open.
        /// </summary>
        /// <param name="result">The port's result.</param>
        private void OnPortChecked(PortCheckResult result)
        {
            _Checked++;
            Progress = (double)_Checked / _Total;

            if (result.IsOpen)
            {
                int index = 0;

                while ((index < OpenPorts.Count) && (int.Parse(OpenPorts[index].Leading) < result.Port))
                {
                    index++;
                }

                OpenPorts.Insert(index, new ResultRow(
                    result.Port.ToString(),
                    result.Service ?? AppResources.PortsUnknownService,
                    Trailing: AppResources.PortsOpen,
                    Level: StatusLevel.Good));
            }

            UpdateSummary();
        }

        /// <summary>
        /// Refreshes the progress line.
        /// </summary>
        private void UpdateSummary()
        {
            Summary = _Checked < _Total
                ? NetworkFormat.Text(AppResources.PortsProgressFormat, _Checked, _Total, OpenPorts.Count)
                : NetworkFormat.Text(AppResources.PortsDoneFormat, OpenPorts.Count, _Total);
        }

        #endregion
    }
}
