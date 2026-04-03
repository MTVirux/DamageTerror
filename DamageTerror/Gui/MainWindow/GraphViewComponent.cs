using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImPlot;
using DamageTerror.Services;
using ImGui = Dalamud.Bindings.ImGui.ImGui;
using ImPlot = Dalamud.Bindings.ImPlot.ImPlot;

namespace DamageTerror.Gui.MainWindow;

public class GraphViewComponent
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

        // Gather samples for all combatants
        var allSeries = new List<(CombatantEntry combatant, List<GraphSample> samples)>();
        foreach (var c in combatants)
        {
            List<GraphSample> samples;
            if (isLive)
            {
                samples = graphTracker.GetSamples(c.Name);
                // Fall back to stored data when live tracker is empty
                // (e.g. encounter restored from history after plugin reload)
                if (samples.Count == 0
                    && snapshot?.GraphData != null
                    && snapshot.GraphData.TryGetValue(c.Name, out var fallback)
                    && fallback.Count > 0)
                {
                    samples = fallback;
                }
            }
            else if (snapshot?.GraphData != null
                && snapshot.GraphData.TryGetValue(c.Name, out var saved)
                && saved.Count > 0)
            {
                samples = saved;
            }
            else
            {
                samples = [];
            }

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

        // Apply graph font scaling
        var prevScale = ImGui.GetFont().Scale;
        ImGui.GetFont().Scale = config.GetFontScale(config.GraphViewFontSize);
        ImGui.PushFont(ImGui.GetFont());

        // Compute max time and max value across all series
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

        // Count enabled metrics for labeling
        var metricCount = (showDps ? 1 : 0) + (showHps ? 1 : 0) + (showDtps ? 1 : 0);

        // Configure ImPlot style
        ImPlot.PushStyleColor(ImPlotCol.Bg, config.GraphViewBackgroundColor);
        ImPlot.PushStyleColor(ImPlotCol.FrameBg, new Vector4(0, 0, 0, 0));

        var plotFlags = ImPlotFlags.NoMouseText;
        if (!config.GraphViewShowLegend)
            plotFlags |= ImPlotFlags.NoLegend;

        var xAxisFlags = ImPlotAxisFlags.None;
        if (!config.GraphViewShowGrid)
            xAxisFlags |= ImPlotAxisFlags.NoGridLines;
        if (!config.GraphViewShowXAxisLabels)
            xAxisFlags |= ImPlotAxisFlags.NoTickLabels;

        var yAxisFlags = ImPlotAxisFlags.AutoFit;
        if (!config.GraphViewShowGrid)
            yAxisFlags |= ImPlotAxisFlags.NoGridLines;
        if (!config.GraphViewShowYAxisLabels)
            yAxisFlags |= ImPlotAxisFlags.NoTickLabels;

        if (config.GraphViewShowGrid)
        {
            ImPlot.PushStyleColor(ImPlotCol.AxisGrid, config.GraphViewGridColor);
        }

        if (ImPlot.BeginPlot("##GraphView", new Vector2(regionW, graphH), plotFlags))
        {
            ImPlot.SetupAxes("", "", xAxisFlags, yAxisFlags);
            if (!plotFlags.HasFlag(ImPlotFlags.NoLegend))
                ImPlot.SetupLegend(ImPlotLocation.NorthWest);
            // Use Always when actively receiving data to keep axis locked; Once when done to allow zoom/pan
            var isActivelyUpdating = isLive && snapshot?.Encounter.IsActive == true;
            var justEnded = wasActivelyUpdating && !isActivelyUpdating;
            wasActivelyUpdating = isActivelyUpdating;
            var axisLimitCond = (isActivelyUpdating || justEnded) ? ImPlotCond.Always : ImPlotCond.Once;

            if (config.GraphViewAutoScroll && isActivelyUpdating && maxTime > config.GraphViewAutoScrollWindow)
            {
                var windowSec = config.GraphViewAutoScrollWindow;
                var targetMin = maxTime - windowSec;
                var targetMax = maxTime + windowSec * (config.GraphViewXAxisPadding - 1f);
                var dt = ImGui.GetIO().DeltaTime;
                var speed = config.GraphViewAutoScrollSmoothing;
                var t = (float)Math.Min(1.0, speed * dt);
                if (double.IsNaN(scrollXMin) || double.IsNaN(scrollXMax))
                {
                    scrollXMin = targetMin;
                    scrollXMax = targetMax;
                }
                else
                {
                    scrollXMin += (targetMin - scrollXMin) * t;
                    scrollXMax += (targetMax - scrollXMax) * t;
                }
                ImPlot.SetupAxisLimits(ImAxis.X1, scrollXMin, scrollXMax, axisLimitCond);
            }
            else
            {
                if (!double.IsNaN(scrollXMin)) { scrollXMin = double.NaN; scrollXMax = double.NaN; }
                ImPlot.SetupAxisLimits(ImAxis.X1, 0, maxTime * config.GraphViewXAxisPadding, axisLimitCond);
            }
            // Prevent panning below 0
            ImPlot.SetupAxisLimitsConstraints(ImAxis.X1, 0, double.MaxValue);

            // Custom Y-axis ticks with K/M abbreviations, skipping 0
            if (maxVal > 0f) SetupAbbreviatedYTicks(maxVal, config.GraphViewYAxisHeadroom, config.GraphViewYAxisTickCount);

            var defaultThickness = config.GraphViewLineThickness;
            var selfThickness = config.GraphViewHighlightSelf ? config.GraphViewSelfLineThickness : defaultThickness;
            var labelOffset = new Vector2(config.GraphViewLabelOffsetX, config.GraphViewLabelOffsetY);
            var hiddenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var legendLabels = new List<(string label, string combatantName)>();

            foreach (var (combatant, samples) in allSeries)
            {
                var jobColor = JobColorHelper.GetColor(combatant.Job, config);
                var isSelf = string.Equals(combatant.Name, currentPlayerName, StringComparison.OrdinalIgnoreCase);
                var thickness = isSelf ? selfThickness : defaultThickness;
                var displayName = NameFormatHelper.GetDisplayName(combatant.Name, combatant.Job, isSelf, config);
                string? primaryLabel = null;

                // Extract arrays from samples
                var times = new float[samples.Count];
                var dpsVals = showDps ? new float[samples.Count] : null;
                var hpsVals = showHps ? new float[samples.Count] : null;
                var dtpsVals = showDtps ? new float[samples.Count] : null;

                for (var i = 0; i < samples.Count; i++)
                {
                    times[i] = samples[i].TimeSec;
                    if (dpsVals != null) dpsVals[i] = samples[i].Dps;
                    if (hpsVals != null) hpsVals[i] = samples[i].Hps;
                    if (dtpsVals != null) dtpsVals[i] = samples[i].Dtps;
                }

                if (dpsVals != null)
                {
                    var label = metricCount > 1 ? $"{displayName} (DPS)" : displayName;
                    primaryLabel ??= label;
                    legendLabels.Add((label, combatant.Name));
                    ImPlot.PushStyleColor(ImPlotCol.Line, jobColor);
                    ImPlot.PushStyleVar(ImPlotStyleVar.LineWeight, thickness);
                    ImPlot.PlotLine(label, ref times[0], ref dpsVals[0], samples.Count);
                    ImPlot.PopStyleVar();
                    ImPlot.PopStyleColor();

                    if (config.GraphViewShowLabels && !hiddenLegendEntries.Contains(label))
                    {
                        var lastVal = dpsVals[^1];
                        ImPlot.PushStyleColor(ImPlotCol.InlayText, jobColor);
                        ImPlot.PlotText(FormatValue(lastVal), times[^1], lastVal, labelOffset);
                        ImPlot.PopStyleColor();
                    }
                }

                if (hpsVals != null)
                {
                    var hpsColor = metricCount > 1
                        ? new Vector4(
                            Math.Min(1f, jobColor.X * 0.7f + 0.3f),
                            Math.Min(1f, jobColor.Y * 0.7f + 0.3f),
                            Math.Min(1f, jobColor.Z * 0.7f + 0.3f),
                            jobColor.W * 0.7f)
                        : jobColor;
                    var label = metricCount > 1 ? $"{displayName} (HPS)" : displayName;
                    primaryLabel ??= label;
                    legendLabels.Add((label, combatant.Name));
                    ImPlot.PushStyleColor(ImPlotCol.Line, hpsColor);
                    ImPlot.PushStyleVar(ImPlotStyleVar.LineWeight, thickness);
                    ImPlot.PlotLine(label, ref times[0], ref hpsVals[0], samples.Count);
                    ImPlot.PopStyleVar();
                    ImPlot.PopStyleColor();

                    if (config.GraphViewShowLabels && !hiddenLegendEntries.Contains(label))
                    {
                        var lastVal = hpsVals[^1];
                        ImPlot.PushStyleColor(ImPlotCol.InlayText, hpsColor);
                        ImPlot.PlotText(FormatValue(lastVal), times[^1], lastVal, labelOffset);
                        ImPlot.PopStyleColor();
                    }
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
                    var label = metricCount > 1 ? $"{displayName} (DTPS)" : displayName;
                    primaryLabel ??= label;
                    legendLabels.Add((label, combatant.Name));
                    ImPlot.PushStyleColor(ImPlotCol.Line, dtpsColor);
                    ImPlot.PushStyleVar(ImPlotStyleVar.LineWeight, thickness);
                    ImPlot.PlotLine(label, ref times[0], ref dtpsVals[0], samples.Count);
                    ImPlot.PopStyleVar();
                    ImPlot.PopStyleColor();

                    if (config.GraphViewShowLabels && !hiddenLegendEntries.Contains(label))
                    {
                        var lastVal = dtpsVals[^1];
                        ImPlot.PushStyleColor(ImPlotCol.InlayText, dtpsColor);
                        ImPlot.PlotText(FormatValue(lastVal), times[^1], lastVal, labelOffset);
                        ImPlot.PopStyleColor();
                    }
                }

                // Determine if combatant is hidden — primary legend entry was toggled off
                var combatantHidden = primaryLabel != null && hiddenLegendEntries.Contains(primaryLabel);
                if (combatantHidden) hiddenNames.Add(combatant.Name);

                // ── Skill use markers per combatant, split by metric type ──
                if (!combatantHidden)
                {
                    var dpsMc = activeTab?.DpsMarkers;
                    var hpsMc = activeTab?.HpsMarkers;
                    var dtpsMc = activeTab?.DtpsMarkers;

                    // Fetch source-side skill events (damage dealt + heals cast)
                    List<SkillUseEvent>? sourceEvents = null;
                    if ((dpsMc?.ShowMarkers == true && dpsVals != null)
                        || (hpsMc?.ShowMarkers == true && hpsVals != null))
                    {
                        if (isLive)
                        {
                            sourceEvents = skillTracker.GetSkillEvents(combatant.Name);
                            if (sourceEvents.Count == 0
                                && snapshot?.SkillEvents != null
                                && snapshot.SkillEvents.TryGetValue(combatant.Name, out var fallbackEvt))
                            {
                                sourceEvents = fallbackEvt;
                            }
                        }
                        else if (snapshot?.SkillEvents != null
                            && snapshot.SkillEvents.TryGetValue(combatant.Name, out var saved))
                        {
                            sourceEvents = saved;
                        }
                    }

                    // DPS markers: damage events (non-heal) on the DPS line
                    var dpsLabel = metricCount > 1 ? $"{displayName} (DPS)" : displayName;
                    if (dpsMc?.ShowMarkers == true && dpsVals != null && !hiddenLegendEntries.Contains(dpsLabel) && sourceEvents != null)
                    {
                        var filtered = sourceEvents.Where(e => !e.IsHeal).ToList();
                        if (filtered.Count > 0)
                            PlotSkillMarkers(filtered, times, dpsVals, $"{combatant.Name}_dps", dpsMc);
                    }

                    // HPS markers: healing events on the HPS line
                    var hpsLabel = metricCount > 1 ? $"{displayName} (HPS)" : displayName;
                    if (hpsMc?.ShowMarkers == true && hpsVals != null && !hiddenLegendEntries.Contains(hpsLabel) && sourceEvents != null)
                    {
                        var filtered = sourceEvents.Where(e => e.IsHeal).ToList();
                        if (filtered.Count > 0)
                            PlotSkillMarkers(filtered, times, hpsVals, $"{combatant.Name}_hps", hpsMc);
                    }

                    // DTPS markers: enemy skills hitting this combatant, on the DTPS line
                    var dtpsLabel = metricCount > 1 ? $"{displayName} (DTPS)" : displayName;
                    if (dtpsMc?.ShowMarkers == true && dtpsVals != null && !hiddenLegendEntries.Contains(dtpsLabel))
                    {
                        List<SkillUseEvent> dtEvents;
                        if (isLive)
                        {
                            dtEvents = skillTracker.GetDamageTakenEvents(combatant.Name);
                            if (dtEvents.Count == 0
                                && snapshot?.DamageTakenEvents != null
                                && snapshot.DamageTakenEvents.TryGetValue(combatant.Name, out var dtFallback))
                            {
                                dtEvents = dtFallback;
                            }
                        }
                        else if (snapshot?.DamageTakenEvents != null
                            && snapshot.DamageTakenEvents.TryGetValue(combatant.Name, out var dtSaved))
                        {
                            dtEvents = dtSaved;
                        }
                        else
                        {
                            dtEvents = [];
                        }

                        if (dtEvents.Count > 0)
                            PlotSkillMarkers(dtEvents, times, dtpsVals, $"{combatant.Name}_dtps", dtpsMc);
                    }
                }
            }

            // Skill marker tooltip — find nearest marker across all combatants on hover.
            // Uses plot-space coordinates (from GetPlotMousePos, which accounts for zoom/pan)
            // with per-axis normalization by plot pixel size, giving uniform hover behaviour
            // regardless of where markers sit on the graph or the current zoom level.
            var anyMarkersEnabled = activeTab?.DpsMarkers.ShowMarkers == true
                                || activeTab?.HpsMarkers.ShowMarkers == true
                                || activeTab?.DtpsMarkers.ShowMarkers == true;
            if (anyMarkersEnabled && ImPlot.IsPlotHovered())
            {
                var mouse = ImPlot.GetPlotMousePos();
                var plotSize = ImPlot.GetPlotSize();

                var bestDist = float.MaxValue;
                SkillUseEvent? bestEvent = null;
                SkillMarkerConfig? bestMc = null;
                var bestIsDamageTaken = false;

                var ttDpsMc = activeTab?.DpsMarkers;
                var ttHpsMc = activeTab?.HpsMarkers;
                var ttDtpsMc = activeTab?.DtpsMarkers;

                foreach (var (combatant, samples) in allSeries)
                {
                    if (hiddenNames.Contains(combatant.Name)) continue;

                    var ts = new float[samples.Count];
                    var dv = showDps ? new float[samples.Count] : null;
                    var hv = showHps ? new float[samples.Count] : null;
                    var tv = showDtps ? new float[samples.Count] : null;
                    for (var i = 0; i < samples.Count; i++)
                    {
                        ts[i] = samples[i].TimeSec;
                        if (dv != null) dv[i] = samples[i].Dps;
                        if (hv != null) hv[i] = samples[i].Hps;
                        if (tv != null) tv[i] = samples[i].Dtps;
                    }

                    // Source-side events (damage dealt + heals cast)
                    List<SkillUseEvent>? sourceEvents = null;
                    if ((ttDpsMc?.ShowMarkers == true && dv != null)
                        || (ttHpsMc?.ShowMarkers == true && hv != null))
                    {
                        if (isLive)
                        {
                            sourceEvents = skillTracker.GetSkillEvents(combatant.Name);
                            if (sourceEvents.Count == 0
                                && snapshot?.SkillEvents != null
                                && snapshot.SkillEvents.TryGetValue(combatant.Name, out var fallbackEvt))
                            {
                                sourceEvents = fallbackEvt;
                            }
                        }
                        else if (snapshot?.SkillEvents != null
                            && snapshot.SkillEvents.TryGetValue(combatant.Name, out var saved))
                        {
                            sourceEvents = saved;
                        }
                    }

                    // DPS markers: non-heal events on DPS line
                    if (ttDpsMc?.ShowMarkers == true && dv != null && sourceEvents != null)
                    {
                        foreach (var ev in sourceEvents)
                        {
                            if (ev.IsHeal) continue;
                            FindNearest(ev, ts, dv, mouse, plotSize, maxTime, maxVal, false, ref bestDist, ref bestEvent, ref bestIsDamageTaken, ttDpsMc, ref bestMc);
                        }
                    }

                    // HPS markers: heal events on HPS line
                    if (ttHpsMc?.ShowMarkers == true && hv != null && sourceEvents != null)
                    {
                        foreach (var ev in sourceEvents)
                        {
                            if (!ev.IsHeal) continue;
                            FindNearest(ev, ts, hv, mouse, plotSize, maxTime, maxVal, false, ref bestDist, ref bestEvent, ref bestIsDamageTaken, ttHpsMc, ref bestMc);
                        }
                    }

                    // DTPS markers: damage-taken events on DTPS line
                    if (ttDtpsMc?.ShowMarkers == true && tv != null)
                    {
                        List<SkillUseEvent> dtEvents;
                        if (isLive)
                        {
                            dtEvents = skillTracker.GetDamageTakenEvents(combatant.Name);
                            if (dtEvents.Count == 0
                                && snapshot?.DamageTakenEvents != null
                                && snapshot.DamageTakenEvents.TryGetValue(combatant.Name, out var dtFallback))
                            {
                                dtEvents = dtFallback;
                            }
                        }
                        else if (snapshot?.DamageTakenEvents != null
                            && snapshot.DamageTakenEvents.TryGetValue(combatant.Name, out var dtSaved))
                        {
                            dtEvents = dtSaved;
                        }
                        else
                        {
                            dtEvents = [];
                        }

                        foreach (var ev in dtEvents)
                        {
                            FindNearest(ev, ts, tv, mouse, plotSize, maxTime, maxVal, true, ref bestDist, ref bestEvent, ref bestIsDamageTaken, ttDtpsMc, ref bestMc);
                        }
                    }
                }

                // Threshold in pseudo-pixel units (16 px radius when not zoomed,
                // proportionally tighter when zoomed in).
                var hoverRadiusSq = 16f * 16f;
                if (bestEvent.HasValue && bestDist <= hoverRadiusSq)
                {
                    ImGui.BeginTooltip();
                    ImGui.Text(bestEvent.Value.SkillName);
                    ImGui.Text(FormatValueLong(bestEvent.Value.Amount));
                    if (bestIsDamageTaken)
                        ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), "(Received)");
                    if (bestEvent.Value.IsDoTTick || bestEvent.Value.IsHoTTick)
                        ImGui.TextColored(bestMc?.DoTTickColor ?? new Vector4(0.6f, 0.2f, 0.8f, 0.9f),
                            bestEvent.Value.IsDoTTick ? "DoT Tick" : "HoT Tick");
                    else if (bestEvent.Value.IsDoTApplication || bestEvent.Value.IsHoTApplication)
                        ImGui.TextColored(bestMc?.DoTApplicationColor ?? new Vector4(0.9f, 0.3f, 0.9f, 0.95f),
                            bestEvent.Value.IsDoTApplication ? "DoT Application" : "HoT Application");
                    else if (bestMc?.ShowCritMarkers == true)
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

            // Detect legend entry clicks to track hidden state
            foreach (var (label, _) in legendLabels)
            {
                if (ImPlot.IsLegendEntryHovered(label) && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    if (!hiddenLegendEntries.Remove(label))
                        hiddenLegendEntries.Add(label);
                }
            }

            // Custom mouse position text — X axis only with "s" suffix
            if (ImPlot.IsPlotHovered())
            {
                var pos = ImPlot.GetPlotMousePos();
                var plotRect = ImPlot.GetPlotPos();
                var plotSize = ImPlot.GetPlotSize();
                var text = $"{pos.X:F1}s";
                var textSize = ImGui.CalcTextSize(text);
                var drawList = ImPlot.GetPlotDrawList();
                drawList.AddText(
                    new Vector2(plotRect.X + plotSize.X - textSize.X - 4, plotRect.Y + plotSize.Y - textSize.Y - 4),
                    ImGui.GetColorU32(new Vector4(1, 1, 1, config.GraphViewMouseTextOpacity)),
                    text);
            }

            // Middle-click context menu (right-click opens ImPlot's native axis controls)
            if (ImGui.BeginPopupContextItem("##GraphViewCtx", ImGuiPopupFlags.MouseButtonMiddle))
            {
                ImGui.TextDisabled("DamageTerror");
                ImGui.Separator();

                var autoScroll = config.GraphViewAutoScroll;
                if (ImGui.Checkbox("Auto-scroll", ref autoScroll))
                    config.GraphViewAutoScroll = autoScroll;

                if (config.GraphViewAutoScroll)
                {
                    ImGui.SetNextItemWidth(150);
                    var scrollWin = config.GraphViewAutoScrollWindow;
                    if (ImGui.SliderFloat("Window##gvctx", ref scrollWin, 15f, 300f, "%.0f sec"))
                        config.GraphViewAutoScrollWindow = scrollWin;

                    ImGui.SetNextItemWidth(150);
                    var scrollSmooth = config.GraphViewAutoScrollSmoothing;
                    if (ImGui.SliderFloat("Smoothing##gvctx", ref scrollSmooth, 1f, 30f, "%.1f"))
                        config.GraphViewAutoScrollSmoothing = scrollSmooth;
                }

                ImGui.Separator();

                var showLegend = config.GraphViewShowLegend;
                if (ImGui.Checkbox("Show Legend", ref showLegend))
                    config.GraphViewShowLegend = showLegend;

                var showGrid = config.GraphViewShowGrid;
                if (ImGui.Checkbox("Show Grid", ref showGrid))
                    config.GraphViewShowGrid = showGrid;

                var showLabels = config.GraphViewShowLabels;
                if (ImGui.Checkbox("Show Value Labels", ref showLabels))
                    config.GraphViewShowLabels = showLabels;

                ImGui.EndPopup();
            }

            ImPlot.EndPlot();
        }

        if (config.GraphViewShowGrid)
            ImPlot.PopStyleColor(); // AxisGrid

        ImPlot.PopStyleColor(2); // PlotBg, FrameBg

        ImGui.PopFont();
        ImGui.GetFont().Scale = prevScale;
    }

    private static void PlotScatterSubset(float[] allX, float[] allY, List<int> indices, string label, Vector4 color, float? markerSize = null)
    {
        if (indices.Count == 0) return;
        var sx = new float[indices.Count];
        var sy = new float[indices.Count];
        for (var i = 0; i < indices.Count; i++)
        {
            sx[i] = allX[indices[i]];
            sy[i] = allY[indices[i]];
        }
        if (markerSize.HasValue)
            ImPlot.PushStyleVar(ImPlotStyleVar.MarkerSize, markerSize.Value);
        ImPlot.PushStyleColor(ImPlotCol.MarkerFill, color);
        ImPlot.PushStyleColor(ImPlotCol.MarkerOutline, color);
        ImPlot.PlotScatter(label, ref sx[0], ref sy[0], indices.Count);
        ImPlot.PopStyleColor(2);
        if (markerSize.HasValue)
            ImPlot.PopStyleVar();
    }

    /// <summary>Plot skill use markers for a list of events, interpolated onto the given value line.</summary>
    private static void PlotSkillMarkers(List<SkillUseEvent> events, float[] times, float[] values, string idPrefix, SkillMarkerConfig mc)
    {
        var mX = new float[events.Count];
        var mY = new float[events.Count];
        for (var ei = 0; ei < events.Count; ei++)
        {
            mX[ei] = events[ei].TimeSec;
            mY[ei] = InterpolateValue(times, values, events[ei].TimeSec);
        }

        // Separate DoT/HoT ticks and applications from regular ability events
        List<int> dotTickIdx = [], dotAppIdx = [], regularIdx = [];
        for (var ei = 0; ei < events.Count; ei++)
        {
            var ev = events[ei];
            if (ev.IsDoTTick || ev.IsHoTTick)
                dotTickIdx.Add(ei);
            else if (ev.IsDoTApplication || ev.IsHoTApplication)
                dotAppIdx.Add(ei);
            else
                regularIdx.Add(ei);
        }

        // Plot DoT/HoT tick markers (diamond shape via separate size)
        if (mc.ShowDoTTickMarkers && dotTickIdx.Count > 0)
            PlotScatterSubset(mX, mY, dotTickIdx, $"##{idPrefix}_skills_dot", mc.DoTTickColor, mc.DoTTickMarkerSize);

        // Plot DoT/HoT application markers (larger, distinct color)
        if (mc.ShowDoTApplicationMarkers && dotAppIdx.Count > 0)
            PlotScatterSubset(mX, mY, dotAppIdx, $"##{idPrefix}_skills_dotapp", mc.DoTApplicationColor, mc.DoTApplicationMarkerSize);

        // Plot regular ability markers with crit/DH split
        if (regularIdx.Count > 0)
        {
            ImPlot.PushStyleVar(ImPlotStyleVar.MarkerSize, mc.MarkerSize);

            if (mc.ShowCritMarkers)
            {
                List<int> normalIdx = [], critIdx = [], dhIdx = [], cdhIdx = [];
                foreach (var ei in regularIdx)
                {
                    var ev = events[ei];
                    if (ev.IsCrit && ev.IsDirectHit) cdhIdx.Add(ei);
                    else if (ev.IsDirectHit) dhIdx.Add(ei);
                    else if (ev.IsCrit) critIdx.Add(ei);
                    else normalIdx.Add(ei);
                }

                PlotScatterSubset(mX, mY, normalIdx, $"##{idPrefix}_skills_n", mc.MarkerColor);
                PlotScatterSubset(mX, mY, critIdx, $"##{idPrefix}_skills_c", mc.CritMarkerColor);
                PlotScatterSubset(mX, mY, dhIdx, $"##{idPrefix}_skills_dh", mc.DirectHitMarkerColor);
                PlotScatterSubset(mX, mY, cdhIdx, $"##{idPrefix}_skills_cdh", mc.CritDirectHitMarkerColor);
            }
            else
            {
                PlotScatterSubset(mX, mY, regularIdx, $"##{idPrefix}_skills", mc.MarkerColor);
            }

            ImPlot.PopStyleVar();
        }
    }

    /// <summary>Check if a skill event is the nearest marker to the mouse, updating best-match state.</summary>
    private static void FindNearest(SkillUseEvent ev, float[] times, float[] values,
        ImPlotPoint mouse, Vector2 plotSize, float maxTime, float maxVal,
        bool isDamageTaken, ref float bestDist, ref SkillUseEvent? bestEvent, ref bool bestIsDamageTaken,
        SkillMarkerConfig mc, ref SkillMarkerConfig? bestMc)
    {
        var y = InterpolateValue(times, values, ev.TimeSec);
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

    private static string FormatValue(float val)
    {
        if (val >= 1_000_000) return $"{val / 1_000_000:F1}M";
        if (val >= 10_000) return $"{val / 1_000:F1}K";
        return $"{val:F0}";
    }

    private static string FormatValueLong(long val)
    {
        if (val >= 1_000_000) return $"{val / 1_000_000.0:F1}M";
        if (val >= 10_000) return $"{val / 1_000.0:F1}K";
        return val.ToString();
    }

    private static float InterpolateValue(float[] times, float[]? values, float t)
    {
        if (values == null || values.Length == 0) return 0f;
        if (values.Length == 1) return values[0];
        if (t <= times[0]) return values[0];
        if (t >= times[^1]) return values[^1];

        for (var i = 1; i < times.Length; i++)
        {
            if (t <= times[i])
            {
                var frac = (t - times[i - 1]) / (times[i] - times[i - 1]);
                return values[i - 1] + frac * (values[i] - values[i - 1]);
            }
        }

        return values[^1];
    }

    private static void SetupAbbreviatedYTicks(float maxVal, float headroomMultiplier, int targetTickCount)
    {
        var headroom = maxVal * headroomMultiplier;
        var step = headroom / targetTickCount;

        // Round step to a nice number
        var magnitude = MathF.Pow(10f, MathF.Floor(MathF.Log10(step)));
        var normalized = step / magnitude;
        float niceStep;
        if (normalized <= 1f) niceStep = magnitude;
        else if (normalized <= 2f) niceStep = 2f * magnitude;
        else if (normalized <= 5f) niceStep = 5f * magnitude;
        else niceStep = 10f * magnitude;

        var ticks = new List<double>();
        var labels = new List<string>();
        for (var v = niceStep; v <= headroom; v += niceStep)
        {
            ticks.Add(v);
            if (v >= 1_000_000) labels.Add($"{v / 1_000_000:F1}M");
            else if (v >= 1_000) labels.Add($"{v / 1_000:G4}K");
            else labels.Add($"{v:F0}");
        }

        if (ticks.Count > 0)
        {
            var tickArr = ticks.ToArray();
            var labelArr = labels.ToArray();
            ImPlot.SetupAxisTicks(ImAxis.Y1, ref tickArr[0], ticks.Count, labelArr, false);
        }
    }
}
