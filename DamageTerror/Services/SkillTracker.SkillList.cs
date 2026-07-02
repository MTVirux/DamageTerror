namespace DamageTerror.Services;

public sealed partial class SkillTracker
{
    private SkillDamageType LookupDamageType(uint actionId)
    {
        if (damageTypeCache.TryGetValue(actionId, out var cached))
            return cached;

        var result = SkillDamageType.Unknown;
        try
        {
            var sheet = dataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>();
            if (sheet != null)
            {
                var row = sheet.GetRowOrDefault(actionId);
                if (row.HasValue)
                {
                    // AttackType: 0=None, 1=Slashing, 2=Piercing, 3=Blunt,
                    // 4=Shooting, 5=Magic, 6+=other physical types
                    var attackType = row.Value.AttackType.RowId;
                    result = attackType switch
                    {
                        0 => SkillDamageType.Unknown,
                        5 => SkillDamageType.Magic,
                        _ => SkillDamageType.Physical,
                    };
                }
            }
        }
        catch (Exception ex)
        {
            ServiceManager.LogDebug(LogChannel.SkillTracker, $"Failed to look up damage type for action {actionId}: {ex.Message}");
        }

        damageTypeCache[actionId] = result;
        return result;
    }

    /// <summary>Hardcoded damage type overrides for status-based detonations (e.g. Wildfire).</summary>
    private static readonly Dictionary<uint, SkillDamageType> StatusDamageTypeOverrides = new()
    {
        { 2310, SkillDamageType.Physical }, // Wildfire (MCH)
    };

    private SkillDamageType LookupStatusDamageType(uint statusId)
        => StatusDamageTypeOverrides.GetValueOrDefault(statusId, SkillDamageType.Unknown);

    /// <summary>
    /// Resolve a status effect's display name from active statuses or Lumina.
    /// Used for non-DoT status detonations (e.g. Wildfire) that arrive via Type 24 lines.
    /// </summary>
    private string ResolveStatusName(uint statusId, string targetName)
    {
        if (statusTracker != null)
        {
            var statuses = statusTracker.GetActiveStatuses(targetName);
            foreach (var s in statuses)
            {
                if (s.StatusId == statusId)
                    return s.StatusName;
            }
        }

        try
        {
            var sheet = dataManager.GetExcelSheet<Lumina.Excel.Sheets.Status>();
            if (sheet != null)
            {
                var row = sheet.GetRowOrDefault(statusId);
                if (row.HasValue)
                {
                    var name = row.Value.Name.ToString();
                    if (!string.IsNullOrEmpty(name))
                        return name;
                }
            }
        }
        catch { }

        return $"Status {statusId}";
    }

    public List<SkillEntry> GetSkills(string combatantName)
    {
        return BuildSkillList(damageData, dotTickData, petDamageData, combatantName, "DoT");
    }

    public List<SkillEntry> GetHealSkills(string combatantName)
    {
        return BuildSkillList(healData, hotTickData, petHealData, combatantName, "HoT");
    }

    private List<SkillEntry> BuildSkillList(
        Dictionary<string, Dictionary<string, SkillAccum>> store,
        Dictionary<string, Dictionary<string, SkillAccum>> tickStore,
        Dictionary<string, Dictionary<string, Dictionary<string, SkillAccum>>> petStore,
        string combatantName,
        string tickLabel)
    {
        lock (syncLock)
        {
            store.TryGetValue(combatantName, out var skills);
            tickStore.TryGetValue(combatantName, out var ticks);

            var list = new List<SkillEntry>();

            if (skills != null)
            {
                foreach (var kv in skills)
                {
                    var a = kv.Value;
                    var entry = new SkillEntry
                    {
                        Name = kv.Key,
                        TotalDamage = a.Amount,
                        HitCount = a.Hits,
                        DamageType = a.DamageType,
                    };
                    if (a.Hits > 0)
                        entry.SetHitRates(a.Crits, a.DirectHits, a.CritDirectHits, a.Hits);

                    if (ticks != null && ticks.TryGetValue(kv.Key, out var tickAccum) && tickAccum.Hits > 0)
                    {
                        var tickEntry = new SkillEntry
                        {
                            Name = $"{kv.Key} ({tickLabel})",
                            TotalDamage = tickAccum.Amount,
                            HitCount = tickAccum.Hits,
                            DamageType = tickAccum.DamageType,
                        };
                        if (tickAccum.Hits > 0)
                            tickEntry.SetHitRates(tickAccum.Crits, tickAccum.DirectHits, tickAccum.CritDirectHits, tickAccum.Hits);
                        entry.SubEntries = new List<SkillEntry> { tickEntry };
                    }

                    list.Add(entry);
                }
            }

            // Merge pet categories: each pet becomes a top-level entry with its skills as sub-entries.
            if (petStore.TryGetValue(combatantName, out var pets))
            {
                ServiceManager.LogDebug(LogChannel.PetDebug, $"[PetDebug] BuildSkillList found {pets.Count} pet(s) for {combatantName}");
                foreach (var (petName, petSkills) in pets)
                {
                    long petTotal = 0;
                    int petHits = 0;
                    int petCrits = 0;
                    int petDirectHits = 0;
                    int petCritDirectHits = 0;
                    var subEntries = new List<SkillEntry>();

                    foreach (var (sName, acc) in petSkills)
                    {
                        petTotal += acc.Amount;
                        petHits += acc.Hits;
                        petCrits += acc.Crits;
                        petDirectHits += acc.DirectHits;
                        petCritDirectHits += acc.CritDirectHits;

                        var sub = new SkillEntry
                        {
                            Name = sName,
                            TotalDamage = acc.Amount,
                            HitCount = acc.Hits,
                            DamageType = acc.DamageType,
                        };
                        if (acc.Hits > 0)
                            sub.SetHitRates(acc.Crits, acc.DirectHits, acc.CritDirectHits, acc.Hits);
                        subEntries.Add(sub);
                    }

                    var petEntry = new SkillEntry
                    {
                        Name = petName,
                        TotalDamage = petTotal,
                        HitCount = petHits,
                        SubEntries = subEntries.OrderByDescending(s => s.TotalDamage).ToList(),
                    };
                    if (petHits > 0)
                        petEntry.SetHitRates(petCrits, petDirectHits, petCritDirectHits, petHits);
                    list.Add(petEntry);
                }
            }

            list.Sort((a, b) => b.TotalDamage.CompareTo(a.TotalDamage));

            var total = list.Sum(s => s.TotalDamage);
            if (total > 0)
            {
                foreach (var s in list)
                {
                    s.DamagePercent = (double)s.TotalDamage / total * 100.0;
                    if (s.SubEntries != null)
                    {
                        foreach (var sub in s.SubEntries)
                            sub.DamagePercent = (double)sub.TotalDamage / total * 100.0;
                    }
                }
            }

            return list;
        }
    }
}
