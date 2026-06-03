namespace DamageTerror.Helpers;

/// <summary>
/// Display-name overrides for skill names. Applied both to ability-line names parsed
/// from log lines (<see cref="Services.SkillTracker"/>) and to the skill name embedded
/// in IINACT's "maxhit"/"maxheal" combatant summaries (<see cref="Models.CombatantEntry"/>).
/// Covers the generic "Attack" auto-attack label and actions whose name does not resolve
/// client-side (logged as a "unknown_&lt;hexId&gt;" placeholder).
/// </summary>
public static class SkillNameOverrides
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Attack", "Auto Attack" },
        { "unknown_c50d", "『Ｂｅｔｒａｙａｌ』" },
    };

    /// <summary>Returns the override for <paramref name="skillName"/>, or the name unchanged.</summary>
    public static string Apply(string skillName)
        => string.IsNullOrEmpty(skillName) ? skillName : Map.GetValueOrDefault(skillName, skillName);
}
