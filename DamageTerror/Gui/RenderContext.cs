namespace DamageTerror.Gui;

/// <summary>
/// Per-frame snapshot of shared rendering state, constructed once per Draw() call
/// and passed to all UI components.
/// </summary>
public sealed class RenderContext
{
    public required Configuration Config { get; init; }
    public required EncounterSnapshot? Encounter { get; init; }
    public required string CurrentPlayerName { get; init; }
    public required bool IsLive { get; init; }
    public required MeterTab? ActiveTab { get; init; }
    public required SortField SortBy { get; init; }
    public required bool SortDescending { get; init; }
    public required List<CombatantEntry>? Combatants { get; init; }
    public required double MaxValue { get; init; }
}
