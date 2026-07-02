using System.Globalization;

namespace DamageTerror.Services;

public sealed partial class SkillTracker
{
    /// <summary>Returns the name of the first active reflect status on the target,
    /// or null if no known reflect status is active.</summary>
    private string? ResolveActiveReflectSkill(string targetName)
    {
        if (statusTracker == null)
            return null;

        foreach (var s in statusTracker.GetActiveStatuses(targetName))
        {
            if (ReflectStatusIds.Contains(s.StatusId))
                return s.StatusName;
        }
        return null;
    }

    private void AccumulateSkill(Dictionary<string, Dictionary<string, SkillAccum>> store,
        string sourceName, string skillName, long amount, byte severity, SkillDamageType damageType)
    {
        if (!store.TryGetValue(sourceName, out var skills))
            store[sourceName] = skills = new Dictionary<string, SkillAccum>();

        ApplyHit(skills, skillName, amount, severity, damageType);
    }

    private void AccumulatePetSkill(Dictionary<string, Dictionary<string, Dictionary<string, SkillAccum>>> store,
        string ownerName, string petName, string skillName, long amount, byte severity, SkillDamageType damageType)
    {
        if (!store.TryGetValue(ownerName, out var pets))
            store[ownerName] = pets = new Dictionary<string, Dictionary<string, SkillAccum>>(StringComparer.OrdinalIgnoreCase);

        if (!pets.TryGetValue(petName, out var skills))
            pets[petName] = skills = new Dictionary<string, SkillAccum>(StringComparer.OrdinalIgnoreCase);

        ApplyHit(skills, skillName, amount, severity, damageType);
    }

