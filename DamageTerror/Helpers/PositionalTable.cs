using System.IO;
using System.Net.Http;
using Dalamud.Plugin.Services;

namespace DamageTerror.Helpers;

/// <summary>
/// Positional hit/miss detection using a CSV lookup table approach.
///
/// Inspired by and credited to DamageInfoPlugin by perchbirdd:
/// https://github.com/perchbirdd/DamageInfoPlugin
///
/// Each positional action maps (actionId, bonusPercent) → hit or miss.
/// The data is downloaded from a Google Sheets CSV at startup and cached locally.
/// If the download fails, a static fallback table is used.
///
/// Bonus percent is extracted from the top byte of the flags field
/// in ACT log lines 21/22, which corresponds to EffectEntry.param2 (byte 3)
/// from the ActionEffect network packet.
/// </summary>
public class PositionalTable : IDisposable
{
    /// <summary>
    /// Google Sheets CSV URL — same data source used by DamageInfoPlugin.
    /// https://github.com/perchbirdd/DamageInfoPlugin/blob/main/DamageInfoPlugin/Positionals/PositionalManager.cs
    /// </summary>
    private const string SheetUrl =
        "https://docs.google.com/spreadsheets/d/1z2skn_jokyj02Qv2GPEs6HSmAZVLiw2LbwQxkXPjiEs/gviz/tq?tqx=out:csv&sheet=main1";

    private readonly string cachePath;
    private readonly IPluginLog log;
    private readonly HttpClient client = new();
    private Dictionary<int, PositionalAction> actionStore = new();

    private sealed class PositionalAction
    {
        public int Id;
        public string ActionName = string.Empty;
        public string ActionPosition = string.Empty;
        public Dictionary<int, bool> Positionals = new(); // bonusPercent → isHit
    }

    public PositionalTable(string configDirectory, IPluginLog log)
    {
        this.log = log;
        cachePath = Path.Combine(configDirectory, "positionals.csv");
        LoadFallback();
    }

