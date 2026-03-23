using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

/// <summary>
/// General tab: connection, behavior, duty filters.
/// </summary>
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

        // ===== Connection =====
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

        // ===== Behavior =====
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

            var maxHistory = config.MaxEncounterHistory;
            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderInt("Max encounters kept in history", ref maxHistory, 5, 100))
            {
                config.MaxEncounterHistory = maxHistory;
                changed = true;
            }
        }

        ImGui.Spacing();

        // ===== Duty Filters =====
        if (ImGui.CollapsingHeader("Duty Filters", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.TextDisabled("Choose which content types show the meter.");

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
