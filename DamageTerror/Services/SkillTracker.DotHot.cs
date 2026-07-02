using System.Globalization;

namespace DamageTerror.Services;

public sealed partial class SkillTracker
{
    /// <summary>
    /// Accumulate per-combatant damage stats for DoT/HoT tick simulation.
    /// Strips crit/DH from observed damage to estimate the base per-potency
    /// multiplier, tracks running crit/DH rates, and builds DH-stripped pools
    /// for dynamic crit multiplier estimation.
    /// Must be called under syncLock.
    /// </summary>
    private void AccumulateCombatantStats(string sourceName, long amount, byte severity)
    {
        bool isCrit = (severity & CritFlag) != 0;
        bool isDH = (severity & DirectHitFlag) != 0;

        var stats = combatantDotStats.GetValueOrDefault(sourceName);

        // Use dynamic crit multiplier when available, otherwise fall back to default.
        var critMulti = stats.DynamicCritMulti > 0 ? stats.DynamicCritMulti : DefaultCritMulti;

        // Strip crit/DH multipliers to estimate base damage
        double baseDmg = amount;
        if (isCrit) baseDmg /= critMulti;
        if (isDH) baseDmg /= 1.25;

        // Outlier filter: exclude hits outside 30%-300% of running average
        // to avoid potency spikes from skewing the per-potency estimate.
        // Skip filter when under 10 samples (similar to ACT's <50 swings rule).
        if (stats.TotalHits >= DotMinHitsForOutlierFilter)
        {
            var currentAvg = stats.AverageBaseDmgPerHit;
            if (currentAvg > 0 && (baseDmg < currentAvg * DotOutlierLowThreshold || baseDmg > currentAvg * DotOutlierHighThreshold))
                return;
        }

        stats.TotalBaseDamage += baseDmg;
        stats.TotalHits++;
        if (isCrit) stats.CritHits++;
        if (isDH) stats.DHHits++;

        // DH-stripped pools for dynamic crit multiplier estimation.
        // Crit damage is preserved so we can derive critMulti = avgCrit / avgNonCrit.
        double dhStripped = amount;
        if (isDH) dhStripped /= 1.25;
        if (isCrit)
        {
            stats.CritDHStripped += dhStripped;
            stats.CritCountForMulti++;
        }
        else
        {
            stats.NonCritDHStripped += dhStripped;
            stats.NonCritCount++;
        }

        combatantDotStats[sourceName] = stats;
    }

    /// <summary>
    /// Calibrate per-source damage-per-potency-point coefficient from a DoT initial hit.
    /// When we see a type 21/22 line that deals damage AND applies a known DoT whose
    /// initial hit potency is in DotPotencyTable, we can derive a clean coefficient.
    /// Must be called under syncLock.
    /// </summary>
    private void CalibrateFromDotHit(string sourceName, long amount, byte severity, int initialPotency)
    {
        if (initialPotency <= 0 || amount <= 0)
            return;

        var stats = combatantDotStats.GetValueOrDefault(sourceName);

        var critMulti = stats.DynamicCritMulti > 0 ? stats.DynamicCritMulti : DefaultCritMulti;

        double baseDmg = amount;
        if ((severity & CritFlag) != 0) baseDmg /= critMulti;
        if ((severity & DirectHitFlag) != 0) baseDmg /= 1.25;

        stats.CalibrationSum += baseDmg / initialPotency;
        stats.CalibrationCount++;
        combatantDotStats[sourceName] = stats;
    }

