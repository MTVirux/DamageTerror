using ImGui = Dalamud.Bindings.ImGui.ImGui;
using Dalamud.Bindings.ImGui;

namespace DamageTerror.Gui.MainWindow;

public class StatusBarComponent
{
    private readonly Configuration config;

    public StatusBarComponent(Configuration config)
    {
        this.config = config;
    }

    public float GetHeight()
    {
        if (!config.ShowStatusBar)
            return 0f;
        return config.StatusBarHeight + (config.ShowStatusBarSeparator ? 1f : 0f);
    }

    public void Render(EncounterSnapshot? encounter, string currentPlayerName = "", MeterTab? tab = null)
    {
        if (!config.ShowStatusBar || encounter == null)
            return;

        var showTimer = tab?.ShowStatusBarTimer ?? config.ShowStatusBarTimer;
        var metrics = tab?.StatusBarMetrics ?? config.StatusBarMetrics;

        var windowWidth = ImGui.GetContentRegionAvail().X;
        var cursorPos = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();
        var height = config.StatusBarHeight;

        if (config.ShowStatusBarSeparator)
        {
            drawList.AddLine(
                cursorPos,
                new Vector2(cursorPos.X + windowWidth, cursorPos.Y),
                ImGui.ColorConvertFloat4ToU32(config.StatusBarSeparatorColor));
            cursorPos.Y += 1f;
        }

        var bgColor = config.StatusBarBackgroundColor;
        if (bgColor.W > 0f)
        {
            drawList.AddRectFilled(
                cursorPos,
                new Vector2(cursorPos.X + windowWidth, cursorPos.Y + height),
                ImGui.ColorConvertFloat4ToU32(bgColor));
        }

        var prevScale = ImGui.GetFont().Scale;
        ImGui.GetFont().Scale = config.GetFontScale(config.StatusBarFontSize);
        ImGui.PushFont(ImGui.GetFont());

        var isActive = encounter.Encounter.IsActive;
        var textColor = ImGui.ColorConvertFloat4ToU32(isActive ? config.StatusBarActiveColor : config.StatusBarInactiveColor);
        var labelColor = ImGui.ColorConvertFloat4ToU32(config.StatusBarLabelColor);
        var textY = cursorPos.Y + (height - ImGui.GetTextLineHeight()) * 0.5f;
        var padding = config.StatusBarPadding;

        var localPlayer = !string.IsNullOrEmpty(currentPlayerName)
            ? encounter.Combatants.FirstOrDefault(c => string.Equals(c.Name, currentPlayerName, StringComparison.OrdinalIgnoreCase))
            : null;

        var x = cursorPos.X + padding;
        var hasLeftContent = false;

        if (metrics != null)
        {
            foreach (var col in metrics)
            {
                if (hasLeftContent)
                {
                    var sep = " / ";
                    drawList.AddText(new Vector2(x, textY), labelColor, sep);
                    x += ImGui.CalcTextSize(sep).X;
                }

                var valueText = localPlayer != null
                    ? CombatantBarComponent.GetColumnDisplayValue(localPlayer, col, config, tab)
                    : "0";
                drawList.AddText(new Vector2(x, textY), textColor, valueText);
                x += ImGui.CalcTextSize(valueText).X;

                var label = " " + Configuration.DefaultHeaderLabels.GetValueOrDefault(col, col.ToString());
                drawList.AddText(new Vector2(x, textY), labelColor, label);
                x += ImGui.CalcTextSize(label).X;
                hasLeftContent = true;
            }
        }

        // Timer (right-aligned)
        if (showTimer)
        {
            var timerText = encounter.Encounter.Duration;
            var timerWidth = ImGui.CalcTextSize(timerText).X;
            var rightX = cursorPos.X + windowWidth - padding - timerWidth;
            drawList.AddText(new Vector2(rightX, textY), textColor, timerText);
        }

        ImGui.SetCursorScreenPos(new Vector2(cursorPos.X, cursorPos.Y + height));

        ImGui.GetFont().Scale = prevScale;
        ImGui.PopFont();
    }
}
