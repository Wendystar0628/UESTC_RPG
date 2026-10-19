using Campus.Contracts;
using Campus.Contracts.Bake;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Renderer;

/// <summary>Draws the actual requested materials behind presented opaque coverage before reporting pipeline readiness.</summary>
[KernelNode("material-warmup", Owner = "WP-01")]
[KernelService(typeof(MaterialWarmup), Owner = "WP-01")]
public partial class MaterialWarmup : Node
{
    public const int StableFrames = 30;
    private readonly record struct PipelineCounts(double Surface, double Draw, double Canvas)
    {
        public static PipelineCounts Read() => new(
            Performance.GetMonitor(Performance.Monitor.PipelineCompilationsSurface),
            Performance.GetMonitor(Performance.Monitor.PipelineCompilationsDraw),
            Performance.GetMonitor(Performance.Monitor.PipelineCompilationsCanvas));
    }
    private sealed record Fixture(Vector3[] Positions, Vector3[] Normals, int[] Indices,
        Vector2[] Uv, float[] Custom0, float[] Custom1, float[] Custom2);
    private readonly List<Resource> _owned = [];
    private readonly List<(Node? Spatial, Node? Canvas, int SpatialCount, int CanvasCount, long Revision, ulong Frame)> _retiring = [];
    private int _peakSamples;
    private IToonMaterialProvider? _materials;
    private IPaletteIndexResolver? _palettes;
    private IVeilState? _veil;
    private IVeilPresentation? _presentation;
    private IAppLoadingPresentation? _loading;
    private Camera3D? _camera;
    private Node3D? _samples3D;
    private CanvasLayer? _samples2D;
    private ToonWarmBatch? _batch;
    private ToonWarmBatch? _pending;
    private bool _pendingDirty = true;
    private Task<Fixture>? _fixtureTask;
    private bool _active;
    private bool _drawn;
    private bool _clearing;
    private long _lastCapturedRevision = -1;
    private int _lastCapturedMaterials;
    private int _stable;
    private PipelineCounts _previous;
    private Vector2 _viewportSize;
    private bool _awaitCover;
    private bool _ready;
    public bool IsReady { get => _ready && !HasPendingWarm; private set => _ready = value; }
    public string? Error { get; private set; }
    public bool SamplesReleased => _samples3D is null && _samples2D is null && _retiring.Count == 0;
    public bool HasPendingWarm => PendingWarm.Materials.Count > 0;

    private ToonWarmBatch PendingWarm
    {
        get
        {
            if (!_pendingDirty && _pending is not null) return _pending;
            // Capture may prune disposed materials and publish another invalidation while it runs.
            _pendingDirty = false;
            _pending = _materials!.CapturePendingWarm();
            return _pending;
        }
    }

    private void OnWarmChanged() => _pendingDirty = true;

    /// <summary>Includes detached roots awaiting QueueFree, which a scene-tree census cannot reach.</summary>
    public IReadOnlyList<Node> CaptureParameterRoots()
    {
        var roots = new List<Node>();
        Add(_samples3D); Add(_samples2D);
        foreach (var retired in _retiring) { Add(retired.Spatial); Add(retired.Canvas); }
        return roots;

        void Add(Node? node)
        {
            if (node is not null && GodotObject.IsInstanceValid(node)) roots.Add(node);
        }
    }

    /// <summary>Explicit composition for renderer fixtures; production uses the registered services.</summary>
    public void Configure(IToonMaterialProvider materials, IVeilState veil, IVeilPresentation presentation, Camera3D camera,
        IPaletteIndexResolver palettes, IAppLoadingPresentation loading)
    {
        if (IsInsideTree()) throw new InvalidOperationException("Configure warmup before mounting it.");
        _materials = materials;
        _palettes = palettes;
        _veil = veil;
        _presentation = presentation;
        _loading = loading;
        _camera = camera;
    }

    public override void _Ready()
    {
        ProcessPriority = FramePriority.Systems;
        _materials ??= Services.Get<IToonMaterialProvider>();
        _palettes ??= Services.Get<IPaletteCatalogSource>().Catalog;
        _veil ??= Services.Get<IVeilState>();
        _presentation ??= Services.Get<IVeilPresentation>();
        _loading ??= Services.Get<IAppLoadingPresentation>();
        _camera ??= Services.Get<Camera3D>();
        _pendingDirty = true;
        _materials.WarmChanged += OnWarmChanged;
        RenderingServer.Singleton.FramePostDraw += OnFrameDrawn;
    }

