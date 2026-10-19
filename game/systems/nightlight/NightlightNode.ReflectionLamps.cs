using System.Runtime.InteropServices;
using Campus.Contracts;
using Campus.Systems.Nightlight.Glow;
using Campus.Systems.Nightlight.Lamps;
using Godot;

namespace Campus.Systems.Nightlight;

public partial class NightlightNode
{
    private static readonly StringName ReflectionLampsGlobal = ShaderGlobals.ReflLamps;
    private readonly ReflectionLampSelection _reflectionSelection = new();
    private ICameraRig? _reflectionCamera;
    private GlowSet? _reflectionWindows;
    private HashSet<FeatureId>? _reflectionWaterfrontBuildings;
    private int[] _reflectionWindowIndices = [];
    private Image? _reflectionImage;
    private ImageTexture? _reflectionTexture;

    private void ClearReflectionLamps()
    {
        _reflectionSelection.Clear();
        _reflectionWindows = null;
        _reflectionWaterfrontBuildings = null;
        _reflectionWindowIndices = [];
        UploadReflectionLamps();
    }

    private void UpdateReflectionLamps()
    {
        if (_reflectionCamera is null) return;
        var camera = _reflectionCamera.Pose.Position;
        if (_reflectionSelection.Select(Lamps, _reflectionWindows, _reflectionWindowIndices,
                _channels, _windows.Data, camera)) UploadReflectionLamps();
    }

    private void UploadReflectionLamps()
    {
        var bytes = MemoryMarshal.AsBytes(_reflectionSelection.Data);
        if (_reflectionImage is null || _reflectionTexture is null)
        {
            _reflectionImage = Image.CreateFromData(ReflectionLampSelection.Capacity, ReflectionLampSelection.Rows,
                false, Image.Format.Rgbaf, bytes);
            _reflectionTexture = ImageTexture.CreateFromImage(_reflectionImage);
        }
        else
        {
            _reflectionImage.SetData(ReflectionLampSelection.Capacity, ReflectionLampSelection.Rows,
                false, Image.Format.Rgbaf, bytes);
            _reflectionTexture.Update(_reflectionImage);
        }
        RenderingServer.GlobalShaderParameterSet(ReflectionLampsGlobal, _reflectionTexture);
    }
}
