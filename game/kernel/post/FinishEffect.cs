using Godot;
using Godot.Collections;

namespace Campus.Kernel.Post;

/// <summary>
/// The PostTransparent CompositorEffect (ENG-PERF §10.2 step 6). Every frame it
/// <list type="number">
/// <item>re-bakes the runtime colour LUT (version lut of style/outline/post_outline.glsl: CDL ∘ static base table,
/// 32³) when the CDL moved, so the engine's tonemap pass reads it later in the same frame through
/// Environment.AdjustmentColorCorrection;</item>
/// <item>runs the compose dispatch (version tiltshift_comp of the same file): highlight shoulder on the overflow only
/// (registered lit colours unchanged), vignette, and the tilt-shift blend (amount 0 until the blur passes land).</item>
/// </list>
/// The compose dispatch always runs: the shoulder is a style floor (SYS-GRAPHICS §2).
/// </summary>
public partial class FinishEffect : CompositorEffect
{
    private static readonly StringName ComposeVersion = "tiltshift_comp";
    private static readonly StringName LutVersion = "lut";
    internal const int LutSize = 32;
    private const float ShoulderStart = 1.0f;     // at or above every registered lit colour (peak 1.0)
    private const float ShoulderHeadroom = 0.5f;
    private const float TiltMaxBlend = 0.25f;     // ART-STYLE §7.4
    private const float BaseSaturation = 1.02f;   // static base table (ART-STYLE §7.2)
    private const float SplitToning = 1f;
    internal const float CdlEpsilon = 1e-4f;

    private readonly RDShaderSpirV? _spirvCompose;
    private readonly RDShaderSpirV? _spirvLut;
    private readonly float[] _pc = new float[16];
    private readonly byte[] _pcBytes = new byte[16 * sizeof(float)];
    private readonly RDUniform _uColor = new() { UniformType = RenderingDevice.UniformType.Image, Binding = 0 };
    private readonly RDUniform _uBlur = new() { UniformType = RenderingDevice.UniformType.SamplerWithTexture, Binding = 1 };
    private readonly Array<RDUniform> _setCompose;
    private readonly object _lutGate = new();
    private RenderingDevice? _rd;
    private Rid _shaderCompose, _pipelineCompose, _shaderLut, _pipelineLut, _lutTexture, _lutSet, _sampler, _blurPlaceholder;
    private Vector3 _bakedSlope = new(float.NaN, 0, 0), _bakedOffset, _bakedPower;
    private float _bakedSaturation;
    private bool _ready, _failed;

    internal PostFrameBox? Inputs { get; set; }

    /// <summary>Read-only diagnostic sink (null or disabled = off; the render path is then unchanged).</summary>
    internal PostDiagSink? Diagnostics { get; set; }

    /// <summary>RD texture of the runtime LUT once created on the render thread; invalid before that.</summary>
    internal Rid LutTexture
    {
        get { lock (_lutGate) return _lutTexture; }
    }

    public FinishEffect()
    {
        EffectCallbackType = EffectCallbackTypeEnum.PostTransparent;
        _setCompose = [_uColor, _uBlur];
        var file = GD.Load<RDShaderFile>(OutlineEffect.ShaderPath);
        if (file is null)
        {
            GD.PushError($"WP-05 finish: missing {OutlineEffect.ShaderPath}");
            _failed = true;
            return;
        }
        _spirvCompose = file.GetSpirV(ComposeVersion);
        _spirvLut = file.GetSpirV(LutVersion);
    }

    private bool EnsureResources()
    {
        if (_ready) return true;
        if (_failed || _spirvCompose is null || _spirvLut is null) return false;
        if (_spirvCompose.CompileErrorCompute.Length > 0 || _spirvLut.CompileErrorCompute.Length > 0)
        {
            GD.PushError("WP-05 finish: " + _spirvCompose.CompileErrorCompute + _spirvLut.CompileErrorCompute);
            _failed = true;
            return false;
        }
        _rd = RenderingServer.GetRenderingDevice();
        _shaderCompose = _rd.ShaderCreateFromSpirV(_spirvCompose, "post_outline_tiltshift_comp");
        _pipelineCompose = _rd.ComputePipelineCreate(_shaderCompose);
        _shaderLut = _rd.ShaderCreateFromSpirV(_spirvLut, "post_outline_lut");
        _pipelineLut = _rd.ComputePipelineCreate(_shaderLut);
        _sampler = _rd.SamplerCreate(new RDSamplerState
        {
            MagFilter = RenderingDevice.SamplerFilter.Linear,
            MinFilter = RenderingDevice.SamplerFilter.Linear,
            RepeatU = RenderingDevice.SamplerRepeatMode.ClampToEdge,
            RepeatV = RenderingDevice.SamplerRepeatMode.ClampToEdge,
        });
        // Until the tilt-shift blur passes exist the compose shader samples nothing (amount 0), but the binding
        // must still hold a texture.
        _blurPlaceholder = _rd.TextureCreate(new RDTextureFormat
        {
            Format = RenderingDevice.DataFormat.R16G16B16A16Sfloat,
            TextureType = RenderingDevice.TextureType.Type2D,
            Width = 4,
            Height = 4,
            Depth = 1,
            ArrayLayers = 1,
            Mipmaps = 1,
            UsageBits = RenderingDevice.TextureUsageBits.SamplingBit | RenderingDevice.TextureUsageBits.CanUpdateBit,
        }, new RDTextureView(), [new byte[4 * 4 * 8]]);
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
        var image = new RDUniform { UniformType = RenderingDevice.UniformType.Image, Binding = 0 };
        image.AddId(lut);
        _lutSet = _rd.UniformSetCreate([image], _shaderLut, 0);
        _uBlur.AddId(_sampler);
        _uBlur.AddId(_blurPlaceholder);
        lock (_lutGate) _lutTexture = lut;
        _ready = _pipelineCompose.IsValid && _pipelineLut.IsValid && lut.IsValid && _lutSet.IsValid;
        if (!_ready)
        {
            _failed = true;
            GD.PushError("WP-05 finish: pipeline creation failed");
        }
        return _ready;
    }

