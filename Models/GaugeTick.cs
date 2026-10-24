namespace PingMyNetwork.Models
{
    /// <summary>
    /// A labelled mark on a gauge's scale.
    /// </summary>
    /// <param name="Fraction">Where along the arc it sits, from 0 to 1.</param>
    /// <param name="Label">The value it marks, e.g. "100".</param>
    public readonly record struct GaugeTick(float Fraction, string Label);
}
