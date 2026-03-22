using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.MainWindow;

/// <summary>
/// Renders a single combatant bar row in the damage meter.
/// Shows: colored bar (proportional to top DPS), job icon, name, DPS/HPS value.
/// </summary>
public class CombatantBarComponent
{
    private const float IconPadding = 4.0f;

    private readonly Configuration config;
    private readonly ITextureProvider textureProvider;

    public CombatantBarComponent(Configuration config, ITextureProvider textureProvider)
    {
        this.config = config;
        this.textureProvider = textureProvider;
    }

    /// <summary>
    /// Render a combatant bar. Returns true if the bar is clicked (for detail expansion).
    /// </summary>
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

        // Bar background
        var bgColor = config.BarBackgroundColor;
        var barBgColor = ImGui.ColorConvertFloat4ToU32(bgColor);
        drawList.AddRectFilled(
            cursorPos,
            new Vector2(cursorPos.X + windowWidth, cursorPos.Y + barHeight),
            barBgColor,
            config.BarRounding);

        // Colored bar (proportional width)
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

        // Make the bar area interactive
        var clicked = ImGui.InvisibleButton($"##combatant_{index}", new Vector2(windowWidth, barHeight));

        // Draw content on top of the bar
        var textY = cursorPos.Y + (barHeight - ImGui.GetTextLineHeight()) * 0.5f;
        var textStartX = cursorPos.X + 4.0f;

        // Rank number
        if (config.ShowRankNumber)
        {
            var rankStr = $"{index + 1}. ";
            var rankColor = ImGui.ColorConvertFloat4ToU32(config.NameTextColor);
            drawList.AddText(new Vector2(textStartX, textY), rankColor, rankStr);
            textStartX += ImGui.CalcTextSize(rankStr).X;
        }

        // Job icon
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
                    textStartX += iconSize + IconPadding;
                }
            }
        }

        // Job abbreviation text (when icons are off, or alongside icons)
        if (config.ShowJobAbbrevOnBar && !string.IsNullOrEmpty(combatant.Job))
        {
            var jobStr = $"[{combatant.Job.ToUpperInvariant()}] ";
            var jobColor = ImGui.ColorConvertFloat4ToU32(config.NameTextColor);
            drawList.AddText(new Vector2(textStartX, textY), jobColor, jobStr);
            textStartX += ImGui.CalcTextSize(jobStr).X;
        }

        // Player name
        if (config.ShowNameOnBar)
        {
            var displayName = combatant.IsLocalPlayer && config.ShowYouOnBar ? "YOU" : combatant.Name;
            var fmt = combatant.IsLocalPlayer ? config.SelfNameFormat : config.OthersNameFormat;
            displayName = FormatName(displayName, combatant.Job, fmt);
            var nameColor = ImGui.ColorConvertFloat4ToU32(config.NameTextColor);
            drawList.AddText(new Vector2(textStartX, textY), nameColor, displayName);
        }

        // Right-side values
        var rightX = cursorPos.X + windowWidth - 6.0f;
        var valColor = ImGui.ColorConvertFloat4ToU32(config.ValueTextColor);

        // Damage percent (rightmost if shown alongside value)
        if (config.ShowDamagePercentOnBar && !string.IsNullOrEmpty(combatant.DamagePercent))
        {
            var pctStr = combatant.DamagePercent;
            var pctSize = ImGui.CalcTextSize(pctStr);
            rightX -= pctSize.X;
            drawList.AddText(new Vector2(rightX, textY), valColor, pctStr);
            rightX -= 8.0f; // spacing
        }

        // Crit/DH stats (right-aligned, before the value)
        if (config.ShowCritDirectHitOnBar)
        {
            var cdhStr = $"!!!{combatant.CritDirectHitPct:F0}%";
            var cdhSize = ImGui.CalcTextSize(cdhStr);
            rightX -= cdhSize.X;
            drawList.AddText(new Vector2(rightX, textY), valColor, cdhStr);
            rightX -= 6.0f;
        }

        if (config.ShowCritOnBar)
        {
            var critStr = $"!!{combatant.CritPct:F0}%";
            var critSize = ImGui.CalcTextSize(critStr);
            rightX -= critSize.X;
            drawList.AddText(new Vector2(rightX, textY), valColor, critStr);
            rightX -= 6.0f;
        }

        if (config.ShowDirectHitOnBar)
        {
            var dhStr = $"!{combatant.DirectHitPct:F0}%";
            var dhSize = ImGui.CalcTextSize(dhStr);
            rightX -= dhSize.X;
            drawList.AddText(new Vector2(rightX, textY), valColor, dhStr);
            rightX -= 6.0f;
        }

        // DPS/HPS value (right-aligned)
        if (config.ShowValueOnBar)
        {
            var valueStr = FormatValue(value);
            var valueSize = ImGui.CalcTextSize(valueStr);
            rightX -= valueSize.X;
            drawList.AddText(new Vector2(rightX, textY), valColor, valueStr);
        }

        // Bar spacing
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

    private static string FormatValue(double value)
    {
        if (value >= 1_000_000)
            return $"{value / 1_000_000:F2}M";
        if (value >= 10_000)
            return $"{value / 1_000:F1}K";
        return $"{value:F1}";
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
