namespace DamageTerror.Models;

public struct SkillUseEvent
{
    public float TimeSec;
    public string SkillName;
    public string? TargetName;
    /// <summary>Damage dealt or HP healed, depending on <see cref="IsHeal"/>.</summary>
    public long Amount;
    public bool IsHeal;
    public bool IsCrit;
    public bool IsDirectHit;
    public bool IsDoTTick;
    public bool IsHoTTick;
    public bool IsDoTApplication;
    public bool IsHoTApplication;
}