    public void Begin()
    {
        ClearSamples();
        Error = null;
        IsReady = false;
        _active = true;
        _awaitCover = false;
        _viewportSize = GetViewport().GetVisibleRect().Size;
        _fixtureTask ??= Task.Run(BuildFixture);
    }

    public void End(bool reset = false)
    {
        _active = false;
        ClearSamples();
        if (reset) { IsReady = false; Error = null; }
    }

    public override void _Process(double delta)
    {
        if (!_active || Error is not null) return;
        try
        {
            if (ResetForViewportChange()) return;
            if (_awaitCover && !Covered) return;
            _awaitCover = false;
            RequireCover();
            if (_batch is not null) return;
            if (_fixtureTask is null || !_fixtureTask.IsCompleted) return;
            var fixture = _fixtureTask.GetAwaiter().GetResult();
            var batch = PendingWarm;
            _lastCapturedRevision = batch.Revision;
            _lastCapturedMaterials = batch.Materials.Count;
            if (batch.Materials.Count == 0)
            {
                // An empty batch proves nothing about missing implementations; their IsReady stays false.
                IsReady = true;
                return;
            }
            IsReady = false;
            MountSamples(batch, fixture);
            _batch = batch;
            _drawn = false;
            _stable = 0;
            _viewportSize = GetViewport().GetVisibleRect().Size;
            _previous = PipelineCounts.Read();
        }
        catch (Exception error) { Fail(error); }
    }

    private void OnFrameDrawn()
    {
        ReportReleasedSamples();
        if (!_active || _batch is null || _clearing) return;
        try
        {
            if (ResetForViewportChange()) return;
            RequireCover();
            var counts = PipelineCounts.Read();
            // The first rendered frame establishes a baseline after the samples entered the tree.
            _stable = _drawn && counts == _previous ? _stable + 1 : 0;
            if (!_drawn)
                GD.Print($"[WP-01][phase=material-warmup] event=draw-baseline revision={_batch.Revision} frame={Engine.GetProcessFrames()}");
            _drawn = true;
            _previous = counts;
            if (_stable < StableFrames) return;
            var revision = _batch.Revision;
            var completed = _materials!.CompleteWarm(_batch);
            GD.Print($"[WP-01][phase=material-warmup] event=draw-complete revision={revision} accepted={completed} stable={_stable} frame={Engine.GetProcessFrames()}");
            ClearSamples();
            // A changed material/quality revision rejects stale evidence and is sampled again next frame.
            IsReady = completed && !HasPendingWarm;
        }
        catch (Exception error) { Fail(error); }
    }

