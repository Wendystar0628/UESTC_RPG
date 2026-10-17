using Campus.Kernel.Renderer;
using Godot;
using Godot.Collections;

namespace Campus.Kernel.Post;

/// <summary>
/// The outline CompositorEffect (ART-STYLE §4.5, ENG-PERF §10.2): EffectCallbackType PostSky, NeedsNormalRoughness,
/// two dispatches of style/outline/post_outline.glsl (mask, then composite) on the internal-resolution buffers,
/// before any upscaling. The intermediate RG8 mask lives in the scene buffers' own context and is rebuilt
/// whenever the buffers are reconfigured (render scale, window size, anti-aliasing: SYS-GRAPHICS §4).
/// Runs on the render thread; inputs arrive through <see cref="PostFrameBox"/> and nothing is allocated per frame.
/// </summary>
public partial class OutlineEffect : CompositorEffect
{
    internal const string ShaderPath = "res://style/outline/post_outline.glsl";   // every WP-05 kernel is a version of it
    private const int UboFloats = 96;   // layout of OutlineParams in post_outline.glsl

    private static readonly StringName Context = "wp05_outline";
    private static readonly StringName MaskName = "line_mask";
    private static readonly StringName ForwardContext = "forward_clustered";
    private static readonly StringName NormalRoughness = "normal_roughness";
    private static readonly StringName MaskVersion = "mask";
    private static readonly StringName CompositeVersion = "composite";

    private readonly RDShaderSpirV? _spirvMask;
    private readonly RDShaderSpirV? _spirvComposite;
    private readonly float[] _ubo = new float[UboFloats];
    private readonly byte[] _uboBytes = new byte[UboFloats * sizeof(float)];
    private readonly RDUniform _uUbo = new() { UniformType = RenderingDevice.UniformType.UniformBuffer, Binding = 0 };
    private readonly RDUniform _uDepth = new() { UniformType = RenderingDevice.UniformType.SamplerWithTexture, Binding = 1 };
    private readonly RDUniform _uNormal = new() { UniformType = RenderingDevice.UniformType.SamplerWithTexture, Binding = 2 };
    private readonly RDUniform _uMatTable = new() { UniformType = RenderingDevice.UniformType.SamplerWithTexture, Binding = 3 };
    private readonly RDUniform _uMask = new() { UniformType = RenderingDevice.UniformType.Image, Binding = 4 };
    private readonly RDUniform _uColor = new() { UniformType = RenderingDevice.UniformType.Image, Binding = 5 };
    private readonly RDUniform _uPalette = new() { UniformType = RenderingDevice.UniformType.SamplerWithTexture, Binding = 6 };
    private readonly Array<RDUniform> _setMask;
    private readonly Array<RDUniform> _setComposite;
    private RenderingDevice? _rd;
    private Rid _shaderMask, _shaderComposite, _pipelineMask, _pipelineComposite, _sampler, _uniformBuffer;
    private bool _ready, _failed;

    internal PostFrameBox? Inputs { get; set; }

    public OutlineEffect()
    {
        EffectCallbackType = EffectCallbackTypeEnum.PostSky;
        NeedsNormalRoughness = true;
        _setMask = [_uUbo, _uDepth, _uNormal, _uMatTable, _uMask];
        _setComposite = [_uUbo, _uDepth, _uNormal, _uMatTable, _uMask, _uColor, _uPalette];
        // Shader files load on the main thread; GPU objects are created lazily on the render thread.
        var file = GD.Load<RDShaderFile>(ShaderPath);
        if (file is null)
        {
            GD.PushError($"WP-05 outline: missing {ShaderPath}");
            _failed = true;
            return;
        }
        _spirvMask = file.GetSpirV(MaskVersion);
        _spirvComposite = file.GetSpirV(CompositeVersion);
    }

