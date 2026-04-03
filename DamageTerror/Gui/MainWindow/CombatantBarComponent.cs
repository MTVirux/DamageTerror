using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.MainWindow;

public struct ColumnVisibility
{
    public bool ShowDps, ShowHps, ShowDamage, ShowHealed, ShowDamagePercent, ShowHealPercent;
    public bool ShowDirectHit, ShowCrit, ShowCritDirectHit, ShowDeaths;
    public bool ShowDamageTaken, ShowDamageTakenPercent, ShowOverheal, ShowOverhealAmount, ShowMaxHit, ShowPeakDps;
    public bool ShowMaxHeal, ShowSwings, ShowHits, ShowMisses, ShowHitRate;
    public bool ShowCritHitCount, ShowDirectHitCount, ShowCritDirectHitCount;
    public bool ShowBlockPct, ShowParryPct, ShowHealsTaken, ShowAbsorbHeal, ShowKills, ShowInstantDps, ShowInstantHps;
    public bool ShowCritHealPct, ShowHealCount, ShowCombatantDuration, ShowDamageShield, ShowMaxHealWard, ShowPowerDrain, ShowPowerHeal;

    public static ColumnVisibility Resolve(Configuration config, MeterTab? activeTab) => new()
    {
        ShowDps = activeTab?.ShowDpsColumn ?? true,
        ShowHps = activeTab?.ShowHpsColumn ?? false,
        ShowDamage = activeTab?.ShowDamageColumn ?? false,
        ShowHealed = activeTab?.ShowHealedColumn ?? false,
        ShowDamagePercent = activeTab?.ShowDamagePercentColumn ?? false,
        ShowHealPercent = activeTab?.ShowHealPercentColumn ?? false,
        ShowDirectHit = activeTab?.ShowDirectHitColumn ?? false,
        ShowCrit = activeTab?.ShowCritColumn ?? false,
        ShowCritDirectHit = activeTab?.ShowCritDirectHitColumn ?? false,
        ShowDeaths = activeTab?.ShowDeathsColumn ?? false,
        ShowDamageTaken = activeTab?.ShowDamageTakenColumn ?? false,
        ShowDamageTakenPercent = activeTab?.ShowDamageTakenPercentColumn ?? false,
        ShowOverheal = activeTab?.ShowOverhealColumn ?? false,
        ShowOverhealAmount = activeTab?.ShowOverhealAmountColumn ?? false,
        ShowMaxHit = activeTab?.ShowMaxHitColumn ?? false,
        ShowPeakDps = activeTab?.ShowPeakDpsColumn ?? false,
        ShowMaxHeal = activeTab?.ShowMaxHealColumn ?? false,
        ShowSwings = activeTab?.ShowSwingsColumn ?? false,
        ShowHits = activeTab?.ShowHitsColumn ?? false,
        ShowMisses = activeTab?.ShowMissesColumn ?? false,
        ShowHitRate = activeTab?.ShowHitRateColumn ?? false,
        ShowCritHitCount = activeTab?.ShowCritHitCountColumn ?? false,
        ShowDirectHitCount = activeTab?.ShowDirectHitCountColumn ?? false,
        ShowCritDirectHitCount = activeTab?.ShowCritDirectHitCountColumn ?? false,
        ShowBlockPct = activeTab?.ShowBlockPctColumn ?? false,
        ShowParryPct = activeTab?.ShowParryPctColumn ?? false,
        ShowHealsTaken = activeTab?.ShowHealsTakenColumn ?? false,
        ShowAbsorbHeal = activeTab?.ShowAbsorbHealColumn ?? false,
        ShowKills = activeTab?.ShowKillsColumn ?? false,
        ShowInstantDps = activeTab?.ShowInstantDpsColumn ?? false,
        ShowInstantHps = activeTab?.ShowInstantHpsColumn ?? false,
        ShowCritHealPct = activeTab?.ShowCritHealPctColumn ?? false,
        ShowHealCount = activeTab?.ShowHealCountColumn ?? false,
        ShowCombatantDuration = activeTab?.ShowCombatantDurationColumn ?? false,
        ShowDamageShield = activeTab?.ShowDamageShieldColumn ?? false,
        ShowMaxHealWard = activeTab?.ShowMaxHealWardColumn ?? false,
        ShowPowerDrain = activeTab?.ShowPowerDrainColumn ?? false,
        ShowPowerHeal = activeTab?.ShowPowerHealColumn ?? false,
    };

