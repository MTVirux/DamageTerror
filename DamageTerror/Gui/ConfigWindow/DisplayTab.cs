using Dalamud.Bindings.ImGui;
using DamageTerror.Enums;
using DamageTerror.Helpers;
using DamageTerror.Services;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

public class DisplayTab
{
    public bool Draw(Configuration config)
    {
        var changed = false;

        if (ImGui.CollapsingHeader("Content", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.TextDisabled("General bar display options.");

            ImGui.Spacing();

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

            if (config.ShowJobIcons)
            {
                ImGui.SameLine();
                var styleIdx = (int)config.JobIconStyle;
                var styleLabels = new[] { "Framed", "Plain", "Custom" };
                ImGui.SetNextItemWidth(120);
                if (ImGui.Combo("Icon style", ref styleIdx, styleLabels, styleLabels.Length))
                {
                    config.JobIconStyle = (JobIconStyle)styleIdx;
                    changed = true;
                }

                if (config.JobIconStyle == JobIconStyle.Custom)
                {
                    ImGui.Indent();
                    ImGui.TextDisabled("Set a game icon ID per job (0 = default framed).");
                    ImGui.Spacing();

                    foreach (var abbr in JobIconHelper.AllJobAbbreviations.OrderBy(a => a))
                    {
                        config.CustomJobIcons.TryGetValue(abbr, out var curId);
                        var idInt = (int)curId;
                        ImGui.SetNextItemWidth(100);
                        if (ImGui.InputInt($"{abbr.ToUpperInvariant()}##custicon_{abbr}", ref idInt, 0))
                        {
                            if (idInt < 0) idInt = 0;
                            config.CustomJobIcons[abbr] = (uint)idInt;
                            changed = true;
                        }

                        if (idInt > 0)
                        {
                            ImGui.SameLine();
                            var preview = ServiceManager.TextureProvider.GetFromGameIcon(new Dalamud.Interface.Textures.GameIconLookup((uint)idInt));
                            if (preview.TryGetWrap(out var wrap, out _))
                            {
                                ImGui.Image(wrap.Handle, new Vector2(ImGui.GetTextLineHeight(), ImGui.GetTextLineHeight()));
                            }
                        }
                    }

                    ImGui.Unindent();
                }
            }

        }

        return changed;
    }

    internal static readonly Dictionary<BarColumn, string> ColumnLabels = new()
    {
        { BarColumn.Dps, "DPS" },
        { BarColumn.Hps, "HPS" },
        { BarColumn.Damage, "Damage" },
        { BarColumn.Healed, "Healed" },
        { BarColumn.DamagePercent, "Damage %" },
        { BarColumn.HealPercent, "Heal %" },
        { BarColumn.DirectHit, "Direct Hit %" },
        { BarColumn.Crit, "Critical Hit %" },
        { BarColumn.CritDirectHit, "Crit Direct Hit %" },
        { BarColumn.Deaths, "Deaths" },
        { BarColumn.DamageTaken, "Damage Taken" },
        { BarColumn.DamageTakenPercent, "Damage Taken %" },
        { BarColumn.Overheal, "Overheal %" },
        { BarColumn.OverhealAmount, "Overheal Amount" },
        { BarColumn.MaxHit, "Highest Hit" },
        { BarColumn.PeakDps, "Peak DPS" },
        { BarColumn.MaxHeal, "Max Heal" },
        { BarColumn.Swings, "Swings" },
        { BarColumn.Hits, "Hits" },
        { BarColumn.Misses, "Misses" },
        { BarColumn.HitRate, "Hit Rate" },
        { BarColumn.CritHitCount, "Crit Hit Count" },
        { BarColumn.DirectHitCount, "Direct Hit Count" },
        { BarColumn.CritDirectHitCount, "Crit DH Count" },
        { BarColumn.BlockPct, "Block %" },
        { BarColumn.ParryPct, "Parry %" },
        { BarColumn.HealsTaken, "Heals Taken" },
        { BarColumn.AbsorbHeal, "Absorb Heal" },
        { BarColumn.Kills, "Kills" },
        { BarColumn.InstantDps, "Instant DPS" },
        { BarColumn.InstantHps, "Instant HPS" },
        { BarColumn.CritHealPct, "Crit Heal %" },
        { BarColumn.HealCount, "Heal Count" },
        { BarColumn.CombatantDuration, "Duration" },
        { BarColumn.DamageShield, "Shield Damage" },
        { BarColumn.MaxHealWard, "Max Heal Ward" },
        { BarColumn.PowerDrain, "MP Drain" },
        { BarColumn.PowerHeal, "Power Heal" },
    };

    public static bool DrawBarColumns(List<BarColumn> columnOrder, Func<BarColumn, bool> getEnabled, Action<BarColumn, bool> setEnabled, Dictionary<BarColumn, string> headerLabels, Dictionary<BarColumn, ColumnFormatOverride>? formatOverrides = null)
    {
        var changed = false;

        // Collect enabled columns (preserve custom order)
        var enabledColumns = new List<(int index, BarColumn col)>();
        var disabledColumns = new List<BarColumn>();
        for (var i = 0; i < columnOrder.Count; i++)
        {
            if (getEnabled(columnOrder[i]))
                enabledColumns.Add((i, columnOrder[i]));
            else
                disabledColumns.Add(columnOrder[i]);
        }

        disabledColumns.Sort((a, b) =>
            string.Compare(
                ColumnLabels.GetValueOrDefault(a, a.ToString()),
                ColumnLabels.GetValueOrDefault(b, b.ToString()),
                StringComparison.OrdinalIgnoreCase));

        // Draw enabled columns with arrows and header labels
        for (var ei = 0; ei < enabledColumns.Count; ei++)
        {
            var (i, col) = enabledColumns[ei];
            var label = ColumnLabels.GetValueOrDefault(col, col.ToString());

            ImGui.PushID(i);

            var canUp = i > 0;
            if (!canUp) ImGui.BeginDisabled();
            if (ImGui.ArrowButton("##up", ImGuiDir.Up))
            {
                (columnOrder[i], columnOrder[i - 1]) = (columnOrder[i - 1], columnOrder[i]);
                changed = true;
            }
            if (!canUp) ImGui.EndDisabled();

            ImGui.SameLine();

            var canDown = i < columnOrder.Count - 1;
            if (!canDown) ImGui.BeginDisabled();
            if (ImGui.ArrowButton("##down", ImGuiDir.Down))
            {
                (columnOrder[i], columnOrder[i + 1]) = (columnOrder[i + 1], columnOrder[i]);
                changed = true;
            }
            if (!canDown) ImGui.EndDisabled();

            ImGui.SameLine();

            var enabled = true;
            if (ImGui.Checkbox(label, ref enabled))
            {
                setEnabled(col, false);
                changed = true;
            }

            ImGui.SameLine();
            var defaultLabel = Configuration.DefaultHeaderLabels.GetValueOrDefault(col, col.ToString());
            headerLabels.TryGetValue(col, out var currentHeader);
            currentHeader ??= "";
            ImGui.SetNextItemWidth(60);
            if (ImGui.InputTextWithHint($"##hdr_{col}", defaultLabel, ref currentHeader, 32))
            {
                if (string.IsNullOrEmpty(currentHeader))
                    headerLabels.Remove(col);
                else
                    headerLabels[col] = currentHeader;
                changed = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(label);

            if (formatOverrides != null && ColumnFormatOverride.SupportsFormatting(col))
            {
                ImGui.SameLine();
                var hasOverride = formatOverrides.ContainsKey(col);
                if (hasOverride)
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.4f, 0.8f, 1.0f, 1.0f));
                if (ImGui.SmallButton($"F##fmt_{col}"))
                    ImGui.OpenPopup($"##fmtPopup_{col}");
                if (hasOverride)
                    ImGui.PopStyleColor();
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(hasOverride ? "Custom format (click to edit)" : "Set custom format");

                if (ImGui.BeginPopup($"##fmtPopup_{col}"))
                {
                    changed |= DrawColumnFormatPopup(col, formatOverrides);
                    ImGui.EndPopup();
                }
            }

            ImGui.PopID();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextDisabled("Disabled");
        ImGui.Spacing();

        if (disabledColumns.Count > 0 && ImGui.BeginTabBar("##disabledCategories"))
        {
            foreach (var (catName, catColumns) in DisabledCategories)
            {
                var catDisabled = new List<BarColumn>();
                foreach (var col in catColumns)
                    if (disabledColumns.Contains(col))
                        catDisabled.Add(col);

                if (catDisabled.Count == 0)
                    continue;

                if (ImGui.BeginTabItem(catName))
                {
                    catDisabled.Sort((a, b) =>
                        string.Compare(
                            ColumnLabels.GetValueOrDefault(a, a.ToString()),
                            ColumnLabels.GetValueOrDefault(b, b.ToString()),
                            StringComparison.OrdinalIgnoreCase));

                    foreach (var col in catDisabled)
                    {
                        var label = ColumnLabels.GetValueOrDefault(col, col.ToString());
                        ImGui.PushID($"disabled_{col}");

                        var enabled = false;
                        if (ImGui.Checkbox(label, ref enabled))
                        {
                            setEnabled(col, true);
                            changed = true;
                        }

                        ImGui.PopID();
                    }

                    ImGui.EndTabItem();
                }
            }

            ImGui.EndTabBar();
        }

        return changed;
    }

    private static readonly (string Name, BarColumn[] Columns)[] DisabledCategories =
    {
        ("Dmg", new[] { BarColumn.Dps, BarColumn.Damage, BarColumn.InstantDps, BarColumn.PeakDps, BarColumn.MaxHit, BarColumn.DamageShield }),
        ("Heal", new[] { BarColumn.Hps, BarColumn.Healed, BarColumn.InstantHps, BarColumn.MaxHeal, BarColumn.MaxHealWard, BarColumn.OverhealAmount, BarColumn.AbsorbHeal }),
        ("Taken", new[] { BarColumn.DamageTaken, BarColumn.HealsTaken }),
        ("D%", new[] { BarColumn.DamagePercent, BarColumn.DirectHit, BarColumn.Crit, BarColumn.CritDirectHit }),
        ("H%", new[] { BarColumn.HealPercent, BarColumn.Overheal, BarColumn.CritHealPct }),
        ("T%", new[] { BarColumn.DamageTakenPercent, BarColumn.BlockPct, BarColumn.ParryPct }),
        ("Counts", new[] { BarColumn.Swings, BarColumn.Hits, BarColumn.Misses, BarColumn.HitRate, BarColumn.CritHitCount, BarColumn.DirectHitCount, BarColumn.CritDirectHitCount, BarColumn.HealCount, BarColumn.Deaths, BarColumn.Kills }),
        ("Others", new[] { BarColumn.CombatantDuration, BarColumn.PowerDrain, BarColumn.PowerHeal }),
    };

    public static bool GetTabColumnEnabled(MeterTab tab, BarColumn col) => tab.IsColumnVisible(col);

    public static void SetTabColumnEnabled(MeterTab tab, BarColumn col, bool value)
    {
        if (value)
            tab.VisibleColumns.Add(col);
        else
            tab.VisibleColumns.Remove(col);
    }

    private static readonly string[] FormatLabels = { "Abbreviated (12.3K)", "Commas (12,345)", "Raw (12345.6)" };

    private static bool DrawColumnFormatPopup(BarColumn col, Dictionary<BarColumn, ColumnFormatOverride> overrides)
    {
        var changed = false;
        var label = ColumnLabels.GetValueOrDefault(col, col.ToString());
        ImGui.TextDisabled($"{label} — Format Override");
        ImGui.Spacing();

        var hasOverride = overrides.TryGetValue(col, out var ov);
        var useCustom = hasOverride;

        if (ImGui.Checkbox("Use custom format##colFmt", ref useCustom))
        {
            if (useCustom && !hasOverride)
            {
                ov = new ColumnFormatOverride();
                overrides[col] = ov;
            }
            else if (!useCustom && hasOverride)
            {
                overrides.Remove(col);
                ov = null;
            }
            changed = true;
        }

        if (ov == null)
        {
            ImGui.TextDisabled("Using global format settings.");
            return changed;
        }

        ImGui.Spacing();

        var isValue = ColumnFormatOverride.ValueColumns.Contains(col);
        var isPercent = ColumnFormatOverride.PercentColumns.Contains(col);

        if (isValue)
        {
            var fmtIdx = (int)ov.ValueDisplayFormat;
            ImGui.SetNextItemWidth(200);
            if (ImGui.Combo("Number format##colFmt", ref fmtIdx, FormatLabels, FormatLabels.Length))
            {
                ov.ValueDisplayFormat = (ValueDisplayFormat)fmtIdx;
                changed = true;
            }

            if (ov.ValueDisplayFormat == ValueDisplayFormat.Abbreviated)
            {
                var abbDec = ov.AbbreviatedDecimalPlaces;
                ImGui.SetNextItemWidth(120);
                if (ImGui.SliderInt("Abbreviated decimals##colFmt", ref abbDec, 0, 2))
                {
                    ov.AbbreviatedDecimalPlaces = abbDec;
                    changed = true;
                }

                ImGui.Spacing();
                var kThresh = (float)ov.AbbreviatedKThreshold;
                ImGui.SetNextItemWidth(120);
                if (ImGui.InputFloat("K threshold##colFmt", ref kThresh, 0, 0, "%.0f"))
                {
                    if (kThresh >= 0) ov.AbbreviatedKThreshold = kThresh;
                    changed = true;
                }

                var mThresh = (float)ov.AbbreviatedMThreshold;
                ImGui.SetNextItemWidth(120);
                if (ImGui.InputFloat("M threshold##colFmt", ref mThresh, 0, 0, "%.0f"))
                {
                    if (mThresh >= 0) ov.AbbreviatedMThreshold = mThresh;
                    changed = true;
                }
            }
            else
            {
                var rawDec = ov.RawDecimalPlaces;
                ImGui.SetNextItemWidth(120);
                if (ImGui.SliderInt("Decimal places##colFmt", ref rawDec, 0, 2))
                {
                    ov.RawDecimalPlaces = rawDec;
                    changed = true;
                }
            }

            ImGui.Spacing();
            ImGui.TextDisabled("Preview:");
            double[] samples = { 500, 9_999, 15_000, 1_500_000 };
            foreach (var s in samples)
            {
                var dec = ov.ValueDisplayFormat == ValueDisplayFormat.Abbreviated
                    ? ov.AbbreviatedDecimalPlaces : ov.RawDecimalPlaces;
                ImGui.BulletText($"{s:N0} → {ValueFormatter.Format(s, ov.ValueDisplayFormat, dec, ov.AbbreviatedKThreshold, ov.AbbreviatedMThreshold)}");
            }
        }

        if (isPercent)
        {
            var pctDec = ov.PercentDecimalPlaces;
            ImGui.SetNextItemWidth(120);
            if (ImGui.SliderInt("Percent decimals##colFmt", ref pctDec, 0, 2))
            {
                ov.PercentDecimalPlaces = pctDec;
                changed = true;
            }

            ImGui.Spacing();
            ImGui.TextDisabled("Preview:");
            double[] pctSamples = { 5.3, 12.75, 48.6 };
            foreach (var s in pctSamples)
                ImGui.BulletText($"{s} → {ValueFormatter.FormatPercent(s, ov.PercentDecimalPlaces)}");
        }

        return changed;
    }
}