    private bool EnsureResources()
    {
        if (_ready) return true;
        if (_failed || _spirvMask is null || _spirvComposite is null) return false;
        if (_spirvMask.CompileErrorCompute.Length > 0 || _spirvComposite.CompileErrorCompute.Length > 0)
        {
            GD.PushError("WP-05 outline: " + _spirvMask.CompileErrorCompute + _spirvComposite.CompileErrorCompute);
            _failed = true;
            return false;
        }
        _rd = RenderingServer.GetRenderingDevice();
        _shaderMask = _rd.ShaderCreateFromSpirV(_spirvMask, "post_outline_mask");
        _shaderComposite = _rd.ShaderCreateFromSpirV(_spirvComposite, "post_outline_composite");
        _pipelineMask = _rd.ComputePipelineCreate(_shaderMask);
        _pipelineComposite = _rd.ComputePipelineCreate(_shaderComposite);
        _sampler = _rd.SamplerCreate(new RDSamplerState
        {
            MagFilter = RenderingDevice.SamplerFilter.Nearest,
            MinFilter = RenderingDevice.SamplerFilter.Nearest,
            RepeatU = RenderingDevice.SamplerRepeatMode.ClampToEdge,
            RepeatV = RenderingDevice.SamplerRepeatMode.ClampToEdge,
        });
        _uniformBuffer = _rd.UniformBufferCreate((uint)_uboBytes.Length, _uboBytes);
        _uUbo.AddId(_uniformBuffer);
        _ready = _pipelineMask.IsValid && _pipelineComposite.IsValid && _uniformBuffer.IsValid;
        if (!_ready)
        {
            _failed = true;
            GD.PushError("WP-05 outline: pipeline creation failed");
        }
        return _ready;
    }

    public override void _RenderCallback(int effectCallbackType, RenderData renderData)
    {
        if (effectCallbackType != (int)EffectCallbackTypeEnum.PostSky) return;
        if (Inputs is null || !Inputs.TryTake(out var batch)) return;
        try
        {
            Render(batch.Frame, renderData, batch.Palette, batch.MatTable);
        }
        finally
        {
            // After issuing the commands (IGlobalDataTextureRegistry lease protocol).
            batch.Dispose();
        }
    }

    private void Render(in PostFrame frame, RenderData renderData, GlobalDataTextureLease? paletteLease,
        GlobalDataTextureLease? matTableLease)
    {
        if (!EnsureResources() || _rd is null) return;
        if (renderData.GetRenderSceneBuffers() is not RenderSceneBuffersRD buffers) return;
        var size = buffers.GetInternalSize();
        if (size.X <= 0 || size.Y <= 0) return;
        // RD RIDs are looked up again every frame: a producer's SetImage may replace the RD texture.
        var matTable = matTableLease is not null ? matTableLease.GetRdTextureOnRenderThread(false)
            : RenderingServer.TextureGetRdTexture(frame.FallbackMatTable);
        var palette = paletteLease is not null ? paletteLease.GetRdTextureOnRenderThread(true)   // sRGB view: linear on fetch
            : RenderingServer.TextureGetRdTexture(frame.FallbackPalette, true);
        if (!matTable.IsValid || !palette.IsValid) return;

        // Own intermediate texture: dropped with the context whenever the buffers are reconfigured; size-checked too.
        if (buffers.HasTexture(Context, MaskName) && buffers.GetTextureSliceSize(Context, MaskName, 0) != size)
            buffers.ClearContext(Context);
        if (!buffers.HasTexture(Context, MaskName))
        {
            buffers.CreateTexture(Context, MaskName, RenderingDevice.DataFormat.R8G8Unorm,
                (uint)(RenderingDevice.TextureUsageBits.StorageBit | RenderingDevice.TextureUsageBits.SamplingBit),
                RenderingDevice.TextureSamples.Samples1, size, 1, 1, true, false);
        }

        WriteParams(frame, renderData.GetRenderSceneData(), size);
        _rd.BufferUpdate(_uniformBuffer, 0, (uint)_uboBytes.Length, _uboBytes);

        Bind(_uDepth, _sampler, buffers.GetDepthLayer(0));
        Bind(_uNormal, _sampler, buffers.GetTextureSlice(ForwardContext, NormalRoughness, 0, 0, 1, 1));
        Bind(_uMatTable, _sampler, matTable);
        Bind(_uPalette, _sampler, palette);
        _uMask.ClearIds();
        _uMask.AddId(buffers.GetTexture(Context, MaskName));
        _uColor.ClearIds();
        _uColor.AddId(buffers.GetColorLayer(0));
        var setMask = UniformSetCacheRD.GetCache(_shaderMask, 0, _setMask);
        var setComposite = UniformSetCacheRD.GetCache(_shaderComposite, 0, _setComposite);

        uint groupsX = (uint)((size.X + 7) / 8), groupsY = (uint)((size.Y + 7) / 8);
        var list = _rd.ComputeListBegin();
        _rd.ComputeListBindComputePipeline(list, _pipelineMask);
        _rd.ComputeListBindUniformSet(list, setMask, 0);
        _rd.ComputeListDispatch(list, groupsX, groupsY, 1);
        _rd.ComputeListAddBarrier(list);
        _rd.ComputeListBindComputePipeline(list, _pipelineComposite);
        _rd.ComputeListBindUniformSet(list, setComposite, 0);
        _rd.ComputeListDispatch(list, groupsX, groupsY, 1);
        _rd.ComputeListEnd();
    }

