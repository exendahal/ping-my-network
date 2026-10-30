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
        /// Marks a web page title as an error or placeholder page rather than a name. Status codes
        /// only count as whole words, so a model such as "EX5400" is kept.
        /// </summary>
        private static readonly System.Text.RegularExpressions.Regex ERROR_TITLE = new(
            @"\b(error|not found|unauthori[sz]ed|forbidden|bad request|unavailable|moved|redirect|index of|it works|welcome to nginx|default page|access denied|(400|401|403|404|500|502|503))\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

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

            // MSI desktop boards ("MS-7817"), which Linux puts in its default host name.
            ("ms-7", DeviceKind.Computer),
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
        /// Adapter makers whose devices on a home network are nearly always one kind, as the
        /// WifiManager.MacVendors registry names them. A maker matches when the vendor name is it or
        /// starts with it as whole words, so "Sonos" does not catch "SonoSite"; the first match wins,
        /// so a maker's specific branch ("Lenovo Mobile") comes before the maker itself.
        /// </summary>
        /// <remarks>
        /// Makers of many kinds are deliberately absent: Apple, Samsung, Google, Amazon, Huawei, Xiaomi,
        /// TP-Link, Asus, HP (printers and PCs), Microsoft (Xbox and Surface), Sharp, Buffalo, and the
        /// Wi-Fi module makers found inside everything (Hon Hai, AzureWave, Liteon, Fugui). A wrong
        /// icon is worse than a generic one.
        /// </remarks>
        private static readonly (string Maker, DeviceKind Type)[] VENDOR_HINTS =
        [
            // Routers, gateways and ISP boxes.
            ("arcadyan", DeviceKind.Router),
            ("sagemcom", DeviceKind.Router),
            ("technicolor", DeviceKind.Router),
            ("t&w electronics", DeviceKind.Router),
            ("ubiquiti", DeviceKind.Router),
            ("netgear", DeviceKind.Router),
            ("routerboard.com", DeviceKind.Router),
            ("avm", DeviceKind.Router),
            ("draytek", DeviceKind.Router),
            ("zyxel", DeviceKind.Router),
            ("eero", DeviceKind.Router),
            ("sercomm", DeviceKind.Router),
            ("askey", DeviceKind.Router),
            ("mitrastar", DeviceKind.Router),
            ("gemtek", DeviceKind.Router),
            ("compal broadband", DeviceKind.Router),
            ("cisco", DeviceKind.Router),
            ("juniper networks", DeviceKind.Router),
            ("ruckus", DeviceKind.Router),
            ("fortinet", DeviceKind.Router),
            ("tenda", DeviceKind.Router),
            ("cambium networks", DeviceKind.Router),

            // Phones, before the makers whose other branches build computers.
            ("lenovo mobile", DeviceKind.Phone),
            ("motorola mobility", DeviceKind.Phone),
            ("oneplus", DeviceKind.Phone),
            ("oppo mobile", DeviceKind.Phone),
            ("vivo mobile", DeviceKind.Phone),
            ("realme", DeviceKind.Phone),
            ("honor device", DeviceKind.Phone),
            ("hmd global", DeviceKind.Phone),
            ("nothing technology", DeviceKind.Phone),
            ("fairphone", DeviceKind.Phone),

            // Computers.
            ("intel", DeviceKind.Computer),
            ("dell", DeviceKind.Computer),
            ("lenovo", DeviceKind.Computer),
            ("compal information", DeviceKind.Computer),
            ("micro-star", DeviceKind.Computer),
            ("msi", DeviceKind.Computer),
            ("asrock", DeviceKind.Computer),
            ("framework computer", DeviceKind.Computer),
            ("raspberry pi", DeviceKind.Computer),

            // TVs and streaming boxes.
            ("roku", DeviceKind.Tv),
            ("vizio", DeviceKind.Tv),
            ("tcl king", DeviceKind.Tv),
            ("tcl new technology", DeviceKind.Tv),
            ("hisense visual", DeviceKind.Tv),
            ("hisense electric", DeviceKind.Tv),
            ("skyworth", DeviceKind.Tv),
            ("funai", DeviceKind.Tv),

            // Speakers and audio.
            ("sonos", DeviceKind.Speaker),
            ("bose", DeviceKind.Speaker),
            ("sound united", DeviceKind.Speaker),
            ("bang & olufsen", DeviceKind.Speaker),

            // Printers.
            ("brother", DeviceKind.Printer),
            ("seiko epson", DeviceKind.Printer),
            ("canon", DeviceKind.Printer),
            ("xerox", DeviceKind.Printer),
            ("kyocera document", DeviceKind.Printer),
            ("lexmark", DeviceKind.Printer),
            ("ricoh", DeviceKind.Printer),
            ("konica minolta", DeviceKind.Printer),
            ("zebra technologies", DeviceKind.Printer),

            // Cameras and doorbells.
            ("hikvision", DeviceKind.Camera),
            ("dahua", DeviceKind.Camera),
            ("reolink", DeviceKind.Camera),
            ("ezviz", DeviceKind.Camera),
            ("wyze", DeviceKind.Camera),
            ("axis communications", DeviceKind.Camera),
            ("arlo", DeviceKind.Camera),
            ("ring", DeviceKind.Camera),
            ("amcrest", DeviceKind.Camera),
            ("uniview", DeviceKind.Camera),

            // Network storage.
            ("synology", DeviceKind.Storage),
            ("qnap", DeviceKind.Storage),
            ("western digital", DeviceKind.Storage),
            ("seagate", DeviceKind.Storage),

            // Game consoles.
            ("nintendo", DeviceKind.GameConsole),
            ("sony interactive", DeviceKind.GameConsole),
            ("valve", DeviceKind.GameConsole),

            // Smart home.
            ("espressif", DeviceKind.SmartHome),
            ("tuya", DeviceKind.SmartHome),
            ("shelly", DeviceKind.SmartHome),
            ("signify", DeviceKind.SmartHome),
            ("philips lighting", DeviceKind.SmartHome),
            ("ecobee", DeviceKind.SmartHome),
            ("nest labs", DeviceKind.SmartHome),
            ("irobot", DeviceKind.SmartHome),
            ("wiz connected", DeviceKind.SmartHome),
            ("gd midea", DeviceKind.SmartHome),
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
                device.MdnsName,
                device.NetBiosName,
                ShortHostName(device.HostName),
                model,
                UsefulTitle(device.HttpTitle))
                ?? (device.Type != DeviceKind.Unknown ? TypeName(device.Type)
                    : device.Vendor is not null ? NetworkFormat.Text(AppResources.DeviceVendorFormat, device.Vendor)
                    : AppResources.DeviceUnnamed);
        }

        /// <summary>
        /// A web page title that names the device, or null for an error page. Many devices answer
        /// http:// with a page like "Document Error: Not Found" or "401 Unauthorized", which says
        /// nothing about what they are.
        /// </summary>
        /// <param name="title">The title of the device's web page.</param>
        /// <returns>The title, or null.</returns>
        public static string? UsefulTitle(string? title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return null;
            }

            return ERROR_TITLE.IsMatch(title) ? null : title.Trim();
        }

        /// <summary>
        /// Tidies an mDNS instance name. AirPlay speakers prefix theirs with a MAC address
        /// ("A1B2C3D4E5F6@Kitchen"), which is dropped.
        /// </summary>
        /// <param name="instance">The instance name.</param>
        /// <returns>The name, or null when nothing readable is left.</returns>
        public static string? CleanServiceName(string? instance)
        {
            if (string.IsNullOrWhiteSpace(instance))
            {
                return null;
            }

            int at = instance.IndexOf('@');
            string name = (at >= 0 ? instance[(at + 1)..] : instance).Trim();

            return name.Length > 0 ? name : null;
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

            // Default names say nothing of the kind: Android TVs and tablets answer as Android.local.
            string names = string.Join(
                ' ',
                DeviceIdentity.OwnName(device.HostName),
                DeviceIdentity.OwnName(device.NetBiosName),
                device.FriendlyName,
                DeviceIdentity.OwnName(device.MdnsName),
                device.MdnsModel,
                device.Manufacturer,
                device.ModelName,
                UsefulTitle(device.HttpTitle)).ToLowerInvariant();

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

            return TypeOfVendor(device.Vendor);
        }

        /// <summary>
        /// The kind of device an adapter maker builds, when it builds nearly only one kind.
        /// </summary>
        /// <param name="vendor">The maker, as WifiManager.MacVendors names it.</param>
        /// <returns>The kind, or Unknown.</returns>
        public static DeviceKind TypeOfVendor(string? vendor)
        {
            if (string.IsNullOrEmpty(vendor))
            {
                return DeviceKind.Unknown;
            }

            foreach ((string maker, DeviceKind type) in VENDOR_HINTS)
            {
                // Whole words only: the maker must end the name or be followed by a non-letter.
                if (vendor.StartsWith(maker, StringComparison.OrdinalIgnoreCase)
                    && ((vendor.Length == maker.Length) || !char.IsLetterOrDigit(vendor[maker.Length])))
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
            Debug.Assert(CleanServiceName("A1B2C3D4E5F6@Kitchen") == "Kitchen");
            Debug.Assert(CleanServiceName("Anna's iPhone") == "Anna's iPhone");
            Debug.Assert(CleanServiceName(" ") is null);
            Debug.Assert(UsefulTitle("Document Error: Not Found") is null);
            Debug.Assert(UsefulTitle("401 Unauthorized") is null);
            Debug.Assert(UsefulTitle("Mi Router") == "Mi Router");
            Debug.Assert(UsefulTitle("NETGEAR EX5400") == "NETGEAR EX5400");
            Debug.Assert(UsefulTitle("TP-Link Login") == "TP-Link Login");
            Debug.Assert(DisplayName(new NetworkDevice { HttpTitle = "Document Error: Not Found" }) != "Document Error: Not Found");

            Debug.Assert(GuessType(new NetworkDevice { HostName = "Galaxy-S24.lan" }) == DeviceKind.Phone);
            Debug.Assert(GuessType(new NetworkDevice { HostName = "android-5f2c9e.lan" }) == DeviceKind.Phone);
            Debug.Assert(GuessType(new NetworkDevice { HostName = "Android.local", MdnsName = "MiTV-AXSO0" }) == DeviceKind.Tv);
            Debug.Assert(GuessType(new NetworkDevice { HostName = "Android.local" }) == DeviceKind.Unknown);
            Debug.Assert(GuessType(new NetworkDevice { OpenPorts = [9100] }) == DeviceKind.Printer);
            Debug.Assert(GuessType(new NetworkDevice { IsGateway = true, HostName = "iphone" }) == DeviceKind.Router);
            Debug.Assert(GuessType(new NetworkDevice { HttpTitle = "Mi Router" }) == DeviceKind.Router);
            Debug.Assert(GuessType(new NetworkDevice { Vendor = "Espressif" }) == DeviceKind.SmartHome);
            Debug.Assert(GuessType(new NetworkDevice { Vendor = "Panasonic" }) == DeviceKind.Unknown);

            // Vendor names exactly as the registry has them, including the near misses.
            Debug.Assert(TypeOfVendor("T&W Electronics") == DeviceKind.Router);
            Debug.Assert(TypeOfVendor("Sagemcom Broadband") == DeviceKind.Router);
            Debug.Assert(TypeOfVendor("AVM Audiovisuelles Marketing und Computersysteme") == DeviceKind.Router);
            Debug.Assert(TypeOfVendor("Compal Broadband Networks") == DeviceKind.Router);
            Debug.Assert(TypeOfVendor("Compal Information (Kunshan)") == DeviceKind.Computer);
            Debug.Assert(TypeOfVendor("Lenovo Mobile Communication Technology") == DeviceKind.Phone);
            Debug.Assert(TypeOfVendor("Lenovo") == DeviceKind.Computer);
            Debug.Assert(TypeOfVendor("Lenovo (Beijing)") == DeviceKind.Computer);
            Debug.Assert(TypeOfVendor("Intel") == DeviceKind.Computer);
            Debug.Assert(TypeOfVendor("Hisense Visual Technology") == DeviceKind.Tv);
            Debug.Assert(TypeOfVendor("Hisense Communications") == DeviceKind.Unknown);
            Debug.Assert(TypeOfVendor("Sonos") == DeviceKind.Speaker);
            Debug.Assert(TypeOfVendor("SonoSite") == DeviceKind.Unknown);
            Debug.Assert(TypeOfVendor("Boser Technology") == DeviceKind.Unknown);
            Debug.Assert(TypeOfVendor("Arlotto Comnet") == DeviceKind.Unknown);
            Debug.Assert(TypeOfVendor("RingCube Technologies") == DeviceKind.Unknown);
            Debug.Assert(TypeOfVendor("Ring") == DeviceKind.Camera);
            Debug.Assert(TypeOfVendor("Hpro") == DeviceKind.Unknown);
            Debug.Assert(TypeOfVendor("Kyocera Wireless") == DeviceKind.Unknown);
            Debug.Assert(TypeOfVendor("Xiaomi Mobile Software") == DeviceKind.Unknown);
            Debug.Assert(TypeOfVendor("Apple") == DeviceKind.Unknown);
            Debug.Assert(TypeOfVendor("GD Midea Air-Conditioning Equipment") == DeviceKind.SmartHome);
            Debug.Assert(GuessType(new NetworkDevice { HostName = "user-MS-7817.local" }) == DeviceKind.Computer);
            Debug.Assert(TypeOfVendor(null) == DeviceKind.Unknown);
            Debug.Assert(DisplayName(new NetworkDevice { Vendor = "Arcadyan" }) == "Arcadyan device");
            Debug.Assert(GuessType(new NetworkDevice()) == DeviceKind.Unknown);

            Debug.Assert(DisplayName(new NetworkDevice { CustomName = "Mum's phone", HostName = "galaxy.lan" }) == "Mum's phone");
            Debug.Assert(DisplayName(new NetworkDevice { HostName = "galaxy.lan" }) == "galaxy");
        }

        #endregion
    }
}
