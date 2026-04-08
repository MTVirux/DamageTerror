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

        // SMN
        { 2706, 30 },  // Slipstream

        // SAM
        { 1228, 45 },  // Higanbana

        // DRG
        { 118, 40 },   // Chaos Thrust
        { 2719, 45 },  // Chaotic Spring

        // VPR
        { 3667, 35 },  // Noxious Gnash

        // BLU
        { 1714, 50 },  // Bleeding
        { 1736, 50 },  // Dropsy
        { 18, 30 },    // Poison
        { 1723, 20 },  // Windburn
        { 3712, 80 },  // Breath of Magic
        { 3643, 50 },  // Mortal Flame

        // ── HoTs ──

        // WHM
        { 158, 250 },  // Regen
        { 150, 150 },  // Medica II
        { 3880, 150 }, // Medica III

        // AST
        { 835, 250 },  // Aspected Benefic
        { 836, 150 },  // Aspected Helios
        { 3894, 150 }, // Helios Conjunction

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

        // BLU
        { 2495, 100 }, // Angel's Snack
    };

    public static int GetTickPotency(uint statusId)
    {
        return TickPotencies.GetValueOrDefault(statusId, DefaultPotency);
    }
}