    private static void Bind(RDUniform uniform, Rid sampler, Rid texture)
    {
        uniform.ClearIds();
        uniform.AddId(sampler);
        uniform.AddId(texture);
    }

    private void WriteParams(in PostFrame f, RenderSceneData scene, Vector2I size)
    {
        // GetCamProjection() is already the device projection (y flipped, reverse-Z remapped): invert as is.
        var inv = scene.GetCamProjection().Inverse();
        var cam = scene.GetCamTransform();
        Vector3 bx = cam.Basis.Column0, by = cam.Basis.Column1, bz = cam.Basis.Column2, origin = cam.Origin;
        var key = f.KeyDir.LengthSquared() > 1e-8f ? f.KeyDir.Normalized() : Vector3.Up;
        var keyView = new Vector3(key.Dot(bx), key.Dot(by), key.Dot(bz));
        var rimDir = new Vector2(keyView.X, -keyView.Y);
        rimDir = rimDir.LengthSquared() > 1e-8f ? rimDir.Normalized() : Vector2.Zero;

        Put(0, inv.X); Put(4, inv.Y); Put(8, inv.Z); Put(12, inv.W);
        Put(16, new Vector4(bx.X, bx.Y, bx.Z, 0)); Put(20, new Vector4(by.X, by.Y, by.Z, 0));
        Put(24, new Vector4(bz.X, bz.Y, bz.Z, 0)); Put(28, new Vector4(origin.X, origin.Y, origin.Z, 1));
        Put(32, new Vector4(size.X, size.Y, 1f / size.X, 1f / size.Y));
        Put(36, new Vector4(f.LineDarken, size.Y / 1080f, f.TentMix, f.DebugView));
        Put(40, new Vector4(keyView.X, keyView.Y, keyView.Z, f.KeyStrength));
        Put(44, new Vector4(f.RimColor.R, f.RimColor.G, f.RimColor.B, f.RimStrength));
        Put(48, new Vector4(rimDir.X, rimDir.Y, 0, 0));
        Put(52, new Vector4(f.FarRain, f.Flash, 0, 0));
        Put(56, f.Fog);
        Put(60, f.WeatherFog);
        Put(64, f.MapRect);
        Put(68, new Vector4(key.X, key.Y, key.Z, f.EdgeFog));
        Put(72, f.Medium);
        Put(76, new Vector4(f.MediumColor.R, f.MediumColor.G, f.MediumColor.B, 0));
        Put(80, f.FogLow);
        Put(84, new Vector4(f.Veil, 0, 0, 0));
        Put(88, new Vector4(f.SunDir.X, f.SunDir.Y, f.SunDir.Z, 0));
        Put(92, new Vector4(f.MoonDir.X, f.MoonDir.Y, f.MoonDir.Z, 0));
        System.Buffer.BlockCopy(_ubo, 0, _uboBytes, 0, _uboBytes.Length);
    }

    private void Put(int at, Vector4 v)
    {
        _ubo[at] = v.X;
        _ubo[at + 1] = v.Y;
        _ubo[at + 2] = v.Z;
        _ubo[at + 3] = v.W;
    }

    /// <summary>Frees the GPU objects; call on the render thread (RenderingServer.CallOnRenderThread) before exit.</summary>
    internal void ReleaseGpuResources()
    {
        if (_rd is null) return;
        // Freeing a shader also frees its pipelines and cached uniform sets.
        if (_shaderMask.IsValid) _rd.FreeRid(_shaderMask);
        if (_shaderComposite.IsValid) _rd.FreeRid(_shaderComposite);
        if (_sampler.IsValid) _rd.FreeRid(_sampler);
        if (_uniformBuffer.IsValid) _rd.FreeRid(_uniformBuffer);
        _shaderMask = _shaderComposite = _pipelineMask = _pipelineComposite = _sampler = _uniformBuffer = default;
        _ready = false;
        _failed = true;   // released for good: the effect is being torn down
    }
}
