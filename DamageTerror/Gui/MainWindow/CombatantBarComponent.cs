using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.MainWindow;

public sealed class CombatantBarComponent
{
    private readonly Configuration config;
    private readonly ITextureProvider textureProvider;

    public CombatantBarComponent(Configuration config, ITextureProvider textureProvider)
    {
        this.config = config;
        this.textureProvider = textureProvider;
    }

    public static readonly Dictionary<BarColumn, string> ColumnWidthTemplates = new()
    {
        { BarColumn.Dps, "000.0K" },
        { BarColumn.Hps, "000.0K" },
        { BarColumn.Damage, "000.0K" },
        { BarColumn.Healed, "000.0K" },
        { BarColumn.DamagePercent, "00.0%" },
        { BarColumn.HealPercent, "00.0%" },
        { BarColumn.DirectHit, "100%" },
        { BarColumn.Crit, "100%" },
        { BarColumn.CritDirectHit, "100%" },
        { BarColumn.Deaths, "00" },
        { BarColumn.DamageTaken, "000.0K" },
        { BarColumn.DamageTakenPercent, "00.0%" },
        { BarColumn.Overheal, "100%" },
        { BarColumn.OverhealAmount, "000.0K" },
        { BarColumn.MaxHit, "SkillNameHere" },
        { BarColumn.MaxHitValue, "000.0K" },
        { BarColumn.PeakDps, "000.0K" },
        { BarColumn.MaxHeal, "SkillNameHere" },
        { BarColumn.MaxHealValue, "000.0K" },
        { BarColumn.Swings, "0000" },
        { BarColumn.Hits, "0000" },
        { BarColumn.Misses, "0000" },
        { BarColumn.HitRate, "100%" },
        { BarColumn.CritHitCount, "0000" },
        { BarColumn.DirectHitCount, "0000" },
        { BarColumn.CritDirectHitCount, "0000" },
        { BarColumn.BlockPct, "100%" },
        { BarColumn.ParryPct, "100%" },
        { BarColumn.HealsTaken, "000.0K" },
        { BarColumn.AbsorbHeal, "000.0K" },
        { BarColumn.Kills, "00" },
        { BarColumn.InstantDps, "000.0K" },
        { BarColumn.InstantHps, "000.0K" },
        { BarColumn.CritHealPct, "100%" },
        { BarColumn.HealCount, "0000" },
        { BarColumn.CombatantDuration, "00:00" },
        { BarColumn.DamageShield, "000.0K" },
        { BarColumn.MaxHealWard, "000.0K" },
        { BarColumn.PowerDrain, "000.0K" },
        { BarColumn.PowerHeal, "000.0K" },
        { BarColumn.LegsSweeped, "00" },
        { BarColumn.SkillIssue, "00" },
        { BarColumn.DamageDown, "00" },
        { BarColumn.EncDps, "000.0K" },
        { BarColumn.EncHps, "000.0K" },
        { BarColumn.DpsRank, "00/00" },
        { BarColumn.HpsRank, "00/00" },
        { BarColumn.GroupDps, "000.0K" },
        { BarColumn.GroupHps, "000.0K" },
        { BarColumn.GroupDamage, "000.0K" },
        { BarColumn.GroupHealed, "000.0K" },
        { BarColumn.GroupDamageTaken, "000.0K" },
        { BarColumn.GroupDeaths, "00" },
        { BarColumn.GroupSkillIssue, "00" },
        { BarColumn.GroupOverheal, "000.0K" },
        { BarColumn.GroupInstantDps, "000.0K" },
        { BarColumn.GroupInstantHps, "000.0K" },
        { BarColumn.GroupAvgDps, "000.0K" },
        { BarColumn.GroupAvgHps, "000.0K" },
        { BarColumn.GroupAvgCrit, "100%" },
        { BarColumn.GroupAvgDirectHit, "100%" },
        { BarColumn.GroupAvgCritDirectHit, "100%" },
        { BarColumn.GroupAvgOverhealPct, "100%" },
        { BarColumn.GroupAvgCritHealPct, "100%" },
        { BarColumn.GroupAvgHitRate, "100%" },
        { BarColumn.GroupPeakDps, "000.0K" },
        { BarColumn.GroupMaxHitValue, "000.0K" },
        { BarColumn.GroupMaxHealValue, "000.0K" },
        { BarColumn.Positionals, "00/00" },
        { BarColumn.PositionalHits, "0000" },
        { BarColumn.PositionalMisses, "0000" },
        { BarColumn.PositionalPct, "100%" },
    };

