using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Dalamud.Interface.Textures;

namespace DamageTerror.Helpers;

public sealed class GifAnimator : IDisposable
{
    private readonly ISharedImmediateTexture[] frameTextures;
    private readonly int[] frameDelaysMs;
    private readonly int totalDurationMs;
    private readonly Stopwatch stopwatch = Stopwatch.StartNew();
    private readonly string tempDir;

    public int FrameCount => frameTextures.Length;

    public GifAnimator(ITextureProvider textureProvider, string gifPath, string tempDir)
    {
        this.tempDir = Path.Combine(tempDir, Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(this.tempDir);

        using var stream = new MemoryStream(File.ReadAllBytes(gifPath));
        using var image = Image.FromStream(stream, false, false);

        var dimension = new FrameDimension(image.FrameDimensionsList[0]);
        var frameCount = image.GetFrameCount(dimension);

        frameTextures = new ISharedImmediateTexture[frameCount];
        frameDelaysMs = new int[frameCount];

        // Property 0x5100 = frame delay in hundredths of a second
        byte[]? delayBytes = null;
        try { delayBytes = image.GetPropertyItem(0x5100)?.Value; }
        catch (Exception ex) { ServiceManager.LogDebug(LogChannel.GifAnimator, $"GIF frame delay property not found: {ex.Message}"); }

        for (var i = 0; i < frameCount; i++)
        {
            var delay = 100; // default 100ms
            if (delayBytes != null && delayBytes.Length >= (i + 1) * 4)
                delay = BitConverter.ToInt32(delayBytes, i * 4) * 10;
            if (delay <= 0) delay = 100;
            frameDelaysMs[i] = delay;

            image.SelectActiveFrame(dimension, i);

            // Force a clean composited copy — SelectActiveFrame alone can
            // leave delta-encoded residue in the underlying Image buffer.
            using var bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                g.DrawImage(image, 0, 0, image.Width, image.Height);
            }

            var path = Path.Combine(this.tempDir, $"frame_{i}.png");
            bitmap.Save(path, ImageFormat.Png);

            // Pre-resolve the texture so the provider starts loading immediately
            frameTextures[i] = textureProvider.GetFromFile(path);
        }

        totalDurationMs = frameDelaysMs.Sum();
        if (totalDurationMs <= 0) totalDurationMs = frameCount * 100;
    }

    public bool TryGetCurrentFrame(out ImTextureID handle, out int width, out int height)
    {
        handle = default;
        width = 0;
        height = 0;

        var elapsed = (int)(stopwatch.ElapsedMilliseconds % totalDurationMs);
        var accumulated = 0;
        var frameIndex = 0;
        for (var i = 0; i < frameDelaysMs.Length; i++)
        {
            accumulated += frameDelaysMs[i];
            if (elapsed < accumulated)
            {
                frameIndex = i;
                break;
            }
        }

        if (frameTextures[frameIndex].TryGetWrap(out var wrap, out _))
        {
            handle = wrap.Handle;
            width = wrap.Width;
            height = wrap.Height;
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
        catch (Exception ex)
        {
            ServiceManager.LogWarning(LogChannel.GifAnimator, $"Failed to clean up GIF temp directory: {ex.Message}");
        }
    }
}
