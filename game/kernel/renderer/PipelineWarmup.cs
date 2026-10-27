using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Quality;
using Campus.Kernel.Diagnostics;
using KernelServices = Campus.Kernel.Registry.Kernel;
using Godot;

namespace Campus.Kernel.Renderer;

/// <summary>The renderer owns packaged pipeline preparation; map lifetime awaits this same task.</summary>
public interface IPipelinePreparation
{
    Task PrepareAsync(CancellationToken cancellationToken = default);
}

/// <summary>Instantiates the offline combinations once; the engine owns pipeline precompilation and ubershaders.</summary>
[KernelNode("pipeline-warmup")]
[KernelService(typeof(PipelineWarmup))]
[KernelService(typeof(IPipelinePreparation))]
public sealed partial class PipelineWarmup : Node, IPipelinePreparation
{
    private IToonMaterialProvider? _toon;
    private IShaderMaterialSource? _shaders;
    private Func<IReadOnlyList<PipelineCombination>>? _readCombinations;
    private TimeSpan _timeout;
    public void Bind(IToonMaterialProvider toon, IShaderMaterialSource shaders,
        Func<IReadOnlyList<PipelineCombination>> readCombinations, TimeSpan timeout)
    {
        if (IsInsideTree() || _toon is not null) throw new InvalidOperationException("Pipeline dependencies bind once before entering the tree.");
        _toon = toon; _shaders = shaders; _readCombinations = readCombinations; _timeout = timeout;
    }
    private Font? _canvasFont;
    private Task? _preparing;
    private bool _prepared;

    /// <summary>Borrows the formal UI font after the UI theme is initialized.</summary>
    public void SetCanvasFont(Font font)
    {
        ArgumentNullException.ThrowIfNull(font);
        _canvasFont = font;
    }

