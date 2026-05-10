using Dalamud.Bindings.ImGui;
using DamageTerror.Gui.ConfigWindow;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.MainWindow.Detail;

internal sealed class DetailsTabRenderer : IDetailTabRenderer
{
    private static readonly (string Name, BarColumn[] Columns)[] Sections = MetricPicker.BarColumnCategories;

    private readonly Configuration config;

    public DetailsTabRenderer(Configuration config)
    {
        this.config = config;
    }

    public void Render(in DetailRenderContext ctx)
    {
        var combatant = ctx.Combatant;
        var index = ctx.Index;
        var activeTab = ctx.ActiveTab;
        var vis = activeTab?.DetailVisibleColumns ?? config.DetailVisibleColumns;
        var lc = config.DetailLabelColor;

        ImGui.Spacing();

        if (!ImGui.BeginTabBar("##detailSections", ImGuiTabBarFlags.Reorderable))
            return;

        foreach (var (sectionName, defaultOrder) in Sections)
        {
            if (sectionName == "Group")
                continue;

            if (!HasAny(vis, defaultOrder))
                continue;

            if (!ImGui.BeginTabItem($"{sectionName}##detailSection"))
                continue;

            var order = GetSectionOrder(sectionName, defaultOrder, activeTab);
            DrawOrderedSection(order, combatant, vis, lc, activeTab);
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }

    private void DrawOrderedSection(List<BarColumn> order, CombatantEntry combatant, HashSet<BarColumn> vis, Vector4 lc, MeterTab? activeTab)
    {
        var newLineSet = activeTab?.DetailNewLineColumns ?? config.DetailNewLineColumns;
        var first = true;
        var regionMin = ImGui.GetCursorScreenPos().X;
        var availWidth = ImGui.GetContentRegionAvail().X;
        foreach (var col in order)
        {
            var data = GetDetailColumnData(col, combatant, vis, activeTab);
            if (data == null)
                continue;

            var (label, value) = data.Value;

            if (newLineSet.Contains(col) && !first)
                first = true;

            var colColor = activeTab?.GetColumnValueColor(col);

            var displayLabel = col == BarColumn.Deaths
                ? (activeTab != null ? activeTab.GetDetailColumnLabel(BarColumn.Deaths) : "Deaths")
                : label;

            if (!first)
            {
                var spacing = ImGui.GetStyle().ItemSpacing.X;
                var prefix = $"  {displayLabel}:";
                var prevEndX = ImGui.GetItemRectMax().X - regionMin;
                var itemWidth = spacing + ImGui.CalcTextSize(prefix).X + spacing + ImGui.CalcTextSize(value).X;

                if (prevEndX + itemWidth > availWidth)
                {
                    ImGui.TextColored(lc, $"{displayLabel}:");
                }
                else
                {
                    ImGui.SameLine();
                    ImGui.TextColored(lc, prefix);
                }
            }
            else
            {
                ImGui.TextColored(lc, $"{displayLabel}:");
                first = false;
            }

            ImGui.SameLine();
            if (colColor.HasValue)
                ImGui.TextColored(colColor.Value, value);
            else
                ImGui.TextUnformatted(value);
        }
    }

    private (string label, string value)? GetDetailColumnData(BarColumn col, CombatantEntry c, HashSet<BarColumn> vis, MeterTab? activeTab)
    {
        if (!vis.Contains(col))
            return null;

        string Label(BarColumn bc) => activeTab != null ? activeTab.GetDetailColumnLabel(bc)
            : Configuration.DefaultDetailColumnLabels.GetValueOrDefault(bc, bc.ToString());

        string Fmt(double v) => DetailRenderHelpers.Fmt(config, v);
        string FmtPct(double v) => DetailRenderHelpers.FmtPct(config, v);

        return col switch
        {
            BarColumn.Dps => (Label(col), Fmt(c.EncDps)),
            BarColumn.InstantDps => (Label(col), Fmt(c.InstantDps)),
            BarColumn.PeakDps => (Label(col), Fmt(c.PeakDps)),
            BarColumn.Damage => (Label(col), Fmt(c.Damage)),
            BarColumn.DamagePercent => (Label(col), c.DamagePercent),
            BarColumn.MaxHit when !string.IsNullOrEmpty(c.MaxHit) => (Label(col), c.MaxHitSkillName),
            BarColumn.MaxHitValue when c.MaxHitDamage > 0 => (Label(col), Fmt(c.MaxHitDamage)),
            BarColumn.DamageShield => (Label(col), Fmt(c.DamageShield)),
            BarColumn.EncDps => (Label(col), Fmt(c.RaidDps)),

            BarColumn.Hps => (Label(col), Fmt(c.EncHps)),
            BarColumn.InstantHps => (Label(col), Fmt(c.InstantHps)),
            BarColumn.Healed => (Label(col), Fmt(c.Healed)),
            BarColumn.HealPercent => (Label(col), c.HealedPercent),
            BarColumn.Overheal => (Label(col), FmtPct(c.OverhealPct)),
            BarColumn.OverhealAmount => (Label(col), Fmt(c.OverhealAmount)),
            BarColumn.CritHealPct => (Label(col), FmtPct(c.CritHealPct)),
            BarColumn.MaxHeal when !string.IsNullOrEmpty(c.MaxHeal) => (Label(col), c.MaxHealSkillName),
            BarColumn.MaxHealValue when c.MaxHealAmount > 0 => (Label(col), Fmt(c.MaxHealAmount)),
            BarColumn.HealCount => (Label(col), c.HealCount.ToString()),
            BarColumn.EncHps => (Label(col), Fmt(c.RaidHps)),

            BarColumn.Crit => (Label(col), FmtPct(c.CritPct)),
            BarColumn.DirectHit => (Label(col), FmtPct(c.DirectHitPct)),
            BarColumn.CritDirectHit => (Label(col), FmtPct(c.CritDirectHitPct)),
            BarColumn.CritHitCount => (Label(col), c.CritHitCount.ToString()),
            BarColumn.DirectHitCount => (Label(col), c.DirectHitCount.ToString()),
            BarColumn.CritDirectHitCount => (Label(col), c.CritDirectHitCount.ToString()),
            BarColumn.HitRate => (Label(col), FmtPct(c.HitRate)),
            BarColumn.Swings => (Label(col), c.Swings.ToString()),
            BarColumn.Hits => (Label(col), c.Hits.ToString()),
            BarColumn.Misses => (Label(col), c.Misses.ToString()),
            BarColumn.Positionals => (Label(col), c.Positionals.ToString()),
            BarColumn.PositionalHits => (Label(col), c.PositionalHits.ToString()),
            BarColumn.PositionalMisses => (Label(col), c.PositionalMisses.ToString()),
            BarColumn.PositionalPct => (Label(col), FmtPct(c.PositionalPct)),

            BarColumn.DamageTaken => (Label(col), Fmt(c.DamageTaken)),
            BarColumn.DamageTakenPercent => (Label(col), c.DamageTakenPercent),
            BarColumn.BlockPct => (Label(col), FmtPct(c.BlockPct)),
            BarColumn.ParryPct => (Label(col), FmtPct(c.ParryPct)),
            BarColumn.HealsTaken => (Label(col), Fmt(c.HealsTaken)),

            BarColumn.Deaths => (Label(col), c.Deaths.ToString()),
            BarColumn.Kills => (Label(col), c.Kills.ToString()),
            BarColumn.CombatantDuration => (Label(col), c.CombatantDuration),
            BarColumn.PowerHeal => (Label(col), Fmt(c.PowerHeal)),

            BarColumn.PowerDrain => (Label(col), Fmt(c.PowerDrain)),
            BarColumn.AbsorbHeal => (Label(col), Fmt(c.AbsorbHeal)),
            BarColumn.MaxHealWard when !string.IsNullOrEmpty(c.MaxHealWardName) => (Label(col), $"{c.MaxHealWardName} ({Fmt(c.MaxHealWardAmount)})"),

            BarColumn.LegsSweeped => (Label(col), c.Stuns.ToString()),
            BarColumn.SkillIssue => (Label(col), c.SkillIssue.ToString()),
            BarColumn.DamageDown => (Label(col), c.DamageDown.ToString()),

            _ => null,
        };
    }

    private static List<BarColumn> GetSectionOrder(string sectionName, BarColumn[] defaultOrder, MeterTab? activeTab)
    {
        if (activeTab?.DetailSectionOrder != null
            && activeTab.DetailSectionOrder.TryGetValue(sectionName, out var order)
            && order.Count > 0)
        {
            var valid = new HashSet<BarColumn>(defaultOrder);
            var result = new List<BarColumn>();
            foreach (var col in order)
            {
                if (valid.Contains(col))
                    result.Add(col);
            }
            foreach (var col in defaultOrder)
            {
                if (!result.Contains(col))
                    result.Add(col);
            }
            return result;
        }
        return new List<BarColumn>(defaultOrder);
    }

    private static bool HasAny(HashSet<BarColumn> vis, BarColumn[] cols)
    {
        foreach (var c in cols)
            if (vis.Contains(c)) return true;
        return false;
    }
}
