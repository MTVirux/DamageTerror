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

    // Cache action ID -> damage type to avoid repeated Lumina lookups
    private readonly ConcurrentDictionary<uint, SkillDamageType> damageTypeCache = new();
    private readonly IDataManager dataManager;

    public SkillTracker(IDataManager dataManager)
    {
        this.dataManager = dataManager;
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
        if (line.Length < 10)
            return;

        var type = line[0];
        if (type != "21" && type != "22")
            return;

        var sourceName = line[3];
        var skillName = string.Equals(line[5], "Attack", StringComparison.OrdinalIgnoreCase) ? "Auto Attack" : line[5];

        if (string.IsNullOrEmpty(sourceName) || string.IsNullOrEmpty(skillName))
            return;

        // Resolve damage type from action ID via Lumina
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
                AccumulateSkill(damageData, sourceName, skillName, dmgAmount, dmgSeverity, damageType);
            if (healAmount > 0)
                AccumulateSkill(healData, sourceName, skillName, healAmount, healSeverity, damageType);
        }
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

        // Keep the first resolved damage type
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
        catch
        {
            // Lumina lookup failure — leave as Unknown
        }

        damageTypeCache[actionId] = result;
        return result;
    }

    public List<SkillEntry> GetSkills(string combatantName)
    {
        return BuildSkillList(damageData, combatantName);
    }

    public List<SkillEntry> GetHealSkills(string combatantName)
    {
        return BuildSkillList(healData, combatantName);
    }

    public void Reset()
    {
        lock (syncLock)
        {
            damageData.Clear();
            healData.Clear();
            damageTypeCache.Clear();
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
                    DamageType = a.DamageType,
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
}
