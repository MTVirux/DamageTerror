using Dalamud.Interface.Windowing;
using Dalamud.Interface;
using Dalamud.Plugin.Services;
using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.MainWindow;

/// <summary>
/// The main damage meter window. Displays encounter header + sorted combatant bars.
/// </summary>
public class MainWindow : Window, IDisposable
{
    private static string GetTitleWithVersion()
    {
        try
        {
            var ver = typeof(DamageTerrorPlugin).Assembly.GetName().Version?.ToString() ?? string.Empty;

#if DEBUG
            var title = string.IsNullOrEmpty(ver)
                ? "Damage Terror [TESTING]"
                : $"Damage Terror  -  v{ver} [TESTING]";
#else
            var title = string.IsNullOrEmpty(ver)
                ? "Damage Terror"
                : $"Damage Terror  -  v{ver}";
#endif
            return title;
        }
        catch
        {
            return "Damage Terror";
        }
    }

    private readonly DamageTerrorPlugin plugin;
    private readonly ITextureProvider textureProvider;
    private readonly EncounterHeaderComponent headerComponent;
    private readonly CombatantBarComponent barComponent;
    private readonly CombatantDetailPanel detailPanel;
    private TitleBarButton? lockButton;

    public MainWindow(DamageTerrorPlugin plugin, ITextureProvider textureProvider)
        : base(GetTitleWithVersion())
    {
        this.plugin = plugin;
        this.textureProvider = textureProvider;
        this.SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(250, 150),
            MaximumSize = new Vector2(2000, 2000),
        };

        this.headerComponent = new EncounterHeaderComponent(plugin.DataService);
        this.barComponent = new CombatantBarComponent(plugin.Config, textureProvider);
        this.detailPanel = new CombatantDetailPanel(plugin.Config);

        // Settings button
        TitleBarButtons.Add(new TitleBarButton
        {
            Click = (m) => { if (m == ImGuiMouseButton.Left) plugin.OpenConfigUi(); },
            Icon = FontAwesomeIcon.Cog,
            IconOffset = new Vector2(2, 2),
            ShowTooltip = () => ImGui.SetTooltip("Open settings"),
        });

        // DPS/HPS toggle button
        TitleBarButtons.Add(new TitleBarButton
        {
            Click = (m) =>
            {
                if (m == ImGuiMouseButton.Left)
                {
                    plugin.Config.ShowHps = !plugin.Config.ShowHps;
                    plugin.SaveConfig();
                }
            },
            Icon = FontAwesomeIcon.Heartbeat,
            IconOffset = new Vector2(2, 2),
            ShowTooltip = () => ImGui.SetTooltip(plugin.Config.ShowHps ? "Showing HPS — click for DPS" : "Showing DPS — click for HPS"),
        });

        // Lock/pin button
        lockButton = new TitleBarButton
        {
            Icon = plugin.Config.PinMainWindow ? FontAwesomeIcon.Lock : FontAwesomeIcon.LockOpen,
            IconOffset = new Vector2(3, 2),
            ShowTooltip = () => ImGui.SetTooltip("Lock window position and size"),
        };
        lockButton.Click = (m) =>
        {
            if (m == ImGuiMouseButton.Left)
            {
                if (!plugin.Config.PinMainWindow)
                {
                    plugin.Config.MainWindowPos = ImGui.GetWindowPos();
                    plugin.Config.MainWindowSize = ImGui.GetWindowSize();
                }

                plugin.Config.PinMainWindow = !plugin.Config.PinMainWindow;
                plugin.SaveConfig();
                lockButton!.Icon = plugin.Config.PinMainWindow ? FontAwesomeIcon.Lock : FontAwesomeIcon.LockOpen;
            }
        };
        TitleBarButtons.Add(lockButton);
    }

    public void Dispose()
    {
    }

    public override void PreDraw()
    {
        if (this.plugin.Config.PinMainWindow)
        {
            Flags |= ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;

            var pos = this.plugin.Config.MainWindowPos;
            var size = this.plugin.Config.MainWindowSize;
            if (pos.X > 1f && pos.Y > 1f && size.X > 1f && size.Y > 1f)
            {
                ImGui.SetNextWindowPos(pos);
                ImGui.SetNextWindowSize(size);
            }
        }
        else
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
            Flags &= ~ImGuiWindowFlags.NoResize;
        }

        if (lockButton != null)
            lockButton.Icon = this.plugin.Config.PinMainWindow ? FontAwesomeIcon.Lock : FontAwesomeIcon.LockOpen;
    }

    public override void Draw()
    {
        // Encounter header with navigation
        headerComponent.Render();

        var encounter = headerComponent.SelectedEncounter;
        if (encounter == null)
        {
            ImGui.TextDisabled("No encounter data. Make sure IINACT is running.");
            if (ImGui.Button("Reconnect"))
            {
                Task.Run(async () => await plugin.DataService.ReconnectAsync().ConfigureAwait(false));
            }
            return;
        }

        // Get sorted combatants
        var combatants = GetSortedCombatants(encounter);
        if (combatants.Count == 0)
        {
            ImGui.TextDisabled("No combatant data.");
            return;
        }

        // Determine max value for bar scaling
        var showHps = plugin.Config.ShowHps;
        var maxVal = combatants.Max(c => showHps ? c.EncHps : c.EncDps);

        // Render bars in a scrollable child region
        if (ImGui.BeginChild("##combatants", new Vector2(0, 0), false))
        {
            for (int i = 0; i < combatants.Count; i++)
            {
                var combatant = combatants[i];
                if (barComponent.Render(combatant, maxVal, i))
                {
                    detailPanel.Toggle(i);
                }

                // Render expanded detail panel if this combatant is selected
                detailPanel.Render(combatant, i);
            }
        }
        ImGui.EndChild();
    }

    private List<CombatantEntry> GetSortedCombatants(EncounterSnapshot encounter)
    {
        var combatants = new List<CombatantEntry>(encounter.Combatants);
        var sortBy = plugin.Config.SortBy;
        var desc = plugin.Config.SortDescending;

        combatants.Sort((a, b) =>
        {
            var cmp = sortBy switch
            {
                SortField.EncDps => a.EncDps.CompareTo(b.EncDps),
                SortField.EncHps => a.EncHps.CompareTo(b.EncHps),
                SortField.Damage => a.Damage.CompareTo(b.Damage),
                SortField.Healed => a.Healed.CompareTo(b.Healed),
                SortField.CritPct => a.CritPct.CompareTo(b.CritPct),
                SortField.Deaths => a.Deaths.CompareTo(b.Deaths),
                _ => a.EncDps.CompareTo(b.EncDps),
            };
            return desc ? -cmp : cmp;
        });

        return combatants;
    }
}
