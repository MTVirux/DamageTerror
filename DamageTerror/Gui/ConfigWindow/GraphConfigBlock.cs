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

        ImGui.TextDisabled("Dimensions");

        // Graph height — different per-context ranges, and GraphView gates on AutoHeight.
        if (!isGraphView || !config.GraphViewAutoHeight)
        {
            var (heightMin, heightMax) = isGraphView ? (100f, 600f) : (60f, 300f);
            changed |= ConfigHelpers.SliderFloatProp("Graph height",
                isGraphView ? config.GraphViewHeight : config.GraphHeight, heightMin, heightMax, "%.0f px",
                v => { if (isGraphView) config.GraphViewHeight = v; else config.GraphHeight = v; }, 200);
        }

        // Line thickness — different per-context max.
        var (lineMin, lineMax) = isGraphView ? (1f, 6f) : (1f, 5f);
        changed |= ConfigHelpers.SliderFloatProp("Line thickness",
            isGraphView ? config.GraphViewLineThickness : config.GraphLineThickness, lineMin, lineMax, "%.1f",
            v => { if (isGraphView) config.GraphViewLineThickness = v; else config.GraphLineThickness = v; }, 200);

        changed |= ConfigHelpers.SliderFloatProp($"Font size{suffix}",
            isGraphView ? config.GraphViewFontSize : config.GraphFontSize, 6f, 40f, "%.1fpt",
            v => { if (isGraphView) config.GraphViewFontSize = v; else config.GraphFontSize = v; }, 200);

        changed |= ConfigHelpers.SliderFloatProp($"Smoothing window{suffix}",
            isGraphView ? config.GraphViewSmoothingWindow : config.GraphSmoothingWindow, 1f, 30f, "%.0f sec",
            v => { if (isGraphView) config.GraphViewSmoothingWindow = v; else config.GraphSmoothingWindow = v; }, 200);

        changed |= ConfigHelpers.SliderFloatProp($"Update interval{suffix}",
            isGraphView ? config.GraphViewUpdateInterval : config.GraphUpdateInterval, 0.1f, 2f, "%.2f sec",
            v => { if (isGraphView) config.GraphViewUpdateInterval = v; else config.GraphUpdateInterval = v; }, 200);

        ImGui.Spacing();
        ImGui.TextDisabled("Display Options");

        changed |= ConfigHelpers.CheckboxProp($"Show legend{suffix}",
            isGraphView ? config.GraphViewShowLegend : config.GraphShowLegend,
            v => { if (isGraphView) config.GraphViewShowLegend = v; else config.GraphShowLegend = v; });

        changed |= ConfigHelpers.CheckboxProp($"Show grid lines{suffix}",
            isGraphView ? config.GraphViewShowGrid : config.GraphShowGrid,
            v => { if (isGraphView) config.GraphViewShowGrid = v; else config.GraphShowGrid = v; });

        changed |= ConfigHelpers.CheckboxProp($"Show X axis labels{suffix}",
            isGraphView ? config.GraphViewShowXAxisLabels : config.GraphShowXAxisLabels,
            v => { if (isGraphView) config.GraphViewShowXAxisLabels = v; else config.GraphShowXAxisLabels = v; });

        changed |= ConfigHelpers.CheckboxProp($"Show Y axis labels{suffix}",
            isGraphView ? config.GraphViewShowYAxisLabels : config.GraphShowYAxisLabels,
            v => { if (isGraphView) config.GraphViewShowYAxisLabels = v; else config.GraphShowYAxisLabels = v; });

        ImGui.Spacing();
        ImGui.TextDisabled("Colors");

        if (isGraphView)
            changed |= ConfigHelpers.ColorEditProp("Background", config.GraphViewBackgroundColor, v => config.GraphViewBackgroundColor = v);
        else
            changed |= ConfigHelpers.ColorEditProp("Graph background", config.GraphBackgroundColor, v => config.GraphBackgroundColor = v);

        if (isGraphView)
            changed |= ConfigHelpers.ColorEditProp("Grid lines", config.GraphViewGridColor, v => config.GraphViewGridColor = v);
        else
            changed |= ConfigHelpers.ColorEditProp("Grid lines", config.GraphGridColor, v => config.GraphGridColor = v);

        ImGui.Spacing();
        ImGui.TextDisabled("Value Labels");

        changed |= ConfigHelpers.CheckboxProp($"Show value labels{suffix}",
            isGraphView ? config.GraphViewShowLabels : config.GraphShowLabels,
            v => { if (isGraphView) config.GraphViewShowLabels = v; else config.GraphShowLabels = v; });

        changed |= ConfigHelpers.SliderFloatProp($"Label offset X{suffix}",
            isGraphView ? config.GraphViewLabelOffsetX : config.GraphLabelOffsetX, -20f, 40f, "%.0f px",
            v => { if (isGraphView) config.GraphViewLabelOffsetX = v; else config.GraphLabelOffsetX = v; }, 200);

        changed |= ConfigHelpers.SliderFloatProp($"Label offset Y{suffix}",
            isGraphView ? config.GraphViewLabelOffsetY : config.GraphLabelOffsetY, -20f, 20f, "%.0f px",
            v => { if (isGraphView) config.GraphViewLabelOffsetY = v; else config.GraphLabelOffsetY = v; }, 200);

        ImGui.Spacing();
        ImGui.TextDisabled("Axis & Mouse Text");

        changed |= ConfigHelpers.CheckboxProp($"Auto-scroll{suffix}",
            isGraphView ? config.GraphViewAutoScroll : config.GraphAutoScroll,
            v => { if (isGraphView) config.GraphViewAutoScroll = v; else config.GraphAutoScroll = v; });
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("During combat, scroll the graph to show only the most recent time window instead of the full encounter.");

        // Conditional scroll window + smoothing
        var autoScrollActive = isGraphView ? config.GraphViewAutoScroll : config.GraphAutoScroll;
        if (autoScrollActive)
        {
            changed |= ConfigHelpers.SliderFloatProp($"Scroll window{suffix}",
                isGraphView ? config.GraphViewAutoScrollWindow : config.GraphAutoScrollWindow, 15f, 300f, "%.0f sec",
                v => { if (isGraphView) config.GraphViewAutoScrollWindow = v; else config.GraphAutoScrollWindow = v; }, 200);

            changed |= ConfigHelpers.SliderFloatProp($"Scroll smoothing{suffix}",
                isGraphView ? config.GraphViewAutoScrollSmoothing : config.GraphAutoScrollSmoothing, 1f, 30f, "%.1f",
                v => { if (isGraphView) config.GraphViewAutoScrollSmoothing = v; else config.GraphAutoScrollSmoothing = v; }, 200);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("How quickly the graph scrolls to the new position.\nHigher = snappier, lower = smoother.");
        }

        changed |= ConfigHelpers.SliderFloatProp($"X axis padding{suffix}",
            isGraphView ? config.GraphViewXAxisPadding : config.GraphXAxisPadding, 1.0f, 2.0f, "%.2fx",
            v => { if (isGraphView) config.GraphViewXAxisPadding = v; else config.GraphXAxisPadding = v; }, 200);

        changed |= ConfigHelpers.SliderFloatProp($"Y axis headroom{suffix}",
            isGraphView ? config.GraphViewYAxisHeadroom : config.GraphYAxisHeadroom, 1.0f, 2.0f, "%.2fx",
            v => { if (isGraphView) config.GraphViewYAxisHeadroom = v; else config.GraphYAxisHeadroom = v; }, 200);

        changed |= ConfigHelpers.SliderIntProp($"Y axis tick count{suffix}",
            isGraphView ? config.GraphViewYAxisTickCount : config.GraphYAxisTickCount, 2, 16,
            v => { if (isGraphView) config.GraphViewYAxisTickCount = v; else config.GraphYAxisTickCount = v; }, 200);

        changed |= ConfigHelpers.SliderFloatProp($"Mouse text opacity{suffix}",
            isGraphView ? config.GraphViewMouseTextOpacity : config.GraphMouseTextOpacity, 0f, 1f, "%.2f",
            v => { if (isGraphView) config.GraphViewMouseTextOpacity = v; else config.GraphMouseTextOpacity = v; }, 200);

        return changed;
    }
}
