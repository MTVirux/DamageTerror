namespace DamageTerror.Enums;

/// <summary>
/// Which of the game's own faces a party list text node is drawn in. <see cref="Game"/> leaves
/// the node with whatever the game gave it, so nothing is written until a face is picked.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum PartyListFont
{
    Game = 0,
    Axis = 1,
    MiedingerMed = 2,
    Miedinger = 3,
    TrumpGothic = 4,
    Jupiter = 5,
    JupiterLarge = 6,
}
