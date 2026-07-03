namespace DamageTerror.Gui.ConfigWindow;

internal static class TooltipSection
{
    public static bool Draw(Configuration config)
    {
        var changed = false;

        changed |= ConfigHelpers.CheckboxProp("Show tooltip on hover", config.ShowTooltip, v => config.ShowTooltip = v);

        if (!config.ShowTooltip)
        {
            ImGui.BeginDisabled();
        }

        changed |= ConfigHelpers.SliderFloatProp("Hover delay", config.TooltipDelay, 0.0f, 1.0f, "%.2f s", v => config.TooltipDelay = v, 200);

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Appearance", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= ConfigHelpers.SliderFloatProp("Font size", config.TooltipFontSize, 8f, 24f, "%.1f pt", v => config.TooltipFontSize = v, 200);
            changed |= ConfigHelpers.SliderFloatProp("Rounding", config.TooltipRounding, 0f, 12f, "%.1f", v => config.TooltipRounding = v, 200);
            changed |= ConfigHelpers.SliderFloatProp("Padding", config.TooltipPadding, 0f, 16f, "%.0f px", v => config.TooltipPadding = v, 200);

            ImGui.Spacing();

            changed |= ConfigHelpers.ColorEditProp("Background", config.TooltipBackgroundColor, v => config.TooltipBackgroundColor = v);
            changed |= ConfigHelpers.ColorEditProp("Text Color", config.TooltipTextColor, v => config.TooltipTextColor = v);
            changed |= ConfigHelpers.ColorEditProp("Label Color", config.TooltipLabelColor, v => config.TooltipLabelColor = v);
        }

        if (ImGui.CollapsingHeader("Top Skills"))
        {
            changed |= ConfigHelpers.SliderIntProp("Skills to show", config.TooltipTopSkillCount, 1, 10, v => config.TooltipTopSkillCount = v, 200);
            ConfigHelpers.HelpMarker("Number of top skills to show when \"Top Damage Skills\" or\n\"Top Healing Skills\" tooltip fields are enabled.");
        }

        if (!config.ShowTooltip)
        {
            ImGui.EndDisabled();
        }

        return changed;
    }
}
