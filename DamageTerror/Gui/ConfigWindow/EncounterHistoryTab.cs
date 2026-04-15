using Dalamud.Bindings.ImGui;
using Newtonsoft.Json;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

public class EncounterHistoryTab
{
    private readonly DamageTerrorPlugin plugin;
    private string historySearchFilter = string.Empty;
    private int pendingLimitValue;
    private string importJson = string.Empty;
    private string? importError;
    private string? statusMessage;
    private DateTime statusMessageTime;

    public EncounterHistoryTab(DamageTerrorPlugin plugin)
    {
        this.plugin = plugin;
        SyncPendingValue();
    }

    private void SyncPendingValue()
    {
        var config = plugin.Config;
        pendingLimitValue = config.HistoryLimitMode == HistoryLimitMode.Count
            ? config.MaxEncounterHistory
            : config.MaxEncounterHistoryDays;
    }

    public void Draw()
    {
        var config = plugin.Config;
        var store = plugin.DataService.Store;
        var history = store.History;

        ImGui.TextDisabled($"{history.Count} encounter(s) stored.  ({FormatSize(store.StorageSizeBytes)})");
        ConfigHelpers.HelpMarker("Encounter history is saved automatically and persists across restarts.");
#if DEBUG
        ImGui.TextColored(new Vector4(1f, 0.85f, 0.3f, 1f),
            "Please be aware that raw log lines are stored in the debug build and may contain DM and Linkshell messages.");
        ImGui.TextColored(new Vector4(1f, 0.85f, 0.3f, 1f),
            "Be careful when sharing.");
#endif
        ImGui.Spacing();

        // --- History limit settings ---
        var modeInt = (int)config.HistoryLimitMode;
        ImGui.TextUnformatted("Limit history by:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        if (ImGui.Combo("##historyLimitMode", ref modeInt, "Count\0Days\0"))
        {
            config.HistoryLimitMode = (HistoryLimitMode)modeInt;
            config.Save?.Invoke();
            SyncPendingValue();
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        var inputLabel = config.HistoryLimitMode == HistoryLimitMode.Count ? "##historyMaxCount" : "##historyMaxDays";
        if (ImGui.InputInt(inputLabel, ref pendingLimitValue))
        {
            pendingLimitValue = Math.Max(1, pendingLimitValue);
        }

        ImGui.SameLine();
        if (ImGui.Button("Apply##historyLimitApply"))
        {
            if (config.HistoryLimitMode == HistoryLimitMode.Count)
                config.MaxEncounterHistory = Math.Max(1, pendingLimitValue);
            else
                config.MaxEncounterHistoryDays = Math.Max(1, pendingLimitValue);

            config.Save?.Invoke();
            store.PruneHistory();
            store.Save(force: true);
        }

        if (config.HistoryLimitMode == HistoryLimitMode.Count)
            ImGui.TextDisabled($"Currently keeping up to {config.MaxEncounterHistory} encounter(s).");
        else
            ImGui.TextDisabled($"Currently keeping encounters from the last {config.MaxEncounterHistoryDays} day(s).");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##historySearch", "Search by zone, title, player, or job...", ref historySearchFilter, 256);
        ImGui.Spacing();

        if (history.Count > 0 && ImGui.Button("Clear All History"))
        {
            ImGui.OpenPopup("##confirmClearHistory");
        }

        if (ImGui.BeginPopup("##confirmClearHistory"))
        {
            ImGui.TextUnformatted("Delete all encounter history?");
            if (ImGui.Button("Yes, clear all"))
            {
                store.Clear();
                store.Save(force: true);
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }

        if (history.Count > 0)
            ImGui.SameLine();
        if (ImGui.Button("Import Encounter"))
        {
            ImGui.OpenPopup("##importEncounter");
            importJson = string.Empty;
            importError = null;
        }

        if (ImGui.BeginPopup("##importEncounter"))
        {
            ImGui.TextUnformatted("Paste exported encounter JSON:");
            ImGui.SetNextItemWidth(400);
            ImGui.InputTextMultiline("##importJsonInput", ref importJson, 1024 * 512, new Vector2(400, 200));

            if (ImGui.Button("Paste from Clipboard"))
            {
                var clip = ImGui.GetClipboardText();
                if (!string.IsNullOrEmpty(clip))
                    importJson = clip;
            }

            ImGui.SameLine();
            if (ImGui.Button("Import"))
            {
                if (string.IsNullOrWhiteSpace(importJson))
                {
                    importError = "No JSON provided.";
                }
                else
                {
                    var result = store.ImportEncounter(importJson, out var error);
                    if (result != null)
                    {
                        store.Save(force: true);
                        importJson = string.Empty;
                        importError = null;
                        SetStatus("Encounter imported successfully!");
                        ImGui.CloseCurrentPopup();
                    }
                    else
                    {
                        importError = error;
                    }
                }
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel##importCancel"))
            {
                ImGui.CloseCurrentPopup();
            }

            if (!string.IsNullOrEmpty(importError))
            {
                ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), importError);
            }

            ImGui.EndPopup();
        }

        // Status message display
        if (statusMessage != null)
        {
            if ((DateTime.UtcNow - statusMessageTime).TotalSeconds > 3)
            {
                statusMessage = null;
            }
            else
            {
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.3f, 1f, 0.3f, 1f), statusMessage);
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (history.Count == 0)
        {
            ImGui.TextUnformatted("No encounters in history yet.");
            return;
        }

        int removeIdx = -1;

        var filter = historySearchFilter.Trim();

        for (int i = history.Count - 1; i >= 0; i--)
        {
            var enc = history[i];
            var encounter = enc.Encounter;
            var label = $"{encounter.ZoneName}";
            if (!string.IsNullOrEmpty(encounter.Title) && encounter.Title != encounter.ZoneName)
                label = $"{encounter.Title} \u2014 {encounter.ZoneName}";
            if (string.IsNullOrEmpty(label))
                label = "Unknown";

            if (filter.Length > 0
                && !encounter.ZoneName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                && !(encounter.Title?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
                && !enc.Combatants.Any(c => c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                    || c.Job.Contains(filter, StringComparison.OrdinalIgnoreCase)))
                continue;

            var header = $"[{enc.Timestamp.ToLocalTime():yyyy-MM-dd HH:mm}]  {label}  ({encounter.Duration})";

            ImGui.PushID(i);
            if (ImGui.TreeNodeEx(header, ImGuiTreeNodeFlags.None))
            {
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "Duration:");
                ImGui.SameLine();
                ImGui.TextUnformatted(encounter.Duration);

                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "Raid DPS:");
                ImGui.SameLine();
                ImGui.TextUnformatted(ValueFormatter.Format(encounter.EncDps, plugin.Config));
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "  HPS:");
                ImGui.SameLine();
                ImGui.TextUnformatted(ValueFormatter.Format(encounter.EncHps, plugin.Config));

                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "Deaths:");
                ImGui.SameLine();
                ImGui.TextUnformatted($"{encounter.Deaths}");

                if (enc.Combatants.Count > 0)
                {
                    ImGui.Spacing();
                    ImGui.TextDisabled("Combatants:");
                    foreach (var c in enc.Combatants.OrderByDescending(c => c.EncDps))
                    {
                        var jobTag = !string.IsNullOrEmpty(c.Job) ? $"[{c.Job.ToUpperInvariant()}] " : "";
                        var cHeader = $"{jobTag}{c.Name}  —  DPS: {ValueFormatter.Format(c.EncDps, plugin.Config)}  HPS: {ValueFormatter.Format(c.EncHps, plugin.Config)}  Deaths: {c.Deaths}";

                        if (ImGui.TreeNodeEx(cHeader, ImGuiTreeNodeFlags.None))
                        {
                            if (c.Skills.Count > 0)
                            {
                                ImGui.TextColored(new Vector4(1f, 0.8f, 0.5f, 1f), "Damage Skills:");
                                ImGui.Indent(8f);
                                foreach (var s in c.Skills.OrderByDescending(s => s.TotalDamage))
                                {
                                    ImGui.TextUnformatted(
                                        $"{s.Name}  —  {ValueFormatter.Format(s.TotalDamage, plugin.Config)} ({ValueFormatter.FormatPercent(s.DamagePercent, plugin.Config.PercentDecimalPlaces)})  Hits: {s.HitCount}  C: {ValueFormatter.FormatPercent(s.CritPct, plugin.Config.PercentDecimalPlaces)}  DH: {ValueFormatter.FormatPercent(s.DirectHitPct, plugin.Config.PercentDecimalPlaces)}  CDH: {ValueFormatter.FormatPercent(s.CritDirectHitPct, plugin.Config.PercentDecimalPlaces)}");
                                }
                                ImGui.Unindent(8f);
                            }

                            if (c.HealingSkills.Count > 0)
                            {
                                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), "Healing Skills:");
                                ImGui.Indent(8f);
                                foreach (var s in c.HealingSkills.OrderByDescending(s => s.TotalDamage))
                                {
                                    ImGui.TextUnformatted(
                                        $"{s.Name}  —  {ValueFormatter.Format(s.TotalDamage, plugin.Config)} ({ValueFormatter.FormatPercent(s.DamagePercent, plugin.Config.PercentDecimalPlaces)})  Hits: {s.HitCount}");
                                }
                                ImGui.Unindent(8f);
                            }

                            if (c.Skills.Count == 0 && c.HealingSkills.Count == 0)
                            {
                                ImGui.TextDisabled("No skill data recorded.");
                            }

                            ImGui.TreePop();
                        }
                    }
                }

