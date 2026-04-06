using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;
using DamageTerror.Gui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.MainWindow;

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
        { BarColumn.RaidDps, "000.0K" },
        { BarColumn.RaidHps, "000.0K" },
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
        BarColumn.MaxHit => combatant.MaxHitSkillName,
        BarColumn.MaxHitValue => ValueFormatter.FormatColumn(combatant.MaxHitDamage, config, BarColumn.MaxHitValue, activeTab),
        BarColumn.PeakDps => ValueFormatter.FormatColumn(combatant.PeakDps, config, BarColumn.PeakDps, activeTab),
        BarColumn.MaxHeal => combatant.MaxHealSkillName,
        BarColumn.MaxHealValue => ValueFormatter.FormatColumn(combatant.MaxHealAmount, config, BarColumn.MaxHealValue, activeTab),
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
        BarColumn.LegsSweeped => $"{combatant.Stuns}",
        BarColumn.SkillIssue => $"{combatant.SkillIssue}",
        BarColumn.RaidDps => ValueFormatter.FormatColumn(combatant.RaidDps, config, BarColumn.RaidDps, activeTab),
        BarColumn.RaidHps => ValueFormatter.FormatColumn(combatant.RaidHps, config, BarColumn.RaidHps, activeTab),
        _ => string.Empty,
    };

    public bool Render(RenderContext ctx, CombatantEntry combatant, int index)
    {
        return Render(combatant, ctx.MaxValue, index, ctx.SortBy, ctx.ActiveTab, ctx.CurrentPlayerName);
    }

    public bool Render(CombatantEntry combatant, double maxValue, int index, SortField sortBy, MeterTab? activeTab, string currentPlayerName = "")
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
        var valColor = ImGui.ColorConvertFloat4ToU32(config.ValueTextColor);
        var colSpacing = config.BarColumnSpacing;

        var columnOrder = activeTab?.ColumnOrder ?? new List<BarColumn>();
        EnsureColumnOrderComplete(columnOrder);

        for (var ci = columnOrder.Count - 1; ci >= 0; ci--)
        {
            var col = columnOrder[ci];
            if (activeTab == null || !activeTab.IsColumnVisible(col)) continue;

            var text = GetColumnDisplayValue(combatant, col, config, activeTab);
            if (!ColumnWidthTemplates.TryGetValue(col, out var template)) continue;
            var colW = ImGui.CalcTextSize(template).X;
            rightX -= colW;
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

        using var tooltipFont = FontScope.Push(config.GetFontScale(config.TooltipFontSize));

        var labelColor = config.TooltipLabelColor;
        var textColor = config.TooltipTextColor;

        var tooltipFields = activeTab?.TooltipFields ?? config.TooltipFields;
        if (tooltipFields.Count == 0)
        {
            tooltipFont.Dispose();
            ImGui.EndTooltip();
            ImGui.PopStyleColor();
            ImGui.PopStyleVar(2);
            return;
        }

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
        TooltipField.MaxHit => ("Max Hit", combatant.MaxHitSkillName),
        TooltipField.MaxHitValue => ("Max Hit Value", GetColumnDisplayValue(combatant, BarColumn.MaxHitValue, config, activeTab)),
        TooltipField.MaxHeal => ("Max Heal", combatant.MaxHealSkillName),
        TooltipField.MaxHealValue => ("Max Heal Value", GetColumnDisplayValue(combatant, BarColumn.MaxHealValue, config, activeTab)),
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
        TooltipField.LegsSweeped => ("Legs Sweeped", $"{combatant.Stuns}"),
        TooltipField.SkillIssue => ("Skill Issue", $"{combatant.SkillIssue}"),
        TooltipField.RaidDps => ("Group DPS", GetColumnDisplayValue(combatant, BarColumn.RaidDps, config, activeTab)),
        TooltipField.RaidHps => ("Group HPS", GetColumnDisplayValue(combatant, BarColumn.RaidHps, config, activeTab)),
        TooltipField.TopDamageSkills => ("Top Damage Skills", ""),
        TooltipField.TopHealingSkills => ("Top Healing Skills", ""),
        _ => ("", ""),
    };

    private void DrawTopSkillsTooltipSection(CombatantEntry combatant, TooltipField field, MeterTab? activeTab, Vector4 labelColor, Vector4 textColor)
    {
        var isHealing = field == TooltipField.TopHealingSkills;
        var skills = isHealing ? combatant.HealingSkills : combatant.Skills;
        var count = activeTab?.TooltipTopSkillCount ?? config.TooltipTopSkillCount;
        var header = isHealing ? "Top Healing Skills" : "Top Damage Skills";

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
