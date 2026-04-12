using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImPlot;
using DamageTerror.Gui;
using DamageTerror.Gui.ConfigWindow;
using DamageTerror.Services;
using ImGui = Dalamud.Bindings.ImGui.ImGui;
using ImPlot = Dalamud.Bindings.ImPlot.ImPlot;

namespace DamageTerror.Gui.MainWindow;

public class CombatantDetailPanel
{
    private readonly Configuration config;
    private readonly GraphDataTracker graphTracker;
    private readonly SkillTracker skillTracker;
    private readonly StatusTracker statusTracker;
    private string? expandedName;
    private readonly HashSet<string> expandedSkills = new();
    private readonly HashSet<string> hiddenLegendEntries = new(StringComparer.Ordinal);
    private readonly Dictionary<uint, string> itemNameCache = new();
    private bool wasActivelyUpdating;
    private double scrollXMin = double.NaN;
    private double scrollXMax = double.NaN;

    internal static readonly (string Name, BarColumn[] Columns)[] Sections = MetricPicker.BarColumnCategories;

    private EncounterSnapshot? currentSnapshot;
    private bool isLive;

    public CombatantDetailPanel(Configuration config, GraphDataTracker graphTracker, SkillTracker skillTracker, StatusTracker statusTracker)
    {
        this.config = config;
        this.graphTracker = graphTracker;
        this.skillTracker = skillTracker;
        this.statusTracker = statusTracker;
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

    public void Toggle(string name)
    {
        expandedName = expandedName == name ? null : name;
    }

    public bool IsExpanded(string name) => expandedName == name;

    public void Render(RenderContext ctx, CombatantEntry combatant)
    {
        Render(combatant, ctx.Encounter, ctx.IsLive, ctx.ActiveTab);
    }

    public void Render(CombatantEntry combatant, EncounterSnapshot? snapshot, bool isLive, MeterTab? activeTab = null)
    {
        if (expandedName != combatant.Name)
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

        using var detailFont = FontScope.Push(config.GetFontScale(config.DetailFontSize));

        var showDetailsTab = activeTab?.DetailShowDetailsTab ?? config.DetailShowDetailsTab;
        var showSkillsTab = activeTab?.DetailShowSkillsTab ?? config.DetailShowSkillsTab;
        var showGraphTab = activeTab?.DetailShowGraphTab ?? config.DetailShowGraphTab;
        var showBuffsTab = activeTab?.DetailShowBuffsTab ?? config.DetailShowBuffsTab;
        var showItemTab = activeTab?.DetailShowItemTab ?? config.DetailShowItemTab;

        if (ImGui.BeginTabBar("##detailTabs", ImGuiTabBarFlags.Reorderable))
        {
            if (showDetailsTab && ImGui.BeginTabItem($"Details##detail"))
            {
                DrawDetailsTab(combatant, combatant.Name, vis, lc, activeTab);
                ImGui.EndTabItem();
            }

            if (showSkillsTab && ImGui.BeginTabItem($"Skills##detail"))
            {
                DrawSkillsTab(combatant, combatant.Name, activeTab);
                ImGui.EndTabItem();
            }

            if (showGraphTab && ImGui.BeginTabItem($"Graph##detail"))
            {
                DrawGraphTab(combatant, combatant.Name, activeTab);
                ImGui.EndTabItem();
            }

            if (showBuffsTab && ImGui.BeginTabItem($"Buffs/Debuffs##detail"))
            {
                DrawBuffsTab(combatant, combatant.Name);
                ImGui.EndTabItem();
            }

            if (showItemTab && ImGui.BeginTabItem($"Items##detail"))
            {
                DrawItemTab(combatant, combatant.Name);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        detailFont.Dispose();

        ImGui.Unindent(config.DetailIndent);

        var panelEnd = new Vector2(panelStart.X + ImGui.GetContentRegionAvail().X + config.DetailIndent, ImGui.GetCursorScreenPos().Y);
        drawList.ChannelsSetCurrent(0);
        drawList.AddRectFilled(panelStart, panelEnd, ImGui.ColorConvertFloat4ToU32(config.DetailBackgroundColor));
        drawList.ChannelsMerge();

        ImGui.Spacing();
    }

    private void DrawGraphTab(CombatantEntry combatant, string index, MeterTab? activeTab)
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

        var gs = GraphSettings.FromDetail(config);
        GraphRenderHelper.PushGraphStyles(in gs);
        var (plotFlags, xAxisFlags, yAxisFlags) = GraphRenderHelper.ComputePlotFlags(in gs, maxVal);

        if (ImPlot.BeginPlot($"##DetailGraph_{index}", new Vector2(regionW, graphH), plotFlags))
        {
            GraphRenderHelper.SetupGraphAxes(in gs, plotFlags, xAxisFlags, yAxisFlags, maxTime, maxVal,
                isLive, currentSnapshot?.Encounter.IsActive == true, ref wasActivelyUpdating, ref scrollXMin, ref scrollXMax);

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

            GraphRenderHelper.DrawMousePositionText(gs.MouseTextOpacity);

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

        GraphRenderHelper.PopGraphStyles(gs.ShowGrid);

        ImGui.Spacing();
    }



    private void DrawBuffsTab(CombatantEntry combatant, string index)
    {
        // Resolve status data: use live tracker if available, else fall back to snapshot
        List<StatusApplication> received;
        List<StatusApplication> applied;

        if (isLive)
        {
            received = statusTracker.GetStatusesReceived(combatant.Name);
            applied = statusTracker.GetStatusHistory(combatant.Name);

            // Fall back to stored data when live tracker is empty
            if (received.Count == 0
                && currentSnapshot?.StatusesReceived != null
                && currentSnapshot.StatusesReceived.TryGetValue(combatant.Name, out var fallbackR))
                received = fallbackR;

            if (applied.Count == 0
                && currentSnapshot?.StatusHistory != null
                && currentSnapshot.StatusHistory.TryGetValue(combatant.Name, out var fallbackA))
                applied = fallbackA;
        }
        else
        {
            received = currentSnapshot?.StatusesReceived != null
                && currentSnapshot.StatusesReceived.TryGetValue(combatant.Name, out var savedR)
                ? savedR : [];
            applied = currentSnapshot?.StatusHistory != null
                && currentSnapshot.StatusHistory.TryGetValue(combatant.Name, out var savedA)
                ? savedA : [];
        }

        var snapshotDuration = DurationHelper.ParseDuration(currentSnapshot?.Encounter?.Duration, 60f);
        var encounterDuration = isLive ? Math.Max(snapshotDuration, statusTracker.ElapsedSeconds) : snapshotDuration;
        var currentTime = isLive ? statusTracker.ElapsedSeconds : encounterDuration;
        var hasReceived = received.Count > 0;
        var hasApplied = applied.Count > 0;

        if (!hasReceived && !hasApplied)
        {
            ImGui.Spacing();
            ImGui.TextDisabled("No buff/debuff data available.");
            ImGui.Spacing();
            return;
        }

        if (ImGui.BeginTabBar($"##buffDebuffTabs_{index}"))
        {
            if (hasReceived && ImGui.BeginTabItem($"Received##{index}"))
            {
                DrawStatusTable(AggregateStatuses(received, currentTime), encounterDuration, index, "recv", config.BuffFillColor);
                ImGui.EndTabItem();
            }

            if (hasApplied && ImGui.BeginTabItem($"Applied##{index}"))
            {
                DrawStatusTable(AggregateStatuses(applied, currentTime), encounterDuration, index, "appl", config.DebuffFillColor);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawItemTab(CombatantEntry combatant, string index)
    {
        List<SkillUseEvent> items;

        if (isLive)
        {
            items = skillTracker.GetItemEvents(combatant.Name);

            if (items.Count == 0
                && currentSnapshot?.ItemEvents != null
                && currentSnapshot.ItemEvents.TryGetValue(combatant.Name, out var fallback))
                items = fallback;
        }
        else
        {
            items = currentSnapshot?.ItemEvents != null
                && currentSnapshot.ItemEvents.TryGetValue(combatant.Name, out var saved)
                ? saved : [];
        }

        if (items.Count == 0)
        {
            ImGui.Spacing();
            ImGui.TextDisabled("No item usage recorded.");
            ImGui.Spacing();
            return;
        }

        var sorted = items.OrderBy(e => e.TimeSec).ToList();

        if (ImGui.BeginTable($"##itemTable_{index}", 2,
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Item", ImGuiTableColumnFlags.None, 0.65f);
            ImGui.TableSetupColumn("Timestamp", ImGuiTableColumnFlags.None, 0.35f);
            ImGui.TableHeadersRow();

            foreach (var evt in sorted)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                var displayName = ResolveItemName(evt.SkillName);
                ImGui.TextUnformatted(displayName);

                ImGui.TableNextColumn();
                var min = (int)(evt.TimeSec / 60);
                var sec = (int)(evt.TimeSec % 60);
                ImGui.TextUnformatted($"{min}:{sec:00}");
            }

            ImGui.EndTable();
        }
    }

    private string ResolveItemName(string skillName)
    {
        if (!skillName.StartsWith("item_", StringComparison.OrdinalIgnoreCase))
            return skillName;

        var idStr = skillName[5..];
        if (!uint.TryParse(idStr, System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out var itemId))
            return idStr;

        if (itemNameCache.TryGetValue(itemId, out var cached))
            return cached;

        // Strip HQ (+1,000,000) and Collectible (+500,000) flags from item IDs.
        var baseId = itemId;
        bool isHq = false;
        if (baseId >= 1_000_000)
        {
            baseId -= 1_000_000;
            isHq = true;
        }
        else if (baseId >= 500_000)
        {
            baseId -= 500_000;
        }

        string resolved = idStr;
        try
        {
            var sheet = ServiceManager.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Item>();
            if (sheet != null)
            {
                var row = sheet.GetRowOrDefault(baseId);
                if (row.HasValue)
                {
                    var name = row.Value.Name.ToString();
                    if (!string.IsNullOrEmpty(name))
                        resolved = isHq ? $"{name} (HQ)" : name;
                }
            }
        }
        catch { }

        itemNameCache[itemId] = resolved;
        return resolved;
    }

    private sealed class AggregatedStatus
    {
        public string StatusName = string.Empty;
        public uint StatusId;
        public int ApplicationCount;
        public float TotalUptime;
        public bool IsPermanent;
        public bool IsDoT;
        public bool IsHoT;
        public bool IsBuff;
    }

    private static List<AggregatedStatus> AggregateStatuses(List<StatusApplication> statuses, float currentTime)
    {
        var dict = new Dictionary<uint, AggregatedStatus>();
        foreach (var s in statuses)
        {
            if (!dict.TryGetValue(s.StatusId, out var agg))
            {
                agg = new AggregatedStatus
                {
                    StatusName = s.StatusName,
                    StatusId = s.StatusId,
                    IsPermanent = s.IsPermanent,
                    IsDoT = s.IsDoT,
                    IsHoT = s.IsHoT,
                    IsBuff = s.IsBuff,
                };
                dict[s.StatusId] = agg;
            }

            agg.ApplicationCount++;
            var fallbackEnd = s.IsPermanent ? currentTime : Math.Min(currentTime, s.AppliedAtSec + s.Duration);
            var end = s.RemovedAtSec ?? fallbackEnd;
            var uptime = Math.Max(0f, end - s.AppliedAtSec);
            agg.TotalUptime += uptime;
        }

        var result = dict.Values.ToList();
        result.Sort((a, b) => b.TotalUptime.CompareTo(a.TotalUptime));
        return result;
    }

    private void DrawStatusTable(List<AggregatedStatus> statuses, float encounterDuration, string index, string idPrefix, Vector4 defaultFillColor)
    {
        var availWidth = ImGui.GetContentRegionAvail().X;
        var rowHeight = config.BuffRowHeight;
        var drawList = ImGui.GetWindowDrawList();
        var bgColor = ImGui.ColorConvertFloat4ToU32(config.BuffRowBackgroundColor);
        var textColor = ImGui.ColorConvertFloat4ToU32(config.BuffTextColor);
        var headerColor = ImGui.ColorConvertFloat4ToU32(config.BuffHeaderTextColor);
        var rounding = config.BuffBarRounding;
        var colPad = config.BuffColumnPadding;

        using var buffFont = FontScope.Push(config.GetFontScale(config.BuffFontSize));

        var textHeight = ImGui.CalcTextSize("X").Y;
        var textYOff = (rowHeight - textHeight) * 0.5f;

        // Measure column widths
        float colCountW = ImGui.CalcTextSize("Count").X;
        float colUptimeW = ImGui.CalcTextSize("Uptime").X;
        float colPctW = ImGui.CalcTextSize("Uptime%").X;
        float colAvgW = ImGui.CalcTextSize("Avg Dur").X;

        foreach (var s in statuses)
        {
            colCountW = Math.Max(colCountW, ImGui.CalcTextSize($"x{s.ApplicationCount}").X);
            colUptimeW = Math.Max(colUptimeW, ImGui.CalcTextSize($"{s.TotalUptime:F1}s").X);
            var pct = encounterDuration > 0 ? Math.Min(100.0, s.TotalUptime / encounterDuration * 100.0) : 0.0;
            colPctW = Math.Max(colPctW, ImGui.CalcTextSize($"{pct:F1}%").X);
            var avgText0 = s.IsPermanent ? "\u221E" : $"{(s.ApplicationCount > 0 ? s.TotalUptime / s.ApplicationCount : 0f):F1}s";
            colAvgW = Math.Max(colAvgW, ImGui.CalcTextSize(avgText0).X);
        }

        // Header row
        ImGui.InvisibleButton($"##{idPrefix}_hdr_{index}", new Vector2(availWidth, rowHeight));
        var hdrMin = ImGui.GetItemRectMin();
        var hdrMax = ImGui.GetItemRectMax();
        drawList.AddText(new Vector2(hdrMin.X + 3, hdrMin.Y + textYOff), headerColor, "Status");

        var hdrX = hdrMax.X - 3;
        hdrX -= colAvgW; drawList.AddText(new Vector2(hdrX + (colAvgW - ImGui.CalcTextSize("Avg Dur").X) * 0.5f, hdrMin.Y + textYOff), headerColor, "Avg Dur"); hdrX -= colPad;
        hdrX -= colPctW; drawList.AddText(new Vector2(hdrX + (colPctW - ImGui.CalcTextSize("Uptime%").X) * 0.5f, hdrMin.Y + textYOff), headerColor, "Uptime%"); hdrX -= colPad;
        hdrX -= colUptimeW; drawList.AddText(new Vector2(hdrX + (colUptimeW - ImGui.CalcTextSize("Uptime").X) * 0.5f, hdrMin.Y + textYOff), headerColor, "Uptime"); hdrX -= colPad;
        hdrX -= colCountW; drawList.AddText(new Vector2(hdrX + (colCountW - ImGui.CalcTextSize("Count").X) * 0.5f, hdrMin.Y + textYOff), headerColor, "Count");

        // Find max uptime for bar fraction
        var maxUptime = statuses.Count > 0 ? statuses[0].TotalUptime : 1f;
        if (maxUptime <= 0f) maxUptime = 1f;

        var rowIdx = 0;
        foreach (var status in statuses)
        {
            var barFraction = status.TotalUptime / maxUptime;
            var fillColorVec = status.IsDoT ? config.SkillPhysicalFillColor
                : status.IsHoT ? config.SkillHealingFillColor
                : status.IsBuff ? config.BuffFillColor
                : config.DebuffFillColor;
            var fillColor = ImGui.ColorConvertFloat4ToU32(fillColorVec);

            ImGui.InvisibleButton($"##{idPrefix}_{index}_{rowIdx}", new Vector2(availWidth, rowHeight));
            var min = ImGui.GetItemRectMin();
            var max = ImGui.GetItemRectMax();

            drawList.AddRectFilled(min, max, bgColor, rounding);
            drawList.AddRectFilled(min, new Vector2(min.X + availWidth * barFraction, max.Y), fillColor, rounding);

            // Status name with DoT/HoT indicator
            var nameText = status.StatusName;
            if (status.IsDoT) nameText += " [DoT]";
            else if (status.IsHoT) nameText += " [HoT]";
            drawList.AddText(new Vector2(min.X + 3, min.Y + textYOff), textColor, nameText);

            // Right-aligned columns
            var x = max.X - 3;

            var avgText = status.IsPermanent ? "\u221E" : $"{(status.ApplicationCount > 0 ? status.TotalUptime / status.ApplicationCount : 0f):F1}s";
            x -= colAvgW; drawList.AddText(new Vector2(x + (colAvgW - ImGui.CalcTextSize(avgText).X) * 0.5f, min.Y + textYOff), textColor, avgText); x -= colPad;

            var pct = encounterDuration > 0 ? Math.Min(100.0, status.TotalUptime / encounterDuration * 100.0) : 0.0;
            var pctText = $"{pct:F1}%";
            x -= colPctW; drawList.AddText(new Vector2(x + (colPctW - ImGui.CalcTextSize(pctText).X) * 0.5f, min.Y + textYOff), textColor, pctText); x -= colPad;

            var uptimeText = $"{status.TotalUptime:F1}s";
            x -= colUptimeW; drawList.AddText(new Vector2(x + (colUptimeW - ImGui.CalcTextSize(uptimeText).X) * 0.5f, min.Y + textYOff), textColor, uptimeText); x -= colPad;

            var countText = $"x{status.ApplicationCount}";
            x -= colCountW; drawList.AddText(new Vector2(x + (colCountW - ImGui.CalcTextSize(countText).X) * 0.5f, min.Y + textYOff), textColor, countText);

            rowIdx++;
        }
    }

    private void DrawSkillsTab(CombatantEntry combatant, string index, MeterTab? activeTab)
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

    private void DrawDetailsTab(CombatantEntry combatant, string index, HashSet<BarColumn> vis, Vector4 lc, MeterTab? activeTab)
    {
        ImGui.Spacing();

        if (!ImGui.BeginTabBar("##detailSections", ImGuiTabBarFlags.Reorderable))
            return;

        foreach (var (sectionName, defaultOrder) in Sections)
        {
            if (sectionName == "Group")
                continue;

            if (!HasAny(vis, defaultOrder))
                continue;

            if (!ImGui.BeginTabItem($"{sectionName}##detailSection"))
                continue;

            var order = GetSectionOrder(sectionName, defaultOrder, activeTab);
            DrawOrderedSection(order, combatant, vis, lc, activeTab);
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }

    private void DrawOrderedSection(List<BarColumn> order, CombatantEntry combatant, HashSet<BarColumn> vis, Vector4 lc, MeterTab? activeTab)
    {
        var newLineSet = activeTab?.DetailNewLineColumns ?? config.DetailNewLineColumns;
        var first = true;
        var regionMin = ImGui.GetCursorScreenPos().X;
        var availWidth = ImGui.GetContentRegionAvail().X;
        foreach (var col in order)
        {
            var data = GetDetailColumnData(col, combatant, vis, activeTab);
            if (data == null)
                continue;

            var (label, value) = data.Value;

            if (newLineSet.Contains(col) && !first)
                first = true;

            var colColor = activeTab?.GetColumnValueColor(col);

            var displayLabel = col == BarColumn.Deaths
                ? (activeTab != null ? activeTab.GetDetailColumnLabel(BarColumn.Deaths) : "Deaths")
                : label;

            if (!first)
            {
                var spacing = ImGui.GetStyle().ItemSpacing.X;
                var prefix = $"  {displayLabel}:";
                var prevEndX = ImGui.GetItemRectMax().X - regionMin;
                var itemWidth = spacing + ImGui.CalcTextSize(prefix).X + spacing + ImGui.CalcTextSize(value).X;

                if (prevEndX + itemWidth > availWidth)
                {
                    ImGui.TextColored(lc, $"{displayLabel}:");
                }
                else
                {
                    ImGui.SameLine();
                    ImGui.TextColored(lc, prefix);
                }
            }
            else
            {
                ImGui.TextColored(lc, $"{displayLabel}:");
                first = false;
            }

            ImGui.SameLine();
            if (colColor.HasValue)
                ImGui.TextColored(colColor.Value, value);
            else
                ImGui.TextUnformatted(value);
        }
    }

    private (string label, string value)? GetDetailColumnData(BarColumn col, CombatantEntry c, HashSet<BarColumn> vis, MeterTab? activeTab)
    {
        if (!vis.Contains(col))
            return null;

        string Label(BarColumn bc) => activeTab != null ? activeTab.GetDetailColumnLabel(bc)
            : Configuration.DefaultDetailColumnLabels.GetValueOrDefault(bc, bc.ToString());

        return col switch
        {
            // Damage
            BarColumn.Dps => (Label(col), Fmt(c.EncDps)),
            BarColumn.InstantDps => (Label(col), Fmt(c.InstantDps)),
            BarColumn.PeakDps => (Label(col), Fmt(c.PeakDps)),
            BarColumn.Damage => (Label(col), Fmt(c.Damage)),
            BarColumn.DamagePercent => (Label(col), c.DamagePercent),
            BarColumn.MaxHit when !string.IsNullOrEmpty(c.MaxHit) => (Label(col), c.MaxHitSkillName),
            BarColumn.MaxHitValue when c.MaxHitDamage > 0 => (Label(col), Fmt(c.MaxHitDamage)),
            BarColumn.DamageShield => (Label(col), Fmt(c.DamageShield)),
            BarColumn.EncDps => (Label(col), Fmt(c.RaidDps)),

            // Healing
            BarColumn.Hps => (Label(col), Fmt(c.EncHps)),
            BarColumn.InstantHps => (Label(col), Fmt(c.InstantHps)),
            BarColumn.Healed => (Label(col), Fmt(c.Healed)),
            BarColumn.HealPercent => (Label(col), c.HealedPercent),
            BarColumn.Overheal => (Label(col), FmtPct(c.OverhealPct)),
            BarColumn.OverhealAmount => (Label(col), Fmt(c.OverhealAmount)),
            BarColumn.CritHealPct => (Label(col), FmtPct(c.CritHealPct)),
            BarColumn.MaxHeal when !string.IsNullOrEmpty(c.MaxHeal) => (Label(col), c.MaxHealSkillName),
            BarColumn.MaxHealValue when c.MaxHealAmount > 0 => (Label(col), Fmt(c.MaxHealAmount)),
            BarColumn.HealCount => (Label(col), c.HealCount.ToString()),
            BarColumn.EncHps => (Label(col), Fmt(c.RaidHps)),

            // Hit Stats
            BarColumn.Crit => (Label(col), FmtPct(c.CritPct)),
            BarColumn.DirectHit => (Label(col), FmtPct(c.DirectHitPct)),
            BarColumn.CritDirectHit => (Label(col), FmtPct(c.CritDirectHitPct)),
            BarColumn.CritHitCount => (Label(col), c.CritHitCount.ToString()),
            BarColumn.DirectHitCount => (Label(col), c.DirectHitCount.ToString()),
            BarColumn.CritDirectHitCount => (Label(col), c.CritDirectHitCount.ToString()),
            BarColumn.HitRate => (Label(col), FmtPct(c.HitRate)),
            BarColumn.Swings => (Label(col), c.Swings.ToString()),
            BarColumn.Hits => (Label(col), c.Hits.ToString()),
            BarColumn.Misses => (Label(col), c.Misses.ToString()),
            BarColumn.Positionals => (Label(col), c.Positionals.ToString()),
            BarColumn.PositionalHits => (Label(col), c.PositionalHits.ToString()),
            BarColumn.PositionalMisses => (Label(col), c.PositionalMisses.ToString()),
            BarColumn.PositionalPct => (Label(col), FmtPct(c.PositionalPct)),

            // Defense
            BarColumn.DamageTaken => (Label(col), Fmt(c.DamageTaken)),
            BarColumn.DamageTakenPercent => (Label(col), c.DamageTakenPercent),
            BarColumn.BlockPct => (Label(col), FmtPct(c.BlockPct)),
            BarColumn.ParryPct => (Label(col), FmtPct(c.ParryPct)),
            BarColumn.HealsTaken => (Label(col), Fmt(c.HealsTaken)),

            // Other
            BarColumn.Deaths => (Label(col), c.Deaths.ToString()),
            BarColumn.Kills => (Label(col), c.Kills.ToString()),
            BarColumn.CombatantDuration => (Label(col), c.CombatantDuration),
            BarColumn.PowerHeal => (Label(col), Fmt(c.PowerHeal)),

            // Debug
            BarColumn.PowerDrain => (Label(col), Fmt(c.PowerDrain)),
            BarColumn.AbsorbHeal => (Label(col), Fmt(c.AbsorbHeal)),
            BarColumn.MaxHealWard when !string.IsNullOrEmpty(c.MaxHealWardName) => (Label(col), $"{c.MaxHealWardName} ({Fmt(c.MaxHealWardAmount)})"),

            // High-end Raiding
            BarColumn.LegsSweeped => (Label(col), c.Stuns.ToString()),
            BarColumn.SkillIssue => (Label(col), c.SkillIssue.ToString()),
            BarColumn.DamageDown => (Label(col), c.DamageDown.ToString()),

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

    private void DrawSkillTable(List<SkillEntry> skills, string index, string idPrefix, Vector4 fillColorVec, MeterTab? activeTab)
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

        using var skillFont = FontScope.Push(config.GetFontScale(config.SkillFontSize));

        var maxCount = activeTab?.MaxSkillBreakdownCount ?? config.MaxSkillBreakdownCount;
        var topSkills = maxCount > 0 ? skills.Take(maxCount).ToList() : skills;
        var headerColor = ImGui.ColorConvertFloat4ToU32(config.SkillHeaderTextColor);
        var colPad = config.SkillColumnPadding;
        var isHeal = idPrefix == "heal";
        var valLabel = isHeal ? "Amount" : "Damage";
        var valTooltip = isHeal ? "Amount healed by the skill" : "Damage dealt by the skill";

        float colValW = ImGui.CalcTextSize(valLabel).X;
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

        var mousePos = ImGui.GetMousePos();
        var hdrX = hdrMax.X - 3;
        hdrX -= colHitsW; drawList.AddText(new Vector2(hdrX + (colHitsW - ImGui.CalcTextSize("Hits").X) * 0.5f, hdrMin.Y + textYOff), headerColor, "Hits");
        if (mousePos.X >= hdrX && mousePos.X < hdrX + colHitsW && mousePos.Y >= hdrMin.Y && mousePos.Y < hdrMax.Y) { ImGui.SetTooltip("Hit Count"); }
        hdrX -= colPad;
        hdrX -= colCdhW; drawList.AddText(new Vector2(hdrX + (colCdhW - ImGui.CalcTextSize("!!!").X) * 0.5f, hdrMin.Y + textYOff), headerColor, "!!!");
        if (mousePos.X >= hdrX && mousePos.X < hdrX + colCdhW && mousePos.Y >= hdrMin.Y && mousePos.Y < hdrMax.Y) { ImGui.SetTooltip("Critical Direct Hit %"); }
        hdrX -= colPad;
        hdrX -= colDhW; drawList.AddText(new Vector2(hdrX + (colDhW - ImGui.CalcTextSize("!!").X) * 0.5f, hdrMin.Y + textYOff), headerColor, "!!");
        if (mousePos.X >= hdrX && mousePos.X < hdrX + colDhW && mousePos.Y >= hdrMin.Y && mousePos.Y < hdrMax.Y) { ImGui.SetTooltip("Direct Hit %"); }
        hdrX -= colPad;
        hdrX -= colCritW; drawList.AddText(new Vector2(hdrX + (colCritW - ImGui.CalcTextSize("!").X) * 0.5f, hdrMin.Y + textYOff), headerColor, "!");
        if (mousePos.X >= hdrX && mousePos.X < hdrX + colCritW && mousePos.Y >= hdrMin.Y && mousePos.Y < hdrMax.Y) { ImGui.SetTooltip("Critical Hit %"); }
        hdrX -= colPad;
        hdrX -= colPctW; drawList.AddText(new Vector2(hdrX + (colPctW - ImGui.CalcTextSize("%").X) * 0.5f, hdrMin.Y + textYOff), headerColor, "%");
        if (mousePos.X >= hdrX && mousePos.X < hdrX + colPctW && mousePos.Y >= hdrMin.Y && mousePos.Y < hdrMax.Y) { ImGui.SetTooltip("Damage %"); }
        hdrX -= colPad;
        hdrX -= colValW; drawList.AddText(new Vector2(hdrX + (colValW - ImGui.CalcTextSize(valLabel).X) * 0.5f, hdrMin.Y + textYOff), headerColor, valLabel);
        if (mousePos.X >= hdrX && mousePos.X < hdrX + colValW && mousePos.Y >= hdrMin.Y && mousePos.Y < hdrMax.Y) { ImGui.SetTooltip(valTooltip); }

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

    }

    public void CollapseAll()
    {
        expandedName = null;
    }
}
