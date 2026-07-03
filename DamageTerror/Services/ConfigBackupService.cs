namespace DamageTerror.Services;

public sealed class ConfigBackupService
{
    private const string BackupSuffix = ".bak";
    private const string BrokenSuffixPrefix = ".broken_";
    private const double DefaultBackupCooldownSeconds = 60.0;

    private readonly string configFilePath;
    private readonly string configDir;
    private readonly string configFileName;
    private readonly IPluginLog log;
    private readonly object writeLock = new();

    private DateTime lastBackupUtc = DateTime.MinValue;

    public ConfigBackupService(string configFilePath, IPluginLog log)
    {
        this.configFilePath = configFilePath;
        this.configDir = Path.GetDirectoryName(configFilePath) ?? string.Empty;
        this.configFileName = Path.GetFileName(configFilePath);
        this.log = log;
    }

    public string BackupPath => configFilePath + BackupSuffix;

    public DateTime LastBackupUtc => lastBackupUtc;

    public double BackupCooldownSeconds { get; set; } = DefaultBackupCooldownSeconds;

    /// <summary>
    /// Mirrors the live config file to <c>.bak</c> using an atomic temp+move.
    /// Throttled by <see cref="BackupCooldownSeconds"/> to avoid rewriting on
    /// every save (color-picker drags can fire many saves a second).
    /// Pass <paramref name="force"/> to bypass throttling — used right after
    /// load to seed an initial backup.
    /// </summary>
    public bool WriteBackupFromLiveConfig(bool force = false)
    {
        lock (writeLock)
        {
            if (!force)
            {
                var since = (DateTime.UtcNow - lastBackupUtc).TotalSeconds;
                if (since < BackupCooldownSeconds)
                    return false;
            }

            try
            {
                if (!File.Exists(configFilePath))
                    return false;

                var dst = BackupPath;
                var tmp = dst + ".tmp";
                File.Copy(configFilePath, tmp, overwrite: true);
                File.Move(tmp, dst, overwrite: true);
                lastBackupUtc = DateTime.UtcNow;
                return true;
            }
            catch (Exception ex)
            {
                log.Warning($"[ConfigBackup] Failed to write backup: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Snapshots the live config file to <c>.broken_&lt;ts&gt;</c> so the user
    /// can recover after we replace it with defaults. Returns the path written
    /// (or null on failure / missing source).
    /// </summary>
    public string? SaveBrokenSnapshot()
    {
        try
        {
            if (!File.Exists(configFilePath))
                return null;

            var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var dst = configFilePath + BrokenSuffixPrefix + ts;
            File.Copy(configFilePath, dst, overwrite: true);
            return dst;
        }
        catch (Exception ex)
        {
            log.Warning($"[ConfigBackup] Failed to save broken snapshot: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Returns true if a usable <c>.bak</c> exists; the caller is expected to
    /// then copy it into place and re-trigger the standard load path.
    /// </summary>
    public bool HasBackup() => File.Exists(BackupPath);

    /// <summary>
    /// Replaces the live config file with the chosen recovery file. The plugin
    /// must be reloaded to pick up the change — we don't try to swap the
    /// in-memory Configuration instance because it's referenced from ~50 other
    /// places.
    /// </summary>
    public bool RestoreFromFile(string sourcePath)
    {
        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            return false;

        lock (writeLock)
        {
            try
            {
                var tmp = configFilePath + ".restore.tmp";
                File.Copy(sourcePath, tmp, overwrite: true);
                File.Move(tmp, configFilePath, overwrite: true);
                log.Information($"[ConfigBackup] Restored config from {sourcePath}");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"[ConfigBackup] Failed to restore from {sourcePath}: {ex.Message}");
                return false;
            }
        }
    }

    public bool DeleteRecoveryFile(string path)
    {
        try
        {
            if (string.Equals(path, configFilePath, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!path.StartsWith(configFilePath, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!File.Exists(path))
                return false;

            File.Delete(path);
            return true;
        }
        catch (Exception ex)
        {
            log.Warning($"[ConfigBackup] Failed to delete {path}: {ex.Message}");
            return false;
        }
    }

    public List<RecoveryEntry> ListRecoveryFiles()
    {
        var result = new List<RecoveryEntry>();
        if (string.IsNullOrEmpty(configDir) || !Directory.Exists(configDir))
            return result;

        try
        {
            foreach (var path in Directory.EnumerateFiles(configDir, configFileName + ".*"))
            {
                var fileName = Path.GetFileName(path);
                var suffix = fileName.Substring(configFileName.Length);

                RecoveryKind kind;
                if (string.Equals(suffix, BackupSuffix, StringComparison.OrdinalIgnoreCase))
                    kind = RecoveryKind.Backup;
                else if (suffix.StartsWith(BrokenSuffixPrefix, StringComparison.OrdinalIgnoreCase))
                    kind = RecoveryKind.Broken;
                else
                    continue;

                var info = new FileInfo(path);
                result.Add(new RecoveryEntry(
                    FilePath: path,
                    Kind: kind,
                    Timestamp: info.LastWriteTimeUtc,
                    SizeBytes: info.Length));
            }
        }
        catch (Exception ex)
        {
            log.Warning($"[ConfigBackup] Failed to enumerate recovery files: {ex.Message}");
        }

        result.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
        return result;
    }
}

public enum RecoveryKind
{
    Backup,
    Broken,
}

public sealed record RecoveryEntry(string FilePath, RecoveryKind Kind, DateTime Timestamp, long SizeBytes);