    /// <summary>
    /// Calculate the simulated tick weight for a source's DoT/HoT on a target.
    /// Weight = baseDmgPerHit × tickPotency × expectedCritDHFactor.
    /// Used to proportionally distribute aggregate type-24 tick damage.
    /// Must be called under syncLock.
    /// </summary>
    private double CalculateTickWeight(string sourceName, uint statusId, bool isHoT,
        byte damageLowByte, byte critLowByte, bool hasLowByteData, double fallbackCoeff)
    {
        var potency = DotPotencyTable.GetTickPotency(statusId);
        if (potency <= 0)
            return 0;

        if (!combatantDotStats.TryGetValue(sourceName, out var stats) || !stats.HasData)
            return (fallbackCoeff > 0 ? fallbackCoeff : 1.0) * potency;

        // Use calibrated per-potency coefficient when available, otherwise
        // fall back to the shared coefficient from other calibrated sources.
        // IMPORTANT: AverageBaseDmgPerHit is NOT per-potency — using it raw
        // would create ~100x weight distortion vs calibrated sources.
        double estimatedTick;
        if (stats.HasCalibration)
            estimatedTick = stats.CalibratedCoeff * potency;
        else if (fallbackCoeff > 0)
            estimatedTick = fallbackCoeff * potency;
        else
            return potency; // No calibration anywhere — weight by potency alone

        // Refined mode: snap estimated tick to the nearest value whose LSB
        // matches the low-byte from the 0x0E status-application effect.
        if (config.DotCalcMode != DotCalcMode.Iinact && hasLowByteData && estimatedTick > 0)
        {
            estimatedTick = SnapToLowByte(estimatedTick, damageLowByte);
        }

        // Use dynamic crit multiplier when available.
        var critMulti = stats.DynamicCritMulti > 0 ? stats.DynamicCritMulti : DefaultCritMulti;

        // Expected crit/DH multiplier applied to periodic ticks.
        double critRate = stats.CritRate;

        // Refined mode: use per-application crit rate from low-byte when available
        // and within the reliable range (≤ 25.5%, byte overflow threshold).
        if (config.DotCalcMode != DotCalcMode.Iinact && hasLowByteData && critLowByte > 0)
        {
            var perAppCrit = critLowByte / 1000.0;
            if (perAppCrit <= 0.255)
                critRate = perAppCrit;
        }

        double critFactor = 1.0 + (critMulti - 1.0) * critRate;
        double dhFactor = isHoT ? 1.0 : 1.0 + 0.25 * stats.DHRate;

        return estimatedTick * critFactor * dhFactor;
    }

    /// <summary>
    /// Snap an estimated tick value to the nearest integer whose least significant
    /// byte matches <paramref name="lowByte"/>. Used by the Refined DoT simulation
    /// to leverage the damage low-byte from 0x0E status-application effects.
    /// </summary>
    private static double SnapToLowByte(double estimated, byte lowByte)
    {
        // The LSB constrains: result & 0xFF == lowByte.
        // Candidates are: base | lowByte, (base+256) | lowByte, (base-256) | lowByte
        int estInt = (int)estimated;
        int baseVal = estInt & ~0xFF; // clear bottom byte

        double bestDist = double.MaxValue;
        double bestVal = estimated;

        for (int offset = -256; offset <= 256; offset += 256)
        {
            int candidate = (baseVal + offset) | lowByte;
            if (candidate <= 0)
                continue;
            double dist = Math.Abs(candidate - estimated);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestVal = candidate;
            }
        }

