using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

/// <summary>
/// Encounter History tab: search, browse, and manage past encounters.
/// </summary>
public class EncounterHistoryTab
{
    private readonly DamageTerrorPlugin plugin;
    private string historySearchFilter = string.Empty;

    public EncounterHistoryTab(DamageTerrorPlugin plugin)
    {
        this.plugin = plugin;
    }

    public void Draw()
    {
        var store = plugin.DataService.Store;
        var history = store.History;

        ImGui.TextDisabled($"Encounter history is saved automatically and persists across restarts.");
        ImGui.TextDisabled($"{history.Count} encounter(s) stored.");
        ImGui.Spacing();

        if (history.Count == 0)
        {
            ImGui.TextUnformatted("No encounters in history yet.");
            return;
        }

        // Search bar
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##historySearch", "Search by zone, title, player, or job...", ref historySearchFilter, 256);
        ImGui.Spacing();

        // Clear all button
        if (ImGui.Button("Clear All History"))
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

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        int removeIdx = -1;

        var filter = historySearchFilter.Trim();

        for (int i = history.Count - 1; i >= 0; i--)
        {
            var enc = history[i];
            var encounter = enc.Encounter;
            var label = $"{encounter.ZoneName}";
            if (!string.IsNullOrEmpty(encounter.Title) && encounter.Title != encounter.ZoneName)
                label = $"{encounter.Title} — {encounter.ZoneName}";
            if (string.IsNullOrEmpty(label))
                label = "Unknown";

            // Apply search filter
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
                ImGui.TextUnformatted($"{encounter.EncDps:F1}");
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "  HPS:");
                ImGui.SameLine();
                ImGui.TextUnformatted($"{encounter.EncHps:F1}");

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
                        var cHeader = $"{jobTag}{c.Name}  —  DPS: {c.EncDps:F1}  HPS: {c.EncHps:F1}  Deaths: {c.Deaths}";

                        if (ImGui.TreeNodeEx(cHeader, ImGuiTreeNodeFlags.None))
                        {
                            if (c.Skills.Count > 0)
                            {
                                ImGui.TextColored(new Vector4(1f, 0.8f, 0.5f, 1f), "Damage Skills:");
                                ImGui.Indent(8f);
                                foreach (var s in c.Skills.OrderByDescending(s => s.TotalDamage))
                                {
                                    ImGui.TextUnformatted(
                                        $"{s.Name}  —  {s.TotalDamage:N0} ({s.DamagePercent:F1}%)  Hits: {s.HitCount}  C: {s.CritPct:F1}%  DH: {s.DirectHitPct:F1}%  CDH: {s.CritDirectHitPct:F1}%");
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
                                        $"{s.Name}  —  {s.TotalDamage:N0} ({s.DamagePercent:F1}%)  Hits: {s.HitCount}");
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
}
