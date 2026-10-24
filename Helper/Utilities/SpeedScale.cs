using System.Diagnostics;
using PingMyNetwork.Models;

namespace PingMyNetwork.Helper.Utilities
{
    /// <summary>
    /// The speed meter's scale: logarithmic, so 5, 50 and 500 Mbps are evenly spaced, and
    /// auto-ranging, so a multi-gigabit connection steps the top up instead of pinning the needle.
    /// </summary>
    public static class SpeedScale
    {
        #region Variables

        /// <summary>
        /// The tops the scale can range to, in Mbps; every test starts on the first.
        /// </summary>
        private static readonly double[] RANGES = [1000, 2500, 5000, 10000];

        /// <summary>
        /// The marks for each range, chosen so labels never crowd the dial.
        /// </summary>
        private static readonly Dictionary<double, double[]> MARKS = new()
        {
            [1000] = [0, 5, 10, 50, 100, 250, 500, 1000],
            [2500] = [0, 10, 50, 100, 250, 500, 1000, 2500],
            [5000] = [0, 10, 50, 100, 500, 1000, 2500, 5000],
            [10000] = [0, 10, 100, 500, 1000, 2500, 5000, 10000],
        };

        #endregion

        #region Methods

        /// <summary>
        /// The top a test starts with.
        /// </summary>
        public static double DefaultRange => RANGES[0];

        /// <summary>
        /// The range to show a speed on: the current one while it fits, otherwise the smallest that
        /// does. Never steps back down during a test, so the dial does not jump around.
        /// </summary>
        /// <param name="mbps">The speed.</param>
        /// <param name="current">The range on screen.</param>
        /// <returns>The range to use.</returns>
        public static double RangeFor(double mbps, double current)
        {
            if (mbps <= current)
            {
                return current;
            }

            return RANGES.FirstOrDefault(range => range >= mbps, RANGES[^1]);
        }

        /// <summary>
        /// Where a speed sits on the dial.
        /// </summary>
        /// <param name="mbps">The speed.</param>
        /// <param name="range">The top of the scale.</param>
        /// <returns>0 to 1; above the top stays at 1.</returns>
        public static double FractionOf(double mbps, double range)
        {
            return Math.Clamp(Math.Log10(1 + Math.Max(0, mbps)) / Math.Log10(1 + range), 0, 1);
        }

        /// <summary>
        /// The labelled marks for a range.
        /// </summary>
        /// <param name="range">The top of the scale.</param>
        /// <returns>The marks.</returns>
        public static IReadOnlyList<GaugeTick> Ticks(double range)
        {
            return [.. MARKS[range].Select(mbps => new GaugeTick((float)FractionOf(mbps, range), Label(mbps)))];
        }

        /// <summary>
        /// A speed with the precision its size deserves: "0.45", "8.4", "47" or "1,450".
        /// </summary>
        /// <param name="mbps">The speed.</param>
        /// <returns>The number.</returns>
        public static string Format(double mbps)
        {
            if (mbps < 1)
            {
                return mbps.ToString("0.00");
            }

            return mbps < 10 ? mbps.ToString("0.0") : mbps.ToString("N0");
        }

        /// <summary>
        /// A mark's label: "50", or "2.5G" from a gigabit up, which keeps the dial uncrowded.
        /// </summary>
        /// <param name="mbps">The marked speed.</param>
        /// <returns>The label.</returns>
        private static string Label(double mbps)
        {
            return mbps >= 1000 ? $"{mbps / 1000:0.#}G" : mbps.ToString("0");
        }

        /// <summary>
        /// Checks ranging, placement and formatting at the edges. Called at startup in Debug.
        /// </summary>
        [Conditional("DEBUG")]
        public static void SelfCheck()
        {
            Debug.Assert(RangeFor(47, 1000) == 1000);
            Debug.Assert(RangeFor(1450, 1000) == 2500);
            Debug.Assert(RangeFor(3000, 1000) == 5000);
            Debug.Assert(RangeFor(40000, 1000) == 10000);
            Debug.Assert(RangeFor(300, 2500) == 2500);
            Debug.Assert(FractionOf(0, 1000) == 0);
            Debug.Assert(FractionOf(-1, 1000) == 0);
            Debug.Assert(FractionOf(1000, 1000) == 1);
            Debug.Assert(FractionOf(5000, 1000) == 1);
            Debug.Assert(FractionOf(0.5, 1000) is > 0 and < 0.1);
            Debug.Assert(Format(0.04) == (0.04).ToString("0.00"));
            Debug.Assert(Format(8.44) == (8.4).ToString("0.0"));
            Debug.Assert(Format(1450) == (1450.0).ToString("N0"));
            Debug.Assert(Ticks(2500)[^1].Label == "2.5G");
            Debug.Assert(Ticks(1000)[^1] is { Label: "1G", Fraction: 1f });
            Debug.Assert(MARKS.Keys.SequenceEqual(RANGES));
        }

        #endregion
    }
}
