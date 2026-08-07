
namespace DamageTerror.Services;

/// <summary>
/// Reads, writes, and deletes per-encounter sidecar files at
/// <c>&lt;baseDir&gt;/&lt;subdirectory&gt;/&lt;id&gt;.json</c>. Each sidecar contains a
/// single <typeparamref name="T"/>. Failures are logged on the
/// <see cref="LogChannel.EncounterStore"/> channel and otherwise swallowed -
/// the caller falls back to a no-sidecar view.
/// </summary>
public class SidecarStore<T> where T : class
{
    private readonly string baseDirectory;
    private readonly string subdirectoryName;
    private readonly object cacheLock = new();
    private readonly JsonSerializerSettings? serializerSettings;
    private long cachedTotalBytes = -1;
    private int cachedFileCount = -1;

    public SidecarStore(string configFilePath, string subdirectoryName, JsonSerializerSettings? settings = null)
    {
        var configDir = Path.GetDirectoryName(configFilePath)
                        ?? throw new ArgumentException("configFilePath must contain a directory", nameof(configFilePath));
        baseDirectory = Path.Combine(configDir, subdirectoryName);
        this.subdirectoryName = subdirectoryName;
        serializerSettings = settings;
        CleanupTempFiles();
    }

    /// <summary>Remove .tmp files left behind by a crash mid-write.</summary>
    private void CleanupTempFiles()
    {
        try
        {
            if (!Directory.Exists(baseDirectory)) return;
            foreach (var f in Directory.EnumerateFiles(baseDirectory, "*.tmp"))
                File.Delete(f);
        }
        catch { }
    }

    public string DirectoryPath => baseDirectory;

    public string PathFor(long encounterId)
        => Path.Combine(baseDirectory, $"{encounterId}.json");

    public T? Load(long encounterId)
    {
        var path = PathFor(encounterId);
        if (!File.Exists(path))
            return null;
        try
        {
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (Exception ex)
        {
            ServiceManager.LogWarning(LogChannel.EncounterStore,
                $"Failed to read {subdirectoryName} sidecar {path}: {ex.Message}");
            return null;
        }
    }

    public bool Save(long encounterId, T value)
    {
        try
        {
            Directory.CreateDirectory(baseDirectory);
            var path = PathFor(encounterId);
            var tmp = path + ".tmp";
            var json = JsonConvert.SerializeObject(value, serializerSettings);
            File.WriteAllText(tmp, json);
            File.Move(tmp, path, overwrite: true);
            InvalidateSizeCache();
            return true;
        }
        catch (Exception ex)
        {
            ServiceManager.LogWarning(LogChannel.EncounterStore,
                $"Failed to write {subdirectoryName} sidecar for {encounterId}: {ex.Message}");
            return false;
        }
    }

    public bool Delete(long encounterId)
    {
        var path = PathFor(encounterId);
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                InvalidateSizeCache();
                return true;
            }
        }
        catch (Exception ex)
        {
            ServiceManager.LogWarning(LogChannel.EncounterStore,
                $"Failed to delete {subdirectoryName} sidecar {path}: {ex.Message}");
        }
        return false;
    }

    /// <summary>Size of a single encounter's sidecar file, or 0 if it has none.</summary>
    public long SizeBytes(long encounterId)
    {
        try
        {
            var info = new FileInfo(PathFor(encounterId));
            return info.Exists ? info.Length : 0;
        }
        catch { return 0; }
    }

    public long TotalSizeBytes()
    {
        lock (cacheLock)
        {
            if (cachedTotalBytes < 0)
                RefreshSizeCacheLocked();
            return cachedTotalBytes;
        }
    }

    public int FileCount()
    {
        lock (cacheLock)
        {
            if (cachedFileCount < 0)
                RefreshSizeCacheLocked();
            return cachedFileCount;
        }
    }

    private void RefreshSizeCacheLocked()
    {
        long total = 0;
        var count = 0;
        try
        {
            if (Directory.Exists(baseDirectory))
            {
                foreach (var f in Directory.EnumerateFiles(baseDirectory, "*.json"))
                {
                    total += new FileInfo(f).Length;
                    count++;
                }
            }
        }
        catch { }
        cachedTotalBytes = total;
        cachedFileCount = count;
    }

    private void InvalidateSizeCache()
    {
        lock (cacheLock)
        {
            cachedTotalBytes = -1;
            cachedFileCount = -1;
        }
    }

    /// <summary>Return all sidecar IDs currently present on disk.</summary>
    public IEnumerable<long> EnumerateIds()
    {
        if (!Directory.Exists(baseDirectory))
            yield break;
        foreach (var f in Directory.EnumerateFiles(baseDirectory, "*.json"))
        {
            var name = Path.GetFileNameWithoutExtension(f);
            if (long.TryParse(name, out var id))
                yield return id;
        }
    }
}
