using Dalamud.Bindings.ImGui;
using DamageTerror.Enums;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

public static class MetricPicker
{
    public static readonly (string Name, BarColumn[] Items)[] BarColumnCategories =
    {
        ("Damage", new[] { BarColumn.Dps, BarColumn.InstantDps, BarColumn.PeakDps, BarColumn.Damage, BarColumn.DamagePercent, BarColumn.MaxHit, BarColumn.MaxHitValue, BarColumn.DamageShield, BarColumn.EncDps }),
        ("Healing", new[] { BarColumn.Hps, BarColumn.InstantHps, BarColumn.Healed, BarColumn.HealPercent, BarColumn.Overheal, BarColumn.OverhealAmount, BarColumn.CritHealPct, BarColumn.MaxHeal, BarColumn.MaxHealValue, BarColumn.HealCount, BarColumn.EncHps }),
        ("Hit Stats", new[] { BarColumn.Crit, BarColumn.DirectHit, BarColumn.CritDirectHit, BarColumn.CritHitCount, BarColumn.DirectHitCount, BarColumn.CritDirectHitCount, BarColumn.HitRate, BarColumn.Swings, BarColumn.Hits, BarColumn.Misses }),
        ("Defense", new[] { BarColumn.DamageTaken, BarColumn.DamageTakenPercent, BarColumn.BlockPct, BarColumn.ParryPct, BarColumn.HealsTaken }),
        ("High-end Raiding", new[] { BarColumn.LegsSweeped, BarColumn.SkillIssue, BarColumn.DamageDown }),
        ("Group", new[] { BarColumn.DpsRank, BarColumn.HpsRank, BarColumn.GroupDps, BarColumn.GroupHps, BarColumn.GroupDamage, BarColumn.GroupHealed, BarColumn.GroupDamageTaken, BarColumn.GroupDeaths, BarColumn.GroupOverheal, BarColumn.GroupDamageDown, BarColumn.GroupInstantDps, BarColumn.GroupInstantHps, BarColumn.GroupAvgDps, BarColumn.GroupAvgHps, BarColumn.GroupAvgCrit, BarColumn.GroupAvgDirectHit, BarColumn.GroupAvgCritDirectHit, BarColumn.GroupAvgOverhealPct, BarColumn.GroupAvgCritHealPct, BarColumn.GroupAvgHitRate, BarColumn.GroupPeakDps, BarColumn.GroupMaxHitValue, BarColumn.GroupMaxHealValue }),
        ("Other", new[] { BarColumn.Deaths, BarColumn.Kills, BarColumn.CombatantDuration, BarColumn.PowerHeal }),
#if DEBUG
        ("Unknown", new[] { BarColumn.PowerDrain, BarColumn.AbsorbHeal, BarColumn.MaxHealWard }),
#endif
    };

    public static readonly (string Name, TooltipField[] Items)[] TooltipFieldCategories =
    {
        ("Damage", new[] { TooltipField.Dps, TooltipField.InstantDps, TooltipField.PeakDps, TooltipField.Damage, TooltipField.DamagePercent, TooltipField.MaxHit, TooltipField.MaxHitValue, TooltipField.DamageShield, TooltipField.EncDps, TooltipField.TopDamageSkills }),
        ("Healing", new[] { TooltipField.Hps, TooltipField.InstantHps, TooltipField.Healed, TooltipField.HealPercent, TooltipField.Overheal, TooltipField.OverhealAmount, TooltipField.CritHealPct, TooltipField.MaxHeal, TooltipField.MaxHealValue, TooltipField.MaxHealWard, TooltipField.HealCount, TooltipField.EncHps, TooltipField.TopHealingSkills }),
        ("Hit Stats", new[] { TooltipField.Crit, TooltipField.DirectHit, TooltipField.CritDirectHit, TooltipField.HitRate, TooltipField.Swings, TooltipField.Hits, TooltipField.Misses }),
        ("Defense", new[] { TooltipField.DamageTaken, TooltipField.HealsTaken }),
        ("Other", new[] { TooltipField.Name, TooltipField.Job, TooltipField.Deaths, TooltipField.Kills, TooltipField.CombatantDuration }),
        ("High-end Raiding", new[] { TooltipField.LegsSweeped, TooltipField.SkillIssue, TooltipField.DamageDown }),
    };

