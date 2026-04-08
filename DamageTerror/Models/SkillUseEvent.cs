namespace DamageTerror.Models;

public struct SkillUseEvent
{
    public float TimeSec;
    public string SkillName;
    public string? TargetName;
    public long Amount;
    public bool IsHeal;
    public bool IsCrit;
    public bool IsDirectHit;
    public bool IsDoTTick;
    public bool IsHoTTick;
    public bool IsDoTApplication;
    public bool IsHoTApplication;
}
