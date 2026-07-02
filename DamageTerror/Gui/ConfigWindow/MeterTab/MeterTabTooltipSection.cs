using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

internal static class MeterTabTooltipSection
{
    public static bool Draw(MeterTab tab)
    {
        var changed = false;

        if (!ImGui.CollapsingHeader("Tooltip Content", ImGuiTreeNodeFlags.None))
            return changed;

        changed |= ConfigHelpers.SliderIntProp("Top skills to show", tab.TooltipTopSkillCount, 1, 10, v => tab.TooltipTopSkillCount = v, 200);
        ImGui.Spacing();

        Func<TooltipField, bool> tooltipExtras = field =>
        {
            var extChanged = false;
            extChanged |= MeterTabSectionHelpers.DrawLabelOverride(field, "ttLbl_",
                ColumnLabels.DefaultTooltipFieldLabels.GetValueOrDefault(field, field.ToString()),
                tab.TooltipFieldLabels, MetricPicker.GetTooltipFieldLabel(field));
            return extChanged;
        };
        changed |= MetricPicker.Draw("tooltip", tab.TooltipFields,
            MetricPicker.GetTooltipFieldLabel,
            MetricPicker.TooltipFieldCategories,
            tooltipExtras,
            f => MetricPicker.TooltipFieldDescriptions.GetValueOrDefault(f));

        return changed;
    }
}
