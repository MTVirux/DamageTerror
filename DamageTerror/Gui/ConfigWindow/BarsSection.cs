using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

internal static class BarsSection
{
    public static bool Draw(Configuration config)
    {
        var changed = false;

        if (ImGui.CollapsingHeader("Naming", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Spacing();

            changed |= ConfigHelpers.CheckboxProp("Job abbreviation text", config.ShowJobAbbrevOnBar, v => config.ShowJobAbbrevOnBar = v);
            changed |= ConfigHelpers.CheckboxProp("Rank number", config.ShowRankNumber, v => config.ShowRankNumber = v);
            changed |= ConfigHelpers.CheckboxProp("Job icons", config.ShowJobIcons, v => config.ShowJobIcons = v);

            if (config.ShowJobIcons)
            {
                ImGui.SameLine();
                var styleLabels = new[] { "Framed", "Plain", "Custom" };
                changed |= ConfigHelpers.ComboProp("Icon style", (int)config.JobIconStyle, styleLabels, v => config.JobIconStyle = (JobIconStyle)v, 120);

                if (config.JobIconStyle == JobIconStyle.Custom)
                {
                    ImGui.Indent();
                    ImGui.TextUnformatted("Custom Icons");
                    ConfigHelpers.HelpMarker("Set a game icon ID per job (0 = default framed).");
                    ImGui.Spacing();

                    foreach (var abbr in JobIconHelper.AllJobAbbreviations.OrderBy(a => a))
                    {
                        config.CustomJobIcons.TryGetValue(abbr, out var curId);
                        var idInt = (int)curId;
                        ImGui.SetNextItemWidth(100);
                        if (ImGui.InputInt($"{abbr.ToUpperInvariant()}##custicon_{abbr}", ref idInt, 0))
                        {
                            if (idInt < 0) idInt = 0;
                            config.CustomJobIcons[abbr] = (uint)idInt;
                            changed = true;
                        }

                        if (idInt > 0)
                        {
                            ImGui.SameLine();
                            var preview = ServiceManager.TextureProvider.GetFromGameIcon(new GameIconLookup((uint)idInt));
                            if (preview.TryGetWrap(out var wrap, out _))
                            {
                                ImGui.Image(wrap.Handle, new Vector2(ImGui.GetTextLineHeight(), ImGui.GetTextLineHeight()));
                            }
                        }
                    }

                    ImGui.Unindent();
                }
            }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Dimensions", ImGuiTreeNodeFlags.DefaultOpen))
        {
        changed |= ConfigHelpers.SliderFloatProp("Bar height", config.BarHeight, 14.0f, 40.0f, "%.0f px", v => config.BarHeight = v, 200);
        changed |= ConfigHelpers.SliderFloatProp("Bar spacing", config.BarSpacing, 0.0f, 8.0f, "%.0f px", v => config.BarSpacing = v, 200);
        changed |= ConfigHelpers.SliderFloatProp("Bar rounding", config.BarRounding, 0.0f, 12.0f, "%.1f", v => config.BarRounding = v, 200);
        changed |= ConfigHelpers.SliderFloatProp("Icon size", config.IconSize, 10.0f, 32.0f, "%.0f px", v => config.IconSize = v, 200);
        changed |= ConfigHelpers.SliderFloatProp("Bar opacity", config.BarAlpha, 0.1f, 1.0f, "%.2f", v => config.BarAlpha = v, 200);
        changed |= ConfigHelpers.SliderFloatProp("Bar font size", config.BarFontSize, 6f, 40f, "%.1fpt", v => config.BarFontSize = v, 200);
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Padding", ImGuiTreeNodeFlags.DefaultOpen))
        {
        changed |= ConfigHelpers.SliderFloatProp("Left padding", config.BarLeftPadding, 0.0f, 20.0f, "%.0f px", v => config.BarLeftPadding = v, 200);
        changed |= ConfigHelpers.SliderFloatProp("Right padding", config.BarRightPadding, 0.0f, 20.0f, "%.0f px", v => config.BarRightPadding = v, 200);
        changed |= ConfigHelpers.SliderFloatProp("Column spacing", config.BarColumnSpacing, 0.0f, 16.0f, "%.0f px", v => config.BarColumnSpacing = v, 200);
        changed |= ConfigHelpers.SliderFloatProp("Icon-text padding", config.IconTextPadding, 0.0f, 12.0f, "%.0f px", v => config.IconTextPadding = v, 200);
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Self Highlighting", ImGuiTreeNodeFlags.DefaultOpen))
        {
        changed |= ConfigHelpers.CheckboxProp("Highlight local player bar", config.SelfBarHighlight, v => config.SelfBarHighlight = v);

        if (config.SelfBarHighlight)
        {
            ImGui.Indent();
            changed |= ConfigHelpers.ColorEditProp("Accent color", config.SelfBarHighlightColor, v => config.SelfBarHighlightColor = v);
            ImGui.Unindent();
        }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Colors", ImGuiTreeNodeFlags.DefaultOpen))
        {
        changed |= ConfigHelpers.ColorEditProp("Name text", config.NameTextColor, v => config.NameTextColor = v);
        changed |= ConfigHelpers.ColorEditProp("Value text", config.ValueTextColor, v => config.ValueTextColor = v);
        changed |= ConfigHelpers.ColorEditProp("Bar background", config.BarBackgroundColor, v => config.BarBackgroundColor = v);
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Header Row", ImGuiTreeNodeFlags.DefaultOpen))
        {
        changed |= ConfigHelpers.CheckboxProp("Show header row", config.ShowMeterHeader, v => config.ShowMeterHeader = v);

        changed |= ConfigHelpers.ColorEditProp("Header text color", config.HeaderTextColor, v => config.HeaderTextColor = v);
        changed |= ConfigHelpers.ColorEditProp("Header background", config.HeaderBackgroundColor, v => config.HeaderBackgroundColor = v);

        changed |= ConfigHelpers.SliderFloatProp("Header height", config.HeaderHeight, 14.0f, 40.0f, "%.0f px", v => config.HeaderHeight = v, 200);
        changed |= ConfigHelpers.SliderFloatProp("Header font size", config.HeaderFontSize, 6f, 40f, "%.1fpt", v => config.HeaderFontSize = v, 200);

        changed |= ConfigHelpers.CheckboxProp("Show separator line##header", config.HeaderSeparator, v => config.HeaderSeparator = v);

        if (config.HeaderSeparator)
        {
            ImGui.Indent();
            changed |= ConfigHelpers.ColorEditProp("Separator color##header", config.HeaderSeparatorColor, v => config.HeaderSeparatorColor = v);
            ImGui.Unindent();
        }

        ImGui.Spacing();

        if (ConfigHelpers.ShiftResetButton("Reset Header"))
        {
            config.HeaderTextColor = new Vector4(0.7f, 0.7f, 0.7f, 0.9f);
            config.HeaderBackgroundColor = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            config.HeaderHeight = 22.0f;
            config.HeaderFontSize = 14f;
            config.HeaderSeparator = false;
            config.HeaderSeparatorColor = new Vector4(0.4f, 0.4f, 0.4f, 0.5f);
            changed = true;
        }
        }

        return changed;
    }
}
