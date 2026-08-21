using Dalamud.Game.Config;

using Lumina.Excel.Sheets;

namespace DamageTerror.Services;

/// <summary>
/// The game's own palette. A UIColor row is not one colour but one per UI theme, so a colour
/// read off an addon under one theme is wrong under the others - the party list name's outline
/// is 49,97,134 on Dark and 43,70,109 on Clear Green. Reading the row keeps a colour we hand
/// the user matched to the theme they actually play on.
/// </summary>
public static class GameUiColors
{
    /// <summary>The UIColor rows PartyList.uld gives the member name's text node, which is where
    /// the party list metrics take their look from.</summary>
    private const uint NameTextRow = 1;
    private const uint NameOutlineRow = 36;

    /// <summary>How the game draws a resting party list name, or null when the palette can't be
    /// read.</summary>
    public static Vector4? PartyListName => Resolve(NameTextRow);

    public static Vector4? PartyListNameOutline => Resolve(NameOutlineRow);

    /// <summary>One palette row in the theme the player is on. Alpha is dropped - the sheet is
    /// opaque throughout and the callers here draw text, not artwork.</summary>
    public static Vector4? Resolve(uint row)
    {
        try
        {
            var color = ServiceManager.DataManager.GetExcelSheet<UIColor>()?.GetRowOrDefault(row);
            if (color == null)
                return null;

            // The sheet's columns are the themes in the order the game's own list offers them.
            var packed = Theme() switch
            {
                1 => color.Value.Light,
                2 => color.Value.ClassicFF,
                3 => color.Value.ClearBlue,
                4 => color.Value.ClearWhite,
                5 => color.Value.ClearGreen,
                _ => color.Value.Dark,
            };

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

    private static uint Theme()
        => Svc.GameConfig.TryGet(SystemConfigOption.ColorThemeType, out uint theme) ? theme : 0;
}
