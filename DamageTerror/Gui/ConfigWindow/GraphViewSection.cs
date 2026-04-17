using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

internal static class GraphViewSection
{
    public static bool Draw(Configuration config)
    {
        var changed = false;

        ImGui.TextWrapped("Configure the appearance of the graph view mode. Each tab can be switched to graph view in the Tabs settings page.");
        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Dimensions", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var autoViewHeight = config.GraphViewAutoHeight;
            if (ImGui.Checkbox("Auto-fit height##graphview", ref autoViewHeight))
            {
                config.GraphViewAutoHeight = autoViewHeight;
                changed = true;
            }

            if (!config.GraphViewAutoHeight)
            {
                var height = config.GraphViewHeight;
                ImGui.SetNextItemWidth(200);
                if (ImGui.SliderFloat("Graph height", ref height, 100f, 600f, "%.0f px"))
                {
                    config.GraphViewHeight = height;
                    changed = true;
                }
            }

            var lineThickness = config.GraphViewLineThickness;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Line thickness", ref lineThickness, 1f, 6f, "%.1f"))
            {
                config.GraphViewLineThickness = lineThickness;
                changed = true;
            }

            var gvFontSize = config.GraphViewFontSize;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Font size##graphview", ref gvFontSize, 6f, 40f, "%.1fpt"))
            {
                config.GraphViewFontSize = gvFontSize;
                changed = true;
            }

            var gvSmoothing = config.GraphViewSmoothingWindow;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Smoothing window##graphview", ref gvSmoothing, 1f, 30f, "%.0f sec"))
            {
                config.GraphViewSmoothingWindow = gvSmoothing;
                changed = true;
            }

            var gvUpdateInterval = config.GraphViewUpdateInterval;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Update interval##graphview", ref gvUpdateInterval, 0.1f, 2f, "%.2f sec"))
            {
                config.GraphViewUpdateInterval = gvUpdateInterval;
                changed = true;
            }

        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Display Options", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var showLegend = config.GraphViewShowLegend;
            if (ImGui.Checkbox("Show legend", ref showLegend))
            {
                config.GraphViewShowLegend = showLegend;
                changed = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Show a legend below the graph with combatant names and current values.");

            var showGrid = config.GraphViewShowGrid;
            if (ImGui.Checkbox("Show grid lines", ref showGrid))
            {
                config.GraphViewShowGrid = showGrid;
                changed = true;
            }

            var showXAxis = config.GraphViewShowXAxisLabels;
            if (ImGui.Checkbox("Show X axis labels", ref showXAxis))
            {
                config.GraphViewShowXAxisLabels = showXAxis;
                changed = true;
            }

            var showYAxis = config.GraphViewShowYAxisLabels;
            if (ImGui.Checkbox("Show Y axis labels", ref showYAxis))
            {
                config.GraphViewShowYAxisLabels = showYAxis;
                changed = true;
            }

            var highlightSelf = config.GraphViewHighlightSelf;
            if (ImGui.Checkbox("Highlight self (thicker line)", ref highlightSelf))
            {
                config.GraphViewHighlightSelf = highlightSelf;
                changed = true;
            }

            if (config.GraphViewHighlightSelf)
            {
                var selfThickness = config.GraphViewSelfLineThickness;
                ImGui.SetNextItemWidth(200);
                if (ImGui.SliderFloat("Self line thickness", ref selfThickness, 1f, 8f, "%.1f"))
                {
                    config.GraphViewSelfLineThickness = selfThickness;
                    changed = true;
                }
            }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Colors##graphview", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= ConfigHelpers.ColorEditProp("Background", config.GraphViewBackgroundColor, v => config.GraphViewBackgroundColor = v);
            changed |= ConfigHelpers.ColorEditProp("Grid lines", config.GraphViewGridColor, v => config.GraphViewGridColor = v);
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Value Labels##graphview", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var showViewLabels = config.GraphViewShowLabels;
            if (ImGui.Checkbox("Show value labels##graphview", ref showViewLabels))
            {
                config.GraphViewShowLabels = showViewLabels;
                changed = true;
            }

            var labelOffsetX = config.GraphViewLabelOffsetX;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Label offset X##graphview", ref labelOffsetX, -20f, 40f, "%.0f px"))
            {
                config.GraphViewLabelOffsetX = labelOffsetX;
                changed = true;
            }

            var labelOffsetY = config.GraphViewLabelOffsetY;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Label offset Y##graphview", ref labelOffsetY, -20f, 20f, "%.0f px"))
            {
                config.GraphViewLabelOffsetY = labelOffsetY;
                changed = true;
            }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Axis & Mouse Text##graphview", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var gvAutoScroll = config.GraphViewAutoScroll;
            if (ImGui.Checkbox("Auto-scroll##graphview", ref gvAutoScroll))
            {
                config.GraphViewAutoScroll = gvAutoScroll;
                changed = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("During combat, scroll the graph to show only the most recent time window instead of the full encounter.");

            if (config.GraphViewAutoScroll)
            {
                var gvScrollWindow = config.GraphViewAutoScrollWindow;
                ImGui.SetNextItemWidth(200);
                if (ImGui.SliderFloat("Scroll window##graphview", ref gvScrollWindow, 15f, 300f, "%.0f sec"))
                {
                    config.GraphViewAutoScrollWindow = gvScrollWindow;
                    changed = true;
                }

                var gvScrollSmooth = config.GraphViewAutoScrollSmoothing;
                ImGui.SetNextItemWidth(200);
                if (ImGui.SliderFloat("Scroll smoothing##graphview", ref gvScrollSmooth, 1f, 30f, "%.1f"))
                {
                    config.GraphViewAutoScrollSmoothing = gvScrollSmooth;
                    changed = true;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("How quickly the graph scrolls to the new position. Higher = snappier, lower = smoother.");
            }

            var gvXPadding = config.GraphViewXAxisPadding;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("X axis padding##graphview", ref gvXPadding, 1.0f, 2.0f, "%.2fx"))
            {
                config.GraphViewXAxisPadding = gvXPadding;
                changed = true;
            }

            var gvYHeadroom = config.GraphViewYAxisHeadroom;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Y axis headroom##graphview", ref gvYHeadroom, 1.0f, 2.0f, "%.2fx"))
            {
                config.GraphViewYAxisHeadroom = gvYHeadroom;
                changed = true;
            }

            var gvYTickCount = config.GraphViewYAxisTickCount;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderInt("Y axis tick count##graphview", ref gvYTickCount, 2, 16))
            {
                config.GraphViewYAxisTickCount = gvYTickCount;
                changed = true;
            }

            var gvMouseOpacity = config.GraphViewMouseTextOpacity;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Mouse text opacity##graphview", ref gvMouseOpacity, 0f, 1f, "%.2f"))
            {
                config.GraphViewMouseTextOpacity = gvMouseOpacity;
                changed = true;
            }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Skill Markers##graphview", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= ConfigHelpers.DrawSkillMarkerSection("gv_dps", "DPS Markers", config.GraphViewDpsMarkers);
            ImGui.Spacing();
            changed |= ConfigHelpers.DrawSkillMarkerSection("gv_hps", "HPS Markers", config.GraphViewHpsMarkers);
            ImGui.Spacing();
            changed |= ConfigHelpers.DrawSkillMarkerSection("gv_dtps", "DTPS Markers", config.GraphViewDtpsMarkers);
        }

        ImGui.Spacing();

        if (ConfigHelpers.ShiftResetButton("Reset Graph View"))
        {
            config.GraphViewAutoHeight = true;
            config.GraphViewHeight = 300f;
            config.GraphViewLineThickness = 2f;
            config.GraphViewSmoothingWindow = 5f;
            config.GraphViewUpdateInterval = 0.25f;
            config.GraphViewBackgroundColor = new Vector4(0.08f, 0.08f, 0.08f, 0.6f);
            config.GraphViewGridColor = new Vector4(0.3f, 0.3f, 0.3f, 0.3f);
            config.GraphViewShowLegend = true;
            config.GraphViewShowGrid = true;
            config.GraphViewShowXAxisLabels = true;
            config.GraphViewShowYAxisLabels = true;
            config.GraphViewHighlightSelf = true;
            config.GraphViewSelfLineThickness = 3.5f;
            config.GraphViewShowLabels = true;
            config.GraphViewLabelOffsetX = 8f;
            config.GraphViewLabelOffsetY = 0f;
            config.GraphViewFontSize = 14f;
            config.GraphViewAutoScroll = false;
            config.GraphViewAutoScrollWindow = 60f;
            config.GraphViewAutoScrollSmoothing = 8f;
            config.GraphViewXAxisPadding = 1.25f;
            config.GraphViewYAxisHeadroom = 1.1f;
            config.GraphViewYAxisTickCount = 8;
            config.GraphViewMouseTextOpacity = 0.6f;
            config.GraphViewDpsMarkers = new SkillMarkerConfig();
            config.GraphViewHpsMarkers = new SkillMarkerConfig();
            config.GraphViewDtpsMarkers = new SkillMarkerConfig();
            changed = true;
        }

        return changed;
    }
}
