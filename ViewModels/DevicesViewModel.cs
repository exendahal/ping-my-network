using System.Collections.ObjectModel;
using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PingMyNetwork.Helper.Enums;
using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.Implementation;
using PingMyNetwork.Models;
using PingMyNetwork.Resources.Strings;

namespace PingMyNetwork.ViewModels
{
    /// <summary>
    /// The Devices screen: the list, its filters and search, and the scan.
    /// </summary>
    /// <remarks>
    /// A singleton, so switching tabs keeps the list, the filter and a scan in progress. Updates from
    /// a scan arrive many times a second from background threads; they are coalesced into at most
    /// one list refresh every 250 ms, and the list is synced in place so rows already on screen are
    /// never rebuilt.
    /// </remarks>
    public sealed partial class DevicesViewModel : ObservableObject
    {
        #region Variables

        /// <summary>
        /// Shortest gap between two list refreshes while devices stream in.
        /// </summary>
        private const int REFRESH_INTERVAL_MS = 250;

        private readonly DeviceManager _Manager;
        private readonly NetworkMonitor _Monitor;
        private readonly Dictionary<string, DeviceItem> _Items = [];

        private bool _IsRefreshQueued;
        private bool _HasScannedThisSession;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the view model.
        /// </summary>
        /// <param name="manager">The device manager.</param>
        /// <param name="monitor">The connection monitor, for whether there is a network to scan.</param>
        public DevicesViewModel(DeviceManager manager, NetworkMonitor monitor)
        {
            _Manager = manager;
            _Monitor = monitor;

            _Manager.Changed += (_, _) => QueueRefresh();

            // On another network the list starts over, so it scans that network straight away.
            _Manager.NetworkSwitched += (_, _) =>
            {
                if (_HasScannedThisSession && _Monitor.Current.IsConnected)
                {
                    _ = _Manager.ScanAsync();
                }
            };
            _Monitor.SnapshotChanged += (_, _) => QueueRefresh();
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised every time a row is opened, even the one already selected.
        /// </summary>
        public event EventHandler<DeviceItem>? DeviceOpened;

        #endregion

        #region Properties

        /// <summary>
        /// The rows that pass the filter and search, in sort order.
        /// </summary>
        public ObservableCollection<DeviceItem> Devices { get; } = [];

        /// <summary>
        /// Which devices to show.
        /// </summary>
        [ObservableProperty]
        public partial DeviceFilter Filter { get; set; }

        /// <summary>
        /// How to order them.
        /// </summary>
        [ObservableProperty]
        public partial DeviceSort Sort { get; set; }

        /// <summary>
        /// Text typed into search.
        /// </summary>
        [ObservableProperty]
        public partial string SearchText { get; set; } = string.Empty;

        /// <summary>
        /// The device shown in the details pane on wide windows.
        /// </summary>
        [ObservableProperty]
        public partial DeviceItem? SelectedDevice { get; set; }

        /// <summary>
        /// Whether the selected row is highlighted. Only true beside the details pane; on narrow
        /// windows the details open as their own page and a highlight would just linger.
        /// </summary>
        [ObservableProperty]
        public partial bool IsSelectionShown { get; set; }

        /// <summary>
        /// True while the sweep runs.
        /// </summary>
        [ObservableProperty]
        public partial bool IsScanning { get; set; }

        /// <summary>
        /// True while the sweep or the details lookups run.
        /// </summary>
        [ObservableProperty]
        public partial bool IsBusy { get; set; }

        /// <summary>
        /// The scan button's label: Scan, or Stop while scanning.
        /// </summary>
        [ObservableProperty]
        public partial string ScanButtonText { get; set; } = AppResources.DevicesScan;

        /// <summary>
        /// One line under the title: progress, or the last scan's summary.
        /// </summary>
        [ObservableProperty]
        public partial string Summary { get; set; } = AppResources.DevicesSummaryNoScan;

        /// <summary>
        /// The network's name, for the title area.
        /// </summary>
        [ObservableProperty]
        public partial string NetworkName { get; set; } = string.Empty;

        /// <summary>
        /// Whether there is a network to scan.
        /// </summary>
        [ObservableProperty]
        public partial bool IsConnected { get; set; } = true;

        /// <summary>
        /// No network and nothing remembered to show.
        /// </summary>
        [ObservableProperty]
        public partial bool IsOffline { get; set; }

        /// <summary>
        /// Placeholder rows while the very first results are on their way.
        /// </summary>
        [ObservableProperty]
        public partial bool ShowSkeleton { get; set; }

        /// <summary>
        /// Whether the list has rows to show. Hidden otherwise, so its footer note does not sit
        /// under the skeleton or the empty states.
        /// </summary>
        [ObservableProperty]
        public partial bool HasRows { get; set; }

        /// <summary>
        /// The invitation to scan, before any device is known.
        /// </summary>
        [ObservableProperty]
        public partial bool ShowEmpty { get; set; }

        /// <summary>
        /// Devices exist, but none pass the filter and search.
        /// </summary>
        [ObservableProperty]
        public partial bool ShowNoMatch { get; set; }

        /// <summary>
        /// Whether to ask if this network is the user's own, after its first scan.
        /// </summary>
        [ObservableProperty]
        public partial bool AskOwnership { get; set; }

        /// <summary>
        /// The question, e.g. "Is flyoderr_5 your network?".
        /// </summary>
        [ObservableProperty]
        public partial string OwnershipQuestion { get; set; } = string.Empty;

        /// <summary>
        /// "My network" or "Guest network", once the user has said.
        /// </summary>
        [ObservableProperty]
        public partial string OwnershipText { get; set; } = string.Empty;

        /// <summary>
        /// A house for the user's own network, a person for a guest network.
        /// </summary>
        [ObservableProperty]
        public partial string OwnershipIcon { get; set; } = MaterialIcon.HOME;

        /// <summary>
        /// The switch-over action: "Make guest" on the user's network, "Make mine" on a guest one.
        /// </summary>
        [ObservableProperty]
        public partial string OwnershipChangeText { get; set; } = string.Empty;

        /// <summary>
        /// Whether the user has said whose network this is.
        /// </summary>
        [ObservableProperty]
        public partial bool HasOwnership { get; set; }

        /// <summary>
        /// Whether there are devices to act on in bulk.
        /// </summary>
        [ObservableProperty]
        public partial bool HasUnknown { get; set; }

        /// <summary>
        /// The review banner heading, e.g. "5 devices to review".
        /// </summary>
        [ObservableProperty]
        public partial string ReviewTitle { get; set; } = string.Empty;

        /// <summary>
        /// Whether the banner offers "Review", which is pointless once the list shows only those.
        /// </summary>
        [ObservableProperty]
        public partial bool CanReview { get; set; }

        /// <summary>
        /// Filter chip captions with counts, e.g. "Unknown 3".
        /// </summary>
        [ObservableProperty]
        public partial string AllCaption { get; set; } = AppResources.FilterAll;

        /// <inheritdoc cref="AllCaption" />
        [ObservableProperty]
        public partial string OnlineCaption { get; set; } = AppResources.FilterOnline;

        /// <inheritdoc cref="AllCaption" />
        [ObservableProperty]
        public partial string UnknownCaption { get; set; } = AppResources.FilterUnknown;

        /// <inheritdoc cref="AllCaption" />
        [ObservableProperty]
        public partial string NewCaption { get; set; } = AppResources.FilterNew;

        /// <summary>
        /// Sort choices, in the order of <see cref="DeviceSort"/>.
        /// </summary>
        public IReadOnlyList<string> SortOptions { get; } =
            [AppResources.SortAddress, AppResources.SortName, AppResources.SortLastSeen];

        /// <summary>
        /// The picked sort, as an index into <see cref="SortOptions"/>.
        /// </summary>
        public int SortIndex
        {
            get => (int)Sort;
            set => Sort = (DeviceSort)Math.Clamp(value, 0, SortOptions.Count - 1);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Shows the remembered devices and scans once per session. Called when the screen appears.
        /// </summary>
        /// <returns>A task that completes once the scan has started.</returns>
        public async Task AppearAsync()
        {
            await _Manager.LoadCurrentNetworkAsync();
            Refresh();

            if (!_HasScannedThisSession && _Monitor.Current.IsConnected)
            {
                _HasScannedThisSession = true;
                _ = _Manager.ScanAsync();
            }
        }

        /// <summary>
        /// Starts a scan, or stops the one running.
        /// </summary>
        [RelayCommand]
        private void ScanOrStop()
        {
            if (_Manager.IsScanning)
            {
                _Manager.CancelScan();
                return;
            }

            _HasScannedThisSession = true;
            _ = _Manager.ScanAsync();
        }

        /// <summary>
        /// Picks a filter chip.
        /// </summary>
        /// <param name="filter">The filter's name, from the chip.</param>
        [RelayCommand]
        private void SetFilter(string filter)
        {
            Filter = Enum.Parse<DeviceFilter>(filter);
        }

        /// <summary>
        /// Marks every device on the network as known.
        /// </summary>
        /// <returns>A task that completes once saved.</returns>
        [RelayCommand]
        private Task MarkAllKnownAsync()
        {
            return _Manager.MarkAllKnownAsync();
        }

        /// <summary>
        /// Says this network is the user's own: backed up, and alerts when a device joins.
        /// </summary>
        /// <returns>A task that completes once saved.</returns>
        [RelayCommand]
        private Task MarkMineAsync()
        {
            return _Manager.SetMineAsync(true);
        }

        /// <summary>
        /// Says this is a guest network: not backed up, no alerts.
        /// </summary>
        /// <returns>A task that completes once saved.</returns>
        [RelayCommand]
        private Task MarkGuestAsync()
        {
            return _Manager.SetMineAsync(false);
        }

        /// <summary>
        /// Switches between "my network" and "guest network".
        /// </summary>
        /// <returns>A task that completes once saved.</returns>
        [RelayCommand]
        private Task ToggleOwnershipAsync()
        {
            return _Manager.SetMineAsync(_Manager.IsMine != true);
        }

        /// <summary>
        /// Opens a device: selects it for the wide pane and tells the page, which pushes the details
        /// page on narrow windows.
        /// </summary>
        /// <param name="item">The row.</param>
        private void OpenDevice(DeviceItem item)
        {
            SelectedDevice = item;
            DeviceOpened?.Invoke(this, item);
        }

        /// <summary>
        /// Flips a device between known and unknown.
        /// </summary>
        /// <param name="item">The row.</param>
        /// <returns>A task that completes once saved.</returns>
        private Task ToggleKnownAsync(DeviceItem item)
        {
            return _Manager.SetKnownAsync(item.Id, !item.IsKnown);
        }

        /// <summary>
        /// Copies a device's address.
        /// </summary>
        /// <param name="item">The row.</param>
        /// <returns>A task that completes once copied.</returns>
        private Task CopyAddressAsync(DeviceItem item)
        {
            return Clipboard.Default.SetTextAsync(item.Device.IpAddress);
        }

        /// <summary>
        /// Moves the selection highlight.
        /// </summary>
        /// <param name="oldValue">The previously selected row.</param>
        /// <param name="newValue">The newly selected row.</param>
        partial void OnSelectedDeviceChanged(DeviceItem? oldValue, DeviceItem? newValue)
        {
            if (oldValue is not null)
            {
                oldValue.IsSelected = false;
            }

            if (newValue is not null)
            {
                newValue.IsSelected = IsSelectionShown;
            }
        }

        /// <summary>
        /// Shows or hides the highlight on the selected row.
        /// </summary>
        /// <param name="value">Whether the pane is showing.</param>
        partial void OnIsSelectionShownChanged(bool value)
        {
            if (SelectedDevice is not null)
            {
                SelectedDevice.IsSelected = value;
            }
        }

        /// <summary>
        /// Re-filters when the filter, sort or search changes.
        /// </summary>
        /// <param name="value">The new filter.</param>
        partial void OnFilterChanged(DeviceFilter value)
        {
            Refresh();
        }

        /// <summary>
        /// Re-sorts when the sort changes.
        /// </summary>
        /// <param name="value">The new sort.</param>
        partial void OnSortChanged(DeviceSort value)
        {
            OnPropertyChanged(nameof(SortIndex));
            Refresh();
        }

        /// <summary>
        /// Re-filters as the user types.
        /// </summary>
        /// <param name="value">The new search text.</param>
        partial void OnSearchTextChanged(string value)
        {
            Refresh();
        }

        /// <summary>
        /// Schedules one refresh for every burst of changes, on the UI thread.
        /// </summary>
        private void QueueRefresh()
        {
            lock (_Items)
            {
                if (_IsRefreshQueued)
                {
                    return;
                }

                _IsRefreshQueued = true;
            }

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(REFRESH_INTERVAL_MS);

                lock (_Items)
                {
                    _IsRefreshQueued = false;
                }

                Refresh();
            });
        }

