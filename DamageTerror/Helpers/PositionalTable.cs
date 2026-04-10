namespace DamageTerror.Helpers;

/// <summary>
/// Static lookup table for melee DPS positional actions.
/// Maps action IDs to the set of bonus-percent values that indicate a MISSED positional.
/// Detection approach from xivanalysis: if the observed bonus percent matches a known "miss" value,
/// the positional was missed; otherwise it was hit.
///
/// Bonus percent is extracted from the leftmost byte of the damage value field in ACT log lines 21/22.
/// See: https://github.com/OverlayPlugin/cactbot/blob/main/docs/LogGuide.md#ability-damage
///
/// Potency data verified against Dawntrail 7.x (xivanalysis action definitions).
/// </summary>
public static class PositionalTable
{
    /// <summary>
    /// Action ID → set of bonus-percent values that indicate a missed positional.
    /// 0 is always included (no combo / no bonus at all).
    /// Additional values are floor(100 × (1 − basePotency / comboPotency)) for combo-only variants.
    /// </summary>
    private static readonly Dictionary<uint, HashSet<int>> MissedBonusPercents = new()
    {
        // ── DRG ──
        // Chaotic Spring (rear) — combo 300 → 340 positional
        { 25772, new HashSet<int> { 0, 53 } },
        // Fang and Claw (flank) — combo 300 → 340 positional
        { 3554, new HashSet<int> { 0, 53 } },
        // Wheeling Thrust (rear) — combo 300 → 340 positional
        { 3556, new HashSet<int> { 0, 53 } },

        // ── MNK ──
        // Snap Punch (flank) — combo 250 → 310 positional (310−250=60, base 250, combo 310)
        { 56, new HashSet<int> { 0, 25 } },
        // Demolish (rear) — only combo variant, no separate positional miss percent distinguishable
        { 66, new HashSet<int> { 0 } },
        // Pouncing Coeurl (flank) — combo 300 → 370 positional
        { 36947, new HashSet<int> { 0, 22 } },

        // ── NIN ──
        // Aeolian Edge (rear) — combo 200 → 260 positional (base 200, combo only gives floor(100*(1-200/260))=23... 
        // xivanalysis computes: base potencies with no modifiers or COMBO-only; bonus potencies higher without POSITIONAL
        // From 7.x data: rear combo 380, non-rear combo 200→260. missedPercents = {0, 47}
        { 2255, new HashSet<int> { 0, 47 } },
        // Armor Crush (flank) — similar structure
        { 3563, new HashSet<int> { 0, 47 } },

        // ── SAM ──
        // Gekko (rear) — combo 300 → 340 positional
        { 7481, new HashSet<int> { 0, 53 } },
        // Kasha (flank) — combo 300 → 340 positional
        { 7482, new HashSet<int> { 0, 53 } },

        // ── RPR ──
        // Gibbet (flank) — 460 → 500 positional
        { 24382, new HashSet<int> { 0, 10 } },
        // Gallows (rear) — 460 → 500 positional
        { 24383, new HashSet<int> { 0, 10 } },
        // Executioner's Gibbet (flank) — 700 → 740 positional
        { 36970, new HashSet<int> { 0, 7 } },
        // Executioner's Gallows (rear) — 700 → 740 positional
        { 36971, new HashSet<int> { 0, 7 } },

        // ── VPR ──
        // These have many potency tiers due to venom/buff modifiers.
        // missedBonusPercents includes all floor(100*(1-base/bonus)) combos from xivanalysis.
        // Flanksting Strike (flank)
        { 34610, new HashSet<int> { 0, 22, 23, 38, 40, 52, 63 } },
        // Flanksbane Fang (flank)
        { 34611, new HashSet<int> { 0, 22, 23, 38, 40, 52, 63 } },
        // Hindsting Strike (rear)
        { 34612, new HashSet<int> { 0, 22, 23, 38, 40, 52, 63 } },
        // Hindsbane Fang (rear)
        { 34613, new HashSet<int> { 0, 22, 23, 38, 40, 52, 63 } },
        // Hunter's Coil (rear) — only combo variant
        { 34621, new HashSet<int> { 0 } },
        // Swiftskin's Coil (flank) — only combo variant
        { 34622, new HashSet<int> { 0 } },
    };

    /// <summary>Returns true if the given action ID is a known positional action.</summary>
    public static bool IsPositional(uint actionId) => MissedBonusPercents.ContainsKey(actionId);

    /// <summary>
    /// Returns true if the given bonus percent indicates a missed positional for this action.
    /// Returns false (treat as hit) if the action ID is not in the table.
    /// </summary>
    public static bool IsPositionalMiss(uint actionId, int bonusPercent)
    {
        if (MissedBonusPercents.TryGetValue(actionId, out var missedSet))
            return missedSet.Contains(bonusPercent);
        return false;
    }
}
