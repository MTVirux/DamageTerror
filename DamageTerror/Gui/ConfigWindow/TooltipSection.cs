using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

internal static class TooltipSection
{
    public static bool Draw(Configuration config)
    {
        var changed = false;

        var showTooltip = config.ShowTooltip;
        if (ImGui.Checkbox("Show tooltip on hover", ref showTooltip))
        {
            config.ShowTooltip = showTooltip;
            changed = true;
        }

        if (!config.ShowTooltip)
        {
            ImGui.BeginDisabled();
        }

        var delay = config.TooltipDelay;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Hover delay", ref delay, 0.0f, 1.0f, "%.2f s"))
        {
            config.TooltipDelay = delay;
            changed = true;
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Appearance", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var fontSize = config.TooltipFontSize;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Font size", ref fontSize, 8f, 24f, "%.1f pt"))
            {
                config.TooltipFontSize = fontSize;
                changed = true;
            }

            var rounding = config.TooltipRounding;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Rounding", ref rounding, 0f, 12f, "%.1f"))
            {
                config.TooltipRounding = rounding;
                changed = true;
            }

            var padding = config.TooltipPadding;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Padding", ref padding, 0f, 16f, "%.0f px"))
            {
                config.TooltipPadding = padding;
                changed = true;
            }

            ImGui.Spacing();

            if (ConfigHelpers.ColorEditProp("Background", config.TooltipBackgroundColor, v => config.TooltipBackgroundColor = v))
                changed = true;

            if (ConfigHelpers.ColorEditProp("Text Color", config.TooltipTextColor, v => config.TooltipTextColor = v))
                changed = true;

            if (ConfigHelpers.ColorEditProp("Label Color", config.TooltipLabelColor, v => config.TooltipLabelColor = v))
                changed = true;
        }

        if (ImGui.CollapsingHeader("Top Skills"))
        {
            var skillCount = config.TooltipTopSkillCount;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderInt("Skills to show", ref skillCount, 1, 10))
            {
                config.TooltipTopSkillCount = skillCount;
                changed = true;
            }
            ConfigHelpers.HelpMarker("Number of top skills to show when \"Top Damage Skills\" or\n\"Top Healing Skills\" tooltip fields are enabled.");
        }

        if (!config.ShowTooltip)
        {
            ImGui.EndDisabled();
        }

        return changed;
    }
}
