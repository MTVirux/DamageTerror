using Dalamud.Bindings.ImGui;
using DamageTerror.Services;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.MainWindow;

public class CombatantDetailPanel
{
    private readonly Configuration config;
    private readonly GraphDataTracker graphTracker;
    private int expandedIndex = -1;

    private static readonly BarColumn[] DamageSection = { BarColumn.Dps, BarColumn.InstantDps, BarColumn.PeakDps, BarColumn.Damage, BarColumn.DamagePercent, BarColumn.MaxHit, BarColumn.DamageShield };
    private static readonly BarColumn[] HealingSection = { BarColumn.Hps, BarColumn.InstantHps, BarColumn.Healed, BarColumn.HealPercent, BarColumn.Overheal, BarColumn.OverhealAmount, BarColumn.CritHealPct, BarColumn.MaxHeal, BarColumn.MaxHealWard, BarColumn.HealCount, BarColumn.AbsorbHeal };
    private static readonly BarColumn[] HitStatSection = { BarColumn.Crit, BarColumn.DirectHit, BarColumn.CritDirectHit, BarColumn.HitRate, BarColumn.Swings, BarColumn.Hits, BarColumn.Misses, BarColumn.CritHitCount, BarColumn.DirectHitCount, BarColumn.CritDirectHitCount };
    private static readonly BarColumn[] DefenseSection = { BarColumn.DamageTaken, BarColumn.DamageTakenPercent, BarColumn.BlockPct, BarColumn.ParryPct, BarColumn.HealsTaken };
    private static readonly BarColumn[] OtherSection = { BarColumn.Deaths, BarColumn.Kills, BarColumn.CombatantDuration, BarColumn.PowerDrain, BarColumn.PowerHeal };

    private EncounterSnapshot? currentSnapshot;
    private bool isLive;

    public CombatantDetailPanel(Configuration config, GraphDataTracker graphTracker)
    {
        this.config = config;
        this.graphTracker = graphTracker;
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

    public void Render(CombatantEntry combatant, int index, EncounterSnapshot? snapshot, bool isLive)
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

        if (ImGui.BeginTabBar($"##detailTabs_{index}"))
        {
            if (ImGui.BeginTabItem($"Graph##{index}"))
            {
                DrawGraphTab(combatant, index);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem($"Skills##{index}"))
            {
                DrawSkillsTab(combatant, index);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem($"Details##{index}"))
            {
                DrawDetailsTab(combatant, index, vis, lc);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.GetFont().Scale = prevScale;
        ImGui.PopFont();

        ImGui.Unindent(config.DetailIndent);
        ImGui.Spacing();
    }

    private void DrawGraphTab(CombatantEntry combatant, int index)
    {
        // For the current encounter (live), always use the live tracker.
        // For historical encounters, use the persisted graph data from the snapshot.
        List<GraphSample> samples;
        if (isLive)
        {
            samples = graphTracker.GetSamples(combatant.Name);
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

        var drawList = ImGui.GetWindowDrawList();
        var regionW = ImGui.GetContentRegionAvail().X;
        var graphH = config.GraphHeight;
        var origin = ImGui.GetCursorScreenPos();

        // Reserve space
        ImGui.Dummy(new Vector2(regionW, graphH));

        // Background
        drawList.AddRectFilled(origin, new Vector2(origin.X + regionW, origin.Y + graphH),
            ImGui.ColorConvertFloat4ToU32(config.GraphBackgroundColor));

        // Compute axis ranges
        var maxTime = samples[^1].TimeSec;
        if (maxTime <= 0f) maxTime = 1f;

        var maxVal = 0f;
        foreach (var s in samples)
        {
            if (config.GraphShowDps && s.Dps > maxVal) maxVal = s.Dps;
            if (config.GraphShowHps && s.Hps > maxVal) maxVal = s.Hps;
            if (config.GraphShowDtps && s.Dtps > maxVal) maxVal = s.Dtps;
        }
        if (maxVal <= 0f) maxVal = 1f;
        maxVal *= 1.1f; // 10% headroom

        var padding = 4f;
        var plotW = regionW - padding * 2;
        var plotH = graphH - padding * 2;
        var plotOrigin = new Vector2(origin.X + padding, origin.Y + padding);

        // Grid lines (3 horizontal)
        var gridColor = ImGui.ColorConvertFloat4ToU32(config.GraphGridColor);
        for (var g = 1; g <= 3; g++)
        {
            var gy = plotOrigin.Y + plotH * (1f - g / 4f);
            drawList.AddLine(new Vector2(plotOrigin.X, gy), new Vector2(plotOrigin.X + plotW, gy), gridColor);
        }

        // Helper to map sample to screen pos
        Vector2 MapPoint(GraphSample s, float val) => new(
            plotOrigin.X + (s.TimeSec / maxTime) * plotW,
            plotOrigin.Y + plotH * (1f - val / maxVal));

        var thickness = config.GraphLineThickness;

        // Draw lines for each enabled series
        if (config.GraphShowDps)
        {
            var col = ImGui.ColorConvertFloat4ToU32(config.GraphDpsColor);
            for (var i = 1; i < samples.Count; i++)
                drawList.AddLine(MapPoint(samples[i - 1], samples[i - 1].Dps), MapPoint(samples[i], samples[i].Dps), col, thickness);
        }
        if (config.GraphShowHps)
        {
            var col = ImGui.ColorConvertFloat4ToU32(config.GraphHpsColor);
            for (var i = 1; i < samples.Count; i++)
                drawList.AddLine(MapPoint(samples[i - 1], samples[i - 1].Hps), MapPoint(samples[i], samples[i].Hps), col, thickness);
        }
        if (config.GraphShowDtps)
        {
            var col = ImGui.ColorConvertFloat4ToU32(config.GraphDtpsColor);
            for (var i = 1; i < samples.Count; i++)
                drawList.AddLine(MapPoint(samples[i - 1], samples[i - 1].Dtps), MapPoint(samples[i], samples[i].Dtps), col, thickness);
        }

        // Y-axis label
        var maxLabel = maxVal >= 1000 ? $"{maxVal / 1000:F1}K" : $"{maxVal:F0}";
        drawList.AddText(new Vector2(origin.X + 2, origin.Y + 2), ImGui.ColorConvertFloat4ToU32(config.DetailLabelColor), maxLabel);

        // Time axis label
        var timeLabel = FormatTime(maxTime);
        var timeLabelSize = ImGui.CalcTextSize(timeLabel);
        drawList.AddText(new Vector2(origin.X + regionW - timeLabelSize.X - 2, origin.Y + graphH - timeLabelSize.Y - 2),
            ImGui.ColorConvertFloat4ToU32(config.DetailLabelColor), timeLabel);

        // Legend
        ImGui.Spacing();
        if (config.GraphShowDps)
        {
            ImGui.TextColored(config.GraphDpsColor, "iDPS");
            ImGui.SameLine();
        }
        if (config.GraphShowHps)
        {
            ImGui.TextColored(config.GraphHpsColor, "iHPS");
            ImGui.SameLine();
        }
        if (config.GraphShowDtps)
        {
            ImGui.TextColored(config.GraphDtpsColor, "iDTPS");
        }
        ImGui.Spacing();
    }


    private static string FormatTime(float seconds)
    {
        var m = (int)(seconds / 60);
        var s = (int)(seconds % 60);
        return m > 0 ? $"{m}:{s:D2}" : $"{s}s";
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
