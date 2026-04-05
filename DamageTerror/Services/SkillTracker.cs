using System.Collections.Concurrent;
using System.Globalization;
using Dalamud.Plugin.Services;

namespace DamageTerror.Services;

public class SkillTracker
{
    private readonly object syncLock = new();

    // combatantName -> skillName -> accumulated hit statistics
    private Dictionary<string, Dictionary<string, SkillAccum>> damageData = new();
    private Dictionary<string, Dictionary<string, SkillAccum>> healData = new();

    // Tick-only accumulators keyed by originating action name (for sub-entry display).
    private Dictionary<string, Dictionary<string, SkillAccum>> dotTickData = new();
    private Dictionary<string, Dictionary<string, SkillAccum>> hotTickData = new();

    // Timestamped skill use events per combatant (source-side: damage dealt / heals cast)
    private readonly Dictionary<string, List<SkillUseEvent>> skillEvents = new(StringComparer.OrdinalIgnoreCase);

    // Timestamped damage-taken events per combatant (target-side: enemy abilities hitting a player)
    private readonly Dictionary<string, List<SkillUseEvent>> damageTakenEvents = new(StringComparer.OrdinalIgnoreCase);

    // Stun skill use counts per combatant (Leg Sweep + Low Blow only)
    private readonly Dictionary<string, int> stunCounts = new(StringComparer.OrdinalIgnoreCase);

    // Historical skill events loaded from disk, used as fallback until live data is available.
    private Dictionary<string, List<SkillUseEvent>>? seededEvents;
    private Dictionary<string, List<SkillUseEvent>>? seededDamageTakenEvents;

    // Shared encounter timer — same time base as GraphDataTracker
    private EncounterTimer? timer;

    // Graph tracker to feed high-resolution LogLine damage/heal totals into
    private GraphDataTracker? graphTracker;

    // Status tracker for DoT/HoT lifecycle correlation
    private StatusTracker? statusTracker;

    // Cache action ID -> damage type to avoid repeated Lumina lookups
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

        if (dmgAmount <= 0 && healAmount <= 0)
            return;

        lock (syncLock)
        {
            if (dmgAmount > 0)
            {
                AccumulateSkill(damageData, sourceName, skillName, dmgAmount, dmgSeverity, damageType);
                RecordEvent(sourceName, skillName, dmgAmount, false, dmgSeverity);

                if (!string.IsNullOrEmpty(targetName))
                    RecordDamageTakenEvent(targetName, skillName, dmgAmount, dmgSeverity);

                if (string.Equals(skillName, "Leg Sweep", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(skillName, "Low Blow", StringComparison.OrdinalIgnoreCase))
                    stunCounts[sourceName] = stunCounts.GetValueOrDefault(sourceName) + 1;
            }
            if (healAmount > 0)
            {
                AccumulateSkill(healData, sourceName, skillName, healAmount, healSeverity, damageType);
                RecordEvent(sourceName, skillName, healAmount, true, healSeverity);
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
            seededEvents = null;
            seededDamageTakenEvents = null;
            damageTypeCache.Clear();
        }
    }

    private void RecordEvent(string combatantName, string skillName, long amount, bool isHeal, byte severity,
        bool isDoTTick = false, bool isHoTTick = false)
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
            Amount = amount,
            IsHeal = isHeal,
            IsCrit = (severity & 0x20) != 0,
            IsDirectHit = (severity & 0x40) != 0,
            IsDoTTick = isDoTTick,
            IsHoTTick = isHoTTick,
        });
    }

    /// <summary>
    /// Retroactively tag the most recent skill event from a combatant as a DoT/HoT application.
    /// Called by StatusTracker when a GainsEffect is received for a known DoT/HoT status,
    /// since type 26 fires immediately after the type 21/22 that applied it.
    /// Returns the action name of the tagged event, or null if no match.
    /// </summary>
    public string? MarkLastEventAsApplication(string combatantName, bool isDoT, bool isHoT)
    {
        lock (syncLock)
        {
            if (!skillEvents.TryGetValue(combatantName, out var events) || events.Count == 0)
                return null;

            var last = events[events.Count - 1];
            // Only tag if it's a very recent event (within ~1s) and not already a tick
            if (last.IsDoTTick || last.IsHoTTick)
                return null;

            var now = timer?.ElapsedSeconds ?? 0f;
            if (now - last.TimeSec > 1.0f)
                return null;

            if (isDoT) last.IsDoTApplication = true;
            if (isHoT) last.IsHoTApplication = true;
            events[events.Count - 1] = last;
            return last.SkillName;
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
    /// Type 26 (GainsEffect) field layout:
    ///   [0]=type, [1]=timestamp, [2]=targetId, [3]=targetName,
    ///   [4]=statusName, [5]=statusId(hex), [6]=duration(float),
    ///   [7]=sourceId, [8]=sourceName, ...
    ///
    /// Type 30 (LosesEffect) field layout:
    /// Parse ACT log line types 26 (GainsEffect) and 30 (LosesEffect)
    /// and forward to the StatusTracker for DoT/HoT lifecycle tracking.
    ///
    /// Actual IINACT field layout (verified in-game):
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
        }
        else if (type == "30")
        {
            // LosesEffect
            var removalTime = timer?.ElapsedSeconds ?? 0f;
            statusTracker.OnStatusLost(sourceName, targetName, statusId, removalTime);
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

        // Resolve the status name and originating action from StatusTracker.
        // Type 24 doesn't include the status name, so we look up
        // what DoT/HoT the source currently has on the target.
        string skillName = isHoT ? "HoT" : "DoT";
        string? actionName = null;
        if (statusTracker != null)
        {
            var statuses = statusTracker.GetActiveStatuses(targetName);
            foreach (var s in statuses)
            {
                if (!string.Equals(s.SourceName, sourceName, StringComparison.OrdinalIgnoreCase))
                    continue;
                if ((isDoT && s.IsDoT) || (isHoT && s.IsHoT))
                {
                    actionName = s.ApplyingActionName;
                    skillName = actionName ?? s.StatusName;
                    break;
                }
            }
        }

        lock (syncLock)
        {
            if (isDoT)
            {
                AccumulateSkill(damageData, sourceName, skillName, amount, 0, SkillDamageType.Magic);
                AccumulateSkill(dotTickData, sourceName, skillName, amount, 0, SkillDamageType.Magic);
                RecordEvent(sourceName, skillName, amount, false, 0, isDoTTick: true);

                if (!string.IsNullOrEmpty(targetName))
                    RecordDamageTakenEvent(targetName, skillName, amount, 0);
            }
            else
            {
                AccumulateSkill(healData, sourceName, skillName, amount, 0, SkillDamageType.Magic);
                AccumulateSkill(hotTickData, sourceName, skillName, amount, 0, SkillDamageType.Magic);
                RecordEvent(sourceName, skillName, amount, true, 0, isHoTTick: true);
            }
        }

        // Feed into graph tracker for sliding-window DPS/HPS
        if (isDoT)
            graphTracker?.RecordLogLineEvent(sourceName, amount, 0);
        else
            graphTracker?.RecordLogLineEvent(sourceName, 0, amount);
    }
}
