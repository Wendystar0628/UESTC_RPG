using Campus.Contracts;
using KernelServices = Campus.Kernel.Registry.Kernel;
using Godot;
using Godot.Collections;

namespace Campus.Kernel.Post;

/// <summary>
/// The PostTransparent CompositorEffect: updates the runtime colour LUT when CDL changes, then applies
/// the highlight shoulder and vignette before the engine's glow, tonemap and LUT (ENG-PERF).
/// The finish dispatch always runs because the shoulder is a style floor (SYS-GRAPHICS).
/// </summary>
public partial class FinishEffect : CompositorEffect
{
    private static readonly StringName FinishVersion = "finish";
    private static readonly StringName LutVersion = "lut";
    internal const int LutSize = 32;
    private const float ShoulderStart = 1.0f;     // at or above every registered lit colour (peak 1.0)
    private const float ShoulderHeadroom = 0.5f;
    private const float BaseSaturation = 1.02f;   // static base table (ART-STYLE)
    private const float SplitToning = 1f;
    internal const float CdlEpsilon = 1e-4f;

    private readonly RDShaderSpirV _spirvFinish;
    private readonly RDShaderSpirV _spirvLut;
    private readonly float[] _pc = new float[16];
    private readonly byte[] _pcBytes = new byte[16 * sizeof(float)];
    private readonly RDUniform _uColor = new() { UniformType = RenderingDevice.UniformType.Image, Binding = 0 };
    private readonly Array<RDUniform> _setFinish;
    private readonly object _lutGate = new();
    private RenderingDevice? _rd;
    private Rid _shaderFinish, _pipelineFinish, _shaderLut, _pipelineLut, _lutTexture, _lutSet;
    private Vector3 _bakedSlope = new(float.NaN, 0, 0), _bakedOffset, _bakedPower;
    private float _bakedSaturation;
    private bool _ready, _released;
    private bool _rendered;
    private Exception? _error;
    internal Exception? Failure => Volatile.Read(ref _error);
    internal bool Rendered => Volatile.Read(ref _rendered);

    internal PostFrameBox? Inputs { get; set; }

    /// <summary>RD texture of the runtime LUT once created on the render thread; invalid before that.</summary>
    internal Rid LutTexture
    {
        get { lock (_lutGate) return _lutTexture; }
    }

    public FinishEffect()
    {
        EffectCallbackType = EffectCallbackTypeEnum.PostTransparent;
        _setFinish = [_uColor];
        var file = KernelServices.Get<IRequiredResources>().Load<RDShaderFile>(OutlineEffect.ShaderPath);
        _spirvFinish = file.GetSpirV(FinishVersion);
        _spirvLut = file.GetSpirV(LutVersion);
        if (_spirvFinish.CompileErrorCompute.Length > 0 || _spirvLut.CompileErrorCompute.Length > 0)
            throw new InvalidDataException("Finish shader compilation failed: " +
                _spirvFinish.CompileErrorCompute + _spirvLut.CompileErrorCompute);
    }

    private bool EnsureResources()
    {
        if (_released || _error is not null) return false;
        if (_ready) return true;
        _rd = RenderingServer.GetRenderingDevice()
            ?? throw new InvalidOperationException("Finish requires a rendering device.");
        _shaderFinish = _rd.ShaderCreateFromSpirV(_spirvFinish, "post_outline_finish");
        _shaderLut = _rd.ShaderCreateFromSpirV(_spirvLut, "post_outline_lut");
        if (!_shaderFinish.IsValid || !_shaderLut.IsValid)
            throw new InvalidOperationException("Finish shader creation failed.");
        _pipelineFinish = _rd.ComputePipelineCreate(_shaderFinish);
        _pipelineLut = _rd.ComputePipelineCreate(_shaderLut);
        var lut = _rd.TextureCreate(new RDTextureFormat
        {
            Format = RenderingDevice.DataFormat.R16G16B16A16Sfloat,
            TextureType = RenderingDevice.TextureType.Type3D,
            Width = LutSize,
            Height = LutSize,
            Depth = LutSize,
            ArrayLayers = 1,
            Mipmaps = 1,
            UsageBits = RenderingDevice.TextureUsageBits.SamplingBit | RenderingDevice.TextureUsageBits.StorageBit
                | RenderingDevice.TextureUsageBits.CanCopyFromBit,
        }, new RDTextureView());
        lock (_lutGate) _lutTexture = lut;
        if (!_pipelineFinish.IsValid || !_pipelineLut.IsValid || !lut.IsValid)
            throw new InvalidOperationException("Finish pipeline or LUT texture creation failed.");
        var image = new RDUniform { UniformType = RenderingDevice.UniformType.Image, Binding = 0 };
        image.AddId(lut);
        _lutSet = _rd.UniformSetCreate([image], _shaderLut, 0);
        _ready = _pipelineFinish.IsValid && _pipelineLut.IsValid && lut.IsValid && _lutSet.IsValid;
        if (!_ready) throw new InvalidOperationException("Finish LUT uniform set creation failed.");
        return _ready;
    }

