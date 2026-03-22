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
                changed |= DrawGeneralTab(config);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Customization"))
            {
                changed |= DrawCustomizationTab(config);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        if (changed)
        {
            plugin.SaveConfig();
        }
    }

    private bool DrawGeneralTab(Configuration config)
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

        return changed;
    }

    private bool DrawCustomizationTab(Configuration config)
    {
        var changed = false;

        // ===== Role Colors =====
        if (ImGui.CollapsingHeader("Role Colors", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= ColorEditProp("Tank", config.TankColor, v => config.TankColor = v);
            changed |= ColorEditProp("Healer", config.HealerColor, v => config.HealerColor = v);
            changed |= ColorEditProp("Melee DPS", config.MeleeDpsColor, v => config.MeleeDpsColor = v);
            changed |= ColorEditProp("Ranged DPS", config.RangedDpsColor, v => config.RangedDpsColor = v);
            changed |= ColorEditProp("Caster DPS", config.CasterDpsColor, v => config.CasterDpsColor = v);
            changed |= ColorEditProp("Unknown/Other", config.DefaultJobColor, v => config.DefaultJobColor = v);

            if (ImGui.Button("Reset Colors to Defaults"))
            {
                config.TankColor = new Vector4(0.2f, 0.4f, 0.8f, 1.0f);
                config.HealerColor = new Vector4(0.2f, 0.7f, 0.3f, 1.0f);
                config.MeleeDpsColor = new Vector4(0.8f, 0.2f, 0.2f, 1.0f);
                config.RangedDpsColor = new Vector4(0.9f, 0.5f, 0.2f, 1.0f);
                config.CasterDpsColor = new Vector4(0.6f, 0.3f, 0.8f, 1.0f);
                config.DefaultJobColor = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
                changed = true;
            }
        }

        ImGui.Spacing();

        // ===== Text Colors =====
        if (ImGui.CollapsingHeader("Text Colors", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= ColorEditProp("Name text", config.NameTextColor, v => config.NameTextColor = v);
            changed |= ColorEditProp("Value text", config.ValueTextColor, v => config.ValueTextColor = v);
            changed |= ColorEditProp("Bar background", config.BarBackgroundColor, v => config.BarBackgroundColor = v);

            if (ImGui.Button("Reset Text Colors"))
            {
                config.NameTextColor = new Vector4(1f, 1f, 1f, 1f);
                config.ValueTextColor = new Vector4(1f, 1f, 1f, 1f);
                config.BarBackgroundColor = new Vector4(0.15f, 0.15f, 0.15f, 1.0f);
                changed = true;
            }
        }

        ImGui.Spacing();

        // ===== Bar Appearance =====
        if (ImGui.CollapsingHeader("Bar Appearance", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var barHeight = config.BarHeight;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Bar height", ref barHeight, 14.0f, 40.0f, "%.0f px"))
            {
                config.BarHeight = barHeight;
                changed = true;
            }

            var barSpacing = config.BarSpacing;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Bar spacing", ref barSpacing, 0.0f, 8.0f, "%.0f px"))
            {
                config.BarSpacing = barSpacing;
                changed = true;
            }

            var barRounding = config.BarRounding;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Bar rounding", ref barRounding, 0.0f, 12.0f, "%.1f"))
            {
                config.BarRounding = barRounding;
                changed = true;
            }

            var iconSize = config.IconSize;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Icon size", ref iconSize, 10.0f, 32.0f, "%.0f px"))
            {
                config.IconSize = iconSize;
                changed = true;
            }
        }

        ImGui.Spacing();

        // ===== Bar Info =====
        if (ImGui.CollapsingHeader("Bar Content", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.TextDisabled("Choose what to display on each combatant bar.");

            var showName = config.ShowNameOnBar;
            if (ImGui.Checkbox("Player name", ref showName))
            {
                config.ShowNameOnBar = showName;
                changed = true;
            }

            var showValue = config.ShowValueOnBar;
            if (ImGui.Checkbox("DPS/HPS value", ref showValue))
            {
                config.ShowValueOnBar = showValue;
                changed = true;
            }

            var showPct = config.ShowDamagePercentOnBar;
            if (ImGui.Checkbox("Damage percent", ref showPct))
            {
                config.ShowDamagePercentOnBar = showPct;
                changed = true;
            }

            var showJob = config.ShowJobAbbrevOnBar;
            if (ImGui.Checkbox("Job abbreviation text", ref showJob))
            {
                config.ShowJobAbbrevOnBar = showJob;
                changed = true;
            }

            var showRank = config.ShowRankNumber;
            if (ImGui.Checkbox("Rank number (#1, #2...)", ref showRank))
            {
                config.ShowRankNumber = showRank;
                changed = true;
            }
        }

        ImGui.Spacing();

        // ===== Detail Panel =====
        if (ImGui.CollapsingHeader("Detail Panel"))
        {
            ImGui.TextDisabled("Choose what to show in the expanded detail view.");

            var showDmg = config.DetailShowDamage;
            if (ImGui.Checkbox("Total damage", ref showDmg))
            {
                config.DetailShowDamage = showDmg;
                changed = true;
            }

            var showCrit = config.DetailShowCritDhStats;
            if (ImGui.Checkbox("Crit / DH / CDH stats", ref showCrit))
            {
                config.DetailShowCritDhStats = showCrit;
                changed = true;
            }

            var showDeaths = config.DetailShowDeaths;
            if (ImGui.Checkbox("Deaths", ref showDeaths))
            {
                config.DetailShowDeaths = showDeaths;
                changed = true;
            }

            var showOh = config.DetailShowOverheal;
            if (ImGui.Checkbox("Overheal %", ref showOh))
            {
                config.DetailShowOverheal = showOh;
                changed = true;
            }

            var showMax = config.DetailShowMaxHit;
            if (ImGui.Checkbox("Max hit", ref showMax))
            {
                config.DetailShowMaxHit = showMax;
                changed = true;
            }

            var showTrend = config.DetailShowDpsTrend;
            if (ImGui.Checkbox("DPS trend (10s/30s/60s)", ref showTrend))
            {
                config.DetailShowDpsTrend = showTrend;
                changed = true;
            }
        }

        return changed;
    }

    private static bool ColorEditProp(string label, Vector4 color, Action<Vector4> setter)
    {
        var c = color;
        if (ImGui.ColorEdit4(label, ref c, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar))
        {
            setter(c);
            return true;
        }
        return false;
    }
}
