using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

internal static class GraphViewSection
{
    public static bool Draw(Configuration config)
    {
        var changed = false;

        ImGui.TextWrapped("Configure the appearance of the graph view mode. Each tab can be switched to graph view in the Tabs settings page.");
        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Graph view configuration", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= ConfigHelpers.CheckboxProp("Auto-fit height##graphview", config.GraphViewAutoHeight, v => config.GraphViewAutoHeight = v);

            changed |= GraphConfigBlock.Draw(config, isGraphView: true);

            ImGui.Spacing();
            ImGui.TextDisabled("Self Highlight");

            changed |= ConfigHelpers.CheckboxProp("Highlight self (thicker line)", config.GraphViewHighlightSelf, v => config.GraphViewHighlightSelf = v);

            if (config.GraphViewHighlightSelf)
            {
                changed |= ConfigHelpers.SliderFloatProp("Self line thickness", config.GraphViewSelfLineThickness, 1f, 8f, "%.1f", v => config.GraphViewSelfLineThickness = v, 200);
            }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Skill Markers##graphview", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= ConfigHelpers.DrawSkillMarkerSection("gv_dps", "DPS Markers", config.GraphViewMarkers[MetricType.Dps]);
            ImGui.Spacing();
            changed |= ConfigHelpers.DrawSkillMarkerSection("gv_hps", "HPS Markers", config.GraphViewMarkers[MetricType.Hps]);
            ImGui.Spacing();
            changed |= ConfigHelpers.DrawSkillMarkerSection("gv_dtps", "DTPS Markers", config.GraphViewMarkers[MetricType.Dtps]);
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
            config.GraphViewMarkers[MetricType.Dps] = new SkillMarkerConfig();
            config.GraphViewMarkers[MetricType.Hps] = new SkillMarkerConfig();
            config.GraphViewMarkers[MetricType.Dtps] = new SkillMarkerConfig();
            changed = true;
        }

        return changed;
    }
}