        /// <summary>
        /// Brings the rows, counts and states up to date with the manager. UI thread only.
        /// </summary>
        private void Refresh()
        {
            List<NetworkDevice> devices = _Manager.GetDevices();
            HashSet<string> ids = devices.Select(device => device.Id).ToHashSet();

            foreach (NetworkDevice device in devices)
            {
                if (_Items.TryGetValue(device.Id, out DeviceItem? item))
                {
                    item.Update(device);
                }
                else
                {
                    _Items[device.Id] = new DeviceItem(device, OpenDevice, ToggleKnownAsync, CopyAddressAsync);
                }
            }

            foreach (string gone in _Items.Keys.Where(id => !ids.Contains(id)).ToList())
            {
                _Items.Remove(gone);
            }

            List<DeviceItem> visible = Order(_Items.Values.Where(Passes)).ToList();

            Sync(visible);
            ApplyCounts(devices);
            ApplyState(devices.Count, visible.Count);

            if ((SelectedDevice is not null) && !_Items.ContainsKey(SelectedDevice.Id))
            {
                SelectedDevice = null;
            }
        }

        /// <summary>
        /// Whether a row passes the filter and search.
        /// </summary>
        /// <param name="item">The row.</param>
        /// <returns>True to show it.</returns>
        private bool Passes(DeviceItem item)
        {
            bool isInFilter = Filter switch
            {
                DeviceFilter.Online => item.IsOnline,
                DeviceFilter.Unknown => item.IsUnknown,
                DeviceFilter.New => item.IsNew,
                _ => true,
            };

            if (!isInFilter)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return true;
            }

