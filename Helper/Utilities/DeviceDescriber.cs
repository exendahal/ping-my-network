using System.Diagnostics;
using PingMyNetwork.Helper.Enums;
using PingMyNetwork.Models;
using PingMyNetwork.Resources.Strings;

namespace PingMyNetwork.Helper.Utilities
{
    /// <summary>
    /// What to call a device, which icon it gets, and a best guess at what kind of device it is.
    /// </summary>
    public static class DeviceDescriber
    {
        #region Variables

        /// <summary>
        /// Suffixes routers append to host names, which add nothing for the reader.
        /// </summary>
        private static readonly string[] LOCAL_SUFFIXES = [".lan", ".local", ".home", ".localdomain", ".home.arpa"];

        /// <summary>
        /// Words in a name or model that give the kind of device away, checked in order.
        /// </summary>
        private static readonly (string Word, DeviceKind Type)[] NAME_HINTS =
        [
            ("galaxy-tab", DeviceKind.Tablet),
            ("iphone", DeviceKind.Phone),
            ("android", DeviceKind.Phone),
            ("galaxy", DeviceKind.Phone),
            ("pixel", DeviceKind.Phone),
            ("redmi", DeviceKind.Phone),
            ("oneplus", DeviceKind.Phone),
            ("ipad", DeviceKind.Tablet),
            ("macbook", DeviceKind.Computer),
            ("imac", DeviceKind.Computer),
            ("desktop", DeviceKind.Computer),
            ("laptop", DeviceKind.Computer),
            ("-pc", DeviceKind.Computer),
            ("thinkpad", DeviceKind.Computer),
            ("tv", DeviceKind.Tv),
            ("chromecast", DeviceKind.Tv),
            ("roku", DeviceKind.Tv),
            ("firestick", DeviceKind.Tv),
            ("echo", DeviceKind.Speaker),
            ("sonos", DeviceKind.Speaker),
            ("homepod", DeviceKind.Speaker),
            ("printer", DeviceKind.Printer),
            ("epson", DeviceKind.Printer),
            ("brother", DeviceKind.Printer),
            ("canon", DeviceKind.Printer),
            ("cam", DeviceKind.Camera),
            ("nas", DeviceKind.Storage),
            ("synology", DeviceKind.Storage),
            ("playstation", DeviceKind.GameConsole),
            ("xbox", DeviceKind.GameConsole),
            ("nintendo", DeviceKind.GameConsole),
            ("esp", DeviceKind.SmartHome),
            ("tasmota", DeviceKind.SmartHome),
            ("shelly", DeviceKind.SmartHome),
            ("plug", DeviceKind.SmartHome),
            ("router", DeviceKind.Router),
        ];

        /// <summary>
        /// Adapter makers that only build one kind of device, matched against the start of the maker
        /// name. Makers of many kinds (Apple, Samsung, TP-Link) are deliberately absent.
        /// </summary>
        private static readonly (string Maker, DeviceKind Type)[] VENDOR_HINTS =
        [
            ("espressif", DeviceKind.SmartHome),
            ("tuya", DeviceKind.SmartHome),
            ("hikvision", DeviceKind.Camera),
            ("dahua", DeviceKind.Camera),
            ("reolink", DeviceKind.Camera),
            ("raspberry pi", DeviceKind.Computer),
            ("roku", DeviceKind.Tv),
            ("vizio", DeviceKind.Tv),
            ("sonos", DeviceKind.Speaker),
            ("synology", DeviceKind.Storage),
            ("nintendo", DeviceKind.GameConsole),
        ];

        #endregion

        #region Methods

        /// <summary>
        /// The name to show: the user's, then the best name the device gave, then its model.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <returns>A name, never empty.</returns>
        public static string DisplayName(NetworkDevice device)
        {
            string? model = string.Join(' ', new[] { device.Manufacturer, device.ModelName }.Where(part => !string.IsNullOrWhiteSpace(part)));

            return FirstOf(
                device.CustomName,
                device.FriendlyName,
                device.NetBiosName,
                ShortHostName(device.HostName),
                model,
                device.HttpTitle)
                ?? (device.Type != DeviceKind.Unknown ? TypeName(device.Type)
                    : device.Vendor is not null ? NetworkFormat.Text(AppResources.DeviceVendorFormat, device.Vendor)
                    : AppResources.DeviceUnnamed);
        }

        /// <summary>
        /// Drops the local domain a router appends, e.g. "galaxy-s24.lan" becomes "galaxy-s24".
        /// </summary>
        /// <param name="hostName">The full host name.</param>
        /// <returns>The short name, or null.</returns>
        public static string? ShortHostName(string? hostName)
        {
            if (string.IsNullOrWhiteSpace(hostName))
            {
                return null;
            }

            foreach (string suffix in LOCAL_SUFFIXES)
            {
                if (hostName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return hostName[..^suffix.Length];
                }
            }

            return hostName;
        }

