using ImPlot = Dalamud.Bindings.ImPlot.ImPlot;

namespace DamageTerror.Gui.MainWindow;

public sealed class GraphViewComponent
{
    private readonly Configuration config;
    private readonly GraphDataTracker graphTracker;
    private readonly SkillTracker skillTracker;
    private readonly HashSet<string> hiddenLegendEntries = new(StringComparer.Ordinal);
    private bool wasActivelyUpdating;
    private double scrollXMin = double.NaN;
    private double scrollXMax = double.NaN;

    public GraphViewComponent(Configuration config, GraphDataTracker graphTracker, SkillTracker skillTracker)
    {
        this.config = config;
        this.graphTracker = graphTracker;
        this.skillTracker = skillTracker;
    }

    public void Render(List<CombatantEntry> combatants, EncounterSnapshot? snapshot,
        bool isLive, MeterTab? activeTab, string currentPlayerName)
    {
        var showDps = activeTab?.GraphShowDpsLine ?? true;
        var showHps = activeTab?.GraphShowHpsLine ?? false;
        var showDtps = activeTab?.GraphShowDtpsLine ?? false;

        if (!showDps && !showHps && !showDtps)
        {
            ImGui.TextDisabled("No graph metrics enabled. Enable DPS, HPS, or DTPS in the tab's graph settings.");
            return;
        }

        var allSeries = new List<(CombatantEntry combatant, List<GraphSample> samples)>();
        foreach (var c in combatants)
        {
            // Live tracker, falling back to stored data when empty
            // (e.g. encounter restored from history after plugin reload).
            var samples = GraphRenderHelper.ResolveTracked(isLive,
                () => graphTracker.GetSamples(c.Name), snapshot?.GraphData, c.Name);

            if (samples.Count >= 2)
                allSeries.Add((c, samples));
        }

        if (allSeries.Count == 0)
        {
            ImGui.TextDisabled("Not enough graph data yet.");
            return;
        }

        var regionW = ImGui.GetContentRegionAvail().X;
        var graphH = config.GraphViewAutoHeight
            ? Math.Max(100f, ImGui.GetContentRegionAvail().Y)
            : config.GraphViewHeight;

        using var fontScope = FontScope.Push(config.GetFontScale(config.GraphViewFontSize));

        var maxTime = 0f;
        var maxVal = 0f;
        foreach (var (_, samples) in allSeries)
        {
            if (samples[^1].TimeSec > maxTime)
                maxTime = samples[^1].TimeSec;

            foreach (var s in samples)
            {
                if (showDps && s.Dps > maxVal) maxVal = s.Dps;
                if (showHps && s.Hps > maxVal) maxVal = s.Hps;
                if (showDtps && s.Dtps > maxVal) maxVal = s.Dtps;
            }
        }
        if (maxTime <= 0f) maxTime = 1f;
        if (maxVal <= 0f) maxVal = 1f;

        var metricCount = (showDps ? 1 : 0) + (showHps ? 1 : 0) + (showDtps ? 1 : 0);

        var gs = GraphSettings.From(config, isGraphView: true);
        GraphRenderHelper.PushGraphStyles(in gs);
        var (plotFlags, xAxisFlags, yAxisFlags) = GraphRenderHelper.ComputePlotFlags(in gs, maxVal);

        if (ImPlot.BeginPlot("##GraphView", new Vector2(regionW, graphH), plotFlags))
        {
            GraphRenderHelper.SetupGraphAxes(in gs, plotFlags, xAxisFlags, yAxisFlags, maxTime, maxVal,
                isLive, snapshot?.Encounter.IsActive == true, ref wasActivelyUpdating, ref scrollXMin, ref scrollXMax);

            var defaultThickness = config.GraphViewLineThickness;
            var selfThickness = config.GraphViewHighlightSelf ? config.GraphViewSelfLineThickness : defaultThickness;
            var labelOffset = new Vector2(config.GraphViewLabelOffsetX, config.GraphViewLabelOffsetY);
            var hiddenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var legendLabels = new List<(string label, string combatantName)>();

            foreach (var (combatant, samples) in allSeries)
            {
                var jobColor = JobColorHelper.GetEffectiveJobColor(combatant.Job, config);
                var isSelf = string.Equals(combatant.Name, currentPlayerName, StringComparison.OrdinalIgnoreCase);
                var thickness = isSelf ? selfThickness : defaultThickness;
                var displayName = NameFormatHelper.GetDisplayName(combatant.Name, combatant.Job, isSelf, config);
                string? primaryLabel = null;

                var (times, dpsVals, hpsVals, dtpsVals) =
                    GraphRenderHelper.BuildSampleArrays(samples, showDps, showHps, showDtps);

                void PlotMetricLine(float[] values, Vector4 color, string suffix)
                {
                    var label = metricCount > 1 ? $"{displayName} ({suffix})" : displayName;
                    primaryLabel ??= label;
                    legendLabels.Add((label, combatant.Name));
                    GraphRenderHelper.PlotMetricLine(label, times, values, samples.Count, color, thickness,
                        hiddenLegendEntries.Contains(label), config.GraphViewShowLabels, labelOffset);
                }

                if (dpsVals != null)
                    PlotMetricLine(dpsVals, jobColor, "DPS");

                if (hpsVals != null)
                {
                    var hpsColor = metricCount > 1
                        ? new Vector4(
                            Math.Min(1f, jobColor.X * 0.7f + 0.3f),
                            Math.Min(1f, jobColor.Y * 0.7f + 0.3f),
                            Math.Min(1f, jobColor.Z * 0.7f + 0.3f),
                            jobColor.W * 0.7f)
                        : jobColor;
                    PlotMetricLine(hpsVals, hpsColor, "HPS");
                }

                if (dtpsVals != null)
                {
                    var dtpsColor = metricCount > 1
                        ? new Vector4(
                            jobColor.X * 0.5f,
                            jobColor.Y * 0.5f,
                            jobColor.Z * 0.5f,
                            jobColor.W * 0.6f)
                        : jobColor;
                    PlotMetricLine(dtpsVals, dtpsColor, "DTPS");
                }

                var combatantHidden = primaryLabel != null && hiddenLegendEntries.Contains(primaryLabel);
                if (combatantHidden) hiddenNames.Add(combatant.Name);

                // ── Skill use markers per combatant, split by metric type ──
                if (!combatantHidden)
                {
                    var dpsMc = config.GraphViewMarkers[MetricType.Dps];
                    var hpsMc = config.GraphViewMarkers[MetricType.Hps];
                    var dtpsMc = config.GraphViewMarkers[MetricType.Dtps];

                    List<SkillUseEvent>? sourceEvents = null;
                    if ((dpsMc?.ShowMarkers == true && dpsVals != null)
                        || (hpsMc?.ShowMarkers == true && hpsVals != null))
                    {
                        sourceEvents = GraphRenderHelper.GetSourceEvents(isLive, combatant.Name, skillTracker, snapshot);
                    }

                    var dpsLabel = metricCount > 1 ? $"{displayName} (DPS)" : displayName;
                    if (dpsMc?.ShowMarkers == true && dpsVals != null && !hiddenLegendEntries.Contains(dpsLabel) && sourceEvents != null)
                    {
                        var filtered = sourceEvents.Where(e => !e.IsHeal).ToList();
                        if (filtered.Count > 0)
                            GraphRenderHelper.PlotSkillMarkers(filtered, times, dpsVals, $"{combatant.Name}_dps", dpsMc);
                    }

                    var hpsLabel = metricCount > 1 ? $"{displayName} (HPS)" : displayName;
                    if (hpsMc?.ShowMarkers == true && hpsVals != null && !hiddenLegendEntries.Contains(hpsLabel) && sourceEvents != null)
                    {
                        var filtered = sourceEvents.Where(e => e.IsHeal).ToList();
                        if (filtered.Count > 0)
                            GraphRenderHelper.PlotSkillMarkers(filtered, times, hpsVals, $"{combatant.Name}_hps", hpsMc);
                    }

                    var dtpsLabel = metricCount > 1 ? $"{displayName} (DTPS)" : displayName;
                    if (dtpsMc?.ShowMarkers == true && dtpsVals != null && !hiddenLegendEntries.Contains(dtpsLabel))
                    {
                        var dtEvents = GraphRenderHelper.GetDamageTakenEvents(isLive, combatant.Name, skillTracker, snapshot);

                        if (dtEvents.Count > 0)
                            GraphRenderHelper.PlotSkillMarkers(dtEvents, times, dtpsVals, $"{combatant.Name}_dtps", dtpsMc);
                    }
                }
            }

            // Skill marker tooltip — find nearest marker across all combatants on hover.
            // Uses plot-space coordinates (from GetPlotMousePos, which accounts for zoom/pan)
            // with per-axis normalization by plot pixel size, giving uniform hover behaviour
            // regardless of where markers sit on the graph or the current zoom level.
            var anyMarkersEnabled = config.GraphViewMarkers[MetricType.Dps].ShowMarkers
                                || config.GraphViewMarkers[MetricType.Hps].ShowMarkers
                                || config.GraphViewMarkers[MetricType.Dtps].ShowMarkers;
            if (anyMarkersEnabled && ImPlot.IsPlotHovered())
            {
                var mouse = ImPlot.GetPlotMousePos();
                var plotSize = ImPlot.GetPlotSize();

                var bestDist = float.MaxValue;
                SkillUseEvent? bestEvent = null;
                SkillMarkerConfig? bestMc = null;
                var bestIsDamageTaken = false;

                var ttDpsMc = config.GraphViewMarkers[MetricType.Dps];
                var ttHpsMc = config.GraphViewMarkers[MetricType.Hps];
                var ttDtpsMc = config.GraphViewMarkers[MetricType.Dtps];

                foreach (var (combatant, samples) in allSeries)
                {
                    if (hiddenNames.Contains(combatant.Name)) continue;

                    var (ts, dv, hv, tv) = GraphRenderHelper.BuildSampleArrays(samples, showDps, showHps, showDtps);

                    List<SkillUseEvent>? sourceEvents = null;
                    if ((ttDpsMc?.ShowMarkers == true && dv != null)
                        || (ttHpsMc?.ShowMarkers == true && hv != null))
                    {
                        sourceEvents = GraphRenderHelper.GetSourceEvents(isLive, combatant.Name, skillTracker, snapshot);
                    }

                    if (ttDpsMc?.ShowMarkers == true && dv != null && sourceEvents != null)
                    {
                        foreach (var ev in sourceEvents)
                        {
                            if (ev.IsHeal) continue;
                            FindNearest(ev, ts, dv, mouse, plotSize, maxTime, maxVal, false, ref bestDist, ref bestEvent, ref bestIsDamageTaken, ttDpsMc, ref bestMc);
                        }
                    }

                    if (ttHpsMc?.ShowMarkers == true && hv != null && sourceEvents != null)
                    {
                        foreach (var ev in sourceEvents)
                        {
                            if (!ev.IsHeal) continue;
                            FindNearest(ev, ts, hv, mouse, plotSize, maxTime, maxVal, false, ref bestDist, ref bestEvent, ref bestIsDamageTaken, ttHpsMc, ref bestMc);
                        }
                    }

                    if (ttDtpsMc?.ShowMarkers == true && tv != null)
                    {
                        var dtEvents = GraphRenderHelper.GetDamageTakenEvents(isLive, combatant.Name, skillTracker, snapshot);

                        foreach (var ev in dtEvents)
                        {
                            FindNearest(ev, ts, tv, mouse, plotSize, maxTime, maxVal, true, ref bestDist, ref bestEvent, ref bestIsDamageTaken, ttDtpsMc, ref bestMc);
                        }
                    }
                }

                // Threshold in pseudo-pixel units (16 px radius when not zoomed,
                // proportionally tighter when zoomed in).
                var hoverRadiusSq = GraphRenderHelper.MarkerHoverRadiusPx * GraphRenderHelper.MarkerHoverRadiusPx;
                if (bestEvent.HasValue && bestDist <= hoverRadiusSq)
                {
                    ImGui.BeginTooltip();
                    ImGui.Text(bestEvent.Value.SkillName);
                    ImGui.Text(GraphRenderHelper.FormatValueLong(bestEvent.Value.Amount));
                    if (bestIsDamageTaken)
                        ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), "(Received)");
                    if (bestEvent.Value.IsDoTTick || bestEvent.Value.IsHoTTick)
                        ImGui.TextColored(bestMc?.DoTTickColor ?? new Vector4(0.6f, 0.2f, 0.8f, 0.9f),
                            bestEvent.Value.IsDoTTick ? "DoT Tick" : "HoT Tick");
                    else if (bestEvent.Value.IsDoTApplication || bestEvent.Value.IsHoTApplication)
                        ImGui.TextColored(bestMc?.DoTApplicationColor ?? new Vector4(0.9f, 0.3f, 0.9f, 0.95f),
                            bestEvent.Value.IsDoTApplication ? "DoT Application" : "HoT Application");
                    else if (bestMc?.ShowCritMarkers == true)
                        GraphRenderHelper.DrawCritMarkerLabel(bestEvent.Value, bestMc);
                    ImGui.EndTooltip();
                }
            }

