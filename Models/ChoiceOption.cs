namespace PingMyNetwork.Models
{
    /// <summary>
    /// One chip in a row of mutually exclusive choices.
    /// </summary>
    /// <param name="Key">What the choice stands for.</param>
    /// <param name="Name">The chip's label.</param>
    /// <param name="IsSelected">Whether it is the current choice.</param>
    public sealed record ChoiceOption(string Key, string Name, bool IsSelected);
}
