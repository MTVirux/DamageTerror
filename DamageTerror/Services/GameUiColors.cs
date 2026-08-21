using Lumina.Excel.Sheets;

namespace DamageTerror.Services;

/// <summary>
/// The game's own palette, read the way the party list reads it. A UIColor row carries a colour
/// per UI theme, but the game only reaches for those when a lookup asks: AtkUIColorHolder keeps a
/// plain colour and a themed one per row, and hands back the plain one unless the caller passes
/// useThemeColor. Nothing in PartyList.uld asks - its name node carries a row id and an "is a UI
/// colour" bit and nothing else - so the party list is drawn in the untinted colours on every
/// theme, and that is the column taken here.
/// </summary>
public static class GameUiColors
{
    /// <summary>The UIColor rows PartyList.uld gives the member name's text node, which is where
    /// the party list metrics take their look from.</summary>
    private const uint NameTextRow = 1;
    private const uint NameOutlineRow = 36;

    /// <summary>What the party list was last seen drawing a name with, filled in by the overlay
    /// from a row that is actually showing one. It beats reading the sheet: it is the colour on
    /// screen, whichever row of the palette the game reached for.</summary>
    public static Vector4? ObservedName { get; set; }

    public static Vector4? ObservedNameOutline { get; set; }

    /// <summary>How the game draws a resting party list name, or null when nothing has been seen
    /// and the palette can't be read either.</summary>
    public static Vector4? PartyListName => ObservedName ?? Resolve(NameTextRow);

    public static Vector4? PartyListNameOutline => ObservedNameOutline ?? Resolve(NameOutlineRow);

    /// <summary>One palette row's untinted colour - Lumina calls the column Dark because that
    /// theme leaves it alone. Alpha is dropped: the sheet is opaque throughout and the callers
    /// here draw text, not artwork.</summary>
    public static Vector4? Resolve(uint row)
    {
        try
        {
            var color = ServiceManager.DataManager.GetExcelSheet<UIColor>()?.GetRowOrDefault(row);
            if (color == null)
                return null;

            var packed = color.Value.Dark;
            return new Vector4(
                ((packed >> 24) & 0xFF) / 255f,
                ((packed >> 16) & 0xFF) / 255f,
                ((packed >> 8) & 0xFF) / 255f,
                1f);
        }
        catch (Exception ex)
        {
            ServiceManager.LogDebug(LogChannel.Plugin, $"Failed to read UIColor {row}: {ex.Message}");
            return null;
        }
    }
}
