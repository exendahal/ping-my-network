using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.Models;
using PingMyNetwork.Resources.Strings;

namespace PingMyNetwork.ViewModels
{
    /// <summary>
    /// One row of the device list, formatted once per change so the row template binds plain values.
    /// </summary>
    /// <remarks>
    /// Rows are updated in place rather than replaced, so a scan that reports the same device again
    /// changes a few labels instead of rebuilding the row.
    /// </remarks>
    public sealed partial class DeviceItem : ObservableObject
    {
        #region Variables

        /// <summary>
        /// Opacity of a device that did not answer the latest scan.
        /// </summary>
        private const double OFFLINE_OPACITY = 0.55;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a row for a device. The actions are the list's, so the row template and the
        /// right-click menu bind to the row itself rather than reaching up to the page.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="open">Opens the device's details.</param>
        /// <param name="toggleKnown">Flips it between known and unknown.</param>
        /// <param name="copyAddress">Copies its address.</param>
        public DeviceItem(
            NetworkDevice device,
            Action<DeviceItem> open,
            Func<DeviceItem, Task> toggleKnown,
            Func<DeviceItem, Task> copyAddress)
        {
            Id = device.Id;
            OpenCommand = new RelayCommand(() => open(this));
            ToggleKnownCommand = new AsyncRelayCommand(() => toggleKnown(this));
            CopyAddressCommand = new AsyncRelayCommand(() => copyAddress(this));

            Update(device);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The device's id.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Opens the device's details.
        /// </summary>
        public IRelayCommand OpenCommand { get; }

        /// <summary>
        /// Flips the device between known and unknown.
        /// </summary>
        public IAsyncRelayCommand ToggleKnownCommand { get; }

        /// <summary>
        /// Copies the device's address.
        /// </summary>
        public IAsyncRelayCommand CopyAddressCommand { get; }

        /// <summary>
        /// Whether this row's details are open in the pane on wide windows.
        /// </summary>
        [ObservableProperty]
        public partial bool IsSelected { get; set; }

        /// <summary>
        /// The device as last shown, for sorting and filtering.
        /// </summary>
        public NetworkDevice Device { get; private set; } = new();

        /// <summary>
        /// What to call it.
        /// </summary>
        [ObservableProperty]
        public partial string Name { get; set; } = string.Empty;

        /// <summary>
        /// Address and maker, or when it was last seen.
        /// </summary>
        [ObservableProperty]
        public partial string Subtitle { get; set; } = string.Empty;

        /// <summary>
        /// Icon for its kind.
        /// </summary>
        [ObservableProperty]
        public partial string Icon { get; set; } = MaterialIcon.DEVICE_UNKNOWN;

        /// <summary>
        /// Tag naming its role ("This device", "Router"), or empty.
        /// </summary>
        [ObservableProperty]
        public partial string RoleTag { get; set; } = string.Empty;

        /// <summary>
        /// Whether to show the role tag.
        /// </summary>
        [ObservableProperty]
        public partial bool HasRoleTag { get; set; }

        /// <summary>
        /// Whether to show the New tag.
        /// </summary>
        [ObservableProperty]
        public partial bool IsNew { get; set; }

        /// <summary>
        /// Whether to show the Unknown tag.
        /// </summary>
        [ObservableProperty]
        public partial bool IsUnknown { get; set; }

        /// <summary>
        /// Whether the user has marked it known.
        /// </summary>
        [ObservableProperty]
        public partial bool IsKnown { get; set; }

        /// <summary>
        /// Whether it answered the latest scan.
        /// </summary>
        [ObservableProperty]
        public partial bool IsOnline { get; set; }

        /// <summary>
        /// Dims offline devices.
        /// </summary>
        [ObservableProperty]
        public partial double RowOpacity { get; set; } = 1;

        /// <summary>
        /// The row read as one phrase by a screen reader.
        /// </summary>
        [ObservableProperty]
        public partial string SemanticDescription { get; set; } = string.Empty;

        /// <summary>
        /// Label for the mark known / unknown action.
        /// </summary>
        [ObservableProperty]
        public partial string ToggleKnownText { get; set; } = AppResources.ActionMarkKnown;

        #endregion

        #region Methods

        /// <summary>
        /// Refreshes the row from the device's latest state.
        /// </summary>
        /// <param name="device">The device.</param>
        public void Update(NetworkDevice device)
        {
            // A private (randomised) MAC has no maker; saying so explains the missing name.
            string? maker = device.Manufacturer ?? device.ModelName ?? device.Vendor
                ?? (device.IsRandomizedMac == true ? AppResources.DevicePrivateAddress : null);

            Device = device;
            Name = DeviceDescriber.DisplayName(device);
            Icon = DeviceDescriber.Icon(device.Type);
            IsOnline = device.IsOnline;
            IsKnown = device.IsKnown;
            IsUnknown = !device.IsKnown && !device.IsThisDevice;
            IsNew = device.IsNew;
            RowOpacity = device.IsOnline ? 1 : OFFLINE_OPACITY;
            ToggleKnownText = device.IsKnown ? AppResources.ActionMarkUnknown : AppResources.ActionMarkKnown;

            RoleTag = device.IsThisDevice ? AppResources.TagThisDevice
                : device.IsGateway ? AppResources.TagRouter
                : string.Empty;
            HasRoleTag = RoleTag.Length > 0;

            Subtitle = !device.IsOnline && (device.LastSeen != default)
                ? NetworkFormat.Text(AppResources.DeviceOfflineFormat, device.LastSeen.LocalDateTime)
                : maker is null
                    ? device.IpAddress
                    : NetworkFormat.Text(AppResources.DeviceSubtitleFormat, device.IpAddress, maker);

            SemanticDescription = string.Join(
                ", ",
                new[]
                {
                    Name,
                    Subtitle,
                    RoleTag,
                    IsNew ? AppResources.TagNew : null,
                    device.IsKnown ? AppResources.TagKnown : AppResources.TagUnknown,
                }.Where(part => !string.IsNullOrEmpty(part)));
        }

        #endregion
    }
}
