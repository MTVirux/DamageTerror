namespace DamageTerror.Services;

/// <summary>
/// Interface for data sources that provide combat data from IINACT.
/// </summary>
public interface IDataSource : IDisposable
{
    /// <summary>
    /// Fired when a new CombatData event is received and parsed.
    /// </summary>
    event Action<EncounterSnapshot>? OnCombatData;

    /// <summary>
    /// Fired when the primary player name/ID is received.
    /// </summary>
    event Action<string, uint>? OnPrimaryPlayerChanged;

    /// <summary>
    /// Fired when a LogLine event is received, providing the parsed line fields.
    /// </summary>
    event Action<string[]>? OnLogLine;

    /// <summary>
    /// Whether the data source is currently connected and receiving data.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connect to the data source asynchronously.
    /// </summary>
    Task ConnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Disconnect from the data source.
    /// </summary>
    void Disconnect();
}
