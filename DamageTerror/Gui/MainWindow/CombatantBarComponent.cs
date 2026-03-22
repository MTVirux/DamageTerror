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
    private const float BarHeight = 22.0f;
    private const float IconSize = 16.0f;
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
        var showHps = config.ShowHps;
        var value = showHps ? combatant.EncHps : combatant.EncDps;
        var fraction = maxValue > 0 ? (float)(value / maxValue) : 0f;
        fraction = Math.Clamp(fraction, 0f, 1f);

        var windowWidth = ImGui.GetContentRegionAvail().X;
        var cursorPos = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();

        // Bar background (full width, darker)
        var barBgColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.15f, 0.15f, 0.15f, config.BarAlpha));
        drawList.AddRectFilled(
            cursorPos,
            new Vector2(cursorPos.X + windowWidth, cursorPos.Y + BarHeight),
            barBgColor);

        // Colored bar (proportional width)
        if (fraction > 0)
        {
            var barColor = JobColorHelper.GetBarColor(combatant.Job, config.BarAlpha);
            var barColorU32 = ImGui.ColorConvertFloat4ToU32(barColor);
            drawList.AddRectFilled(
                cursorPos,
                new Vector2(cursorPos.X + windowWidth * fraction, cursorPos.Y + BarHeight),
                barColorU32);
        }

        // Make the bar area interactive (invisible button for click detection)
        var clicked = ImGui.InvisibleButton($"##combatant_{index}", new Vector2(windowWidth, BarHeight));

        // Draw content on top of the bar (using overlay positions)
        var textY = cursorPos.Y + (BarHeight - ImGui.GetTextLineHeight()) * 0.5f;
        var textStartX = cursorPos.X + 4.0f;

        // Job icon
        if (config.ShowJobIcons)
        {
            var iconId = JobIconHelper.GetIconId(combatant.Job);
            if (iconId.HasValue)
            {
                var icon = textureProvider.GetFromGameIcon(new GameIconLookup(iconId.Value));
                if (icon.TryGetWrap(out var iconWrap, out _))
                {
                    var iconY = cursorPos.Y + (BarHeight - IconSize) * 0.5f;
                    drawList.AddImage(
                        iconWrap.Handle,
                        new Vector2(textStartX, iconY),
                        new Vector2(textStartX + IconSize, iconY + IconSize));
                    textStartX += IconSize + IconPadding;
                }
            }
        }

        // Player name
        var nameColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));
        drawList.AddText(new Vector2(textStartX, textY), nameColor, combatant.Name);

        // DPS/HPS value (right-aligned)
        var valueStr = FormatValue(value);
        var valueSize = ImGui.CalcTextSize(valueStr);
        var valueX = cursorPos.X + windowWidth - valueSize.X - 6.0f;
        drawList.AddText(new Vector2(valueX, textY), nameColor, valueStr);

        return clicked;
    }

    private static string FormatValue(double value)
    {
        if (value >= 1_000_000)
            return $"{value / 1_000_000:F2}M";
        if (value >= 10_000)
            return $"{value / 1_000:F1}K";
        return $"{value:F1}";
    }
}