        return bestVal;
    }

    /// <summary>
    /// Parse ACT log line type 24 (DoTHoT) — periodic damage/heal ticks.
    ///
    /// Type 24 lines are AGGREGATED — one line per tick combining damage from
    /// ALL DoTs/HoTs on the target from ALL sources. We simulate individual tick
    /// amounts using a per-potency multiplier approach inspired by the
    /// FFXIV_ACT_Plugin (see wiki link below) and distribute proportionally.
    ///
    /// https://github.com/ravahn/FFXIV_ACT_Plugin/wiki/DoT---HoT-Simulation-details
    ///
    /// Verified IINACT field layout:
    ///   [0]=type, [1]=timestamp, [2]=targetId(hex), [3]=targetName,
    ///   [4]="DoT"/"HoT", [5]=effectId(?), [6]=amount(hex),
    ///   [7]=targetCurrentHP, [8]=targetMaxHP, ...
    ///   [17]=sourceId(hex), [18]=sourceName, ...
    /// </summary>
    private void ProcessDoTHoTLine(string[] line)
    {
        // Need at least 7 fields to read amount; 19 to read sourceName.
        if (line.Length < 7)
            return;

        var targetName = line[3];
        var dotOrHot = line[4];       // "DoT" or "HoT"
        var effectIdHex = line[5];    // Non-zero for ground-effect DoTs/HoTs
        var amountHex = line[6];
        var sourceName = line.Length > 18 ? line[18] : string.Empty;

        if (string.IsNullOrEmpty(amountHex))
            return;

        if (!long.TryParse(amountHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
            return;

        bool isHoT = string.Equals(dotOrHot, "HoT", StringComparison.OrdinalIgnoreCase);
        bool isDoT = !isHoT;

        #if DEBUG
            dotHotLineCount++;
            if (isDoT) dotLineCount++;
        #endif

        // Ground-effect lines have a non-zero effectId and carry damage for
        // that one specific ground effect only.  Regular (aggregate) lines
        // have effectId=0 and carry the combined damage of ALL status-effect
        // DoTs/HoTs on the target from ALL sources.
        long eid = 0;
        bool isGroundEffect = !string.IsNullOrEmpty(effectIdHex)
                              && effectIdHex != "0"
                              && long.TryParse(effectIdHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out eid)
                              && eid != 0;

        var sourceSkills = new Dictionary<string, List<(string Name, uint StatusId, byte DamageLowByte, byte CritLowByte, bool HasLowByteData)>>(StringComparer.OrdinalIgnoreCase);

#if DEBUG
        if (isGroundEffect) dotGroundEffectCount++;
        else if (isDoT) dotAggregateCount++;
#endif

        if (statusTracker != null)
        {
            if (isGroundEffect)
            {
                // Non-DoT status detonation (e.g. Wildfire): attribute as
                // direct damage from the source, not as a periodic tick.
                var effectStatusId = (uint)eid;
                if (!statusTracker.IsDoT(effectStatusId)
                    && !statusTracker.IsHoT(effectStatusId)
                    && !statusTracker.IsGroundEffectDot(effectStatusId))
                {
                    if (isDoT && !string.IsNullOrEmpty(sourceName))
                    {
                        var skillName = ResolveStatusName(effectStatusId, targetName);
                        var damageType = LookupStatusDamageType(effectStatusId);
                        lock (syncLock)
                        {
                            AccumulateSkill(damageData, sourceName, skillName, amount, 0, damageType);
                            RecordEvent(sourceName, skillName, amount, false, 0);
                            if (!string.IsNullOrEmpty(targetName))
                                RecordDamageTakenEvent(targetName, skillName, amount, 0);
                        }
                        graphTracker?.RecordLogLineEvent(sourceName, amount, 0);
                    }
                    return;
                }

                // Ground-effect line: attribute only to the source's ground
                // effect skill.  The sourceName IS meaningful here.
                if (isDoT && !string.IsNullOrEmpty(sourceName))
                {
                    var groundEffects = statusTracker.GetActiveGroundEffectDots(sourceName);
                    foreach (var (skillName, statusId2) in groundEffects)
                    {
                        if (!sourceSkills.TryGetValue(sourceName, out var skills))
                            sourceSkills[sourceName] = skills = new List<(string, uint, byte, byte, bool)>();
                        if (!skills.Any(e => e.Name == skillName))
                            skills.Add((skillName, statusId2, 0, 0, false));
                    }
                }
            }
            else
            {
                // Aggregate line: collect ALL sources' status-effect DoTs/HoTs
                // on this target.  The sourceName field on aggregate lines
                // rotates arbitrarily and must NOT be used to filter.
                var statuses = statusTracker.GetActiveStatuses(targetName);
#if DEBUG
                if (isDoT && dotAggregateCount <= 5)
                    ServiceManager.LogDebug(LogChannel.DoTDiag, $"[DoTDiag] Aggregate #{dotAggregateCount}: target={targetName} amt={amount} activeStatuses={statuses.Count} dotStatuses={statuses.Count(s => s.IsDoT)}");
#endif
                foreach (var s in statuses)
                {
                    if ((isDoT && s.IsDoT) || (isHoT && s.IsHoT))
                    {
                        var name = s.ApplyingActionName ?? s.StatusName;
                        if (!sourceSkills.TryGetValue(s.SourceName, out var skills))
                            sourceSkills[s.SourceName] = skills = new List<(string, uint, byte, byte, bool)>();
                        skills.Add((name, s.StatusId, s.DamageLowByte, s.CritLowByte, s.HasLowByteData));
                    }
                }

                // Grace period buffer for recently-removed statuses.
                var recentlyRemoved = statusTracker.GetRecentlyRemovedDoTs(targetName);
                foreach (var s in recentlyRemoved)
                {
                    if ((isDoT && s.IsDoT) || (isHoT && s.IsHoT))
                    {
                        var name = s.ApplyingActionName ?? s.StatusName;
                        if (sourceSkills.TryGetValue(s.SourceName, out var existingSkills)
                            && existingSkills.Any(e => e.Name == name))
                            continue;

                        if (!sourceSkills.TryGetValue(s.SourceName, out var skills))
                            sourceSkills[s.SourceName] = skills = new List<(string, uint, byte, byte, bool)>();
                        skills.Add((name, s.StatusId, s.DamageLowByte, s.CritLowByte, s.HasLowByteData));
                    }
                }
            }
        }

#if DEBUG
        if (sourceSkills.Count > 0 && isDoT) dotStatusFoundCount++;
#endif

        // Fallback: attribute to the named source with a generic label when
        // StatusTracker has no data (e.g. plugin started mid-encounter).
        if (sourceSkills.Count == 0)
        {
            // If the target is a player entity, this is an enemy DoT/HoT ticking
            // on a player — record as damage taken only, not as damage dealt/healed.
            var targetId = line[2];
            if (IsPlayerEntity(targetId))
            {
#if DEBUG
                dotFallbackCount++;
#endif
                if (isDoT)
                {
                    lock (syncLock)
                    {
                        if (!string.IsNullOrEmpty(targetName))
                            RecordDamageTakenEvent(targetName, "DoT", amount, 0);
                    }
                }
                return;
            }

#if DEBUG
            dotFallbackCount++;
#endif
            var fallbackSource = !string.IsNullOrEmpty(sourceName) ? sourceName : targetName;
            sourceSkills[fallbackSource] = new List<(string, uint, byte, byte, bool)> { (isHoT ? "HoT" : "DoT", 0u, 0, 0, false) };
        }

        // Potency-weighted distribution: simulate each DoT/HoT's expected tick
        // amount using per-combatant stats and tick potency, then distribute the
        // aggregate damage proportionally to the simulated weights.
        var weightedSlots = new List<(string Source, string SkillName, double Weight)>();
        var sourceWeights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        lock (syncLock)
        {
            // Compute an average calibrated per-potency coefficient from sources
            // that have calibration data.  Sources without calibration will use
            // this shared coefficient so all weights stay in the same units.
            double calibSum = 0;
            int calibCount = 0;
            foreach (var (src, _) in sourceSkills)
            {
                if (combatantDotStats.TryGetValue(src, out var s) && s.HasCalibration)
                {
                    calibSum += s.CalibratedCoeff;
                    calibCount++;
                }
            }
            double fallbackCoeff = calibCount > 0 ? calibSum / calibCount : 0;

            // Phase 1: calculate weights for each source × skill slot.
            foreach (var (src, skills) in sourceSkills)
            {
                foreach (var (skillName, statusId, dLB, cLB, hasLB) in skills)
                {
                    var weight = CalculateTickWeight(src, statusId, isHoT, dLB, cLB, hasLB, fallbackCoeff);
                    weightedSlots.Add((src, skillName, weight));

                    sourceWeights[src] = sourceWeights.GetValueOrDefault(src) + weight;
                }
            }

            var totalWeight = weightedSlots.Sum(s => s.Weight);
            if (totalWeight <= 0) totalWeight = 1;

            // Phase 2: distribute the aggregate damage proportionally.
            long distributed = 0;
            for (int i = 0; i < weightedSlots.Count; i++)
            {
                var (src, skillName, weight) = weightedSlots[i];

                // Last slot gets remainder to ensure total matches exactly.
                long share;
                if (i == weightedSlots.Count - 1)
                    share = amount - distributed;
                else
                    share = (long)(amount * weight / totalWeight);

                distributed += share;
                if (share <= 0) continue;

                if (isDoT)
                {
                    AccumulateSkill(damageData, src, skillName, share, 0, SkillDamageType.Magic);
                    AccumulateSkill(dotTickData, src, skillName, share, 0, SkillDamageType.Magic);
                    RecordEvent(src, skillName, share, false, 0, isDoTTick: true);
#if DEBUG
                    dotTotalDamageDistributed += share;
#endif

                    if (!string.IsNullOrEmpty(targetName))
                        RecordDamageTakenEvent(targetName, skillName, share, 0);
                }
                else
                {
                    AccumulateSkill(healData, src, skillName, share, 0, SkillDamageType.Magic);
                    AccumulateSkill(hotTickData, src, skillName, share, 0, SkillDamageType.Magic);
                    RecordEvent(src, skillName, share, true, 0, isHoTTick: true);
                }
            }
        }

        // Feed into graph tracker — split per source proportionally to weights.
        var totalSourceWeight = sourceWeights.Sum(kv => kv.Value);
        if (totalSourceWeight <= 0) totalSourceWeight = 1;

        long graphDistributed = 0;
        var sourceList = sourceWeights.ToList();
        for (int i = 0; i < sourceList.Count; i++)
        {
            var (src, weight) = sourceList[i];
            long srcShare;
            if (i == sourceList.Count - 1)
                srcShare = amount - graphDistributed;
            else
                srcShare = (long)(amount * weight / totalSourceWeight);

            graphDistributed += srcShare;
            if (srcShare <= 0) continue;

            if (isDoT)
                graphTracker?.RecordLogLineEvent(src, srcShare, 0);
            else
                graphTracker?.RecordLogLineEvent(src, 0, srcShare);
        }
    }
}
