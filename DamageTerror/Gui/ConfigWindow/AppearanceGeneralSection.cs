using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

internal static class AppearanceGeneralSection
{
    public static bool Draw(Configuration config)
    {
        var changed = false;

        changed |= ConfigHelpers.ColorEditProp("Window background", config.WindowBackgroundColor, v => config.WindowBackgroundColor = v);

        var padLeft = config.WindowPaddingLeft;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Padding left", ref padLeft, 0.0f, 32.0f, "%.0f"))
        {
            config.WindowPaddingLeft = padLeft;
            changed = true;
        }

        var padRight = config.WindowPaddingRight;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Padding right", ref padRight, 0.0f, 32.0f, "%.0f"))
        {
            config.WindowPaddingRight = padRight;
            changed = true;
        }

        var padTop = config.WindowPaddingTop;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Padding top", ref padTop, 0.0f, 32.0f, "%.0f"))
        {
            config.WindowPaddingTop = padTop;
            changed = true;
        }

        var padBottom = config.WindowPaddingBottom;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Padding bottom", ref padBottom, 0.0f, 32.0f, "%.0f"))
        {
            config.WindowPaddingBottom = padBottom;
            changed = true;
        }

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

                var opacity = config.BackgroundImageOpacity;
                ImGui.SetNextItemWidth(200);
                if (ImGui.SliderFloat("Opacity", ref opacity, 0.0f, 1.0f, "%.2f"))
                {
                    config.BackgroundImageOpacity = opacity;
                    changed = true;
                }

                changed |= ConfigHelpers.ColorEditProp("Tint", config.BackgroundImageTint, v => config.BackgroundImageTint = v);

                var scaleIdx = (int)config.BackgroundImageScale;
                var scaleLabels = new[] { "Stretch", "Fit", "Fill", "Tile" };
                ImGui.SetNextItemWidth(200);
                if (ImGui.Combo("Scale mode", ref scaleIdx, scaleLabels, scaleLabels.Length))
                {
                    config.BackgroundImageScale = (BackgroundImageScaleMode)scaleIdx;
                    changed = true;
                }

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
