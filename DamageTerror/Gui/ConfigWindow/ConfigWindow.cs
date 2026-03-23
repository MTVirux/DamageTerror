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

    private static readonly string[] NameFormatLabels = new[]
    {
        "Full Name",
        "First Name Only",
        "Last Name Only",
        "Initials (F. L.)",
        "Job Abbreviation",
        "Job Full Name",
    };

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

            if (ImGui.BeginTabItem("Encounter History"))
            {
                DrawEncounterHistoryTab();
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

        // ===== Window =====
        if (ImGui.CollapsingHeader("Window", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var showHps = config.ShowHps;
            if (ImGui.Checkbox("Show HPS instead of DPS", ref showHps))
            {
                config.ShowHps = showHps;
                changed = true;
            }

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

        // ===== Damage / Heal Meter =====
        if (ImGui.CollapsingHeader("Damage / Heal Meter", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();

            // --- Selection Bar ---
            if (ImGui.TreeNodeEx("Selection Bar", ImGuiTreeNodeFlags.None))
            {
                var hideWhenPinned = config.HideSelectionBarWhenPinned;
                if (ImGui.Checkbox("Hide when window is pinned", ref hideWhenPinned))
                {
                    config.HideSelectionBarWhenPinned = hideWhenPinned;
                    changed = true;
                }

                if (config.HideSelectionBarWhenPinned)
                {
                    ImGui.Indent();
                    var showOnCtrlShift = config.SelectionBarShowOnCtrlShift;
                    if (ImGui.Checkbox("Show if Ctrl + Shift is held", ref showOnCtrlShift))
                    {
                        config.SelectionBarShowOnCtrlShift = showOnCtrlShift;
                        changed = true;
                    }
                    ImGui.Unindent();
                }

                ImGui.Spacing();

                var showSelBar = config.ShowSelectionBar;
                if (ImGui.Checkbox("Show selection bar", ref showSelBar))
                {
                    config.ShowSelectionBar = showSelBar;
                    changed = true;
                }

                var showEncPicker = config.ShowEncounterPicker;
                if (ImGui.Checkbox("Show encounter picker", ref showEncPicker))
                {
                    config.ShowEncounterPicker = showEncPicker;
                    changed = true;
                }

                var showSortDd = config.ShowSortDropdown;
                if (ImGui.Checkbox("Show sort dropdown", ref showSortDd))
                {
                    config.ShowSortDropdown = showSortDd;
                    changed = true;
                }

                ImGui.Spacing();
                changed |= ColorEditProp("Text color", config.SelectionBarTextColor, v => config.SelectionBarTextColor = v);
                changed |= ColorEditProp("Background color", config.SelectionBarBackgroundColor, v => config.SelectionBarBackgroundColor = v);

                var selBarHeight = config.SelectionBarHeight;
                ImGui.SetNextItemWidth(200);
                if (ImGui.SliderFloat("Extra padding", ref selBarHeight, 0.0f, 16.0f, "%.0f px"))
                {
                    config.SelectionBarHeight = selBarHeight;
                    changed = true;
                }

                var showSelSep = config.ShowSelectionBarSeparator;
                if (ImGui.Checkbox("Show separator line", ref showSelSep))
                {
                    config.ShowSelectionBarSeparator = showSelSep;
                    changed = true;
                }

                if (config.ShowSelectionBarSeparator)
                {
                    ImGui.Indent();
                    changed |= ColorEditProp("Separator color", config.SelectionBarSeparatorColor, v => config.SelectionBarSeparatorColor = v);
                    ImGui.Unindent();
                }

                if (ImGui.Button("Reset Selection Bar"))
                {
                    config.ShowSelectionBar = true;
                    config.SelectionBarTextColor = new Vector4(1f, 1f, 1f, 1f);
                    config.SelectionBarBackgroundColor = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
                    config.SelectionBarHeight = 0.0f;
                    config.ShowEncounterPicker = true;
                    config.ShowSortDropdown = true;
                    config.ShowSelectionBarSeparator = true;
                    config.SelectionBarSeparatorColor = new Vector4(0.4f, 0.4f, 0.4f, 0.5f);
                    config.HideSelectionBarWhenPinned = false;
                    config.SelectionBarShowOnCtrlShift = true;
                    changed = true;
                }

                ImGui.TreePop();
            }

            ImGui.Spacing();

            // --- Header Row ---
            if (ImGui.TreeNodeEx("Header Row", ImGuiTreeNodeFlags.None))
            {
                var showHeader = config.ShowMeterHeader;
                if (ImGui.Checkbox("Show header row", ref showHeader))
                {
                    config.ShowMeterHeader = showHeader;
                    changed = true;
                }

                changed |= ColorEditProp("Header text color", config.HeaderTextColor, v => config.HeaderTextColor = v);
                changed |= ColorEditProp("Header background", config.HeaderBackgroundColor, v => config.HeaderBackgroundColor = v);

                var headerHeight = config.HeaderHeight;
                ImGui.SetNextItemWidth(200);
                if (ImGui.SliderFloat("Header height", ref headerHeight, 14.0f, 40.0f, "%.0f px"))
                {
                    config.HeaderHeight = headerHeight;
                    changed = true;
                }

                var headerSep = config.HeaderSeparator;
                if (ImGui.Checkbox("Show separator line", ref headerSep))
                {
                    config.HeaderSeparator = headerSep;
                    changed = true;
                }

                if (config.HeaderSeparator)
                {
                    ImGui.Indent();
                    changed |= ColorEditProp("Separator color", config.HeaderSeparatorColor, v => config.HeaderSeparatorColor = v);
                    ImGui.Unindent();
                }

                if (ImGui.Button("Reset Header"))
                {
                    config.HeaderTextColor = new Vector4(0.7f, 0.7f, 0.7f, 0.9f);
                    config.HeaderBackgroundColor = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
                    config.HeaderHeight = 22.0f;
                    config.HeaderSeparator = false;
                    config.HeaderSeparatorColor = new Vector4(0.4f, 0.4f, 0.4f, 0.5f);
                    changed = true;
                }

                ImGui.TreePop();
            }

            ImGui.Spacing();

            // --- Meter Colors ---
            if (ImGui.TreeNodeEx("Meter Colors", ImGuiTreeNodeFlags.DefaultOpen))
            {
                changed |= ColorEditProp("Tank", config.TankColor, v => config.TankColor = v);
                changed |= ColorEditProp("Healer", config.HealerColor, v => config.HealerColor = v);
                changed |= ColorEditProp("Melee DPS", config.MeleeDpsColor, v => config.MeleeDpsColor = v);
                changed |= ColorEditProp("Ranged DPS", config.RangedDpsColor, v => config.RangedDpsColor = v);
                changed |= ColorEditProp("Caster DPS", config.CasterDpsColor, v => config.CasterDpsColor = v);
                changed |= ColorEditProp("Unknown/Other", config.DefaultJobColor, v => config.DefaultJobColor = v);

                ImGui.Spacing();

                changed |= ColorEditProp("Name text", config.NameTextColor, v => config.NameTextColor = v);
                changed |= ColorEditProp("Value text", config.ValueTextColor, v => config.ValueTextColor = v);
                changed |= ColorEditProp("Bar background", config.BarBackgroundColor, v => config.BarBackgroundColor = v);
                changed |= ColorEditProp("Window background", config.WindowBackgroundColor, v => config.WindowBackgroundColor = v);

                if (ImGui.Button("Reset All Colors"))
                {
                    config.TankColor = new Vector4(0.2f, 0.4f, 0.8f, 1.0f);
                    config.HealerColor = new Vector4(0.2f, 0.7f, 0.3f, 1.0f);
                    config.MeleeDpsColor = new Vector4(0.8f, 0.2f, 0.2f, 1.0f);
                    config.RangedDpsColor = new Vector4(0.9f, 0.5f, 0.2f, 1.0f);
                    config.CasterDpsColor = new Vector4(0.6f, 0.3f, 0.8f, 1.0f);
                    config.DefaultJobColor = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
                    config.NameTextColor = new Vector4(1f, 1f, 1f, 1f);
                    config.ValueTextColor = new Vector4(1f, 1f, 1f, 1f);
                    config.BarBackgroundColor = new Vector4(0.15f, 0.15f, 0.15f, 1.0f);
                    config.WindowBackgroundColor = new Vector4(0.06f, 0.06f, 0.06f, 0.94f);
                    changed = true;
                }

                ImGui.TreePop();
            }

            ImGui.Spacing();

            // --- Bar Appearance ---
            if (ImGui.TreeNodeEx("Bar Appearance", ImGuiTreeNodeFlags.DefaultOpen))
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

                ImGui.TreePop();
            }

            ImGui.Spacing();

            // --- Bar Content ---
            if (ImGui.TreeNodeEx("Bar Content", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.TextDisabled("Choose what to display on each combatant bar.");

                var showName = config.ShowNameOnBar;
                if (ImGui.Checkbox("Player name", ref showName))
                {
                    config.ShowNameOnBar = showName;
                    changed = true;
                }

                var showYou = config.ShowYouOnBar;
                if (ImGui.Checkbox("Show \"YOU\" instead of character name", ref showYou))
                {
                    config.ShowYouOnBar = showYou;
                    changed = true;
                }

                ImGui.Spacing();
                ImGui.TextDisabled("Name display format.");

                var selfFmt = (int)config.SelfNameFormat;
                if (ImGui.Combo("Your name", ref selfFmt, NameFormatLabels, NameFormatLabels.Length))
                {
                    config.SelfNameFormat = (NameDisplayFormat)selfFmt;
                    changed = true;
                }

                var othersFmt = (int)config.OthersNameFormat;
                if (ImGui.Combo("Others' names", ref othersFmt, NameFormatLabels, NameFormatLabels.Length))
                {
                    config.OthersNameFormat = (NameDisplayFormat)othersFmt;
                    changed = true;
                }

                ImGui.Spacing();

                var showValue = config.ShowValueOnBar;
                if (ImGui.Checkbox("DPS/HPS value", ref showValue))
                {
                    config.ShowValueOnBar = showValue;
                    changed = true;
                }

                var showPct = config.ShowDamagePercentOnBar;
                if (ImGui.Checkbox("DPS/HPS percent", ref showPct))
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
                if (ImGui.Checkbox("Rank number", ref showRank))
                {
                    config.ShowRankNumber = showRank;
                    changed = true;
                }

                var showJobIcons = config.ShowJobIcons;
                if (ImGui.Checkbox("Job icons", ref showJobIcons))
                {
                    config.ShowJobIcons = showJobIcons;
                    changed = true;
                }

                ImGui.Spacing();
                ImGui.TextDisabled("Hit stats displayed on each bar.");

                var showDh = config.ShowDirectHitOnBar;
                if (ImGui.Checkbox("! (Direct Hit %%)", ref showDh))
                {
                    config.ShowDirectHitOnBar = showDh;
                    changed = true;
                }

                var showCrit = config.ShowCritOnBar;
                if (ImGui.Checkbox("!! (Critical Hit %%)", ref showCrit))
                {
                    config.ShowCritOnBar = showCrit;
                    changed = true;
                }

                var showCdh = config.ShowCritDirectHitOnBar;
                if (ImGui.Checkbox("!!! (Crit Direct Hit %%)", ref showCdh))
                {
                    config.ShowCritDirectHitOnBar = showCdh;
                    changed = true;
                }

                ImGui.TreePop();
            }

            ImGui.Unindent();
        }

        ImGui.Spacing();

        // ===== Detail Panel =====
        if (ImGui.CollapsingHeader("Detail Panel"))
        {
            ImGui.Indent();
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

            ImGui.Unindent();
        }

        ImGui.Spacing();

        // ===== Skill Breakdown =====
        if (ImGui.CollapsingHeader("Skill Breakdown"))
        {
            ImGui.Indent();

            var showSkills = config.DetailShowSkillBreakdown;
            if (ImGui.Checkbox("Show skill breakdown", ref showSkills))
            {
                config.DetailShowSkillBreakdown = showSkills;
                changed = true;
            }

            if (config.DetailShowSkillBreakdown)
            {
                var maxSkills = config.MaxSkillBreakdownCount;
                ImGui.SetNextItemWidth(200);
                if (ImGui.SliderInt("Max skills shown (0 = all)", ref maxSkills, 0, 30))
                {
                    config.MaxSkillBreakdownCount = maxSkills;
                    changed = true;
                }
            }

            ImGui.Spacing();

            // --- Skill Colors ---
            if (ImGui.TreeNodeEx("Skill Colors", ImGuiTreeNodeFlags.None))
            {
                changed |= ColorEditProp("Damage fill", config.SkillDamageFillColor, v => config.SkillDamageFillColor = v);
                changed |= ColorEditProp("Healing fill", config.SkillHealingFillColor, v => config.SkillHealingFillColor = v);
                changed |= ColorEditProp("Row background", config.SkillRowBackgroundColor, v => config.SkillRowBackgroundColor = v);
                changed |= ColorEditProp("Skill text", config.SkillTextColor, v => config.SkillTextColor = v);
                changed |= ColorEditProp("Header text", config.SkillHeaderTextColor, v => config.SkillHeaderTextColor = v);

                if (ImGui.Button("Reset Skill Colors"))
                {
                    config.SkillDamageFillColor = new Vector4(0.35f, 0.35f, 0.55f, 0.7f);
                    config.SkillHealingFillColor = new Vector4(0.25f, 0.50f, 0.30f, 0.7f);
                    config.SkillRowBackgroundColor = new Vector4(0.12f, 0.12f, 0.12f, 0.6f);
                    config.SkillTextColor = new Vector4(1f, 1f, 1f, 0.9f);
                    config.SkillHeaderTextColor = new Vector4(0.6f, 0.6f, 0.6f, 0.9f);
                    changed = true;
                }

                ImGui.TreePop();
            }

            ImGui.Spacing();

            // --- Skill Appearance ---
            if (ImGui.TreeNodeEx("Skill Appearance", ImGuiTreeNodeFlags.None))
            {
                var skillRowHeight = config.SkillRowHeight;
                ImGui.SetNextItemWidth(200);
                if (ImGui.SliderFloat("Row height", ref skillRowHeight, 10.0f, 30.0f, "%.0f px"))
                {
                    config.SkillRowHeight = skillRowHeight;
                    changed = true;
                }

                var skillColPad = config.SkillColumnPadding;
                ImGui.SetNextItemWidth(200);
                if (ImGui.SliderFloat("Column padding", ref skillColPad, 0.0f, 16.0f, "%.0f px"))
                {
                    config.SkillColumnPadding = skillColPad;
                    changed = true;
                }

                ImGui.TreePop();
            }

            ImGui.Unindent();
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

    private void DrawEncounterHistoryTab()
    {
        var store = plugin.DataService.Store;
        var history = store.History;

        ImGui.TextDisabled($"Encounter history is saved automatically and persists across restarts.");
        ImGui.TextDisabled($"{history.Count} encounter(s) stored.");
        ImGui.Spacing();

        if (history.Count == 0)
        {
            ImGui.TextUnformatted("No encounters in history yet.");
            return;
        }

        // Clear all button
        if (ImGui.Button("Clear All History"))
        {
            ImGui.OpenPopup("##confirmClearHistory");
        }

        if (ImGui.BeginPopup("##confirmClearHistory"))
        {
            ImGui.TextUnformatted("Delete all encounter history?");
            if (ImGui.Button("Yes, clear all"))
            {
                store.Clear();
                store.Save(force: true);
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        int removeIdx = -1;

        for (int i = history.Count - 1; i >= 0; i--)
        {
            var enc = history[i];
            var encounter = enc.Encounter;
            var label = $"{encounter.ZoneName}";
            if (!string.IsNullOrEmpty(encounter.Title) && encounter.Title != encounter.ZoneName)
                label = $"{encounter.Title} — {encounter.ZoneName}";
            if (string.IsNullOrEmpty(label))
                label = "Unknown";

            var header = $"[{enc.Timestamp.ToLocalTime():yyyy-MM-dd HH:mm}]  {label}  ({encounter.Duration})";

            ImGui.PushID(i);
            if (ImGui.TreeNodeEx(header, ImGuiTreeNodeFlags.None))
            {
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "Duration:");
                ImGui.SameLine();
                ImGui.TextUnformatted(encounter.Duration);

                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "Raid DPS:");
                ImGui.SameLine();
                ImGui.TextUnformatted($"{encounter.EncDps:F1}");
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "  HPS:");
                ImGui.SameLine();
                ImGui.TextUnformatted($"{encounter.EncHps:F1}");

                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "Deaths:");
                ImGui.SameLine();
                ImGui.TextUnformatted($"{encounter.Deaths}");

                if (enc.Combatants.Count > 0)
                {
                    ImGui.Spacing();
                    ImGui.TextDisabled("Combatants:");
                    foreach (var c in enc.Combatants.OrderByDescending(c => c.EncDps))
                    {
                        var jobTag = !string.IsNullOrEmpty(c.Job) ? $"[{c.Job.ToUpperInvariant()}] " : "";
                        var cHeader = $"{jobTag}{c.Name}  —  DPS: {c.EncDps:F1}  HPS: {c.EncHps:F1}  Deaths: {c.Deaths}";

                        if (ImGui.TreeNodeEx(cHeader, ImGuiTreeNodeFlags.None))
                        {
                            if (c.Skills.Count > 0)
                            {
                                ImGui.TextColored(new Vector4(1f, 0.8f, 0.5f, 1f), "Damage Skills:");
                                ImGui.Indent(8f);
                                foreach (var s in c.Skills.OrderByDescending(s => s.TotalDamage))
                                {
                                    ImGui.TextUnformatted(
                                        $"{s.Name}  —  {s.TotalDamage:N0} ({s.DamagePercent:F1}%)  Hits: {s.HitCount}  C: {s.CritPct:F1}%  DH: {s.DirectHitPct:F1}%  CDH: {s.CritDirectHitPct:F1}%");
                                }
                                ImGui.Unindent(8f);
                            }

                            if (c.HealingSkills.Count > 0)
                            {
                                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), "Healing Skills:");
                                ImGui.Indent(8f);
                                foreach (var s in c.HealingSkills.OrderByDescending(s => s.TotalDamage))
                                {
                                    ImGui.TextUnformatted(
                                        $"{s.Name}  —  {s.TotalDamage:N0} ({s.DamagePercent:F1}%)  Hits: {s.HitCount}");
                                }
                                ImGui.Unindent(8f);
                            }

                            if (c.Skills.Count == 0 && c.HealingSkills.Count == 0)
                            {
                                ImGui.TextDisabled("No skill data recorded.");
                            }

                            ImGui.TreePop();
                        }
                    }
                }

                ImGui.Spacing();
                if (ImGui.SmallButton("Delete"))
                {
                    removeIdx = i;
                }

                ImGui.TreePop();
            }
            ImGui.PopID();
        }

        if (removeIdx >= 0)
        {
            store.RemoveHistory(removeIdx);
            store.Save(force: true);
        }
    }
}
