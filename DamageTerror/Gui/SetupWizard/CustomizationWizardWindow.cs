using Dalamud.Interface.Windowing;

namespace DamageTerror.Gui.SetupWizard;

public sealed class CustomizationWizardWindow : Window
{
    private const int StepCount = 4;

    private static readonly string[] StepTitles = { "Colors", "Icons and Jobs", "Markings", "All done" };

    private readonly DamageTerrorPlugin plugin;

    private int currentStep;
    private bool sampleLoadedByWizard;
    private bool previewPending;

    public CustomizationWizardWindow(DamageTerrorPlugin plugin)
        : base("Damage Terror - Customization###DamageTerrorCustomization")
    {
        this.plugin = plugin;
        this.Flags = ImGuiWindowFlags.NoCollapse;
        this.SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(470, 420),
            MaximumSize = new Vector2(620, 560),
        };
    }

    // Same reason as FirstRunWindow: the preview meter needs a logged-in
    // character to live beside.
    public override bool DrawConditions() => Svc.ClientState.IsLoggedIn;

    public void Restart()
    {
        currentStep = 0;
        previewPending = true;
    }

    // Handoff from the first-run wizard: take over the sample data it loaded
    // so it survives that wizard's close and is cleaned up by this one.
    public void AdoptSampleOwnership() => sampleLoadedByWizard = true;

    // Handoff to another wizard: give up sample-data ownership so this
    // window's close doesn't clear the preview out from under it.
    public bool ReleaseSampleOwnership()
    {
        var owned = sampleLoadedByWizard;
        sampleLoadedByWizard = false;
        return owned;
    }

    public override void OnClose()
    {
        MeterWindowHelper.SimulatedCombat = null;
        CleanupOwnedSampleData();
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
        // Held in-combat every frame so the preview meter stays visible while
        // the user pages through. OnClose restores real visibility.
        MeterWindowHelper.SimulatedCombat = true;

        // Deferred from Restart: Draw only runs while logged in, so the
        // preview meter can safely open here on the first frame. Force-load so
        // an adopted sample (first-run's 8-player party) swaps to the 24-man.
        if (previewPending)
        {
            previewPending = false;
            EnsurePreview(forceReload: true);
        }

        ImGui.TextDisabled($"Step {currentStep + 1} of {StepCount} - {StepTitles[currentStep]}");
        ImGui.Separator();
        ImGui.Spacing();

        var footer = ImGui.GetFrameHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.Y + 4f;
        if (ImGui.BeginChild("##customizationContent", new Vector2(0, -footer), false))
        {
            switch (currentStep)
            {
                case 0: DrawColorsStep(); break;
                case 1: DrawIconsStep(); break;
                case 2: DrawMarkingsStep(); break;
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
            if (ImGui.Button("Next", btnSize))
                GoToStep(currentStep + 1);
        }
        else if (ImGui.Button("Finish", btnSize))
        {
            if (!plugin.Config.HasCompletedCustomizationWizard)
            {
                plugin.Config.HasCompletedCustomizationWizard = true;
                plugin.SaveConfig();
            }
            IsOpen = false;
        }
    }

    private void GoToStep(int step)
    {
        currentStep = Math.Clamp(step, 0, StepCount - 1);
        // Reloads the preview if something else (e.g. the first-run wizard
        // closing while both were open) cleared the sample data.
        EnsurePreview();
    }

    private void EnsurePreview(bool forceReload = false)
    {
        plugin.OpenMainUi();
        var store = plugin.DataService.Store;
        if (forceReload || !store.IsSampleDataActive)
        {
            store.LoadSampleData(SampleDataGenerator.CreateAllianceRaid(), simulate: true);
            sampleLoadedByWizard = true;
        }
    }

    private void DrawColorsStep()
    {
        var config = plugin.Config;

        ImGui.TextWrapped("Colors first - the preview meter beside this window updates as you pick.");
        ImGui.Spacing();

        var changed = ConfigHelpers.CheckboxProp("Use per-job colors", config.UsePerJobColors, v => config.UsePerJobColors = v);
        ConfigHelpers.HelpMarker("Off: one color per role. On: a color for every job.");
        ImGui.Spacing();

        if (!config.UsePerJobColors)
        {
            changed |= ConfigHelpers.ColorEditProp("Tank", config.TankColor, v => config.TankColor = v);
            changed |= ConfigHelpers.ColorEditProp("Healer", config.HealerColor, v => config.HealerColor = v);
            changed |= ConfigHelpers.ColorEditProp("Melee DPS", config.MeleeDpsColor, v => config.MeleeDpsColor = v);
            changed |= ConfigHelpers.ColorEditProp("Phys Ranged DPS", config.RangedDpsColor, v => config.RangedDpsColor = v);
            changed |= ConfigHelpers.ColorEditProp("Caster DPS", config.CasterDpsColor, v => config.CasterDpsColor = v);
            changed |= ConfigHelpers.ColorEditProp("DoH/DoL", config.DoHLColor, v => config.DoHLColor = v);
            changed |= ConfigHelpers.ColorEditProp("Limit Break", config.LimitBreakColor, v => config.LimitBreakColor = v);
            changed |= ConfigHelpers.ColorEditProp("Unknown/Other", config.DefaultJobColor, v => config.DefaultJobColor = v);
        }
        else
        {
            changed |= ConfigHelpers.DrawPerJobColorGroup("Tanks", JobRegistry.TankJobs, config);
            changed |= ConfigHelpers.DrawPerJobColorGroup("Healers", JobRegistry.HealerJobs, config);
            changed |= ConfigHelpers.DrawPerJobColorGroup("Melee DPS", JobRegistry.MeleeDpsJobs, config);
            changed |= ConfigHelpers.DrawPerJobColorGroup("Phys Ranged DPS", JobRegistry.RangedDpsJobs, config);
            changed |= ConfigHelpers.DrawPerJobColorGroup("Caster DPS", JobRegistry.CasterDpsJobs, config);
            changed |= ConfigHelpers.DrawPerJobColorGroup("DoH/DoL", JobRegistry.DoHLJobs, config);
            changed |= ConfigHelpers.DrawPerJobColorGroup("Base Classes", JobRegistry.BaseClassJobs, config);
            changed |= ConfigHelpers.ColorEditProp("Limit Break", config.LimitBreakColor, v => config.LimitBreakColor = v);
            changed |= ConfigHelpers.ColorEditProp("Unknown/Other", config.DefaultJobColor, v => config.DefaultJobColor = v);
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextDisabled("Text & background");
        changed |= ConfigHelpers.ColorEditProp("Name text", config.NameTextColor, v => config.NameTextColor = v);
        changed |= ConfigHelpers.ColorEditProp("Value text", config.ValueTextColor, v => config.ValueTextColor = v);
        changed |= ConfigHelpers.ColorEditProp("Bar background", config.BarBackgroundColor, v => config.BarBackgroundColor = v);
        changed |= ConfigHelpers.ColorEditProp("Window background", config.WindowBackgroundColor, v => config.WindowBackgroundColor = v);

        if (changed)
            plugin.SaveConfig();
    }

    private void DrawIconsStep()
    {
        var config = plugin.Config;

        ImGui.TextWrapped("How each combatant's job shows up on the meter bars.");
        ImGui.Spacing();

        var changed = ConfigHelpers.CheckboxProp("Show job icons", config.ShowJobIcons, v => config.ShowJobIcons = v);

        if (config.ShowJobIcons)
        {
            ImGui.Indent();
            var styleLabels = new[] { "Framed", "Plain", "Custom" };
            changed |= ConfigHelpers.ComboProp("Icon style", (int)config.JobIconStyle, styleLabels, v => config.JobIconStyle = (JobIconStyle)v, 120);
            changed |= ConfigHelpers.SliderFloatProp("Icon size", config.IconSize, 10.0f, 32.0f, "%.0f px", v => config.IconSize = v, 200);
            changed |= ConfigHelpers.SliderFloatProp("Icon-text padding", config.IconTextPadding, 0.0f, 12.0f, "%.0f px", v => config.IconTextPadding = v, 200);

            if (config.JobIconStyle == JobIconStyle.Custom)
                ImGui.TextDisabled("Per-job icon IDs live under Settings → Appearance → Meter Bars.");
            ImGui.Unindent();
        }

        ImGui.Spacing();
        changed |= ConfigHelpers.CheckboxProp("Job abbreviation before names", config.ShowJobAbbrevOnBar, v => config.ShowJobAbbrevOnBar = v);
        ConfigHelpers.HelpMarker("Shows the job tag, e.g. [WAR], in front of each name.");

        if (changed)
            plugin.SaveConfig();
    }

    private void DrawMarkingsStep()
    {
        var config = plugin.Config;

        ImGui.TextWrapped("Markings that call out your bar and the meter's rows.");
        ImGui.Spacing();

        var changed = ConfigHelpers.CheckboxProp("Highlight local player bar", config.SelfBarHighlight, v => config.SelfBarHighlight = v);
        if (config.SelfBarHighlight)
        {
            ImGui.Indent();
            changed |= ConfigHelpers.ColorEditProp("Accent color", config.SelfBarHighlightColor, v => config.SelfBarHighlightColor = v);
            ImGui.Unindent();
        }

        changed |= ConfigHelpers.CheckboxProp("Custom color for your name", config.UseSelfNameColor, v => config.UseSelfNameColor = v);
        if (config.UseSelfNameColor)
        {
            ImGui.Indent();
            changed |= ConfigHelpers.ColorEditProp("Name color", config.SelfNameColor, v => config.SelfNameColor = v);
            ImGui.Unindent();
        }

        changed |= ConfigHelpers.CheckboxProp("Rank number", config.ShowRankNumber, v => config.ShowRankNumber = v);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextDisabled("Header row");
        changed |= ConfigHelpers.CheckboxProp("Show header row", config.ShowMeterHeader, v => config.ShowMeterHeader = v);
        if (config.ShowMeterHeader)
        {
            ImGui.Indent();
            changed |= ConfigHelpers.CheckboxProp("Separator line", config.HeaderSeparator, v => config.HeaderSeparator = v);
            if (config.HeaderSeparator)
                changed |= ConfigHelpers.ColorEditProp("Separator color", config.HeaderSeparatorColor, v => config.HeaderSeparatorColor = v);
            ImGui.Unindent();
        }

        if (changed)
            plugin.SaveConfig();
    }

    private void DrawFinishStep()
    {
        ImGui.TextWrapped("That's the quick pass. Everything here - and much more - can be found under Settings → Appearance.");
        ImGui.Spacing();
        ImGui.TextWrapped("Finishing clears the sample data from the meter.");
    }
}
