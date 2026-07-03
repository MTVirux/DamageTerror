namespace DamageTerror.Helpers;

public readonly struct StyleScope : IDisposable
{
    private readonly int colorCount;

    private StyleScope(int colorCount)
    {
        this.colorCount = colorCount;
    }

    public static StyleScope PushColor(ImGuiCol idx, Vector4 color)
    {
        ImGui.PushStyleColor(idx, color);
        return new StyleScope(1);
    }

    public static StyleScope PushColors(params ReadOnlySpan<(ImGuiCol Idx, Vector4 Color)> colors)
    {
        foreach (var (idx, color) in colors)
            ImGui.PushStyleColor(idx, color);
        return new StyleScope(colors.Length);
    }

    public void Dispose()
    {
        if (colorCount > 0)
            ImGui.PopStyleColor(colorCount);
    }
}
