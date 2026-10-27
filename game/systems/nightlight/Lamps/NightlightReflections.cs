using System.Runtime.InteropServices;
using Campus.Contracts;
using Godot;

namespace Campus.Systems.Nightlight.Lamps;

internal sealed class NightlightReflections
{
    private static readonly StringName ReflectionLampsGlobal = ShaderGlobals.ReflLamps;
    private readonly ReflectionLampSelection _reflectionSelection = new();
    private Image? _reflectionImage;
    private ImageTexture? _reflectionTexture;

    public bool Ready => _reflectionTexture is not null;

    public void Clear()
    {
        _reflectionSelection.Clear();
        UploadReflectionLamps();
    }

    public void Update(IReadOnlyList<LampRecord> lamps, IReadOnlyList<NightlightTileContent> tiles,
        ReadOnlySpan<System.Numerics.Vector2> channels, ReadOnlySpan<byte> overrides,
        ReadOnlySpan<float> thresholds, Pose pose)
    {
        if (_reflectionSelection.Select(lamps, tiles, channels, overrides, thresholds, pose.Position)) UploadReflectionLamps();
    }

    public void Dispose()
    {
        _reflectionTexture?.Dispose();
        _reflectionImage?.Dispose();
        _reflectionTexture = null;
        _reflectionImage = null;
    }

    private void UploadReflectionLamps()
    {
        var bytes = MemoryMarshal.AsBytes(_reflectionSelection.Data);
        if (_reflectionImage is null || _reflectionTexture is null)
        {
            _reflectionImage = Image.CreateFromData(ReflectionLampSelection.Capacity, ReflectionLampSelection.Rows,
                false, Image.Format.Rgbaf, bytes);
            _reflectionTexture = ImageTexture.CreateFromImage(_reflectionImage);
            RenderingServer.GlobalShaderParameterSet(ReflectionLampsGlobal, _reflectionTexture);
        }
        else
        {
            _reflectionImage.SetData(ReflectionLampSelection.Capacity, ReflectionLampSelection.Rows,
                false, Image.Format.Rgbaf, bytes);
            _reflectionTexture.Update(_reflectionImage);
        }
    }
}