    /// <summary>The first mounted map submits the global packaged catalog; later maps share that completed preparation.</summary>
    public async Task PrepareAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_prepared) return;
        if (_preparing is { } current)
        {
            await current.WaitAsync(cancellationToken);
            return;
        }
        var readCombinations = _readCombinations ?? throw new InvalidOperationException("Pipeline dependencies have not been bound.");
        var preparation = PrepareCoreAsync(readCombinations(), _timeout, cancellationToken);
        _preparing = preparation;
        try
        {
            await preparation;
            _prepared = true;
        }
        finally { _preparing = null; }
    }

    private async Task PrepareCoreAsync(IReadOnlyList<PipelineCombination> combinations, TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsInsideTree()) throw new InvalidOperationException("Pipeline preparation requires a mounted renderer.");
        if (OS.GetThreadCallerId() != OS.GetMainThreadId())
            throw new InvalidOperationException("Pipeline instances must be mounted on the main thread.");
        var toon = _toon ?? throw new InvalidOperationException("Pipeline dependencies have not been bound.");
        var shaders = _shaders!;
        var materials = new Dictionary<(ToonKind? Kind, ShaderId? Shader), Material>();
        var owned = new List<Resource>();
        var spatial = new Node3D { Name = "PipelineInstances", Visible = false };
        var canvas = new CanvasLayer { Name = "PipelineCanvas", Layer = 4 };
        Exception? failure = null;
        try
        {
            AddChild(spatial);
            AddChild(canvas);
            ImageTexture? white = null;
            foreach (var combination in combinations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var key = (combination.Kind, combination.Shader);
                if (!materials.TryGetValue(key, out var material))
                {
                    material = combination.Kind is { } kind ? toon.CreatePipelineMaterial(kind)
                        : shaders.Create(combination.Shader ?? throw new InvalidDataException("Pipeline has no shader."));
                    materials.Add(key, material);
                    owned.Add(material);
                }
                if (material is not ShaderMaterial { Shader: { } shader })
                    throw new InvalidDataException("Pipeline material has no authored shader.");
                if (shader.GetMode() == Shader.Mode.CanvasItem)
                {
                    MountCanvas(combination, material, canvas, owned, ref white);
                    continue;
                }
                if (shader.GetMode() != Shader.Mode.Spatial || combination.Canvas != PipelineCanvasDraw.None)
                    throw new InvalidDataException("The baked combination does not describe a spatial or canvas pipeline.");
                MountSpatial(combination, material, spatial, owned);
            }
            // One submission lets Godot see hidden spatial instances and actually draw the canvas types.
            // This is a frame boundary, not compilation evidence or a stable-frame threshold.
            await FrameAwaiter.StartupReadyDrawAsync(this, static () => true,
                timeout, cancellationToken);
            var clarityCombination = combinations.First(item => item.Kind is not null && !item.Instanced &&
                item.Primitive == BakedPrimitive.Triangles && item.Canvas == PipelineCanvasDraw.None);
            await PrepareClarityAsync(clarityCombination, materials[(clarityCombination.Kind, clarityCombination.Shader)],
                timeout, cancellationToken);
        }
        catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        try { if (GodotObject.IsInstanceValid(spatial)) spatial.Free(); }
        catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        try { if (GodotObject.IsInstanceValid(canvas)) canvas.Free(); }
        catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        for (var i = owned.Count - 1; i >= 0; i--)
        {
            try { if (GodotObject.IsInstanceValid(owned[i])) owned[i].Dispose(); }
            catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private async Task PrepareClarityAsync(PipelineCombination combination, Material material, TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var quality = KernelServices.Get<IQualityService>();
        var scaleSpec = quality.Knobs.Single(spec => spec.Knob == QualityKnob.RenderScale);
        var aaSpec = quality.Knobs.Single(spec => spec.Knob == QualityKnob.AntiAliasing);
        var scales = scaleSpec.Values ?? throw new InvalidDataException("Render precision requires discrete startup combinations.");
        // A native render target keeps preparation independent of the interactive title and player's viewport.
        // Always is required because this viewport's texture is deliberately never presented in the UI.
        var viewport = new SubViewport
        {
            Name = "ClarityPreparation", Size = new Vector2I(64, 64), OwnWorld3D = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            UseDebanding = GetViewport().UseDebanding,
        };
        ArrayMesh? mesh = null;
        Exception? failure = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddChild(viewport);
            ViewportClarity.ApplyMeshLod(viewport, viewport.Size.Y);
            // The existing preparation triangle is clockwise from -Z, so view that face without changing its material.
            viewport.AddChild(new Camera3D { Current = true, Position = new Vector3(.5f, .5f, -2), Rotation = new Vector3(0, Mathf.Pi, 0) });
            mesh = CreateMesh(combination);
            mesh.SurfaceSetMaterial(0, material); // borrowed: PrepareCoreAsync owns and releases the catalog material
            viewport.AddChild(new MeshInstance3D { Mesh = mesh, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
            foreach (var scale in scales)
            {
                for (var aa = aaSpec.Min; aa <= aaSpec.Max; aa += aaSpec.Step)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var clarity = ViewportClarity.MapClarity(scale, aa, quality.Effective.Requested(QualityKnob.FsrSharpness));
                    ViewportClarity.ApplyClarity(viewport, clarity);
                    await FrameAwaiter.StartupReadyDrawAsync(this, () => viewport.GetCamera3D() is not null,
                        timeout, cancellationToken);
                }
            }
        }
        catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        try { if (GodotObject.IsInstanceValid(viewport)) viewport.Free(); }
        catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        try { mesh?.Dispose(); }
        catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private void MountCanvas(PipelineCombination combination, Material material, CanvasLayer canvas,
        List<Resource> owned, ref ImageTexture? white)
    {
        if (combination.Canvas == PipelineCanvasDraw.MsdfText &&
            (_canvasFont is null || !GodotObject.IsInstanceValid(_canvasFont)))
            throw new InvalidOperationException("The formal UI font must be registered before canvas preparation.");
        if (combination.Canvas == PipelineCanvasDraw.TexturedRect && white is null)
        {
            using var image = Image.CreateFromData(1, 1, false, Image.Format.Rgba8, [255, 255, 255, 255]);
            white = ImageTexture.CreateFromImage(image);
            owned.Add(white);
        }
        canvas.AddChild(new PipelineCanvas
        {
            Material = material, DrawType = combination.Canvas, Font = _canvasFont, Texture = white
        });
    }

    private static void MountSpatial(PipelineCombination combination, Material material, Node3D spatial,
        List<Resource> owned)
    {
        var mesh = CreateMesh(combination);
        owned.Add(mesh);
        mesh.SurfaceSetMaterial(0, material);
        GeometryInstance3D instance;
        if (combination.Instanced)
        {
            var multi = new MultiMesh
            {
                TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
                UseColors = false, UseCustomData = true, Mesh = mesh, InstanceCount = 1
            };
            owned.Add(multi);
            multi.SetInstanceTransform(0, Transform3D.Identity);
            instance = new MultiMeshInstance3D { Multimesh = multi };
        }
        else instance = new MeshInstance3D { Mesh = mesh };
        // Reflection proxies use an override in production; ordinary surfaces bind on the mesh.
        if (combination.Reflect) instance.MaterialOverride = material;
        instance.CastShadow = combination.Shadows switch
        {
            PipelineShadowMode.Off => GeometryInstance3D.ShadowCastingSetting.Off,
            PipelineShadowMode.On => GeometryInstance3D.ShadowCastingSetting.On,
            PipelineShadowMode.ShadowsOnly => GeometryInstance3D.ShadowCastingSetting.ShadowsOnly,
            PipelineShadowMode.DoubleSided => GeometryInstance3D.ShadowCastingSetting.DoubleSided,
            _ => throw new InvalidDataException("Unknown pipeline shadow state.")
        };
        instance.Visible = false;
        spatial.AddChild(instance);
    }

    private static ArrayMesh CreateMesh(PipelineCombination combination)
    {
        bool Has(Mesh.ArrayFormat bit) => (combination.Format & (ulong)bit) != 0;
        if (!Has(Mesh.ArrayFormat.FormatVertex) || Has(Mesh.ArrayFormat.FormatBones) ||
            Has(Mesh.ArrayFormat.FormatWeights) || Has(Mesh.ArrayFormat.FormatCustom3) || Has(Mesh.ArrayFormat.FlagUse2DVertices))
            throw new InvalidDataException("The catalog contains an unsupported spatial vertex format.");
        using var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = new Vector3[] { Vector3.Zero, Vector3.Right, Vector3.Up };
        if (Has(Mesh.ArrayFormat.FormatNormal)) arrays[(int)Mesh.ArrayType.Normal] = new Vector3[] { Vector3.Back, Vector3.Back, Vector3.Back };
        if (Has(Mesh.ArrayFormat.FormatTangent)) arrays[(int)Mesh.ArrayType.Tangent] = new float[] { 1, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 1 };
        if (Has(Mesh.ArrayFormat.FormatColor)) arrays[(int)Mesh.ArrayType.Color] = new Color[] { Colors.White, Colors.White, Colors.White };
        if (Has(Mesh.ArrayFormat.FormatTexUV)) arrays[(int)Mesh.ArrayType.TexUV] = new Vector2[] { Vector2.Zero, Vector2.Right, Vector2.Down };
        if (Has(Mesh.ArrayFormat.FormatTexUV2)) arrays[(int)Mesh.ArrayType.TexUV2] = new Vector2[] { Vector2.Zero, Vector2.Right, Vector2.Down };
        if (Has(Mesh.ArrayFormat.FormatIndex)) arrays[(int)Mesh.ArrayType.Index] = combination.Primitive switch
        {
            BakedPrimitive.Points => new[] { 0, 1, 2 },
            BakedPrimitive.Lines => new[] { 0, 1 },
            BakedPrimitive.LineStrip or BakedPrimitive.Triangles or BakedPrimitive.TriangleStrip => new[] { 0, 1, 2 },
            _ => throw new InvalidDataException("Unknown pipeline primitive.")
        };
        Custom(Mesh.ArrayFormat.FormatCustom0, Mesh.ArrayType.Custom0, Mesh.ArrayFormat.FormatCustom0Shift);
        Custom(Mesh.ArrayFormat.FormatCustom1, Mesh.ArrayType.Custom1, Mesh.ArrayFormat.FormatCustom1Shift);
        Custom(Mesh.ArrayFormat.FormatCustom2, Mesh.ArrayType.Custom2, Mesh.ArrayFormat.FormatCustom2Shift);
        var primitive = combination.Primitive switch
        {
            BakedPrimitive.Points => Mesh.PrimitiveType.Points,
            BakedPrimitive.Lines => Mesh.PrimitiveType.Lines,
            BakedPrimitive.LineStrip => Mesh.PrimitiveType.LineStrip,
            BakedPrimitive.Triangles => Mesh.PrimitiveType.Triangles,
            BakedPrimitive.TriangleStrip => Mesh.PrimitiveType.TriangleStrip,
            _ => throw new InvalidDataException("Unknown pipeline primitive.")
        };
        var mesh = new ArrayMesh();
        try
        {
            mesh.AddSurfaceFromArrays(primitive, arrays, flags: (Mesh.ArrayFormat)combination.Format);
            if (mesh.GetSurfaceCount() != 1) throw new InvalidDataException("Godot rejected the declared pipeline mesh.");
            return mesh;
        }
        catch { mesh.Dispose(); throw; }

        void Custom(Mesh.ArrayFormat bit, Mesh.ArrayType slot, Mesh.ArrayFormat shift)
        {
            if (!Has(bit)) return;
            var custom = (combination.Format >> (int)shift) & (ulong)Mesh.ArrayFormat.FormatCustomMask;
            if (custom != (ulong)Mesh.ArrayCustomFormat.RgbaFloat)
                throw new InvalidDataException("The catalog contains an unsupported custom vertex channel.");
            arrays[(int)slot] = new float[12];
        }
    }
}

/// <summary>One declared canvas drawing type, rendered below the application's opaque loading presentation.</summary>
internal sealed partial class PipelineCanvas : Node2D
{
    public PipelineCanvasDraw DrawType { get; init; }
    public Font? Font { get; init; }
    public Texture2D? Texture { get; init; }

    public override void _Draw()
    {
        var rect = new Rect2(Vector2.Zero, Vector2.One * 4);
        switch (DrawType)
        {
            case PipelineCanvasDraw.Rect: DrawRect(rect, Colors.White); break;
            case PipelineCanvasDraw.TexturedRect:
                DrawTextureRect(Texture ?? throw new InvalidOperationException("Missing canvas texture."), rect, false); break;
            case PipelineCanvasDraw.Polygon:
                DrawColoredPolygon(new Vector2[] { Vector2.Zero, Vector2.Right * 4, Vector2.Down * 4 }, Colors.White); break;
            case PipelineCanvasDraw.MsdfText:
                DrawString(Font ?? throw new InvalidOperationException("Missing formal UI font."), new(0, 16), "开"); break;
            case PipelineCanvasDraw.None:
            default: throw new InvalidDataException("The canvas catalog entry has no drawing type.");
        }
    }
}