    /// <summary>
    /// Downloads the latest CSV from Google Sheets, caches it, and loads the data.
    /// Falls back to the cached file or embedded fallback if download fails.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            var text = await client.GetStringAsync(SheetUrl).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(text))
            {
                // Only write if content changed
                if (!File.Exists(cachePath) || File.ReadAllText(cachePath) != text)
                    File.WriteAllText(cachePath, text);

                LoadCsv(text);
                log.Debug($"PositionalTable: loaded {actionStore.Count} actions from remote CSV");
                return;
            }
        }
        catch (Exception ex)
        {
            log.Debug($"PositionalTable: remote CSV download failed: {ex.Message}");
        }

        // Try cached file
        if (File.Exists(cachePath))
        {
            try
            {
                var cached = File.ReadAllText(cachePath);
                LoadCsv(cached);
                log.Debug($"PositionalTable: loaded {actionStore.Count} actions from cached CSV");
                return;
            }
            catch (Exception ex)
            {
                log.Debug($"PositionalTable: cached CSV parse failed: {ex.Message}");
            }
        }

        // Fall back to embedded data
        log.Debug($"PositionalTable: using embedded fallback ({actionStore.Count} actions)");
    }

    /// <summary>Re-download and reload the CSV data.</summary>
    public async Task ResetAsync()
    {
        LoadFallback();
        await InitializeAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Parses the CSV text into the action store.
    /// Expected columns: Id, Percent, IsHit, ActionName, ActionPosition, Comment
    /// Mirrors DamageInfoPlugin's PositionalRecord/PositionalManager format.
    /// </summary>
    private void LoadCsv(string csvText)
    {
        var store = new Dictionary<int, PositionalAction>();
        var lines = csvText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Skip header row
        for (var i = 1; i < lines.Length; i++)
        {
            var fields = ParseCsvLine(lines[i]);
            if (fields.Length < 5) continue;

            if (!int.TryParse(fields[0].Trim(), out var id)) continue;
            if (!int.TryParse(fields[1].Trim(), out var percent)) continue;
            var isHit = string.Equals(fields[2].Trim(), "TRUE", StringComparison.OrdinalIgnoreCase);
            var actionName = fields[3].Trim();
            var actionPosition = fields[4].Trim();

            if (!store.TryGetValue(id, out var action))
            {
                action = new PositionalAction
                {
                    Id = id,
                    ActionName = actionName,
                    ActionPosition = actionPosition,
                    Positionals = new Dictionary<int, bool>(),
                };
                store[id] = action;
            }

            action.Positionals[percent] = isHit;
        }

        actionStore = store;
    }

    /// <summary>
    /// Parses a single CSV line, handling quoted fields that may contain commas.
    /// </summary>
    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var inQuotes = false;
        var field = new System.Text.StringBuilder();

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else if (c != '\r')
            {
                field.Append(c);
            }
        }

        fields.Add(field.ToString());
        return fields.ToArray();
    }

    /// <summary>Returns true if the given action ID is a known positional action.</summary>
    public bool IsPositional(uint actionId) => actionStore.ContainsKey((int)actionId);

    /// <summary>
    /// Returns true if the given bonus percent indicates a positional hit for this action.
    /// Mirrors DamageInfoPlugin's PositionalManager.IsPositionalHit().
    /// Returns false for unknown percents — the CSV only contains known hit values,
    /// so any unrecognized percent is treated as "not a confirmed hit".
    /// </summary>
    public bool IsPositionalHit(uint actionId, int bonusPercent)
    {
        if (!actionStore.TryGetValue((int)actionId, out var action)) return false;
        if (action.Positionals.TryGetValue(bonusPercent, out var isHit)) return isHit;
        return false;
    }

    /// <summary>
    /// Returns true if the given bonus percent indicates a missed positional for this action.
    /// Mirrors DamageInfoPlugin: defaults to Failure (miss) for unknown percents.
    /// The CSV only contains confirmed hit values, so unrecognized percents are misses.
    /// </summary>
    public bool IsPositionalMiss(uint actionId, int bonusPercent)
    {
        if (!actionStore.TryGetValue((int)actionId, out var action))
            return false;

        if (action.Positionals.TryGetValue(bonusPercent, out var isHit))
            return !isHit;

        // Unknown percent for a known action: treat as miss (matches DamageInfoPlugin)
        return true;
    }

    public void Dispose()
    {
        client.Dispose();
    }

    private void LoadFallback()
    {
        var store = new Dictionary<int, PositionalAction>();

        void Add(int id, string name, string position, (int percent, bool isHit)[] entries)
        {
            var action = new PositionalAction
            {
                Id = id,
                ActionName = name,
                ActionPosition = position,
                Positionals = new Dictionary<int, bool>(),
            };
            foreach (var (p, h) in entries)
                action.Positionals[p] = h;
            store[id] = action;
        }

        Add(56, "Snap Punch", "Flank", [(0, false), (16, false), (25, false), (17, true), (27, true), (20, true), (30, true)]);
        Add(66, "Demolish", "Rear", [(0, false), (15, true), (18, true)]);
        Add(36947, "Pouncing Coeurl", "Flank", [(0, false), (23, false), (15, true), (18, true), (12, true), (14, true)]);

        Add(3554, "Fang and Claw", "Flank", [(0, false), (53, false), (10, true), (11, true), (58, true), (59, true)]);
        Add(3556, "Wheeling Thrust", "Rear", [(0, false), (53, false), (10, true), (11, true), (58, true), (59, true)]);
        Add(25772, "Chaotic Spring", "Rear", [(0, false), (53, false), (10, true), (11, true), (58, true), (59, true)]);

        Add(2255, "Aeolian Edge", "Rear", [(0, false), (47, false), (23, true), (30, true), (56, true), (59, true)]);
        Add(2258, "Trick Attack", "Rear", [(0, false), (25, true)]);
        Add(3563, "Armor Crush", "Flank", [(0, false), (47, false), (21, true), (27, true), (53, true), (58, true)]);

        Add(7481, "Gekko", "Rear", [(0, false), (53, false), (10, true), (22, true), (11, true), (58, true)]);
        Add(7482, "Kasha", "Flank", [(0, false), (53, false), (10, true), (22, true), (11, true), (58, true)]);

        Add(24382, "Gibbet", "Flank", [(0, false), (10, false), (11, true), (19, true)]);
        Add(24383, "Gallows", "Rear", [(0, false), (10, false), (11, true), (19, true)]);
        Add(36970, "Executioner's Gibbet", "Flank", [(0, false), (7, true)]);
        Add(36971, "Executioner's Gallows", "Rear", [(0, false), (7, true)]);

        Add(34610, "Flanksting Strike", "Flank", [(0, false), (15, true), (12, true)]);
        Add(34611, "Flanksbane Fang", "Flank", [(0, false), (15, true), (12, true)]);
        Add(34612, "Hindsting Strike", "Rear", [(0, false), (15, true), (12, true)]);
        Add(34613, "Hindsbane Fang", "Rear", [(0, false), (15, true), (12, true)]);
        Add(34621, "Hunter's Coil", "Rear", [(0, false), (9, true)]);
        Add(34622, "Swiftskin's Coil", "Flank", [(0, false), (9, true)]);

        actionStore = store;
    }
}
