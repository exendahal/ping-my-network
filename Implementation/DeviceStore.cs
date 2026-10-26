using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using PingMyNetwork.Models;

namespace PingMyNetwork.Implementation
{
    /// <summary>
    /// Saves the remembered networks and their devices as JSON in the app's data folder: the user's
    /// own networks in one file, guest and undecided networks in another.
    /// </summary>
    /// <remarks>
    /// ponytail: one JSON file instead of SQLite. A household network has tens of devices,
    /// so reading and writing the whole file costs less than a database dependency. Move to SQLite
    /// when a device history makes the file large.
    ///
    /// Backup, with no account and no server of our own, and only of the user's own
    /// networks: Android Auto Backup carries devices.json to the user's Google account, while the
    /// backup rules leave devices.local.json out. On iOS and Mac a copy of devices.json also goes
    /// to the user's iCloud key-value store, which restores it on a reinstall or a new device
    /// signed in to the same Apple ID; devices.local.json is excluded from the device backup.
    /// </remarks>
    public sealed class DeviceStore
    {
        #region Variables

        /// <summary>
        /// The user's own networks; backed up.
        /// </summary>
        private const string FILE_NAME = "devices.json";

        /// <summary>
        /// Guest and undecided networks; stays on this device. Named in the Android backup rules.
        /// </summary>
        private const string LOCAL_FILE_NAME = "devices.local.json";

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
        private readonly string _LocalPath = Path.Combine(FileSystem.AppDataDirectory, LOCAL_FILE_NAME);
        private readonly SemaphoreSlim _WriteGate = new(1, 1);
        private readonly ILogger<DeviceStore> _Logger;