    private void MountSamples(ToonWarmBatch batch, Fixture fixture)
    {
        foreach (var layout in batch.Materials.SelectMany(item => item.Layouts))
            if (layout.Deformation != WarmDeformation.None)
                throw new NotSupportedException($"No truthful warmup draw path exists for deformation {layout.Deformation}; a static sample cannot prove skinning readiness.");
        _samples3D = new Node3D { Name = "MaterialWarmSamples" };
        _camera!.AddChild(_samples3D);
        _samples2D = new CanvasLayer { Name = "MaterialWarmCanvas", Layer = 4 };
        AddChild(_samples2D);
        var total = Math.Max(1, batch.Materials.Sum(item => Math.Max(1, item.Layouts.Count)));
        var columns = (int)Math.Ceiling(Math.Sqrt(total));
        var visible = GetViewport().GetVisibleRect().Size;
        var aspect = visible.X / Math.Max(1f, visible.Y);
        var span = 2f * MathF.Tan(_camera.Fov * float.Pi / 360f) * .6f;
        var cell = span / columns;
        var draw = 0;
        foreach (var item in batch.Materials)
        {
            if (item.Material is not ShaderMaterial { Shader: { } shader })
                throw new NotSupportedException("Material warmup requires a real authored ShaderMaterial.");
            switch (shader.GetMode())
            {
                case Shader.Mode.Spatial:
                    if (item.Layouts.Count == 0)
                        throw new InvalidOperationException("A spatial warm material has no registered draw layout.");
                    foreach (var layout in item.Layouts)
                    {
                        var mesh = UploadFixture(fixture, item.Spec is { } spec ? _palettes!.Resolve(spec.Lit) : new PaletteIndex(0), layout);
                        _owned.Add(mesh);
                        if (!layout.Instanced)
                        {
                            Place(new MeshInstance3D
                            {
                                Mesh = mesh,
                                MaterialOverride = item.Material,
                                IgnoreOcclusionCulling = true
                            }, draw++);
                        }
                        else
                        {
                            var multi = new MultiMesh
                            {
                                TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
                                UseColors = false,
                                UseCustomData = true,
                                Mesh = mesh,
                                InstanceCount = 1
                            };
                            _owned.Add(multi);
                            multi.SetInstanceTransform(0, Transform3D.Identity);
                            multi.SetInstanceCustomData(0, new Color(0f, 0f, 0f, 0f));
                            Place(new MultiMeshInstance3D { Multimesh = multi, MaterialOverride = item.Material, IgnoreOcclusionCulling = true }, draw++);
                        }
                    }
                    break;
                case Shader.Mode.CanvasItem:
                    _samples2D!.AddChild(new ColorRect
                    {
                        Position = new(4 + draw % columns * 12, 4 + draw / columns * 12),
                        Size = new(10, 10),
                        Color = Colors.White,
                        Material = item.Material,
                        MouseFilter = Control.MouseFilterEnum.Ignore
                    });
                    draw++;
                    break;
                case Shader.Mode.Particles:
                case Shader.Mode.Sky:
                case Shader.Mode.Fog:
                default:
                    throw new NotSupportedException($"No truthful warmup draw path exists for shader mode {shader.GetMode()}.");
            }
        }
        var spatialCount = _samples3D.GetChildCount();
        var canvasCount = _samples2D.GetChildCount();
        var retiringCount = _retiring.Sum(item => item.SpatialCount + item.CanvasCount);
        _peakSamples = Math.Max(_peakSamples, spatialCount + canvasCount + retiringCount);
        GD.Print($"[WP-01][phase=warm-samples] event=mounted revision={batch.Revision} spatial={spatialCount} canvas={canvasCount} " +
            $"retiring={retiringCount} peak={_peakSamples} frame={Engine.GetProcessFrames()}");

        void Place(Node3D instance, int index)
        {
            var x = ((index % columns + .5f) / columns - .5f) * span * Math.Min(aspect, 1f);
            var y = (.5f - (index / columns + .5f) / columns) * span;
            instance.Position = new(x, y, -2f);
            instance.Scale = new(cell * .7f * Math.Min(aspect, 1f), cell * .7f, cell * .7f);
            _samples3D!.AddChild(instance);
        }
    }

    private static Fixture BuildFixture()
    {
        // Both windings keep the real material visible regardless of its authored cull mode.
        Vector3[] vertices = [new(-.5f, -.5f, 0), new(.5f, -.5f, 0), new(.5f, .5f, 0), new(-.5f, .5f, 0)];
        return new(vertices, [Vector3.Back, Vector3.Back, Vector3.Back, Vector3.Back],
            [0, 1, 2, 0, 2, 3, 0, 2, 1, 0, 3, 2], [new(0, 0), new(1, 0), new(1, 1), new(0, 1)],
            vertices.SelectMany(v => new[] { v.X, v.Y, v.Z, 1f }).ToArray(),
            [0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1],
            [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]);
    }

