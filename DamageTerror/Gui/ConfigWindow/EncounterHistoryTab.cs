namespace DamageTerror.Gui.ConfigWindow;

public sealed class EncounterHistoryTab
{
    private readonly DamageTerrorPlugin plugin;
    private string historySearchFilter = string.Empty;
    private int pendingLimitValue;
    private int pendingTimelineLimitValue;
    private string importJson = string.Empty;
    private string importFilePath = string.Empty;
    private string? importError;
    private string? statusMessage;
    private DateTime statusMessageTime;
    private int pendingDeleteIndex = -1;
    private bool deleteConfirmOpenedThisFrame;

    public EncounterHistoryTab(DamageTerrorPlugin plugin)
    {
        this.plugin = plugin;
        var config = plugin.Config;
        SyncPending(config.HistoryLimitMode, config.MaxEncounterHistory, config.MaxEncounterHistoryDays, ref pendingLimitValue);
        SyncPending(config.TimelineRetentionMode, config.MaxTimelineCount, config.MaxTimelineDays, ref pendingTimelineLimitValue);
    }

    private static void SyncPending(HistoryLimitMode mode, int countValue, int daysValue, ref int pending)
    {
        pending = mode == HistoryLimitMode.Count ? countValue : daysValue;
    }

    private void DrawRetentionLimit(
        string label,
        string modeComboId,
        string countInputId,
        string daysInputId,
        string applyButtonId,
        ref int pending,
        HistoryLimitMode mode,
        Action<HistoryLimitMode> onModeChanged,
        int countMin,
        int daysMin,
        Action<int> applyCount,
        Action<int> applyDays,
        Func<string> summary)
    {
        var config = plugin.Config;
        var store = plugin.DataService.Store;

        var modeInt = (int)mode;
        ImGui.TextUnformatted(label);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        if (ImGui.Combo(modeComboId, ref modeInt, "Count\0Days\0"))
        {
            mode = (HistoryLimitMode)modeInt;
            onModeChanged(mode);
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        var inputLabel = mode == HistoryLimitMode.Count ? countInputId : daysInputId;
        if (ImGui.InputInt(inputLabel, ref pending))
        {
            var min = mode == HistoryLimitMode.Count ? countMin : daysMin;
            pending = Math.Max(min, pending);
        }

        ImGui.SameLine();
        if (ImGui.Button(applyButtonId))
        {
            if (mode == HistoryLimitMode.Count)
                applyCount(Math.Max(countMin, pending));
            else
                applyDays(Math.Max(daysMin, pending));

            config.Save?.Invoke();
            store.PruneHistory();
            store.Save(force: true);
        }

        ImGui.TextDisabled(summary());
    }

    public void Draw()
    {
        var config = plugin.Config;
        var store = plugin.DataService.Store;
        var history = store.History;

        var summaryMb = store.StorageSizeBytes / (1024.0 * 1024.0);
        var timelinesMb = store.TimelineStorageSizeBytes / (1024.0 * 1024.0);
        var totalMb = summaryMb + timelinesMb;
        ImGui.TextDisabled(
            $"{history.Count} encounter(s) stored — encounters.json {summaryMb:F2} MB · {store.TimelineFileCount} timelines ({timelinesMb:F2} MB) · total {totalMb:F2} MB");
        ConfigHelpers.HelpMarker("Encounter history is saved automatically and persists across restarts.");
#if DEBUG
        if (!config.HideDebugFeatures)
        {
            ImGui.TextColored(new Vector4(1f, 0.85f, 0.3f, 1f),
                "Please be aware that raw log lines are stored in the debug build and may contain DM and Linkshell messages.");
            ImGui.TextColored(new Vector4(1f, 0.85f, 0.3f, 1f),
                "Be careful when sharing.");
        }
#endif
        ImGui.Spacing();

        DrawRetentionLimit(
            "Limit history by:",
            "##historyLimitMode",
            "##historyMaxCount",
            "##historyMaxDays",
            "Apply##historyLimitApply",
            ref pendingLimitValue,
            config.HistoryLimitMode,
            onModeChanged: m => { config.HistoryLimitMode = m; config.Save?.Invoke(); SyncPending(config.HistoryLimitMode, config.MaxEncounterHistory, config.MaxEncounterHistoryDays, ref pendingLimitValue); },
            countMin: 1,
            daysMin: 1,
            applyCount: v => config.MaxEncounterHistory = v,
            applyDays: v => config.MaxEncounterHistoryDays = v,
            summary: () => config.HistoryLimitMode == HistoryLimitMode.Count
                ? $"Currently keeping up to {config.MaxEncounterHistory} encounter(s)."
                : $"Currently keeping encounters from the last {config.MaxEncounterHistoryDays} day(s).");

        DrawRetentionLimit(
            "Limit timelines by:",
            "##timelineRetentionMode",
            "##maxTimelineCount",
            "##maxTimelineDays",
            "Apply##timelineLimitApply",
            ref pendingTimelineLimitValue,
            config.TimelineRetentionMode,
            onModeChanged: m => { config.TimelineRetentionMode = m; config.Save?.Invoke(); SyncPending(config.TimelineRetentionMode, config.MaxTimelineCount, config.MaxTimelineDays, ref pendingTimelineLimitValue); },
            countMin: 0,
            daysMin: 1,
            applyCount: v => config.MaxTimelineCount = v,
            applyDays: v => config.MaxTimelineDays = v,
            summary: () => config.TimelineRetentionMode == HistoryLimitMode.Count
                ? $"Currently keeping up to {config.MaxTimelineCount} timeline(s)."
                : $"Currently keeping timelines from the last {config.MaxTimelineDays} day(s).");

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
            importFilePath = string.Empty;
            importError = null;
        }

        if (ImGui.BeginPopup("##importEncounter"))
        {
            ImGui.TextUnformatted("Import from file (recommended for large encounters):");
            ImGui.SetNextItemWidth(350);
            ImGui.InputTextWithHint("##importFilePath", "Path to .json file...", ref importFilePath, 1024);
            ImGui.SameLine();
            if (ImGui.Button("Import File"))
            {
                if (string.IsNullOrWhiteSpace(importFilePath))
                {
                    importError = "No file path provided.";
                }
                else if (!File.Exists(importFilePath.Trim()))
                {
                    importError = "File not found.";
                }
                else
                {
                    try
                    {
                        var fileJson = File.ReadAllText(importFilePath.Trim());
                        var result = store.ImportEncounter(fileJson, out var error);
                        if (result != null)
                        {
                            store.Save(force: true);
                            importFilePath = string.Empty;
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
                    catch (IOException ex)
                    {
                        importError = $"Failed to read file: {ex.Message}";
                    }
                }
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.TextUnformatted("Or paste exported encounter JSON:");
            ImGui.SetNextItemWidth(400);
            ImGui.InputTextMultiline("##importJsonInput", ref importJson, 1024 * 1024 * 16, new Vector2(400, 200));

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

            if (!EncounterSearchHelper.MatchesFilter(enc, filter))
                continue;

            var header = $"[{enc.Timestamp.ToLocalTime():yyyy-MM-dd HH:mm}]  {label}  ({encounter.Duration})";

            ImGui.PushID(i);
            if (ImGui.TreeNodeEx(header, ImGuiTreeNodeFlags.None))
            {
                plugin.DataService.Store.EnsureTimelineLoaded(enc);

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
                    pendingDeleteIndex = i;
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
                    var path = Path.Combine(exportsDir, filename);

                    // Avoid overwriting — append a counter if needed.
                    var counter = 1;
                    while (File.Exists(path))
                    {
                        var numbered = SanitizeFilename($"{encounter.ZoneName}_{enc.Timestamp.ToLocalTime():yyyy-MM-dd_HH-mm}_{counter}") + ".json";
                        path = Path.Combine(exportsDir, numbered);
                        counter++;
                    }

                    File.WriteAllText(path, json);
                    SetStatus($"Saved to {path}");
                }

                ImGui.SameLine();
                var canReplay = (enc.SkillEvents.Count > 0 || enc.GraphData.Count > 0)
                                && DurationHelper.ParseDuration(encounter.Duration, 0f) > 0.5f;
                ImGui.BeginDisabled(!canReplay);
                if (ImGui.SmallButton("Replay"))
                {
                    plugin.DataService.Store.LoadReplay(enc);
                    SetStatus("Replay started — see main window.");
                }
                ImGui.EndDisabled();
                if (!canReplay && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    ImGui.SetTooltip("Encounter has no replayable timeline data.");

#if DEBUG
                if (!plugin.Config.HideDebugFeatures)
                {
                    if (enc.RawLogLines.Count > 0)
                    {
                        ImGui.SameLine();
                        if (ImGui.SmallButton($"Recalculate##{i}"))
                        {
                            plugin.DataService.RecalculateFromLogLines(enc);
                            SetStatus("Recalculated from raw log lines!");
                        }
                    }
                    if (enc.RawCombatDataFrames.Count > 0)
                    {
                        ImGui.SameLine();
                        if (ImGui.SmallButton($"Replay CombatData##{i}"))
                        {
                            plugin.DataService.ReplayCombatData(enc);
                            SetStatus($"Replayed {enc.RawCombatDataFrames.Count} CombatData frames — see plugin log.");
                        }
                    }
                }
#endif

                ImGui.TreePop();
            }
            ImGui.PopID();
        }

        if (pendingDeleteIndex != -1 && !deleteConfirmOpenedThisFrame)
        {
            ImGui.OpenPopup("##confirmDeleteEncounterTab");
            deleteConfirmOpenedThisFrame = true;
        }

        var modalOpen = true;
        if (ImGui.BeginPopupModal("##confirmDeleteEncounterTab", ref modalOpen,
            ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
        {
            ImGui.TextUnformatted("Remove this encounter?");
            ImGui.TextDisabled("This cannot be undone.");
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.65f, 0.2f, 0.2f, 1f));
            if (ImGui.Button("Remove", new Vector2(100, 0)))
            {
                if (pendingDeleteIndex >= 0)
                {
                    store.RemoveHistory(pendingDeleteIndex);
                    store.Save(force: true);
                }
                pendingDeleteIndex = -1;
                deleteConfirmOpenedThisFrame = false;
                ImGui.CloseCurrentPopup();
            }
            ImGui.PopStyleColor();

            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(100, 0)))
            {
                pendingDeleteIndex = -1;
                deleteConfirmOpenedThisFrame = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
        else if (!modalOpen)
        {
            pendingDeleteIndex = -1;
            deleteConfirmOpenedThisFrame = false;
        }
    }

    private void SetStatus(string message)
    {
        statusMessage = message;
        statusMessageTime = DateTime.UtcNow;
    }

    private static string SanitizeFilename(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new System.Text.StringBuilder(name.Length);
        foreach (var c in name)
            sanitized.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
        return sanitized.ToString();
    }
}
