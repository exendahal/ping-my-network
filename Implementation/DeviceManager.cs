using MauiWifiManager;
using MauiWifiManager.Abstractions;
using PingMyNetwork.Helper.Enums;
using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.Models;

namespace PingMyNetwork.Implementation
{
    /// <summary>
    /// The devices on the current network: scanning for them, recognising them from earlier scans,
    /// learning more about each one, and remembering what the user said about them.
    /// </summary>
    /// <remarks>
    /// A scan runs in two passes. The sweep reports each device as it answers, so the list fills in
    /// as it goes. Then each device seen for the first time gets one details lookup (names, MAC,
    /// model, open ports, about 3 s each); devices already looked up are not probed again on every
    /// scan.
    ///
    /// Everything here runs off the UI thread and raises <see cref="Changed"/> from wherever it is.
    /// Screens copy what they need with <see cref="GetDevices"/> and coalesce updates themselves.
    /// </remarks>
    public sealed class DeviceManager
    {
        #region Variables

        /// <summary>
        /// Details lookups run at once. Each opens a handful of connections to the device, so a few
        /// at a time keeps the network and slow IoT devices from being flooded.
        /// </summary>
        private const int DETAILS_CONCURRENCY = 3;

        private readonly IWifiNetworkService _Wifi;
        private readonly NetworkMonitor _Monitor;
        private readonly DeviceStore _Store;
        private readonly object _Gate = new();
        private readonly SemaphoreSlim _LoadGate = new(1, 1);

        /// <summary>
        /// How long the mDNS pass listens for answers to each service type.
        /// </summary>
        private static readonly TimeSpan MDNS_LISTEN = TimeSpan.FromSeconds(3);

        /// <summary>
        /// Services that phones, computers, TVs, speakers and printers commonly advertise. Their
        /// instance names ("Anna's iPhone", "Living Room TV") are the friendliest names a device gives.
        /// </summary>
        private static readonly string[] MDNS_TYPES =
        [
            "_companion-link._tcp",
            "_device-info._tcp",
            "_apple-mobdev2._tcp",
            "_airplay._tcp",
            "_raop._tcp",
            "_googlecast._tcp",
            "_spotify-connect._tcp",
            "_hap._tcp",
            "_ipp._tcp",
            "_printer._tcp",
            "_smb._tcp",
            "_workstation._tcp",
            "_amzn-wplay._tcp",
        ];

