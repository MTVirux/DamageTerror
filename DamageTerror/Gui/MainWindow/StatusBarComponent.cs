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

    public void Render(EncounterSnapshot? encounter, string currentPlayerName = "")
    {
        if (!config.ShowStatusBar || encounter == null)
            return;

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
        var personalDps = localPlayer?.EncDps ?? 0.0;
        var raidDps = encounter.Encounter.EncDps;

        var pct = raidDps > 0 ? (personalDps / raidDps) * 100.0 : 0.0;

        var x = cursorPos.X + padding;

        if (config.ShowStatusBarPersonalDps)
        {
            var dpsText = ValueFormatter.Format(personalDps, config.ValueDisplayFormat);
            drawList.AddText(new Vector2(x, textY), textColor, dpsText);
            x += ImGui.CalcTextSize(dpsText).X;

            var dpsLabel = " DPS";
            drawList.AddText(new Vector2(x, textY), labelColor, dpsLabel);
            x += ImGui.CalcTextSize(dpsLabel).X;
        }

        if (config.ShowStatusBarPersonalDps && config.ShowStatusBarRaidDps)
        {
            var sep = " / ";
            drawList.AddText(new Vector2(x, textY), labelColor, sep);
            x += ImGui.CalcTextSize(sep).X;
        }

        if (config.ShowStatusBarRaidDps)
        {
            var rdpsText = ValueFormatter.Format(raidDps, config.ValueDisplayFormat);
            drawList.AddText(new Vector2(x, textY), textColor, rdpsText);
            x += ImGui.CalcTextSize(rdpsText).X;

            var pctText = $" RDPS ({pct:F0}%)";
            drawList.AddText(new Vector2(x, textY), labelColor, pctText);
        }

        if (config.ShowStatusBarTimer)
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
