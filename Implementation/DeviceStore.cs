using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using PingMyNetwork.Models;

namespace PingMyNetwork.Implementation
{
    /// <summary>
    /// Saves the remembered networks and their devices as one JSON file in the app's data folder.
    /// </summary>
    /// <remarks>
    /// ponytail: one JSON file instead of the PRD's SQLite. A household network has tens of devices,
    /// so reading and writing the whole file costs less than a database dependency. Move to SQLite
    /// when history (PRD 6.12) makes the file large.
    ///
    /// Backup (PRD 6.16), with no account and no server of our own: Android Auto Backup carries the
    /// file to the user's Google account. On iOS and Mac a copy also goes to the user's iCloud
    /// key-value store, which restores it on a reinstall or a new device signed in to the same
    /// Apple ID, and the file is part of the normal iCloud device backup.
    /// </remarks>
    public sealed class DeviceStore
    {
        #region Variables

        private const string FILE_NAME = "devices.json";

        /// <summary>
        /// Key of the iCloud copy.
        /// </summary>
        private const string CLOUD_KEY = "devices";

        /// <summary>
        /// iCloud's key-value store holds at most 1 MB per value; a larger file is only backed up
        /// with the device backup.
        /// </summary>
        private const int CLOUD_MAX_BYTES = 1_000_000;

        private readonly string _Path = Path.Combine(FileSystem.AppDataDirectory, FILE_NAME);
        private readonly SemaphoreSlim _WriteGate = new(1, 1);
        private readonly ILogger<DeviceStore> _Logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the store.
        /// </summary>
        /// <param name="logger">Where read and write failures are reported.</param>
        public DeviceStore(ILogger<DeviceStore> logger)
        {
            _Logger = logger;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Reads every remembered network.
        /// </summary>
        /// <returns>The networks, or an empty list when there is no file or it cannot be read.</returns>
        public async Task<List<NetworkRecord>> LoadAsync()
        {
            if (!File.Exists(_Path) && (ReadCloudCopy() is byte[] restored))
            {
                // A reinstall or a new device: start from the iCloud copy.
                await SaveAsync(restored);
            }

            if (!File.Exists(_Path))
            {
                return [];
            }

            try
            {
                await using FileStream stream = File.OpenRead(_Path);

                return await JsonSerializer.DeserializeAsync(stream, DeviceStoreJsonContext.Default.ListNetworkRecord) ?? [];
            }
            catch (Exception exception) when (exception is IOException or JsonException)
            {
                // A damaged file is kept aside rather than overwritten, so nothing the user named is lost for good.
                _Logger.LogError(exception, "Could not read {Path}; starting with no remembered devices.", _Path);
                TryMoveAside();

                return [];
            }
        }

        /// <summary>
        /// Turns the networks into the file's contents. Fast enough to call while holding a lock,
        /// which is how callers get a consistent copy of data a scan is still changing.
        /// </summary>
        /// <param name="networks">The networks to save; not modified.</param>
        /// <returns>The JSON bytes.</returns>
        public byte[] Serialize(List<NetworkRecord> networks)
        {
            return JsonSerializer.SerializeToUtf8Bytes(networks, DeviceStoreJsonContext.Default.ListNetworkRecord);
        }

        /// <summary>
        /// Writes the file. It is replaced in one step, so a crash mid-write leaves the previous
        /// version intact.
        /// </summary>
        /// <param name="json">Contents from <see cref="Serialize"/>.</param>
        /// <returns>A task that completes when the file is written.</returns>
        public async Task SaveAsync(byte[] json)
        {
            string temporary = $"{_Path}.tmp";

            await _WriteGate.WaitAsync();

            try
            {
                await File.WriteAllBytesAsync(temporary, json);
                File.Move(temporary, _Path, overwrite: true);
                WriteCloudCopy(json);
            }
            catch (IOException exception)
            {
                _Logger.LogError(exception, "Could not save {Path}.", _Path);
            }
            finally
            {
                _WriteGate.Release();
            }
        }

        /// <summary>
        /// Reads the iCloud copy of the file, on iOS and Mac.
        /// </summary>
        /// <returns>The file's contents, or null when there is none.</returns>
        private static byte[]? ReadCloudCopy()
        {
#if IOS || MACCATALYST
            Foundation.NSUbiquitousKeyValueStore cloud = Foundation.NSUbiquitousKeyValueStore.DefaultStore;

            cloud.Synchronize();

            return cloud.GetData(CLOUD_KEY)?.ToArray();
#else
            return null;
#endif
        }

        /// <summary>
        /// Puts a copy of the file in the user's iCloud, on iOS and Mac.
        /// </summary>
        /// <param name="json">The file's contents.</param>
        private static void WriteCloudCopy(byte[] json)
        {
#if IOS || MACCATALYST
            if (json.Length > CLOUD_MAX_BYTES)
            {
                return;
            }

            Foundation.NSUbiquitousKeyValueStore cloud = Foundation.NSUbiquitousKeyValueStore.DefaultStore;

            cloud.SetData(CLOUD_KEY, Foundation.NSData.FromArray(json));
            cloud.Synchronize();
#endif
        }

        /// <summary>
        /// Renames an unreadable file so the next save does not overwrite it.
        /// </summary>
        private void TryMoveAside()
        {
            try
            {
                File.Move(_Path, $"{_Path}.{DateTime.Now:yyyyMMddHHmmss}.bad", overwrite: true);
            }
            catch (IOException)
            {
                // Nothing more to do; the next save replaces it.
            }
        }

        #endregion
    }

    /// <summary>
    /// Source-generated JSON metadata, so saving works with trimming and AOT.
    /// </summary>
    [JsonSourceGenerationOptions(WriteIndented = false, UseStringEnumConverter = true)]
    [JsonSerializable(typeof(List<NetworkRecord>))]
    [JsonSerializable(typeof(List<SignalSpot>))]
    internal sealed partial class DeviceStoreJsonContext : JsonSerializerContext
    {
    }
}
