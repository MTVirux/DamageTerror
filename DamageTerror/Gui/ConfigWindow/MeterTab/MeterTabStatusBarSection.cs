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

        var sbTimer = tab.ShowStatusBarTimer;
        if (ImGui.Checkbox("Show combat timer##sbtab", ref sbTimer))
        {
            tab.ShowStatusBarTimer = sbTimer;
            changed = true;
        }

        var colorOverride = tab.StatusBarColorOverridesActive;
        if (ImGui.Checkbox("Custom colors override active color##sbColorOverride", ref colorOverride))
        {
            tab.StatusBarColorOverridesActive = colorOverride;
            changed = true;
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("When enabled, per-metric custom colors are used even during active encounters.\nWhen disabled, active encounters always use the active encounter color.");

        ImGui.Spacing();
        ImGui.TextDisabled("Metrics");
        tab.StatusBarMetrics ??= new List<BarColumn> { BarColumn.Dps, BarColumn.EncDps };
        Func<BarColumn, bool> sbExtras = col =>
        {
            var extChanged = false;
            ImGui.SameLine();
            var defaultLabel = Configuration.DefaultHeaderLabels.GetValueOrDefault(col, col.ToString());
            tab.StatusBarMetricLabels.TryGetValue(col, out var current);
            current ??= "";
            ImGui.SetNextItemWidth(60);
            if (ImGui.InputTextWithHint($"##sbLbl_{col}", defaultLabel, ref current, 32))
            {
                if (string.IsNullOrEmpty(current))
                    tab.StatusBarMetricLabels.Remove(col);
                else
                    tab.StatusBarMetricLabels[col] = current;
                extChanged = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(MetricPicker.GetBarColumnLabel(col));
            ImGui.SameLine();
            var hasColor = tab.ColumnValueColors.ContainsKey(col);
            if (hasColor)
                ImGui.PushStyleColor(ImGuiCol.Text, tab.ColumnValueColors[col]);
            if (ImGui.SmallButton($"C##sbClr_{col}"))
                ImGui.OpenPopup($"##sbClrPopup_{col}");
            if (hasColor)
                ImGui.PopStyleColor();
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(hasColor ? "Custom value color (click to edit)" : "Set custom value color");
            if (ImGui.BeginPopup($"##sbClrPopup_{col}"))
            {
                extChanged |= MeterTabSectionHelpers.DrawColumnColorPopup(col, tab.ColumnValueColors);
                ImGui.EndPopup();
            }
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
