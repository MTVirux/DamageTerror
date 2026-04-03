using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImPlot;
using DamageTerror.Services;
using ImGui = Dalamud.Bindings.ImGui.ImGui;
using ImPlot = Dalamud.Bindings.ImPlot.ImPlot;

namespace DamageTerror.Gui.MainWindow;

public class CombatantDetailPanel
{
    private readonly Configuration config;
    private readonly GraphDataTracker graphTracker;
    private readonly SkillTracker skillTracker;
    private int expandedIndex = -1;
    private bool wasActivelyUpdating;
    private double scrollXMin = double.NaN;
    private double scrollXMax = double.NaN;

    private static readonly BarColumn[] DamageSection = { BarColumn.Dps, BarColumn.InstantDps, BarColumn.PeakDps, BarColumn.Damage, BarColumn.DamagePercent, BarColumn.MaxHit, BarColumn.DamageShield };
    private static readonly BarColumn[] HealingSection = { BarColumn.Hps, BarColumn.InstantHps, BarColumn.Healed, BarColumn.HealPercent, BarColumn.Overheal, BarColumn.OverhealAmount, BarColumn.CritHealPct, BarColumn.MaxHeal, BarColumn.MaxHealWard, BarColumn.HealCount, BarColumn.AbsorbHeal };
    private static readonly BarColumn[] HitStatSection = { BarColumn.Crit, BarColumn.DirectHit, BarColumn.CritDirectHit, BarColumn.HitRate, BarColumn.Swings, BarColumn.Hits, BarColumn.Misses, BarColumn.CritHitCount, BarColumn.DirectHitCount, BarColumn.CritDirectHitCount };
    private static readonly BarColumn[] DefenseSection = { BarColumn.DamageTaken, BarColumn.DamageTakenPercent, BarColumn.BlockPct, BarColumn.ParryPct, BarColumn.HealsTaken };
    private static readonly BarColumn[] OtherSection = { BarColumn.Deaths, BarColumn.Kills, BarColumn.CombatantDuration, BarColumn.PowerDrain, BarColumn.PowerHeal };

    private EncounterSnapshot? currentSnapshot;
    private bool isLive;

    public CombatantDetailPanel(Configuration config, GraphDataTracker graphTracker, SkillTracker skillTracker)
    {
        this.config = config;
        this.graphTracker = graphTracker;
        this.skillTracker = skillTracker;
    }

    private bool PersistentTreeNode(string label, string id)
    {
        var key = label;
        var isOpen = config.DetailExpandedSections.Contains(key);
        var flags = isOpen ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None;
        var nowOpen = ImGui.TreeNodeEx($"{label}##{id}", flags);
        if (nowOpen && !isOpen)
        {
            config.DetailExpandedSections.Add(key);
            config.Save?.Invoke();
        }
        else if (!nowOpen && isOpen)
        {
            config.DetailExpandedSections.Remove(key);
            config.Save?.Invoke();
        }
        return nowOpen;
    }

    public void Toggle(int index)
    {
        expandedIndex = expandedIndex == index ? -1 : index;
    }

    public bool IsExpanded(int index) => expandedIndex == index;

