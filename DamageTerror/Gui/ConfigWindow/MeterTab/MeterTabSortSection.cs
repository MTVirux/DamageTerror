using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

internal static class MeterTabSortSection
{
    public static bool Draw(MeterTab tab)
    {
        var changed = false;

        if (!ImGui.CollapsingHeader("Sort", ImGuiTreeNodeFlags.None))
            return changed;

        var sortOptions = Enum.GetNames(typeof(SortField));
        var currentSort = (int)tab.SortBy;
        ImGui.SetNextItemWidth(200);
        if (ImGui.Combo("Sort by", ref currentSort, sortOptions, sortOptions.Length))
        {
            tab.SortBy = (SortField)currentSort;
            changed = true;
        }

        var sortDesc = tab.SortDescending;
        if (ImGui.Checkbox("Descending (highest first)", ref sortDesc))
        {
            tab.SortDescending = sortDesc;
            changed = true;
        }

        return changed;
    }
}
