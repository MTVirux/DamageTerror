using System.Diagnostics.CodeAnalysis;
using Dalamud.Interface.Windowing;

namespace DamageTerror.Gui.SetupWizard;

public sealed class ColumnWizardWindow : Window
{
    private const int StepCount = 6;

    private static readonly string[] StepTitles = { "Pick a tab", "Damage", "Healing", "More stats", "Arrange order", "All done" };

    private static readonly string[] MoreStatsCategories = { "Hit Stats", "Defense", "High-end Raiding", "Group", "Other" };

    private readonly DamageTerrorPlugin plugin;

    private int currentStep;
    private bool sampleLoadedByWizard;
    private bool previewPending;
    private int selectedTabIndex;

    public ColumnWizardWindow(DamageTerrorPlugin plugin)
        : base("Damage Terror - Columns###DamageTerrorColumns")
    {
        this.plugin = plugin;
        this.Flags = ImGuiWindowFlags.NoCollapse;
        this.SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(470, 420),
            MaximumSize = new Vector2(620, 560),
        };
    }

    // Same reason as the other wizards: the preview meter needs a logged-in
    // character to live beside.
    public override bool DrawConditions() => Svc.ClientState.IsLoggedIn;

    public void Restart()
    {
        currentStep = 0;
        selectedTabIndex = plugin.Config.SelectedMeterTab;
        previewPending = true;
    }

    // Handoff from another wizard: take over the sample data it loaded so it
    // survives that wizard's close and is cleaned up by this one.
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

    // The tab being edited, re-validated every frame in case tabs were
    // deleted or hidden while the wizard is open.
    private MeterTab? TargetTab
    {
        get
        {
            var tabs = plugin.Config.MeterTabs;
            if (tabs.Count == 0) return null;
            if (selectedTabIndex < 0 || selectedTabIndex >= tabs.Count || tabs[selectedTabIndex].IsHidden)
            {
                var firstVisible = tabs.FindIndex(t => !t.IsHidden);
                if (firstVisible < 0) return null;
                selectedTabIndex = firstVisible;
                plugin.SelectMeterTab(firstVisible);
            }
            return tabs[selectedTabIndex];
        }
    }

    public override void Draw()
    {
        // Held in-combat every frame so the preview meter stays visible while
        // the user pages through. OnClose restores real visibility.
        MeterWindowHelper.SimulatedCombat = true;

        // Deferred from Restart: Draw only runs while logged in, so the
        // preview meter can safely open here on the first frame. Force-load so
        // an adopted sample swaps to the 8-man party this wizard previews with.
        if (previewPending)
        {
            previewPending = false;
            EnsurePreview(forceReload: true);
        }

        ImGui.TextDisabled($"Step {currentStep + 1} of {StepCount} - {StepTitles[currentStep]}");
        ImGui.Separator();
        ImGui.Spacing();

        var footer = ImGui.GetFrameHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.Y + 4f;
        if (ImGui.BeginChild("##columnWizardContent", new Vector2(0, -footer), false))
        {
            switch (currentStep)
            {
                case 0: DrawTabPickStep(); break;
                case 1: DrawCategoryStep("Damage numbers - the meter's bread and butter.", "Damage"); break;
                case 2: DrawCategoryStep("Healing output and overheal.", "Healing"); break;
                case 3: DrawMoreStatsStep(); break;
                case 4: DrawOrderStep(); break;
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
            var gated = currentStep == 0 && TargetTab == null;
            if (gated) ImGui.BeginDisabled();
            if (ImGui.Button("Next", btnSize))
                GoToStep(currentStep + 1);
            if (gated) ImGui.EndDisabled();
        }
        else if (ImGui.Button("Finish", btnSize))
        {
            if (!plugin.Config.HasCompletedColumnWizard)
            {
                plugin.Config.HasCompletedColumnWizard = true;
                plugin.SaveConfig();
            }
            IsOpen = false;
        }
    }

    private void GoToStep(int step)
    {
        currentStep = Math.Clamp(step, 0, StepCount - 1);
        // Reloads the preview if something else (e.g. another wizard closing
        // while both were open) cleared the sample data.
        EnsurePreview();
    }

    private void EnsurePreview(bool forceReload = false)
    {
        plugin.OpenMainUi();
        var store = plugin.DataService.Store;
        if (forceReload || !store.IsSampleDataActive)
        {
            store.LoadSampleData(SampleDataGenerator.CreateFullParty(), simulate: true);
            sampleLoadedByWizard = true;
        }
    }

    private static void EnsureColumnOrder(MeterTab tab)
    {
        tab.ColumnOrder ??= new List<BarColumn>();
        CombatantBarComponent.EnsureColumnOrderComplete(tab.ColumnOrder);
    }

    // All tabs were hidden or deleted (e.g. via the config window) while the
    // wizard was open on a later step.
    private static bool NoTargetTab([NotNullWhen(false)] MeterTab? tab)
    {
        if (tab != null) return false;
        ImGui.TextWrapped("No visible meter tabs found. Tabs are managed under Settings -> Tabs.");
        return true;
    }

    private void DrawTabPickStep()
    {
        var tabs = plugin.Config.MeterTabs;
        var target = TargetTab;

        ImGui.TextWrapped("Pick the meter tab whose columns you want to set up. The preview meter switches to whichever tab you select - run this wizard again to do another tab.");
        ImGui.Spacing();

        if (target == null)
        {
            ImGui.TextWrapped("No visible meter tabs found. Tabs are managed under Settings -> Tabs.");
            return;
        }

        if (target.ViewMode == ViewMode.LineGraph)
        {
            ImGui.TextDisabled("This tab is in graph mode - columns apply to its bars view.");
            ImGui.Spacing();
        }

        if (ImGui.BeginChild("##columnWizardTabList", new Vector2(0, 0), true))
        {
            for (var i = 0; i < tabs.Count; i++)
            {
                if (tabs[i].IsHidden) continue;
                if (ImGui.Selectable($"{tabs[i].Name}##columnWizardTab{i}", selectedTabIndex == i))
                {
                    selectedTabIndex = i;
                    plugin.SelectMeterTab(i);
                    EnsureColumnOrder(tabs[i]);
                }
            }
        }
        ImGui.EndChild();
    }

    private void DrawCategoryStep(string intro, string categoryName)
    {
        var tab = TargetTab;
        if (NoTargetTab(tab)) return;
        EnsureColumnOrder(tab);

        ImGui.TextWrapped(intro);
        ImGui.Spacing();

        var changed = false;
        foreach (var (name, items) in MetricPicker.BarColumnCategories)
        {
            if (name != categoryName) continue;
            changed |= DrawColumnCheckboxes(tab, items);
        }

        if (changed)
            plugin.SaveConfig();
    }

    private void DrawMoreStatsStep()
    {
        var tab = TargetTab;
        if (NoTargetTab(tab)) return;
        EnsureColumnOrder(tab);

        ImGui.TextWrapped("Everything else - hit stats, defense, and group-wide numbers.");
        ImGui.Spacing();

        var changed = false;
        foreach (var (name, items) in MetricPicker.BarColumnCategories)
        {
            if (!MoreStatsCategories.Contains(name)) continue;
            ImGui.TextDisabled(name);
            changed |= DrawColumnCheckboxes(tab, items);
            ImGui.Spacing();
        }

        if (changed)
            plugin.SaveConfig();
    }

    private static bool DrawColumnCheckboxes(MeterTab tab, BarColumn[] items)
    {
        var changed = false;
        foreach (var col in items)
        {
            var visible = tab.IsColumnVisible(col);
            if (ImGui.Checkbox($"{MetricPicker.GetBarColumnLabel(col)}##columnWizard{col}", ref visible))
            {
                if (visible) tab.VisibleColumns.Add(col);
                else tab.VisibleColumns.Remove(col);
                changed = true;
            }

            var desc = MetricPicker.BarColumnDescriptions.GetValueOrDefault(col);
            if (!string.IsNullOrEmpty(desc) && ImGui.IsItemHovered())
                ImGui.SetTooltip(desc);
        }
        return changed;
    }

    private void DrawOrderStep()
    {
        var tab = TargetTab;
        if (NoTargetTab(tab)) return;
        EnsureColumnOrder(tab);

        ImGui.TextWrapped("Arrange the enabled columns - the top of this list is the leftmost column on the meter.");
        ImGui.Spacing();

        var enabled = tab.ColumnOrder.Where(tab.IsColumnVisible).ToList();
        if (enabled.Count == 0)
        {
            ImGui.TextDisabled("No columns enabled - go back and tick some first.");
            return;
        }

        var moved = false;
        for (var i = 0; i < enabled.Count; i++)
        {
            ImGui.PushID(i);
            moved |= ConfigHelpers.ReorderArrows(enabled, i);
            ImGui.Text(MetricPicker.GetBarColumnLabel(enabled[i]));
            ImGui.PopID();
        }

        if (moved)
        {
            // Same merge rule as the settings picker: enabled columns in the
            // chosen order first, disabled ones keep their trailing slots.
            var enabledSet = new HashSet<BarColumn>(enabled);
            var disabledOrder = tab.ColumnOrder.Where(c => !enabledSet.Contains(c)).ToList();
            tab.ColumnOrder.Clear();
            tab.ColumnOrder.AddRange(enabled);
            tab.ColumnOrder.AddRange(disabledOrder);
            plugin.SaveConfig();
        }
    }

    private void DrawFinishStep()
    {
        ImGui.TextWrapped("Columns set. Per-column extras - custom labels, formats, value colors, and widths - live under Settings -> Appearance.");
        ImGui.Spacing();
        ImGui.TextWrapped("Finishing clears the sample data from the meter.");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        WizardFinishNav.Draw(plugin, WizardFinishNav.WizardKind.Columns, ReleaseSampleOwnership, () => IsOpen = false);
    }
}
