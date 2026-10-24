namespace PingMyNetwork.Helper.Enums
{
    /// <summary>
    /// How good a reading is. The order matches the status colours in StatusPalette.
    /// </summary>
    public enum StatusLevel
    {
        Good = 0,
        Fair = 1,
        Poor = 2,
        Bad = 3,
        Notice = 4,
    }

    /// <summary>
    /// What the Home screen leads with: whether this device can actually reach the internet.
    /// </summary>
    public enum ConnectionStatus
    {
        Checking,
        Online,
        CaptivePortal,
        NoInternet,
        Offline,
        WifiOff,
    }

    /// <summary>
    /// Width classes from the PRD's responsive layout section. Layouts follow window width, not
    /// device type, so a narrow desktop window gets the phone layout.
    /// </summary>
    public enum LayoutWidth
    {
        Compact,
        Medium,
        Expanded,
        Large,
    }

    /// <summary>
    /// What kind of thing a device on the network is, which picks its icon.
    /// </summary>
    public enum DeviceKind
    {
        Unknown = 0,
        Router,
        Computer,
        Phone,
        Tablet,
        Tv,
        Speaker,
        Printer,
        Camera,
        Storage,
        GameConsole,
        SmartHome,
    }

    /// <summary>
    /// Which devices the list shows.
    /// </summary>
    public enum DeviceFilter
    {
        All,
        Online,
        Unknown,
        New,
    }

    /// <summary>
    /// How the device list is ordered.
    /// </summary>
    public enum DeviceSort
    {
        Address,
        Name,
        LastSeen,
    }
}