        /// <summary>
        /// Guesses the kind of device from its role, names and open ports.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <returns>The guess, or Unknown.</returns>
        public static DeviceKind GuessType(NetworkDevice device)
        {
            if (device.IsGateway)
            {
                return DeviceKind.Router;
            }

            if (device.IsThisDevice)
            {
                return DeviceInfo.Current.Idiom == DeviceIdiom.Phone ? DeviceKind.Phone
                    : DeviceInfo.Current.Idiom == DeviceIdiom.Tablet ? DeviceKind.Tablet
                    : DeviceKind.Computer;
            }

            string names = string.Join(
                ' ',
                device.HostName,
                device.NetBiosName,
                device.FriendlyName,
                device.Manufacturer,
                device.ModelName,
                device.HttpTitle).ToLowerInvariant();

            foreach ((string word, DeviceKind type) in NAME_HINTS)
            {
                if (names.Contains(word, StringComparison.Ordinal))
                {
                    return type;
                }
            }

            List<int> ports = device.OpenPorts;

            if (ports.Contains(9100) || ports.Contains(631))
            {
                return DeviceKind.Printer;
            }

            if (ports.Contains(8008) || ports.Contains(8009) || ports.Contains(7000))
            {
                return DeviceKind.Tv;
            }

            if (ports.Contains(554))
            {
                return DeviceKind.Camera;
            }

            if (ports.Contains(62078))
            {
                return DeviceKind.Phone;
            }

            if (ports.Contains(1883))
            {
                return DeviceKind.SmartHome;
            }

            if (ports.Contains(3389) || (ports.Contains(445) && !string.IsNullOrEmpty(device.NetBiosName)))
            {
                return DeviceKind.Computer;
            }

            foreach ((string maker, DeviceKind type) in VENDOR_HINTS)
            {
                if (device.Vendor?.StartsWith(maker, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return type;
                }
            }

            return DeviceKind.Unknown;
        }

        /// <summary>
        /// The icon glyph for a kind of device.
        /// </summary>
        /// <param name="type">The kind of device.</param>
        /// <returns>A Material Icons glyph.</returns>
        public static string Icon(DeviceKind type)
        {
            return type switch
            {
                DeviceKind.Router => MaterialIcon.ROUTER,
                DeviceKind.Computer => MaterialIcon.COMPUTER,
                DeviceKind.Phone => MaterialIcon.SMARTPHONE,
                DeviceKind.Tablet => MaterialIcon.TABLET,
                DeviceKind.Tv => MaterialIcon.TV,
                DeviceKind.Speaker => MaterialIcon.SPEAKER,
                DeviceKind.Printer => MaterialIcon.PRINT,
                DeviceKind.Camera => MaterialIcon.VIDEOCAM,
                DeviceKind.Storage => MaterialIcon.STORAGE,
                DeviceKind.GameConsole => MaterialIcon.GAMEPAD,
                DeviceKind.SmartHome => MaterialIcon.SMART_HOME,
                _ => MaterialIcon.DEVICE_UNKNOWN,
            };
        }

        /// <summary>
        /// The name of a kind of device.
        /// </summary>
        /// <param name="type">The kind of device.</param>
        /// <returns>Its name.</returns>
        public static string TypeName(DeviceKind type)
        {
            return AppResources.ResourceManager.GetString($"DeviceType{type}", AppResources.Culture) ?? type.ToString();
        }

        /// <summary>
        /// The first value that is not empty.
        /// </summary>
        /// <param name="values">Candidates in order of preference.</param>
        /// <returns>The first non-empty value, or null.</returns>
        private static string? FirstOf(params string?[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        }

        /// <summary>
        /// Runs the naming and type rules against known cases. Called at startup in Debug builds.
        /// </summary>
        [Conditional("DEBUG")]
        public static void SelfCheck()
        {
            Debug.Assert(ShortHostName("galaxy-s24.lan") == "galaxy-s24");
            Debug.Assert(ShortHostName("nas.home.arpa") == "nas");
            Debug.Assert(ShortHostName("printer") == "printer");

            Debug.Assert(GuessType(new NetworkDevice { HostName = "Galaxy-S24.lan" }) == DeviceKind.Phone);
            Debug.Assert(GuessType(new NetworkDevice { OpenPorts = [9100] }) == DeviceKind.Printer);
            Debug.Assert(GuessType(new NetworkDevice { IsGateway = true, HostName = "iphone" }) == DeviceKind.Router);
            Debug.Assert(GuessType(new NetworkDevice { HttpTitle = "Mi Router" }) == DeviceKind.Router);
            Debug.Assert(GuessType(new NetworkDevice { Vendor = "Espressif" }) == DeviceKind.SmartHome);
            Debug.Assert(GuessType(new NetworkDevice { Vendor = "Panasonic" }) == DeviceKind.Unknown);
            Debug.Assert(DisplayName(new NetworkDevice { Vendor = "Arcadyan" }) == "Arcadyan device");
            Debug.Assert(GuessType(new NetworkDevice()) == DeviceKind.Unknown);

            Debug.Assert(DisplayName(new NetworkDevice { CustomName = "Mum's phone", HostName = "galaxy.lan" }) == "Mum's phone");
            Debug.Assert(DisplayName(new NetworkDevice { HostName = "galaxy.lan" }) == "galaxy");
        }

        #endregion
    }
}