    public override void _RenderCallback(int effectCallbackType, RenderData renderData)
    {
        if (effectCallbackType != (int)EffectCallbackTypeEnum.PostTransparent) return;
        if (Inputs is null || !Inputs.TryRead(out var frame)) return;
        if (!EnsureResources() || _rd is null) return;
        var diag = Diagnostics;
        if (diag is { Enabled: true }) BakeAndCapture(diag, frame);
        else BakeLutIfChanged(frame);
        if (renderData.GetRenderSceneBuffers() is not RenderSceneBuffersRD buffers) return;
        var size = buffers.GetInternalSize();
        if (size.X <= 0 || size.Y <= 0) return;

        Set(0, size.X, size.Y, 1f / size.X, 1f / size.Y);
        Set(4, frame.TiltAmount, TiltMaxBlend, 0, 0);
        Set(8, frame.VignetteTint.X, frame.VignetteTint.Y, frame.VignetteTint.Z, frame.VignetteK);
        Set(12, ShoulderStart, ShoulderHeadroom, 0, 0);
        System.Buffer.BlockCopy(_pc, 0, _pcBytes, 0, _pcBytes.Length);
        _uColor.ClearIds();
        _uColor.AddId(buffers.GetColorLayer(0));
        var set = UniformSetCacheRD.GetCache(_shaderCompose, 0, _setCompose);
        var list = _rd.ComputeListBegin();
        _rd.ComputeListBindComputePipeline(list, _pipelineCompose);
        _rd.ComputeListBindUniformSet(list, set, 0);
        _rd.ComputeListSetPushConstant(list, _pcBytes, (uint)_pcBytes.Length);
        _rd.ComputeListDispatch(list, (uint)((size.X + 7) / 8), (uint)((size.Y + 7) / 8), 1);
        _rd.ComputeListEnd();
    }

    // Diagnostic variant of the bake call: same decision, plus an immutable snapshot of what this callback saw.
    // Render thread only; reads the render thread's own baked fields, never writes them.
    private void BakeAndCapture(PostDiagSink diag, in PostFrame f)
    {
        var before = Baked();
        var beforeValid = !float.IsNaN(_bakedSlope.X);
        var baked = false;
        BakeLutIfChanged(f, ref baked);
        diag.Capture(f.DiagWorldInstance, f.DiagMainFrame, Engine.GetFramesDrawn(),
            new CdlSample(f.CdlSlope.X, f.CdlSlope.Y, f.CdlSlope.Z, f.CdlOffset.X, f.CdlOffset.Y, f.CdlOffset.Z,
                f.CdlPower.X, f.CdlPower.Y, f.CdlPower.Z, f.CdlSaturation),
            beforeValid, before, Baked(), baked, LutSize, BaseSaturation, SplitToning);
    }

    private CdlSample Baked() => new(_bakedSlope.X, _bakedSlope.Y, _bakedSlope.Z, _bakedOffset.X, _bakedOffset.Y,
        _bakedOffset.Z, _bakedPower.X, _bakedPower.Y, _bakedPower.Z, _bakedSaturation);

    private void BakeLutIfChanged(in PostFrame f)
    {
        var baked = false;
        BakeLutIfChanged(f, ref baked);
    }

    private void BakeLutIfChanged(in PostFrame f, ref bool baked)
    {
        if (_rd is null) return;
        if (Near(f.CdlSlope, _bakedSlope) && Near(f.CdlOffset, _bakedOffset) && Near(f.CdlPower, _bakedPower)
            && Mathf.Abs(f.CdlSaturation - _bakedSaturation) < CdlEpsilon) return;
        baked = true;
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
        if (_shaderCompose.IsValid) _rd.FreeRid(_shaderCompose);
        if (_shaderLut.IsValid) _rd.FreeRid(_shaderLut);
        if (_sampler.IsValid) _rd.FreeRid(_sampler);
        if (_blurPlaceholder.IsValid) _rd.FreeRid(_blurPlaceholder);
        _shaderCompose = _pipelineCompose = _shaderLut = _pipelineLut = _lutSet = _sampler = _blurPlaceholder = default;
        _ready = false;
        _failed = true;
    }
}
