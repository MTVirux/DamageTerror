using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

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
            var activeColor = config.StatusBarActiveColor;
            if (ImGui.ColorEdit4("In combat##statusbar", ref activeColor))
            {
                config.StatusBarActiveColor = activeColor;
                changed = true;
            }

            var inactiveColor = config.StatusBarInactiveColor;
            if (ImGui.ColorEdit4("Out of combat##statusbar", ref inactiveColor))
            {
                config.StatusBarInactiveColor = inactiveColor;
                changed = true;
            }

            var labelColor = config.StatusBarLabelColor;
            if (ImGui.ColorEdit4("Labels##statusbar", ref labelColor))
            {
                config.StatusBarLabelColor = labelColor;
                changed = true;
            }

            var bgColor = config.StatusBarBackgroundColor;
            if (ImGui.ColorEdit4("Background##statusbar", ref bgColor))
            {
                config.StatusBarBackgroundColor = bgColor;
                changed = true;
            }

            if (config.ShowStatusBarSeparator)
            {
                var sepColor = config.StatusBarSeparatorColor;
                if (ImGui.ColorEdit4("Separator##statusbar", ref sepColor))
                {
                    config.StatusBarSeparatorColor = sepColor;
                    changed = true;
                }
            }
        }

        return changed;
    }
}