        /// <summary>
        /// Whether this device has had networks of the user's own since it started. Until then an
        /// empty list is never sent to iCloud: on a fresh install the iCloud copy can arrive after
        /// the first save, and writing "no networks" first would wipe it.
        /// </summary>
        private bool _HasOwnNetworks;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the store.
        /// </summary>
        /// <param name="logger">Where read and write failures are reported.</param>
        public DeviceStore(ILogger<DeviceStore> logger)
        {
            _Logger = logger;

#if IOS || MACCATALYST
            // iCloud delivers its copy whenever it syncs, which on a new install is often after the
            // app has started.
            Foundation.NSNotificationCenter.DefaultCenter.AddObserver(
                Foundation.NSUbiquitousKeyValueStore.DidChangeExternallyNotification,
                notification => _ = RestoreLateCloudCopyAsync(),
                Foundation.NSUbiquitousKeyValueStore.DefaultStore);
#endif
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised, on any thread, when iCloud delivers the user's networks after the app started
        /// with none of its own. Carries the restored networks.
        /// </summary>
        public event EventHandler<List<NetworkRecord>>? CloudRestored;

        #endregion

        #region Methods

        /// <summary>
        /// Reads every remembered network.
        /// </summary>
        /// <returns>The networks, or an empty list when there are no files or they cannot be read.</returns>
        public async Task<List<NetworkRecord>> LoadAsync()
        {
            if (!File.Exists(_Path) && (ReadCloudCopy() is byte[] restored))
            {
                // A reinstall or a new device: start from the iCloud copy.
                await WriteAsync(_Path, restored);
            }

            List<NetworkRecord> mine = await ReadAsync(_Path);

            _HasOwnNetworks = mine.Count > 0;

            return [.. mine, .. await ReadAsync(_LocalPath)];
        }

        /// <summary>
        /// Turns the networks into a file's contents. Fast enough to call while holding a lock,
        /// which is how callers get a consistent copy of data a scan is still changing.
        /// </summary>
        /// <param name="networks">The networks to save; not modified.</param>
        /// <returns>The JSON bytes.</returns>
        public byte[] Serialize(List<NetworkRecord> networks)
        {
            return JsonSerializer.SerializeToUtf8Bytes(networks, DeviceStoreJsonContext.Default.ListNetworkRecord);
        }

        /// <summary>
        /// Writes both files, and the iCloud copy of the user's own networks.
        /// </summary>
        /// <param name="mine">The user's own networks, from <see cref="Serialize"/>.</param>
        /// <param name="others">Guest and undecided networks, from <see cref="Serialize"/>.</param>
        /// <param name="hasMine">Whether <paramref name="mine"/> holds any network.</param>
        /// <returns>A task that completes when the files are written.</returns>
        public async Task SaveAsync(byte[] mine, byte[] others, bool hasMine)
        {
            await _WriteGate.WaitAsync();

            try
            {
                if (await WriteAsync(_Path, mine) && (hasMine || _HasOwnNetworks))
                {
                    WriteCloudCopy(mine);
                }

                _HasOwnNetworks |= hasMine;

                if (await WriteAsync(_LocalPath, others))
                {
                    ExcludeFromBackup(_LocalPath);
                }
            }
            finally
            {
                _WriteGate.Release();
            }
        }

        /// <summary>
        /// Takes the iCloud copy when it arrives after the app started, as long as this device has
        /// no networks of the user's own yet: a reinstall or a new device catching up.
        /// </summary>
        /// <returns>A task that completes once restored, or at once when there is nothing to do.</returns>
        private async Task RestoreLateCloudCopyAsync()
        {
            if (_HasOwnNetworks || (ReadCloudCopy() is not byte[] cloud))
            {
                return;
            }

            List<NetworkRecord> restored;

            try
            {
                restored = JsonSerializer.Deserialize(cloud, DeviceStoreJsonContext.Default.ListNetworkRecord) ?? [];
            }
            catch (JsonException exception)
            {
                _Logger.LogError(exception, "The iCloud copy of the networks could not be read.");
                return;
            }

            if (restored.Count == 0)
            {
                return;
            }

            await _WriteGate.WaitAsync();

            try
            {
                await WriteAsync(_Path, cloud);
                _HasOwnNetworks = true;
            }
            finally
            {
                _WriteGate.Release();
            }

            CloudRestored?.Invoke(this, restored);
        }

        /// <summary>
        /// Reads one file.
        /// </summary>
        /// <param name="path">The file.</param>
        /// <returns>Its networks, or none when it is missing or cannot be read.</returns>
        private async Task<List<NetworkRecord>> ReadAsync(string path)
        {
            if (!File.Exists(path))
            {
                return [];
            }

            try
            {
                await using FileStream stream = File.OpenRead(path);

                return await JsonSerializer.DeserializeAsync(stream, DeviceStoreJsonContext.Default.ListNetworkRecord) ?? [];
            }
            catch (Exception exception) when (exception is IOException or JsonException)
            {
                // A damaged file is kept aside rather than overwritten, so nothing the user named is lost for good.
                _Logger.LogError(exception, "Could not read {Path}; starting without its networks.", path);
                TryMoveAside(path);

                return [];
            }
        }

        /// <summary>
        /// Writes one file. It is replaced in one step, so a crash mid-write leaves the previous
        /// version intact.
        /// </summary>
        /// <param name="path">The file.</param>
        /// <param name="json">Its contents.</param>
        /// <returns>True once written.</returns>
        private async Task<bool> WriteAsync(string path, byte[] json)
        {
            string temporary = $"{path}.tmp";

            try
            {
                await File.WriteAllBytesAsync(temporary, json);
                File.Move(temporary, path, overwrite: true);

                return true;
            }
            catch (IOException exception)
            {
                _Logger.LogError(exception, "Could not save {Path}.", path);

                return false;
            }
        }

        /// <summary>
        /// Keeps a file out of the iCloud device backup, on iOS and Mac. Android leaves it out
        /// through its backup rules instead.
        /// </summary>
        /// <param name="path">The file.</param>
        private static void ExcludeFromBackup(string path)
        {
#if IOS || MACCATALYST
            Foundation.NSUrl.FromFilename(path).SetResource(
                Foundation.NSUrl.IsExcludedFromBackupKey,
                Foundation.NSNumber.FromBoolean(true),
                out _);
#endif
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
        /// <param name="path">The file.</param>
        private static void TryMoveAside(string path)
        {
            try
            {
                File.Move(path, $"{path}.{DateTime.Now:yyyyMMddHHmmss}.bad", overwrite: true);
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