    private static void ApplyHit(Dictionary<string, SkillAccum> skills,
        string skillName, long amount, byte severity, SkillDamageType damageType)
    {
        bool isCrit = (severity & CritFlag) != 0;
        bool isDirectHit = (severity & DirectHitFlag) != 0;
        bool isCritDirectHit = isCrit && isDirectHit;

        var existing = skills.GetValueOrDefault(skillName);

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

    /// Decode an ability effect from FFXIV network log line fields.
    /// See: https://github.com/OverlayPlugin/cactbot/blob/main/docs/LogGuide.md#ability-damage
    private static (long Amount, byte Severity, byte EffectType, int BonusPercent) DecodeEffect(string flagsHex, string valueHex)
    {
        if (string.IsNullOrEmpty(flagsHex) || string.IsNullOrEmpty(valueHex))
            return (0, 0, 0, -1);

        if (!uint.TryParse(flagsHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var flags))
            return (0, 0, 0, -1);
        if (!uint.TryParse(valueHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var raw))
            return (0, 0, 0, -1);

        // Low byte of flags is the effect type:
        //   0x03 = damage dealt, 0x04 = heal, 0x05 = blocked damage, 0x06 = parried damage
        var effectType = (byte)(flags & 0xFF);
        if (effectType != 3 && effectType != 4 && effectType != 5 && effectType != 6)
            return (0, 0, 0, -1);

        // Second byte of flags is the severity (crit/DH):
        //   0x20 = crit, 0x40 = direct hit, 0x60 = crit direct hit
        var severity = (byte)((flags >> 8) & 0xFF);

        // Bonus percent: DamageInfoPlugin reads EffectEntry.param2 (byte 3 of the
        // 8-byte struct), which maps to the top byte of the FLAGS field in ACT logs.
        // The 8-byte EffectEntry is split as flagsHex=[type,param0,param1,param2] and
        // valueHex=[mult,flags,value]. param2 = (flagsHex >> 24) & 0xFF.
        // https://github.com/perchbirdd/DamageInfoPlugin
        int bonusPercent = (int)((flags >> 24) & 0xFF);

        // Value bytes (left-extended to 4 bytes): ABCD
        // Normal: damage is upper 16 bits (AB).
        // "A lot" (0x4000 mask in value field): damage reassembled as D-A-B.
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

        return (amount, severity, effectType, bonusPercent);
    }

    private bool TryParseAbilityLine(string[] line, out AbilityLineContext ctx)
    {
        ctx = default;
        if (line.Length < 10) return false;

        var sourceName = line[3];
        if (string.IsNullOrEmpty(sourceName))
            return false;

        var damageType = SkillDamageType.Unknown;
        uint actionId = 0;
        if (uint.TryParse(line[4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out actionId))
            damageType = LookupDamageType(actionId);

        var skillName = SkillNameOverrides.Apply(actionId, line[5]);
        if (string.IsNullOrEmpty(skillName))
            return false;

        var sourceId = line[2] ?? string.Empty;
        var targetName = line.Length > 7 ? line[7] : null;

        ctx = new AbilityLineContext
        {
            SourceId = sourceId,
            SourceName = sourceName,
            TargetName = targetName,
            SkillName = skillName,
            ActionId = actionId,
            DamageType = damageType,
        };
        return true;
    }

    private AbilityLineContext ResolvePetOrGroundEffect(AbilityLineContext ctx)
    {
        var sourceId = ctx.SourceId;
        var sourceName = ctx.SourceName;
        var skillName = ctx.SkillName;

        if (!string.IsNullOrEmpty(sourceId) && !string.IsNullOrEmpty(sourceName))
        {
            lock (syncLock)
                entityIdToName[sourceId] = sourceName;
        }

        string? petOwnerName = null;
        string? petEntityName = null;

        if (!string.IsNullOrEmpty(sourceId))
        {
            lock (syncLock)
            {
                if (petToOwnerId.TryGetValue(sourceId, out var ownerId)
                    && entityIdToName.TryGetValue(ownerId, out var ownerName))
                {
                    petOwnerName = ownerName;
                    petEntityName = sourceName;
                }
            }
        }

        if (petOwnerName == null && GroundEffectEntityNames.Contains(sourceName))
        {
            lock (syncLock)
            {
                if (groundEffectEntityOwners.TryGetValue(sourceName, out var owner))
                {
                    petOwnerName = owner;
                    petEntityName = sourceName;
                }
            }
        }
        else if (petOwnerName == null && GroundEffectEntityNames.Contains(skillName))
        {
            lock (syncLock)
            {
                groundEffectEntityOwners[skillName] = sourceName;
            }
        }

        return ctx with { PetOwnerName = petOwnerName, PetEntityName = petEntityName };
    }

    // Scan all 8 effect pairs (fields 8-23).
    // A single ability can have both damage and healing in different pairs
    // (e.g. drain abilities like Souleater, Energy Drain).
    // A second 0x03 damage effect is captured separately as a reflect
    // candidate (e.g. WAR's Damnation reflects incoming damage back).
    private AbilityEffectAmounts DecodeAbilityEffects(string[] line)
    {
        long dmgAmount = 0;
        byte dmgSeverity = 0;
        int dmgBonusPercent = -1;
        long healAmount = 0;
        byte healSeverity = 0;
        long reflectAmount = 0;
        byte reflectSeverity = 0;

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
                dmgBonusPercent = result.BonusPercent;
            }
            else if (reflectAmount == 0)
            {
                reflectAmount = result.Amount;
                reflectSeverity = result.Severity;
            }
        }

        return new AbilityEffectAmounts
        {
            Damage = dmgAmount,
            DamageSeverity = dmgSeverity,
            DamageBonusPercent = dmgBonusPercent,
            Heal = healAmount,
            HealSeverity = healSeverity,
            Reflect = reflectAmount,
            ReflectSeverity = reflectSeverity,
        };
    }

