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
        changed |= ConfigHelpers.ComboProp("Sort by", (int)tab.SortBy, sortOptions, v => tab.SortBy = (SortField)v, 200);

        changed |= ConfigHelpers.CheckboxProp("Descending (highest first)", tab.SortDescending, v => tab.SortDescending = v);

        return changed;
    }
}
