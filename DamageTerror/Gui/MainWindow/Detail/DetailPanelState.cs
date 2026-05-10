namespace DamageTerror.Gui.MainWindow.Detail;

internal sealed class DetailPanelState
{
    public string? ExpandedName;
    public readonly HashSet<string> ExpandedSkills = new();
    public readonly HashSet<string> HiddenLegendEntries = new(StringComparer.Ordinal);
    public readonly Dictionary<uint, string> ItemNameCache = new();
    public bool WasActivelyUpdating;
    public double ScrollXMin = double.NaN;
    public double ScrollXMax = double.NaN;

    public void Toggle(string name)
    {
        if (ExpandedName == name)
        {
            ExpandedName = null;
        }
        else
        {
            ExpandedName = name;
            ExpandedSkills.Clear();
            HiddenLegendEntries.Clear();
        }
    }

    public void CollapseAll()
    {
        ExpandedName = null;
        ExpandedSkills.Clear();
        HiddenLegendEntries.Clear();
    }
}
