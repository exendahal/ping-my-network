using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiWifiManager.Abstractions;
using PingMyNetwork.Helper.Enums;
using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.Implementation;
using PingMyNetwork.Models;
using PingMyNetwork.Resources.Strings;

namespace PingMyNetwork.ViewModels
{
    /// <summary>
    /// One device in full: what it is, what it reveals about itself, and what the user says about it.
    /// </summary>
    /// <remarks>
    /// Used by the pushed details page on narrow windows and by the pane beside the list on wide
    /// ones. Opening a device shows what is remembered straight away and refreshes it with a fresh
    /// lookup, which takes a few seconds.
    /// </remarks>
    public sealed partial class DeviceDetailsViewModel : ObservableObject
    {
        #region Variables

        /// <summary>
        /// How long the Forget button waits for its confirming second tap.
        /// </summary>
        private const int FORGET_CONFIRM_MS = 3000;

        private readonly DeviceManager _Manager;

        private CancellationTokenSource? _LookupCancel;
        private CancellationTokenSource? _ForgetCancel;
        private bool _IsAttached;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the view model.
        /// </summary>
        /// <param name="manager">The device manager.</param>
        public DeviceDetailsViewModel(DeviceManager manager)
        {
            _Manager = manager;
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised after the device is forgotten, so the hosting page can close or clear itself.
        /// </summary>
        public event EventHandler? Forgotten;

        #endregion

        #region Properties

        /// <summary>
        /// The device's id, or null when nothing is selected.
        /// </summary>
        [ObservableProperty]
        public partial string? DeviceId { get; set; }

        /// <summary>
        /// Whether a device is showing.
        /// </summary>
        [ObservableProperty]
        public partial bool HasDevice { get; set; }

        /// <summary>
        /// What to call it.
        /// </summary>
        [ObservableProperty]
        public partial string Name { get; set; } = string.Empty;

        /// <summary>
        /// Address and kind.
        /// </summary>
        [ObservableProperty]
        public partial string Subtitle { get; set; } = string.Empty;

        /// <summary>
        /// Icon for its kind.
        /// </summary>
        [ObservableProperty]
        public partial string Icon { get; set; } = MaterialIcon.DEVICE_UNKNOWN;

        /// <summary>
        /// The name being edited; saved when editing finishes.
        /// </summary>
        [ObservableProperty]
        public partial string EditableName { get; set; } = string.Empty;

        /// <summary>
        /// The kind of device, as an index into <see cref="TypeOptions"/>.
        /// </summary>
        [ObservableProperty]
        public partial int TypeIndex { get; set; }

        /// <summary>
        /// Every kind of device, in the order of <see cref="DeviceKind"/>.
        /// </summary>
        public IReadOnlyList<string> TypeOptions { get; } =
            Enum.GetValues<DeviceKind>().Select(DeviceDescriber.TypeName).ToList();

        /// <summary>
        /// Whether the user has marked it known.
        /// </summary>
        [ObservableProperty]
        public partial bool IsKnown { get; set; }

        /// <summary>
        /// Whether the known status can change. This device and the router are always known.
        /// </summary>
        [ObservableProperty]
        public partial bool CanToggleKnown { get; set; }

        /// <summary>
        /// One sentence on its Known status.
        /// </summary>
        [ObservableProperty]
        public partial string KnownText { get; set; } = string.Empty;

        /// <summary>
        /// Label for the known toggle.
        /// </summary>
        [ObservableProperty]
        public partial string ToggleKnownText { get; set; } = AppResources.ActionMarkKnown;

        /// <summary>
        /// Whether the device has a web page worth opening.
        /// </summary>
        [ObservableProperty]
        public partial bool HasWebPage { get; set; }

        /// <summary>
        /// The cards of facts.
        /// </summary>
        [ObservableProperty]
        public partial IReadOnlyList<DetailSection> Sections { get; set; } = [];

        /// <summary>
        /// True while the fresh lookup runs.
        /// </summary>
        [ObservableProperty]
        public partial bool IsLookingUp { get; set; }

        /// <summary>
        /// The last ping's result, or empty.
        /// </summary>
        [ObservableProperty]
        public partial string PingResult { get; set; } = string.Empty;

        /// <summary>
        /// Whether there is a ping result to show.
        /// </summary>
        [ObservableProperty]
        public partial bool HasPingResult { get; set; }

        /// <summary>
        /// True while a ping runs.
        /// </summary>
        [ObservableProperty]
        public partial bool IsPinging { get; set; }

        /// <summary>
        /// The Forget button's label, which asks for confirmation on the first tap.
        /// </summary>
        [ObservableProperty]
        public partial string ForgetText { get; set; } = AppResources.ActionForget;

        #endregion

        #region Methods

        /// <summary>
        /// Shows a device, or nothing for null, and starts a fresh lookup.
        /// </summary>
        /// <param name="id">The device's id.</param>
        public void Show(string? id)
        {
            _LookupCancel?.Cancel();

            DeviceId = id;
            EditableName = string.Empty;
            PingResult = string.Empty;
            ResetForget();
            Apply();

            if (id is not null)
            {
                _ = LookUpAsync(id);
            }
        }

        /// <summary>
        /// Follows changes while the view is on screen.
        /// </summary>
        public void Attach()
        {
            if (_IsAttached)
            {
                return;
            }

            _IsAttached = true;
            _Manager.Changed += OnManagerChanged;
            Apply();
        }

        /// <summary>
        /// Stops following changes and cancels the lookup once the view is gone.
        /// </summary>
        public void Detach()
        {
            _IsAttached = false;
            _Manager.Changed -= OnManagerChanged;
            _LookupCancel?.Cancel();
        }

        /// <summary>
        /// Saves the edited name.
        /// </summary>
        /// <returns>A task that completes once saved.</returns>
        [RelayCommand]
        private async Task SaveNameAsync()
        {
            // Only a real edit is saved; leaving the field untouched writes nothing.
            if ((DeviceId is not null)
                && (_Manager.GetDevice(DeviceId) is NetworkDevice device)
                && (EditableName.Trim() != (device.CustomName ?? string.Empty)))
            {
                await _Manager.RenameAsync(DeviceId, EditableName);
            }
        }

        /// <summary>
        /// Flips the device between known and unknown.
        /// </summary>
        /// <returns>A task that completes once saved.</returns>
        [RelayCommand]
        private async Task ToggleKnownAsync()
        {
            if (DeviceId is not null)
            {
                await _Manager.SetKnownAsync(DeviceId, !IsKnown);
            }
        }

        /// <summary>
        /// Pings the device four times and shows the result.
        /// </summary>
        /// <returns>A task that completes with the result shown.</returns>
        [RelayCommand]
        private async Task PingAsync()
        {
            if ((DeviceId is null) || IsPinging)
            {
                return;
            }

            IsPinging = true;
            PingResult = string.Empty;

            try
            {
                PingStatistics? ping = await _Manager.PingAsync(DeviceId, CancellationToken.None);

                PingResult = ping is { Received: > 0, AverageMs: double average }
                    ? NetworkFormat.Text(AppResources.PingResultFormat, average, ping.PacketLossPercent)
                    : AppResources.PingNoReply;
            }
            finally
            {
                IsPinging = false;
            }
        }

        /// <summary>
        /// Opens the device's web page in the browser.
        /// </summary>
        /// <returns>A task that completes once handed over.</returns>
        [RelayCommand]
        private async Task OpenWebPageAsync()
        {
            if ((DeviceId is null) || (_Manager.GetDevice(DeviceId) is not NetworkDevice device))
            {
                return;
            }

            bool isHttpsOnly = device.OpenPorts.Contains(443) && !device.OpenPorts.Contains(80);

            await Launcher.Default.OpenAsync(new Uri($"{(isHttpsOnly ? "https" : "http")}://{device.IpAddress}"));
        }

        /// <summary>
        /// Copies one value from the cards.
        /// </summary>
        /// <param name="row">The row that was tapped.</param>
        /// <returns>A task that completes once copied.</returns>
        [RelayCommand]
        private Task CopyAsync(DetailRow row)
        {
            return row.IsCopyable ? Clipboard.Default.SetTextAsync(row.Value) : Task.CompletedTask;
        }

        /// <summary>
        /// Forgets the device on the second tap within a few seconds. A two-step button rather than
        /// a dialog: the action is easy to redo (the next scan finds the device again).
        /// </summary>
        /// <returns>A task that completes once forgotten or the confirmation lapses.</returns>
        [RelayCommand]
        private async Task ForgetAsync()
        {
            if (DeviceId is null)
            {
                return;
            }

            if (ForgetText == AppResources.ActionForgetConfirm)
            {
                string id = DeviceId;

                ResetForget();
                Show(null);
                await _Manager.ForgetAsync(id);
                Forgotten?.Invoke(this, EventArgs.Empty);

                return;
            }

            ForgetText = AppResources.ActionForgetConfirm;
            _ForgetCancel = new CancellationTokenSource();

            try
            {
                await Task.Delay(FORGET_CONFIRM_MS, _ForgetCancel.Token);
                ResetForget();
            }
            catch (TaskCanceledException)
            {
                // Confirmed or moved on to another device.
            }
        }

        /// <summary>
        /// Goes back to the list.
        /// </summary>
        /// <returns>A task that completes once navigation finishes.</returns>
        [RelayCommand]
        private async Task BackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        /// <summary>
        /// Saves a new type when the picker changes.
        /// </summary>
        /// <param name="value">The picked index.</param>
        partial void OnTypeIndexChanged(int value)
        {
            if ((DeviceId is null) || (_Manager.GetDevice(DeviceId) is not NetworkDevice device) || ((int)device.Type == value))
            {
                return;
            }

            _ = _Manager.SetTypeAsync(DeviceId, (DeviceKind)value);
        }

        /// <summary>
        /// Shows the result line only when there is a result.
        /// </summary>
        /// <param name="value">The new result.</param>
        partial void OnPingResultChanged(string value)
        {
            HasPingResult = value.Length > 0;
        }

        /// <summary>
        /// Puts the Forget button back to its first state.
        /// </summary>
        private void ResetForget()
        {
            _ForgetCancel?.Cancel();
            ForgetText = AppResources.ActionForget;
        }

        /// <summary>
        /// Runs a fresh lookup for the device on show.
        /// </summary>
        /// <param name="id">The device's id.</param>
        /// <returns>A task that completes when the lookup is done.</returns>
        private async Task LookUpAsync(string id)
        {
            CancellationTokenSource cancel = new();

            _LookupCancel = cancel;
            IsLookingUp = true;

            try
            {
                await _Manager.RefreshDetailsAsync(id, cancel.Token);
            }
            catch (OperationCanceledException)
            {
                // Another device was opened, or the view closed.
            }
            finally
            {
                if (_LookupCancel == cancel)
                {
                    IsLookingUp = false;
                }
            }
        }

        /// <summary>
        /// Re-reads the device on the UI thread when anything changes.
        /// </summary>
        /// <param name="sender">The manager.</param>
        /// <param name="e">Unused.</param>
        private void OnManagerChanged(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(Apply);
        }

        /// <summary>
        /// Formats the device into the view's values.
        /// </summary>
        private void Apply()
        {
            NetworkDevice? device = DeviceId is null ? null : _Manager.GetDevice(DeviceId);

            HasDevice = device is not null;

            if (device is null)
            {
                Sections = [];
                return;
            }

            Name = DeviceDescriber.DisplayName(device);
            Icon = DeviceDescriber.Icon(device.Type);
            string typeName = DeviceDescriber.TypeName(device.Type);

            // Say what kind of device it is, unless the name already does.
            Subtitle = (device.Type == DeviceKind.Unknown) || (Name == typeName)
                ? device.IpAddress
                : $"{device.IpAddress} · {typeName}";
            IsKnown = device.IsKnown;
            CanToggleKnown = !device.IsThisDevice && !device.IsGateway;
            KnownText = device.IsThisDevice ? AppResources.DeviceThisDeviceBody
                : device.IsGateway ? AppResources.DeviceRouterBody
                : device.IsKnown ? AppResources.DeviceKnownBody
                : AppResources.DeviceUnknownBody;
            ToggleKnownText = device.IsKnown ? AppResources.ActionMarkUnknown : AppResources.ActionMarkKnown;
            HasWebPage = device.OpenPorts.Contains(80) || device.OpenPorts.Contains(443) || (device.HttpTitle is not null);

            // Leave the field alone while the user is typing in it.
            if (string.IsNullOrEmpty(EditableName) || (EditableName == (device.CustomName ?? string.Empty)))
            {
                EditableName = device.CustomName ?? string.Empty;
            }

            TypeIndex = (int)device.Type;
            Sections = Build(device);
        }

        /// <summary>
        /// Builds the cards, leaving out what the device did not reveal.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <returns>The non-empty cards.</returns>
        private static IReadOnlyList<DetailSection> Build(NetworkDevice device)
        {
            string? mac = device.MacAddress is null ? null
                : device.IsRandomizedMac == true ? NetworkFormat.Text(AppResources.DeviceMacPrivate, device.MacAddress)
                : device.MacAddress;

            List<DetailSection> sections =
            [
                DetailSection.From(
                    AppResources.DeviceSectionIdentity,
                    (AppResources.DeviceFriendlyName, device.FriendlyName),
                    (AppResources.DeviceHostName, device.HostName),
                    (AppResources.DeviceNetBios, device.NetBiosName),
                    (AppResources.DeviceMac, mac),
                    (AppResources.DeviceVendor, device.Vendor),
                    (AppResources.DeviceManufacturer, device.Manufacturer),
                    (AppResources.DeviceModel, device.ModelName),
                    (AppResources.DeviceWebTitle, device.HttpTitle)),
                DetailSection.From(
                    AppResources.DeviceSectionNetwork,
                    (AppResources.DeviceIpAddress, device.IpAddress),
                    (AppResources.DeviceLatency, device.RoundTripMs is long ms ? NetworkFormat.Latency(ms) : null),
                    (AppResources.DeviceFirstSeen, device.FirstSeen == default ? null : device.FirstSeen.LocalDateTime.ToString("g")),
                    (AppResources.DeviceLastSeen, device.LastSeen == default ? null : device.LastSeen.LocalDateTime.ToString("g"))),
            ];

            if (device.DetailsFetchedAt is not null)
            {
                sections.Add(new DetailSection(
                    AppResources.DeviceSectionPorts,
                    device.OpenPorts.Count == 0
                        ? [new DetailRow(AppResources.DeviceNoPorts, string.Empty)]
                        : device.OpenPorts
                            .Order()
                            .Select(port => new DetailRow(
                                LanDeviceDetails.KnownPorts.TryGetValue(port, out string? use)
                                    ? use
                                    : NetworkFormat.Text(AppResources.PortUnknownFormat, port),
                                port.ToString()))
                            .ToList()));
            }

            return sections.Where(section => section.Rows.Count > 0).ToList();
        }

        #endregion
    }
}
