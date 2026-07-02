using System.Globalization;

namespace DamageTerror.Services;

public sealed partial class SkillTracker
{
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

    /// <summary>Increments a status-stack counter (skill issue or damage down). Must NOT be called under <see cref="syncLock"/>.</summary>
    private void IncrementStatusStackCount(
        string targetName,
        string statusName,
        string[] line,
        Dictionary<string, int> counts,
        Dictionary<(string, string), int> stacks)
    {
        int newStacks = 1;
        if (line.Length > 9 && int.TryParse(line[9], out var parsed) && parsed > 0)
            newStacks = parsed;

        lock (syncLock)
        {
            var key = (targetName.ToLowerInvariant(), statusName.ToLowerInvariant());
            var prevStacks = stacks.GetValueOrDefault(key);
            var delta = newStacks - prevStacks;
            counts[targetName] = counts.GetValueOrDefault(targetName) + Math.Max(delta, 1);
            stacks[key] = newStacks;
        }
    }

    public int GetStunCount(string combatantName) => GetCountLocked(stunCounts, combatantName);
    public int GetSkillIssueCount(string combatantName) => GetCountLocked(skillIssueCounts, combatantName);
    public int GetDamageDownCount(string combatantName) => GetCountLocked(damageDownCounts, combatantName);
    public int GetPositionalHits(string combatantName) => GetCountLocked(positionalHitCounts, combatantName);
    public int GetPositionalMisses(string combatantName) => GetCountLocked(positionalMissCounts, combatantName);

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

        // IINACT field layout for type 26/30:
        //   [0]=type, [1]=timestamp, [2]=statusId(hex), [3]=statusName,
        //   [4]=duration, [5]=sourceId, [6]=sourceName,
        //   [7]=targetId, [8]=targetName, [9]=count, ...
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

            // Consume pending low-byte refinement data captured from the Type 21/22
            // ability line that applied this status.
            byte damageLB = 0, critLB = 0;
            bool hasLB = false;
            lock (syncLock)
            {
                var lbKey = (sourceName, targetName, statusId);
                if (pendingLowBytes.Remove(lbKey, out var lb))
                {
                    damageLB = lb.DamageLowByte;
                    critLB = lb.CritLowByte;
                    hasLB = true;
                }
            }

            statusTracker.OnStatusGained(sourceName, targetName, statusId, statusName, duration,
                damageLB, critLB, hasLB);

            if (SkillIssueNames.Contains(statusName))
                IncrementStatusStackCount(targetName, statusName, line, skillIssueCounts, skillIssueStacks);

            if (DamageDownNames.Contains(statusName))
                IncrementStatusStackCount(targetName, statusName, line, damageDownCounts, damageDownStacks);
        }
        else if (type == "30")
        {
            var removalTime = timer?.ElapsedSeconds ?? 0f;
            statusTracker.OnStatusLost(sourceName, targetName, statusId, removalTime);

            if (SkillIssueNames.Contains(statusName))
            {
                lock (syncLock)
                    skillIssueStacks.Remove((targetName.ToLowerInvariant(), statusName.ToLowerInvariant()));
            }

            if (DamageDownNames.Contains(statusName))
            {
                lock (syncLock)
                    damageDownStacks.Remove((targetName.ToLowerInvariant(), statusName.ToLowerInvariant()));
            }
        }
    }
}
