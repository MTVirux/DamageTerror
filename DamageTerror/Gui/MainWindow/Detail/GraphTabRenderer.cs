using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImPlot;
using ImGui = Dalamud.Bindings.ImGui.ImGui;
using ImPlot = Dalamud.Bindings.ImPlot.ImPlot;

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

        List<GraphSample> samples;
        if (isLive)
        {
            samples = graphTracker.GetSamples(combatant.Name);
            if (samples.Count == 0
                && currentSnapshot?.GraphData != null
                && currentSnapshot.GraphData.TryGetValue(combatant.Name, out var fallback)
                && fallback.Count > 0)
            {
                samples = fallback;
            }
        }
        else if (currentSnapshot?.GraphData != null
            && currentSnapshot.GraphData.TryGetValue(combatant.Name, out var saved)
            && saved.Count > 0)
        {
            samples = saved;
        }
        else
        {
            samples = [];
        }

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

        var times = new float[samples.Count];
        var dpsVals = config.GraphShowDps ? new float[samples.Count] : null;
        var hpsVals = config.GraphShowHps ? new float[samples.Count] : null;
        var dtpsVals = config.GraphShowDtps ? new float[samples.Count] : null;

        for (var i = 0; i < samples.Count; i++)
        {
            times[i] = samples[i].TimeSec;
            if (dpsVals != null) dpsVals[i] = samples[i].Dps;
            if (hpsVals != null) hpsVals[i] = samples[i].Hps;
            if (dtpsVals != null) dtpsVals[i] = samples[i].Dtps;
        }

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

            if (dpsVals != null)
            {
                if (dpsHidden)
                    ImPlot.HideNextItem(true, ImPlotCond.Always);
                ImPlot.PushStyleColor(ImPlotCol.Line, config.GraphDpsColor);
                ImPlot.PushStyleVar(ImPlotStyleVar.LineWeight, thickness);
                ImPlot.PlotLine("iDPS", ref times[0], ref dpsVals[0], samples.Count);
                ImPlot.PopStyleVar();
                ImPlot.PopStyleColor();

                if (config.GraphShowLabels && !dpsHidden)
                {
                    var lastVal = dpsVals[^1];
                    ImPlot.PushStyleColor(ImPlotCol.InlayText, config.GraphDpsColor);
                    ImPlot.PlotText(GraphRenderHelper.FormatValue(lastVal), times[^1], lastVal, labelOffset);
                    ImPlot.PopStyleColor();
                }
            }

            if (hpsVals != null)
            {
                if (hpsHidden)
                    ImPlot.HideNextItem(true, ImPlotCond.Always);
                ImPlot.PushStyleColor(ImPlotCol.Line, config.GraphHpsColor);
                ImPlot.PushStyleVar(ImPlotStyleVar.LineWeight, thickness);
                ImPlot.PlotLine("iHPS", ref times[0], ref hpsVals[0], samples.Count);
                ImPlot.PopStyleVar();
                ImPlot.PopStyleColor();

                if (config.GraphShowLabels && !hpsHidden)
                {
                    var lastVal = hpsVals[^1];
                    ImPlot.PushStyleColor(ImPlotCol.InlayText, config.GraphHpsColor);
                    ImPlot.PlotText(GraphRenderHelper.FormatValue(lastVal), times[^1], lastVal, labelOffset);
                    ImPlot.PopStyleColor();
                }
            }

            if (dtpsVals != null)
            {
                if (dtpsHidden)
                    ImPlot.HideNextItem(true, ImPlotCond.Always);
                ImPlot.PushStyleColor(ImPlotCol.Line, config.GraphDtpsColor);
                ImPlot.PushStyleVar(ImPlotStyleVar.LineWeight, thickness);
                ImPlot.PlotLine("iDTPS", ref times[0], ref dtpsVals[0], samples.Count);
                ImPlot.PopStyleVar();
                ImPlot.PopStyleColor();

                if (config.GraphShowLabels && !dtpsHidden)
                {
                    var lastVal = dtpsVals[^1];
                    ImPlot.PushStyleColor(ImPlotCol.InlayText, config.GraphDtpsColor);
                    ImPlot.PlotText(GraphRenderHelper.FormatValue(lastVal), times[^1], lastVal, labelOffset);
                    ImPlot.PopStyleColor();
                }
            }

            var dpsMc = config.DetailDpsMarkers;
            var hpsMc = config.DetailHpsMarkers;
            var dtpsMc = config.DetailDtpsMarkers;

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

                if (bestEvent.HasValue && bestDist < 0.03f * 0.03f + 0.08f * 0.08f)
                {
                    ImGui.BeginTooltip();
                    ImGui.Text(bestEvent.Value.SkillName);
                    ImGui.Text(GraphRenderHelper.FormatValue(bestEvent.Value.Amount));
                    if (bestMc?.ShowCritMarkers == true)
                    {
                        if (bestEvent.Value.IsCrit && bestEvent.Value.IsDirectHit)
                            ImGui.TextColored(bestMc.CritDirectHitMarkerColor, "Critical Direct Hit !!!");
                        else if (bestEvent.Value.IsDirectHit)
                            ImGui.TextColored(bestMc.DirectHitMarkerColor, "Direct Hit !!");
                        else if (bestEvent.Value.IsCrit)
                            ImGui.TextColored(bestMc.CritMarkerColor, "Critical !");
                    }
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

            if (ImGui.BeginPopupContextItem($"##DetailGraphCtx_{index}", ImGuiPopupFlags.MouseButtonMiddle))
            {
                ImGui.TextDisabled("DamageTerror");
                ImGui.Separator();

                var autoScroll = config.GraphAutoScroll;
                if (ImGui.Checkbox("Auto-scroll", ref autoScroll))
                    config.GraphAutoScroll = autoScroll;

                if (config.GraphAutoScroll)
                {
                    ImGui.SetNextItemWidth(150);
                    var scrollWin = config.GraphAutoScrollWindow;
                    if (ImGui.SliderFloat("Window##dgctx", ref scrollWin, 15f, 300f, "%.0f sec"))
                        config.GraphAutoScrollWindow = scrollWin;

                    ImGui.SetNextItemWidth(150);
                    var scrollSmooth = config.GraphAutoScrollSmoothing;
                    if (ImGui.SliderFloat("Smoothing##dgctx", ref scrollSmooth, 1f, 30f, "%.1f"))
                        config.GraphAutoScrollSmoothing = scrollSmooth;
                }

                ImGui.Separator();

                var showLegend = config.GraphShowLegend;
                if (ImGui.Checkbox("Show Legend", ref showLegend))
                    config.GraphShowLegend = showLegend;

                var showGrid = config.GraphShowGrid;
                if (ImGui.Checkbox("Show Grid", ref showGrid))
                    config.GraphShowGrid = showGrid;

                var showLabels = config.GraphShowLabels;
                if (ImGui.Checkbox("Show Value Labels", ref showLabels))
                    config.GraphShowLabels = showLabels;

                var minSec = config.GraphXAxisMinSec;
                if (ImGui.Checkbox("X-Axis min:sec", ref minSec))
                    config.GraphXAxisMinSec = minSec;

                ImGui.EndPopup();
            }

            ImPlot.EndPlot();
        }

        GraphRenderHelper.PopGraphStyles(gs.ShowGrid);

        ImGui.Spacing();
    }
}
