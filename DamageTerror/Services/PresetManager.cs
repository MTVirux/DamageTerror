using System.IO;
using System.Text.RegularExpressions;
using Dalamud.Plugin.Services;
using DamageTerror.Presets;
using Newtonsoft.Json;

namespace DamageTerror.Services;

/// <summary>
/// Manages built-in and user-created theme presets.
/// Custom presets are stored as individual JSON files in a <c>presets/</c> subdirectory
/// of the plugin's config folder.
/// </summary>
public class PresetManager
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

    /// <summary>Returns all built-in presets.</summary>
    public IReadOnlyList<ThemePreset> BuiltInPresets => BuiltInPresets_All;
    private static readonly ThemePreset[] BuiltInPresets_All = Presets.BuiltInPresets.All;

    /// <summary>Returns all custom (user-saved) presets.</summary>
    public IReadOnlyList<ThemePreset> CustomPresets => customPresets;

    /// <summary>Returns all presets: built-in first, then custom.</summary>
    public IEnumerable<ThemePreset> GetAllPresets()
    {
        foreach (var p in BuiltInPresets_All) yield return p;
        foreach (var p in customPresets) yield return p;
    }

    /// <summary>Saves a custom preset to disk. Overwrites if a preset with the same name already exists.</summary>
    public void SaveCustomPreset(ThemePreset preset)
    {
        preset.IsBuiltIn = false;
        var json = JsonConvert.SerializeObject(preset, JsonSettings);
        var path = GetPresetPath(preset.Name);
        File.WriteAllText(path, json);
        ReloadCustomPresets();
    }

    /// <summary>Deletes a custom preset by name. Returns true if deleted.</summary>
    public bool DeleteCustomPreset(string name)
    {
        var path = GetPresetPath(name);
        if (!File.Exists(path)) return false;
        File.Delete(path);
        ReloadCustomPresets();
        return true;
    }

    /// <summary>Serializes a preset to JSON for clipboard export.</summary>
    public string ExportPreset(ThemePreset preset)
    {
        return JsonConvert.SerializeObject(preset, JsonSettings);
    }

    /// <summary>
    /// Deserializes a preset from JSON. Returns the preset on success, or null with an error message on failure.
    /// </summary>
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

    private string GetPresetPath(string name)
    {
        var safe = SanitizeFileName(name);
        return Path.Combine(presetsDir, safe + ".json");
    }

    private static string SanitizeFileName(string name)
    {
        // Remove invalid filesystem characters, collapse whitespace
        var invalid = new string(Path.GetInvalidFileNameChars());
        var cleaned = Regex.Replace(name, $"[{Regex.Escape(invalid)}]", "");
        cleaned = Regex.Replace(cleaned.Trim(), @"\s+", "_");
        return string.IsNullOrEmpty(cleaned) ? "preset" : cleaned;
    }
}
