using System.Runtime.InteropServices;
using Campus.Contracts;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;

namespace Campus.Kernel.Lighting;

/// <summary>
/// Owner of the light_channels global texture (32×1 RGF, nearest; r = L, g = Δ, ENG-PERF 1.5 and 10.3).
/// The schedule submits once per frame in FramePriority.Channels and the texture is updated on the
/// spot, so node order inside that step does not matter. Readers (audio activity, point-light energy)
/// get the cached L through <see cref="IChannelReader"/>; nothing reads the global back.
/// </summary>
[KernelService(typeof(IChannelReader))]
[KernelService(typeof(ILightChannelSink))]
public sealed class LightChannelService : IChannelReader, ILightChannelSink
{
    private static readonly StringName _shaderGlobalLightChannels = ShaderGlobals.LightChannels;

    private readonly N.Vector2[] _slots = new N.Vector2[LightChannelCatalog.Count];
    private Image? _image;
    private ImageTexture? _texture;

    public long Submissions { get; private set; }

    public float Value(ChannelId id)
    {
        var slot = LightChannelCatalog.Slot(id);
        return slot < 0 ? 0f : _slots[slot].X;
    }

    public void Submit(ReadOnlySpan<N.Vector2> lDelta)
    {
        if (lDelta.Length != LightChannelCatalog.Count)
            throw new ArgumentException($"Expected {LightChannelCatalog.Count} (L, Δ) pairs.", nameof(lDelta));
        foreach (var pair in lDelta)
            if (!float.IsFinite(pair.X) || !float.IsFinite(pair.Y))
                throw new ArgumentException("Light channel values must be finite.", nameof(lDelta));
        lDelta.CopyTo(_slots);
        var bytes = MemoryMarshal.AsBytes(_slots.AsSpan());
        if (_image is null || _texture is null)
        {
            _image = Image.CreateFromData(LightChannelCatalog.Count, 1, false, Image.Format.Rgf, bytes);
            _texture = ImageTexture.CreateFromImage(_image);
            RenderingServer.GlobalShaderParameterSet(_shaderGlobalLightChannels, _texture);
        }
        else
        {
            _image.SetData(LightChannelCatalog.Count, 1, false, Image.Format.Rgf, bytes);
            _texture.Update(_image);
        }
        Submissions++;
    }
}
