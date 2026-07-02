using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

internal static class MeterTabStatusBarSection
{
    public static bool Draw(MeterTab tab)
    {
        var changed = false;

        if (!ImGui.CollapsingHeader("Status Bar Content", ImGuiTreeNodeFlags.None))
            return changed;

        changed |= ConfigHelpers.CheckboxProp("Show combat timer##sbtab", tab.ShowStatusBarTimer, v => tab.ShowStatusBarTimer = v);

        changed |= ConfigHelpers.CheckboxProp("Custom colors override active color##sbColorOverride", tab.StatusBarColorOverridesActive, v => tab.StatusBarColorOverridesActive = v);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("When enabled, per-metric custom colors are used even during active encounters.\nWhen disabled, active encounters always use the active encounter color.");

        ImGui.Spacing();
        ImGui.TextDisabled("Metrics");
        tab.StatusBarMetrics ??= new List<BarColumn> { BarColumn.Dps, BarColumn.EncDps };
        Func<BarColumn, bool> sbExtras = col =>
        {
            var extChanged = false;
            extChanged |= MeterTabSectionHelpers.DrawLabelOverride(col, "sbLbl_",
                Configuration.DefaultHeaderLabels.GetValueOrDefault(col, col.ToString()),
                tab.StatusBarMetricLabels, MetricPicker.GetBarColumnLabel(col));
            extChanged |= MeterTabSectionHelpers.DrawColorButton(col, "sbClr", tab.ColumnValueColors);
            return extChanged;
        };
        changed |= MetricPicker.Draw("statusBar", tab.StatusBarMetrics,
            MetricPicker.GetBarColumnLabel,
            MetricPicker.BarColumnCategories,
            sbExtras,
            c => MetricPicker.BarColumnDescriptions.GetValueOrDefault(c));

        return changed;
    }
}
