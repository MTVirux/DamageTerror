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
    private readonly HashSet<string> expandedSkills = new();
    private readonly HashSet<string> hiddenLegendEntries = new(StringComparer.Ordinal);
    private bool wasActivelyUpdating;
    private double scrollXMin = double.NaN;
    private double scrollXMax = double.NaN;

    private static readonly BarColumn[] DamageSection = { BarColumn.Dps, BarColumn.InstantDps, BarColumn.PeakDps, BarColumn.Damage, BarColumn.DamagePercent, BarColumn.MaxHit, BarColumn.MaxHitValue, BarColumn.DamageShield, BarColumn.RaidDps };
    private static readonly BarColumn[] HealingSection = { BarColumn.Hps, BarColumn.InstantHps, BarColumn.Healed, BarColumn.HealPercent, BarColumn.Overheal, BarColumn.OverhealAmount, BarColumn.CritHealPct, BarColumn.MaxHeal, BarColumn.MaxHealValue, BarColumn.HealCount, BarColumn.RaidHps };
    private static readonly BarColumn[] HitStatSection = { BarColumn.Crit, BarColumn.DirectHit, BarColumn.CritDirectHit, BarColumn.CritHitCount, BarColumn.DirectHitCount, BarColumn.CritDirectHitCount, BarColumn.HitRate, BarColumn.Swings, BarColumn.Hits, BarColumn.Misses };
    private static readonly BarColumn[] DefenseSection = { BarColumn.DamageTaken, BarColumn.DamageTakenPercent, BarColumn.BlockPct, BarColumn.ParryPct, BarColumn.HealsTaken };
    private static readonly BarColumn[] OtherSection = { BarColumn.Deaths, BarColumn.Kills, BarColumn.CombatantDuration, BarColumn.PowerHeal };
#if DEBUG
    private static readonly BarColumn[] UnknownSection = { BarColumn.PowerDrain, BarColumn.AbsorbHeal, BarColumn.MaxHealWard };
