using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiWifiManager;
using MauiWifiManager.Abstractions;
using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.Implementation;
using PingMyNetwork.Models;
using PingMyNetwork.Resources.Strings;

namespace PingMyNetwork.ViewModels
{
    /// <summary>
    /// Asks a DNS server for one kind of record and lists the answers with their lifetimes.
    /// </summary>
    public sealed partial class DnsLookupViewModel : ToolViewModel
    {
        #region Variables

        private readonly IWifiNetworkService _Wifi;

        /// <summary>
        /// The network's own DNS server, used when the server field is left empty.
        /// </summary>
        private readonly string? _NetworkServer;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the view model.
        /// </summary>
        /// <param name="wifi">The library.</param>
        /// <param name="monitor">The connection monitor, for the network's DNS server.</param>
        public DnsLookupViewModel(IWifiNetworkService wifi, NetworkMonitor monitor)
        {
            _Wifi = wifi;

            // The app knows the network's DNS server even where the platform hides it from .NET
            // (Android), so it is passed explicitly rather than left to the library's fallback.
            _NetworkServer = monitor.Current.DnsServers.FirstOrDefault(server => server.Contains('.'));

            ServerPlaceholder = _NetworkServer is null
                ? AppResources.DnsServerPlaceholder
                : NetworkFormat.Text(AppResources.DnsServerNetworkFormat, _NetworkServer);
            Types =
            [
                .. new[] { DnsRecordType.A, DnsRecordType.Aaaa, DnsRecordType.Cname, DnsRecordType.Mx, DnsRecordType.Txt, DnsRecordType.Ns }
                    .Select(type => new ChoiceOption(type.ToString(), TypeName(type), type == DnsRecordType.A)),
            ];
        }

        #endregion

        #region Properties

        /// <summary>
        /// The name to look up.
        /// </summary>
        [ObservableProperty]
        public partial string Name { get; set; } = "example.com";

        /// <summary>
        /// A DNS server's address, or empty for the network's own.
        /// </summary>
        [ObservableProperty]
        public partial string Server { get; set; } = string.Empty;

        /// <summary>
        /// Hint in the empty server field, naming the server that will be used.
        /// </summary>
        public string ServerPlaceholder { get; }

        /// <summary>
        /// The record types to choose from.
        /// </summary>
        public ObservableCollection<ChoiceOption> Types { get; }

        /// <summary>
        /// The answers.
        /// </summary>
        public ObservableCollection<ResultRow> Records { get; } = [];

        /// <summary>
        /// Which server answered, how fast, and how many records.
        /// </summary>
        [ObservableProperty]
        public partial string Summary { get; set; } = string.Empty;

        /// <summary>
        /// Whether a lookup has finished, so the results card shows.
        /// </summary>
        [ObservableProperty]
        public partial bool HasResults { get; set; }

        /// <inheritdoc />
        protected override string ActionText => AppResources.DnsLookUp;

        #endregion

        #region Methods

        /// <summary>
        /// Picks the record type.
        /// </summary>
        /// <param name="key">The type's name.</param>
        [RelayCommand]
        private void SelectType(string key)
        {
            for (var i = 0; i < Types.Count; i++)
            {
                Types[i] = Types[i] with { IsSelected = Types[i].Key == key };
            }
        }

        /// <summary>
        /// Looks the name up.
        /// </summary>
        /// <param name="token">Cancelled on stop.</param>
        /// <returns>A task that completes with the answer.</returns>
        protected override async Task RunCoreAsync(CancellationToken token)
        {
            string name = Name.Trim();

            if (name.Length == 0)
            {
                ErrorText = AppResources.DnsNameRequired;
                return;
            }

            DnsRecordType type = Enum.Parse<DnsRecordType>(Types.First(option => option.IsSelected).Key);
            string? server = string.IsNullOrWhiteSpace(Server) ? _NetworkServer : Server.Trim();

            Records.Clear();
            HasResults = false;

            WifiManagerResponse<DnsLookupResult> response = await _Wifi.LookupDnsAsync(name, type, server, token);

            token.ThrowIfCancellationRequested();

            if (!Succeeded(response) || (response.Data is not DnsLookupResult result))
            {
                return;
            }

            // Mail servers are tried lowest preference first, so list them that way.
            foreach (DnsRecord record in result.Records.OrderBy(record => record.Type == DnsRecordType.Mx ? int.Parse(record.Value.Split(' ')[0]) : 0))
            {
                Records.Add(new ResultRow(
                    TypeName(record.Type),
                    record.Value,
                    string.Equals(record.Name, result.Name, StringComparison.OrdinalIgnoreCase) ? null : record.Name,
                    NetworkFormat.Text(AppResources.DnsTtlFormat, Duration(record.TimeToLiveSeconds))));
            }

            HasResults = true;
            Summary = result.NameNotFound
                ? NetworkFormat.Text(AppResources.DnsNotFoundFormat, result.Name, result.Server)
                : result.Records.Count == 0
                    ? NetworkFormat.Text(AppResources.DnsNoRecordsFormat, TypeName(type), result.Server)
                    : NetworkFormat.Text(AppResources.DnsSummaryFormat, result.Records.Count, result.Server, result.ElapsedMs);
        }

        /// <summary>
        /// How a record type is usually written.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>For example "AAAA".</returns>
        private static string TypeName(DnsRecordType type)
        {
            return type.ToString().ToUpperInvariant();
        }

        /// <summary>
        /// A time to live in the largest whole unit, e.g. "5 min" or "1 h".
        /// </summary>
        /// <param name="seconds">The time to live.</param>
        /// <returns>The duration.</returns>
        private static string Duration(int seconds)
        {
            TimeSpan span = TimeSpan.FromSeconds(seconds);

            if (span.TotalDays >= 1)
            {
                return NetworkFormat.Text(AppResources.DurationDaysFormat, (int)span.TotalDays);
            }

            if (span.TotalHours >= 1)
            {
                return NetworkFormat.Text(AppResources.DurationHoursFormat, (int)span.TotalHours);
            }

            return span.TotalMinutes >= 1
                ? NetworkFormat.Text(AppResources.DurationMinutesFormat, (int)span.TotalMinutes)
                : NetworkFormat.Text(AppResources.DurationSecondsFormat, seconds);
        }

        #endregion
    }
}