    private static readonly Dictionary<BarColumn, Func<CombatantEntry, Configuration, MeterTab?, string>> ColumnFormatters
        = new()
        {
            [BarColumn.Dps]                 = (c, cfg, tab) => ValueFormatter.FormatColumn(c.EncDps, cfg, BarColumn.Dps, tab),
            [BarColumn.Hps]                 = (c, cfg, tab) => ValueFormatter.FormatColumn(c.EncHps, cfg, BarColumn.Hps, tab),
            [BarColumn.Damage]              = (c, cfg, tab) => ValueFormatter.FormatColumn(c.Damage, cfg, BarColumn.Damage, tab),
            [BarColumn.Healed]              = (c, cfg, tab) => ValueFormatter.FormatColumn(c.Healed, cfg, BarColumn.Healed, tab),
            [BarColumn.DamagePercent]       = (c, cfg, tab) => !string.IsNullOrEmpty(c.DamagePercent) ? c.DamagePercent : "0%",
            [BarColumn.HealPercent]         = (c, cfg, tab) => !string.IsNullOrEmpty(c.HealedPercent) ? c.HealedPercent : "0%",
            [BarColumn.DirectHit]           = (c, cfg, tab) => ValueFormatter.FormatPercentColumn(c.DirectHitPct, cfg, BarColumn.DirectHit, tab),
            [BarColumn.Crit]                = (c, cfg, tab) => ValueFormatter.FormatPercentColumn(c.CritPct, cfg, BarColumn.Crit, tab),
            [BarColumn.CritDirectHit]       = (c, cfg, tab) => ValueFormatter.FormatPercentColumn(c.CritDirectHitPct, cfg, BarColumn.CritDirectHit, tab),
            [BarColumn.Deaths]              = (c, cfg, tab) => $"{c.Deaths}",
            [BarColumn.DamageTaken]         = (c, cfg, tab) => ValueFormatter.FormatColumn(c.DamageTaken, cfg, BarColumn.DamageTaken, tab),
            [BarColumn.DamageTakenPercent]  = (c, cfg, tab) => !string.IsNullOrEmpty(c.DamageTakenPercent) ? c.DamageTakenPercent : "0%",
            [BarColumn.Overheal]            = (c, cfg, tab) => ValueFormatter.FormatPercentColumn(c.OverhealPct, cfg, BarColumn.Overheal, tab),
            [BarColumn.OverhealAmount]      = (c, cfg, tab) => ValueFormatter.FormatColumn(c.OverhealAmount, cfg, BarColumn.OverhealAmount, tab),
            [BarColumn.MaxHit]              = (c, cfg, tab) => ValueFormatter.AbbreviateSkillName(c.MaxHitSkillName, cfg.MaxHitSkillNameLength, cfg.TruncateSkillNames),
            [BarColumn.MaxHitValue]         = (c, cfg, tab) => ValueFormatter.FormatColumn(c.MaxHitDamage, cfg, BarColumn.MaxHitValue, tab),
            [BarColumn.PeakDps]             = (c, cfg, tab) => ValueFormatter.FormatColumn(c.PeakDps, cfg, BarColumn.PeakDps, tab),
            [BarColumn.MaxHeal]             = (c, cfg, tab) => ValueFormatter.AbbreviateSkillName(c.MaxHealSkillName, cfg.MaxHitSkillNameLength, cfg.TruncateSkillNames),
            [BarColumn.MaxHealValue]        = (c, cfg, tab) => ValueFormatter.FormatColumn(c.MaxHealAmount, cfg, BarColumn.MaxHealValue, tab),
            [BarColumn.Swings]              = (c, cfg, tab) => $"{c.Swings}",
            [BarColumn.Hits]                = (c, cfg, tab) => $"{c.Hits}",
            [BarColumn.Misses]              = (c, cfg, tab) => $"{c.Misses}",
            [BarColumn.HitRate]             = (c, cfg, tab) => ValueFormatter.FormatPercentColumn(c.HitRate, cfg, BarColumn.HitRate, tab),
            [BarColumn.CritHitCount]        = (c, cfg, tab) => $"{c.CritHitCount}",
            [BarColumn.DirectHitCount]      = (c, cfg, tab) => $"{c.DirectHitCount}",
            [BarColumn.CritDirectHitCount]  = (c, cfg, tab) => $"{c.CritDirectHitCount}",
            [BarColumn.BlockPct]            = (c, cfg, tab) => ValueFormatter.FormatPercentColumn(c.BlockPct, cfg, BarColumn.BlockPct, tab),
            [BarColumn.ParryPct]            = (c, cfg, tab) => ValueFormatter.FormatPercentColumn(c.ParryPct, cfg, BarColumn.ParryPct, tab),
            [BarColumn.HealsTaken]          = (c, cfg, tab) => ValueFormatter.FormatColumn(c.HealsTaken, cfg, BarColumn.HealsTaken, tab),
            [BarColumn.AbsorbHeal]          = (c, cfg, tab) => ValueFormatter.FormatColumn(c.AbsorbHeal, cfg, BarColumn.AbsorbHeal, tab),
            [BarColumn.Kills]               = (c, cfg, tab) => $"{c.Kills}",
            [BarColumn.InstantDps]          = (c, cfg, tab) => ValueFormatter.FormatColumn(c.InstantDps, cfg, BarColumn.InstantDps, tab),
            [BarColumn.InstantHps]          = (c, cfg, tab) => ValueFormatter.FormatColumn(c.InstantHps, cfg, BarColumn.InstantHps, tab),
            [BarColumn.CritHealPct]         = (c, cfg, tab) => ValueFormatter.FormatPercentColumn(c.CritHealPct, cfg, BarColumn.CritHealPct, tab),
            [BarColumn.HealCount]           = (c, cfg, tab) => $"{c.HealCount}",
            [BarColumn.CombatantDuration]   = (c, cfg, tab) => c.CombatantDuration,
            [BarColumn.DamageShield]        = (c, cfg, tab) => ValueFormatter.FormatColumn(c.DamageShield, cfg, BarColumn.DamageShield, tab),
            [BarColumn.MaxHealWard]         = (c, cfg, tab) => ValueFormatter.FormatColumn(c.MaxHealWardAmount, cfg, BarColumn.MaxHealWard, tab),
            [BarColumn.PowerDrain]          = (c, cfg, tab) => ValueFormatter.FormatColumn(c.PowerDrain, cfg, BarColumn.PowerDrain, tab),
            [BarColumn.PowerHeal]           = (c, cfg, tab) => ValueFormatter.FormatColumn(c.PowerHeal, cfg, BarColumn.PowerHeal, tab),
            [BarColumn.LegsSweeped]         = (c, cfg, tab) => $"{c.Stuns}",
            [BarColumn.SkillIssue]          = (c, cfg, tab) => $"{c.SkillIssue}",
            [BarColumn.DamageDown]          = (c, cfg, tab) => $"{c.DamageDown}",
            [BarColumn.Positionals]         = (c, cfg, tab) => $"{c.Positionals}",
            [BarColumn.PositionalHits]      = (c, cfg, tab) => $"{c.PositionalHits}",
            [BarColumn.PositionalMisses]    = (c, cfg, tab) => $"{c.PositionalMisses}",
            [BarColumn.PositionalPct]       = (c, cfg, tab) => ValueFormatter.FormatPercentColumn(c.PositionalPct, cfg, BarColumn.PositionalPct, tab),
            [BarColumn.EncDps]              = (c, cfg, tab) => ValueFormatter.FormatColumn(c.RaidDps, cfg, BarColumn.EncDps, tab),
            [BarColumn.EncHps]              = (c, cfg, tab) => ValueFormatter.FormatColumn(c.RaidHps, cfg, BarColumn.EncHps, tab),
            [BarColumn.DpsRank]             = (c, cfg, tab) => $"{c.DpsRank}/{c.DpsRankTotal}",
            [BarColumn.HpsRank]             = (c, cfg, tab) => $"{c.HpsRank}/{c.HpsRankTotal}",
        };

