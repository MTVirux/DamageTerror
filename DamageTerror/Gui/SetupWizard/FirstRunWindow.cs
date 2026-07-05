using Dalamud.Interface.Windowing;

namespace DamageTerror.Gui.SetupWizard;

public sealed class FirstRunWindow : Window
{
    private const int StepCount = 4;

    private static readonly string[] StepTitles = { "Data source", "Pick a look", "Behavior", "All done" };

    private readonly DamageTerrorPlugin plugin;

    private int currentStep;
    private bool sampleLoadedByWizard;

    public FirstRunWindow(DamageTerrorPlugin plugin, PresetManager presetManager)
        : base("Damage Terror — Setup###DamageTerrorSetup")
    {
        this.plugin = plugin;
        this.Flags = ImGuiWindowFlags.NoCollapse;
        this.SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(470, 420),
            MaximumSize = new Vector2(620, 560),
        };
    }

    // The plugin can load at the title screen; hold the wizard until a
    // character is in so the meter preview it opens has somewhere to live.
    public override bool DrawConditions() => Svc.ClientState.IsLoggedIn;

    public void Restart()
    {
        currentStep = 0;
    }

    public override void OnClose()
    {
        CleanupOwnedSampleData();
        if (!plugin.Config.HasCompletedSetup)
        {
            plugin.Config.HasCompletedSetup = true;
            plugin.SaveConfig();
        }
    }

    // Public: plugin Dispose must call this too, since RemoveAllWindows
    // tears windows down without firing OnClose.
    public void CleanupOwnedSampleData()
    {
        if (!sampleLoadedByWizard) return;
        sampleLoadedByWizard = false;
        plugin.DataService.Store.ClearSampleData();
    }

    public override void Draw()
    {
        ImGui.TextDisabled($"Step {currentStep + 1} of {StepCount} — {StepTitles[currentStep]}");
        ImGui.Separator();
        ImGui.Spacing();

        var footer = ImGui.GetFrameHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.Y + 4f;
        if (ImGui.BeginChild("##wizardContent", new Vector2(0, -footer), false))
        {
            switch (currentStep)
            {
                case 0: DrawDataSourceStep(); break;
                case 1: DrawAppearanceStep(); break;
                case 2: DrawBehaviorStep(); break;
                case 3: DrawFinishStep(); break;
            }
        }
        ImGui.EndChild();

        ImGui.Separator();
        DrawNavRow();
    }

    private void DrawNavRow()
    {
        var scale = ImGui.GetIO().FontGlobalScale;
        var btnSize = new Vector2(80f * scale, 0);

        if (ImGui.Button("Skip", btnSize))
            IsOpen = false;
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Keep the defaults. You can re-run setup from Settings → General.");

        var navWidth = btnSize.X * 2 + ImGui.GetStyle().ItemSpacing.X;
        ImGui.SameLine(ImGui.GetWindowWidth() - navWidth - ImGui.GetStyle().WindowPadding.X);

        if (currentStep == 0) ImGui.BeginDisabled();
        if (ImGui.Button("Back", btnSize))
            GoToStep(currentStep - 1);
        if (currentStep == 0) ImGui.EndDisabled();

        ImGui.SameLine();
        if (currentStep < StepCount - 1)
        {
            if (ImGui.Button("Next", btnSize))
                GoToStep(currentStep + 1);
        }
        else if (ImGui.Button("Finish", btnSize))
        {
            IsOpen = false;
        }
    }

    private void GoToStep(int step) => currentStep = Math.Clamp(step, 0, StepCount - 1);

    private void DrawDataSourceStep() => ImGui.TextWrapped("Data source selection goes here.");

    private void DrawAppearanceStep() => ImGui.TextWrapped("Theme preset selection goes here.");

    private void DrawBehaviorStep() => ImGui.TextWrapped("Behavior toggles go here.");

    private void DrawFinishStep() => ImGui.TextWrapped("Recap goes here.");
}