    public static readonly Dictionary<BarColumn, string> BarColumnLabels = new()
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
        { BarColumn.MaxHitValue, "Highest Hit Value" },
        { BarColumn.PeakDps, "Peak DPS" },
        { BarColumn.MaxHeal, "Max Heal" },
        { BarColumn.MaxHealValue, "Max Heal Value" },
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
        { BarColumn.PowerHeal, "MP Recovery" },
        { BarColumn.LegsSweeped, "Legs Sweeped" },
        { BarColumn.SkillIssue, "Skill Issue" },
        { BarColumn.DamageDown, "Damage Down" },
        { BarColumn.EncDps, "Encounter DPS" },
        { BarColumn.EncHps, "Encounter HPS" },
        { BarColumn.DpsRank, "DPS Rank" },
        { BarColumn.HpsRank, "HPS Rank" },
        { BarColumn.GroupDps, "Group DPS" },
        { BarColumn.GroupHps, "Group HPS" },
        { BarColumn.GroupDamage, "Group Damage" },
        { BarColumn.GroupHealed, "Group Healed" },
        { BarColumn.GroupDamageTaken, "Group Dmg Taken" },
        { BarColumn.GroupDeaths, "Group Deaths" },
        { BarColumn.GroupOverheal, "Group Overheal" },
        { BarColumn.GroupInstantDps, "Group Instant DPS" },
        { BarColumn.GroupInstantHps, "Group Instant HPS" },
        { BarColumn.GroupAvgDps, "Group Avg DPS" },
        { BarColumn.GroupAvgHps, "Group Avg HPS" },
        { BarColumn.GroupAvgCrit, "Group Avg Crit %" },
        { BarColumn.GroupAvgDirectHit, "Group Avg Direct Hit %" },
        { BarColumn.GroupAvgCritDirectHit, "Group Avg Crit DH %" },
        { BarColumn.GroupAvgOverhealPct, "Group Avg Overheal %" },
        { BarColumn.GroupAvgCritHealPct, "Group Avg Crit Heal %" },
        { BarColumn.GroupAvgHitRate, "Group Avg Hit Rate" },
        { BarColumn.GroupPeakDps, "Group Peak DPS" },
        { BarColumn.GroupMaxHitValue, "Group Max Hit Value" },
        { BarColumn.GroupMaxHealValue, "Group Max Heal Value" },
        { BarColumn.GroupDamageDown, "Group Damage Down" },
    };

    public static readonly Dictionary<TooltipField, string> TooltipFieldLabels = new()
    {
        { TooltipField.Name, "Name" },
        { TooltipField.Job, "Job" },
        { TooltipField.Dps, "DPS" },
        { TooltipField.Hps, "HPS" },
        { TooltipField.Damage, "Damage" },
        { TooltipField.Healed, "Healed" },
        { TooltipField.DamagePercent, "Damage %" },
        { TooltipField.HealPercent, "Heal %" },
        { TooltipField.Crit, "Crit %" },
        { TooltipField.DirectHit, "Direct Hit %" },
        { TooltipField.CritDirectHit, "Crit DH %" },
        { TooltipField.Deaths, "Deaths" },
        { TooltipField.DamageTaken, "Damage Taken" },
        { TooltipField.Overheal, "Overheal %" },
        { TooltipField.OverhealAmount, "Overheal" },
        { TooltipField.MaxHit, "Max Hit" },
        { TooltipField.MaxHitValue, "Max Hit Value" },
        { TooltipField.MaxHeal, "Max Heal" },
        { TooltipField.MaxHealValue, "Max Heal Value" },
        { TooltipField.PeakDps, "Peak DPS" },
        { TooltipField.Swings, "Swings" },
        { TooltipField.Hits, "Hits" },
        { TooltipField.Misses, "Misses" },
        { TooltipField.HitRate, "Hit Rate" },
        { TooltipField.Kills, "Kills" },
        { TooltipField.CombatantDuration, "Duration" },
        { TooltipField.HealsTaken, "Heals Taken" },
        { TooltipField.InstantDps, "Instant DPS" },
        { TooltipField.InstantHps, "Instant HPS" },
        { TooltipField.CritHealPct, "Crit Heal %" },
        { TooltipField.HealCount, "Heal Count" },
        { TooltipField.DamageShield, "Damage Shield" },
        { TooltipField.MaxHealWard, "Max Heal Ward" },
        { TooltipField.LegsSweeped, "Legs Sweeped" },
        { TooltipField.SkillIssue, "Skill Issue" },
        { TooltipField.DamageDown, "Damage Down" },
        { TooltipField.EncDps, "Encounter DPS" },
        { TooltipField.EncHps, "Encounter HPS" },
        { TooltipField.TopDamageSkills, "Top Damage Skills" },
        { TooltipField.TopHealingSkills, "Top Healing Skills" },
    };

    public static readonly Dictionary<BarColumn, string> BarColumnDescriptions = new()
    {
        { BarColumn.LegsSweeped, "That move was a low blow..." },
        { BarColumn.SkillIssue, "Ratio of damage taken to damage dealt. Higher values indicate taking more damage relative to output." },
        { BarColumn.DamageDown, "Count of Damage Down debuffs received (excludes Vulnerability Up)." },
    };

    public static readonly Dictionary<TooltipField, string> TooltipFieldDescriptions = new()
    {
        { TooltipField.LegsSweeped, "That move was a low blow..." },
        { TooltipField.SkillIssue, "Ratio of damage taken to damage dealt. Higher values indicate taking more damage relative to output." },
        { TooltipField.DamageDown, "Count of Damage Down debuffs received (excludes Vulnerability Up)." },
    };

    public static string GetBarColumnLabel(BarColumn col) =>
        BarColumnLabels.GetValueOrDefault(col, col.ToString());

    public static string GetTooltipFieldLabel(TooltipField field) =>
        TooltipFieldLabels.GetValueOrDefault(field, field.ToString());

    /// <summary>
    /// Draws a metric picker with an ordered enabled list (with reorder arrows) at the top,
    /// and disabled items organized in categorized tabs below.
    /// </summary>
    public static bool Draw<T>(
        string id,
        List<T> enabledItems,
        Func<T, string> getLabel,
        (string Name, T[] Items)[] categories,
        Func<T, bool>? drawItemExtras = null,
        Func<T, string?>? getDescription = null) where T : struct, Enum
    {
        var changed = false;

        var enabledSet = new HashSet<T>(enabledItems);

        for (var i = 0; i < enabledItems.Count; i++)
        {
            var item = enabledItems[i];
            var label = getLabel(item);

            ImGui.PushID($"{id}_e_{i}");

            var canUp = i > 0;
            if (!canUp) ImGui.BeginDisabled();
            if (ImGui.ArrowButton("##up", ImGuiDir.Up))
            {
                (enabledItems[i - 1], enabledItems[i]) = (enabledItems[i], enabledItems[i - 1]);
                changed = true;
            }
            if (!canUp) ImGui.EndDisabled();

            ImGui.SameLine();

            var canDown = i < enabledItems.Count - 1;
            if (!canDown) ImGui.BeginDisabled();
            if (ImGui.ArrowButton("##down", ImGuiDir.Down))
            {
                (enabledItems[i], enabledItems[i + 1]) = (enabledItems[i + 1], enabledItems[i]);
                changed = true;
            }
            if (!canDown) ImGui.EndDisabled();

            ImGui.SameLine();

            var enabled = true;
            if (ImGui.Checkbox(label, ref enabled))
            {
                enabledItems.RemoveAt(i);
                enabledSet.Remove(item);
                i--;
                changed = true;
                ImGui.PopID();
                continue;
            }

            if (getDescription != null)
            {
                var desc = getDescription(item);
                if (desc != null && ImGui.IsItemHovered())
                    ImGui.SetTooltip(desc);
            }

            if (drawItemExtras != null)
                changed |= drawItemExtras(item);

            ImGui.PopID();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextDisabled("Disabled");
        ImGui.Spacing();

        var hasAnyDisabled = false;
        foreach (var (_, catItems) in categories)
            foreach (var item in catItems)
                if (!enabledSet.Contains(item))
                {
                    hasAnyDisabled = true;
                    break;
                }

        if (hasAnyDisabled && ImGui.BeginTabBar($"##{id}_disabled"))
        {
            foreach (var (catName, catItems) in categories)
            {
                var catDisabled = new List<T>();
                foreach (var item in catItems)
                    if (!enabledSet.Contains(item))
                        catDisabled.Add(item);

                if (catDisabled.Count == 0)
                    continue;

                if (ImGui.BeginTabItem(catName))
                {
                    catDisabled.Sort((a, b) =>
                        string.Compare(getLabel(a), getLabel(b), StringComparison.OrdinalIgnoreCase));

                    foreach (var item in catDisabled)
                    {
                        var label = getLabel(item);
                        ImGui.PushID($"{id}_d_{item}");

                        var off = false;
                        if (ImGui.Checkbox(label, ref off))
                        {
                            enabledItems.Add(item);
                            enabledSet.Add(item);
                            changed = true;
                        }

                        if (getDescription != null)
                        {
                            var desc = getDescription(item);
                            if (desc != null && ImGui.IsItemHovered())
                                ImGui.SetTooltip(desc);
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

    /// <summary>
    /// Draws a categorized metric picker with tabs per category.
    /// Each category tab shows items with optional reorder arrows and visibility checkboxes.
    /// </summary>
    public static bool DrawCategorized<T>(
        string id,
        HashSet<T> enabledSet,
        Func<T, string> getLabel,
        (string Name, T[] Items)[] categories,
        Dictionary<string, List<T>>? sectionOrder = null,
        Func<T, bool>? drawItemExtras = null,
        Func<T, string?>? getDescription = null) where T : struct, Enum
    {
        var changed = false;

        if (!ImGui.BeginTabBar($"##{id}_cats"))
            return changed;

        foreach (var (catName, catItems) in categories)
        {
            if (!ImGui.BeginTabItem(catName))
                continue;

            List<T> order;
            if (sectionOrder != null)
            {
                if (!sectionOrder.TryGetValue(catName, out order!) || order.Count == 0)
                {
                    order = new List<T>(catItems);
                    sectionOrder[catName] = order;
                }

                foreach (var item in catItems)
                    if (!order.Contains(item))
                        order.Add(item);

                var validItems = new HashSet<T>(catItems);
                order.RemoveAll(item => !validItems.Contains(item));
            }
            else
            {
                order = new List<T>(catItems);
            }

            for (var i = 0; i < order.Count; i++)
            {
                var item = order[i];
                var label = getLabel(item);
                var enabled = enabledSet.Contains(item);

                ImGui.PushID($"{id}_c_{catName}_{i}");

                if (sectionOrder != null)
                {
                    var canUp = i > 0;
                    if (!canUp) ImGui.BeginDisabled();
                    if (ImGui.ArrowButton("##up", ImGuiDir.Up))
                    {
                        (order[i], order[i - 1]) = (order[i - 1], order[i]);
                        changed = true;
                    }
                    if (!canUp) ImGui.EndDisabled();

                    ImGui.SameLine();

                    var canDown = i < order.Count - 1;
                    if (!canDown) ImGui.BeginDisabled();
                    if (ImGui.ArrowButton("##down", ImGuiDir.Down))
                    {
                        (order[i], order[i + 1]) = (order[i + 1], order[i]);
                        changed = true;
                    }
                    if (!canDown) ImGui.EndDisabled();

                    ImGui.SameLine();
                }

                if (ImGui.Checkbox(label, ref enabled))
                {
                    if (enabled)
                        enabledSet.Add(item);
                    else
                        enabledSet.Remove(item);
                    changed = true;
                }

                if (getDescription != null)
                {
                    var desc = getDescription(item);
                    if (desc != null && ImGui.IsItemHovered())
                        ImGui.SetTooltip(desc);
                }

                if (drawItemExtras != null)
                    changed |= drawItemExtras(item);

                ImGui.PopID();
            }

            if (sectionOrder != null)
            {
                if (ImGui.Button($"Reset Order##{catName}"))
                {
                    sectionOrder[catName] = new List<T>(catItems);
                    changed = true;
                }
            }

            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();

        return changed;
    }
}
