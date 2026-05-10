using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.MainWindow.Detail;

internal static class DetailRenderHelpers
{
    public static bool PersistentTreeNode(Configuration config, string label, string id)
    {
        var key = label;
        var isOpen = config.DetailExpandedSections.Contains(key);
        var flags = isOpen ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None;
        var nowOpen = ImGui.TreeNodeEx($"{label}##{id}", flags);
        if (nowOpen && !isOpen)
        {
            config.DetailExpandedSections.Add(key);
            config.Save?.Invoke();
        }
        else if (!nowOpen && isOpen)
        {
            config.DetailExpandedSections.Remove(key);
            config.Save?.Invoke();
        }
        return nowOpen;
    }

    public static string Fmt(Configuration config, double value)
        => ValueFormatter.Format(value, config);

    public static string FmtPct(Configuration config, double value)
        => ValueFormatter.FormatPercent(value, config.PercentDecimalPlaces);
}
