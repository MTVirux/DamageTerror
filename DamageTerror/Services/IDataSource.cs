namespace DamageTerror.Services;

public interface IDataSource : IDisposable
{
    event Action<EncounterSnapshot>? OnCombatData;

    event Action<string, uint>? OnPrimaryPlayerChanged;

    event Action<string[]>? OnLogLine;

    event Action? OnConnected;

    bool IsConnected { get; }

    Task ConnectAsync(CancellationToken ct = default);

    void Disconnect();
}
