using PingMyNetwork.Helper.Enums;

namespace PingMyNetwork.Models
{
    /// <summary>
    /// One finding about the user's own Wi-Fi, with what to do about it.
    /// </summary>
    /// <param name="Level">How serious it is, which picks its colour.</param>
    /// <param name="Icon">Icon glyph.</param>
    /// <param name="Title">The finding in a few words.</param>
    /// <param name="Body">One or two sentences of explanation.</param>
    /// <param name="Action">What the action button does.</param>
    /// <param name="ActionText">The action button's label, or empty for none.</param>
    public sealed record WifiInsight(
        StatusLevel Level,
        string Icon,
        string Title,
        string Body,
        InsightAction Action = InsightAction.None,
        string ActionText = "")
    {
        #region Properties

        /// <summary>
        /// Whether the insight offers an action.
        /// </summary>
        public bool HasAction => Action != InsightAction.None;

        #endregion
    }

    /// <summary>
    /// A signal reading saved under a place name during a signal walk.
    /// </summary>
    /// <param name="Name">Where the reading was taken, e.g. "Bedroom".</param>
    /// <param name="Percent">Signal quality from 0 to 100.</param>
    /// <param name="RssiDbm">Raw strength, where the platform reports it.</param>
    /// <param name="Level">The status level for the reading.</param>
    /// <param name="ValueText">The reading as shown, e.g. "100% · -38 dBm".</param>
    public sealed record SignalSpot(string Name, int Percent, int? RssiDbm, StatusLevel Level, string ValueText)
    {
        #region Properties

        /// <summary>
        /// Strength for ranking. dBm keeps telling spots apart where the percentage has already
        /// reached 100 (anywhere within a few metres of the router).
        /// </summary>
        public double Strength => RssiDbm ?? ((Percent / 2.0) - 100);

        #endregion
    }
}
