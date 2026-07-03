namespace DamageTerror.Gui.MainWindow.Detail;

internal sealed class BuffsTabRenderer : IDetailTabRenderer
{
    private readonly Configuration config;
    private readonly StatusTracker statusTracker;

    public BuffsTabRenderer(Configuration config, StatusTracker statusTracker)
    {
        this.config = config;
        this.statusTracker = statusTracker;
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

    public void Render(in DetailRenderContext ctx)
    {
        var combatant = ctx.Combatant;
        var index = ctx.Index;
        var isLive = ctx.IsLive;
        var currentSnapshot = ctx.Snapshot;

        List<StatusApplication> received;
        List<StatusApplication> applied;

        if (isLive)
        {
            received = statusTracker.GetStatusesReceived(combatant.Name);
            applied = statusTracker.GetStatusHistory(combatant.Name);

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
                DrawStatusTable(AggregateStatuses(received, currentTime), encounterDuration, index, "recv");
                ImGui.EndTabItem();
            }

            if (hasApplied && ImGui.BeginTabItem($"Applied##{index}"))
            {
                DrawStatusTable(AggregateStatuses(applied, currentTime), encounterDuration, index, "appl");
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
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

    private void DrawStatusTable(List<AggregatedStatus> statuses, float encounterDuration, string index, string idPrefix)
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
            var avgText0 = s.IsPermanent ? "∞" : $"{(s.ApplicationCount > 0 ? s.TotalUptime / s.ApplicationCount : 0f):F1}s";
            colAvgW = Math.Max(colAvgW, ImGui.CalcTextSize(avgText0).X);
        }

        ImGui.InvisibleButton($"##{idPrefix}_hdr_{index}", new Vector2(availWidth, rowHeight));
        var hdrMin = ImGui.GetItemRectMin();
        var hdrMax = ImGui.GetItemRectMax();
        drawList.AddText(new Vector2(hdrMin.X + 3, hdrMin.Y + textYOff), headerColor, "Status");

        var hdrX = hdrMax.X - 3;
        hdrX -= colAvgW; drawList.AddText(new Vector2(hdrX + (colAvgW - ImGui.CalcTextSize("Avg Dur").X) * 0.5f, hdrMin.Y + textYOff), headerColor, "Avg Dur"); hdrX -= colPad;
        hdrX -= colPctW; drawList.AddText(new Vector2(hdrX + (colPctW - ImGui.CalcTextSize("Uptime%").X) * 0.5f, hdrMin.Y + textYOff), headerColor, "Uptime%"); hdrX -= colPad;
        hdrX -= colUptimeW; drawList.AddText(new Vector2(hdrX + (colUptimeW - ImGui.CalcTextSize("Uptime").X) * 0.5f, hdrMin.Y + textYOff), headerColor, "Uptime"); hdrX -= colPad;
        hdrX -= colCountW; drawList.AddText(new Vector2(hdrX + (colCountW - ImGui.CalcTextSize("Count").X) * 0.5f, hdrMin.Y + textYOff), headerColor, "Count");

        var maxUptime = statuses.Count > 0 ? statuses[0].TotalUptime : 1f;
        if (maxUptime <= 0f) maxUptime = 1f;

        var rowIdx = 0;
        foreach (var status in statuses)
        {
            var barFraction = status.TotalUptime / maxUptime;
            var fillColorVec = status switch
            {
                { IsDoT: true } => config.SkillPhysicalFillColor,
                { IsHoT: true } => config.SkillHealingFillColor,
                { IsBuff: true } => config.BuffFillColor,
                _ => config.DebuffFillColor,
            };
            var fillColor = ImGui.ColorConvertFloat4ToU32(fillColorVec);

            ImGui.InvisibleButton($"##{idPrefix}_{index}_{rowIdx}", new Vector2(availWidth, rowHeight));
            var min = ImGui.GetItemRectMin();
            var max = ImGui.GetItemRectMax();

            drawList.AddRectFilled(min, max, bgColor, rounding);
            drawList.AddRectFilled(min, new Vector2(min.X + availWidth * barFraction, max.Y), fillColor, rounding);

            var nameText = status.StatusName;
            if (status.IsDoT) nameText += " [DoT]";
            else if (status.IsHoT) nameText += " [HoT]";
            drawList.AddText(new Vector2(min.X + 3, min.Y + textYOff), textColor, nameText);

            var x = max.X - 3;

            var avgText = status.IsPermanent ? "∞" : $"{(status.ApplicationCount > 0 ? status.TotalUptime / status.ApplicationCount : 0f):F1}s";
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
}
