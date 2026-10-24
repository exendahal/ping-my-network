namespace PingMyNetwork.Helper.Utilities
{
    /// <summary>
    /// Reads a value defined in App.xaml's merged dictionaries from code.
    /// </summary>
    public static class ResourceLookup
    {
        #region Methods

        /// <summary>
        /// Finds a resource by key.
        /// </summary>
        /// <typeparam name="T">The resource's type.</typeparam>
        /// <param name="key">Its key in Colors.xaml or Styles.xaml.</param>
        /// <param name="fallback">Returned when the key is missing or of another type.</param>
        /// <returns>The resource, or the fallback.</returns>
        public static T Get<T>(string key, T fallback)
        {
            return (Application.Current?.Resources.TryGetValue(key, out object? value) == true) && (value is T typed)
                ? typed
                : fallback;
        }

        #endregion
    }
}