    public bool IsVisible(BarColumn col) => col switch
    {
        BarColumn.Dps => ShowDps,
        BarColumn.Hps => ShowHps,
        BarColumn.Damage => ShowDamage,
        BarColumn.Healed => ShowHealed,
        BarColumn.DamagePercent => ShowDamagePercent,
        BarColumn.HealPercent => ShowHealPercent,
        BarColumn.DirectHit => ShowDirectHit,
        BarColumn.Crit => ShowCrit,
        BarColumn.CritDirectHit => ShowCritDirectHit,
        BarColumn.Deaths => ShowDeaths,
        BarColumn.DamageTaken => ShowDamageTaken,
        BarColumn.DamageTakenPercent => ShowDamageTakenPercent,
        BarColumn.Overheal => ShowOverheal,
        BarColumn.OverhealAmount => ShowOverhealAmount,
        BarColumn.MaxHit => ShowMaxHit,
        BarColumn.PeakDps => ShowPeakDps,
        BarColumn.MaxHeal => ShowMaxHeal,
        BarColumn.Swings => ShowSwings,
        BarColumn.Hits => ShowHits,
        BarColumn.Misses => ShowMisses,
        BarColumn.HitRate => ShowHitRate,
        BarColumn.CritHitCount => ShowCritHitCount,
        BarColumn.DirectHitCount => ShowDirectHitCount,
        BarColumn.CritDirectHitCount => ShowCritDirectHitCount,
        BarColumn.BlockPct => ShowBlockPct,
        BarColumn.ParryPct => ShowParryPct,
        BarColumn.HealsTaken => ShowHealsTaken,
        BarColumn.AbsorbHeal => ShowAbsorbHeal,
        BarColumn.Kills => ShowKills,
        BarColumn.InstantDps => ShowInstantDps,
        BarColumn.InstantHps => ShowInstantHps,
        BarColumn.CritHealPct => ShowCritHealPct,
        BarColumn.HealCount => ShowHealCount,
        BarColumn.CombatantDuration => ShowCombatantDuration,
        BarColumn.DamageShield => ShowDamageShield,
        BarColumn.MaxHealWard => ShowMaxHealWard,
        BarColumn.PowerDrain => ShowPowerDrain,
        BarColumn.PowerHeal => ShowPowerHeal,
        _ => false,
    };
}

