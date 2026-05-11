using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

/// <summary>
/// Shared graph-config widget block used by DetailsSection (Detail panel graph)
/// and GraphViewSection (GraphView mode). Internally branches on
/// <c>isGraphView</c> to bind to Graph* or GraphView* Configuration properties
/// and to select per-context slider ranges.
///
/// Callers wrap the block in their own CollapsingHeader and add their
/// context-specific widgets (e.g. series visibility for Details, AutoHeight
/// and HighlightSelf for GraphView).
/// </summary>
internal static class GraphConfigBlock
{
    public static bool Draw(Configuration config, bool isGraphView)
    {
        var changed = false;
        var suffix = isGraphView ? "##graphview" : "##graph";

        // Dimensions
        ImGui.TextDisabled("Dimensions");

        // Graph height — different per-context ranges, and GraphView gates on AutoHeight.
        if (!isGraphView || !config.GraphViewAutoHeight)
        {
            var height = isGraphView ? config.GraphViewHeight : config.GraphHeight;
            var (heightMin, heightMax) = isGraphView ? (100f, 600f) : (60f, 300f);
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Graph height", ref height, heightMin, heightMax, "%.0f px"))
            {
                if (isGraphView) config.GraphViewHeight = height;
                else config.GraphHeight = height;
                changed = true;
            }
        }

        // Line thickness — different per-context max.
        {
            var lineThickness = isGraphView ? config.GraphViewLineThickness : config.GraphLineThickness;
            var (lineMin, lineMax) = isGraphView ? (1f, 6f) : (1f, 5f);
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Line thickness", ref lineThickness, lineMin, lineMax, "%.1f"))
            {
                if (isGraphView) config.GraphViewLineThickness = lineThickness;
                else config.GraphLineThickness = lineThickness;
                changed = true;
            }
        }

        // Font size
        {
            var fontSize = isGraphView ? config.GraphViewFontSize : config.GraphFontSize;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat($"Font size{suffix}", ref fontSize, 6f, 40f, "%.1fpt"))
            {
                if (isGraphView) config.GraphViewFontSize = fontSize;
                else config.GraphFontSize = fontSize;
                changed = true;
            }
        }