    private static ArrayMesh UploadFixture(Fixture source, PaletteIndex palette, WarmDrawLayout layout)
    {
        bool Has(Mesh.ArrayFormat bit) => (layout.Format & (ulong)bit) != 0;
        if (!Has(Mesh.ArrayFormat.FormatVertex) || Has(Mesh.ArrayFormat.FormatBones) || Has(Mesh.ArrayFormat.FormatWeights) ||
            Has(Mesh.ArrayFormat.FormatCustom3) || Has(Mesh.ArrayFormat.FlagUse2DVertices))
            throw new NotSupportedException($"Unsupported warm vertex layout: {layout.Format}.");
        var primitive = layout.Primitive switch
        {
            BakedPrimitive.Points => Mesh.PrimitiveType.Points,
            BakedPrimitive.Lines => Mesh.PrimitiveType.Lines,
            BakedPrimitive.LineStrip => Mesh.PrimitiveType.LineStrip,
            BakedPrimitive.Triangles => Mesh.PrimitiveType.Triangles,
            BakedPrimitive.TriangleStrip => Mesh.PrimitiveType.TriangleStrip,
            _ => throw new ArgumentOutOfRangeException(nameof(layout))
        };
        if (!Has(Mesh.ArrayFormat.FormatIndex) && layout.Primitive == BakedPrimitive.Triangles)
            throw new NotSupportedException("Non-indexed triangle warming requires a matching fixture.");
        using var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = source.Positions;
        if (Has(Mesh.ArrayFormat.FormatNormal)) arrays[(int)Mesh.ArrayType.Normal] = source.Normals;
        if (Has(Mesh.ArrayFormat.FormatTangent)) arrays[(int)Mesh.ArrayType.Tangent] = new float[] { 1, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 1 };
        if (Has(Mesh.ArrayFormat.FormatColor))
        {
            var rgba = PaletteEncoding.EncodeColor(palette, 255, 0);
            arrays[(int)Mesh.ArrayType.Color] = Enumerable.Repeat(new Color(
                PaletteEncoding.Unorm8Input(rgba.R), PaletteEncoding.Unorm8Input(rgba.G),
                PaletteEncoding.Unorm8Input(rgba.B), PaletteEncoding.Unorm8Input(rgba.A)), 4).ToArray();
        }
        if (Has(Mesh.ArrayFormat.FormatTexUV)) arrays[(int)Mesh.ArrayType.TexUV] = source.Uv;
        if (Has(Mesh.ArrayFormat.FormatTexUV2)) arrays[(int)Mesh.ArrayType.TexUV2] = source.Uv;
        if (Has(Mesh.ArrayFormat.FormatIndex))
        {
            arrays[(int)Mesh.ArrayType.Index] = layout.Primitive switch
            {
                BakedPrimitive.Triangles => source.Indices,
                BakedPrimitive.Lines => new[] { 0, 1, 1, 2, 2, 3, 3, 0 },
                BakedPrimitive.Points or BakedPrimitive.LineStrip => new[] { 0, 1, 2, 3 },
                BakedPrimitive.TriangleStrip => new[] { 0, 1, 3, 2 },
                _ => throw new ArgumentOutOfRangeException(nameof(layout))
            };
        }
        Custom(Mesh.ArrayFormat.FormatCustom0, Mesh.ArrayType.Custom0, Mesh.ArrayFormat.FormatCustom0Shift, source.Custom0);
        Custom(Mesh.ArrayFormat.FormatCustom1, Mesh.ArrayType.Custom1, Mesh.ArrayFormat.FormatCustom1Shift, source.Custom1);
        Custom(Mesh.ArrayFormat.FormatCustom2, Mesh.ArrayType.Custom2, Mesh.ArrayFormat.FormatCustom2Shift, source.Custom2);
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(primitive, arrays, flags: (Mesh.ArrayFormat)layout.Format);
        try
        {
            // SurfaceGetFormat's resource-level binding omits the high version bit in the locked runtime.
            // The calibrated Variant call preserves all cached bits without GPU readback or normalization.
            var actual = MeshSurfaceFormat.Read(mesh);
            if (actual != layout.Format)
                throw new InvalidOperationException($"Warm sample native format {actual} does not match requested runtime format {layout.Format} " +
                    $"(resource format {(ulong)mesh.SurfaceGetFormat(0)}).");
        }
        catch
        {
            mesh.Dispose();
            throw;
        }
        return mesh;

        void Custom(Mesh.ArrayFormat bit, Mesh.ArrayType slot, Mesh.ArrayFormat shift, float[] values)
        {
            if (!Has(bit)) return;
            var format = (layout.Format >> (int)shift) & (ulong)Mesh.ArrayFormat.FormatCustomMask;
            if (format != (ulong)Mesh.ArrayCustomFormat.RgbaFloat)
                throw new NotSupportedException("Only requested RGBAFloat custom layouts have a warm fixture.");
            arrays[(int)slot] = values;
        }
    }