            string query = SearchText.Trim();
            NetworkDevice device = item.Device;

            return new[] { item.Name, device.IpAddress, device.HostName, device.Manufacturer, device.ModelName, device.Vendor, device.MacAddress }
                .Any(field => field?.Contains(query, StringComparison.OrdinalIgnoreCase) == true);
        }

        /// <summary>
        /// Orders rows by the chosen sort, with this device and the router always first.
        /// </summary>
        /// <param name="items">The rows to order.</param>
        /// <returns>The ordered rows.</returns>
        private IEnumerable<DeviceItem> Order(IEnumerable<DeviceItem> items)
        {
            IOrderedEnumerable<DeviceItem> pinned = items
                .OrderByDescending(item => item.Device.IsGateway)
                .ThenByDescending(item => item.Device.IsThisDevice);

            return Sort switch
            {
                DeviceSort.Name => pinned.ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase),
                DeviceSort.LastSeen => pinned.ThenByDescending(item => item.Device.LastSeen),
                _ => pinned.ThenBy(item => AddressOrder(item.Device.IpAddress)),
            };
        }

        /// <summary>
        /// Numeric order for an IPv4 address, so .9 comes before .10.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <returns>A sortable number.</returns>
        private static uint AddressOrder(string address)
        {
            return IPAddress.TryParse(address, out IPAddress? parsed) && (parsed.GetAddressBytes() is { Length: 4 } bytes)
                ? System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes)
                : uint.MaxValue;
        }

        /// <summary>
        /// Makes the visible list match the wanted order with the fewest moves, so rows on screen
        /// keep their state and the list does not jump.
        /// </summary>
        /// <param name="wanted">The rows in display order.</param>
        private void Sync(List<DeviceItem> wanted)
        {
            HashSet<DeviceItem> keep = [.. wanted];

            for (int i = Devices.Count - 1; i >= 0; i--)
            {
                if (!keep.Contains(Devices[i]))
                {
                    Devices.RemoveAt(i);
                }
            }

            for (var i = 0; i < wanted.Count; i++)
            {
                if ((i < Devices.Count) && ReferenceEquals(Devices[i], wanted[i]))
                {
                    continue;
                }

                int current = Devices.IndexOf(wanted[i]);

                if (current >= 0)
                {
                    Devices.Move(current, i);
                }
                else
                {
                    Devices.Insert(i, wanted[i]);
                }
            }
        }

        /// <summary>
        /// Updates the filter captions and the summary line.
        /// </summary>
        /// <param name="devices">Every device on the network.</param>
        private void ApplyCounts(List<NetworkDevice> devices)
        {
            int online = devices.Count(device => device.IsOnline);
            int unknown = _Items.Values.Count(item => item.IsUnknown);
            int fresh = devices.Count(device => device.IsNew);

            AllCaption = Caption(AppResources.FilterAll, devices.Count);
            OnlineCaption = Caption(AppResources.FilterOnline, online);
            UnknownCaption = Caption(AppResources.FilterUnknown, unknown);
            NewCaption = Caption(AppResources.FilterNew, fresh);
            HasUnknown = unknown > 0;
            CanReview = HasUnknown && (Filter != DeviceFilter.Unknown);
            ReviewTitle = unknown == 1
                ? AppResources.DevicesReviewOneTitle
                : NetworkFormat.Text(AppResources.DevicesReviewTitleFormat, unknown);

            IsScanning = _Manager.IsScanning;
            IsBusy = _Manager.IsScanning || _Manager.IsIdentifying;
            ScanButtonText = _Manager.IsScanning ? AppResources.DevicesStop : AppResources.DevicesScan;
            NetworkName = _Manager.NetworkName ?? _Monitor.Current.Ssid ?? string.Empty;

            bool? isMine = _Manager.IsMine;

            AskOwnership = (isMine is null) && (_Manager.LastScan is not null);
            HasOwnership = isMine is not null;
            OwnershipText = isMine == true ? AppResources.OwnershipMine : AppResources.OwnershipGuest;
            OwnershipIcon = isMine == true ? MaterialIcon.HOME : MaterialIcon.GUEST;
            OwnershipChangeText = isMine == true ? AppResources.OwnershipMakeGuest : AppResources.OwnershipMakeMine;
            OwnershipQuestion = NetworkFormat.Text(AppResources.OwnershipQuestionFormat, NetworkName);

            Summary = _Manager.IsScanning ? NetworkFormat.Text(AppResources.DevicesScanningFormat, online)
                : _Manager.IsIdentifying ? AppResources.DevicesIdentifying
                : _Manager.LastScan is DateTimeOffset last
                    ? NetworkFormat.Text(AppResources.DevicesSummaryFormat, NetworkName, online, last.LocalDateTime)
                    : AppResources.DevicesSummaryNoScan;
        }

        /// <summary>
        /// Picks which of the list, skeleton and empty states shows.
        /// </summary>
        /// <param name="total">Devices on the network.</param>
        /// <param name="visible">Devices passing the filter.</param>
        private void ApplyState(int total, int visible)
        {
            IsConnected = _Monitor.Current.IsConnected || (total > 0);
            IsOffline = !IsConnected;
            ShowSkeleton = IsConnected && (total == 0) && _Manager.IsScanning;
            ShowEmpty = IsConnected && (total == 0) && !_Manager.IsScanning;
            ShowNoMatch = (total > 0) && (visible == 0);
            HasRows = visible > 0;
        }

        /// <summary>
        /// A filter caption with its count, e.g. "Unknown 3".
        /// </summary>
        /// <param name="label">The filter's name.</param>
        /// <param name="count">How many devices it matches.</param>
        /// <returns>The caption.</returns>
        private static string Caption(string label, int count)
        {
            return count > 0 ? $"{label} {count}" : label;
        }

        #endregion
    }
}
