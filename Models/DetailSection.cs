namespace PingMyNetwork.Models
{
    /// <summary>
    /// One labelled value on a details screen.
    /// </summary>
    /// <param name="Title">What the value is.</param>
    /// <param name="Value">The value, already formatted.</param>
    public sealed record DetailRow(string Title, string Value)
    {
        #region Properties

        /// <summary>
        /// Whether there is a value to copy. An informational row ("No common ports open") has none.
        /// </summary>
        public bool IsCopyable => Value.Length > 0;

        #endregion
    }

    /// <summary>
    /// A titled group of detail rows, shown as one card.
    /// </summary>
    /// <param name="Title">The section heading.</param>
    /// <param name="Rows">The rows in display order.</param>
    public sealed record DetailSection(string Title, IReadOnlyList<DetailRow> Rows)
    {
        #region Methods

        /// <summary>
        /// Builds a section from title and value pairs, leaving out values that are unknown.
        /// </summary>
        /// <param name="title">The section heading.</param>
        /// <param name="rows">Title and value pairs.</param>
        /// <returns>The section.</returns>
        public static DetailSection From(string title, params (string Title, string? Value)[] rows)
        {
            return new DetailSection(
                title,
                rows.Where(row => !string.IsNullOrWhiteSpace(row.Value)).Select(row => new DetailRow(row.Title, row.Value!)).ToList());
        }

        #endregion
    }
}