    // Scan for 0x0E/0x0F status-application effects to extract low-byte
    // refinement data (damage lowbyte + crit lowbyte) for DoT simulation,
    // and calibrate per-source damage-per-potency-point coefficients from
    // DoT initial hits.
    private void CalibrateStatusLowBytes(string[] line, in AbilityLineContext ctx, long dmgAmount, byte dmgSeverity)
    {
        if (config.DotCalcMode == DotCalcMode.Iinact) return;

        var sourceName = ctx.SourceName;
        var targetName = ctx.TargetName;

        for (int i = 0; i < 8; i++)
        {
            int flagIdx = 8 + i * 2;
            int valIdx = flagIdx + 1;
            if (valIdx >= line.Length)
                break;

            if (string.IsNullOrEmpty(line[flagIdx]) || string.IsNullOrEmpty(line[valIdx]))
                continue;

            if (!uint.TryParse(line[flagIdx], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var sFlags))
                continue;
            if (!uint.TryParse(line[valIdx], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var sRaw))
                continue;

            var sEffectType = (byte)(sFlags & 0xFF);
            if (sEffectType != 0x0E && sEffectType != 0x0F)
                continue;

            var appliedStatusId = (uint)((sRaw >> 16) & 0xFFFF);
            if (appliedStatusId == 0)
                continue;

            var damageLB = (byte)((sFlags >> 8) & 0xFF);
            var critLB = (byte)((sFlags >> 16) & 0xFF);

            var statusTarget = sEffectType == 0x0E ? targetName : sourceName;
            if (string.IsNullOrEmpty(statusTarget))
                continue;

            lock (syncLock)
            {
                pendingLowBytes[(sourceName, statusTarget, appliedStatusId)] = (damageLB, critLB);
            }

            if (dmgAmount > 0)
            {
                var initialPot = DotPotencyTable.GetInitialHitPotency(appliedStatusId);
                if (initialPot > 0)
                {
                    lock (syncLock)
                    {
                        CalibrateFromDotHit(sourceName, dmgAmount, dmgSeverity, initialPot);
                    }
                }
            }
        }
    }

    // Track positional hits/misses for known melee positional actions.
    // Uses CSV lookup table approach inspired by DamageInfoPlugin:
    // https://github.com/perchbirdd/DamageInfoPlugin
    private void RecordPositionalAndStun(in AbilityLineContext ctx, long dmgAmount, int dmgBonusPercent)
    {
        if (dmgAmount > 0 && dmgBonusPercent >= 0 && positionalTable.IsPositional(ctx.ActionId))
        {
            lock (syncLock)
            {
                if (positionalTable.IsPositionalMiss(ctx.ActionId, dmgBonusPercent))
                    positionalMissCounts[ctx.SourceName] = positionalMissCounts.GetValueOrDefault(ctx.SourceName) + 1;
                else
                    positionalHitCounts[ctx.SourceName] = positionalHitCounts.GetValueOrDefault(ctx.SourceName) + 1;
            }
        }

        // Count Leg Sweep / Low Blow uses regardless of whether they deal damage.
        if (string.Equals(ctx.SkillName, "Leg Sweep", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ctx.SkillName, "Low Blow", StringComparison.OrdinalIgnoreCase))
        {
            lock (syncLock)
                stunCounts[ctx.SourceName] = stunCounts.GetValueOrDefault(ctx.SourceName) + 1;
        }
    }

