using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

public class GeneralTab
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

        if (ImGui.CollapsingHeader("Connection", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var preferIpc = config.PreferIpc;
            if (ImGui.Checkbox("Prefer IPC (in-process, lowest latency)", ref preferIpc))
            {
                config.PreferIpc = preferIpc;
                changed = true;
            }

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
            var showOnStart = config.ShowOnStart;
            if (ImGui.Checkbox("Open meter on plugin start", ref showOnStart))
            {
                config.ShowOnStart = showOnStart;
                changed = true;
            }

            var hideOoc = config.HideOutOfCombat;
            if (ImGui.Checkbox("Hide when out of combat", ref hideOoc))
            {
                config.HideOutOfCombat = hideOoc;
                changed = true;
            }

            if (config.HideOutOfCombat)
            {
                ImGui.Indent();
                var delay = config.HideOutOfCombatDelay;
                ImGui.SetNextItemWidth(150);
                if (ImGui.SliderFloat("Hide delay (seconds)", ref delay, 0f, 30f, "%.1f"))
                {
                    config.HideOutOfCombatDelay = delay;
                    changed = true;
                }
                ImGui.Unindent();
            }

            var skipZeroEdps = config.SkipZeroEdpsEncounters;
            if (ImGui.Checkbox("Don't store 0 eDPS encounters", ref skipZeroEdps))
            {
                config.SkipZeroEdpsEncounters = skipZeroEdps;
                changed = true;
            }

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

            var ignoreEsc = config.IgnoreEscClose;
            if (ImGui.Checkbox("Ignore ESC key closing the meter", ref ignoreEsc))
            {
                config.IgnoreEscClose = ignoreEsc;
                changed = true;
            }

            var hideHeader = config.HideWindowHeader;
            if (ImGui.Checkbox("Hide window header", ref hideHeader))
            {
                config.HideWindowHeader = hideHeader;
                changed = true;
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            var comboNames = new[] { "Ctrl + Shift", "Ctrl + Alt", "Shift + Alt", "Ctrl", "Shift", "Alt" };
            var comboIndex = (int)config.ModifierKeyCombo;
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo("Modifier keys", ref comboIndex, comboNames, comboNames.Length))
            {
                config.ModifierKeyCombo = (ModifierCombo)comboIndex;
                changed = true;
            }
            ConfigHelpers.HelpMarker("Modifier key used by hidden layout elements and header reveal.");

            var modeNames = new[] { "Hold", "Toggle" };
            var modeIndex = (int)config.ModifierKeyMode;
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo("Modifier mode", ref modeIndex, modeNames, modeNames.Length))
            {
                config.ModifierKeyMode = (ModifierMode)modeIndex;
                changed = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Hold: active only while keys are pressed.\nToggle: press once to activate, press again to deactivate.");


        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Duty Filters", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var v = config.EnableInOverworld;
            if (ImGui.Checkbox("Overworld / Open World", ref v)) { config.EnableInOverworld = v; changed = true; }
            v = config.EnableInDungeons;
            if (ImGui.Checkbox("Dungeons", ref v)) { config.EnableInDungeons = v; changed = true; }
            v = config.EnableInTrials;
            if (ImGui.Checkbox("Trials", ref v)) { config.EnableInTrials = v; changed = true; }
            v = config.EnableInRaids;
            if (ImGui.Checkbox("Raids (Savage / Ultimate)", ref v)) { config.EnableInRaids = v; changed = true; }
            v = config.EnableInAllianceRaids;
            if (ImGui.Checkbox("Alliance Raids", ref v)) { config.EnableInAllianceRaids = v; changed = true; }
            v = config.EnableInDeepDungeons;
            if (ImGui.Checkbox("Deep Dungeons (PotD / HoH / EO)", ref v)) { config.EnableInDeepDungeons = v; changed = true; }
            v = config.EnableInFieldOperations;
            if (ImGui.Checkbox("Field Operations (Eureka / Bozja)", ref v)) { config.EnableInFieldOperations = v; changed = true; }
            v = config.EnableInFieldRaids;
            if (ImGui.Checkbox("Field Raids (Delubrum / Dalriada)", ref v)) { config.EnableInFieldRaids = v; changed = true; }
            v = config.EnableInCriterion;
            if (ImGui.Checkbox("Criterion Dungeons", ref v)) { config.EnableInCriterion = v; changed = true; }
            v = config.EnableInVariant;
            if (ImGui.Checkbox("Variant Dungeons", ref v)) { config.EnableInVariant = v; changed = true; }
            v = config.EnableInPvP;
            if (ImGui.Checkbox("PvP", ref v)) { config.EnableInPvP = v; changed = true; }
        }

        return changed;
    }
}