#endif

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

        var vis = activeTab?.DetailVisibleColumns ?? config.DetailVisibleColumns;
        var lc = config.DetailLabelColor;

        var panelStart = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();
        drawList.ChannelsSplit(2);
        drawList.ChannelsSetCurrent(1);

        ImGui.Indent(config.DetailIndent);

        var prevScale = ImGui.GetFont().Scale;
        ImGui.GetFont().Scale = config.GetFontScale(config.DetailFontSize);
        ImGui.PushFont(ImGui.GetFont());

        var showDetailsTab = activeTab?.DetailShowDetailsTab ?? config.DetailShowDetailsTab;
        var showSkillsTab = activeTab?.DetailShowSkillsTab ?? config.DetailShowSkillsTab;
        var showGraphTab = activeTab?.DetailShowGraphTab ?? config.DetailShowGraphTab;

        if (ImGui.BeginTabBar("##detailTabs", ImGuiTabBarFlags.Reorderable))
        {
            if (showDetailsTab && ImGui.BeginTabItem($"Details##detail"))
            {
                DrawDetailsTab(combatant, index, vis, lc, activeTab);
                ImGui.EndTabItem();
            }

            if (showSkillsTab && ImGui.BeginTabItem($"Skills##detail"))
            {
                DrawSkillsTab(combatant, index, activeTab);
                ImGui.EndTabItem();
            }

            if (showGraphTab && ImGui.BeginTabItem($"Graph##detail"))
            {
                DrawGraphTab(combatant, index, activeTab);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.GetFont().Scale = prevScale;
        ImGui.PopFont();

        ImGui.Unindent(config.DetailIndent);

        var panelEnd = new Vector2(panelStart.X + ImGui.GetContentRegionAvail().X + config.DetailIndent, ImGui.GetCursorScreenPos().Y);
        drawList.ChannelsSetCurrent(0);
        drawList.AddRectFilled(panelStart, panelEnd, ImGui.ColorConvertFloat4ToU32(config.DetailBackgroundColor));
        drawList.ChannelsMerge();

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
        var graphH = config.GraphHeight;
        var thickness = config.GraphLineThickness;

        var prevGraphScale = ImGui.GetFont().Scale;
        ImGui.GetFont().Scale = config.GetFontScale(config.GraphFontSize);
        ImGui.PushFont(ImGui.GetFont());

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

        var maxTime = times[^1];
        if (maxTime <= 0f) maxTime = 1f;
        var maxVal = 0f;
        for (var i = 0; i < samples.Count; i++)
        {
            if (dpsVals != null && dpsVals[i] > maxVal) maxVal = dpsVals[i];
            if (hpsVals != null && hpsVals[i] > maxVal) maxVal = hpsVals[i];
            if (dtpsVals != null && dtpsVals[i] > maxVal) maxVal = dtpsVals[i];
        }

        var yAxisFlags = maxVal > 0f ? ImPlotAxisFlags.AutoFit : ImPlotAxisFlags.None;
        if (!config.GraphShowGrid)
            yAxisFlags |= ImPlotAxisFlags.NoGridLines;
        if (!config.GraphShowYAxisLabels)
            yAxisFlags |= ImPlotAxisFlags.NoTickLabels;

        if (config.GraphShowGrid)
        {
            ImPlot.PushStyleColor(ImPlotCol.AxisGrid, config.GraphGridColor);
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
            ImPlot.SetupAxisLimitsConstraints(ImAxis.X1, 0, double.MaxValue);

            // Custom Y-axis ticks with K/M abbreviations, skipping 0
            if (maxVal > 0f) GraphRenderHelper.SetupAbbreviatedYTicks(maxVal, config.GraphYAxisHeadroom, config.GraphYAxisTickCount);
            else ImPlot.SetupAxisLimits(ImAxis.Y1, 0, 1, ImPlotCond.Always);

            var labelOffset = new Vector2(config.GraphLabelOffsetX, config.GraphLabelOffsetY);

            var dpsHidden = hiddenLegendEntries.Contains("iDPS");
            var hpsHidden = hiddenLegendEntries.Contains("iHPS");
            var dtpsHidden = hiddenLegendEntries.Contains("iDTPS");

            if (dpsVals != null)
            {
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

            // ── Skill use markers (per-metric, using detail config) ──
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

            // Skill marker tooltip — find nearest marker across all metrics on hover
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

            // Detect legend entry clicks to track hidden state
            foreach (var label in new[] { "iDPS", "iHPS", "iDTPS" })
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



    private void DrawSkillsTab(CombatantEntry combatant, int index, MeterTab? activeTab)
    {
        var showBreakdown = activeTab?.DetailShowSkillBreakdown ?? config.DetailShowSkillBreakdown;

        if (showBreakdown && combatant.Skills.Count > 0)
        {
            ImGui.Spacing();
            if (PersistentTreeNode("Damage Skills", index.ToString()))
            {
                DrawSkillTable(combatant.Skills, index, "dmg", config.SkillDamageFillColor, activeTab);
                ImGui.TreePop();
            }
        }

        if (showBreakdown && combatant.HealingSkills.Count > 0)
        {
            ImGui.Spacing();
            if (PersistentTreeNode("Healing Skills", index.ToString()))
            {
                DrawSkillTable(combatant.HealingSkills, index, "heal", config.SkillHealingFillColor, activeTab);
                ImGui.TreePop();
            }
        }

        if (!showBreakdown || (combatant.Skills.Count == 0 && combatant.HealingSkills.Count == 0))
        {
            ImGui.Spacing();
            ImGui.TextDisabled("No skill data available.");
            ImGui.Spacing();
        }
    }

    internal const string SectionDamage = "Damage";
    internal const string SectionHealing = "Healing";
    internal const string SectionHitStats = "Hit Statistics";
    internal const string SectionDefense = "Defense";
    internal const string SectionOther = "Other";
#if DEBUG
    internal const string SectionUnknown = "Unknown";
#endif

    internal static readonly (string Name, BarColumn[] Columns)[] Sections =
    {
        (SectionDamage, DamageSection),
        (SectionHealing, HealingSection),
        (SectionHitStats, HitStatSection),
        (SectionDefense, DefenseSection),
        (SectionOther, OtherSection),
#if DEBUG
        (SectionUnknown, UnknownSection),
#endif
    };

    private void DrawDetailsTab(CombatantEntry combatant, int index, HashSet<BarColumn> vis, Vector4 lc, MeterTab? activeTab)
    {
        ImGui.Spacing();

        if (!ImGui.BeginTabBar($"##detailSections_{index}", ImGuiTabBarFlags.Reorderable))
            return;

        foreach (var (sectionName, defaultOrder) in Sections)
        {
            if (!HasAny(vis, defaultOrder))
                continue;

            var tabLabel = sectionName == SectionHitStats ? "Hit Stats" : sectionName;
            if (!ImGui.BeginTabItem($"{tabLabel}##{index}"))
                continue;

            var order = GetSectionOrder(sectionName, defaultOrder, activeTab);
            DrawOrderedSection(order, combatant, vis, lc);
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }

    private void DrawOrderedSection(List<BarColumn> order, CombatantEntry combatant, HashSet<BarColumn> vis, Vector4 lc)
    {
        var rowCount = 0;
        var first = true;
        foreach (var col in order)
        {
            var data = GetDetailColumnData(col, combatant, vis);
            if (data == null)
                continue;

            var (label, value) = data.Value;

            if (rowCount == 3)
            {
                rowCount = 0;
                first = true;
            }

            if (col == BarColumn.Deaths)
            {
                if (!first) ImGui.SameLine();
                ImGui.TextColored(lc, first ? "Deaths:" : "  Deaths:");
                ImGui.SameLine();
                if (combatant.Deaths > 0)
                    ImGui.TextColored(config.DetailDeathColor, value);
                else
                    ImGui.TextUnformatted("0");
            }
            else if (first)
            {
                ImGui.TextColored(lc, $"{label}:");
                ImGui.SameLine();
                ImGui.TextUnformatted(value);
            }
            else
            {
                ImGui.SameLine();
                ImGui.TextColored(lc, $"  {label}:");
                ImGui.SameLine();
                ImGui.TextUnformatted(value);
            }

            first = false;
            rowCount++;
        }
    }

    private (string label, string value)? GetDetailColumnData(BarColumn col, CombatantEntry c, HashSet<BarColumn> vis)
    {
        if (!vis.Contains(col))
            return null;

        return col switch
        {
            // Damage
            BarColumn.Dps => ("DPS", Fmt(c.EncDps)),
            BarColumn.InstantDps => ("iDPS", Fmt(c.InstantDps)),
            BarColumn.PeakDps => ("Peak", Fmt(c.PeakDps)),
            BarColumn.Damage => ("Total", Fmt(c.Damage)),
            BarColumn.DamagePercent => ("Dmg %", c.DamagePercent),
            BarColumn.MaxHit when !string.IsNullOrEmpty(c.MaxHit) => ("Max Hit", c.MaxHitSkillName),
            BarColumn.MaxHitValue when c.MaxHitDamage > 0 => ("Max Hit Value", Fmt(c.MaxHitDamage)),
            BarColumn.DamageShield => ("Shield", Fmt(c.DamageShield)),
            BarColumn.RaidDps => ("Group DPS", Fmt(c.RaidDps)),

            // Healing
            BarColumn.Hps => ("HPS", Fmt(c.EncHps)),
            BarColumn.InstantHps => ("iHPS", Fmt(c.InstantHps)),
            BarColumn.Healed => ("Total", Fmt(c.Healed)),
            BarColumn.HealPercent => ("Heal %", c.HealedPercent),
            BarColumn.Overheal => ("Overheal", FmtPct(c.OverhealPct)),
            BarColumn.OverhealAmount => ("OH Amt", Fmt(c.OverhealAmount)),
            BarColumn.CritHealPct => ("Crit Heal", FmtPct(c.CritHealPct)),
            BarColumn.MaxHeal when !string.IsNullOrEmpty(c.MaxHeal) => ("Max Heal", c.MaxHealSkillName),
            BarColumn.MaxHealValue when c.MaxHealAmount > 0 => ("Max Heal Value", Fmt(c.MaxHealAmount)),
            BarColumn.HealCount => ("Heals", c.HealCount.ToString()),
            BarColumn.RaidHps => ("Group HPS", Fmt(c.RaidHps)),

            // Hit Stats
            BarColumn.Crit => ("Crit", FmtPct(c.CritPct)),
            BarColumn.DirectHit => ("DH", FmtPct(c.DirectHitPct)),
            BarColumn.CritDirectHit => ("CDH", FmtPct(c.CritDirectHitPct)),
            BarColumn.CritHitCount => ("Crit#", c.CritHitCount.ToString()),
            BarColumn.DirectHitCount => ("DH#", c.DirectHitCount.ToString()),
            BarColumn.CritDirectHitCount => ("CDH#", c.CritDirectHitCount.ToString()),
            BarColumn.HitRate => ("Hit Rate", FmtPct(c.HitRate)),
            BarColumn.Swings => ("Swings", c.Swings.ToString()),
            BarColumn.Hits => ("Hits", c.Hits.ToString()),
            BarColumn.Misses => ("Misses", c.Misses.ToString()),

            // Defense
            BarColumn.DamageTaken => ("Taken", Fmt(c.DamageTaken)),
            BarColumn.DamageTakenPercent => ("Taken %", c.DamageTakenPercent),
            BarColumn.BlockPct => ("Block", FmtPct(c.BlockPct)),
            BarColumn.ParryPct => ("Parry", FmtPct(c.ParryPct)),
            BarColumn.HealsTaken => ("Heals Taken", Fmt(c.HealsTaken)),

            // Other
            BarColumn.Deaths => ("Deaths", c.Deaths.ToString()),
            BarColumn.Kills => ("Kills", c.Kills.ToString()),
            BarColumn.CombatantDuration => ("Duration", c.CombatantDuration),
            BarColumn.PowerHeal => ("MP Recovery", Fmt(c.PowerHeal)),

            // Debug
            BarColumn.PowerDrain => ("MP Drain", Fmt(c.PowerDrain)),
            BarColumn.AbsorbHeal => ("Absorb", Fmt(c.AbsorbHeal)),
            BarColumn.MaxHealWard when !string.IsNullOrEmpty(c.MaxHealWardName) => ("Max Ward", $"{c.MaxHealWardName} ({Fmt(c.MaxHealWardAmount)})"),

            _ => null,
        };
    }

    private static List<BarColumn> GetSectionOrder(string sectionName, BarColumn[] defaultOrder, MeterTab? activeTab)
    {
        if (activeTab?.DetailSectionOrder != null
            && activeTab.DetailSectionOrder.TryGetValue(sectionName, out var order)
            && order.Count > 0)
        {
            var valid = new HashSet<BarColumn>(defaultOrder);
            var result = new List<BarColumn>();
            foreach (var col in order)
            {
                if (valid.Contains(col))
                    result.Add(col);
            }
            foreach (var col in defaultOrder)
            {
                if (!result.Contains(col))
                    result.Add(col);
            }
            return result;
        }
        return new List<BarColumn>(defaultOrder);
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

    private void DrawSkillTable(List<SkillEntry> skills, int index, string idPrefix, Vector4 fillColorVec, MeterTab? activeTab)
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

        var maxCount = activeTab?.MaxSkillBreakdownCount ?? config.MaxSkillBreakdownCount;
        var topSkills = maxCount > 0 ? skills.Take(maxCount).ToList() : skills;
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

        var textHeight = ImGui.CalcTextSize("X").Y;
        var textYOff = (skillBarHeight - textHeight) * 0.5f;

        ImGui.InvisibleButton($"##{idPrefix}_hdr_{index}", new Vector2(availWidth, skillBarHeight));
        var hdrMin = ImGui.GetItemRectMin();
        var hdrMax = ImGui.GetItemRectMax();
        drawList.AddText(new Vector2(hdrMin.X + 3, hdrMin.Y + textYOff), headerColor, "Skill");

        var hdrX = hdrMax.X - 3;
        hdrX -= colHitsW; drawList.AddText(new Vector2(hdrX + (colHitsW - ImGui.CalcTextSize("Hits").X) * 0.5f, hdrMin.Y + textYOff), headerColor, "Hits"); hdrX -= colPad;
        hdrX -= colCdhW; drawList.AddText(new Vector2(hdrX + (colCdhW - ImGui.CalcTextSize("!!!").X) * 0.5f, hdrMin.Y + textYOff), headerColor, "!!!"); hdrX -= colPad;
        hdrX -= colDhW; drawList.AddText(new Vector2(hdrX + (colDhW - ImGui.CalcTextSize("!!").X) * 0.5f, hdrMin.Y + textYOff), headerColor, "!!"); hdrX -= colPad;
        hdrX -= colCritW; drawList.AddText(new Vector2(hdrX + (colCritW - ImGui.CalcTextSize("!").X) * 0.5f, hdrMin.Y + textYOff), headerColor, "!"); hdrX -= colPad;
        hdrX -= colPctW; drawList.AddText(new Vector2(hdrX + (colPctW - ImGui.CalcTextSize("%").X) * 0.5f, hdrMin.Y + textYOff), headerColor, "%"); hdrX -= colPad;
        hdrX -= colValW; drawList.AddText(new Vector2(hdrX + (colValW - ImGui.CalcTextSize("Amount").X) * 0.5f, hdrMin.Y + textYOff), headerColor, "Amount");

        var skillIdx = 0;
        foreach (var skill in topSkills)
        {
            var barFraction = maxSkillVal > 0 ? (float)skill.TotalDamage / maxSkillVal : 0f;
            var hasSubEntries = skill.SubEntries != null && skill.SubEntries.Count > 0;
            var skillKey = $"{idPrefix}_{index}_{skill.Name}";
            var isExpanded = hasSubEntries && expandedSkills.Contains(skillKey);

            ImGui.InvisibleButton($"##{idPrefix}_{index}_{skillIdx}", new Vector2(availWidth, skillBarHeight));
            var min = ImGui.GetItemRectMin();
            var max = ImGui.GetItemRectMax();

            // Toggle expansion on click if skill has sub-entries
            if (hasSubEntries && ImGui.IsItemClicked())
            {
                if (isExpanded)
                    expandedSkills.Remove(skillKey);
                else
                    expandedSkills.Add(skillKey);
                isExpanded = !isExpanded;
            }

            drawList.AddRectFilled(min, max, bgColor, skillRounding);
            var barColor = skill.DamageType switch
                {
                    SkillDamageType.Physical => physFillColor,
                    SkillDamageType.Magic => magFillColor,
                    _ => fillColor,
                };
            drawList.AddRectFilled(min, new Vector2(min.X + availWidth * barFraction, max.Y), barColor, skillRounding);

            // Draw expand indicator for skills with sub-entries
            var nameX = min.X + 3;
            if (hasSubEntries)
            {
                var arrow = isExpanded ? "v " : "> ";
                drawList.AddText(new Vector2(nameX, min.Y + textYOff), textColor, arrow);
                nameX += ImGui.CalcTextSize(arrow).X;
            }
            drawList.AddText(new Vector2(nameX, min.Y + textYOff), textColor, skill.Name);

            var x = max.X - 3;
            var hitsText = $"x{skill.HitCount}";
            x -= colHitsW; drawList.AddText(new Vector2(x + (colHitsW - ImGui.CalcTextSize(hitsText).X) * 0.5f, min.Y + textYOff), textColor, hitsText); x -= colPad;
            var cdhText = ValueFormatter.FormatPercent(skill.CritDirectHitPct, config.PercentDecimalPlaces);
            x -= colCdhW; drawList.AddText(new Vector2(x + (colCdhW - ImGui.CalcTextSize(cdhText).X) * 0.5f, min.Y + textYOff), textColor, cdhText); x -= colPad;
            var dhText = ValueFormatter.FormatPercent(skill.DirectHitPct, config.PercentDecimalPlaces);
            x -= colDhW; drawList.AddText(new Vector2(x + (colDhW - ImGui.CalcTextSize(dhText).X) * 0.5f, min.Y + textYOff), textColor, dhText); x -= colPad;
            var critText = ValueFormatter.FormatPercent(skill.CritPct, config.PercentDecimalPlaces);
            x -= colCritW; drawList.AddText(new Vector2(x + (colCritW - ImGui.CalcTextSize(critText).X) * 0.5f, min.Y + textYOff), textColor, critText); x -= colPad;
            var pctText = ValueFormatter.FormatPercent(skill.DamagePercent, config.PercentDecimalPlaces);
            x -= colPctW; drawList.AddText(new Vector2(x + (colPctW - ImGui.CalcTextSize(pctText).X) * 0.5f, min.Y + textYOff), textColor, pctText); x -= colPad;
            var valText = ValueFormatter.Format(skill.TotalDamage, config);
            x -= colValW; drawList.AddText(new Vector2(x + (colValW - ImGui.CalcTextSize(valText).X) * 0.5f, min.Y + textYOff), textColor, valText);

            // Draw sub-entries when expanded
            if (isExpanded && skill.SubEntries != null)
            {
                var subIndent = 16f;
                var subAvailWidth = availWidth - subIndent;
                var subAlpha = 0.7f;

                foreach (var sub in skill.SubEntries)
                {
                    var subFraction = skill.TotalDamage > 0 ? (float)sub.TotalDamage / maxSkillVal : 0f;

                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + subIndent);
                    ImGui.InvisibleButton($"##{idPrefix}_{index}_{skillIdx}_sub", new Vector2(subAvailWidth, skillBarHeight));
                    var sMin = ImGui.GetItemRectMin();
                    var sMax = ImGui.GetItemRectMax();

                    drawList.AddRectFilled(sMin, sMax, bgColor, skillRounding);
                    var subBarColor = sub.DamageType switch
                    {
                        SkillDamageType.Physical => physFillColor,
                        SkillDamageType.Magic => magFillColor,
                        _ => fillColor,
                    };
                    // Dim the sub-entry bar color
                    var subBarColorVec = ImGui.ColorConvertU32ToFloat4(subBarColor);
                    subBarColorVec.W *= subAlpha;
                    var subBarColorU32 = ImGui.ColorConvertFloat4ToU32(subBarColorVec);
                    drawList.AddRectFilled(sMin, new Vector2(sMin.X + subAvailWidth * subFraction, sMax.Y), subBarColorU32, skillRounding);
                    drawList.AddText(new Vector2(sMin.X + 3, sMin.Y + textYOff), textColor, sub.Name);

                    var sx = sMax.X - 3;
                    var sHitsText = $"x{sub.HitCount}";
                    sx -= colHitsW; drawList.AddText(new Vector2(sx + (colHitsW - ImGui.CalcTextSize(sHitsText).X) * 0.5f, sMin.Y + textYOff), textColor, sHitsText); sx -= colPad;
                    var sCdhText = ValueFormatter.FormatPercent(sub.CritDirectHitPct, config.PercentDecimalPlaces);
                    sx -= colCdhW; drawList.AddText(new Vector2(sx + (colCdhW - ImGui.CalcTextSize(sCdhText).X) * 0.5f, sMin.Y + textYOff), textColor, sCdhText); sx -= colPad;
                    var sDhText = ValueFormatter.FormatPercent(sub.DirectHitPct, config.PercentDecimalPlaces);
                    sx -= colDhW; drawList.AddText(new Vector2(sx + (colDhW - ImGui.CalcTextSize(sDhText).X) * 0.5f, sMin.Y + textYOff), textColor, sDhText); sx -= colPad;
                    var sCritText = ValueFormatter.FormatPercent(sub.CritPct, config.PercentDecimalPlaces);
                    sx -= colCritW; drawList.AddText(new Vector2(sx + (colCritW - ImGui.CalcTextSize(sCritText).X) * 0.5f, sMin.Y + textYOff), textColor, sCritText); sx -= colPad;
                    var sPctText = ValueFormatter.FormatPercent(sub.DamagePercent, config.PercentDecimalPlaces);
                    sx -= colPctW; drawList.AddText(new Vector2(sx + (colPctW - ImGui.CalcTextSize(sPctText).X) * 0.5f, sMin.Y + textYOff), textColor, sPctText); sx -= colPad;
                    var sValText = ValueFormatter.Format(sub.TotalDamage, config);
                    sx -= colValW; drawList.AddText(new Vector2(sx + (colValW - ImGui.CalcTextSize(sValText).X) * 0.5f, sMin.Y + textYOff), textColor, sValText);
                }
            }

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
