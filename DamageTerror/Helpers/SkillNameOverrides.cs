namespace DamageTerror.Helpers;

/// <summary>
/// Display-name overrides for skill names. Applied both to ability-line names parsed
/// from log lines (<see cref="Services.SkillTracker"/>) and to the skill name embedded
/// in IINACT's "maxhit"/"maxheal" combatant summaries (<see cref="Models.CombatantEntry"/>).
/// Covers the generic "Attack" auto-attack label and actions whose name does not resolve
/// client-side (logged as a "unknown_&lt;hexId&gt;" placeholder).
/// Action-ID overrides take precedence over name overrides, so an action is named
/// consistently whether or not its name resolved (e.g. C50D logs as "attack" when resolved
/// and "unknown_c50d" when not — both must map to 『Betrayal』).
/// </summary>
public static class SkillNameOverrides
{
    private static readonly Dictionary<uint, string> ById = new()
    {
        { 0xC50D, "『Ｂｅｔｒａｙａｌ』" },
    };

    private static readonly Dictionary<string, string> ByName = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Attack", "Auto Attack" },
        { "unknown_c50d", "『Ｂｅｔｒａｙａｌ』" },
    };

    /// <summary>
    /// Returns the override for <paramref name="actionId"/> if one exists, otherwise the
    /// name override for <paramref name="skillName"/>, otherwise the name unchanged.
    /// </summary>
    public static string Apply(uint actionId, string skillName)
        => ById.TryGetValue(actionId, out var byId) ? byId : Apply(skillName);

    /// <summary>Returns the override for <paramref name="skillName"/>, or the name unchanged.</summary>
    public static string Apply(string skillName)
        => string.IsNullOrEmpty(skillName) ? skillName : ByName.GetValueOrDefault(skillName, skillName);
}
