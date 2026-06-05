using Dalamud.Bindings.ImGui;
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
            ImGui.SameLine();
            var defaultLabel = Configuration.DefaultTooltipFieldLabels.GetValueOrDefault(field, field.ToString());
            tab.TooltipFieldLabels.TryGetValue(field, out var current);
            current ??= "";
            ImGui.SetNextItemWidth(60);
            if (ImGui.InputTextWithHint($"##ttLbl_{field}", defaultLabel, ref current, 32))
            {
                if (string.IsNullOrEmpty(current))
                    tab.TooltipFieldLabels.Remove(field);
                else
                    tab.TooltipFieldLabels[field] = current;
                extChanged = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(MetricPicker.GetTooltipFieldLabel(field));
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