    public static string GetColumnDisplayValue(CombatantEntry combatant, BarColumn col,
        Configuration config, MeterTab? activeTab)
    {
        return ColumnFormatters.TryGetValue(col, out var formatter)
            ? formatter(combatant, config, activeTab)
            : string.Empty;
    }

    public static string GetGroupColumnDisplayValue(BarColumn col, Configuration config, MeterTab? activeTab, GroupAggregates? group)
    {
        if (group == null) return "\u2014";
        return col switch
        {
            BarColumn.GroupDps => ValueFormatter.FormatColumn(group.Dps, config, BarColumn.GroupDps, activeTab),
            BarColumn.GroupHps => ValueFormatter.FormatColumn(group.Hps, config, BarColumn.GroupHps, activeTab),
            BarColumn.GroupDamage => ValueFormatter.FormatColumn(group.Damage, config, BarColumn.GroupDamage, activeTab),
            BarColumn.GroupHealed => ValueFormatter.FormatColumn(group.Healed, config, BarColumn.GroupHealed, activeTab),
            BarColumn.GroupDamageTaken => ValueFormatter.FormatColumn(group.DamageTaken, config, BarColumn.GroupDamageTaken, activeTab),
            BarColumn.GroupDeaths => $"{group.Deaths}",
            BarColumn.GroupSkillIssue => $"{group.SkillIssue}",
            BarColumn.GroupDamageDown => $"{group.DamageDown}",
            BarColumn.GroupOverheal => ValueFormatter.FormatColumn(group.Overheal, config, BarColumn.GroupOverheal, activeTab),
            BarColumn.GroupInstantDps => ValueFormatter.FormatColumn(group.InstantDps, config, BarColumn.GroupInstantDps, activeTab),
            BarColumn.GroupInstantHps => ValueFormatter.FormatColumn(group.InstantHps, config, BarColumn.GroupInstantHps, activeTab),
            BarColumn.GroupAvgDps => ValueFormatter.FormatColumn(group.AvgDps, config, BarColumn.GroupAvgDps, activeTab),
            BarColumn.GroupAvgHps => ValueFormatter.FormatColumn(group.AvgHps, config, BarColumn.GroupAvgHps, activeTab),
            BarColumn.GroupAvgCrit => ValueFormatter.FormatPercentColumn(group.AvgCrit, config, BarColumn.GroupAvgCrit, activeTab),
            BarColumn.GroupAvgDirectHit => ValueFormatter.FormatPercentColumn(group.AvgDirectHit, config, BarColumn.GroupAvgDirectHit, activeTab),
            BarColumn.GroupAvgCritDirectHit => ValueFormatter.FormatPercentColumn(group.AvgCritDirectHit, config, BarColumn.GroupAvgCritDirectHit, activeTab),
            BarColumn.GroupAvgOverhealPct => ValueFormatter.FormatPercentColumn(group.AvgOverhealPct, config, BarColumn.GroupAvgOverhealPct, activeTab),
            BarColumn.GroupAvgCritHealPct => ValueFormatter.FormatPercentColumn(group.AvgCritHealPct, config, BarColumn.GroupAvgCritHealPct, activeTab),
            BarColumn.GroupAvgHitRate => ValueFormatter.FormatPercentColumn(group.AvgHitRate, config, BarColumn.GroupAvgHitRate, activeTab),
            BarColumn.GroupPeakDps => ValueFormatter.FormatColumn(group.PeakDps, config, BarColumn.GroupPeakDps, activeTab),
            BarColumn.GroupMaxHitValue => ValueFormatter.FormatColumn(group.MaxHitValue, config, BarColumn.GroupMaxHitValue, activeTab),
            BarColumn.GroupMaxHealValue => ValueFormatter.FormatColumn(group.MaxHealValue, config, BarColumn.GroupMaxHealValue, activeTab),
            _ => string.Empty,
        };
    }

