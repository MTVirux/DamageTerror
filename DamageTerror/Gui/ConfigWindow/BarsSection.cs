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

            var showJob = config.ShowJobAbbrevOnBar;
            if (ImGui.Checkbox("Job abbreviation text", ref showJob))
            {
                config.ShowJobAbbrevOnBar = showJob;
                changed = true;
            }

            var showRank = config.ShowRankNumber;
            if (ImGui.Checkbox("Rank number", ref showRank))
            {
                config.ShowRankNumber = showRank;
                changed = true;
            }

            var showJobIcons = config.ShowJobIcons;
            if (ImGui.Checkbox("Job icons", ref showJobIcons))
            {
                config.ShowJobIcons = showJobIcons;
                changed = true;
            }

            if (config.ShowJobIcons)
            {
                ImGui.SameLine();
                var styleIdx = (int)config.JobIconStyle;
                var styleLabels = new[] { "Framed", "Plain", "Custom" };
                ImGui.SetNextItemWidth(120);
                if (ImGui.Combo("Icon style", ref styleIdx, styleLabels, styleLabels.Length))
                {
                    config.JobIconStyle = (JobIconStyle)styleIdx;
                    changed = true;
                }

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
        var barHeight = config.BarHeight;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Bar height", ref barHeight, 14.0f, 40.0f, "%.0f px"))
        {
            config.BarHeight = barHeight;
            changed = true;
        }

        var barSpacing = config.BarSpacing;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Bar spacing", ref barSpacing, 0.0f, 8.0f, "%.0f px"))
        {
            config.BarSpacing = barSpacing;
            changed = true;
        }

        var barRounding = config.BarRounding;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Bar rounding", ref barRounding, 0.0f, 12.0f, "%.1f"))
        {
            config.BarRounding = barRounding;
            changed = true;
        }

        var iconSize = config.IconSize;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Icon size", ref iconSize, 10.0f, 32.0f, "%.0f px"))
        {
            config.IconSize = iconSize;
            changed = true;
        }

        var barAlpha = config.BarAlpha;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Bar opacity", ref barAlpha, 0.1f, 1.0f, "%.2f"))
        {
            config.BarAlpha = barAlpha;
            changed = true;
        }

        var barFontSize = config.BarFontSize;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Bar font size", ref barFontSize, 6f, 40f, "%.1fpt"))
        {
            config.BarFontSize = barFontSize;
            changed = true;
        }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Padding", ImGuiTreeNodeFlags.DefaultOpen))
        {
        var barLeftPad = config.BarLeftPadding;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Left padding", ref barLeftPad, 0.0f, 20.0f, "%.0f px"))
        {
            config.BarLeftPadding = barLeftPad;
            changed = true;
        }

        var barRightPad = config.BarRightPadding;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Right padding", ref barRightPad, 0.0f, 20.0f, "%.0f px"))
        {
            config.BarRightPadding = barRightPad;
            changed = true;
        }

        var barColSpacing = config.BarColumnSpacing;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Column spacing", ref barColSpacing, 0.0f, 16.0f, "%.0f px"))
        {
            config.BarColumnSpacing = barColSpacing;
            changed = true;
        }

        var iconTextPad = config.IconTextPadding;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Icon-text padding", ref iconTextPad, 0.0f, 12.0f, "%.0f px"))
        {
            config.IconTextPadding = iconTextPad;
            changed = true;
        }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Self Highlighting", ImGuiTreeNodeFlags.DefaultOpen))
        {
        var selfHighlight = config.SelfBarHighlight;
        if (ImGui.Checkbox("Highlight local player bar", ref selfHighlight))
        {
            config.SelfBarHighlight = selfHighlight;
            changed = true;
        }

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
        var showHeader = config.ShowMeterHeader;
        if (ImGui.Checkbox("Show header row", ref showHeader))
        {
            config.ShowMeterHeader = showHeader;
            changed = true;
        }

        changed |= ConfigHelpers.ColorEditProp("Header text color", config.HeaderTextColor, v => config.HeaderTextColor = v);
        changed |= ConfigHelpers.ColorEditProp("Header background", config.HeaderBackgroundColor, v => config.HeaderBackgroundColor = v);

        var headerHeight = config.HeaderHeight;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Header height", ref headerHeight, 14.0f, 40.0f, "%.0f px"))
        {
            config.HeaderHeight = headerHeight;
            changed = true;
        }

        var headerFontSize = config.HeaderFontSize;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Header font size", ref headerFontSize, 6f, 40f, "%.1fpt"))
        {
            config.HeaderFontSize = headerFontSize;
            changed = true;
        }

        var headerSep = config.HeaderSeparator;
        if (ImGui.Checkbox("Show separator line##header", ref headerSep))
        {
            config.HeaderSeparator = headerSep;
            changed = true;
        }

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
