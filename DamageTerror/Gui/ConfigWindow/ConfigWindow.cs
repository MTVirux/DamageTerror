using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

/// <summary>
/// Configuration window for the DamageTerror plugin.
/// Routes to per-tab components: General, Display, Appearance, History.
/// </summary>
public class ConfigWindow : Window, IDisposable
{
    private readonly DamageTerrorPlugin plugin;
    private readonly GeneralTab generalTab;
    private readonly DisplayTab displayTab;
    private readonly AppearanceTab appearanceTab;
    private readonly EncounterHistoryTab historyTab;

    public ConfigWindow(DamageTerrorPlugin plugin)
        : base("Damage Terror — Settings")
    {
        this.plugin = plugin;
        this.generalTab = new GeneralTab(plugin);
        this.displayTab = new DisplayTab();
        this.appearanceTab = new AppearanceTab();
        this.historyTab = new EncounterHistoryTab(plugin);
        this.SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(420, 450),
            MaximumSize = new Vector2(900, 900),
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        var config = plugin.Config;
        var changed = false;

        if (ImGui.BeginTabBar("##configTabs"))
        {
            if (ImGui.BeginTabItem("General"))
            {
                changed |= generalTab.Draw(config);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Display"))
            {
                changed |= displayTab.Draw(config);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Appearance"))
            {
                changed |= appearanceTab.Draw(config);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Encounter History"))
            {
                historyTab.Draw();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        if (changed)
        {
            plugin.SaveConfig();
        }
    }
}
