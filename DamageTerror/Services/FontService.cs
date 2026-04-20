using Dalamud.Interface;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.ImGuiFontChooserDialog;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;

namespace DamageTerror.Services;

public sealed class FontService : IDisposable
{
    private readonly Configuration config;
    private readonly IPluginLog pluginLog;
    private IUiBuilder? uiBuilder;
    private IFontAtlas? atlas;
    private IFontHandle? customFontHandle;
    private SingleFontChooserDialog? fontChooserDialog;
    private bool disposed;

    private SingleFontSpec? activeSpec;

    // Deferred rebuild: flag set when config changes, applied next frame before push.
    // Volatile because config changes (ApplyFontSpec/ClearCustomFont) could theoretically
    // race with the UI-thread read in PushFont().
    private volatile bool rebuildPending;

    public bool IsInitialized => uiBuilder != null;
    public bool HasCustomFont => customFontHandle is { Available: true };

    public FontService(Configuration config, IPluginLog pluginLog)
    {
        this.config = config;
        this.pluginLog = pluginLog;
    }

    public void Initialize(IUiBuilder uiBuilder)
    {
        this.uiBuilder = uiBuilder;

        try
        {
            atlas = uiBuilder.CreateFontAtlas(FontAtlasAutoRebuildMode.Async, true, "DamageTerrorFonts");
        }
        catch (Exception ex)
        {
            ServiceManager.LogError(LogChannel.FontService, $"[FontService] Failed to create font atlas: {ex.Message}");
            return;
        }

        // Defer font loading to the first draw frame via PushFont(),
        // when the ImGui context is guaranteed to be ready.
        if (!string.IsNullOrEmpty(config.CustomFontSpecJson))
            rebuildPending = true;
    }

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
            ServiceManager.LogError(LogChannel.FontService, $"[FontService] Failed to deserialize font spec: {ex.Message}");
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
            ServiceManager.LogError(LogChannel.FontService, $"[FontService] Failed to create font handle: {ex.Message}");
        }
    }

    public IDisposable? PushFont()
    {
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
                ServiceManager.LogError(LogChannel.FontService, $"[FontService] Font push failed: {ex.Message}");
            }
        }

        return null;
    }

    public void OpenFontChooser()
    {
        if (uiBuilder is not UiBuilder concreteUiBuilder) return;

        fontChooserDialog?.Dispose();
        fontChooserDialog = new SingleFontChooserDialog(concreteUiBuilder, true, "DamageTerrorFontChooser")
        {
            PreviewText = "Damage: 12,345  DPS: 6,789  Crit: 25.4%",
        };
    }

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
            ServiceManager.LogError(LogChannel.FontService, $"[FontService] Failed to serialize font spec: {ex.Message}");
            return;
        }

        // Defer rebuild to next frame (before push), not mid-frame
        rebuildPending = true;
    }

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
