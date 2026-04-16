namespace DamageTerror.Helpers;

/// <summary>
/// Maps FFXIV status effect IDs to their DoT/HoT tick potency values.
/// Used for potency-weighted tick damage distribution when multiple players
/// have DoTs/HoTs active on the same target.
///
/// Based on the simulation approach described at:
/// https://github.com/ravahn/FFXIV_ACT_Plugin/wiki/DoT---HoT-Simulation-details
///
/// Values are approximate for FFXIV 7.x (Dawntrail) and may need updating per patch.
/// </summary>
public static class DotPotencyTable
{
    public const int DefaultPotency = 50;

    private static readonly Dictionary<uint, int> TickPotencies = new()
    {
        // ── DoTs ──

        // WHM
        { 1871, 65 },  // Dia
        { 143, 50 },   // Aero
        { 144, 50 },   // Aero II
        { 798, 50 },   // Aero III

        // SCH
        { 1895, 75 },  // Biolysis
        { 189, 20 },   // Bio II
        { 3883, 50 },  // Baneful Impaction

        // AST
        { 838, 40 },   // Combust
        { 843, 50 },   // Combust II
        { 1881, 55 },  // Combust III

        // SGE
        { 2614, 40 },  // Eukrasian Dosis
        { 2615, 60 },  // Eukrasian Dosis II
        { 2616, 75 },  // Eukrasian Dosis III
        { 3897, 40 },  // Eukrasian Dyskrasia

        // BRD
        { 124, 15 },   // Venomous Bite
        { 129, 20 },   // Windbite
        { 1200, 20 },  // Caustic Bite
        { 1201, 25 },  // Stormbite

        // BLM
        { 163, 35 },   // Thunder III
        { 1210, 30 },  // Thunder IV
        { 3871, 30 },  // High Thunder
        { 3872, 30 },  // High Thunder II

        // SMN
        { 2706, 30 },  // Slipstream

        // SAM
        { 1228, 45 },  // Higanbana

        // DRG
        { 118, 40 },   // Chaos Thrust
        { 2719, 45 },  // Chaotic Spring

        // VPR
        { 3667, 35 },  // Noxious Gnash

        // PLD
        { 248, 30 },   // Circle of Scorn

        // GNB
        { 1837, 60 },  // Sonic Break
        { 1838, 60 },  // Bow Shock

        // MCH
        { 1866, 50 },  // Bioblaster

        // NIN
        { 501, 50 },   // Doton

        // DRK
        { 749, 50 },   // Salted Earth

        // BLU
        { 1714, 50 },  // Bleeding
        { 1736, 50 },  // Dropsy
        { 18, 30 },    // Poison
        { 1723, 20 },  // Windburn
        { 3712, 80 },  // Breath of Magic
        { 3643, 50 },  // Mortal Flame

        // PvP DoTs
        { 2039, 50 },  // Biolysis (SCH PvP)
        { 3976, 50 },  // Eukrasian Dosis III (SGE PvP)
        { 2019, 65 },  // Bioblaster (MCH PvP)
        { 3184, 80 },  // Goka Mekkyaku (NIN PvP)
        { 3231, 65 },  // Scarlet Flame (SMN PvP)
        { 4319, 65 },  // Scorch (RDM PvP)
        // PvP ground-effect DoTs
        { 3036, 80 },  // Salted Earth (DRK PvP)
        { 3162, 75 },  // Honing Dance (DNC PvP)
        { 4304, 50 },  // Doton (NIN PvP)

        // ── HoTs ──

        // WHM
        { 158, 250 },  // Regen
        { 150, 150 },  // Medica II
        { 3880, 150 }, // Medica III
        { 1911, 100 }, // Asylum

        // AST
        { 835, 250 },  // Aspected Benefic
        { 836, 150 },  // Aspected Helios
        { 3894, 150 }, // Helios Conjunction
        { 848, 100 },  // Collective Unconscious
        { 956, 100 },  // Wheel of Fortune

        // SCH
        { 315, 120 },  // Whispering Dawn
        { 1874, 120 }, // Angel's Whisper
        { 1944, 100 }, // Sacred Soil
        { 3885, 100 }, // Seraphism

        // SGE
        { 2617, 100 }, // Physis
        { 2620, 100 }, // Physis II
        { 2938, 100 }, // Kerakeia
        { 3898, 170 }, // Philosophia

        // WAR
        { 2681, 200 }, // Equilibrium
        { 2108, 100 }, // Shake It Off (Over Time)

        // GNB
        { 1835, 200 }, // Aurora

        // PLD
        { 2676, 250 }, // Knight's Benediction

        // DNC
        { 2695, 100 }, // Improvisation

        // BLU
        { 2495, 100 }, // Angel's Snack

        // PvP HoTs
        { 3037, 80 },  // Salted Earth (DRK PvP, self-HoT)
        { 3189, 65 },  // Meisui (NIN PvP)
        { 2862, 100 }, // Crest of Time Returned (RPR PvP)
    };

    /// <summary>
    /// Maps DoT status IDs to the initial hit potency of the applying ability.
    /// Used to calibrate a per-source damage-per-potency-point coefficient
    /// for more accurate tick weight distribution.
    /// Only includes DoTs whose applying ability deals direct damage on application.
    /// </summary>
    private static readonly Dictionary<uint, int> InitialHitPotencies = new()
    {
        // SAM
        { 1228, 200 },  // Higanbana

        // DRG
        { 118, 100 },   // Chaos Thrust
        { 2719, 300 },  // Chaotic Spring

        // VPR
        { 3667, 200 },  // Noxious Gnash

        // BRD
        { 1200, 150 },  // Caustic Bite
        { 1201, 100 },  // Stormbite

        // BLM
        { 163, 120 },   // Thunder III
        { 1210, 80 },   // Thunder IV
        { 3871, 150 },  // High Thunder
        { 3872, 80 },   // High Thunder II

        // WHM
        { 1871, 65 },   // Dia

        // SCH
        { 1895, 75 },   // Biolysis

        // PLD
        { 248, 120 },   // Circle of Scorn

        // GNB
        { 1837, 300 },  // Sonic Break
        { 1838, 150 },  // Bow Shock

        // MCH
        { 1866, 50 },   // Bioblaster
    };

    public static int GetTickPotency(uint statusId)
    {
        return TickPotencies.GetValueOrDefault(statusId, DefaultPotency);
    }

    /// <summary>
    /// Returns the initial hit potency for a DoT-applying ability, or 0 if unknown.
    /// Used by SkillTracker to calibrate per-source damage-per-potency-point coefficients.
    /// </summary>
    public static int GetInitialHitPotency(uint statusId)
    {
        return InitialHitPotencies.GetValueOrDefault(statusId, 0);
    }
}
