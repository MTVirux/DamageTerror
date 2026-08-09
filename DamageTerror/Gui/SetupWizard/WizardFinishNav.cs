namespace DamageTerror.Gui.SetupWizard;

// Cross-wizard navigation for every wizard's finish step: buttons for the
// other wizards, with a completed marker for ones already finished.
internal static class WizardFinishNav
{
    public enum WizardKind { Setup, Customization, Columns }

    private static readonly Vector4 CompletedColor = new(0.4f, 1f, 0.4f, 1f);

    public static void Draw(DamageTerrorPlugin plugin, WizardKind current, Func<bool> releaseSampleOwnership, Action closeSelf)
    {
        ImGui.TextWrapped("There are other wizards too. You can also get to them later from Settings -> General:");
        ImGui.Spacing();

        if (current != WizardKind.Setup)
            DrawRow("Run the setup wizard", plugin.Config.HasCompletedSetup,
                () => plugin.OpenSetupWizard(takeOverSampleData: releaseSampleOwnership()), closeSelf);
        if (current != WizardKind.Customization)
            DrawRow("Customize the look", plugin.Config.HasCompletedCustomizationWizard,
                () => plugin.OpenCustomizationWizard(takeOverSampleData: releaseSampleOwnership()), closeSelf);
        if (current != WizardKind.Columns)
            DrawRow("Set up the columns", plugin.Config.HasCompletedColumnWizard,
                () => plugin.OpenColumnWizard(takeOverSampleData: releaseSampleOwnership()), closeSelf);
    }

    private static void DrawRow(string label, bool completed, Action open, Action closeSelf)
    {
        if (ImGui.Button(label))
        {
            open();
            closeSelf();
        }

        if (completed)
        {
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(CompletedColor, FontAwesomeIcon.Check.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.TextColored(CompletedColor, "Completed");
        }
    }
}