        // Smoothing window
        {
            var smoothing = isGraphView ? config.GraphViewSmoothingWindow : config.GraphSmoothingWindow;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat($"Smoothing window{suffix}", ref smoothing, 1f, 30f, "%.0f sec"))
            {
                if (isGraphView) config.GraphViewSmoothingWindow = smoothing;
                else config.GraphSmoothingWindow = smoothing;
                changed = true;
            }
        }

        // Update interval
        {
            var updateInterval = isGraphView ? config.GraphViewUpdateInterval : config.GraphUpdateInterval;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat($"Update interval{suffix}", ref updateInterval, 0.1f, 2f, "%.2f sec"))
            {
                if (isGraphView) config.GraphViewUpdateInterval = updateInterval;
                else config.GraphUpdateInterval = updateInterval;
                changed = true;
            }
        }

        ImGui.Spacing();
        ImGui.TextDisabled("Display Options");

        // Show legend
        {
            var v = isGraphView ? config.GraphViewShowLegend : config.GraphShowLegend;
            if (ImGui.Checkbox($"Show legend{suffix}", ref v))
            {
                if (isGraphView) config.GraphViewShowLegend = v;
                else config.GraphShowLegend = v;
                changed = true;
            }
        }

        // Show grid lines
        {
            var v = isGraphView ? config.GraphViewShowGrid : config.GraphShowGrid;
            if (ImGui.Checkbox($"Show grid lines{suffix}", ref v))
            {
                if (isGraphView) config.GraphViewShowGrid = v;
                else config.GraphShowGrid = v;
                changed = true;
            }
        }

        // Show X axis labels
        {
            var v = isGraphView ? config.GraphViewShowXAxisLabels : config.GraphShowXAxisLabels;
            if (ImGui.Checkbox($"Show X axis labels{suffix}", ref v))
            {
                if (isGraphView) config.GraphViewShowXAxisLabels = v;
                else config.GraphShowXAxisLabels = v;
                changed = true;
            }
        }

        // Show Y axis labels
        {
            var v = isGraphView ? config.GraphViewShowYAxisLabels : config.GraphShowYAxisLabels;
            if (ImGui.Checkbox($"Show Y axis labels{suffix}", ref v))
            {
                if (isGraphView) config.GraphViewShowYAxisLabels = v;
                else config.GraphShowYAxisLabels = v;
                changed = true;
            }
        }

        ImGui.Spacing();
        ImGui.TextDisabled("Colors");

        // Background color
        if (isGraphView)
        {
            changed |= ConfigHelpers.ColorEditProp("Background", config.GraphViewBackgroundColor, v => config.GraphViewBackgroundColor = v);
        }
        else
        {
            changed |= ConfigHelpers.ColorEditProp("Graph background", config.GraphBackgroundColor, v => config.GraphBackgroundColor = v);
        }

        // Grid color
        if (isGraphView)
        {
            changed |= ConfigHelpers.ColorEditProp("Grid lines", config.GraphViewGridColor, v => config.GraphViewGridColor = v);
        }
        else
        {
            changed |= ConfigHelpers.ColorEditProp("Grid lines", config.GraphGridColor, v => config.GraphGridColor = v);
        }

        ImGui.Spacing();
        ImGui.TextDisabled("Value Labels");

        // Show value labels
        {
            var v = isGraphView ? config.GraphViewShowLabels : config.GraphShowLabels;
            if (ImGui.Checkbox($"Show value labels{suffix}", ref v))
            {
                if (isGraphView) config.GraphViewShowLabels = v;
                else config.GraphShowLabels = v;
                changed = true;
            }
        }

        // Label offset X
        {
            var v = isGraphView ? config.GraphViewLabelOffsetX : config.GraphLabelOffsetX;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat($"Label offset X{suffix}", ref v, -20f, 40f, "%.0f px"))
            {
                if (isGraphView) config.GraphViewLabelOffsetX = v;
                else config.GraphLabelOffsetX = v;
                changed = true;
            }
        }

        // Label offset Y
        {
            var v = isGraphView ? config.GraphViewLabelOffsetY : config.GraphLabelOffsetY;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat($"Label offset Y{suffix}", ref v, -20f, 20f, "%.0f px"))
            {
                if (isGraphView) config.GraphViewLabelOffsetY = v;
                else config.GraphLabelOffsetY = v;
                changed = true;
            }
        }

        ImGui.Spacing();
        ImGui.TextDisabled("Axis & Mouse Text");

        // Auto-scroll
        {
            var v = isGraphView ? config.GraphViewAutoScroll : config.GraphAutoScroll;
            if (ImGui.Checkbox($"Auto-scroll{suffix}", ref v))
            {
                if (isGraphView) config.GraphViewAutoScroll = v;
                else config.GraphAutoScroll = v;
                changed = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("During combat, scroll the graph to show only the most recent time window instead of the full encounter.");
        }

        // Conditional scroll window + smoothing
        var autoScrollActive = isGraphView ? config.GraphViewAutoScroll : config.GraphAutoScroll;
        if (autoScrollActive)
        {
            var scrollWindow = isGraphView ? config.GraphViewAutoScrollWindow : config.GraphAutoScrollWindow;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat($"Scroll window{suffix}", ref scrollWindow, 15f, 300f, "%.0f sec"))
            {
                if (isGraphView) config.GraphViewAutoScrollWindow = scrollWindow;
                else config.GraphAutoScrollWindow = scrollWindow;
                changed = true;
            }

            var scrollSmooth = isGraphView ? config.GraphViewAutoScrollSmoothing : config.GraphAutoScrollSmoothing;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat($"Scroll smoothing{suffix}", ref scrollSmooth, 1f, 30f, "%.1f"))
            {
                if (isGraphView) config.GraphViewAutoScrollSmoothing = scrollSmooth;
                else config.GraphAutoScrollSmoothing = scrollSmooth;
                changed = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("How quickly the graph scrolls to the new position. Higher = snappier, lower = smoother.");
        }

        // X axis padding
        {
            var v = isGraphView ? config.GraphViewXAxisPadding : config.GraphXAxisPadding;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat($"X axis padding{suffix}", ref v, 1.0f, 2.0f, "%.2fx"))
            {
                if (isGraphView) config.GraphViewXAxisPadding = v;
                else config.GraphXAxisPadding = v;
                changed = true;
            }
        }

        // Y axis headroom
        {
            var v = isGraphView ? config.GraphViewYAxisHeadroom : config.GraphYAxisHeadroom;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat($"Y axis headroom{suffix}", ref v, 1.0f, 2.0f, "%.2fx"))
            {
                if (isGraphView) config.GraphViewYAxisHeadroom = v;
                else config.GraphYAxisHeadroom = v;
                changed = true;
            }
        }

        // Y axis tick count
        {
            var v = isGraphView ? config.GraphViewYAxisTickCount : config.GraphYAxisTickCount;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderInt($"Y axis tick count{suffix}", ref v, 2, 16))
            {
                if (isGraphView) config.GraphViewYAxisTickCount = v;
                else config.GraphYAxisTickCount = v;
                changed = true;
            }
        }

        // Mouse text opacity
        {
            var v = isGraphView ? config.GraphViewMouseTextOpacity : config.GraphMouseTextOpacity;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat($"Mouse text opacity{suffix}", ref v, 0f, 1f, "%.2f"))
            {
                if (isGraphView) config.GraphViewMouseTextOpacity = v;
                else config.GraphMouseTextOpacity = v;
                changed = true;
            }
        }

        return changed;
    }
}
