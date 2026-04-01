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
        ShowDps = activeTab?.ShowDpsOnBar ?? true,
        ShowHps = activeTab?.ShowHpsOnBar ?? false,
        ShowDamage = activeTab?.ShowDamageOnBar ?? false,
        ShowHealed = activeTab?.ShowHealedOnBar ?? false,
        ShowDamagePercent = activeTab?.ShowDamagePercentOnBar ?? false,
        ShowHealPercent = activeTab?.ShowHealPercentOnBar ?? false,
        ShowDirectHit = activeTab?.ShowDirectHitOnBar ?? false,
        ShowCrit = activeTab?.ShowCritOnBar ?? false,
        ShowCritDirectHit = activeTab?.ShowCritDirectHitOnBar ?? false,
        ShowDeaths = activeTab?.ShowDeathsOnBar ?? false,
        ShowDamageTaken = activeTab?.ShowDamageTakenOnBar ?? false,
        ShowDamageTakenPercent = activeTab?.ShowDamageTakenPercentOnBar ?? false,
        ShowOverheal = activeTab?.ShowOverhealOnBar ?? false,
        ShowOverhealAmount = activeTab?.ShowOverhealAmountOnBar ?? false,
        ShowMaxHit = activeTab?.ShowMaxHitOnBar ?? false,
        ShowPeakDps = activeTab?.ShowPeakDpsOnBar ?? false,
        ShowMaxHeal = activeTab?.ShowMaxHealOnBar ?? false,
        ShowSwings = activeTab?.ShowSwingsOnBar ?? false,
        ShowHits = activeTab?.ShowHitsOnBar ?? false,
        ShowMisses = activeTab?.ShowMissesOnBar ?? false,
        ShowHitRate = activeTab?.ShowHitRateOnBar ?? false,
        ShowCritHitCount = activeTab?.ShowCritHitCountOnBar ?? false,
        ShowDirectHitCount = activeTab?.ShowDirectHitCountOnBar ?? false,
        ShowCritDirectHitCount = activeTab?.ShowCritDirectHitCountOnBar ?? false,
        ShowBlockPct = activeTab?.ShowBlockPctOnBar ?? false,
        ShowParryPct = activeTab?.ShowParryPctOnBar ?? false,
        ShowHealsTaken = activeTab?.ShowHealsTakenOnBar ?? false,
        ShowAbsorbHeal = activeTab?.ShowAbsorbHealOnBar ?? false,
        ShowKills = activeTab?.ShowKillsOnBar ?? false,
        ShowInstantDps = activeTab?.ShowInstantDpsOnBar ?? false,
        ShowInstantHps = activeTab?.ShowInstantHpsOnBar ?? false,
        ShowCritHealPct = activeTab?.ShowCritHealPctOnBar ?? false,
        ShowHealCount = activeTab?.ShowHealCountOnBar ?? false,
        ShowCombatantDuration = activeTab?.ShowCombatantDurationOnBar ?? false,
        ShowDamageShield = activeTab?.ShowDamageShieldOnBar ?? false,
        ShowMaxHealWard = activeTab?.ShowMaxHealWardOnBar ?? false,
        ShowPowerDrain = activeTab?.ShowPowerDrainOnBar ?? false,
        ShowPowerHeal = activeTab?.ShowPowerHealOnBar ?? false,
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

    public bool Render(CombatantEntry combatant, double maxValue, int index, SortField sortBy, MeterTab? activeTab, string currentPlayerName = "")
    {
        var barHeight = config.BarHeight;
        var iconSize = config.IconSize;
        var value = GetSortValue(combatant, sortBy);
        var isLocalPlayer = !string.IsNullOrEmpty(currentPlayerName)
            && string.Equals(combatant.Name, currentPlayerName, StringComparison.OrdinalIgnoreCase);

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
            var displayName = isLocalPlayer && config.ShowYouOnBar ? "YOU" : combatant.Name;
            var fmt = isLocalPlayer ? config.SelfNameFormat : config.OthersNameFormat;
            displayName = FormatName(displayName, combatant.Job, fmt);
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
            switch (col)
            {
                case BarColumn.DamagePercent when vis.ShowDamagePercent:
                {
                    var pctStr = !string.IsNullOrEmpty(combatant.DamagePercent) ? combatant.DamagePercent : "0%";
                    var colW = ImGui.CalcTextSize("00.0%").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(pctStr).X) * 0.5f, textY), valColor, pctStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.HealPercent when vis.ShowHealPercent:
                {
                    var hpStr = !string.IsNullOrEmpty(combatant.HealedPercent) ? combatant.HealedPercent : "0%";
                    var colW = ImGui.CalcTextSize("00.0%").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(hpStr).X) * 0.5f, textY), valColor, hpStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.CritDirectHit when vis.ShowCritDirectHit:
                {
                    var cdhStr = ValueFormatter.FormatPercentColumn(combatant.CritDirectHitPct, config, BarColumn.CritDirectHit, activeTab);
                    var colW = ImGui.CalcTextSize("100%").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(cdhStr).X) * 0.5f, textY), valColor, cdhStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.Crit when vis.ShowCrit:
                {
                    var critStr = ValueFormatter.FormatPercentColumn(combatant.CritPct, config, BarColumn.Crit, activeTab);
                    var colW = ImGui.CalcTextSize("100%").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(critStr).X) * 0.5f, textY), valColor, critStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.DirectHit when vis.ShowDirectHit:
                {
                    var dhStr = ValueFormatter.FormatPercentColumn(combatant.DirectHitPct, config, BarColumn.DirectHit, activeTab);
                    var colW = ImGui.CalcTextSize("100%").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(dhStr).X) * 0.5f, textY), valColor, dhStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.Deaths when vis.ShowDeaths:
                {
                    var deathStr = $"{combatant.Deaths}";
                    var deathW = ImGui.CalcTextSize("00").X;
                    rightX -= deathW;
                    drawList.AddText(new Vector2(rightX + (deathW - ImGui.CalcTextSize(deathStr).X) * 0.5f, textY), valColor, deathStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.DamageTaken when vis.ShowDamageTaken:
                {
                    var takenStr = ValueFormatter.FormatColumn(combatant.DamageTaken, config, BarColumn.DamageTaken, activeTab);
                    var colW = ImGui.CalcTextSize("000.0K").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(takenStr).X) * 0.5f, textY), valColor, takenStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.DamageTakenPercent when vis.ShowDamageTakenPercent:
                {
                    var dtPctStr = !string.IsNullOrEmpty(combatant.DamageTakenPercent) ? combatant.DamageTakenPercent : "0%";
                    var colW = ImGui.CalcTextSize("00.0%").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(dtPctStr).X) * 0.5f, textY), valColor, dtPctStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.Overheal when vis.ShowOverheal:
                {
                    var ohStr = ValueFormatter.FormatPercentColumn(combatant.OverhealPct, config, BarColumn.Overheal, activeTab);
                    var colW = ImGui.CalcTextSize("100%").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(ohStr).X) * 0.5f, textY), valColor, ohStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.OverhealAmount when vis.ShowOverhealAmount:
                {
                    var ohaStr = ValueFormatter.FormatColumn(combatant.OverhealAmount, config, BarColumn.OverhealAmount, activeTab);
                    var colW = ImGui.CalcTextSize("000.0K").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(ohaStr).X) * 0.5f, textY), valColor, ohaStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.MaxHit when vis.ShowMaxHit:
                {
                    var mhStr = ValueFormatter.FormatColumn(combatant.MaxHitDamage, config, BarColumn.MaxHit, activeTab);
                    var colW = ImGui.CalcTextSize("000.0K").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(mhStr).X) * 0.5f, textY), valColor, mhStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.PeakDps when vis.ShowPeakDps:
                {
                    var pkStr = ValueFormatter.FormatColumn(combatant.PeakDps, config, BarColumn.PeakDps, activeTab);
                    var colW = ImGui.CalcTextSize("000.0K").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(pkStr).X) * 0.5f, textY), valColor, pkStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.MaxHeal when vis.ShowMaxHeal:
                {
                    var mhStr = ValueFormatter.FormatColumn(combatant.MaxHealAmount, config, BarColumn.MaxHeal, activeTab);
                    var colW = ImGui.CalcTextSize("000.0K").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(mhStr).X) * 0.5f, textY), valColor, mhStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.Swings when vis.ShowSwings:
                {
                    var swStr = $"{combatant.Swings}";
                    var colW = ImGui.CalcTextSize("0000").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(swStr).X) * 0.5f, textY), valColor, swStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.Hits when vis.ShowHits:
                {
                    var hitStr = $"{combatant.Hits}";
                    var colW = ImGui.CalcTextSize("0000").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(hitStr).X) * 0.5f, textY), valColor, hitStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.Misses when vis.ShowMisses:
                {
                    var missStr = $"{combatant.Misses}";
                    var colW = ImGui.CalcTextSize("0000").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(missStr).X) * 0.5f, textY), valColor, missStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.HitRate when vis.ShowHitRate:
                {
                    var hrStr = ValueFormatter.FormatPercentColumn(combatant.HitRate, config, BarColumn.HitRate, activeTab);
                    var colW = ImGui.CalcTextSize("100%").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(hrStr).X) * 0.5f, textY), valColor, hrStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.CritHitCount when vis.ShowCritHitCount:
                {
                    var chStr = $"{combatant.CritHitCount}";
                    var colW = ImGui.CalcTextSize("0000").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(chStr).X) * 0.5f, textY), valColor, chStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.DirectHitCount when vis.ShowDirectHitCount:
                {
                    var dhcStr = $"{combatant.DirectHitCount}";
                    var colW = ImGui.CalcTextSize("0000").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(dhcStr).X) * 0.5f, textY), valColor, dhcStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.CritDirectHitCount when vis.ShowCritDirectHitCount:
                {
                    var cdhcStr = $"{combatant.CritDirectHitCount}";
                    var colW = ImGui.CalcTextSize("0000").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(cdhcStr).X) * 0.5f, textY), valColor, cdhcStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.BlockPct when vis.ShowBlockPct:
                {
                    var blkStr = ValueFormatter.FormatPercentColumn(combatant.BlockPct, config, BarColumn.BlockPct, activeTab);
                    var colW = ImGui.CalcTextSize("100%").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(blkStr).X) * 0.5f, textY), valColor, blkStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.ParryPct when vis.ShowParryPct:
                {
                    var parStr = ValueFormatter.FormatPercentColumn(combatant.ParryPct, config, BarColumn.ParryPct, activeTab);
                    var colW = ImGui.CalcTextSize("100%").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(parStr).X) * 0.5f, textY), valColor, parStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.HealsTaken when vis.ShowHealsTaken:
                {
                    var htStr = ValueFormatter.FormatColumn(combatant.HealsTaken, config, BarColumn.HealsTaken, activeTab);
                    var colW = ImGui.CalcTextSize("000.0K").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(htStr).X) * 0.5f, textY), valColor, htStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.AbsorbHeal when vis.ShowAbsorbHeal:
                {
                    var absStr = ValueFormatter.FormatColumn(combatant.AbsorbHeal, config, BarColumn.AbsorbHeal, activeTab);
                    var colW = ImGui.CalcTextSize("000.0K").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(absStr).X) * 0.5f, textY), valColor, absStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.Kills when vis.ShowKills:
                {
                    var killStr = $"{combatant.Kills}";
                    var colW = ImGui.CalcTextSize("00").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(killStr).X) * 0.5f, textY), valColor, killStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.InstantDps when vis.ShowInstantDps:
                {
                    var idStr = ValueFormatter.FormatColumn(combatant.InstantDps, config, BarColumn.InstantDps, activeTab);
                    var colW = ImGui.CalcTextSize("000.0K").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(idStr).X) * 0.5f, textY), valColor, idStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.InstantHps when vis.ShowInstantHps:
                {
                    var ihStr = ValueFormatter.FormatColumn(combatant.InstantHps, config, BarColumn.InstantHps, activeTab);
                    var colW = ImGui.CalcTextSize("000.0K").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(ihStr).X) * 0.5f, textY), valColor, ihStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.CritHealPct when vis.ShowCritHealPct:
                {
                    var chpStr = ValueFormatter.FormatPercentColumn(combatant.CritHealPct, config, BarColumn.CritHealPct, activeTab);
                    var colW = ImGui.CalcTextSize("100%").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(chpStr).X) * 0.5f, textY), valColor, chpStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.HealCount when vis.ShowHealCount:
                {
                    var hcStr = $"{combatant.HealCount}";
                    var colW = ImGui.CalcTextSize("0000").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(hcStr).X) * 0.5f, textY), valColor, hcStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.CombatantDuration when vis.ShowCombatantDuration:
                {
                    var durStr = combatant.CombatantDuration;
                    var colW = ImGui.CalcTextSize("00:00").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(durStr).X) * 0.5f, textY), valColor, durStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.DamageShield when vis.ShowDamageShield:
                {
                    var dsStr = ValueFormatter.FormatColumn(combatant.DamageShield, config, BarColumn.DamageShield, activeTab);
                    var colW = ImGui.CalcTextSize("000.0K").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(dsStr).X) * 0.5f, textY), valColor, dsStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.MaxHealWard when vis.ShowMaxHealWard:
                {
                    var mhwStr = ValueFormatter.FormatColumn(combatant.MaxHealWardAmount, config, BarColumn.MaxHealWard, activeTab);
                    var colW = ImGui.CalcTextSize("000.0K").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(mhwStr).X) * 0.5f, textY), valColor, mhwStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.PowerDrain when vis.ShowPowerDrain:
                {
                    var pdStr = ValueFormatter.FormatColumn(combatant.PowerDrain, config, BarColumn.PowerDrain, activeTab);
                    var colW = ImGui.CalcTextSize("000.0K").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(pdStr).X) * 0.5f, textY), valColor, pdStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.PowerHeal when vis.ShowPowerHeal:
                {
                    var phStr = ValueFormatter.FormatColumn(combatant.PowerHeal, config, BarColumn.PowerHeal, activeTab);
                    var colW = ImGui.CalcTextSize("000.0K").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(phStr).X) * 0.5f, textY), valColor, phStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.Healed when vis.ShowHealed:
                {
                    var healStr = ValueFormatter.FormatColumn(combatant.Healed, config, BarColumn.Healed, activeTab);
                    var colW = ImGui.CalcTextSize("000.0K").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(healStr).X) * 0.5f, textY), valColor, healStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.Damage when vis.ShowDamage:
                {
                    var dmgStr = ValueFormatter.FormatColumn(combatant.Damage, config, BarColumn.Damage, activeTab);
                    var colW = ImGui.CalcTextSize("000.0K").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(dmgStr).X) * 0.5f, textY), valColor, dmgStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.Hps when vis.ShowHps:
                {
                    var hpsStr = ValueFormatter.FormatColumn(combatant.EncHps, config, BarColumn.Hps, activeTab);
                    var colW = ImGui.CalcTextSize("000.0K").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(hpsStr).X) * 0.5f, textY), valColor, hpsStr);
                    rightX -= colSpacing;
                    break;
                }
                case BarColumn.Dps when vis.ShowDps:
                {
                    var dpsStr = ValueFormatter.FormatColumn(combatant.EncDps, config, BarColumn.Dps, activeTab);
                    var colW = ImGui.CalcTextSize("000.0K").X;
                    rightX -= colW;
                    drawList.AddText(new Vector2(rightX + (colW - ImGui.CalcTextSize(dpsStr).X) * 0.5f, textY), valColor, dpsStr);
                    rightX -= colSpacing;
                    break;
                }
            }
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

    private static string FormatName(string name, string job, NameDisplayFormat fmt)
    {
        switch (fmt)
        {
            case NameDisplayFormat.FirstNameOnly:
            {
                var spaceIdx = name.IndexOf(' ');
                return spaceIdx > 0 ? name[..spaceIdx] : name;
            }
            case NameDisplayFormat.LastNameOnly:
            {
                var spaceIdx = name.LastIndexOf(' ');
                return spaceIdx >= 0 ? name[(spaceIdx + 1)..] : name;
            }
            case NameDisplayFormat.Initials:
            {
                var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return parts.Length >= 2
                    ? $"{parts[0][0]}. {parts[1][0]}."
                    : name;
            }
            case NameDisplayFormat.JobAbbreviation:
                return !string.IsNullOrEmpty(job) ? job.ToUpperInvariant() : name;
            case NameDisplayFormat.JobFullName:
                return !string.IsNullOrEmpty(job) ? JobNameHelper.GetFullName(job) : name;
            default:
                return name;
        }
    }
}