    public void Render(CombatantEntry combatant, int index, EncounterSnapshot? snapshot, bool isLive, MeterTab? activeTab = null)
    {
        if (expandedIndex != index)
            return;

        currentSnapshot = snapshot;
        this.isLive = isLive;

        var vis = config.DetailVisibleColumns;
        var lc = config.DetailLabelColor;
        ImGui.Indent(config.DetailIndent);

        var prevScale = ImGui.GetFont().Scale;
        ImGui.GetFont().Scale = config.GetFontScale(config.DetailFontSize);
        ImGui.PushFont(ImGui.GetFont());

        if (ImGui.BeginTabBar($"##detailTabs_{index}", ImGuiTabBarFlags.Reorderable))
        {
            if (ImGui.BeginTabItem($"Details##{index}"))
            {
                DrawDetailsTab(combatant, index, vis, lc);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem($"Skills##{index}"))
            {
                DrawSkillsTab(combatant, index);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem($"Graph##{index}"))
            {
                DrawGraphTab(combatant, index, activeTab);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.GetFont().Scale = prevScale;
        ImGui.PopFont();

        ImGui.Unindent(config.DetailIndent);
        ImGui.Spacing();
    }

    private void DrawGraphTab(CombatantEntry combatant, int index, MeterTab? activeTab)
    {
        List<GraphSample> samples;
        if (isLive)
        {
            samples = graphTracker.GetSamples(combatant.Name);
            // Fall back to stored data when live tracker is empty
            // (e.g. encounter restored from history after plugin reload)
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
        var graphH = config.GraphAutoHeight
            ? Math.Max(60f, ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing())
            : config.GraphHeight;
        var thickness = config.GraphLineThickness;

        // Apply graph-specific font scaling
        var prevGraphScale = ImGui.GetFont().Scale;
        ImGui.GetFont().Scale = config.GetFontScale(config.GraphFontSize);
        ImGui.PushFont(ImGui.GetFont());

        // Extract arrays
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

        ImPlot.PushStyleColor(ImPlotCol.Bg, config.GraphBackgroundColor);
        ImPlot.PushStyleColor(ImPlotCol.FrameBg, new Vector4(0, 0, 0, 0));

        var plotFlags = ImPlotFlags.NoMouseText;
        if (!config.GraphShowLegend)
            plotFlags |= ImPlotFlags.NoLegend;

        var xAxisFlags = ImPlotAxisFlags.None;
        if (!config.GraphShowGrid)
            xAxisFlags |= ImPlotAxisFlags.NoGridLines;
        if (!config.GraphShowXAxisLabels)
            xAxisFlags |= ImPlotAxisFlags.NoTickLabels;

        var yAxisFlags = ImPlotAxisFlags.AutoFit;
        if (!config.GraphShowGrid)
            yAxisFlags |= ImPlotAxisFlags.NoGridLines;
        if (!config.GraphShowYAxisLabels)
            yAxisFlags |= ImPlotAxisFlags.NoTickLabels;

        if (config.GraphShowGrid)
        {
            ImPlot.PushStyleColor(ImPlotCol.AxisGrid, config.GraphGridColor);
        }

        // Compute max time and max value for axis limits
        var maxTime = times[^1];
        if (maxTime <= 0f) maxTime = 1f;
        var maxVal = 0f;
        for (var i = 0; i < samples.Count; i++)
        {
            if (dpsVals != null && dpsVals[i] > maxVal) maxVal = dpsVals[i];
            if (hpsVals != null && hpsVals[i] > maxVal) maxVal = hpsVals[i];
            if (dtpsVals != null && dtpsVals[i] > maxVal) maxVal = dtpsVals[i];
        }

        if (ImPlot.BeginPlot($"##DetailGraph_{index}", new Vector2(regionW, graphH), plotFlags))
        {
            ImPlot.SetupAxes("", "", xAxisFlags, yAxisFlags);
            // Use Always when actively receiving data to keep axis locked; Once when done to allow zoom/pan
            var isActivelyUpdating = isLive && currentSnapshot?.Encounter.IsActive == true;
            var justEnded = wasActivelyUpdating && !isActivelyUpdating;
            wasActivelyUpdating = isActivelyUpdating;
            var axisLimitCond = (isActivelyUpdating || justEnded) ? ImPlotCond.Always : ImPlotCond.Once;

            if (config.GraphAutoScroll && isActivelyUpdating && maxTime > config.GraphAutoScrollWindow)
            {
                var windowSec = config.GraphAutoScrollWindow;
                var targetMin = maxTime - windowSec;
                var targetMax = maxTime + windowSec * (config.GraphXAxisPadding - 1f);
                var dt = ImGui.GetIO().DeltaTime;
                var speed = config.GraphAutoScrollSmoothing;
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
                ImPlot.SetupAxisLimits(ImAxis.X1, 0, maxTime * config.GraphXAxisPadding, axisLimitCond);
            }
            // Prevent panning below 0
            ImPlot.SetupAxisLimitsConstraints(ImAxis.X1, 0, double.MaxValue);

            // Custom Y-axis ticks with K/M abbreviations, skipping 0
            if (maxVal > 0f) SetupAbbreviatedYTicks(maxVal, config.GraphYAxisHeadroom, config.GraphYAxisTickCount);

            var labelOffset = new Vector2(config.GraphLabelOffsetX, config.GraphLabelOffsetY);

            if (dpsVals != null)
            {
                ImPlot.PushStyleColor(ImPlotCol.Line, config.GraphDpsColor);
                ImPlot.PushStyleVar(ImPlotStyleVar.LineWeight, thickness);
                ImPlot.PlotLine("iDPS", ref times[0], ref dpsVals[0], samples.Count);
                ImPlot.PopStyleVar();
                ImPlot.PopStyleColor();

                if (config.GraphShowLabels)
                {
                    var lastVal = dpsVals[^1];
                    ImPlot.PushStyleColor(ImPlotCol.InlayText, config.GraphDpsColor);
                    ImPlot.PlotText(FormatValue(lastVal), times[^1], lastVal, labelOffset);
                    ImPlot.PopStyleColor();
                }
            }

            if (hpsVals != null)
            {
                ImPlot.PushStyleColor(ImPlotCol.Line, config.GraphHpsColor);
                ImPlot.PushStyleVar(ImPlotStyleVar.LineWeight, thickness);
                ImPlot.PlotLine("iHPS", ref times[0], ref hpsVals[0], samples.Count);
                ImPlot.PopStyleVar();
                ImPlot.PopStyleColor();

                if (config.GraphShowLabels)
                {
                    var lastVal = hpsVals[^1];
                    ImPlot.PushStyleColor(ImPlotCol.InlayText, config.GraphHpsColor);
                    ImPlot.PlotText(FormatValue(lastVal), times[^1], lastVal, labelOffset);
                    ImPlot.PopStyleColor();
                }
            }

            if (dtpsVals != null)
            {
                ImPlot.PushStyleColor(ImPlotCol.Line, config.GraphDtpsColor);
                ImPlot.PushStyleVar(ImPlotStyleVar.LineWeight, thickness);
                ImPlot.PlotLine("iDTPS", ref times[0], ref dtpsVals[0], samples.Count);
                ImPlot.PopStyleVar();
                ImPlot.PopStyleColor();

                if (config.GraphShowLabels)
                {
                    var lastVal = dtpsVals[^1];
                    ImPlot.PushStyleColor(ImPlotCol.InlayText, config.GraphDtpsColor);
                    ImPlot.PlotText(FormatValue(lastVal), times[^1], lastVal, labelOffset);
                    ImPlot.PopStyleColor();
                }
            }

            // ── Skill use markers (per-metric, using active tab config) ──
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
                        && currentSnapshot?.SkillEvents != null
                        && currentSnapshot.SkillEvents.TryGetValue(combatant.Name, out var fallbackEvt))
                    {
                        sourceEvents = fallbackEvt;
                    }
                }
                else if (currentSnapshot?.SkillEvents != null
                    && currentSnapshot.SkillEvents.TryGetValue(combatant.Name, out var saved))
                {
                    sourceEvents = saved;
                }
            }

            // DPS markers: damage events (non-heal) on the DPS line
            if (dpsMc?.ShowMarkers == true && dpsVals != null && sourceEvents != null)
            {
                var filtered = sourceEvents.Where(e => !e.IsHeal).ToList();
                if (filtered.Count > 0)
                    PlotSkillMarkers(filtered, times, dpsVals, $"detail_{index}_dps", dpsMc);
            }

            // HPS markers: healing events on the HPS line
            if (hpsMc?.ShowMarkers == true && hpsVals != null && sourceEvents != null)
            {
                var filtered = sourceEvents.Where(e => e.IsHeal).ToList();
                if (filtered.Count > 0)
                    PlotSkillMarkers(filtered, times, hpsVals, $"detail_{index}_hps", hpsMc);
            }

            // DTPS markers: damage-taken events on the DTPS line
            if (dtpsMc?.ShowMarkers == true && dtpsVals != null)
            {
                List<SkillUseEvent> dtEvents;
                if (isLive)
                {
                    dtEvents = skillTracker.GetDamageTakenEvents(combatant.Name);
                    if (dtEvents.Count == 0
                        && currentSnapshot?.DamageTakenEvents != null
                        && currentSnapshot.DamageTakenEvents.TryGetValue(combatant.Name, out var dtFallback))
                    {
                        dtEvents = dtFallback;
                    }
                }
                else if (currentSnapshot?.DamageTakenEvents != null
                    && currentSnapshot.DamageTakenEvents.TryGetValue(combatant.Name, out var dtSaved))
                {
                    dtEvents = dtSaved;
                }
                else
                {
                    dtEvents = [];
                }

                if (dtEvents.Count > 0)
                    PlotSkillMarkers(dtEvents, times, dtpsVals, $"detail_{index}_dtps", dtpsMc);
            }

            // Skill marker tooltip — find nearest marker across all metrics on hover
            var anyMarkersEnabled = dpsMc?.ShowMarkers == true
                                 || hpsMc?.ShowMarkers == true
                                 || dtpsMc?.ShowMarkers == true;
            if (anyMarkersEnabled && ImPlot.IsPlotHovered())
            {
                var mouse = ImPlot.GetPlotMousePos();
                var bestDist = float.MaxValue;
                SkillUseEvent? bestEvent = null;
                SkillMarkerConfig? bestMc = null;

                void FindNearestDetail(SkillUseEvent ev, float[] ts, float[]? vals, SkillMarkerConfig mc)
                {
                    if (vals == null) return;
                    var y = InterpolateValue(ts, vals, ev.TimeSec);
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

                // DPS events
                if (dpsMc?.ShowMarkers == true && sourceEvents != null)
                    foreach (var ev in sourceEvents.Where(e => !e.IsHeal))
                        FindNearestDetail(ev, times, dpsVals, dpsMc);

                // HPS events
                if (hpsMc?.ShowMarkers == true && sourceEvents != null)
                    foreach (var ev in sourceEvents.Where(e => e.IsHeal))
                        FindNearestDetail(ev, times, hpsVals, hpsMc);

                // DTPS events
                if (dtpsMc?.ShowMarkers == true && dtpsVals != null)
                {
                    List<SkillUseEvent> dtEventsForTt;
                    if (isLive)
                    {
                        dtEventsForTt = skillTracker.GetDamageTakenEvents(combatant.Name);
                        if (dtEventsForTt.Count == 0
                            && currentSnapshot?.DamageTakenEvents != null
                            && currentSnapshot.DamageTakenEvents.TryGetValue(combatant.Name, out var dtFb))
                        {
                            dtEventsForTt = dtFb;
                        }
                    }
                    else if (currentSnapshot?.DamageTakenEvents != null
                        && currentSnapshot.DamageTakenEvents.TryGetValue(combatant.Name, out var dtS))
                    {
                        dtEventsForTt = dtS;
                    }
                    else
                    {
                        dtEventsForTt = [];
                    }

                    foreach (var ev in dtEventsForTt)
                        FindNearestDetail(ev, times, dtpsVals, dtpsMc);
                }

                if (bestEvent.HasValue && bestDist < 0.03f * 0.03f + 0.08f * 0.08f)
                {
                    ImGui.BeginTooltip();
                    ImGui.Text(bestEvent.Value.SkillName);
                    ImGui.Text(FormatValue(bestEvent.Value.Amount));
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

            // Custom mouse position text — X axis only with "s" suffix
            if (ImPlot.IsPlotHovered())
            {
                var mousePos = ImPlot.GetPlotMousePos();
                var plotRect = ImPlot.GetPlotPos();
                var plotSize = ImPlot.GetPlotSize();
                var text = $"{mousePos.X:F1}s";
                var textSize = ImGui.CalcTextSize(text);
                var drawList = ImPlot.GetPlotDrawList();
                drawList.AddText(
                    new Vector2(plotRect.X + plotSize.X - textSize.X - 4, plotRect.Y + plotSize.Y - textSize.Y - 4),
                    ImGui.GetColorU32(new Vector4(1, 1, 1, config.GraphMouseTextOpacity)),
                    text);
            }

            // Middle-click context menu (right-click opens ImPlot's native axis controls)
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

                ImGui.EndPopup();
            }

            ImPlot.EndPlot();
        }

        if (config.GraphShowGrid)
            ImPlot.PopStyleColor(); // AxisGrid

        ImPlot.PopStyleColor(2); // PlotBg, FrameBg

        ImGui.PopFont();
        ImGui.GetFont().Scale = prevGraphScale;

        ImGui.Spacing();
    }

    private static void PlotScatterSubset(float[] allX, float[] allY, List<int> indices, string label, Vector4 color)
    {
        if (indices.Count == 0) return;
        var sx = new float[indices.Count];
        var sy = new float[indices.Count];
        for (var i = 0; i < indices.Count; i++)
        {
            sx[i] = allX[indices[i]];
            sy[i] = allY[indices[i]];
        }
        ImPlot.PushStyleColor(ImPlotCol.MarkerFill, color);
        ImPlot.PushStyleColor(ImPlotCol.MarkerOutline, color);
        ImPlot.PlotScatter(label, ref sx[0], ref sy[0], indices.Count);
        ImPlot.PopStyleColor(2);
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

        ImPlot.PushStyleVar(ImPlotStyleVar.MarkerSize, mc.MarkerSize);

        if (mc.ShowCritMarkers)
        {
            List<int> normalIdx = [], critIdx = [], dhIdx = [], cdhIdx = [];
            for (var ei = 0; ei < events.Count; ei++)
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
            ImPlot.PushStyleColor(ImPlotCol.MarkerFill, mc.MarkerColor);
            ImPlot.PushStyleColor(ImPlotCol.MarkerOutline, mc.MarkerColor);
            ImPlot.PlotScatter($"##{idPrefix}_skills", ref mX[0], ref mY[0], events.Count);
            ImPlot.PopStyleColor(2);
        }

        ImPlot.PopStyleVar();
    }

    private static string FormatValue(float val)
    {
        if (val >= 1_000_000) return $"{val / 1_000_000:F1}M";
        if (val >= 10_000) return $"{val / 1_000:F1}K";
        return $"{val:F0}";
    }

    private static string FormatValue(long val)
    {
        if (val >= 1_000_000) return $"{val / 1_000_000.0:F1}M";
        if (val >= 10_000) return $"{val / 1_000.0:F1}K";
        return val.ToString();
    }

    /// <summary>Linearly interpolate a Y value from sample arrays at a given time.</summary>
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

    private void DrawSkillsTab(CombatantEntry combatant, int index)
    {
        if (config.DetailShowSkillBreakdown && combatant.Skills.Count > 0)
        {
            ImGui.Spacing();
            if (PersistentTreeNode("Damage Skills", index.ToString()))
            {
                DrawSkillTable(combatant.Skills, index, "dmg", config.SkillDamageFillColor);
                ImGui.TreePop();
            }
        }

        if (config.DetailShowSkillBreakdown && combatant.HealingSkills.Count > 0)
        {
            ImGui.Spacing();
            if (PersistentTreeNode("Healing Skills", index.ToString()))
            {
                DrawSkillTable(combatant.HealingSkills, index, "heal", config.SkillHealingFillColor);
                ImGui.TreePop();
            }
        }

        if (!config.DetailShowSkillBreakdown || (combatant.Skills.Count == 0 && combatant.HealingSkills.Count == 0))
        {
            ImGui.Spacing();
            ImGui.TextDisabled("No skill data available.");
            ImGui.Spacing();
        }
    }

    private void DrawDetailsTab(CombatantEntry combatant, int index, HashSet<BarColumn> vis, Vector4 lc)
    {
        ImGui.Spacing();

        // ── Damage ──
        if (HasAny(vis, DamageSection))
        {
            if (PersistentTreeNode("Damage", index.ToString()))
            {
                DrawRow(lc,
                    (vis.Contains(BarColumn.Dps), "DPS", Fmt(combatant.EncDps)),
                    (vis.Contains(BarColumn.InstantDps), "iDPS", Fmt(combatant.InstantDps)),
                    (vis.Contains(BarColumn.PeakDps), "Peak", Fmt(combatant.PeakDps)));

                if (vis.Contains(BarColumn.Damage))
                {
                    var dmg = Fmt(combatant.Damage);
                    if (vis.Contains(BarColumn.DamagePercent))
                        dmg += $"  ({combatant.DamagePercent})";
                    DrawRow(lc, (true, "Total", dmg));
                }
                else if (vis.Contains(BarColumn.DamagePercent))
                {
                    DrawRow(lc, (true, "Dmg %", combatant.DamagePercent));
                }

                if (vis.Contains(BarColumn.MaxHit) && !string.IsNullOrEmpty(combatant.MaxHit))
                    DrawRow(lc, (true, "Max Hit", $"{combatant.MaxHit} ({Fmt(combatant.MaxHitDamage)})"));

                if (vis.Contains(BarColumn.DamageShield))
                    DrawRow(lc, (true, "Shield", Fmt(combatant.DamageShield)));

                if (config.DetailShowDpsTrend && (combatant.Last10Dps > 0 || combatant.Last30Dps > 0 || combatant.Last60Dps > 0))
                {
                    ImGui.TextColored(lc, "DPS 10s/30s/60s:");
                    ImGui.SameLine();
                    ImGui.TextUnformatted($"{Fmt(combatant.Last10Dps)} / {Fmt(combatant.Last30Dps)} / {Fmt(combatant.Last60Dps)}");
                }

                ImGui.TreePop();
            }
        }

        // ── Healing ──
        if (HasAny(vis, HealingSection))
        {
            if (PersistentTreeNode("Healing", index.ToString()))
            {
                DrawRow(lc,
                    (vis.Contains(BarColumn.Hps), "HPS", Fmt(combatant.EncHps)),
                    (vis.Contains(BarColumn.InstantHps), "iHPS", Fmt(combatant.InstantHps)));

                if (vis.Contains(BarColumn.Healed))
                {
                    var heal = Fmt(combatant.Healed);
                    if (vis.Contains(BarColumn.HealPercent))
                        heal += $"  ({combatant.HealedPercent})";
                    DrawRow(lc, (true, "Total", heal));
                }
                else if (vis.Contains(BarColumn.HealPercent))
                {
                    DrawRow(lc, (true, "Heal %", combatant.HealedPercent));
                }

                DrawRow(lc,
                    (vis.Contains(BarColumn.Overheal), "Overheal", FmtPct(combatant.OverhealPct)),
                    (vis.Contains(BarColumn.OverhealAmount), "OH Amt", Fmt(combatant.OverhealAmount)));

                DrawRow(lc,
                    (vis.Contains(BarColumn.CritHealPct), "Crit Heal", FmtPct(combatant.CritHealPct)),
                    (vis.Contains(BarColumn.HealCount), "Heals", combatant.HealCount.ToString()));

                if (vis.Contains(BarColumn.MaxHeal) && !string.IsNullOrEmpty(combatant.MaxHeal))
                    DrawRow(lc, (true, "Max Heal", $"{combatant.MaxHeal} ({Fmt(combatant.MaxHealAmount)})"));

                if (vis.Contains(BarColumn.MaxHealWard) && !string.IsNullOrEmpty(combatant.MaxHealWardName))
                    DrawRow(lc, (true, "Max Ward", $"{combatant.MaxHealWardName} ({Fmt(combatant.MaxHealWardAmount)})"));

                if (vis.Contains(BarColumn.AbsorbHeal))
                    DrawRow(lc, (true, "Absorb", Fmt(combatant.AbsorbHeal)));

                ImGui.TreePop();
            }
        }

        // ── Hit Statistics ──
        if (HasAny(vis, HitStatSection))
        {
            if (PersistentTreeNode("Hit Statistics", index.ToString()))
            {
                DrawRow(lc,
                    (vis.Contains(BarColumn.Crit), "Crit", FmtPct(combatant.CritPct)),
                    (vis.Contains(BarColumn.DirectHit), "DH", FmtPct(combatant.DirectHitPct)),
                    (vis.Contains(BarColumn.CritDirectHit), "CDH", FmtPct(combatant.CritDirectHitPct)));

                if (vis.Contains(BarColumn.HitRate))
                    DrawRow(lc, (true, "Hit Rate", FmtPct(combatant.HitRate)));

                DrawRow(lc,
                    (vis.Contains(BarColumn.Swings), "Swings", combatant.Swings.ToString()),
                    (vis.Contains(BarColumn.Hits), "Hits", combatant.Hits.ToString()),
                    (vis.Contains(BarColumn.Misses), "Misses", combatant.Misses.ToString()));

                DrawRow(lc,
                    (vis.Contains(BarColumn.CritHitCount), "Crit#", combatant.CritHitCount.ToString()),
                    (vis.Contains(BarColumn.DirectHitCount), "DH#", combatant.DirectHitCount.ToString()),
                    (vis.Contains(BarColumn.CritDirectHitCount), "CDH#", combatant.CritDirectHitCount.ToString()));

                ImGui.TreePop();
            }
        }

        // ── Defense ──
        if (HasAny(vis, DefenseSection))
        {
            if (PersistentTreeNode("Defense", index.ToString()))
            {
                if (vis.Contains(BarColumn.DamageTaken))
                {
                    var taken = Fmt(combatant.DamageTaken);
                    if (vis.Contains(BarColumn.DamageTakenPercent))
                        taken += $"  ({combatant.DamageTakenPercent})";
                    DrawRow(lc, (true, "Taken", taken));
                }
                else if (vis.Contains(BarColumn.DamageTakenPercent))
                {
                    DrawRow(lc, (true, "Taken %", combatant.DamageTakenPercent));
                }

                DrawRow(lc,
                    (vis.Contains(BarColumn.BlockPct), "Block", FmtPct(combatant.BlockPct)),
                    (vis.Contains(BarColumn.ParryPct), "Parry", FmtPct(combatant.ParryPct)));

                if (vis.Contains(BarColumn.HealsTaken))
                    DrawRow(lc, (true, "Heals Taken", Fmt(combatant.HealsTaken)));

                ImGui.TreePop();
            }
        }

        // ── Other ──
        if (HasAny(vis, OtherSection))
        {
            if (PersistentTreeNode("Other", index.ToString()))
            {
                var deathsVis = vis.Contains(BarColumn.Deaths);
                var killsVis = vis.Contains(BarColumn.Kills);
                if (deathsVis || killsVis)
                {
                    var first = true;
                    if (deathsVis)
                    {
                        ImGui.TextColored(lc, "Deaths:");
                        ImGui.SameLine();
                        if (combatant.Deaths > 0)
                            ImGui.TextColored(config.DetailDeathColor, combatant.Deaths.ToString());
                        else
                            ImGui.TextUnformatted("0");
                        first = false;
                    }
                    if (killsVis)
                    {
                        if (!first) ImGui.SameLine();
                        if (!first) { ImGui.TextColored(lc, "  Kills:"); } else { ImGui.TextColored(lc, "Kills:"); }
                        ImGui.SameLine();
                        ImGui.TextUnformatted(combatant.Kills.ToString());
                    }
                }

                if (vis.Contains(BarColumn.CombatantDuration))
                    DrawRow(lc, (true, "Duration", combatant.CombatantDuration));

                DrawRow(lc,
                    (vis.Contains(BarColumn.PowerDrain), "MP Drain", Fmt(combatant.PowerDrain)),
                    (vis.Contains(BarColumn.PowerHeal), "Power Heal", Fmt(combatant.PowerHeal)));

                ImGui.TreePop();
            }
        }
    }

    private string Fmt(double value) => ValueFormatter.Format(value, config);
    private string FmtPct(double value) => ValueFormatter.FormatPercent(value, config.PercentDecimalPlaces);

    private static bool HasAny(HashSet<BarColumn> vis, BarColumn[] cols)
    {
        foreach (var c in cols)
            if (vis.Contains(c)) return true;
        return false;
    }

    private static void DrawRow(Vector4 lc, params (bool visible, string label, string value)[] metrics)
    {
        var first = true;
        foreach (var (visible, label, value) in metrics)
        {
            if (!visible) continue;
            if (first)
            {
                ImGui.TextColored(lc, $"{label}:");
                ImGui.SameLine();
                ImGui.TextUnformatted(value);
                first = false;
            }
            else
            {
                ImGui.SameLine();
                ImGui.TextColored(lc, $"  {label}:");
                ImGui.SameLine();
                ImGui.TextUnformatted(value);
            }
        }
    }

    private void DrawSkillTable(List<SkillEntry> skills, int index, string idPrefix, Vector4 fillColorVec)
    {
        var availWidth = ImGui.GetContentRegionAvail().X;
        var skillBarHeight = config.SkillRowHeight;
        var maxSkillVal = skills[0].TotalDamage; // Already sorted descending
        var drawList = ImGui.GetWindowDrawList();
        var bgColor = ImGui.ColorConvertFloat4ToU32(config.SkillRowBackgroundColor);
        var fillColor = ImGui.ColorConvertFloat4ToU32(fillColorVec);
        var physFillColor = ImGui.ColorConvertFloat4ToU32(config.SkillPhysicalFillColor);
        var magFillColor = ImGui.ColorConvertFloat4ToU32(config.SkillMagicFillColor);
        var textColor = ImGui.ColorConvertFloat4ToU32(config.SkillTextColor);
        var skillRounding = config.SkillBarRounding;

        var prevSkillScale = ImGui.GetFont().Scale;
        ImGui.GetFont().Scale = config.GetFontScale(config.SkillFontSize);
        ImGui.PushFont(ImGui.GetFont());

        var topSkills = config.MaxSkillBreakdownCount > 0 ? skills.Take(config.MaxSkillBreakdownCount).ToList() : skills;
        var headerColor = ImGui.ColorConvertFloat4ToU32(config.SkillHeaderTextColor);
        var colPad = config.SkillColumnPadding;

        float colValW = ImGui.CalcTextSize("Amount").X;
        float colPctW = ImGui.CalcTextSize("%").X;
        float colHitsW = ImGui.CalcTextSize("Hits").X;
        float colCritW = ImGui.CalcTextSize("!").X;
        float colDhW = ImGui.CalcTextSize("!!").X;
        float colCdhW = ImGui.CalcTextSize("!!!").X;

        foreach (var s in topSkills)
        {
            colValW = Math.Max(colValW, ImGui.CalcTextSize(ValueFormatter.Format(s.TotalDamage, config)).X);
            colPctW = Math.Max(colPctW, ImGui.CalcTextSize(ValueFormatter.FormatPercent(s.DamagePercent, config.PercentDecimalPlaces)).X);
            colHitsW = Math.Max(colHitsW, ImGui.CalcTextSize($"x{s.HitCount}").X);
            colCritW = Math.Max(colCritW, ImGui.CalcTextSize(ValueFormatter.FormatPercent(s.CritPct, config.PercentDecimalPlaces)).X);
            colDhW = Math.Max(colDhW, ImGui.CalcTextSize(ValueFormatter.FormatPercent(s.DirectHitPct, config.PercentDecimalPlaces)).X);
            colCdhW = Math.Max(colCdhW, ImGui.CalcTextSize(ValueFormatter.FormatPercent(s.CritDirectHitPct, config.PercentDecimalPlaces)).X);
        }

        // Draw header row
        ImGui.InvisibleButton($"##{idPrefix}_hdr_{index}", new Vector2(availWidth, skillBarHeight));
        var hdrMin = ImGui.GetItemRectMin();
        var hdrMax = ImGui.GetItemRectMax();
        drawList.AddText(new Vector2(hdrMin.X + 3, hdrMin.Y), headerColor, "Skill");

        var hdrX = hdrMax.X - 3;
        hdrX -= colHitsW; drawList.AddText(new Vector2(hdrX + colHitsW - ImGui.CalcTextSize("Hits").X, hdrMin.Y), headerColor, "Hits"); hdrX -= colPad;
        hdrX -= colCdhW; drawList.AddText(new Vector2(hdrX + colCdhW - ImGui.CalcTextSize("!!!").X, hdrMin.Y), headerColor, "!!!"); hdrX -= colPad;
        hdrX -= colDhW; drawList.AddText(new Vector2(hdrX + colDhW - ImGui.CalcTextSize("!!").X, hdrMin.Y), headerColor, "!!"); hdrX -= colPad;
        hdrX -= colCritW; drawList.AddText(new Vector2(hdrX + colCritW - ImGui.CalcTextSize("!").X, hdrMin.Y), headerColor, "!"); hdrX -= colPad;
        hdrX -= colPctW; drawList.AddText(new Vector2(hdrX + colPctW - ImGui.CalcTextSize("%").X, hdrMin.Y), headerColor, "%"); hdrX -= colPad;
        hdrX -= colValW; drawList.AddText(new Vector2(hdrX + colValW - ImGui.CalcTextSize("Amount").X, hdrMin.Y), headerColor, "Amount");

        var skillIdx = 0;
        foreach (var skill in topSkills)
        {
            var barFraction = maxSkillVal > 0 ? (float)skill.TotalDamage / maxSkillVal : 0f;

            ImGui.InvisibleButton($"##{idPrefix}_{index}_{skillIdx}", new Vector2(availWidth, skillBarHeight));
            var min = ImGui.GetItemRectMin();
            var max = ImGui.GetItemRectMax();

            drawList.AddRectFilled(min, max, bgColor, skillRounding);
            var barColor = skill.DamageType switch
                {
                    SkillDamageType.Physical => physFillColor,
                    SkillDamageType.Magic => magFillColor,
                    _ => fillColor,
                };
            drawList.AddRectFilled(min, new Vector2(min.X + availWidth * barFraction, max.Y), barColor, skillRounding);
            drawList.AddText(new Vector2(min.X + 3, min.Y), textColor, skill.Name);

            var x = max.X - 3;
            var hitsText = $"x{skill.HitCount}";
            x -= colHitsW; drawList.AddText(new Vector2(x + colHitsW - ImGui.CalcTextSize(hitsText).X, min.Y), textColor, hitsText); x -= colPad;
            var cdhText = ValueFormatter.FormatPercent(skill.CritDirectHitPct, config.PercentDecimalPlaces);
            x -= colCdhW; drawList.AddText(new Vector2(x + colCdhW - ImGui.CalcTextSize(cdhText).X, min.Y), textColor, cdhText); x -= colPad;
            var dhText = ValueFormatter.FormatPercent(skill.DirectHitPct, config.PercentDecimalPlaces);
            x -= colDhW; drawList.AddText(new Vector2(x + colDhW - ImGui.CalcTextSize(dhText).X, min.Y), textColor, dhText); x -= colPad;
            var critText = ValueFormatter.FormatPercent(skill.CritPct, config.PercentDecimalPlaces);
            x -= colCritW; drawList.AddText(new Vector2(x + colCritW - ImGui.CalcTextSize(critText).X, min.Y), textColor, critText); x -= colPad;
            var pctText = ValueFormatter.FormatPercent(skill.DamagePercent, config.PercentDecimalPlaces);
            x -= colPctW; drawList.AddText(new Vector2(x + colPctW - ImGui.CalcTextSize(pctText).X, min.Y), textColor, pctText); x -= colPad;
            var valText = ValueFormatter.Format(skill.TotalDamage, config);
            x -= colValW; drawList.AddText(new Vector2(x + colValW - ImGui.CalcTextSize(valText).X, min.Y), textColor, valText);

            skillIdx++;
        }

        ImGui.GetFont().Scale = prevSkillScale;
        ImGui.PopFont();
    }

    public void CollapseAll()
    {
        expandedIndex = -1;
    }
}
