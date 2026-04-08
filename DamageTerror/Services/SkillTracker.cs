using System.Collections.Concurrent;
using System.Globalization;
using Dalamud.Plugin.Services;

namespace DamageTerror.Services;

public class SkillTracker
{
    private readonly object syncLock = new();

    private Dictionary<string, Dictionary<string, SkillAccum>> damageData = new();
    private Dictionary<string, Dictionary<string, SkillAccum>> healData = new();
    private Dictionary<string, Dictionary<string, SkillAccum>> dotTickData = new();
    private Dictionary<string, Dictionary<string, SkillAccum>> hotTickData = new();

    private readonly Dictionary<string, List<SkillUseEvent>> skillEvents = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<SkillUseEvent>> damageTakenEvents = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> stunCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> skillIssueCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(string Target, string Status), int> skillIssueStacks = new();
    private readonly Dictionary<string, int> damageDownCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(string Target, string Status), int> damageDownStacks = new();

    /// <summary>Localized status names that count as "skill issues" (Vulnerability Up / Damage Down).</summary>
    private static readonly HashSet<string> SkillIssueNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // EN
        "Vulnerability Up",
        "Damage Down",
        // DE
        "Erhöhte Verwundbarkeit",
        "Schaden -",
        // FR
        "Vulnérabilité augmentée",
        "Malus de dégâts",
        // JA
        "被ダメージ上昇",
        "ダメージ低下",
    };

    /// <summary>Localized status names that count as "Damage Down" only (subset of skill issues).</summary>
    private static readonly HashSet<string> DamageDownNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // EN
        "Damage Down",
        // DE
        "Schaden -",
        // FR
        "Malus de dégâts",
        // JA
        "ダメージ低下",
    };

    private Dictionary<string, List<SkillUseEvent>>? seededEvents;
    private Dictionary<string, List<SkillUseEvent>>? seededDamageTakenEvents;

    private EncounterTimer? timer;
    private GraphDataTracker? graphTracker;
    private StatusTracker? statusTracker;
    private readonly ConcurrentDictionary<uint, SkillDamageType> damageTypeCache = new();
    private readonly IDataManager dataManager;
    private readonly IPluginLog log;

    public SkillTracker(IDataManager dataManager, IPluginLog log)
    {
        this.dataManager = dataManager;
        this.log = log;
    }

    /// <summary>Bind the shared encounter timer, graph tracker, and status tracker.</summary>
    public void SetDependencies(EncounterTimer encounterTimer, GraphDataTracker tracker, StatusTracker statusTracker)
    {
        timer = encounterTimer;
        graphTracker = tracker;
        this.statusTracker = statusTracker;
    }

    /// <summary>
    /// Pre-load historical skill events so that <see cref="GetSkillEvents"/> returns
    /// them until live data is available. Cleared on the next <see cref="Reset"/> call.
    /// </summary>
    public void SeedHistoricalEvents(Dictionary<string, List<SkillUseEvent>> data)
    {
        lock (syncLock)
        {
            seededEvents = new Dictionary<string, List<SkillUseEvent>>(data, StringComparer.OrdinalIgnoreCase);
        }
    }

    public void SeedHistoricalDamageTakenEvents(Dictionary<string, List<SkillUseEvent>> data)
    {
        lock (syncLock)
        {
            seededDamageTakenEvents = new Dictionary<string, List<SkillUseEvent>>(data, StringComparer.OrdinalIgnoreCase);
        }
    }

    private struct SkillAccum
    {
        public long Amount;
        public int Hits;
        public int Crits;
        public int DirectHits;
        public int CritDirectHits;
        public SkillDamageType DamageType;
    }

    public void ProcessLogLine(string[] line)
    {
        if (line.Length < 2)
            return;

        var type = line[0];

        if (type == "26" || type == "30")
        {
            ProcessStatusLine(type, line);
            return;
        }

        if (type == "24")
        {
            ProcessDoTHoTLine(line);
            return;
        }

        if (line.Length < 10)
            return;

        if (type != "21" && type != "22")
            return;

        var sourceName = line[3];
        var targetName = line.Length > 7 ? line[7] : null;
        var skillName = string.Equals(line[5], "Attack", StringComparison.OrdinalIgnoreCase) ? "Auto Attack" : line[5];

        if (string.IsNullOrEmpty(sourceName) || string.IsNullOrEmpty(skillName))
            return;

        var damageType = SkillDamageType.Unknown;
        if (line.Length > 4 && uint.TryParse(line[4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var actionId))
            damageType = LookupDamageType(actionId);

        // Scan all 8 effect pairs (fields 8-23).
        // A single ability can have both damage and healing in different pairs
        // (e.g. drain abilities like Souleater, Energy Drain).
        long dmgAmount = 0;
        byte dmgSeverity = 0;
        long healAmount = 0;
        byte healSeverity = 0;
        for (int i = 0; i < 8; i++)
        {
            int flagIdx = 8 + i * 2;
            int valIdx = flagIdx + 1;
            if (valIdx >= line.Length)
                break;

            var result = DecodeEffect(line[flagIdx], line[valIdx]);
            if (result.Amount <= 0)
                continue;

            if (result.EffectType == 4)
            {
                if (healAmount == 0)
                {
                    healAmount = result.Amount;
                    healSeverity = result.Severity;
                }
            }
            else if (dmgAmount == 0)
            {
                dmgAmount = result.Amount;
                dmgSeverity = result.Severity;
            }
        }

        // Count Leg Sweep / Low Blow uses regardless of whether they deal damage.
        if (string.Equals(skillName, "Leg Sweep", StringComparison.OrdinalIgnoreCase)
            || string.Equals(skillName, "Low Blow", StringComparison.OrdinalIgnoreCase))
        {
            lock (syncLock)
                stunCounts[sourceName] = stunCounts.GetValueOrDefault(sourceName) + 1;
        }

        if (dmgAmount <= 0 && healAmount <= 0)
            return;

        lock (syncLock)
        {
            if (dmgAmount > 0)
            {
                AccumulateSkill(damageData, sourceName, skillName, dmgAmount, dmgSeverity, damageType);
                RecordEvent(sourceName, skillName, dmgAmount, false, dmgSeverity, targetName);

                if (!string.IsNullOrEmpty(targetName))
                    RecordDamageTakenEvent(targetName, skillName, dmgAmount, dmgSeverity);
            }
            if (healAmount > 0)
            {
                AccumulateSkill(healData, sourceName, skillName, healAmount, healSeverity, damageType);
                RecordEvent(sourceName, skillName, healAmount, true, healSeverity, targetName);
            }
        }

        // Feed high-resolution damage/heal totals into the graph tracker
        // outside the skill lock to avoid nested locking.
        if (dmgAmount > 0 || healAmount > 0)
            graphTracker?.RecordLogLineEvent(sourceName, dmgAmount, healAmount);
    }

    private void AccumulateSkill(Dictionary<string, Dictionary<string, SkillAccum>> store,
        string sourceName, string skillName, long amount, byte severity, SkillDamageType damageType)
    {
        bool isCrit = (severity & 0x20) != 0;
        bool isDirectHit = (severity & 0x40) != 0;
        bool isCritDirectHit = isCrit && isDirectHit;

        if (!store.TryGetValue(sourceName, out var skills))
        {
            skills = new Dictionary<string, SkillAccum>();
            store[sourceName] = skills;
        }

        if (!skills.TryGetValue(skillName, out var existing))
            existing = default;

        existing.Amount += amount;
        existing.Hits++;
        if (isCritDirectHit)
            existing.CritDirectHits++;
        else if (isCrit)
            existing.Crits++;
        else if (isDirectHit)
            existing.DirectHits++;

        if (existing.DamageType == SkillDamageType.Unknown && damageType != SkillDamageType.Unknown)
            existing.DamageType = damageType;

        skills[skillName] = existing;
    }

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
            ServiceManager.PluginLog.Debug($"Failed to look up damage type for action {actionId}: {ex.Message}");
        }

        damageTypeCache[actionId] = result;
        return result;
    }

    public List<SkillEntry> GetSkills(string combatantName)
    {
        return BuildSkillList(damageData, dotTickData, combatantName, "DoT");
    }

    public List<SkillEntry> GetHealSkills(string combatantName)
    {
        return BuildSkillList(healData, hotTickData, combatantName, "HoT");
    }

    public List<SkillUseEvent> GetSkillEvents(string combatantName)
    {
        lock (syncLock)
        {
            if (skillEvents.TryGetValue(combatantName, out var events) && events.Count > 0)
                return new List<SkillUseEvent>(events);

            // Fall back to seeded historical events when live tracker has nothing yet.
            if (seededEvents != null
                && seededEvents.TryGetValue(combatantName, out var seeded)
                && seeded.Count > 0)
                return new List<SkillUseEvent>(seeded);

            return new List<SkillUseEvent>();
        }
    }

    public List<SkillUseEvent> GetDamageTakenEvents(string combatantName)
    {
        lock (syncLock)
        {
            if (damageTakenEvents.TryGetValue(combatantName, out var events) && events.Count > 0)
                return new List<SkillUseEvent>(events);

            if (seededDamageTakenEvents != null
                && seededDamageTakenEvents.TryGetValue(combatantName, out var seeded)
                && seeded.Count > 0)
                return new List<SkillUseEvent>(seeded);

            return new List<SkillUseEvent>();
        }
    }

    public int GetStunCount(string combatantName)
    {
        lock (syncLock)
        {
            return stunCounts.GetValueOrDefault(combatantName);
        }
    }

    public int GetSkillIssueCount(string combatantName)
    {
        lock (syncLock)
        {
            return skillIssueCounts.GetValueOrDefault(combatantName);
        }
    }

    public int GetDamageDownCount(string combatantName)
    {
        lock (syncLock)
        {
            return damageDownCounts.GetValueOrDefault(combatantName);
        }
    }

    public void Reset()
    {
        lock (syncLock)
        {
            damageData.Clear();
            healData.Clear();
            dotTickData.Clear();
            hotTickData.Clear();
            skillEvents.Clear();
            damageTakenEvents.Clear();
            stunCounts.Clear();
            skillIssueCounts.Clear();
            skillIssueStacks.Clear();
            damageDownCounts.Clear();
            damageDownStacks.Clear();
            seededEvents = null;
            seededDamageTakenEvents = null;
            damageTypeCache.Clear();
        }
    }

    private void RecordEvent(string combatantName, string skillName, long amount, bool isHeal, byte severity,
        string? targetName = null, bool isDoTTick = false, bool isHoTTick = false)
    {
        if (!skillEvents.TryGetValue(combatantName, out var events))
        {
            events = new List<SkillUseEvent>();
            skillEvents[combatantName] = events;
        }

        events.Add(new SkillUseEvent
        {
            TimeSec = timer?.ElapsedSeconds ?? 0f,
            SkillName = skillName,
            TargetName = targetName,
            Amount = amount,
            IsHeal = isHeal,
            IsCrit = (severity & 0x20) != 0,
            IsDirectHit = (severity & 0x40) != 0,
            IsDoTTick = isDoTTick,
            IsHoTTick = isHoTTick,
        });
    }

    /// <summary>
    /// Retroactively tag the most recent skill event as a DoT/HoT application.
    /// Called by StatusTracker when a GainsEffect (type 26) arrives for a known DoT/HoT.
    /// Sets IsDoTApplication/IsHoTApplication on the event for graph/timeline highlighting.
    /// </summary>
    public void MarkLastEventAsApplication(string combatantName, bool isDoT, bool isHoT)
    {
        lock (syncLock)
        {
            if (!skillEvents.TryGetValue(combatantName, out var events) || events.Count == 0)
                return;

            var now = timer?.ElapsedSeconds ?? 0f;
            const int maxScan = 10;
            const float maxAge = 3.0f;

            int start = Math.Max(0, events.Count - maxScan);
            for (int i = events.Count - 1; i >= start; i--)
            {
                var evt = events[i];

                if (evt.IsDoTTick || evt.IsHoTTick || evt.IsDoTApplication || evt.IsHoTApplication)
                    continue;

                if (now - evt.TimeSec > maxAge)
                    break;

                // Tag the first eligible event we find
                if (isDoT) evt.IsDoTApplication = true;
                if (isHoT) evt.IsHoTApplication = true;
                events[i] = evt;
                return;
            }
        }
    }

    private void RecordDamageTakenEvent(string targetName, string skillName, long amount, byte severity)
    {
        if (!damageTakenEvents.TryGetValue(targetName, out var events))
        {
            events = new List<SkillUseEvent>();
            damageTakenEvents[targetName] = events;
        }

        events.Add(new SkillUseEvent
        {
            TimeSec = timer?.ElapsedSeconds ?? 0f,
            SkillName = skillName,
            Amount = amount,
            IsHeal = false,
            IsCrit = (severity & 0x20) != 0,
            IsDirectHit = (severity & 0x40) != 0,
        });
    }

    private List<SkillEntry> BuildSkillList(
        Dictionary<string, Dictionary<string, SkillAccum>> store,
        Dictionary<string, Dictionary<string, SkillAccum>> tickStore,
        string combatantName,
        string tickLabel)
    {
        lock (syncLock)
        {
            if (!store.TryGetValue(combatantName, out var skills))
                return new List<SkillEntry>();

            // Look up tick data for this combatant (may be null)
            tickStore.TryGetValue(combatantName, out var ticks);

            var list = skills.Select(kv =>
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
                {
                    entry.CritPct = (double)(a.Crits + a.CritDirectHits) / a.Hits * 100.0;
                    entry.DirectHitPct = (double)(a.DirectHits + a.CritDirectHits) / a.Hits * 100.0;
                    entry.CritDirectHitPct = (double)a.CritDirectHits / a.Hits * 100.0;
                }

                // Attach tick sub-entry if this skill has periodic ticks
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
                    {
                        tickEntry.CritPct = (double)(tickAccum.Crits + tickAccum.CritDirectHits) / tickAccum.Hits * 100.0;
                        tickEntry.DirectHitPct = (double)(tickAccum.DirectHits + tickAccum.CritDirectHits) / tickAccum.Hits * 100.0;
                        tickEntry.CritDirectHitPct = (double)tickAccum.CritDirectHits / tickAccum.Hits * 100.0;
                    }
                    entry.SubEntries = new List<SkillEntry> { tickEntry };
                }

                return entry;
            }).OrderByDescending(s => s.TotalDamage).ToList();

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

    /// Decode an ability effect from FFXIV network log line fields.
    /// See: https://github.com/OverlayPlugin/cactbot/blob/main/docs/LogGuide.md#ability-damage
    private static (long Amount, byte Severity, byte EffectType) DecodeEffect(string flagsHex, string valueHex)
    {
        if (string.IsNullOrEmpty(flagsHex) || string.IsNullOrEmpty(valueHex))
            return (0, 0, 0);

        if (!uint.TryParse(flagsHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var flags))
            return (0, 0, 0);
        if (!uint.TryParse(valueHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var raw))
            return (0, 0, 0);

        // Low byte of flags is the effect type:
        //   0x03 = damage dealt, 0x04 = heal, 0x05 = blocked damage, 0x06 = parried damage
        var effectType = (byte)(flags & 0xFF);
        if (effectType != 3 && effectType != 4 && effectType != 5 && effectType != 6)
            return (0, 0, 0);

        // Second byte of flags is the severity (crit/DH):
        //   0x20 = crit, 0x40 = direct hit, 0x60 = crit direct hit
        var severity = (byte)((flags >> 8) & 0xFF);

        // Value bytes (left-extended to 4 bytes): ABCD
        // Normal: upper 16 bits (AB).
        // "A lot" (0x4000 mask in value field): reassemble as D-A-B.
        long amount;
        if ((raw & 0x4000) != 0)
        {
            var a = (raw >> 24) & 0xFF;
            var b = (raw >> 16) & 0xFF;
            var d = raw & 0xFF;
            amount = (long)((d << 16) | (a << 8) | b);
        }
        else
        {
            amount = (long)((raw >> 16) & 0xFFFF);
        }

        return (amount, severity, effectType);
    }

    /// <summary>
    /// Parse ACT log line types 26 (GainsEffect) and 30 (LosesEffect)
    /// and forward to the StatusTracker for DoT/HoT lifecycle tracking.
    ///
    /// IINACT field layout:
    ///   [0]=type, [1]=timestamp, [2]=statusId(hex), [3]=statusName,
    ///   [4]=duration(float), [5]=sourceId(hex), [6]=sourceName,
    ///   [7]=targetId(hex), [8]=targetName, [9]=stacks, [10]=targetHP, ...
    /// </summary>
    private void ProcessStatusLine(string type, string[] line)
    {
        if (statusTracker == null)
            return;

        if (line.Length < 9)
            return;

        var statusIdHex = line[2];
        var statusName = line[3];
        var sourceName = line[6];
        var targetName = line[8];

        if (string.IsNullOrEmpty(statusIdHex))
            return;

        if (!uint.TryParse(statusIdHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var statusId))
            return;

        if (type == "26")
        {
            // GainsEffect — parse duration from field [4]
            float duration = 0f;
            if (line.Length > 4)
                float.TryParse(line[4], NumberStyles.Float, CultureInfo.InvariantCulture, out duration);

            statusTracker.OnStatusGained(sourceName, targetName, statusId, statusName, duration);

            if (SkillIssueNames.Contains(statusName))
            {
                int newStacks = 1;
                if (line.Length > 9 && int.TryParse(line[9], out var parsed) && parsed > 0)
                    newStacks = parsed;

                lock (syncLock)
                {
                    var key = (targetName.ToLowerInvariant(), statusName.ToLowerInvariant());
                    var prevStacks = skillIssueStacks.GetValueOrDefault(key);
                    var delta = newStacks - prevStacks;
                    skillIssueCounts[targetName] = skillIssueCounts.GetValueOrDefault(targetName) + Math.Max(delta, 1);
                    skillIssueStacks[key] = newStacks;
                }
            }

            if (DamageDownNames.Contains(statusName))
            {
                int newStacks = 1;
                if (line.Length > 9 && int.TryParse(line[9], out var parsedDD) && parsedDD > 0)
                    newStacks = parsedDD;

                lock (syncLock)
                {
                    var key = (targetName.ToLowerInvariant(), statusName.ToLowerInvariant());
                    var prevStacks = damageDownStacks.GetValueOrDefault(key);
                    var delta = newStacks - prevStacks;
                    damageDownCounts[targetName] = damageDownCounts.GetValueOrDefault(targetName) + Math.Max(delta, 1);
                    damageDownStacks[key] = newStacks;
                }
            }
        }
        else if (type == "30")
        {
            // LosesEffect
            var removalTime = timer?.ElapsedSeconds ?? 0f;
            statusTracker.OnStatusLost(sourceName, targetName, statusId, removalTime);

            if (SkillIssueNames.Contains(statusName))
            {
                lock (syncLock)
                {
                    skillIssueStacks.Remove((targetName.ToLowerInvariant(), statusName.ToLowerInvariant()));
                }
            }

            if (DamageDownNames.Contains(statusName))
            {
                lock (syncLock)
                {
                    damageDownStacks.Remove((targetName.ToLowerInvariant(), statusName.ToLowerInvariant()));
                }
            }
        }
    }

    /// <summary>
    /// Parse ACT log line type 24 (DoTHoT) — periodic damage/heal ticks.
    ///
    /// Verified IINACT field layout:
    ///   [0]=type, [1]=timestamp, [2]=targetId(hex), [3]=targetName,
    ///   [4]="DoT"/"HoT", [5]=effectId(?), [6]=amount(hex),
    ///   [7]=targetCurrentHP, [8]=targetMaxHP, ...
    ///   [17]=sourceId(hex), [18]=sourceName, ...
    /// </summary>
    private void ProcessDoTHoTLine(string[] line)
    {
        // Need at least 19 fields to read sourceName at [18]
        if (line.Length < 19)
            return;

        var targetName = line[3];
        var dotOrHot = line[4];       // "DoT" or "HoT"
        var amountHex = line[6];
        var sourceName = line[18];

        if (string.IsNullOrEmpty(sourceName) || string.IsNullOrEmpty(amountHex))
            return;

        if (!long.TryParse(amountHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
            return;

        bool isHoT = string.Equals(dotOrHot, "HoT", StringComparison.OrdinalIgnoreCase);
        bool isDoT = !isHoT;

        // Resolve the status name(s) and originating action from StatusTracker.
        // Type 24 doesn't include the status name, so we look up
        // what DoT/HoT the source currently has on the target.
        // IMPORTANT: Type 24 lines are AGGREGATED — one line per (source, target)
        // combining ALL DoTs/HoTs from that source. We must split the damage
        // evenly across all matching statuses.
        var matchedSkills = new List<string>();
        if (statusTracker != null)
        {
            var statuses = statusTracker.GetActiveStatuses(targetName);
            foreach (var s in statuses)
            {
                if (!string.Equals(s.SourceName, sourceName, StringComparison.OrdinalIgnoreCase))
                    continue;
                if ((isDoT && s.IsDoT) || (isHoT && s.IsHoT))
                {
                    var name = s.ApplyingActionName ?? s.StatusName;
                    matchedSkills.Add(name);
                }
            }
        }

        // Fallback: if no matching status was found, use a generic label.
        if (matchedSkills.Count == 0)
            matchedSkills.Add(isHoT ? "HoT" : "DoT");

        // Split the aggregated tick amount evenly across all matched DoTs/HoTs.
        var perSkillAmount = amount / matchedSkills.Count;
        var remainder = amount - perSkillAmount * matchedSkills.Count;

        lock (syncLock)
        {
            for (int i = 0; i < matchedSkills.Count; i++)
            {
                var skillName = matchedSkills[i];
                // Give any integer-division remainder to the first skill.
                var share = perSkillAmount + (i == 0 ? remainder : 0);
                if (share <= 0) continue;

                if (isDoT)
                {
                    AccumulateSkill(damageData, sourceName, skillName, share, 0, SkillDamageType.Magic);
                    AccumulateSkill(dotTickData, sourceName, skillName, share, 0, SkillDamageType.Magic);
                    RecordEvent(sourceName, skillName, share, false, 0, isDoTTick: true);

                    if (!string.IsNullOrEmpty(targetName))
                        RecordDamageTakenEvent(targetName, skillName, share, 0);
                }
                else
                {
                    AccumulateSkill(healData, sourceName, skillName, share, 0, SkillDamageType.Magic);
                    AccumulateSkill(hotTickData, sourceName, skillName, share, 0, SkillDamageType.Magic);
                    RecordEvent(sourceName, skillName, share, true, 0, isHoTTick: true);
                }
            }
        }

        // Feed into graph tracker for sliding-window DPS/HPS
        if (isDoT)
            graphTracker?.RecordLogLineEvent(sourceName, amount, 0);
        else
            graphTracker?.RecordLogLineEvent(sourceName, 0, amount);
    }
}