    public override void _RenderCallback(int effectCallbackType, RenderData renderData)
    {
        if (effectCallbackType != (int)EffectCallbackTypeEnum.PostTransparent) return;
        if (Inputs is null || !Inputs.TryTakeFinish(out var frame)) return;
        try { Render(frame, renderData); }
        catch (Exception error) { EffectFailure.Record(ref _error, error); }
    }

    private void Render(in PostFrame frame, RenderData renderData)
    {
        if (!EnsureResources() || _rd is null) return;
        BakeLutIfChanged(frame);
        if (renderData.GetRenderSceneBuffers() is not RenderSceneBuffersRD buffers)
            throw new InvalidOperationException("Finish requires rendering-device scene buffers.");
        var size = buffers.GetInternalSize();
        if (size.X <= 0 || size.Y <= 0) return;

        Set(0, size.X, size.Y, 1f / size.X, 1f / size.Y);
        Set(4, frame.VignetteTint.X, frame.VignetteTint.Y, frame.VignetteTint.Z, frame.VignetteK);
        Set(8, ShoulderStart, ShoulderHeadroom, 0, 0);
        System.Buffer.BlockCopy(_pc, 0, _pcBytes, 0, _pcBytes.Length);
        _uColor.ClearIds();
        var color = buffers.GetColorLayer(0);
        if (!color.IsValid) throw new InvalidOperationException("Finish color buffer is unavailable.");
        _uColor.AddId(color);
        var set = UniformSetCacheRD.GetCache(_shaderFinish, 0, _setFinish);
        if (!set.IsValid) throw new InvalidOperationException("Finish uniform set creation failed.");
        var list = _rd.ComputeListBegin();
        _rd.ComputeListBindComputePipeline(list, _pipelineFinish);
        _rd.ComputeListBindUniformSet(list, set, 0);
        _rd.ComputeListSetPushConstant(list, _pcBytes, 12 * sizeof(float));
        _rd.ComputeListDispatch(list, (uint)((size.X + 7) / 8), (uint)((size.Y + 7) / 8), 1);
        _rd.ComputeListEnd();
        Inputs!.PublishDimensions(frame.RenderGeneration,
            new((ulong)Engine.GetFramesDrawn(), buffers.GetTargetSize(), size));
        Volatile.Write(ref _rendered, true);
    }

    private void BakeLutIfChanged(in PostFrame f)
    {
        if (_rd is null) return;
        if (Near(f.CdlSlope, _bakedSlope) && Near(f.CdlOffset, _bakedOffset) && Near(f.CdlPower, _bakedPower)
            && Mathf.Abs(f.CdlSaturation - _bakedSaturation) < CdlEpsilon) return;
        _bakedSlope = f.CdlSlope;
        _bakedOffset = f.CdlOffset;
        _bakedPower = f.CdlPower;
        _bakedSaturation = f.CdlSaturation;
        Set(0, f.CdlSlope.X, f.CdlSlope.Y, f.CdlSlope.Z, LutSize);
        Set(4, f.CdlOffset.X, f.CdlOffset.Y, f.CdlOffset.Z, f.CdlSaturation);
        Set(8, f.CdlPower.X, f.CdlPower.Y, f.CdlPower.Z, BaseSaturation);
        Set(12, SplitToning, 0, 0, 0);
        System.Buffer.BlockCopy(_pc, 0, _pcBytes, 0, _pcBytes.Length);
        var list = _rd.ComputeListBegin();
        _rd.ComputeListBindComputePipeline(list, _pipelineLut);
        _rd.ComputeListBindUniformSet(list, _lutSet, 0);
        _rd.ComputeListSetPushConstant(list, _pcBytes, (uint)_pcBytes.Length);
        _rd.ComputeListDispatch(list, LutSize / 4, LutSize / 4, LutSize / 4);
        _rd.ComputeListEnd();
    }

    private static bool Near(Vector3 a, Vector3 b) =>
        Mathf.Abs(a.X - b.X) < CdlEpsilon && Mathf.Abs(a.Y - b.Y) < CdlEpsilon && Mathf.Abs(a.Z - b.Z) < CdlEpsilon;

    private void Set(int at, float x, float y, float z, float w)
    {
        _pc[at] = x;
        _pc[at + 1] = y;
        _pc[at + 2] = z;
        _pc[at + 3] = w;
    }

    /// <summary>Frees the GPU objects; call on the render thread (RenderingServer.CallOnRenderThread) before exit.</summary>
    internal void ReleaseGpuResources()
    {
        if (_rd is null) return;
        Rid lut;
        lock (_lutGate)
        {
            lut = _lutTexture;
            _lutTexture = default;
        }
        if (lut.IsValid) _rd.FreeRid(lut);   // also frees the LUT uniform set
        if (_shaderFinish.IsValid) _rd.FreeRid(_shaderFinish);
        if (_shaderLut.IsValid) _rd.FreeRid(_shaderLut);
        _shaderFinish = _pipelineFinish = _shaderLut = _pipelineLut = _lutSet = default;
        _ready = false;
        _released = true;
    }
}
