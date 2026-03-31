using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;
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

    public bool Render(CombatantEntry combatant, double maxValue, int index)
    {
        var barHeight = config.BarHeight;
        var iconSize = config.IconSize;
        var value = GetSortValue(combatant, config.SortBy);
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

        if (config.SelfBarHighlight && combatant.IsLocalPlayer)
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
            var iconId = JobIconHelper.GetIconId(combatant.Job);
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
            var displayName = combatant.IsLocalPlayer && config.ShowYouOnBar ? "YOU" : combatant.Name;
            var fmt = combatant.IsLocalPlayer ? config.SelfNameFormat : config.OthersNameFormat;
            displayName = FormatName(displayName, combatant.Job, fmt);
            var nameCol = (combatant.IsLocalPlayer && config.UseSelfNameColor)
                ? config.SelfNameColor
                : config.NameTextColor;
            var nameColor = ImGui.ColorConvertFloat4ToU32(nameCol);
            drawList.AddText(new Vector2(textStartX, textY), nameColor, displayName);
        }

        var rightX = cursorPos.X + windowWidth - config.BarRightPadding;
        var valColor = ImGui.ColorConvertFloat4ToU32(config.ValueTextColor);
        var colSpacing = config.BarColumnSpacing;

        if (config.ShowDamagePercentOnBar && !string.IsNullOrEmpty(combatant.DamagePercent))
        {
            var pctStr = combatant.DamagePercent;
            var colW = ImGui.CalcTextSize("00.0%").X;
            rightX -= colW;
            drawList.AddText(new Vector2(rightX + colW - ImGui.CalcTextSize(pctStr).X, textY), valColor, pctStr);
            rightX -= colSpacing;
        }

        if (config.ShowCritDirectHitOnBar)
        {
            var cdhStr = $"{combatant.CritDirectHitPct:F0}%";
            var colW = ImGui.CalcTextSize("100%").X;
            rightX -= colW;
            drawList.AddText(new Vector2(rightX + colW - ImGui.CalcTextSize(cdhStr).X, textY), valColor, cdhStr);
            rightX -= colSpacing;
        }

        if (config.ShowCritOnBar)
        {
            var critStr = $"{combatant.CritPct:F0}%";
            var colW = ImGui.CalcTextSize("100%").X;
            rightX -= colW;
            drawList.AddText(new Vector2(rightX + colW - ImGui.CalcTextSize(critStr).X, textY), valColor, critStr);
            rightX -= colSpacing;
        }

        if (config.ShowDirectHitOnBar)
        {
            var dhStr = $"{combatant.DirectHitPct:F0}%";
            var colW = ImGui.CalcTextSize("100%").X;
            rightX -= colW;
            drawList.AddText(new Vector2(rightX + colW - ImGui.CalcTextSize(dhStr).X, textY), valColor, dhStr);
            rightX -= colSpacing;
        }

        if (config.ShowValueOnBar)
        {
            var valueStr = ValueFormatter.Format(value, config.ValueDisplayFormat);
            var valueSize = ImGui.CalcTextSize(valueStr);
            rightX -= valueSize.X;
            drawList.AddText(new Vector2(rightX, textY), valColor, valueStr);
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
        _ => c.EncDps,
    };



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
