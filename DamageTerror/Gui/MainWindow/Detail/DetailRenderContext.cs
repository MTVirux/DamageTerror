namespace DamageTerror.Gui.MainWindow.Detail;

internal readonly struct DetailRenderContext
{
    public required CombatantEntry Combatant { get; init; }
    public required string Index { get; init; }
    public required EncounterSnapshot? Snapshot { get; init; }
    public required bool IsLive { get; init; }
    public required MeterTab? ActiveTab { get; init; }
}