public class CombatantBarComponent
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
        { BarColumn.MaxHit, "000.0K" },
        { BarColumn.PeakDps, "000.0K" },
        { BarColumn.MaxHeal, "000.0K" },
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
    };

    public static string GetColumnDisplayValue(CombatantEntry combatant, BarColumn col,
        Configuration config, MeterTab? activeTab) => col switch
    {
        BarColumn.Dps => ValueFormatter.FormatColumn(combatant.EncDps, config, BarColumn.Dps, activeTab),
        BarColumn.Hps => ValueFormatter.FormatColumn(combatant.EncHps, config, BarColumn.Hps, activeTab),
        BarColumn.Damage => ValueFormatter.FormatColumn(combatant.Damage, config, BarColumn.Damage, activeTab),
        BarColumn.Healed => ValueFormatter.FormatColumn(combatant.Healed, config, BarColumn.Healed, activeTab),
        BarColumn.DamagePercent => !string.IsNullOrEmpty(combatant.DamagePercent) ? combatant.DamagePercent : "0%",
        BarColumn.HealPercent => !string.IsNullOrEmpty(combatant.HealedPercent) ? combatant.HealedPercent : "0%",
        BarColumn.DirectHit => ValueFormatter.FormatPercentColumn(combatant.DirectHitPct, config, BarColumn.DirectHit, activeTab),
        BarColumn.Crit => ValueFormatter.FormatPercentColumn(combatant.CritPct, config, BarColumn.Crit, activeTab),
        BarColumn.CritDirectHit => ValueFormatter.FormatPercentColumn(combatant.CritDirectHitPct, config, BarColumn.CritDirectHit, activeTab),
        BarColumn.Deaths => $"{combatant.Deaths}",
        BarColumn.DamageTaken => ValueFormatter.FormatColumn(combatant.DamageTaken, config, BarColumn.DamageTaken, activeTab),
        BarColumn.DamageTakenPercent => !string.IsNullOrEmpty(combatant.DamageTakenPercent) ? combatant.DamageTakenPercent : "0%",
        BarColumn.Overheal => ValueFormatter.FormatPercentColumn(combatant.OverhealPct, config, BarColumn.Overheal, activeTab),
        BarColumn.OverhealAmount => ValueFormatter.FormatColumn(combatant.OverhealAmount, config, BarColumn.OverhealAmount, activeTab),
        BarColumn.MaxHit => ValueFormatter.FormatColumn(combatant.MaxHitDamage, config, BarColumn.MaxHit, activeTab),
        BarColumn.PeakDps => ValueFormatter.FormatColumn(combatant.PeakDps, config, BarColumn.PeakDps, activeTab),
        BarColumn.MaxHeal => ValueFormatter.FormatColumn(combatant.MaxHealAmount, config, BarColumn.MaxHeal, activeTab),
        BarColumn.Swings => $"{combatant.Swings}",
        BarColumn.Hits => $"{combatant.Hits}",
        BarColumn.Misses => $"{combatant.Misses}",
        BarColumn.HitRate => ValueFormatter.FormatPercentColumn(combatant.HitRate, config, BarColumn.HitRate, activeTab),
        BarColumn.CritHitCount => $"{combatant.CritHitCount}",
        BarColumn.DirectHitCount => $"{combatant.DirectHitCount}",
        BarColumn.CritDirectHitCount => $"{combatant.CritDirectHitCount}",
        BarColumn.BlockPct => ValueFormatter.FormatPercentColumn(combatant.BlockPct, config, BarColumn.BlockPct, activeTab),
        BarColumn.ParryPct => ValueFormatter.FormatPercentColumn(combatant.ParryPct, config, BarColumn.ParryPct, activeTab),
        BarColumn.HealsTaken => ValueFormatter.FormatColumn(combatant.HealsTaken, config, BarColumn.HealsTaken, activeTab),
        BarColumn.AbsorbHeal => ValueFormatter.FormatColumn(combatant.AbsorbHeal, config, BarColumn.AbsorbHeal, activeTab),
        BarColumn.Kills => $"{combatant.Kills}",
        BarColumn.InstantDps => ValueFormatter.FormatColumn(combatant.InstantDps, config, BarColumn.InstantDps, activeTab),
        BarColumn.InstantHps => ValueFormatter.FormatColumn(combatant.InstantHps, config, BarColumn.InstantHps, activeTab),
        BarColumn.CritHealPct => ValueFormatter.FormatPercentColumn(combatant.CritHealPct, config, BarColumn.CritHealPct, activeTab),
        BarColumn.HealCount => $"{combatant.HealCount}",
        BarColumn.CombatantDuration => combatant.CombatantDuration,
        BarColumn.DamageShield => ValueFormatter.FormatColumn(combatant.DamageShield, config, BarColumn.DamageShield, activeTab),
        BarColumn.MaxHealWard => ValueFormatter.FormatColumn(combatant.MaxHealWardAmount, config, BarColumn.MaxHealWard, activeTab),
        BarColumn.PowerDrain => ValueFormatter.FormatColumn(combatant.PowerDrain, config, BarColumn.PowerDrain, activeTab),
        BarColumn.PowerHeal => ValueFormatter.FormatColumn(combatant.PowerHeal, config, BarColumn.PowerHeal, activeTab),
        _ => string.Empty,
    };

    public bool Render(CombatantEntry combatant, double maxValue, int index, SortField sortBy, MeterTab? activeTab, string currentPlayerName = "")
    {
        var barHeight = config.BarHeight;
        var iconSize = config.IconSize;
        var value = GetSortValue(combatant, sortBy);
        var isLocalPlayer = combatant.IsLocalPlayer;

        var vis = ColumnVisibility.Resolve(config, activeTab);
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
        // Tooltip on hover
        if (config.ShowTooltip && config.TooltipFields.Count > 0 && ImGui.IsItemHovered())
        {
            DrawTooltip(combatant, activeTab);
        }
        var prevScale = ImGui.GetFont().Scale;
        ImGui.GetFont().Scale = config.GetFontScale(config.BarFontSize);
        ImGui.PushFont(ImGui.GetFont());

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
        var valColor = ImGui.ColorConvertFloat4ToU32(config.ValueTextColor);
        var colSpacing = config.BarColumnSpacing;

        var columnOrder = activeTab?.ColumnOrder ?? new List<BarColumn>();
        EnsureColumnOrderComplete(columnOrder);

        for (var ci = columnOrder.Count - 1; ci >= 0; ci--)
        {
            var col = columnOrder[ci];
            if (!vis.IsVisible(col)) continue;

            var text = GetColumnDisplayValue(combatant, col, config, activeTab);
            if (!ColumnWidthTemplates.TryGetValue(col, out var template)) continue;
            var colW = ImGui.CalcTextSize(template).X;
            rightX -= colW;
            drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(text).X) * 0.5f, textY), valColor, text);
            rightX -= colSpacing;
        }

        ImGui.GetFont().Scale = prevScale;
        ImGui.PopFont();

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
        var allCols = Enum.GetValues<BarColumn>();
        foreach (var col in allCols)
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

        var prevScale = ImGui.GetFont().Scale;
        ImGui.GetFont().Scale = config.GetFontScale(config.TooltipFontSize);
        ImGui.PushFont(ImGui.GetFont());

        var labelColor = config.TooltipLabelColor;
        var textColor = config.TooltipTextColor;

        foreach (var field in config.TooltipFields)
        {
            var (label, value) = GetTooltipFieldValue(combatant, field, activeTab);
            ImGui.TextColored(labelColor, label + ":");
            ImGui.SameLine();
            ImGui.TextColored(textColor, value);
        }

        ImGui.GetFont().Scale = prevScale;
        ImGui.PopFont();

        ImGui.EndTooltip();

        ImGui.PopStyleColor();
        ImGui.PopStyleVar(2);
    }

    private (string Label, string Value) GetTooltipFieldValue(CombatantEntry combatant, TooltipField field, MeterTab? activeTab) => field switch
    {
        TooltipField.Name => ("Name", NameFormatHelper.GetDisplayName(combatant.Name, combatant.Job, combatant.IsLocalPlayer, config)),
        TooltipField.Job => ("Job", !string.IsNullOrEmpty(combatant.Job) ? JobNameHelper.GetFullName(combatant.Job) : "—"),
        TooltipField.Dps => ("DPS", GetColumnDisplayValue(combatant, BarColumn.Dps, config, activeTab)),
        TooltipField.Hps => ("HPS", GetColumnDisplayValue(combatant, BarColumn.Hps, config, activeTab)),
        TooltipField.Damage => ("Damage", GetColumnDisplayValue(combatant, BarColumn.Damage, config, activeTab)),
        TooltipField.Healed => ("Healed", GetColumnDisplayValue(combatant, BarColumn.Healed, config, activeTab)),
        TooltipField.DamagePercent => ("Damage %", combatant.DamagePercent),
        TooltipField.HealPercent => ("Heal %", combatant.HealedPercent),
        TooltipField.Crit => ("Crit %", GetColumnDisplayValue(combatant, BarColumn.Crit, config, activeTab)),
        TooltipField.DirectHit => ("Direct Hit %", GetColumnDisplayValue(combatant, BarColumn.DirectHit, config, activeTab)),
        TooltipField.CritDirectHit => ("Crit DH %", GetColumnDisplayValue(combatant, BarColumn.CritDirectHit, config, activeTab)),
        TooltipField.Deaths => ("Deaths", $"{combatant.Deaths}"),
        TooltipField.DamageTaken => ("Damage Taken", GetColumnDisplayValue(combatant, BarColumn.DamageTaken, config, activeTab)),
        TooltipField.Overheal => ("Overheal %", GetColumnDisplayValue(combatant, BarColumn.Overheal, config, activeTab)),
        TooltipField.OverhealAmount => ("Overheal", GetColumnDisplayValue(combatant, BarColumn.OverhealAmount, config, activeTab)),
        TooltipField.MaxHit => ("Max Hit", $"{combatant.MaxHit} ({GetColumnDisplayValue(combatant, BarColumn.MaxHit, config, activeTab)})"),
        TooltipField.MaxHeal => ("Max Heal", $"{combatant.MaxHeal} ({GetColumnDisplayValue(combatant, BarColumn.MaxHeal, config, activeTab)})"),
        TooltipField.PeakDps => ("Peak DPS", GetColumnDisplayValue(combatant, BarColumn.PeakDps, config, activeTab)),
        TooltipField.Swings => ("Swings", $"{combatant.Swings}"),
        TooltipField.Hits => ("Hits", $"{combatant.Hits}"),
        TooltipField.Misses => ("Misses", $"{combatant.Misses}"),
        TooltipField.HitRate => ("Hit Rate", GetColumnDisplayValue(combatant, BarColumn.HitRate, config, activeTab)),
        TooltipField.Kills => ("Kills", $"{combatant.Kills}"),
        TooltipField.CombatantDuration => ("Duration", combatant.CombatantDuration),
        TooltipField.HealsTaken => ("Heals Taken", GetColumnDisplayValue(combatant, BarColumn.HealsTaken, config, activeTab)),
        TooltipField.InstantDps => ("Instant DPS", GetColumnDisplayValue(combatant, BarColumn.InstantDps, config, activeTab)),
        TooltipField.InstantHps => ("Instant HPS", GetColumnDisplayValue(combatant, BarColumn.InstantHps, config, activeTab)),
        TooltipField.CritHealPct => ("Crit Heal %", GetColumnDisplayValue(combatant, BarColumn.CritHealPct, config, activeTab)),
        TooltipField.HealCount => ("Heal Count", $"{combatant.HealCount}"),
        TooltipField.DamageShield => ("Damage Shield", GetColumnDisplayValue(combatant, BarColumn.DamageShield, config, activeTab)),
        TooltipField.MaxHealWard => ("Max Heal Ward", GetColumnDisplayValue(combatant, BarColumn.MaxHealWard, config, activeTab)),
        _ => ("", ""),
    };
}
