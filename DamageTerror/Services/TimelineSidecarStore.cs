using Newtonsoft.Json;

namespace DamageTerror.Services;

/// <summary>
/// Reads, writes, and deletes per-encounter timeline sidecar files at
/// <c>&lt;baseDir&gt;/timelines/&lt;id&gt;.json</c>. Each sidecar contains a
/// <see cref="TimelineBundle"/>. Failures are logged on the
/// <see cref="LogChannel.EncounterStore"/> channel and otherwise swallowed —
/// the caller falls back to a no-timeline view.
/// </summary>
public sealed class TimelineSidecarStore
{
    private readonly string baseDirectory;

    public TimelineSidecarStore(string configFilePath)
    {
        var configDir = System.IO.Path.GetDirectoryName(configFilePath)
                        ?? throw new ArgumentException("configFilePath must contain a directory", nameof(configFilePath));
        baseDirectory = System.IO.Path.Combine(configDir, "timelines");
    }

    public string DirectoryPath => baseDirectory;

    public string PathFor(long encounterId)
        => System.IO.Path.Combine(baseDirectory, $"{encounterId}.json");

    public TimelineBundle? Load(long encounterId)
    {
        var path = PathFor(encounterId);
        if (!System.IO.File.Exists(path))
            return null;
        try
        {
            var json = System.IO.File.ReadAllText(path);
            return JsonConvert.DeserializeObject<TimelineBundle>(json);
        }
        catch (Exception ex)
        {
            ServiceManager.LogWarning(LogChannel.EncounterStore,
                $"Failed to read timeline sidecar {path}: {ex.Message}");
            return null;
        }
    }

    public bool Save(TimelineBundle bundle)
    {
        try
        {
            System.IO.Directory.CreateDirectory(baseDirectory);
            var path = PathFor(bundle.EncounterId);
            var json = JsonConvert.SerializeObject(bundle);
            System.IO.File.WriteAllText(path, json);
            return true;
        }
        catch (Exception ex)
        {
            ServiceManager.LogWarning(LogChannel.EncounterStore,
                $"Failed to write timeline sidecar for {bundle.EncounterId}: {ex.Message}");
            return false;
        }
    }

    public bool Delete(long encounterId)
    {
        var path = PathFor(encounterId);
        try
        {
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
                return true;
            }
        }
        catch (Exception ex)
        {
            ServiceManager.LogWarning(LogChannel.EncounterStore,
                $"Failed to delete timeline sidecar {path}: {ex.Message}");
        }
        return false;
    }

    public long TotalSizeBytes()
    {
        try
        {
            if (!System.IO.Directory.Exists(baseDirectory)) return 0;
            long total = 0;
            foreach (var f in System.IO.Directory.EnumerateFiles(baseDirectory, "*.json"))
                total += new System.IO.FileInfo(f).Length;
            return total;
        }
        catch { return 0; }
    }

    public int FileCount()
    {
        try
        {
            if (!System.IO.Directory.Exists(baseDirectory)) return 0;
            return System.IO.Directory.EnumerateFiles(baseDirectory, "*.json").Count();
        }
        catch { return 0; }
    }

    /// <summary>Return all sidecar IDs currently present on disk.</summary>
    public IEnumerable<long> EnumerateIds()
    {
        if (!System.IO.Directory.Exists(baseDirectory))
            yield break;
        foreach (var f in System.IO.Directory.EnumerateFiles(baseDirectory, "*.json"))
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(f);
            if (long.TryParse(name, out var id))
                yield return id;
        }
    }
}
