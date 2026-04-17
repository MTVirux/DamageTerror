using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

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
                catch (Exception ex) { ServiceManager.PluginLog.Error($"Failed to initialize font service: {ex.Message}"); }
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

        var barFont = config.BarFontSize;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Bar text", ref barFont, 6f, 40f, "%.1fpt"))
        {
            config.BarFontSize = barFont;
            changed = true;
        }

        var headerFont = config.HeaderFontSize;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Header text", ref headerFont, 6f, 40f, "%.1fpt"))
        {
            config.HeaderFontSize = headerFont;
            changed = true;
        }

        var statusFont = config.StatusBarFontSize;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Status bar text", ref statusFont, 6f, 40f, "%.1fpt"))
        {
            config.StatusBarFontSize = statusFont;
            changed = true;
        }

        var detailFont = config.DetailFontSize;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Detail panel text", ref detailFont, 6f, 40f, "%.1fpt"))
        {
            config.DetailFontSize = detailFont;
            changed = true;
        }

        var skillFont = config.SkillFontSize;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Skill breakdown text", ref skillFont, 6f, 40f, "%.1fpt"))
        {
            config.SkillFontSize = skillFont;
            changed = true;
        }

        var buffFont = config.BuffFontSize;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Buff/debuff text", ref buffFont, 6f, 40f, "%.1fpt"))
        {
            config.BuffFontSize = buffFont;
            changed = true;
        }

        var graphFont = config.GraphFontSize;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Graph labels (detail)", ref graphFont, 6f, 40f, "%.1fpt"))
        {
            config.GraphFontSize = graphFont;
            changed = true;
        }

        var graphViewFont = config.GraphViewFontSize;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Graph labels (overview)", ref graphViewFont, 6f, 40f, "%.1fpt"))
        {
            config.GraphViewFontSize = graphViewFont;
            changed = true;
        }

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
