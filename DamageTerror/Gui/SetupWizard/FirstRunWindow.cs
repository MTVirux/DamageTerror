using Dalamud.Interface.Windowing;

namespace DamageTerror.Gui.SetupWizard;

public sealed class FirstRunWindow : Window
{
    private const int StepCount = 6;

    private static readonly string[] StepTitles = { "Data source", "Pick a look", "Behavior", "Layout", "Position and size", "All done" };

    private readonly DamageTerrorPlugin plugin;
    private readonly PresetManager presetManager;

    private int currentStep;
    private bool sampleLoadedByWizard;
    private string wsUrlBuffer;
    private int selectedPresetIndex = -1;
    private bool modifierDemonstrated;
    private bool simulateInCombat = true;
    private DateTime? simOutOfCombatSince;

    public FirstRunWindow(DamageTerrorPlugin plugin, PresetManager presetManager)
        : base("Damage Terror - Setup###DamageTerrorSetup")
    {
        this.plugin = plugin;
        this.presetManager = presetManager;
        this.wsUrlBuffer = plugin.Config.WebSocketUrl;
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
        selectedPresetIndex = -1;
        modifierDemonstrated = false;
        wsUrlBuffer = plugin.Config.WebSocketUrl;
        simulateInCombat = true;
        simOutOfCombatSince = null;
    }