                ImGui.Spacing();
                if (ImGui.SmallButton("Delete"))
                {
                    removeIdx = i;
                }

                ImGui.SameLine();
                if (ImGui.SmallButton("Copy to Clipboard"))
                {
                    var json = store.ExportEncounter(enc);
                    ImGui.SetClipboardText(json);
                    SetStatus("Copied to clipboard!");
                }

                ImGui.SameLine();
                if (ImGui.SmallButton("Save to File"))
                {
                    var json = store.ExportEncounter(enc);
                    var exportsDir = store.GetExportsDirectory();
                    var filename = SanitizeFilename($"{encounter.ZoneName}_{enc.Timestamp.ToLocalTime():yyyy-MM-dd_HH-mm}") + ".json";
                    var path = System.IO.Path.Combine(exportsDir, filename);

                    // Avoid overwriting — append a counter if needed.
                    var counter = 1;
                    while (System.IO.File.Exists(path))
                    {
                        var numbered = SanitizeFilename($"{encounter.ZoneName}_{enc.Timestamp.ToLocalTime():yyyy-MM-dd_HH-mm}_{counter}") + ".json";
                        path = System.IO.Path.Combine(exportsDir, numbered);
                        counter++;
                    }

                    System.IO.File.WriteAllText(path, json);
                    SetStatus($"Saved to {path}");
                }

#if DEBUG
                ImGui.SameLine();
                var hasLogLines = enc.RawLogLines != null && enc.RawLogLines.Count > 0;
                if (!hasLogLines) ImGui.BeginDisabled();
                if (ImGui.SmallButton("Recalculate"))
                {
                    plugin.DataService.ReprocessEncounterLogLines(enc);
                    store.Save(force: true);
                    SetStatus("Encounter recalculated!");
                }
                if (!hasLogLines) ImGui.EndDisabled();
                if (!hasLogLines)
                {
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                        ImGui.SetTooltip("No raw log lines stored for this encounter.");
                }
#endif

                ImGui.TreePop();
            }
            ImGui.PopID();
        }

        if (removeIdx >= 0)
        {
            store.RemoveHistory(removeIdx);
            store.Save(force: true);
        }
    }

    private void SetStatus(string message)
    {
        statusMessage = message;
        statusMessageTime = DateTime.UtcNow;
    }

    private static string SanitizeFilename(string name)
    {
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var sanitized = new System.Text.StringBuilder(name.Length);
        foreach (var c in name)
            sanitized.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
        return sanitized.ToString();
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }
}
