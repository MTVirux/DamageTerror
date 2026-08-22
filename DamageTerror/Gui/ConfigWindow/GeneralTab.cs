namespace DamageTerror.Gui.ConfigWindow;

public sealed class GeneralTab
{
    private readonly DamageTerrorPlugin plugin;
    private string wsUrlBuffer;

    public GeneralTab(DamageTerrorPlugin plugin)
    {
        this.plugin = plugin;
        this.wsUrlBuffer = plugin.Config.WebSocketUrl;
    }

    public bool Draw(Configuration config)
    {
        var changed = false;

        if (ImGui.CollapsingHeader("Wizards", ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (ImGui.Button("Run setup wizard"))
                plugin.OpenSetupWizard();
            ConfigHelpers.HelpMarker("Walks through data source, theme preset, and core behavior again.\nNothing changes until you pick something.");
            if (ImGui.Button("Run customization wizard"))
                plugin.OpenCustomizationWizard();
            ConfigHelpers.HelpMarker("A quick pass over colors, icons, and markings.\nThe full set of options can be found under Appearance.");
            if (ImGui.Button("Run column wizard"))
                plugin.OpenColumnWizard();
            ConfigHelpers.HelpMarker("Pick which columns a meter tab shows and their order.\nPer-column extras live under Appearance.");
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Connection", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= ConfigHelpers.CheckboxProp("Prefer IPC (in-process, lowest latency)", config.PreferIpc, v => config.PreferIpc = v);

            ImGui.SetNextItemWidth(280);
            if (ImGui.InputText("WebSocket URL", ref wsUrlBuffer, 256))
            {
                config.WebSocketUrl = wsUrlBuffer;
                changed = true;
            }

            ImGui.TextDisabled($"Status: {plugin.DataService.ConnectionStatus}");

            if (ImGui.Button("Reconnect"))
            {
                Task.Run(async () => await plugin.DataService.ReconnectAsync().ConfigureAwait(false));
            }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Behavior", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= ConfigHelpers.CheckboxProp("Open meter on plugin start", config.ShowOnStart, v => config.ShowOnStart = v);

            changed |= ConfigHelpers.CheckboxProp("Hide when out of combat", config.HideOutOfCombat, v => config.HideOutOfCombat = v);

            if (config.HideOutOfCombat)
            {
                ImGui.Indent();
                changed |= ConfigHelpers.SliderFloatProp("Hide delay (seconds)", config.HideOutOfCombatDelay, 0f, 30f, "%.1f", v => config.HideOutOfCombatDelay = v, 150);
                ImGui.Unindent();
            }

            changed |= ConfigHelpers.CheckboxProp("Don't store 0 eDPS encounters", config.SkipZeroEdpsEncounters, v => config.SkipZeroEdpsEncounters = v);

            if (config.SkipZeroEdpsEncounters)
            {
                var zeroCount = plugin.DataService.Store.CountZeroEdpsEncounters();
                if (zeroCount > 0)
                {
                    ImGui.Indent();
                    ImGui.TextColored(new System.Numerics.Vector4(1f, 0.8f, 0.3f, 1f),
                        $"Found {zeroCount} encounter{(zeroCount != 1 ? "s" : "")} with 0 eDPS in history.");
                    ImGui.SameLine();
                    if (ImGui.Button($"Clean up##{zeroCount}"))
                    {
                        plugin.DataService.Store.RemoveZeroEdpsEncounters();
                    }
                    ImGui.Unindent();
                }
            }

            changed |= ConfigHelpers.CheckboxProp("Enable encounter replays", config.EnableReplays, v =>
            {
                config.EnableReplays = v;
                if (!v)
                    plugin.DataService.Store.StopActiveReplay();
            });
            ConfigHelpers.HelpMarker("Play a finished encounter back through the meter.\nWhen off, the Replay buttons and the Replay Bar layout entry are hidden.");

            changed |= ConfigHelpers.CheckboxProp("Ignore ESC key closing the meter", config.IgnoreEscClose, v => config.IgnoreEscClose = v);

            ImGui.Spacing();

            var dotCalcLabels = new[] { "DamageTerror (recommended)", "IINACT / ACT (no DoT Breakdown)" };
            changed |= ConfigHelpers.ComboProp("DoT calculation", (int)config.DotCalcMode, dotCalcLabels, v => config.DotCalcMode = (DotCalcMode)v, 280);
            ConfigHelpers.HelpMarker(
                "DamageTerror: distributes aggregated DoT ticks across active statuses using potency weights. (needed for dot skill breakdown)\n" +
                "IINACT / ACT: trusts the parser's own DoT simulation and attributes each tick to the named source as-is. (no DoT skill breakdown)");

            var endEncLabels = new[] { "/echo end (ACT + IINACT)", "/endenc (IINACT only) (Silent)" };
            changed |= ConfigHelpers.ComboProp("Encounter cut command", (int)config.EndEncounterMode, endEncLabels, v => config.EndEncounterMode = (EndEncounterMode)v, 280);
            ConfigHelpers.HelpMarker(
                "/echo end: sends a visible echo message that both ACT and IINACT recognize as an encounter split trigger.\n" +
                "/endenc: IINACT's built-in Dalamud command.\nSilent, but only works with IINACT.");

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            var comboNames = new[] { "Ctrl + Shift", "Ctrl + Alt", "Shift + Alt", "Ctrl", "Shift", "Alt" };
            changed |= ConfigHelpers.ComboProp("Modifier keys", (int)config.ModifierKeyCombo, comboNames, v => config.ModifierKeyCombo = (ModifierCombo)v, 150);
            ConfigHelpers.HelpMarker("Modifier key used by hidden layout elements and header reveal.");

            var modeNames = new[] { "Hold", "Toggle" };
            changed |= ConfigHelpers.ComboProp("Modifier mode", (int)config.ModifierKeyMode, modeNames, v => config.ModifierKeyMode = (ModifierMode)v, 150);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Hold: active only while keys are pressed.\nToggle: press once to activate, press again to deactivate.");


        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Position & Size"))
            changed |= PositionSizeSection.Draw(config);

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Duty Filters", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= ConfigHelpers.CheckboxProp("Overworld / Open World", config.EnableInOverworld, v => config.EnableInOverworld = v);
            changed |= ConfigHelpers.CheckboxProp("Dungeons", config.EnableInDungeons, v => config.EnableInDungeons = v);
            changed |= ConfigHelpers.CheckboxProp("Trials", config.EnableInTrials, v => config.EnableInTrials = v);
            changed |= ConfigHelpers.CheckboxProp("Raids (Savage / Ultimate)", config.EnableInRaids, v => config.EnableInRaids = v);
            changed |= ConfigHelpers.CheckboxProp("Alliance Raids", config.EnableInAllianceRaids, v => config.EnableInAllianceRaids = v);
            changed |= ConfigHelpers.CheckboxProp("Deep Dungeons (PotD / HoH / EO)", config.EnableInDeepDungeons, v => config.EnableInDeepDungeons = v);
            changed |= ConfigHelpers.CheckboxProp("Field Operations (Eureka / Bozja)", config.EnableInFieldOperations, v => config.EnableInFieldOperations = v);
            changed |= ConfigHelpers.CheckboxProp("Field Raids (Delubrum / Dalriada)", config.EnableInFieldRaids, v => config.EnableInFieldRaids = v);
            changed |= ConfigHelpers.CheckboxProp("Criterion Dungeons", config.EnableInCriterion, v => config.EnableInCriterion = v);
            changed |= ConfigHelpers.CheckboxProp("Variant Dungeons", config.EnableInVariant, v => config.EnableInVariant = v);
            changed |= ConfigHelpers.CheckboxProp("PvP", config.EnableInPvP, v => config.EnableInPvP = v);
        }

        return changed;
    }
}
