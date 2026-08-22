namespace DamageTerror.Services;

public sealed class ConfigBackupService
{
    private const string BackupSuffix = ".bak";
    private const string ExportMetaKey = "$DamageTerrorExport";
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

    /// <summary>
    /// Writes the live config file to a user-chosen destination, keeping only the
    /// selected categories (null exports everything). Callers are expected to save
    /// the in-memory config first so the file is current.
    /// </summary>
    public bool ExportToFile(string destPath, IReadOnlyCollection<ConfigCategory>? categories = null)
    {
        if (string.IsNullOrEmpty(destPath))
            return false;

        lock (writeLock)
        {
            try
            {
                if (!File.Exists(configFilePath))
                    return false;

                var dir = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                if (categories == null || categories.Count == ConfigCategories.All.Count)
                {
                    File.Copy(configFilePath, destPath, overwrite: true);
                    log.Information($"[ConfigBackup] Exported config to {destPath}");
                    return true;
                }

                var root = JObject.Parse(File.ReadAllText(configFilePath));
                KeepOnly(root, ConfigCategories.PropertiesFor(categories), string.Empty);

                root[ExportMetaKey] = new JArray(categories.Select(c => c.ToString()));

                File.WriteAllText(destPath, root.ToString(Formatting.Indented));
                log.Information($"[ConfigBackup] Exported {categories.Count} config categories to {destPath}");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"[ConfigBackup] Failed to export to {destPath}: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Merges an imported file into the live config file: keys present in the
    /// import win, everything else is left alone. A full export carries every key,
    /// so this matches the old replace-the-file behaviour for those.
    /// </summary>
    public bool MergeFromFile(string sourcePath)
    {
        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            return false;

        lock (writeLock)
        {
            try
            {
                var source = JObject.Parse(File.ReadAllText(sourcePath));
                var isPartial = source.Remove(ExportMetaKey);

                JObject target;
                if (File.Exists(configFilePath))
                {
                    target = JObject.Parse(File.ReadAllText(configFilePath));
                    // A partial import can't speak for settings it doesn't carry, so the
                    // stamps this install was migrated against stay as they are.
                    if (isPartial)
                    {
                        foreach (var name in ConfigCategories.MetadataProperties)
                            source.Remove(name);
                    }
                }
                else
                {
                    target = new JObject();
                }

                if (isPartial)
                    MergeInto(target, source, string.Empty);
                else
                    foreach (var prop in source.Properties())
                        target[prop.Name] = prop.Value;

                var tmp = configFilePath + ".import.tmp";
                File.WriteAllText(tmp, target.ToString(Formatting.Indented));
                File.Move(tmp, configFilePath, overwrite: true);
                log.Information($"[ConfigBackup] Merged {source.Count} settings from {sourcePath}");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"[ConfigBackup] Failed to import from {sourcePath}: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Strips everything the selection doesn't cover, descending into a block the
    /// categories split rather than taking it whole. Keys the serializer owns (<c>$type</c>)
    /// are left where they are.
    /// </summary>
    private static void KeepOnly(JObject node, IReadOnlySet<string> keep, string prefix)
    {
        foreach (var name in node.Properties().Select(p => p.Name).ToList())
        {
            if (name.StartsWith('$'))
                continue;

            var path = prefix.Length == 0 ? name : prefix + "." + name;
            if (keep.Contains(path))
                continue;

            if (node[name] is JObject child && keep.Any(k => k.StartsWith(path + ".", StringComparison.Ordinal)))
                KeepOnly(child, keep, path);
            else
                node.Remove(name);
        }
    }

    /// <summary>
    /// Applies a partial file key by key. A block the categories split is merged into
    /// rather than replaced, so importing one party list section leaves the others alone;
    /// anything else - a dictionary of per-metric styles, say - is replaced whole, because
    /// the export carried all of it.
    /// </summary>
    private static void MergeInto(JObject target, JObject source, string prefix)
    {
        foreach (var prop in source.Properties())
        {
            if (prop.Name.StartsWith('$'))
                continue;

            var path = prefix.Length == 0 ? prop.Name : prefix + "." + prop.Name;
            if (ConfigCategories.IsContainer(path) &&
                prop.Value is JObject childSource &&
                target[prop.Name] is JObject childTarget)
            {
                MergeInto(childTarget, childSource, path);
            }
            else
            {
                target[prop.Name] = prop.Value;
            }
        }
    }

    /// <summary>
    /// What an import file would bring in, for the confirmation prompt. Files written
    /// by an older build carry no category marker, so fall back to reading the keys.
    /// </summary>
    public ImportContents InspectImportFile(string path)
    {
        try
        {
            var root = JObject.Parse(File.ReadAllText(path));
            var marker = root[ExportMetaKey] as JArray;
            var settingCount = root.Properties().Count(p => p.Name != ExportMetaKey);
            var configVersion = (int?)root[nameof(Configuration.Version)];
            var gameVersion = (string?)root[nameof(Configuration.LastGameVersion)];

            if (marker != null)
            {
                var listed = marker
                    .Select(t => Enum.TryParse<ConfigCategory>(t.ToString(), out var c) ? c : (ConfigCategory?)null)
                    .OfType<ConfigCategory>()
                    .ToList();
                return new ImportContents(listed, IsPartial: true, settingCount, configVersion, gameVersion);
            }

            var found = CategoriesIn(root, string.Empty).Distinct().ToList();
            return new ImportContents(found, IsPartial: false, settingCount, configVersion, gameVersion);
        }
        catch (Exception ex)
        {
            log.Warning($"[ConfigBackup] Failed to inspect {path}: {ex.Message}");
            return new ImportContents([], IsPartial: false, 0, null, null);
        }
    }

    /// <summary>
    /// Sanity check before an imported file is allowed to replace the live one:
    /// it has to parse as a <see cref="Configuration"/> and carry a version.
    /// </summary>
    public bool IsValidConfigFile(string path, out string? error)
    {
        error = null;

        try
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                error = "File not found.";
                return false;
            }

            var json = File.ReadAllText(path);
            if (JObject.Parse(json)["Version"] == null)
            {
                error = "Not a Damage Terror configuration file.";
                return false;
            }

            if (JsonConvert.DeserializeObject<Configuration>(json) == null)
            {
                error = "Configuration could not be parsed.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static IEnumerable<ConfigCategory> CategoriesIn(JObject node, string prefix)
    {
        foreach (var prop in node.Properties())
        {
            var path = prefix.Length == 0 ? prop.Name : prefix + "." + prop.Name;
            if (ConfigCategories.IsContainer(path) && prop.Value is JObject child)
            {
                foreach (var category in CategoriesIn(child, path))
                    yield return category;
            }
            else if (ConfigCategories.Of(path) is { } found)
            {
                yield return found;
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

public sealed record ImportContents(
    IReadOnlyList<ConfigCategory> Categories,
    bool IsPartial,
    int SettingCount,
    int? ConfigVersion,
    string? GameVersion);