        private List<NetworkRecord>? _Networks;
        private NetworkRecord? _Network;
        private string? _NetworkKey;
        private CancellationTokenSource? _ScanCancel;
        private Task _ScanTask = Task.CompletedTask;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the manager.
        /// </summary>
        /// <param name="wifi">The Wi-Fi manager, for discovery and probing.</param>
        /// <param name="monitor">The connection monitor, for which network this is.</param>
        /// <param name="store">Where devices are remembered.</param>
        public DeviceManager(IWifiNetworkService wifi, NetworkMonitor monitor, DeviceStore store)
        {
            _Wifi = wifi;
            _Monitor = monitor;
            _Store = store;

            _Monitor.SnapshotChanged += OnSnapshotChanged;
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised, on any thread, when a device or the scan state changes.
        /// </summary>
        public event EventHandler? Changed;

        /// <summary>
        /// Raised, on any thread, after the phone moved to another network and the list now shows
        /// that network's devices.
        /// </summary>
        public event EventHandler? NetworkSwitched;

        /// <summary>
        /// Raised, on any thread, for each device that joined a network scanned before and that the
        /// user has not marked as known.
        /// </summary>
        public event EventHandler<NetworkDevice>? NewDeviceFound;

        #endregion

        #region Properties

        /// <summary>
        /// True while the sweep is running.
        /// </summary>
        public bool IsScanning { get; private set; }

        /// <summary>
        /// True while devices found by the sweep are being looked up in detail.
        /// </summary>
        public bool IsIdentifying { get; private set; }

        /// <summary>
        /// The network the devices belong to, or null before the first scan or when offline.
        /// </summary>
        public string? NetworkName
        {
            get
            {
                lock (_Gate)
                {
                    return _Network?.Name;
                }
            }
        }

        /// <summary>
        /// When the current network was last scanned.
        /// </summary>
        public DateTimeOffset? LastScan
        {
            get
            {
                lock (_Gate)
                {
                    return _Network?.LastScan;
                }
            }
        }

        /// <summary>
        /// Why the last scan could not run, or null.
        /// </summary>
        public WifiErrorCodes? LastError { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// Copies of the current network's devices.
        /// </summary>
        /// <returns>The devices; empty when there is no current network.</returns>
        public List<NetworkDevice> GetDevices()
        {
            lock (_Gate)
            {
                return _Network?.Devices.Select(device => device.Clone()).ToList() ?? [];
            }
        }

        /// <summary>
        /// A copy of one device.
        /// </summary>
        /// <param name="id">The device's id.</param>
        /// <returns>The copy, or null when it is not on the current network.</returns>
        public NetworkDevice? GetDevice(string id)
        {
            lock (_Gate)
            {
                return _Network?.Devices.FirstOrDefault(device => device.Id == id)?.Clone();
            }
        }

        /// <summary>
        /// Shows the current network's remembered devices without scanning, so the list is not
        /// empty while the first scan of a session starts.
        /// </summary>
        /// <returns>A task that completes once loaded.</returns>
        public async Task LoadCurrentNetworkAsync()
        {
            await EnsureLoadedAsync();

            // Makers are re-derived from the stored MACs, so a newer registry in a library update
            // reaches remembered devices too. Off the UI thread: the first lookup reads the registry.
            await Task.Run(RefreshVendors);

            SwitchTo(_Monitor.Current);
            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Scans the current network. Does nothing when a scan is already running.
        /// </summary>
        /// <returns>A task that completes when the scan and the details lookups are done.</returns>
        public Task ScanAsync()
        {
            if (IsScanning)
            {
                return _ScanTask;
            }

            return _ScanTask = ScanCoreAsync();
        }

        /// <summary>
        /// Runs one scan: the sweep, then mDNS names, then details for devices seen for the first time.
        /// </summary>
        /// <returns>A task that completes when the scan and the details lookups are done.</returns>
        private async Task ScanCoreAsync()
        {
            await EnsureLoadedAsync();

            NetworkSnapshot network = _Monitor.Current;

            if ((NetworkKey(network) is not string key) || (network.IpAddress is null))
            {
                LastError = WifiErrorCodes.NoConnection;
                Changed?.Invoke(this, EventArgs.Empty);
                return;
            }

            CancellationTokenSource cancel = new();
            NetworkRecord record = BeginScan(key, network);

            _ScanCancel = cancel;
            LastError = null;
            IsScanning = true;
            Changed?.Invoke(this, EventArgs.Empty);

            try
            {
                WifiManagerResponse<List<LanDevice>> result = await _Wifi.DiscoverDevicesAsync(
                    new SweepProgress(device => Merge(device, network, record)),
                    cancel.Token);

                if (result.ErrorCode != WifiErrorCodes.Success)
                {
                    LastError = result.ErrorCode;
                }

                bool isSelfFound;

                lock (_Gate)
                {
                    isSelfFound = record.Devices.Any(device => device.IsOnline && (device.IpAddress == network.IpAddress));
                }

                // This device does not always answer its own ping, but it is certainly on the network.
                if (!isSelfFound)
                {
                    Merge(new LanDevice { IpAddress = network.IpAddress }, network, record);
                }

                lock (_Gate)
                {
                    record.ScanCount++;
                    record.LastScan = DateTimeOffset.Now;
                }
            }
            finally
            {
                IsScanning = false;
                Changed?.Invoke(this, EventArgs.Empty);
            }

            await NameFromMdnsAsync(record, cancel.Token);
            await SaveAsync();
            await IdentifyNewDevicesAsync(cancel.Token);
            AnnounceNewDevices(record);
        }

        /// <summary>
        /// Stops a running scan. Devices found so far stay in the list.
        /// </summary>
        public void CancelScan()
        {
            _ScanCancel?.Cancel();
        }

        /// <summary>
        /// Looks one device up in full (names, MAC, model, ports, web page, ping) and remembers it.
        /// </summary>
        /// <param name="id">The device's id.</param>
        /// <param name="token">Cancels the lookup.</param>
        /// <returns>The ping result from the lookup, or null.</returns>
        public async Task<PingStatistics?> RefreshDetailsAsync(string id, CancellationToken token)
        {
            string? address = GetDevice(id)?.IpAddress;

            if (address is null)
            {
                return null;
            }

            WifiManagerResponse<LanDeviceDetails> details = await _Wifi.GetDeviceDetailsAsync(address, token);

            if ((details.ErrorCode != WifiErrorCodes.Success) || (details.Data is null))
            {
                return null;
            }

            Apply(id, details.Data);
            await SaveAsync();

            return details.Data.Ping;
        }

        /// <summary>
        /// Pings one device.
        /// </summary>
        /// <param name="id">The device's id.</param>
        /// <param name="token">Cancels the ping.</param>
        /// <returns>The result, or null when the device is unknown or the ping failed to run.</returns>
        public async Task<PingStatistics?> PingAsync(string id, CancellationToken token)
        {
            string? address = GetDevice(id)?.IpAddress;

            return address is null ? null : (await _Wifi.PingAsync(address, 4, token)).Data;
        }

        /// <summary>
        /// Records whether the user recognises a device.
        /// </summary>
        /// <param name="id">The device's id.</param>
        /// <param name="isKnown">True to mark it known.</param>
        /// <returns>A task that completes once saved.</returns>
        public Task SetKnownAsync(string id, bool isKnown)
        {
            return EditAsync(id, device => device.IsKnown = isKnown);
        }

        /// <summary>
        /// Marks every device on the current network as known.
        /// </summary>
        /// <returns>A task that completes once saved.</returns>
        public async Task MarkAllKnownAsync()
        {
            lock (_Gate)
            {
                foreach (NetworkDevice device in _Network?.Devices ?? [])
                {
                    device.IsKnown = true;
                }
            }

            Changed?.Invoke(this, EventArgs.Empty);
            await SaveAsync();
        }

        /// <summary>
        /// Gives a device the user's own name, or clears it with an empty name.
        /// </summary>
        /// <param name="id">The device's id.</param>
        /// <param name="name">The new name.</param>
        /// <returns>A task that completes once saved.</returns>
        public Task RenameAsync(string id, string? name)
        {
            return EditAsync(id, device => device.CustomName = string.IsNullOrWhiteSpace(name) ? null : name.Trim());
        }

        /// <summary>
        /// Sets what kind of device it is, overriding the guess for good.
        /// </summary>
        /// <param name="id">The device's id.</param>
        /// <param name="type">The kind of device.</param>
        /// <returns>A task that completes once saved.</returns>
        public Task SetTypeAsync(string id, DeviceKind type)
        {
            return EditAsync(id, device =>
            {
                device.Type = type;
                device.IsTypeSetByUser = true;
            });
        }

        /// <summary>
        /// Forgets a device. If it is still on the network, the next scan finds it as new.
        /// </summary>
        /// <param name="id">The device's id.</param>
        /// <returns>A task that completes once saved.</returns>
        public async Task ForgetAsync(string id)
        {
            lock (_Gate)
            {
                _Network?.Devices.RemoveAll(device => device.Id == id);
            }

            Changed?.Invoke(this, EventArgs.Empty);
            await SaveAsync();
        }

        /// <summary>
        /// Identifies a network across visits: its name plus its router's address. The router's
        /// MAC would be stronger, but Windows does not report the access point's.
        /// </summary>
        /// <param name="network">The connection.</param>
        /// <returns>The key, or null when not connected to a network with a router.</returns>
        private static string? NetworkKey(NetworkSnapshot network)
        {
            if (!network.IsConnected || (network.Gateway is null))
            {
                return null;
            }

            return $"{network.Ssid ?? network.ConnectionType.ToString()}|{network.Gateway}";
        }

        /// <summary>
        /// Moves the list to the new network when the phone changes networks, so one network's
        /// devices are never shown, or shown as online, while on another.
        /// </summary>
        /// <param name="sender">The monitor.</param>
        /// <param name="snapshot">The new connection.</param>
        private async void OnSnapshotChanged(object? sender, NetworkSnapshot snapshot)
        {
            string? key = NetworkKey(snapshot);

            lock (_Gate)
            {
                // Not loaded yet: the first screen to appear loads the right network.
                if ((_Networks is null) || (key == _NetworkKey))
                {
                    return;
                }
            }

            // A sweep of the old network would only find nothing, slowly.
            _ScanCancel?.Cancel();

            try
            {
                await _ScanTask;
            }
            catch (OperationCanceledException)
            {
                // Expected; the old scan stopped.
            }

            if (SwitchTo(_Monitor.Current))
            {
                Changed?.Invoke(this, EventArgs.Empty);
                NetworkSwitched?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Shows the remembered devices of the network the phone is on.
        /// </summary>
        /// <param name="network">The connection.</param>
        /// <returns>True when that is a different network from the one shown before.</returns>
        private bool SwitchTo(NetworkSnapshot network)
        {
            string? key = NetworkKey(network);

            lock (_Gate)
            {
                bool isSwitch = key != _NetworkKey;

                _NetworkKey = key;
                _Network = key is null ? null : _Networks!.FirstOrDefault(record => record.Key == key);

                foreach (NetworkDevice device in _Network?.Devices ?? [])
                {
                    // What a scan learned earlier this session belongs to that visit, not this one.
                    if (isSwitch)
                    {
                        device.IsOnline = false;
                        device.IsNew = false;
                        device.RoundTripMs = null;
                    }

                    // Roles are not saved, since addresses change; mark them from today's connection
                    // so the router and this device are tagged before the scan confirms them.
                    device.IsThisDevice = device.IpAddress == network.IpAddress;
                    device.IsGateway = device.IpAddress == network.Gateway;
                }

                return isSwitch;
            }
        }

        /// <summary>
        /// Asks the network which devices advertise common services over mDNS, and uses the names
        /// they give themselves. Phones and computers rarely answer NetBIOS or reverse DNS, but
        /// Apple devices, TVs, speakers and printers do advertise this way.
        /// </summary>
        /// <param name="record">The network being scanned.</param>
        /// <param name="token">Cancels the lookups.</param>
        /// <returns>A task that completes once every service type has been asked.</returns>
        private async Task NameFromMdnsAsync(NetworkRecord record, CancellationToken token)
        {
            List<NetworkServiceInfo> services = [];

            try
            {
                WifiManagerResponse<List<NetworkServiceInfo>>[] answers = await Task.WhenAll(
                    MDNS_TYPES.Select(type => _Wifi.BrowseServicesAsync(type, MDNS_LISTEN, token)));

                services = answers.SelectMany(answer => answer.Data ?? []).ToList();
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (services.Count == 0)
            {
                return;
            }

            lock (_Gate)
            {
                foreach (NetworkServiceInfo service in services.Where(service => service.IpAddress is not null))
                {
                    if (record.Devices.FirstOrDefault(device => device.IpAddress == service.IpAddress) is not NetworkDevice device)
                    {
                        continue;
                    }

                    // Cast devices put their real name in "fn"; the instance name is a serial number.
                    device.MdnsName ??= service.Properties.GetValueOrDefault("fn")
                        ?? DeviceDescriber.CleanServiceName(service.Name);
                    device.HostName ??= service.HostName?.TrimEnd('.');
                    device.MdnsModel ??= service.Properties.GetValueOrDefault("model")
                        ?? service.Properties.GetValueOrDefault("md");

                    if (!device.IsTypeSetByUser)
                    {
                        device.Type = DeviceDescriber.GuessType(device);
                    }
                }
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Tells listeners about each device that is new on this network and not known yet.
        /// </summary>
        /// <param name="record">The network just scanned.</param>
        private void AnnounceNewDevices(NetworkRecord record)
        {
            List<NetworkDevice> fresh;

            lock (_Gate)
            {
                fresh = record.Devices
                    .Where(device => device.IsNew && !device.IsKnown && !device.IsThisDevice)
                    .Select(device => device.Clone())
                    .ToList();
            }

            foreach (NetworkDevice device in fresh)
            {
                NewDeviceFound?.Invoke(this, device);
            }
        }

        /// <summary>
        /// Looks up the maker of every remembered device that has a MAC.
        /// </summary>
        private void RefreshVendors()
        {
            lock (_Gate)
            {
                foreach (NetworkDevice device in _Networks!.SelectMany(network => network.Devices))
                {
                    if (device.MacAddress is not null)
                    {
                        device.Vendor = MacVendor.Lookup(device.MacAddress);

                        if (!device.IsTypeSetByUser)
                        {
                            device.Type = DeviceDescriber.GuessType(device);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Picks or creates the record for this network and marks every device as not yet seen.
        /// </summary>
        /// <param name="key">The network's key.</param>
        /// <param name="network">The connection.</param>
        /// <returns>The record being scanned.</returns>
        private NetworkRecord BeginScan(string key, NetworkSnapshot network)
        {
            lock (_Gate)
            {
                NetworkRecord? record = _Networks!.FirstOrDefault(candidate => candidate.Key == key);

                if (record is null)
                {
                    record = new NetworkRecord { Key = key };
                    _Networks!.Add(record);
                }

                record.Name = network.Ssid ?? NetworkFormat.ConnectionType(network.ConnectionType);
                _NetworkKey = key;

                foreach (NetworkDevice device in record.Devices)
                {
                    device.IsOnline = false;
                    device.IsNew = false;
                    device.RoundTripMs = null;
                }

                _Network = record;

                return record;
            }
        }

        /// <summary>
        /// Adds a device that answered, or updates the remembered device it turns out to be.
        /// </summary>
        /// <param name="answer">What the sweep reported.</param>
        /// <param name="network">The connection, for spotting this device and the router.</param>
        /// <param name="record">The network being scanned. Answers arriving after a switch to
        /// another network still land here, never in the new network's list.</param>
        private void Merge(LanDevice answer, NetworkSnapshot network, NetworkRecord record)
        {
            NetworkDevice sighting = new() { IpAddress = answer.IpAddress, HostName = answer.HostName };
            DateTimeOffset now = DateTimeOffset.Now;

            lock (_Gate)
            {
                NetworkDevice? device = record.Devices.FirstOrDefault(known => DeviceIdentity.Matches(known, sighting));

                if (device is null)
                {
                    device = new NetworkDevice
                    {
                        FirstSeen = now,

                        // The first scan of a network is the baseline: nothing on it is "new".
                        IsNew = record.ScanCount > 0,
                    };
                    record.Devices.Add(device);
                }

                device.IpAddress = answer.IpAddress;
                device.HostName = answer.HostName ?? device.HostName;
                device.LastSeen = now;
                device.IsOnline = true;
                device.RoundTripMs = answer.RoundTripMs > 0 ? answer.RoundTripMs : device.RoundTripMs;
                device.IsThisDevice = answer.IpAddress == network.IpAddress;
                device.IsGateway = answer.IpAddress == network.Gateway;

                if (device.IsThisDevice)
                {
                    device.FriendlyName ??= DeviceInfo.Current.Name;
                }

                // Your own router and this device are known by definition; asking would be noise.
                if (device.IsGateway || device.IsThisDevice)
                {
                    device.IsKnown = true;
                }

                if (!device.IsTypeSetByUser)
                {
                    device.Type = DeviceDescriber.GuessType(device);
                }
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Looks up, a few at a time, every online device that has never been looked up.
        /// </summary>
        /// <param name="token">Cancels the lookups.</param>
        /// <returns>A task that completes when all lookups are done.</returns>
        private async Task IdentifyNewDevicesAsync(CancellationToken token)
        {
            List<string> pending;

            lock (_Gate)
            {
                pending = _Network?.Devices
                    .Where(device => device.IsOnline && (device.DetailsFetchedAt is null))
                    .Select(device => device.Id)
                    .ToList() ?? [];
            }

            if (pending.Count == 0)
            {
                return;
            }

            using SemaphoreSlim gate = new(DETAILS_CONCURRENCY);

            IsIdentifying = true;
            Changed?.Invoke(this, EventArgs.Empty);

            try
            {
                await Task.WhenAll(pending.Select(async id =>
                {
                    await gate.WaitAsync(token);

                    try
                    {
                        await RefreshDetailsAsync(id, token);
                    }
                    finally
                    {
                        gate.Release();
                    }
                }));
            }
            catch (OperationCanceledException)
            {
                // Cancelled with the scan; what was learned so far is kept.
            }
            finally
            {
                IsIdentifying = false;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Copies what a details lookup learned onto a device, and folds it into an older record of
        /// the same device when its MAC shows they are one and the same.
        /// </summary>
        /// <param name="id">The device's id.</param>
        /// <param name="details">The lookup result.</param>
        private void Apply(string id, LanDeviceDetails details)
        {
            lock (_Gate)
            {
                NetworkDevice? device = _Network?.Devices.FirstOrDefault(candidate => candidate.Id == id);

                if (device is null)
                {
                    return;
                }

                device.HostName ??= details.HostName;
                device.NetBiosName = details.NetBiosName ?? device.NetBiosName;
                device.MacAddress = details.MacAddress?.ToUpperInvariant() ?? device.MacAddress;
                device.IsRandomizedMac = details.IsRandomizedMac ?? device.IsRandomizedMac;
                device.Vendor = details.Vendor ?? device.Vendor;
                device.FriendlyName = details.FriendlyName ?? device.FriendlyName;
                device.Manufacturer = details.Manufacturer ?? device.Manufacturer;
                device.ModelName = details.ModelName ?? device.ModelName;
                device.HttpTitle = DeviceDescriber.UsefulTitle(details.HttpTitle) ?? device.HttpTitle;
                device.OpenPorts = [.. details.OpenPorts];
                device.DetailsFetchedAt = DateTimeOffset.Now;

                if (details.Ping is { Received: > 0, AverageMs: double average })
                {
                    device.RoundTripMs = (long)Math.Round(average);
                }

                if (!device.IsTypeSetByUser)
                {
                    device.Type = DeviceDescriber.GuessType(device);
                }

                FoldDuplicate(device);
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// When a newly learned MAC shows an older record is the same device (it came back on a new
        /// address with no name), keeps the older record with today's details and drops the new one,
        /// so the user's name and Known status carry over. Caller holds the lock.
        /// </summary>
        /// <param name="device">The device that was just looked up.</param>
        private void FoldDuplicate(NetworkDevice device)
        {
            if ((device.MacAddress is null) || (_Network is null))
            {
                return;
            }

            NetworkDevice? older = _Network.Devices.FirstOrDefault(other => (other != device)
                && string.Equals(other.MacAddress, device.MacAddress, StringComparison.OrdinalIgnoreCase));

            if (older is null)
            {
                return;
            }

            older.IpAddress = device.IpAddress;
            older.HostName = device.HostName ?? older.HostName;
            older.NetBiosName = device.NetBiosName ?? older.NetBiosName;
            older.FriendlyName = device.FriendlyName ?? older.FriendlyName;
            older.MdnsName = device.MdnsName ?? older.MdnsName;
            older.MdnsModel = device.MdnsModel ?? older.MdnsModel;
            older.Manufacturer = device.Manufacturer ?? older.Manufacturer;
            older.Vendor = device.Vendor ?? older.Vendor;
            older.ModelName = device.ModelName ?? older.ModelName;
            older.HttpTitle = device.HttpTitle ?? older.HttpTitle;
            older.OpenPorts = device.OpenPorts;
            older.IsRandomizedMac = device.IsRandomizedMac;
            older.DetailsFetchedAt = device.DetailsFetchedAt;
            older.LastSeen = device.LastSeen;
            older.IsOnline = device.IsOnline;
            older.RoundTripMs = device.RoundTripMs;
            older.IsThisDevice = device.IsThisDevice;
            older.IsGateway = device.IsGateway;
            older.IsNew = false;

            _Network.Devices.Remove(device);
        }

        /// <summary>
        /// Applies one user edit and saves.
        /// </summary>
        /// <param name="id">The device's id.</param>
        /// <param name="edit">The change.</param>
        /// <returns>A task that completes once saved.</returns>
        private async Task EditAsync(string id, Action<NetworkDevice> edit)
        {
            lock (_Gate)
            {
                if (_Network?.Devices.FirstOrDefault(device => device.Id == id) is not NetworkDevice device)
                {
                    return;
                }

                edit(device);
            }

            Changed?.Invoke(this, EventArgs.Empty);
            await SaveAsync();
        }

        /// <summary>
        /// Reads the store once per session.
        /// </summary>
        /// <returns>A task that completes once loaded.</returns>
        private async Task EnsureLoadedAsync()
        {
            if (_Networks is not null)
            {
                return;
            }

            await _LoadGate.WaitAsync();

            try
            {
                _Networks ??= await _Store.LoadAsync();
            }
            finally
            {
                _LoadGate.Release();
            }
        }

        /// <summary>
        /// Saves a consistent copy of every network, taken under the lock and written outside it.
        /// </summary>
        /// <returns>A task that completes once written.</returns>
        private Task SaveAsync()
        {
            byte[] json;

            lock (_Gate)
            {
                if (_Networks is null)
                {
                    return Task.CompletedTask;
                }

                json = _Store.Serialize(_Networks);
            }

            return _Store.SaveAsync(json);
        }

        #endregion

        #region Classes

        /// <summary>
        /// Hands each answer straight to the manager on the sweep's own thread. Progress&lt;T&gt;
        /// would post every answer to the UI thread first, which is the work this avoids.
        /// </summary>
        /// <param name="report">What to do with each answer.</param>
        private sealed class SweepProgress(Action<LanDevice> report) : IProgress<LanDevice>
        {
            /// <summary>
            /// Passes one answer on.
            /// </summary>
            /// <param name="value">The device that answered.</param>
            public void Report(LanDevice value)
            {
                report(value);
            }
        }

        #endregion
    }
}
