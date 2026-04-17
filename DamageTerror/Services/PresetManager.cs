using System.IO;
using System.Text.RegularExpressions;
using Dalamud.Plugin.Services;
using DamageTerror.Presets;
using Newtonsoft.Json;

namespace DamageTerror.Services;

public sealed class PresetManager
{
    private readonly string presetsDir;
    private readonly IPluginLog log;
    private List<ThemePreset> customPresets = new();

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
    };

    public PresetManager(string configDir, IPluginLog log)
    {
        this.log = log;
        this.presetsDir = Path.Combine(configDir, "presets");
        Directory.CreateDirectory(this.presetsDir);
        ReloadCustomPresets();
    }

    public IReadOnlyList<ThemePreset> BuiltInPresets => BuiltInPresets_All;
    private static readonly ThemePreset[] BuiltInPresets_All = Presets.BuiltInPresets.All;

    public IReadOnlyList<ThemePreset> CustomPresets => customPresets;

    public IEnumerable<ThemePreset> GetAllPresets()
    {
        foreach (var p in BuiltInPresets_All) yield return p;
        foreach (var p in customPresets) yield return p;
    }

    public void SaveCustomPreset(ThemePreset preset)
    {
        if (IsBuiltInName(preset.Name)) return;
        preset.IsBuiltIn = false;
        var json = JsonConvert.SerializeObject(preset, JsonSettings);
        var path = GetPresetPath(preset.Name);
        var tempPath = path + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, path, overwrite: true);
        ReloadCustomPresets();
    }

    public bool DeleteCustomPreset(string name)
    {
        if (IsBuiltInName(name)) return false;
        var path = GetPresetPath(name);
        if (!File.Exists(path)) return false;
        File.Delete(path);
        ReloadCustomPresets();
        return true;
    }

    public string ExportPreset(ThemePreset preset)
    {
        return JsonConvert.SerializeObject(preset, JsonSettings);
    }

    public ThemePreset? ImportPreset(string json, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            error = "Clipboard is empty.";
            return null;
        }

        try
        {
            var preset = JsonConvert.DeserializeObject<ThemePreset>(json, JsonSettings);
            if (preset == null)
            {
                error = "Failed to parse preset JSON.";
                return null;
            }

            preset.IsBuiltIn = false;
            if (string.IsNullOrWhiteSpace(preset.Name))
                preset.Name = "Imported Preset";

            return preset;
        }
        catch (JsonException ex)
        {
            error = $"Invalid JSON: {ex.Message}";
            return null;
        }
    }

    private void ReloadCustomPresets()
    {
        var list = new List<ThemePreset>();
        if (!Directory.Exists(presetsDir)) return;

        foreach (var file in Directory.GetFiles(presetsDir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var preset = JsonConvert.DeserializeObject<ThemePreset>(json, JsonSettings);
                if (preset != null)
                {
                    preset.IsBuiltIn = false;
                    list.Add(preset);
                }
            }
            catch (Exception ex)
            {
                log.Warning($"[PresetManager] Failed to load preset {file}: {ex.Message}");
            }
        }

        list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        customPresets = list;
    }

    private bool IsBuiltInName(string name)
    {
        return BuiltInPresets_All.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private string GetPresetPath(string name)
    {
        var safe = SanitizeFileName(name);
        return Path.Combine(presetsDir, safe + ".json");
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = new string(Path.GetInvalidFileNameChars());
        var cleaned = Regex.Replace(name, $"[{Regex.Escape(invalid)}]", "");
        cleaned = Regex.Replace(cleaned.Trim(), @"\s+", "_");
        return string.IsNullOrEmpty(cleaned) ? "preset" : cleaned;
    }
}
