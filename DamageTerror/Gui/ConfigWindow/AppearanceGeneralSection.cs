using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

internal static class AppearanceGeneralSection
{
    public static bool Draw(Configuration config)
    {
        var changed = false;

        changed |= ConfigHelpers.ColorEditProp("Window background", config.WindowBackgroundColor, v => config.WindowBackgroundColor = v);

        changed |= ConfigHelpers.SliderFloatProp("Padding left", config.WindowPaddingLeft, 0.0f, 32.0f, "%.0f", v => config.WindowPaddingLeft = v, 200);
        changed |= ConfigHelpers.SliderFloatProp("Padding right", config.WindowPaddingRight, 0.0f, 32.0f, "%.0f", v => config.WindowPaddingRight = v, 200);
        changed |= ConfigHelpers.SliderFloatProp("Padding top", config.WindowPaddingTop, 0.0f, 32.0f, "%.0f", v => config.WindowPaddingTop = v, 200);
        changed |= ConfigHelpers.SliderFloatProp("Padding bottom", config.WindowPaddingBottom, 0.0f, 32.0f, "%.0f", v => config.WindowPaddingBottom = v, 200);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Background Image", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ConfigHelpers.HelpMarker("Display a custom image behind the meter window.");

            var hasImage = !string.IsNullOrEmpty(config.BackgroundImagePath);
            var pathDisplay = hasImage ? config.BackgroundImagePath! : "(none)";
            ImGui.Text($"Image: {pathDisplay}");

            if (ImGui.Button("Browse..."))
            {
                AppearanceTab.FileDialogManager.OpenFileDialog(
                    "Select Background Image",
                    "Image files{.png,.jpg,.jpeg,.gif}",
                    (ok, path) =>
                    {
                        if (ok && !string.IsNullOrEmpty(path))
                        {
                            config.BackgroundImagePath = path;
                            DamageTerrorPlugin.Instance.SaveConfig();
                        }
                    });
            }

            if (hasImage)
            {
                ImGui.SameLine();
                if (ImGui.Button("Clear"))
                {
                    config.BackgroundImagePath = null;
                    changed = true;
                }

                changed |= ConfigHelpers.SliderFloatProp("Opacity", config.BackgroundImageOpacity, 0.0f, 1.0f, "%.2f", v => config.BackgroundImageOpacity = v, 200);

                changed |= ConfigHelpers.ColorEditProp("Tint", config.BackgroundImageTint, v => config.BackgroundImageTint = v);

                var scaleLabels = new[] { "Stretch", "Fit", "Fill", "Tile" };
                changed |= ConfigHelpers.ComboProp("Scale mode", (int)config.BackgroundImageScale, scaleLabels, v => config.BackgroundImageScale = (BackgroundImageScaleMode)v, 200);

                if (System.IO.File.Exists(config.BackgroundImagePath))
                {
                    var preview = ServiceManager.TextureProvider.GetFromFile(config.BackgroundImagePath);
                    if (preview.TryGetWrap(out var wrap, out _))
                    {
                        ImGui.Spacing();
                        var previewHeight = 80f;
                        var aspect = (float)wrap.Width / wrap.Height;
                        ImGui.Image(wrap.Handle, new Vector2(previewHeight * aspect, previewHeight));
                    }
                }
            }
        }

        return changed;
    }
}
