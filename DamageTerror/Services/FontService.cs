using Dalamud.Interface;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.ImGuiFontChooserDialog;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;

namespace DamageTerror.Services;

/// <summary>
/// Manages custom font loading, font atlas lifecycle, and the font chooser dialog.
/// </summary>
public sealed class FontService : IDisposable
{
    private readonly Configuration config;
    private readonly IPluginLog pluginLog;
    private IUiBuilder? uiBuilder;
    private IFontAtlas? atlas;
    private IFontHandle? customFontHandle;
    private SingleFontChooserDialog? fontChooserDialog;
    private bool disposed;

    // The deserialized font spec used to build the current handle
    private SingleFontSpec? activeSpec;

    // Deferred rebuild: flag set when config changes, applied next frame before push
    private bool rebuildPending;

    /// <summary>
    /// Whether the service has been initialized with an UiBuilder.
    /// </summary>
    public bool IsInitialized => uiBuilder != null;
    /// <summary>Whether a custom font is loaded and ready to use.</summary>
    public bool HasCustomFont => customFontHandle is { Available: true };

    public FontService(Configuration config, IPluginLog pluginLog)
    {
        this.config = config;
        this.pluginLog = pluginLog;
    }

    /// <summary>
    /// Initialize the font atlas. Must be called after UiBuilder is available.
    /// </summary>
    public void Initialize(IUiBuilder uiBuilder)
    {
        this.uiBuilder = uiBuilder;

        try
        {
            atlas = uiBuilder.CreateFontAtlas(FontAtlasAutoRebuildMode.Async, true, "DamageTerrorFonts");
        }
        catch (Exception ex)
        {
            pluginLog.Error($"[FontService] Failed to create font atlas: {ex.Message}");
            return;
        }

        // Defer font loading to the first draw frame via PushFont(),
        // when the ImGui context is guaranteed to be ready.
        if (!string.IsNullOrEmpty(config.CustomFontSpecJson))
            rebuildPending = true;
    }

    /// <summary>
    /// Rebuild the custom font handle from current config.
    /// Must NOT be called while the font handle is pushed (mid-frame).
    /// </summary>
    private void RebuildFont()
    {
        if (atlas == null) return;

        customFontHandle?.Dispose();
        customFontHandle = null;
        activeSpec = null;

        var specJson = config.CustomFontSpecJson;
        if (string.IsNullOrEmpty(specJson))
            return;

        SingleFontSpec? spec;
        try
        {
            spec = JsonConvert.DeserializeObject<SingleFontSpec>(specJson, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
            });
        }
        catch (Exception ex)
        {
            pluginLog.Error($"[FontService] Failed to deserialize font spec: {ex.Message}");
            return;
        }

        if (spec == null)
            return;

        try
        {
            customFontHandle = spec.CreateFontHandle(atlas);
            activeSpec = spec;
        }
        catch (Exception ex)
        {
            pluginLog.Error($"[FontService] Failed to create font handle: {ex.Message}");
        }
    }

    /// <summary>
    /// Push the custom font onto the ImGui font stack.
    /// Returns an IDisposable that pops the font when disposed, or null if no custom font.
    /// Call once at the top of Draw(). Do NOT call RebuildFont or ClearCustomFont while pushed.
    /// </summary>
    public IDisposable? PushFont()
    {
        // Apply any deferred rebuild before pushing
        if (rebuildPending)
        {
            rebuildPending = false;
            RebuildFont();
        }

        if (customFontHandle is { Available: true })
        {
            try
            {
                return customFontHandle.Push();
            }
            catch (Exception ex)
            {
                pluginLog.Error($"[FontService] Font push failed: {ex.Message}");
            }
        }

        return null;
    }

    /// <summary>
    /// Open the Dalamud font chooser dialog.
    /// </summary>
    public void OpenFontChooser()
    {
        if (uiBuilder is not UiBuilder concreteUiBuilder) return;

        fontChooserDialog?.Dispose();
        fontChooserDialog = new SingleFontChooserDialog(concreteUiBuilder, true, "DamageTerrorFontChooser")
        {
            PreviewText = "Damage: 12,345  DPS: 6,789  Crit: 25.4%",
        };
    }

    /// <summary>
    /// Draw the font chooser dialog (if open). Call from the config window draw loop.
    /// Returns true if a font was selected this frame.
    /// </summary>
    public bool DrawFontChooser()
    {
        if (fontChooserDialog == null) return false;

        fontChooserDialog.Draw();

        if (fontChooserDialog.ResultTask is { IsCompleted: true } task)
        {
            try
            {
                var selectedSpec = task.Result;
                if (selectedSpec != null)
                {
                    ApplyFontSpec(selectedSpec);
                    return true;
                }
            }
            catch
            {
                // Cancelled or failed — not an error
            }
            finally
            {
                fontChooserDialog.Dispose();
                fontChooserDialog = null;
            }
        }

        return false;
    }

    /// <summary>
    /// Apply a selected font spec to config and schedule a deferred rebuild.
    /// Does NOT rebuild immediately — safe to call mid-frame.
    /// </summary>
    private void ApplyFontSpec(SingleFontSpec spec)
    {
        config.CustomFontSizePt = spec.SizePt;
        config.CustomFontDisplayName = spec.FontId?.EnglishName ?? "Custom";

        try
        {
            config.CustomFontSpecJson = JsonConvert.SerializeObject(spec, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
            });
        }
        catch (Exception ex)
        {
            pluginLog.Error($"[FontService] Failed to serialize font spec: {ex.Message}");
            return;
        }

        // Defer rebuild to next frame (before push), not mid-frame
        rebuildPending = true;
    }

    /// <summary>
    /// Clear the custom font selection and revert to Dalamud default.
    /// Defers the actual handle disposal to next frame.
    /// </summary>
    public void ClearCustomFont()
    {
        config.CustomFontPath = null;
        config.CustomFontIndex = 0;
        config.CustomFontSizePt = 14f;
        config.CustomFontDisplayName = null;
        config.CustomFontSpecJson = null;

        // Defer rebuild (which will clear the handle) to next frame
        rebuildPending = true;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        fontChooserDialog?.Dispose();
        customFontHandle?.Dispose();
        atlas?.Dispose();
    }
}
