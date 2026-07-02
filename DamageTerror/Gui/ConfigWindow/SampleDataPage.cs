using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

public sealed class SampleDataPage
{
    private readonly DamageTerrorPlugin plugin;

    private static readonly string[] PresetNames =
    {
        "8-Player Raid (Full Party)",
        "4-Player Dungeon",
        "24-Player Alliance Raid",
        "72-Player PvP (Frontline)",
        "200-Player Hunt Train",
        "9999-Player Stress Test",
    };

    private int selectedPreset;
    private bool simulateCombat;

    public SampleDataPage(DamageTerrorPlugin plugin)
    {
        this.plugin = plugin;
    }

    public void Draw()
    {
        var store = plugin.DataService.Store;
        var sampleLoaded = store.IsSampleDataActive;

        ImGui.TextUnformatted("Sample Data");
        ConfigHelpers.HelpMarker("Load a simulated encounter to preview and test your UI settings.\nSample data is temporary and will not be saved to history.");
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.SetNextItemWidth(250);
        ImGui.Combo("Preset", ref selectedPreset, PresetNames, PresetNames.Length);
        ImGui.Spacing();

        if (ImGui.Checkbox("Simulate active combat", ref simulateCombat))
        {
            if (sampleLoaded)
                store.SetSampleSimulation(simulateCombat);
        }
        ConfigHelpers.HelpMarker("When enabled, numbers will fluctuate in real-time like a live encounter.");
        ImGui.Spacing();

        if (ImGui.Button("Load Sample Encounter", new Vector2(220, 0)))
        {
            LoadSampleEncounter(selectedPreset);
        }

        if (sampleLoaded)
        {
            ImGui.SameLine();
            if (ImGui.Button("Clear Sample Data", new Vector2(180, 0)))
            {
                ClearSampleData();
            }
        }

        ImGui.Spacing();

        if (sampleLoaded)
        {
            var active = store.ActiveEncounter;
            var count = active?.Combatants.Count ?? 0;
            ImGui.TextColored(new Vector4(0.4f, 1.0f, 0.4f, 1.0f),
                $"Sample encounter loaded ({count} players). Enable simulation and check the main meter window.");
        }
    }

    private void LoadSampleEncounter(int preset)
    {
        var store = plugin.DataService.Store;

        if (preset == 5)
        {
            var (snapshot, factory) = SampleDataGenerator.CreateStressTest();
            store.LoadSampleData(snapshot, simulate: simulateCombat, combatantFactory: factory);
            return;
        }

        var regular = preset switch
        {
            1 => SampleDataGenerator.CreateDungeonParty(),
            2 => SampleDataGenerator.CreateAllianceRaid(),
            3 => SampleDataGenerator.CreateFrontline(),
            4 => SampleDataGenerator.CreateHuntTrain(),
            _ => SampleDataGenerator.CreateFullParty(),
        };

        store.LoadSampleData(regular, simulate: simulateCombat);
    }

    private void ClearSampleData()
    {
        plugin.DataService.Store.ClearSampleData();
    }
}