    private void ClearSamples()
    {
        _clearing = true;
        if (_samples3D is not null || _samples2D is not null)
        {
            var spatialCount = _samples3D?.GetChildCount() ?? 0;
            var canvasCount = _samples2D?.GetChildCount() ?? 0;
            var frame = Engine.GetProcessFrames();
            _retiring.Add((_samples3D, _samples2D, spatialCount, canvasCount, _batch?.Revision ?? -1, frame));
            GD.Print($"[WP-01][phase=warm-samples] event=queued-free revision={_batch?.Revision ?? -1} " +
                $"spatial={spatialCount} canvas={canvasCount} frame={frame}");
        }
        if (_samples3D is { } spatial) { spatial.GetParent()?.RemoveChild(spatial); spatial.QueueFree(); }
        if (_samples2D is { } canvas) { canvas.GetParent()?.RemoveChild(canvas); canvas.QueueFree(); }
        _samples3D = null;
        _samples2D = null;
        foreach (var resource in _owned) resource.Dispose();
        _owned.Clear();
        _batch = null;
        _clearing = false;
    }
    private void ReportReleasedSamples()
    {
        for (var i = _retiring.Count - 1; i >= 0; i--)
        {
            var retired = _retiring[i];
            if (retired.Spatial is { } spatial && GodotObject.IsInstanceValid(spatial) ||
                retired.Canvas is { } canvas && GodotObject.IsInstanceValid(canvas)) continue;
            _retiring.RemoveAt(i);
            GD.Print($"[WP-01][phase=warm-samples] event=nodes-freed-postdraw revision={retired.Revision} " +
                $"spatial={retired.SpatialCount} canvas={retired.CanvasCount} queuedFrame={retired.Frame} " +
                $"frame={Engine.GetProcessFrames()} veil={_veil?.Opacity} retiring={_retiring.Count}");
        }
    }
    private void Fail(Exception error)
    {
        Error = error.GetBaseException().Message;
        IsReady = false;
        _active = false;
        ClearSamples();
        GD.PushError("[WP-01][phase=material-warmup] " + Error);
    }
    private void RequireCover()
    {
        if (!Covered)
            throw new InvalidOperationException("Material warming requires an existing, fully opaque viewport cover.");
        if (_camera is null || !_camera.IsInsideTree() || GetViewport().GetCamera3D() != _camera ||
            _camera.Projection != Camera3D.ProjectionType.Perspective || (_camera.CullMask & 1u) == 0 ||
            _camera.Near >= 2f || _camera.Far <= 2f)
            throw new InvalidOperationException("Material warming requires the active perspective camera to draw its layer-1 sample array at 2 m.");
    }
    private bool Covered => _loading!.IsCovered || _veil!.Opacity >= 1f && _presentation?.IsCovered == true;
    private bool ResetForViewportChange()
    {
        var size = GetViewport().GetVisibleRect().Size;
        if (_viewportSize == size) return false;
        // Rebuild the actual array for the new frustum, after the resized viewport has drawn its cover.
        _viewportSize = size;
        IsReady = false;
        _awaitCover = true;
        ClearSamples();
        return true;
    }
    public string DescribeReadiness() =>
        $"active={_active},ready={IsReady},batch={_batch?.Revision},batchMaterials={_batch?.Materials.Count ?? 0}," +
        $"lastCapturedRevision={_lastCapturedRevision},lastCapturedMaterials={_lastCapturedMaterials}," +
        $"drawn={_drawn},stable={_stable}/{StableFrames},fixture={_fixtureTask?.Status}," +
        $"covered={Covered},loadingCovered={_loading?.IsCovered},veil={_veil?.Opacity},retiringBatches={_retiring.Count},error={Error ?? "none"}";
    public override void _ExitTree()
    {
        if (_materials is not null) _materials.WarmChanged -= OnWarmChanged;
        _pending = null;
        _pendingDirty = true;
        RenderingServer.Singleton.FramePostDraw -= OnFrameDrawn;
        End(reset: true);
    }
}
