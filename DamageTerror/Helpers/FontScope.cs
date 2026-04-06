using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Helpers;

/// <summary>
/// Disposable helper that pushes a font scale and restores the previous value on dispose.
/// Usage: using var _ = FontScope.Push(config.GetFontScale(size));
/// Safe to call Dispose() explicitly before scope exit — subsequent calls are no-ops.
/// </summary>
public struct FontScope : IDisposable
{
    private readonly float previousScale;
    private bool disposed;

    private FontScope(float previousScale)
    {
        this.previousScale = previousScale;
        this.disposed = false;
    }

    public static FontScope Push(float newScale)
    {
        var prev = ImGui.GetFont().Scale;
        ImGui.GetFont().Scale = newScale;
        ImGui.PushFont(ImGui.GetFont());
        return new FontScope(prev);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        ImGui.GetFont().Scale = previousScale;
        ImGui.PopFont();
    }
}
