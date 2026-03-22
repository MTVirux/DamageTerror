using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

/// <summary>
/// Configuration window for the DamageTerror plugin.
/// </summary>
public class ConfigWindow : Window, IDisposable
{
    private readonly DamageTerrorPlugin plugin;
    private string wsUrlBuffer;

    public ConfigWindow(DamageTerrorPlugin plugin)
        : base("Damage Terror — Settings")
    {
        this.plugin = plugin;
        this.wsUrlBuffer = plugin.Config.WebSocketUrl;
        this.SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(380, 350),
            MaximumSize = new Vector2(800, 800),
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        var config = plugin.Config;
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

        // ===== Display =====
        if (ImGui.CollapsingHeader("Display", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var showHps = config.ShowHps;
            if (ImGui.Checkbox("Show HPS instead of DPS", ref showHps))
            {
                config.ShowHps = showHps;
                changed = true;
            }

            var showJobIcons = config.ShowJobIcons;
            if (ImGui.Checkbox("Show job icons", ref showJobIcons))
            {
                config.ShowJobIcons = showJobIcons;
                changed = true;
            }

            var showOnStart = config.ShowOnStart;
            if (ImGui.Checkbox("Open meter on plugin start", ref showOnStart))
            {
                config.ShowOnStart = showOnStart;
                changed = true;
            }

            // Bar alpha slider
            var barAlpha = config.BarAlpha;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Bar opacity", ref barAlpha, 0.1f, 1.0f, "%.2f"))
            {
                config.BarAlpha = barAlpha;
                changed = true;
            }
        }

        ImGui.Spacing();

        // ===== Sorting =====
        if (ImGui.CollapsingHeader("Sorting", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var sortOptions = Enum.GetNames(typeof(SortField));
            var currentSort = (int)config.SortBy;
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo("Sort by", ref currentSort, sortOptions, sortOptions.Length))
            {
                config.SortBy = (SortField)currentSort;
                changed = true;
            }

            var sortDesc = config.SortDescending;
            if (ImGui.Checkbox("Descending (highest first)", ref sortDesc))
            {
                config.SortDescending = sortDesc;
                changed = true;
            }
        }

        ImGui.Spacing();

        // ===== History =====
        if (ImGui.CollapsingHeader("History"))
        {
            var maxHistory = config.MaxEncounterHistory;
            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderInt("Max encounters", ref maxHistory, 5, 100))
            {
                config.MaxEncounterHistory = maxHistory;
                changed = true;
            }
        }

        if (changed)
        {
            plugin.SaveConfig();
        }
    }
}
