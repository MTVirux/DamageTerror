using ImGui = Dalamud.Bindings.ImGui.ImGui;
using Dalamud.Bindings.ImGui;

namespace DamageTerror.Gui.MainWindow;

/// <summary>
/// Renders a bottom status bar showing personal DPS, party rDPS, and combat duration.
/// </summary>
public class StatusBarComponent
{
    private readonly Configuration config;

    public StatusBarComponent(Configuration config)
    {
        this.config = config;
    }

    /// <summary>
    /// Returns the total height the status bar will occupy (including separator), or 0 if hidden.
    /// </summary>
    public float GetHeight()
    {
        if (!config.ShowStatusBar)
            return 0f;
        return config.StatusBarHeight + (config.ShowStatusBarSeparator ? 1f : 0f);
    }

    public void Render(EncounterSnapshot? encounter)
    {
        if (!config.ShowStatusBar || encounter == null)
            return;

        var windowWidth = ImGui.GetContentRegionAvail().X;
        var cursorPos = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();
        var height = config.StatusBarHeight;

        // Separator line above the status bar
        if (config.ShowStatusBarSeparator)
        {
            drawList.AddLine(
                cursorPos,
                new Vector2(cursorPos.X + windowWidth, cursorPos.Y),
                ImGui.ColorConvertFloat4ToU32(config.StatusBarSeparatorColor));
            cursorPos.Y += 1f;
        }

        // Background
        var bgColor = config.StatusBarBackgroundColor;
        if (bgColor.W > 0f)
        {
            drawList.AddRectFilled(
                cursorPos,
                new Vector2(cursorPos.X + windowWidth, cursorPos.Y + height),
                ImGui.ColorConvertFloat4ToU32(bgColor));
        }

        // Apply font scale
        var prevScale = ImGui.GetFont().Scale;
        ImGui.GetFont().Scale = config.StatusBarFontScale;
        ImGui.PushFont(ImGui.GetFont());

        // Color based on encounter active state
        var isActive = encounter.Encounter.IsActive;
        var textColor = ImGui.ColorConvertFloat4ToU32(isActive ? config.StatusBarActiveColor : config.StatusBarInactiveColor);
        var labelColor = ImGui.ColorConvertFloat4ToU32(config.StatusBarLabelColor);
        var textY = cursorPos.Y + (height - ImGui.GetTextLineHeight()) * 0.5f;
        var padding = 6f;

        // Find local player
        var localPlayer = encounter.Combatants.FirstOrDefault(c => c.IsLocalPlayer);
        var personalDps = localPlayer?.EncDps ?? 0.0;
        var raidDps = encounter.Encounter.EncDps;

        // Compute percentage of personal DPS vs raid DPS
        var pct = raidDps > 0 ? (personalDps / raidDps) * 100.0 : 0.0;

        // Layout: {DPS} DPS / {RDPS} RDPS ({pct}%)    [timer]
        var x = cursorPos.X + padding;

        // Personal DPS value
        var dpsText = FormatWithCommas(personalDps);
        drawList.AddText(new Vector2(x, textY), textColor, dpsText);
        x += ImGui.CalcTextSize(dpsText).X;

        // " DPS / "
        var sep1 = " DPS / ";
        drawList.AddText(new Vector2(x, textY), labelColor, sep1);
        x += ImGui.CalcTextSize(sep1).X;

        // Raid DPS value
        var rdpsText = FormatWithCommas(raidDps);
        drawList.AddText(new Vector2(x, textY), textColor, rdpsText);
        x += ImGui.CalcTextSize(rdpsText).X;

        // " RDPS (pct%)"
        var pctText = $" RDPS ({pct:F0}%)";
        drawList.AddText(new Vector2(x, textY), labelColor, pctText);

        // Combat timer — right-aligned
        if (config.ShowStatusBarTimer)
        {
            var timerText = encounter.Encounter.Duration;
            var timerWidth = ImGui.CalcTextSize(timerText).X;
            var rightX = cursorPos.X + windowWidth - padding - timerWidth;
            drawList.AddText(new Vector2(rightX, textY), textColor, timerText);
        }

        // Advance cursor past the status bar
        ImGui.SetCursorScreenPos(new Vector2(cursorPos.X, cursorPos.Y + height));

        // Restore font scale
        ImGui.GetFont().Scale = prevScale;
        ImGui.PopFont();
    }

    private static string FormatWithCommas(double value)
    {
        return ((long)Math.Round(value)).ToString("N0");
    }
}
