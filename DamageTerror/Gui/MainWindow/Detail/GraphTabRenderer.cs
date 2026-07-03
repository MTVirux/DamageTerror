
namespace DamageTerror.Gui.MainWindow.Detail;

internal sealed class GraphTabRenderer : IDetailTabRenderer
{
    private readonly Configuration config;
    private readonly GraphDataTracker graphTracker;
    private readonly SkillTracker skillTracker;
    private readonly DetailPanelState state;

    public GraphTabRenderer(Configuration config, GraphDataTracker graphTracker, SkillTracker skillTracker, DetailPanelState state)
    {
        this.config = config;
        this.graphTracker = graphTracker;
        this.skillTracker = skillTracker;
        this.state = state;
    }

    public void Render(in DetailRenderContext ctx)
    {
        var combatant = ctx.Combatant;
        var index = ctx.Index;
        var isLive = ctx.IsLive;
        var currentSnapshot = ctx.Snapshot;

        var samples = GraphRenderHelper.ResolveTracked(isLive,
            () => graphTracker.GetSamples(combatant.Name), currentSnapshot?.GraphData, combatant.Name);

        if (samples.Count < 2)
        {
            ImGui.Spacing();
            ImGui.TextDisabled("Not enough data to graph yet.");
            ImGui.Spacing();
            return;
        }

        ImGui.Spacing();

        var regionW = ImGui.GetContentRegionAvail().X;
        var graphH = config.GraphHeight;
        var thickness = config.GraphLineThickness;

        using var graphFont = FontScope.Push(config.GetFontScale(config.GraphFontSize));

        var (times, dpsVals, hpsVals, dtpsVals) =
            GraphRenderHelper.BuildSampleArrays(samples, config.GraphShowDps, config.GraphShowHps, config.GraphShowDtps);

        var maxTime = times[^1];
        if (maxTime <= 0f) maxTime = 1f;
        var maxVal = 0f;
        for (var i = 0; i < samples.Count; i++)
        {
            if (dpsVals != null && dpsVals[i] > maxVal) maxVal = dpsVals[i];
            if (hpsVals != null && hpsVals[i] > maxVal) maxVal = hpsVals[i];
            if (dtpsVals != null && dtpsVals[i] > maxVal) maxVal = dtpsVals[i];
        }

        var gs = GraphSettings.From(config, isGraphView: false);
        GraphRenderHelper.PushGraphStyles(in gs);
        var (plotFlags, xAxisFlags, yAxisFlags) = GraphRenderHelper.ComputePlotFlags(in gs, maxVal);

        if (ImPlot.BeginPlot($"##DetailGraph_{index}", new Vector2(regionW, graphH), plotFlags))
        {
            GraphRenderHelper.SetupGraphAxes(in gs, plotFlags, xAxisFlags, yAxisFlags, maxTime, maxVal,
                isLive, currentSnapshot?.Encounter.IsActive == true,
                ref state.WasActivelyUpdating, ref state.ScrollXMin, ref state.ScrollXMax);

            var labelOffset = new Vector2(config.GraphLabelOffsetX, config.GraphLabelOffsetY);

            var dpsHidden = state.HiddenLegendEntries.Contains("iDPS");
            var hpsHidden = state.HiddenLegendEntries.Contains("iHPS");
            var dtpsHidden = state.HiddenLegendEntries.Contains("iDTPS");

            void PlotMetricLine(float[] values, Vector4 color, string label, bool hidden)
                => GraphRenderHelper.PlotMetricLine(label, times, values, samples.Count, color, thickness,
                    hidden, config.GraphShowLabels, labelOffset);

            if (dpsVals != null)
                PlotMetricLine(dpsVals, config.GraphDpsColor, "iDPS", dpsHidden);

            if (hpsVals != null)
                PlotMetricLine(hpsVals, config.GraphHpsColor, "iHPS", hpsHidden);

            if (dtpsVals != null)
                PlotMetricLine(dtpsVals, config.GraphDtpsColor, "iDTPS", dtpsHidden);

            var dpsMc = config.DetailMarkers[MetricType.Dps];
            var hpsMc = config.DetailMarkers[MetricType.Hps];
            var dtpsMc = config.DetailMarkers[MetricType.Dtps];

            List<SkillUseEvent>? sourceEvents = null;
            if ((dpsMc.ShowMarkers && dpsVals != null)
                || (hpsMc.ShowMarkers && hpsVals != null))
            {
                sourceEvents = GraphRenderHelper.GetSourceEvents(isLive, combatant.Name, skillTracker, currentSnapshot);
            }

            if (dpsMc.ShowMarkers && dpsVals != null && !dpsHidden && sourceEvents != null)
            {
                var filtered = sourceEvents.Where(e => !e.IsHeal).ToList();
                if (filtered.Count > 0)
                    GraphRenderHelper.PlotSkillMarkers(filtered, times, dpsVals, $"detail_{index}_dps", dpsMc);
            }

            if (hpsMc.ShowMarkers && hpsVals != null && !hpsHidden && sourceEvents != null)
            {
                var filtered = sourceEvents.Where(e => e.IsHeal).ToList();
                if (filtered.Count > 0)
                    GraphRenderHelper.PlotSkillMarkers(filtered, times, hpsVals, $"detail_{index}_hps", hpsMc);
            }

            if (dtpsMc.ShowMarkers && dtpsVals != null && !dtpsHidden)
            {
                var dtEvents = GraphRenderHelper.GetDamageTakenEvents(isLive, combatant.Name, skillTracker, currentSnapshot);

                if (dtEvents.Count > 0)
                    GraphRenderHelper.PlotSkillMarkers(dtEvents, times, dtpsVals, $"detail_{index}_dtps", dtpsMc);
            }

            var anyMarkersEnabled = dpsMc.ShowMarkers
                                 || hpsMc.ShowMarkers
                                 || dtpsMc.ShowMarkers;
            if (anyMarkersEnabled && ImPlot.IsPlotHovered())
            {
                var mouse = ImPlot.GetPlotMousePos();
                var bestDist = float.MaxValue;
                SkillUseEvent? bestEvent = null;
                SkillMarkerConfig? bestMc = null;

                void FindNearestDetail(SkillUseEvent ev, float[] ts, float[]? vals, SkillMarkerConfig mc)
                {
                    if (vals == null) return;
                    var y = GraphRenderHelper.InterpolateValue(ts, vals, ev.TimeSec);
                    var xRange = maxTime > 0 ? maxTime : 1f;
                    var yRange = maxVal > 0 ? maxVal : 1f;
                    var dx = Math.Abs((float)mouse.X - ev.TimeSec) / xRange;
                    var dy = Math.Abs((float)mouse.Y - y) / yRange;
                    var dist = dx * dx + dy * dy;
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestEvent = ev;
                        bestMc = mc;
                    }
                }

                if (dpsMc.ShowMarkers && !dpsHidden && sourceEvents != null)
                    foreach (var ev in sourceEvents.Where(e => !e.IsHeal))
                        FindNearestDetail(ev, times, dpsVals, dpsMc);

                if (hpsMc.ShowMarkers && !hpsHidden && sourceEvents != null)
                    foreach (var ev in sourceEvents.Where(e => e.IsHeal))
                        FindNearestDetail(ev, times, hpsVals, hpsMc);

                if (dtpsMc.ShowMarkers && !dtpsHidden && dtpsVals != null)
                {
                    var dtEventsForTt = GraphRenderHelper.GetDamageTakenEvents(isLive, combatant.Name, skillTracker, currentSnapshot);

                    foreach (var ev in dtEventsForTt)
                        FindNearestDetail(ev, times, dtpsVals, dtpsMc);
                }

                if (bestEvent.HasValue && bestDist < GraphRenderHelper.DetailMarkerHoverThresholdSq)
                {
                    ImGui.BeginTooltip();
                    ImGui.Text(bestEvent.Value.SkillName);
                    ImGui.Text(GraphRenderHelper.FormatValue(bestEvent.Value.Amount));
                    if (bestMc?.ShowCritMarkers == true)
                        GraphRenderHelper.DrawCritMarkerLabel(bestEvent.Value, bestMc);
                    ImGui.EndTooltip();
                }
            }

            foreach (var label in new[] { "iDPS", "iHPS", "iDTPS" })
            {
                if (ImPlot.IsLegendEntryHovered(label) && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    if (!state.HiddenLegendEntries.Remove(label))
                        state.HiddenLegendEntries.Add(label);
                }
            }

            GraphRenderHelper.DrawMousePositionText(gs.MouseTextOpacity, config.GraphXAxisMinSec);

            GraphRenderHelper.DrawGraphContextMenu(config, isGraphView: false, idSuffix: $"_{index}");

            ImPlot.EndPlot();
        }

        GraphRenderHelper.PopGraphStyles(gs.ShowGrid);

        ImGui.Spacing();
    }
}