    // Pet-sourced skills go into separate pet dictionaries so they appear
    // as a named category in the skill breakdown instead of inline.
    private void AccumulateAbilitySkill(in AbilityLineContext ctx, long dmg, byte dmgSev, long heal, byte healSev)
    {
        if (ctx.PetOwnerName != null && ctx.PetEntityName != null)
        {
            ServiceManager.LogDebug(LogChannel.PetDebug,
                $"[PetDebug] PetAccum owner={ctx.PetOwnerName} pet={ctx.PetEntityName} skill={ctx.SkillName} dmg={dmg} heal={heal}");

            lock (syncLock)
            {
                if (dmg > 0)
                {
                    AccumulatePetSkill(petDamageData, ctx.PetOwnerName, ctx.PetEntityName, ctx.SkillName, dmg, dmgSev, ctx.DamageType);
                    RecordEvent(ctx.PetOwnerName, ctx.SkillName, dmg, false, dmgSev, ctx.TargetName);

                    if (!string.IsNullOrEmpty(ctx.TargetName))
                        RecordDamageTakenEvent(ctx.TargetName, ctx.SkillName, dmg, dmgSev);
                }
                if (heal > 0)
                {
                    AccumulatePetSkill(petHealData, ctx.PetOwnerName, ctx.PetEntityName, ctx.SkillName, heal, healSev, ctx.DamageType);
                    RecordEvent(ctx.PetOwnerName, ctx.SkillName, heal, true, healSev, ctx.TargetName);
                }
            }

            if (dmg > 0 || heal > 0)
                graphTracker?.RecordLogLineEvent(ctx.PetOwnerName, dmg, heal);

            return;
        }

        lock (syncLock)
        {
            if (dmg > 0)
            {
                AccumulateSkill(damageData, ctx.SourceName, ctx.SkillName, dmg, dmgSev, ctx.DamageType);
                RecordEvent(ctx.SourceName, ctx.SkillName, dmg, false, dmgSev, ctx.TargetName);

                if (!string.IsNullOrEmpty(ctx.TargetName))
                    RecordDamageTakenEvent(ctx.TargetName, ctx.SkillName, dmg, dmgSev);

                // Feed per-combatant stats for DoT/HoT tick simulation (exclude auto-attacks).
                if (!string.Equals(ctx.SkillName, "Auto Attack", StringComparison.OrdinalIgnoreCase))
                    AccumulateCombatantStats(ctx.SourceName, dmg, dmgSev);
            }
            if (heal > 0)
            {
                AccumulateSkill(healData, ctx.SourceName, ctx.SkillName, heal, healSev, ctx.DamageType);
                RecordEvent(ctx.SourceName, ctx.SkillName, heal, true, healSev, ctx.TargetName);
            }
        }

        // Feed high-resolution damage/heal totals into the graph tracker
        // outside the skill lock to avoid nested locking.
        if (dmg > 0 || heal > 0)
            graphTracker?.RecordLogLineEvent(ctx.SourceName, dmg, heal);
    }

    // A second damage effect on an enemy-on-player ability line is reflected
    // damage (e.g. Damnation). Re-attribute it to the line's target as the
    // active reflect-status skill, with the line's source as the new target.
    private void RecordReflectDamage(in AbilityLineContext ctx, long reflectAmount, byte reflectSeverity)
    {
        if (reflectAmount <= 0 || string.IsNullOrEmpty(ctx.TargetName))
            return;

        var reflectSkill = ResolveActiveReflectSkill(ctx.TargetName);
        if (reflectSkill == null)
            return;

        lock (syncLock)
        {
            AccumulateSkill(damageData, ctx.TargetName, reflectSkill, reflectAmount, reflectSeverity, SkillDamageType.Unknown);
            RecordEvent(ctx.TargetName, reflectSkill, reflectAmount, false, reflectSeverity, ctx.SourceName);
            RecordDamageTakenEvent(ctx.SourceName, reflectSkill, reflectAmount, reflectSeverity);
        }

        graphTracker?.RecordLogLineEvent(ctx.TargetName, reflectAmount, 0);
    }

    private void ProcessAbilityLine(string[] line)
    {
        if (!TryParseAbilityLine(line, out var ctx))
            return;

        ctx = ResolvePetOrGroundEffect(ctx);

        if (ctx.SkillName.StartsWith("item_", StringComparison.OrdinalIgnoreCase))
        {
            lock (syncLock)
                RecordItemEvent(ctx.SourceName, ctx.SkillName, ctx.TargetName);
            return;
        }

        statusTracker?.NotifyGroundEffectSkillUsed(ctx.SourceName, ctx.SkillName);

        var effects = DecodeAbilityEffects(line);

        CalibrateStatusLowBytes(line, ctx, effects.Damage, effects.DamageSeverity);
        RecordPositionalAndStun(ctx, effects.Damage, effects.DamageBonusPercent);

        if (!effects.HasDamageOrHeal)
        {
            // Reflect alone (no primary damage/heal) is not handled today.
            return;
        }

        AccumulateAbilitySkill(ctx, effects.Damage, effects.DamageSeverity, effects.Heal, effects.HealSeverity);

        // Reflect attribution does not run for pet-sourced lines (matches the original
        // ProcessLogLine where the pet branch returned before reaching the reflect block).
        if (ctx.PetOwnerName == null)
            RecordReflectDamage(ctx, effects.Reflect, effects.ReflectSeverity);
    }
}
