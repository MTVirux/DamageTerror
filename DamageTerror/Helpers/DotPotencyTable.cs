
namespace DamageTerror.Helpers;

/// <summary>
/// Maps FFXIV status effect IDs to their DoT/HoT tick potency values.
/// Delegates to per-job definitions in <see cref="JobRegistry"/>.
/// </summary>
public static class DotPotencyTable
{
    public const int DefaultPotency = 50;

    public static int GetTickPotency(uint statusId) => JobRegistry.GetTickPotency(statusId);

    public static int GetInitialHitPotency(uint statusId) => JobRegistry.GetInitialHitPotency(statusId);
}
