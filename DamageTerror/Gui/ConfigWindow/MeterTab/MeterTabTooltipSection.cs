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

        changed |= MetricPicker.Draw("tooltip", tab.TooltipFields,
            MetricPicker.GetTooltipFieldLabel,
            MetricPicker.TooltipFieldCategories,
            field => DrawTooltipExtras(tab, field),
            f => MetricPicker.TooltipFieldDescriptions.GetValueOrDefault(f));

        return changed;
    }

    private static bool DrawTooltipExtras(MeterTab tab, TooltipField field)
    {
        var extChanged = false;
        extChanged |= MeterTabSectionHelpers.DrawLabelOverride(field, "ttLbl_",
            ColumnLabels.DefaultTooltipFieldLabels.GetValueOrDefault(field, field.ToString()),
            tab.TooltipFieldLabels, MetricPicker.GetTooltipFieldLabel(field));
        return extChanged;
    }
}
