using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

internal static class MeterTabBasicsSection
{
    private static readonly string[] FilterModeLabels =
    {
        "All",
        "Tanks",
        "Healers",
        "DPS (All)",
        "Melee DPS",
        "Ranged DPS",
        "Caster DPS",
        "Deaths Only",
        "Custom Jobs",
    };

    private static readonly string[] GroupFilterLabels =
    {
        "All",
        "Solo",
        "Party Only",
        "Alliance",
    };

    public static bool Draw(MeterTab tab, ref string renameBuffer)
    {
        var changed = false;

        if (!ImGui.CollapsingHeader("Tab Settings", ImGuiTreeNodeFlags.DefaultOpen))
            return changed;

        ImGui.Spacing();

        ImGui.SetNextItemWidth(200);
        if (ImGui.InputText("Name", ref renameBuffer, 64))
        {
            tab.Name = renameBuffer;
            changed = true;
        }

        var groupBuffer = tab.Group ?? "";
        ImGui.SetNextItemWidth(200);
        if (ImGui.InputText("Group", ref groupBuffer, 64))
        {
            tab.Group = groupBuffer;
            changed = true;
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Assign this tab to a group.\nTabs with the same group string are grouped together.");

        var isHidden = tab.IsHidden;
        if (ImGui.Checkbox("Hidden", ref isHidden))
        {
            tab.IsHidden = isHidden;
            changed = true;
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Hide this tab from the tab bar.\nThe tab still exists and can be used for popout windows.");

        ImGui.Spacing();

        var filterIdx = (int)tab.FilterMode;
        ImGui.SetNextItemWidth(200);
        if (ImGui.Combo("Role Filter", ref filterIdx, FilterModeLabels, FilterModeLabels.Length))
        {
            tab.FilterMode = (TabFilterMode)filterIdx;
            changed = true;
        }

        if (tab.FilterMode == TabFilterMode.Custom)
        {
            ImGui.Spacing();
            changed |= DrawCustomJobFilter(tab);
        }

        var groupFilterIdx = (int)tab.GroupFilter;
        ImGui.SetNextItemWidth(200);
        if (ImGui.Combo("Group Filter", ref groupFilterIdx, GroupFilterLabels, GroupFilterLabels.Length))
        {
            tab.GroupFilter = (GroupFilter)groupFilterIdx;
            changed = true;
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Filter by party membership.\nSolo = only you.\nParty Only = your party members.\nAlliance = all alliance members.\nCombines with Role Filter above.");

        return changed;
    }

    private static bool DrawCustomJobFilter(MeterTab tab)
    {
        var changed = false;

        ImGui.TextUnformatted("Custom job filter:");
        ConfigHelpers.HelpMarker("Select which jobs to include.");
        ImGui.Indent();

        DrawJobGroup("Tanks", JobDataTable.TankJobs, tab, ref changed);
        DrawJobGroup("Healers", JobDataTable.HealerJobs, tab, ref changed);
        DrawJobGroup("Melee DPS", JobDataTable.MeleeDpsJobs, tab, ref changed);
        DrawJobGroup("Ranged DPS", JobDataTable.RangedDpsJobs, tab, ref changed);
        DrawJobGroup("Caster DPS", JobDataTable.CasterDpsJobs, tab, ref changed);

        ImGui.Unindent();
        return changed;
    }

    private static void DrawJobGroup(string groupLabel, string[] jobs, MeterTab tab, ref bool changed)
    {
        if (ImGui.TreeNodeEx(groupLabel, ImGuiTreeNodeFlags.None))
        {
            foreach (var job in jobs)
            {
                var isChecked = tab.CustomJobFilter.Contains(job, StringComparer.OrdinalIgnoreCase);
                var fullName = JobDataTable.GetFullName(job);
                if (ImGui.Checkbox($"{fullName} ({job})##custom_{job}", ref isChecked))
                {
                    if (isChecked)
                    {
                        if (!tab.CustomJobFilter.Contains(job, StringComparer.OrdinalIgnoreCase))
                            tab.CustomJobFilter.Add(job);
                    }
                    else
                    {
                        tab.CustomJobFilter.RemoveAll(j => string.Equals(j, job, StringComparison.OrdinalIgnoreCase));
                    }
                    changed = true;
                }
            }
            ImGui.TreePop();
        }
    }
}
