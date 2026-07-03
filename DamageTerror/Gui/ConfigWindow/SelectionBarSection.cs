namespace DamageTerror.Gui.ConfigWindow;

internal static class SelectionBarSection
{
    public static bool Draw(Configuration config)
    {
        var changed = false;

        if (ImGui.CollapsingHeader("Style", ImGuiTreeNodeFlags.DefaultOpen))
        {
        changed |= ConfigHelpers.ColorEditProp("Text color", config.SelectionBarTextColor, v => config.SelectionBarTextColor = v);
        changed |= ConfigHelpers.ColorEditProp("Background color", config.SelectionBarBackgroundColor, v => config.SelectionBarBackgroundColor = v);

        changed |= ConfigHelpers.SliderFloatProp("Extra padding", config.SelectionBarHeight, 0.0f, 16.0f, "%.0f px", v => config.SelectionBarHeight = v, 200);

        changed |= ConfigHelpers.CheckboxProp("Show separator line", config.ShowSelectionBarSeparator, v => config.ShowSelectionBarSeparator = v);

        if (config.ShowSelectionBarSeparator)
        {
            ImGui.Indent();
            changed |= ConfigHelpers.ColorEditProp("Separator color", config.SelectionBarSeparatorColor, v => config.SelectionBarSeparatorColor = v);
            ImGui.Unindent();
        }
        }

        ImGui.Spacing();

        if (ConfigHelpers.ShiftResetButton("Reset Selection Bar"))
        {
            config.SelectionBarTextColor = new Vector4(1f, 1f, 1f, 1f);
            config.SelectionBarBackgroundColor = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            config.SelectionBarHeight = 0.0f;
            config.ShowEncounterPicker = true;
            config.ShowSelectionBarSeparator = true;
            config.SelectionBarSeparatorColor = new Vector4(0.4f, 0.4f, 0.4f, 0.5f);
            changed = true;
        }

        return changed;
    }
}
