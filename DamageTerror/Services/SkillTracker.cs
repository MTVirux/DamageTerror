using System.Globalization;

namespace DamageTerror.Services;

/// <summary>
/// Tracks per-skill damage from LogLine events (network ability types 21/22).
/// Thread-safe: LogLine events arrive from background threads, UI reads skills on main thread.
/// </summary>
public class SkillTracker
{
    private readonly object syncLock = new();

    // combatantName -> skillName -> accumulated hit statistics
    private Dictionary<string, Dictionary<string, SkillAccum>> damageData = new();
    private Dictionary<string, Dictionary<string, SkillAccum>> healData = new();

    private struct SkillAccum
    {
        public long Amount;
        public int Hits;
        public int Crits;
        public int DirectHits;
        public int CritDirectHits;
    }

    /// <summary>
    /// Process a parsed LogLine event. Only handles type 21 (NetworkAbility)
    /// and type 22 (NetworkAOEAbility) for damage and healing tracking.
    /// </summary>
    public void ProcessLogLine(string[] line)
    {
        if (line.Length < 10)
            return;

        var type = line[0];
        if (type != "21" && type != "22")
            return;

        var sourceName = line[3];
        var skillName = string.Equals(line[5], "Attack", StringComparison.OrdinalIgnoreCase) ? "Auto Attack" : line[5];

        if (string.IsNullOrEmpty(sourceName) || string.IsNullOrEmpty(skillName))
            return;

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
                // Heal — take the first heal found
                if (healAmount == 0)
                {
                    healAmount = result.Amount;
                    healSeverity = result.Severity;
                }
            }
            else if (dmgAmount == 0)
            {
                // Damage (3/5/6) — take the first damage found
                dmgAmount = result.Amount;
                dmgSeverity = result.Severity;
            }
        }

        if (dmgAmount <= 0 && healAmount <= 0)
            return;

        lock (syncLock)
        {
            if (dmgAmount > 0)
                AccumulateSkill(damageData, sourceName, skillName, dmgAmount, dmgSeverity);
            if (healAmount > 0)
                AccumulateSkill(healData, sourceName, skillName, healAmount, healSeverity);
        }
    }

    private void AccumulateSkill(Dictionary<string, Dictionary<string, SkillAccum>> store,
        string sourceName, string skillName, long amount, byte severity)
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

        skills[skillName] = existing;
    }

    /// <summary>
    /// Get the accumulated damage skill data for a combatant, sorted by damage descending.
    /// </summary>
    public List<SkillEntry> GetSkills(string combatantName)
    {
        return BuildSkillList(damageData, combatantName);
    }

    /// <summary>
    /// Get the accumulated healing skill data for a combatant, sorted by healing descending.
    /// </summary>
    public List<SkillEntry> GetHealSkills(string combatantName)
    {
        return BuildSkillList(healData, combatantName);
    }

    /// <summary>
    /// Clear all accumulated skill data (called on new encounter boundary).
    /// </summary>
    public void Reset()
    {
        lock (syncLock)
        {
            damageData.Clear();
            healData.Clear();
        }
    }

    private List<SkillEntry> BuildSkillList(Dictionary<string, Dictionary<string, SkillAccum>> store, string combatantName)
    {
        lock (syncLock)
        {
            if (!store.TryGetValue(combatantName, out var skills))
                return new List<SkillEntry>();

            var list = skills.Select(kv =>
            {
                var a = kv.Value;
                var entry = new SkillEntry
                {
                    Name = kv.Key,
                    TotalDamage = a.Amount,
                    HitCount = a.Hits,
                };
                if (a.Hits > 0)
                {
                    entry.CritPct = (double)(a.Crits + a.CritDirectHits) / a.Hits * 100.0;
                    entry.DirectHitPct = (double)(a.DirectHits + a.CritDirectHits) / a.Hits * 100.0;
                    entry.CritDirectHitPct = (double)a.CritDirectHits / a.Hits * 100.0;
                }
                return entry;
            }).OrderByDescending(s => s.TotalDamage).ToList();

            var total = list.Sum(s => s.TotalDamage);
            if (total > 0)
            {
                foreach (var s in list)
                    s.DamagePercent = (double)s.TotalDamage / total * 100.0;
            }

            return list;
        }
    }

    /// <summary>
    /// Decode an ability effect from FFXIV network log line fields.
    /// Returns the amount (damage or healing), severity byte, and effect type.
    /// See: https://github.com/OverlayPlugin/cactbot/blob/main/docs/LogGuide.md#ability-damage
    /// </summary>
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
}
