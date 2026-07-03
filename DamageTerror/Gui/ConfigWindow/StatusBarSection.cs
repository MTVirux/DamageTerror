namespace DamageTerror.Gui.ConfigWindow;

internal static class StatusBarSection
{
    public static bool Draw(Configuration config)
    {
        var changed = false;

        if (ImGui.CollapsingHeader("Options##statusbar", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= ConfigHelpers.CheckboxProp("Show separator line", config.ShowStatusBarSeparator, v => config.ShowStatusBarSeparator = v);
            changed |= ConfigHelpers.SliderFloatProp("Height##statusbar", config.StatusBarHeight, 14f, 40f, "%.0f", v => config.StatusBarHeight = v, 150);
            changed |= ConfigHelpers.SliderFloatProp("Font size##statusbar", config.StatusBarFontSize, 6f, 40f, "%.1fpt", v => config.StatusBarFontSize = v, 150);
            changed |= ConfigHelpers.SliderFloatProp("Padding##statusbar", config.StatusBarPadding, 0f, 20f, "%.0f px", v => config.StatusBarPadding = v, 150);
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Colors##statusbar", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= ConfigHelpers.ColorEditProp("In combat##statusbar", config.StatusBarActiveColor, v => config.StatusBarActiveColor = v, ImGuiColorEditFlags.None);
            changed |= ConfigHelpers.ColorEditProp("Out of combat##statusbar", config.StatusBarInactiveColor, v => config.StatusBarInactiveColor = v, ImGuiColorEditFlags.None);
            changed |= ConfigHelpers.ColorEditProp("Labels##statusbar", config.StatusBarLabelColor, v => config.StatusBarLabelColor = v, ImGuiColorEditFlags.None);
            changed |= ConfigHelpers.ColorEditProp("Background##statusbar", config.StatusBarBackgroundColor, v => config.StatusBarBackgroundColor = v, ImGuiColorEditFlags.None);

            if (config.ShowStatusBarSeparator)
            {
                changed |= ConfigHelpers.ColorEditProp("Separator##statusbar", config.StatusBarSeparatorColor, v => config.StatusBarSeparatorColor = v, ImGuiColorEditFlags.None);
            }
        }

        return changed;
    }
}
