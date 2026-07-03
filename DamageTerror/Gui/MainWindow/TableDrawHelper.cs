using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.MainWindow;

/// <summary>
/// Shared helpers for the hand-rolled right-to-left, centered column tables used by the
/// bar/header/skill/buff renderers (which draw text directly to an <see cref="ImDrawListPtr"/>
/// rather than using ImGui tables).
/// </summary>
internal static class TableDrawHelper
{
    /// <summary>Draws <paramref name="text"/> horizontally centered within the column [x, x+colW] at row Y.</summary>
    public static void DrawCentered(ImDrawListPtr dl, float x, float colW, string text, uint color, float y)
        => dl.AddText(new Vector2(x + (colW - ImGui.CalcTextSize(text).X) * 0.5f, y), color, text);

    /// <summary>
    /// Right-to-left column draw: advances <paramref name="x"/> left by the column width, draws the
    /// centered text, then advances left by the inter-column padding.
    /// </summary>
    public static void DrawCenteredColRTL(ImDrawListPtr dl, ref float x, float colW, float colPad, string text, uint color, float y)
    {
        x -= colW;
        DrawCentered(dl, x, colW, text, color, y);
        x -= colPad;
    }

    /// <summary>
    /// Header variant of <see cref="DrawCenteredColRTL"/> that also runs a manual hit-test against the
    /// column rect and shows <paramref name="tooltip"/> via <see cref="ImGui.SetTooltip"/> when hovered.
    /// </summary>
    public static void DrawHeaderColRTL(ImDrawListPtr dl, ref float x, float colW, float colPad, string text, uint color, float y,
        Vector2 mousePos, float hitTop, float hitBottom, string tooltip)
    {
        x -= colW;
        DrawCentered(dl, x, colW, text, color, y);
        if (mousePos.X >= x && mousePos.X < x + colW && mousePos.Y >= hitTop && mousePos.Y < hitBottom)
            ImGui.SetTooltip(tooltip);
        x -= colPad;
    }
}
