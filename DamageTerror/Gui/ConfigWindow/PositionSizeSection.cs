namespace DamageTerror.Gui.ConfigWindow;

// Body of the settings General tab's "Position & Size" section, shared with
// the setup wizard's position step.
public static class PositionSizeSection
{
    public static bool Draw(Configuration config)
    {
        var changed = false;

        if (config.PinMainWindow)
        {
            ImGui.TextColored(new Vector4(1f, 0.8f, 0.3f, 1f),
                "Window is locked. Using these controls will update its pinned position and size.");
            ImGui.Spacing();
        }

        ImGui.TextDisabled("Snap the meter window to a screen edge or corner.");
        ImGui.Spacing();

        var viewport = ImGui.GetMainViewport();
        var workPos = viewport.WorkPos;
        var workSize = viewport.WorkSize;
        var windowSize = config.MainWindowSize;
        var btnSize = new Vector2(80, 0);

        void Dock(float x, float y)
        {
            x = Math.Max(workPos.X, Math.Min(x, workPos.X + workSize.X - windowSize.X));
            y = Math.Max(workPos.Y, Math.Min(y, workPos.Y + workSize.Y - windowSize.Y));
            config.MainWindowPos = new Vector2(x, y);
            config.PinMainWindow = true;
            changed = true;
        }

        if (ImGui.Button("Top-Left", btnSize))
            Dock(workPos.X, workPos.Y);
        ImGui.SameLine();
        if (ImGui.Button("Top", btnSize))
            Dock(workPos.X + (workSize.X - windowSize.X) / 2f, workPos.Y);
        ImGui.SameLine();
        if (ImGui.Button("Top-Right", btnSize))
            Dock(workPos.X + workSize.X - windowSize.X, workPos.Y);

        if (ImGui.Button("Left", btnSize))
            Dock(workPos.X, workPos.Y + (workSize.Y - windowSize.Y) / 2f);
        ImGui.SameLine();
        ImGui.Dummy(btnSize);
        ImGui.SameLine();
        if (ImGui.Button("Right", btnSize))
            Dock(workPos.X + workSize.X - windowSize.X, workPos.Y + (workSize.Y - windowSize.Y) / 2f);

        if (ImGui.Button("Bot-Left", btnSize))
            Dock(workPos.X, workPos.Y + workSize.Y - windowSize.Y);
        ImGui.SameLine();
        if (ImGui.Button("Bottom", btnSize))
            Dock(workPos.X + (workSize.X - windowSize.X) / 2f, workPos.Y + workSize.Y - windowSize.Y);
        ImGui.SameLine();
        if (ImGui.Button("Bot-Right", btnSize))
            Dock(workPos.X + workSize.X - windowSize.X, workPos.Y + workSize.Y - windowSize.Y);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Sliders pin like the dock buttons do - while unpinned, the live
        // window overwrites this value again next frame.
        changed |= ConfigHelpers.SliderFloatProp("Width", config.MainWindowSize.X, 250f, workSize.X, "%.0f", v =>
        {
            config.MainWindowSize = new Vector2(v, config.MainWindowSize.Y);
            config.PinMainWindow = true;
        }, 200);
        changed |= ConfigHelpers.SliderFloatProp("Height", config.MainWindowSize.Y, 150f, workSize.Y, "%.0f", v =>
        {
            config.MainWindowSize = new Vector2(config.MainWindowSize.X, v);
            config.PinMainWindow = true;
        }, 200);

        return changed;
    }
}
