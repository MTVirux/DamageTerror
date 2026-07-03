namespace DamageTerror.Gui.ConfigWindow;

internal static class FontSection
{
    public static bool Draw(Configuration config, FontService? fontService, IUiBuilder? uiBuilder)
    {
        var changed = false;

        if (ImGui.CollapsingHeader("Font Selection", ImGuiTreeNodeFlags.DefaultOpen))
        {
        var enableFont = config.EnableCustomFont;
        if (ImGui.Checkbox("Enable custom font", ref enableFont))
        {
            config.EnableCustomFont = enableFont;
            changed = true;
            if (enableFont && fontService != null && uiBuilder != null && !fontService.IsInitialized)
            {
                try { fontService.Initialize(uiBuilder); }
                catch (Exception ex) { ServiceManager.LogError(LogChannel.FontService, $"Failed to initialize font service: {ex.Message}"); }
            }
        }

        ImGui.TextWrapped("When enabled, allows loading a custom system font. Disable if you experience crashes.");

        ImGui.Spacing();
        ImGui.TextDisabled("Font Selection");

        if (!config.EnableCustomFont)
        {
            ImGui.BeginDisabled();
            ImGui.Button("Choose Font...");
            ConfigHelpers.HelpMarker("Enable custom font above to use font selection.");
            ImGui.EndDisabled();
        }
        else
        {
            var fontName = config.CustomFontDisplayName ?? "Dalamud Default";
            ImGui.Text($"Current: {fontName}");

            if (config.CustomFontSpecJson != null)
            {
                ImGui.SameLine();
                ImGui.TextDisabled($"({config.CustomFontSizePt:F0}pt)");
            }

            if (fontService != null)
            {
                if (ImGui.Button("Choose Font..."))
                {
                    fontService.OpenFontChooser();
                }

                ImGui.SameLine();

                if (config.CustomFontSpecJson != null && ConfigHelpers.ShiftResetButton("Reset to Default"))
                {
                    fontService.ClearCustomFont();
                    changed = true;
                }

                if (fontService.DrawFontChooser())
                    changed = true;
            }
        }

        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Font Sizes", ImGuiTreeNodeFlags.DefaultOpen))
        {
        ImGui.TextWrapped("Set the font size for each component independently.");

        changed |= ConfigHelpers.SliderFloatProp("Bar text", config.BarFontSize, 6f, 40f, "%.1fpt", v => config.BarFontSize = v, 200);
        changed |= ConfigHelpers.SliderFloatProp("Header text", config.HeaderFontSize, 6f, 40f, "%.1fpt", v => config.HeaderFontSize = v, 200);
        changed |= ConfigHelpers.SliderFloatProp("Status bar text", config.StatusBarFontSize, 6f, 40f, "%.1fpt", v => config.StatusBarFontSize = v, 200);
        changed |= ConfigHelpers.SliderFloatProp("Detail panel text", config.DetailFontSize, 6f, 40f, "%.1fpt", v => config.DetailFontSize = v, 200);
        changed |= ConfigHelpers.SliderFloatProp("Skill breakdown text", config.SkillFontSize, 6f, 40f, "%.1fpt", v => config.SkillFontSize = v, 200);
        changed |= ConfigHelpers.SliderFloatProp("Buff/debuff text", config.BuffFontSize, 6f, 40f, "%.1fpt", v => config.BuffFontSize = v, 200);
        changed |= ConfigHelpers.SliderFloatProp("Graph labels (detail)", config.GraphFontSize, 6f, 40f, "%.1fpt", v => config.GraphFontSize = v, 200);
        changed |= ConfigHelpers.SliderFloatProp("Graph labels (overview)", config.GraphViewFontSize, 6f, 40f, "%.1fpt", v => config.GraphViewFontSize = v, 200);

        ImGui.Spacing();

        if (ConfigHelpers.ShiftResetButton("Reset Sizes"))
        {
            config.BarFontSize = 14f;
            config.HeaderFontSize = 14f;
            config.StatusBarFontSize = 14f;
            config.DetailFontSize = 14f;
            config.SkillFontSize = 14f;
            config.BuffFontSize = 14f;
            config.GraphFontSize = 14f;
            config.GraphViewFontSize = 14f;
            changed = true;
        }
        }

        return changed;
    }
}
