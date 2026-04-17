namespace DamageTerror.Models;

public readonly struct SkillUseEvent
{
    public required float TimeSec { get; init; }
    public required string SkillName { get; init; }
    public string? TargetName { get; init; }
    /// <summary>Damage dealt or HP healed, depending on <see cref="IsHeal"/>.</summary>
    public long Amount { get; init; }
    public bool IsHeal { get; init; }
    public bool IsCrit { get; init; }
    public bool IsDirectHit { get; init; }
    public bool IsDoTTick { get; init; }
    public bool IsHoTTick { get; init; }
    public bool IsDoTApplication { get; init; }
    public bool IsHoTApplication { get; init; }
}