            foreach (var (label, _) in legendLabels)
            {
                if (ImPlot.IsLegendEntryHovered(label) && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    if (!hiddenLegendEntries.Remove(label))
                        hiddenLegendEntries.Add(label);
                }
            }

            GraphRenderHelper.DrawMousePositionText(gs.MouseTextOpacity, config.GraphViewXAxisMinSec);

            // Middle-click context menu (right-click opens ImPlot's native axis controls)
            GraphRenderHelper.DrawGraphContextMenu(config, isGraphView: true, idSuffix: "");

            ImPlot.EndPlot();
        }

        GraphRenderHelper.PopGraphStyles(gs.ShowGrid);
    }

    private static void FindNearest(SkillUseEvent ev, float[] times, float[] values,
        ImPlotPoint mouse, Vector2 plotSize, float maxTime, float maxVal,
        bool isDamageTaken, ref float bestDist, ref SkillUseEvent? bestEvent, ref bool bestIsDamageTaken,
        SkillMarkerConfig mc, ref SkillMarkerConfig? bestMc)
    {
        var y = GraphRenderHelper.InterpolateValue(times, values, ev.TimeSec);
        var ndx = maxTime > 0 ? ((float)mouse.X - ev.TimeSec) / maxTime : 0f;
        var ndy = maxVal > 0 ? ((float)mouse.Y - y) / maxVal : 0f;
        var px = ndx * plotSize.X;
        var py = ndy * plotSize.Y;
        var dist = px * px + py * py;
        if (dist < bestDist)
        {
            bestDist = dist;
            bestEvent = ev;
            bestIsDamageTaken = isDamageTaken;
            bestMc = mc;
        }
    }
}