    public override void OnClose()
    {
        MeterWindowHelper.SimulatedCombat = null;
        MeterWindowHelper.PreviewReplayBar = false;
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

    // Handoff to the customization wizard: give up sample-data ownership so
    // this window's close doesn't clear the preview out from under it.
    public bool ReleaseSampleOwnership()
    {
        var owned = sampleLoadedByWizard;
        sampleLoadedByWizard = false;
        return owned;
    }

    // Handoff from another wizard: take over the sample data it loaded so it
    // survives that wizard's close and is cleaned up by this one.
    public void AdoptSampleOwnership() => sampleLoadedByWizard = true;

    public override void Draw()
    {
        // Held in-combat every frame so the preview meter stays visible while the
        // user pages through the wizard; only the behaviour step's simulator flips
        // it out of combat. OnClose restores real visibility.
        MeterWindowHelper.SimulatedCombat = true;

        // Show the demo replay bar in the preview only while arranging the layout,
        // and only when replays are on. OnClose clears it.
        MeterWindowHelper.PreviewReplayBar = currentStep == 3 && plugin.Config.EnableReplays;

        ImGui.TextDisabled($"Step {currentStep + 1} of {StepCount} - {StepTitles[currentStep]}");
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
                case 3: DrawLayoutStep(); break;
                case 4: DrawPositionStep(); break;
                case 5: DrawFinishStep(); break;
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

        var navWidth = btnSize.X * 2 + ImGui.GetStyle().ItemSpacing.X;
        ImGui.SetCursorPosX(ImGui.GetWindowWidth() - navWidth - ImGui.GetStyle().WindowPadding.X);

        var atFirstStep = currentStep == 0;
        if (atFirstStep) ImGui.BeginDisabled();
        if (ImGui.Button("Back", btnSize))
            GoToStep(currentStep - 1);
        if (atFirstStep) ImGui.EndDisabled();

        ImGui.SameLine();
        if (currentStep < StepCount - 1)
        {
            var onLayoutStep = currentStep == 3;
            var gated = onLayoutStep && !modifierDemonstrated;
            if (gated) ImGui.BeginDisabled();
            if (ImGui.Button("Next", btnSize))
                GoToStep(currentStep + 1);
            if (gated) ImGui.EndDisabled();
            if (gated && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip($"Press {MeterWindowHelper.ModifierComboName(plugin.Config.ModifierKeyCombo)} to continue.");
        }
        else if (ImGui.Button("Finish", btnSize))
        {
            IsOpen = false;
        }
    }

    private void GoToStep(int step)
    {
        currentStep = Math.Clamp(step, 0, StepCount - 1);
        // Reset the combat simulator to in-combat on every navigation so the
        // preview meter is showing when the user lands on the new page.
        simulateInCombat = true;
        simOutOfCombatSince = null;
        if (currentStep is 1 or 3 or 4)
            EnsurePreview();
    }

    private void DrawDataSourceStep()
    {
        var config = plugin.Config;

        ImGui.TextWrapped("Welcome to Damage Terror. This takes a minute, and everything here can be changed later in Settings.");
        ImGui.Spacing();
        ImGui.TextWrapped("First up: where should the combat data come from?");
        ImGui.Spacing();

        if (ImGui.RadioButton("IINACT plugin (recommended)", config.PreferIpc))
            SetDataSource(preferIpc: true);
        ImGui.Indent();
        ImGui.TextDisabled("Talks straight to the IINACT plugin. Nothing else to set up.");
        ImGui.Unindent();
        ImGui.Spacing();

        if (ImGui.RadioButton("WebSocket (ACT / external parser)", !config.PreferIpc))
            SetDataSource(preferIpc: false);
        ImGui.Indent();
        ImGui.TextDisabled("For ACT or another parser running outside the game.");
        if (!config.PreferIpc)
        {
            ImGui.SetNextItemWidth(280);
            if (ImGui.InputText("WebSocket URL", ref wsUrlBuffer, 256))
            {
                config.WebSocketUrl = wsUrlBuffer;
                plugin.SaveConfig();
            }
        }
        ImGui.Unindent();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var connected = plugin.DataService.IsConnected;
        var statusColor = connected ? new Vector4(0.4f, 1f, 0.4f, 1f) : new Vector4(1f, 0.8f, 0.3f, 1f);
        ImGui.TextColored(statusColor, $"Status: {plugin.DataService.ConnectionStatus}");
        if (ImGui.Button("Reconnect"))
            TriggerReconnect();
        if (!connected)
        {
            ImGui.Spacing();
            ImGui.TextWrapped("Not connected? Keep going anyway - you can sort this out later in Settings -> General.");
        }
    }

    private void SetDataSource(bool preferIpc)
    {
        if (plugin.Config.PreferIpc == preferIpc) return;
        plugin.Config.PreferIpc = preferIpc;
        plugin.SaveConfig();
        TriggerReconnect();
    }

    private void TriggerReconnect()
        => Task.Run(async () => await plugin.DataService.ReconnectAsync().ConfigureAwait(false));

    private void DrawAppearanceStep()
    {
        ImGui.TextWrapped("The meter next to this window is running on sample data. Click a preset to try it on - whatever you leave picked is what you keep.");
        ImGui.Spacing();

        var presets = presetManager.GetAllPresets().ToList();
        if (ImGui.BeginChild("##wizardPresetList", new Vector2(0, 0), true))
        {
            DrawPresetGroup(presets, builtIn: true);
            if (presets.Any(p => !p.IsBuiltIn))
            {
                ImGui.Spacing();
                ImGui.TextDisabled("Custom");
                DrawPresetGroup(presets, builtIn: false);
            }
        }
        ImGui.EndChild();
    }

    private void DrawPresetGroup(List<ThemePreset> presets, bool builtIn)
    {
        for (var i = 0; i < presets.Count; i++)
        {
            var preset = presets[i];
            if (preset.IsBuiltIn != builtIn) continue;

            if (ImGui.Selectable($"{preset.Name}##wizardPreset{i}", selectedPresetIndex == i))
            {
                selectedPresetIndex = i;
                preset.ApplyTo(plugin.Config);
                plugin.SaveConfig();
                EnsurePreview();
            }
            if (!string.IsNullOrEmpty(preset.Description) && ImGui.IsItemHovered())
                ImGui.SetTooltip(preset.Description);
        }
    }

    private void EnsurePreview()
    {
        plugin.OpenMainUi();
        var store = plugin.DataService.Store;
        if (!store.IsSampleDataActive)
        {
            store.LoadSampleData(SampleDataGenerator.CreateFullParty(), simulate: true);
            sampleLoadedByWizard = true;
        }
    }

    private void DrawLayoutStep()
    {
        var config = plugin.Config;

        ImGui.TextWrapped("Put the meter's parts in the order you want. Tick one to keep it hidden until you hold the reveal keys, which you can set below. The preview updates as you go.");
        ImGui.Spacing();

        if (LayoutPage.Draw(config))
            plugin.SaveConfig();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var shortcutChanged = false;
        var comboNames = new[] { "Ctrl + Shift", "Ctrl + Alt", "Shift + Alt", "Ctrl", "Shift", "Alt" };
        shortcutChanged |= ConfigHelpers.ComboProp("Reveal shortcut keys", (int)config.ModifierKeyCombo, comboNames, v => config.ModifierKeyCombo = (ModifierCombo)v, 150);

        var modeNames = new[] { "Hold", "Toggle" };
        shortcutChanged |= ConfigHelpers.ComboProp("Reveal shortcut mode", (int)config.ModifierKeyMode, modeNames, v => config.ModifierKeyMode = (ModifierMode)v, 150);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Hold: on only while the keys are held down.\nToggle: press once for on, press again for off.");

        if (shortcutChanged)
            plugin.SaveConfig();

        // Latch on the raw combo (not IsModifierActive) so we don't disturb the
        // meter's toggle-mode edge detection running in the same frame.
        if (MeterWindowHelper.IsModifierComboDown(config))
            modifierDemonstrated = true;
    }

    private void DrawPositionStep()
    {
        var config = plugin.Config;

        ImGui.TextWrapped("Put the meter where you want it. Drag it to move, drag its edges to resize, or use the controls below.");
        ImGui.Spacing();

        var changed = PositionSizeSection.Draw(config);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        changed |= ConfigHelpers.CheckboxProp("Lock meter position and size", config.PinMainWindow, v => config.PinMainWindow = v);
        ConfigHelpers.HelpMarker("Locked, the meter can't be moved or resized. The lock icon on its title bar does the same thing.");

        if (changed)
            plugin.SaveConfig();
    }

    private void DrawBehaviorStep()
    {
        var config = plugin.Config;

        ImGui.TextWrapped("A couple of choices about how the meter behaves. The rest is in Settings.");
        ImGui.Spacing();

        var changed = ConfigHelpers.CheckboxProp("Open meter on plugin start", config.ShowOnStart, v => config.ShowOnStart = v);
        ConfigHelpers.HelpMarker("Off means the meter stays hidden until you open it with /dt.");

        changed |= ConfigHelpers.CheckboxProp("Hide when out of combat", config.HideOutOfCombat, v => config.HideOutOfCombat = v);
        ConfigHelpers.HelpMarker("The meter fades out a few seconds after a fight ends and comes back for the next one.");

        if (config.HideOutOfCombat)
            changed |= DrawHideOutOfCombatDemo(config);

        changed |= ConfigHelpers.CheckboxProp("Enable encounter replays", config.EnableReplays, v =>
        {
            config.EnableReplays = v;
            if (!v)
                plugin.DataService.Store.StopActiveReplay();
        });
        ConfigHelpers.HelpMarker("Play a finished fight back through the meter. Off hides the Replay buttons and the Replay Bar.");

        if (changed)
            plugin.SaveConfig();
    }

    // Interactive demo of the out-of-combat hide behaviour: the delay slider is
    // the timer and the checkbox fakes combat state; the growing bar mirrors the
    // grace countdown. Feeding the toggle to MeterWindowHelper.SimulatedCombat
    // makes the live preview meter hide/show along with it rather than following
    // real encounters.
    private bool DrawHideOutOfCombatDemo(Configuration config)
    {
        ImGui.Indent();

        var changed = ConfigHelpers.SliderFloatProp("Hide delay (seconds)", config.HideOutOfCombatDelay, 0f, 30f, "%.1f", v => config.HideOutOfCombatDelay = v, 150);

        ImGui.Spacing();
        if (ImGui.Checkbox("Simulate: in combat", ref simulateInCombat) && simulateInCombat)
            simOutOfCombatSince = null;
        ConfigHelpers.HelpMarker("Untick to watch the countdown that hides the meter after a fight.");

        MeterWindowHelper.SimulatedCombat = simulateInCombat;

        var delay = config.HideOutOfCombatDelay;
        float fraction;
        string status;
        Vector4 barColor;

        if (simulateInCombat)
        {
            simOutOfCombatSince = null;
            fraction = 0f;
            status = "In a fight - meter visible";
            barColor = new Vector4(0.4f, 0.8f, 0.4f, 1f);
        }
        else
        {
            simOutOfCombatSince ??= DateTime.UtcNow;
            var elapsed = (float)(DateTime.UtcNow - simOutOfCombatSince.Value).TotalSeconds;
            fraction = delay > 0f ? Math.Clamp(elapsed / delay, 0f, 1f) : 1f;
            if (fraction < 1f)
            {
                status = $"Hiding in {Math.Max(0f, delay - elapsed):0.0}s";
                barColor = new Vector4(1f, 0.8f, 0.3f, 1f);
            }
            else
            {
                status = "Hidden";
                barColor = new Vector4(0.5f, 0.5f, 0.5f, 1f);
            }
        }

        ImGui.Spacing();
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, barColor);
        ImGui.ProgressBar(fraction, new Vector2(-1f, 0f), status);
        ImGui.PopStyleColor();

        ImGui.Unindent();
        return changed;
    }

    private void DrawFinishStep()
    {
        ImGui.TextWrapped("All done. A few things worth knowing:");
        ImGui.Spacing();

        ImGui.Bullet();
        ImGui.SameLine();
        ImGui.TextWrapped("/dt shows or hides the meter.");

        ImGui.Bullet();
        ImGui.SameLine();
        ImGui.TextWrapped("/dt config, or the cog on the meter's title bar, opens the settings.");

        ImGui.Bullet();
        ImGui.SameLine();
        ImGui.TextWrapped("You can run this setup again from Settings -> General.");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        WizardFinishNav.Draw(plugin, WizardFinishNav.WizardKind.Setup, ReleaseSampleOwnership, () => IsOpen = false);

        ImGui.Spacing();
        ImGui.TextWrapped("Closing this clears the sample data off the meter.");
    }
}