    private static readonly HashSet<BarColumn> GroupColumns = new()
    {
        BarColumn.GroupDps, BarColumn.GroupHps, BarColumn.GroupDamage,
        BarColumn.GroupHealed, BarColumn.GroupDamageTaken, BarColumn.GroupDeaths,
        BarColumn.GroupOverheal, BarColumn.GroupInstantDps, BarColumn.GroupInstantHps,
        BarColumn.GroupAvgDps, BarColumn.GroupAvgHps, BarColumn.GroupAvgCrit,
        BarColumn.GroupAvgDirectHit, BarColumn.GroupAvgCritDirectHit, BarColumn.GroupAvgOverhealPct,
        BarColumn.GroupAvgCritHealPct, BarColumn.GroupAvgHitRate,
        BarColumn.GroupSkillIssue,
        BarColumn.GroupDamageDown,
        BarColumn.GroupPeakDps, BarColumn.GroupMaxHitValue, BarColumn.GroupMaxHealValue,
    };

    public static bool IsGroupColumn(BarColumn col) => GroupColumns.Contains(col);

    public bool Render(CombatantEntry combatant, double maxValue, int index, SortField sortBy, MeterTab? activeTab, string currentPlayerName = "", GroupAggregates? groupAggregates = null)
    {
        var barHeight = config.BarHeight;
        var iconSize = config.IconSize;
        var value = GetSortValue(combatant, sortBy);
        var isLocalPlayer = combatant.IsLocalPlayer;

        var fraction = maxValue > 0 ? (float)(value / maxValue) : 0f;
        fraction = Math.Clamp(fraction, 0f, 1f);

        var windowWidth = ImGui.GetContentRegionAvail().X;
        var cursorPos = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();

        var bgColor = config.BarBackgroundColor;
        var barBgColor = ImGui.ColorConvertFloat4ToU32(bgColor);
        drawList.AddRectFilled(
            cursorPos,
            new Vector2(cursorPos.X + windowWidth, cursorPos.Y + barHeight),
            barBgColor,
            config.BarRounding);

        if (fraction > 0)
        {
            var barColor = JobColorHelper.GetBarColor(combatant.Job, config.BarAlpha, config);
            var barColorU32 = ImGui.ColorConvertFloat4ToU32(barColor);
            drawList.AddRectFilled(
                cursorPos,
                new Vector2(cursorPos.X + windowWidth * fraction, cursorPos.Y + barHeight),
                barColorU32,
                config.BarRounding);
        }

        if (config.SelfBarHighlight && isLocalPlayer)
        {
            var stripWidth = 3f;
            var highlightColor = ImGui.ColorConvertFloat4ToU32(config.SelfBarHighlightColor);
            drawList.AddRectFilled(
                cursorPos,
                new Vector2(cursorPos.X + stripWidth, cursorPos.Y + barHeight),
                highlightColor);
        }

        var clicked = ImGui.InvisibleButton($"##combatant_{index}", new Vector2(windowWidth, barHeight));
        if (config.ShowTooltip && ImGui.IsItemHovered())
        {
            DrawTooltip(combatant, activeTab);
        }
        using var fontScope = FontScope.Push(config.GetFontScale(config.BarFontSize));

        var textY = cursorPos.Y + (barHeight - ImGui.GetTextLineHeight()) * 0.5f;
        var textStartX = cursorPos.X + config.BarLeftPadding;

        if (config.ShowRankNumber)
        {
            var rankStr = $"{index + 1}. ";
            var rankColor = ImGui.ColorConvertFloat4ToU32(config.NameTextColor);
            drawList.AddText(new Vector2(textStartX, textY), rankColor, rankStr);
            textStartX += ImGui.CalcTextSize(rankStr).X;
        }

        if (config.ShowJobIcons)
        {
            var iconId = JobIconHelper.GetIconId(combatant.Job, config.JobIconStyle, config.CustomJobIcons);
            if (iconId.HasValue)
            {
                var icon = textureProvider.GetFromGameIcon(new GameIconLookup(iconId.Value));
                if (icon.TryGetWrap(out var iconWrap, out _))
                {
                    var iconY = cursorPos.Y + (barHeight - iconSize) * 0.5f;
                    drawList.AddImage(
                        iconWrap.Handle,
                        new Vector2(textStartX, iconY),
                        new Vector2(textStartX + iconSize, iconY + iconSize));
                    textStartX += iconSize + config.IconTextPadding;
                }
            }
        }

        if (config.ShowJobAbbrevOnBar && !string.IsNullOrEmpty(combatant.Job))
        {
            var jobStr = $"[{combatant.Job.ToUpperInvariant()}] ";
            var jobColor = ImGui.ColorConvertFloat4ToU32(config.NameTextColor);
            drawList.AddText(new Vector2(textStartX, textY), jobColor, jobStr);
            textStartX += ImGui.CalcTextSize(jobStr).X;
        }

        if (config.ShowNameOnBar)
        {
            var displayName = NameFormatHelper.GetDisplayName(combatant.Name, combatant.Job, isLocalPlayer, config);
            var nameCol = (isLocalPlayer && config.UseSelfNameColor)
                ? config.SelfNameColor
                : config.NameTextColor;
            var nameColor = ImGui.ColorConvertFloat4ToU32(nameCol);
            drawList.AddText(new Vector2(textStartX, textY), nameColor, displayName);
        }

        var rightX = cursorPos.X + windowWidth - config.BarRightPadding;
        var defaultValColor = ImGui.ColorConvertFloat4ToU32(config.ValueTextColor);
        var colSpacing = config.BarColumnSpacing;

        var columnOrder = activeTab?.ColumnOrder ?? new List<BarColumn>();
        EnsureColumnOrderComplete(columnOrder);

        for (var ci = columnOrder.Count - 1; ci >= 0; ci--)
        {
            var col = columnOrder[ci];
            if (activeTab == null || !activeTab.IsColumnVisible(col)) continue;

            var text = IsGroupColumn(col)
                ? GetGroupColumnDisplayValue(col, config, activeTab, groupAggregates)
                : GetColumnDisplayValue(combatant, col, config, activeTab);
            if (!ColumnWidthTemplates.TryGetValue(col, out var template)) continue;
            var colW = activeTab?.GetColumnWidth(col) ?? ImGui.CalcTextSize(template).X;
            rightX -= colW;
            var colColor = activeTab?.GetColumnValueColor(col);
            var valColor = colColor.HasValue ? ImGui.ColorConvertFloat4ToU32(colColor.Value) : defaultValColor;
            drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(text).X) * 0.5f, textY), valColor, text);
            rightX -= colSpacing;
        }

        fontScope.Dispose();

        if (config.BarSpacing > 0)
        {
            ImGui.SetCursorScreenPos(new Vector2(cursorPos.X, cursorPos.Y + barHeight + config.BarSpacing));
        }

        return clicked;
    }

    public static double GetSortValue(CombatantEntry c, SortField field) => field switch
    {
        SortField.EncDps => c.EncDps,
        SortField.EncHps => c.EncHps,
        SortField.Damage => c.Damage,
        SortField.Healed => c.Healed,
        SortField.CritPct => c.CritPct,
        SortField.Deaths => c.Deaths,
        SortField.DamageTaken => c.DamageTaken,
        _ => c.EncDps,
    };



    public static void EnsureColumnOrderComplete(List<BarColumn> list)
    {
        foreach (var col in Enum.GetValues<BarColumn>())
        {
            if (!list.Contains(col))
                list.Add(col);
        }
        var seen = new HashSet<BarColumn>();
        list.RemoveAll(col => !seen.Add(col));
    }

    private void DrawTooltip(CombatantEntry combatant, MeterTab? activeTab)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, config.TooltipRounding);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(config.TooltipPadding, config.TooltipPadding));
        ImGui.PushStyleColor(ImGuiCol.PopupBg, config.TooltipBackgroundColor);

        ImGui.BeginTooltip();

        using var tooltipFont = FontScope.Push(config.GetFontScale(config.TooltipFontSize));

        var labelColor = config.TooltipLabelColor;
        var textColor = config.TooltipTextColor;

        var tooltipFields = activeTab?.TooltipFields ?? config.TooltipFields;
        foreach (var field in tooltipFields)
        {
            if (field == TooltipField.TopDamageSkills || field == TooltipField.TopHealingSkills)
            {
                DrawTopSkillsTooltipSection(combatant, field, activeTab, labelColor, textColor);
                continue;
            }

            var (label, value) = GetTooltipFieldValue(combatant, field, activeTab);
            ImGui.TextColored(labelColor, label + ":");
            ImGui.SameLine();
            ImGui.TextColored(textColor, value);
        }

        tooltipFont.Dispose();

        ImGui.EndTooltip();

        ImGui.PopStyleColor();
        ImGui.PopStyleVar(2);
    }

    private (string Label, string Value) GetTooltipFieldValue(CombatantEntry combatant, TooltipField field, MeterTab? activeTab)
    {
        var label = activeTab != null ? activeTab.GetTooltipFieldLabel(field)
            : ColumnLabels.DefaultTooltipFieldLabels.GetValueOrDefault(field, field.ToString());
        var value = field switch
        {
            TooltipField.Name => NameFormatHelper.GetDisplayName(combatant.Name, combatant.Job, combatant.IsLocalPlayer, config),
            TooltipField.Job => !string.IsNullOrEmpty(combatant.Job) ? JobRegistry.GetFullName(combatant.Job) : "—",
            TooltipField.Dps => GetColumnDisplayValue(combatant, BarColumn.Dps, config, activeTab),
            TooltipField.Hps => GetColumnDisplayValue(combatant, BarColumn.Hps, config, activeTab),
            TooltipField.Damage => GetColumnDisplayValue(combatant, BarColumn.Damage, config, activeTab),
            TooltipField.Healed => GetColumnDisplayValue(combatant, BarColumn.Healed, config, activeTab),
            TooltipField.DamagePercent => combatant.DamagePercent,
            TooltipField.HealPercent => combatant.HealedPercent,
            TooltipField.Crit => GetColumnDisplayValue(combatant, BarColumn.Crit, config, activeTab),
            TooltipField.DirectHit => GetColumnDisplayValue(combatant, BarColumn.DirectHit, config, activeTab),
            TooltipField.CritDirectHit => GetColumnDisplayValue(combatant, BarColumn.CritDirectHit, config, activeTab),
            TooltipField.Deaths => $"{combatant.Deaths}",
            TooltipField.DamageTaken => GetColumnDisplayValue(combatant, BarColumn.DamageTaken, config, activeTab),
            TooltipField.Overheal => GetColumnDisplayValue(combatant, BarColumn.Overheal, config, activeTab),
            TooltipField.OverhealAmount => GetColumnDisplayValue(combatant, BarColumn.OverhealAmount, config, activeTab),
            TooltipField.MaxHit => combatant.MaxHitSkillName ?? "",
            TooltipField.MaxHitValue => GetColumnDisplayValue(combatant, BarColumn.MaxHitValue, config, activeTab),
            TooltipField.MaxHeal => combatant.MaxHealSkillName ?? "",
            TooltipField.MaxHealValue => GetColumnDisplayValue(combatant, BarColumn.MaxHealValue, config, activeTab),
            TooltipField.PeakDps => GetColumnDisplayValue(combatant, BarColumn.PeakDps, config, activeTab),
            TooltipField.Swings => $"{combatant.Swings}",
            TooltipField.Hits => $"{combatant.Hits}",
            TooltipField.Misses => $"{combatant.Misses}",
            TooltipField.HitRate => GetColumnDisplayValue(combatant, BarColumn.HitRate, config, activeTab),
            TooltipField.Kills => $"{combatant.Kills}",
            TooltipField.CombatantDuration => combatant.CombatantDuration,
            TooltipField.HealsTaken => GetColumnDisplayValue(combatant, BarColumn.HealsTaken, config, activeTab),
            TooltipField.InstantDps => GetColumnDisplayValue(combatant, BarColumn.InstantDps, config, activeTab),
            TooltipField.InstantHps => GetColumnDisplayValue(combatant, BarColumn.InstantHps, config, activeTab),
            TooltipField.CritHealPct => GetColumnDisplayValue(combatant, BarColumn.CritHealPct, config, activeTab),
            TooltipField.HealCount => $"{combatant.HealCount}",
            TooltipField.DamageShield => GetColumnDisplayValue(combatant, BarColumn.DamageShield, config, activeTab),
            TooltipField.MaxHealWard => GetColumnDisplayValue(combatant, BarColumn.MaxHealWard, config, activeTab),
            TooltipField.LegsSweeped => $"{combatant.Stuns}",
            TooltipField.SkillIssue => $"{combatant.SkillIssue}",
            TooltipField.DamageDown => $"{combatant.DamageDown}",
            TooltipField.Positionals => $"{combatant.Positionals}",
            TooltipField.PositionalHits => $"{combatant.PositionalHits}",
            TooltipField.PositionalMisses => $"{combatant.PositionalMisses}",
            TooltipField.EncDps => GetColumnDisplayValue(combatant, BarColumn.EncDps, config, activeTab),
            TooltipField.EncHps => GetColumnDisplayValue(combatant, BarColumn.EncHps, config, activeTab),
            TooltipField.TopDamageSkills => "",
            TooltipField.TopHealingSkills => "",
            _ => "",
        };
        return (label, value);
    }

    private void DrawTopSkillsTooltipSection(CombatantEntry combatant, TooltipField field, MeterTab? activeTab, Vector4 labelColor, Vector4 textColor)
    {
        var isHealing = field == TooltipField.TopHealingSkills;
        var skills = isHealing ? combatant.HealingSkills : combatant.Skills;
        var count = activeTab?.TooltipTopSkillCount ?? config.TooltipTopSkillCount;
        var header = activeTab != null ? activeTab.GetTooltipFieldLabel(field)
            : ColumnLabels.DefaultTooltipFieldLabels.GetValueOrDefault(field, field.ToString());

        if (skills == null || skills.Count == 0)
            return;

        var topSkills = skills
            .OrderByDescending(s => s.TotalDamage)
            .Take(count)
            .ToList();

        if (topSkills.Count == 0)
            return;

        ImGui.Spacing();
        ImGui.TextColored(labelColor, header + ":");
        foreach (var skill in topSkills)
        {
            var value = ValueFormatter.Format(skill.TotalDamage, config);
            ImGui.TextColored(textColor, $"  {skill.Name}  {value}  ({skill.DamagePercent:0.0}%)");
        }
    }
}
