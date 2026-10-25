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
    /// when history (PRD 6.12) makes the file large. The file is also what Android Auto Backup and the
    /// sync folder (PRD 6.16) will carry.
    /// </remarks>
    public sealed class DeviceStore
    {
        #region Variables

        private const string FILE_NAME = "devices.json";

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
