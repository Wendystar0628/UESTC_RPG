using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Kernel.Instances;
using Campus.Kernel.Materials;
using Campus.Kernel.Renderer;
using Campus.Style.Stubs;
using GdUnit4;
using Godot;
using N = System.Numerics;

namespace Campus.Tests;

[TestSuite]
public sealed class GeometryInstancePoolRuntimeTests
{
    [TestCase]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task FrozenMultiSurfaceGeometryRetainsNativeChannelsSlotsAndInstanceBytes()
    {
        using var fixture = Fixture.Create();
        var first = Surface(fixture.Provider.FirstSpec);
        var second = Surface(fixture.Provider.SecondSpec) with { Shader = ShaderId.VegTrunk };
        var frozenFirst = GeometryBuffer.Freeze(first);
        var frozenSecond = GeometryBuffer.Freeze(second);
        var prototype = Prototype(frozenFirst, frozenSecond);
        first.Position[0] = 999;
        first.ColorIds![0] = 254;
        first.Custom0![0] = -999;
        first.Ao![0] = 1;

        var pool = fixture.Host.Mount(Spec("channels", prototype));
        var node = fixture.Host.GetChild<MultiMeshInstance3D>(0);
        var multi = node.Multimesh;
        var mesh = (ArrayMesh)multi.Mesh;
        Check(mesh.GetSurfaceCount() == 2 && node.MaterialOverride is null, "The prototype did not retain two independent material slots.");
        Check(ReferenceEquals(mesh.SurfaceGetMaterial(0), fixture.Provider.FirstMaterial), "The Toon slot was replaced.");
        var ownedShaderMaterial = fixture.Source.Created.Single();
        Check(ReferenceEquals(mesh.SurfaceGetMaterial(1), ownedShaderMaterial), "The ShaderId slot was replaced.");
        Check(!ReferenceEquals(mesh.SurfaceGetMaterial(0), mesh.SurfaceGetMaterial(1)), "Materials leaked across surface slots.");
        Check(multi.UseCustomData && !multi.UseColors && multi.TransformFormat == MultiMesh.TransformFormatEnum.Transform3D,
            "The instance stream must have the single approved transform/custom format.");

        CheckSurface(mesh, 0, frozenFirst);
        CheckSurface(mesh, 1, frozenSecond);
        var formats = new[] { MeshSurfaceFormat.Read(mesh, 0), MeshSurfaceFormat.Read(mesh, 1) };
        var version = (ulong)RenderingServer.ArrayFormat.FlagFormatCurrentVersion;
        Check(formats.All(format => (format & version) == version), "The helper discarded native format-version bits.");
        Check(fixture.Provider.Layouts.Count == 2, "Each real surface must register one actual layout.");
        for (var i = 0; i < 2; i++)
        {
            var tracked = fixture.Provider.Layouts[i];
            Check(tracked.Layout == new WarmDrawLayout(formats[i], Instanced: true), "Prewarm received a guessed or non-instanced layout.");
            Check(ReferenceEquals(tracked.Material, mesh.SurfaceGetMaterial(i)), "Prewarm material and surface slot disagree.");
        }

        // Exactly one complete native-surface dictionary read in this fixture, for calibration/LOD assertions only.
        using (var native = RenderingServer.MeshGetSurface(mesh.GetRid(), 0))
        {
            Check(native["format"].AsUInt64() == formats[0], "Cached 64-bit metadata differs from native surface format.");
            Check(native.ContainsKey("lods"), "The original LOD index buffer was dropped.");
            using var lods = native["lods"].AsGodotArray();
            Check(lods.Count == 1, "The original LOD surface count changed.");
            Check(native["vertex_count"].AsInt32() == 4 && native["index_count"].AsInt32() == 6,
                "The native base surface must retain four vertices and two triangles.");
            // The locked Windows runtime stores <= 65536-vertex surface indices as little-endian uint16.
            Check(native["index_data"].AsByteArray().SequenceEqual(new byte[] { 0, 0, 1, 0, 2, 0, 0, 0, 2, 0, 3, 0 }),
                "The complete native base index bytes changed.");
            using var lod = lods[0].AsGodotDictionary();
            Check(lod["edge_length"].AsSingle() == 2f, "The native LOD error changed.");
            Check(lod["index_data"].AsByteArray().SequenceEqual(new byte[] { 0, 0, 2, 0, 3, 0 }),
                "The native LOD must retain exactly triangle (0, 2, 3), including its fourth-vertex reference.");
        }

        Throws<InvalidOperationException>(() => pool.VisibleCount = 1);
        var scratch = InstanceData(pool.Capacity);
        var submitted = (float[])scratch.Clone();
        pool.WriteBuffer(scratch);
        pool.VisibleCount = 3;
        Check(!node.Visible && multi.VisibleInstanceCount == 3, "A requested prefix escaped the pending-prewarm visibility gate.");
        var meshIdentity = mesh.GetInstanceId();
        var materialIdentity = ownedShaderMaterial.GetInstanceId();
        System.Array.Fill(scratch, float.NaN);
        await fixture.Parent.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Check(multi.GetBuffer().SequenceEqual(submitted), "Reusing caller scratch changed the already uploaded native buffer.");

        fixture.Provider.Ready = true; // Simulates the provider's issued evidence; this test does not certify a warm draw.
        fixture.Host._Process(0);
        Check(node.Visible && fixture.Host.GetStatistics().Single().MaterialReady, "Completed provider readiness did not reveal the pool.");
        pool.WriteBuffer(submitted);
        pool.VisibleCount = 2;
        Check(mesh.GetInstanceId() == meshIdentity && ownedShaderMaterial.GetInstanceId() == materialIdentity && fixture.Source.Created.Count == 1,
            "An instance-buffer update rebuilt the prototype or material.");
        var statistics = fixture.Host.GetStatistics().Single();
        Check(statistics.SurfaceCount == 2 && statistics.MeshTriangles == 4 && statistics.VisibleTriangles == 8 &&
            statistics.InstanceBufferBytes == pool.Capacity * 64L, "Multi-surface allocation statistics are wrong.");
        fixture.Provider.Ready = false;
        fixture.Host._Process(0);
        Check(!node.Visible && fixture.Host.GetStatistics().Single().ReadyVisibleTriangles == 0, "Invalidated readiness left a pool visible.");
    }

    [TestCase]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public void MapCleanupIsIdempotentAndNeverDisposesBorrowedMaterials()
    {
        using var fixture = Fixture.Create();
        var prototype = Prototype(GeometryBuffer.Freeze(Surface(fixture.Provider.FirstSpec)),
            GeometryBuffer.Freeze(Surface(fixture.Provider.SecondSpec) with { Shader = ShaderId.VegTrunk }));
        var mapA = new MapId("pool-fixture-a");
        var mapB = new MapId("pool-fixture-b");
        fixture.Host.OnMapLoaded(Context(mapA));
        var mapped = fixture.Host.Mount(Spec("mapped", prototype) with { Map = mapA });
        var global = fixture.Host.Mount(Spec("global", prototype));
        var mappedNode = FindPool(fixture.Host, "mapped");
        var mappedMulti = mappedNode.Multimesh;
        var mappedMesh = mappedMulti.Mesh;
        Check(fixture.Source.Created.Count == 1, "The same ShaderId was created again for another live pool.");
        Check(mappedNode.GetMeta("map_id").AsString() == mapA.Value, "The native pool lost map attribution.");
        fixture.Host.OnMapUnloading(mapA);
        fixture.Host.OnMapUnloading(mapA);
        mapped.Free();
        Check(!GodotObject.IsInstanceValid(mappedNode) && !GodotObject.IsInstanceValid(mappedMulti) &&
            !GodotObject.IsInstanceValid(mappedMesh), "Map cleanup retained owned native resources.");
        Check(fixture.Host.GetStatistics().Count == 1 && fixture.Host.GetStatistics()[0].Map is null,
            "Map cleanup freed a global pool or retained the mapped pool.");
        Check(GodotObject.IsInstanceValid(fixture.Provider.FirstMaterial) && GodotObject.IsInstanceValid(fixture.Source.Created.Single()),
            "Map cleanup disposed a borrowed or still-shared cached material.");
        Throws<ObjectDisposedException>(() => mapped.WriteBuffer(InstanceData(mapped.Capacity)));
        Throws<InvalidOperationException>(() => fixture.Host.Mount(Spec("late", prototype) with { Map = mapA }));

        fixture.Host.OnMapLoaded(Context(mapB));
        var reloaded = fixture.Host.Mount(Spec("mapped", prototype) with { Map = mapB });
        Check(fixture.Host.GetStatistics().Single(stat => stat.Id == "mapped").Map == mapB, "Reload retained old map attribution.");
        reloaded.Free();
        reloaded.Free();
        fixture.Host.Free();
        global.Free();
        Check(!GodotObject.IsInstanceValid(fixture.Source.Created.Single()), "Host exit did not dispose its cached ShaderMaterial.");
        Check(GodotObject.IsInstanceValid(fixture.Provider.FirstMaterial) && GodotObject.IsInstanceValid(fixture.Provider.SecondMaterial),
            "Host exit disposed provider-owned Toon materials.");
    }

    [TestCase]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public void CatalogPoolsUploadBothBanksWithoutReinterpretingTheirFrozenVersion()
    {
        var legacy = new PaletteRegistry([
            new(new("fixture.first.lit"), 64, new N.Vector3(.5f), "fixture", false, PaletteRole.Lit),
            new(new("fixture.first.shade"), 65, new N.Vector3(.5f), "fixture", false, PaletteRole.Shade),
            new(new("fixture.edge.lit"), 254, new N.Vector3(.5f), "fixture", false, PaletteRole.Lit),
            new(new("fixture.edge.shade"), 255, new N.Vector3(.5f), "fixture", false, PaletteRole.Shade)]);
        var catalog = new PaletteCatalog(legacy, new PaletteEntryV2[] {
            new(new("fixture.high.lit"), new(256), new(.5f), "fixture", false, PaletteRole.Lit),
            new(new("fixture.high.shade"), new(257), new(.5f), "fixture", false, PaletteRole.Shade),
            new(new("fixture.last.lit"), new(510), new(.5f), "fixture", false, PaletteRole.Lit),
            new(new("fixture.last.shade"), new(511), new(.5f), "fixture", false, PaletteRole.Shade) });
        using var fixture = Fixture.Create(catalog);
        var frozen = GeometryBuffer.Freeze(Surface(fixture.Provider.FirstSpec) with
        {
            ColorIds = null,
            ColorIndices = [255, 256, 510, 511],
            Flags = [4, 5, 6, 15],
            Reserved = null,
            DecalBaseColorIndices = [511, 255, 256, 511]
        }, PaletteLayout.EncodingVersion);
        var pool = fixture.Host.Mount(Spec("catalog", Prototype(frozen)));
        var mesh = (ArrayMesh)FindPool(fixture.Host, "catalog").Multimesh.Mesh;
        using var arrays = mesh.SurfaceGetArrays(0);
        var colors = arrays[(int)Mesh.ArrayType.Color].AsColorArray();
        for (var i = 0; i < 4; i++)
        {
            var flags = (byte)Byte(colors[i].B);
            Check(PaletteEncoding.DecodeColor((byte)Byte(colors[i].R), flags).Value == frozen.ColorIndices!.Value.Span[i] &&
                PaletteEncoding.DecodeDecalBase((byte)Byte(colors[i].A), flags).Value == frozen.DecalBaseColorIndices!.Value.Span[i] &&
                (flags & 15) == frozen.Flags!.Value.Span[i], "A geometry pool lost bank bits or changed semantic flags.");
        }
        Check(frozen.PaletteEncodingVersion == PaletteLayout.EncodingVersion, "The host changed its prototype encoding.");
        pool.Free();
    }

    [TestCase]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public void UnsupportedGeometryAndFailedPreparationNeverPublishPartialPools()
    {
        using var fixture = Fixture.Create();
        var basic = Surface(fixture.Provider.FirstSpec);
        var encoding2 = GeometryBuffer.Freeze(basic with { ColorIds = null, ColorIndices = [300, 301, 302, 303] }, PaletteLayout.EncodingVersion);
        Throws<NotSupportedException>(() => fixture.Host.Mount(Spec("encoding2", Prototype(encoding2))));
        Throws<NotSupportedException>(() => fixture.Host.Mount(Spec("reflect", Prototype(GeometryBuffer.Freeze(basic with { Reflect = true })))));
        Throws<NotSupportedException>(() => fixture.Host.Mount(Spec("variant", Prototype(GeometryBuffer.Freeze(basic with { Variant = new VariantTag() })))));
        var normal = Prototype(GeometryBuffer.Freeze(basic));
        Throws<ArgumentOutOfRangeException>(() => fixture.Host.Mount(Spec("capacity", normal) with { Capacity = int.MaxValue }));
        Throws<ArgumentOutOfRangeException>(() => fixture.Host.Mount(Spec("shadows", normal) with { Shadows = (GeometryInstance3D.ShadowCastingSetting)99 }));
        foreach (var invalidLod in new MeshLod[] { new(0f, [0, 2, 3]), new(2f, []), new(2f, [0, 1, 2, 0, 2, 3]) })
        {
            var invalidPrototype = Prototype(GeometryBuffer.Freeze(basic with { Lods = [invalidLod] }));
            Throws<ArgumentException>(() => fixture.Host.Mount(Spec("skipped-lod", invalidPrototype)));
        }
        Check(fixture.Host.GetChildCount() == 0 && fixture.Host.GetStatistics().Count == 0 && fixture.Provider.Layouts.Count == 0,
            "Unsupported input allocated or published a partial pool.");

        fixture.Provider.ThrowAtLayout = 2;
        var two = Prototype(GeometryBuffer.Freeze(basic), GeometryBuffer.Freeze(Surface(fixture.Provider.SecondSpec)));
        Throws<InvalidOperationException>(() => fixture.Host.Mount(Spec("failed-layout", two)));
        Check(fixture.Host.GetChildCount() == 0 && fixture.Host.GetStatistics().Count == 0,
            "A later surface preparation failure published a partial pool.");
        Check(GodotObject.IsInstanceValid(fixture.Provider.FirstMaterial) && GodotObject.IsInstanceValid(fixture.Provider.SecondMaterial),
            "Rollback disposed a borrowed material.");
    }

    [TestCase]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public void ShadowProxyIsAValidatedScalarOnTheInstanceOnly()
    {
        using var fixture = Fixture.Create();
        var prototype = Prototype(GeometryBuffer.Freeze(Surface(fixture.Provider.FirstSpec)));
        var proxy = fixture.Host.Mount(Spec("proxy", prototype) with { Shadows = GeometryInstance3D.ShadowCastingSetting.ShadowsOnly });
        var body = fixture.Host.Mount(Spec("body", prototype));
        var proxyNode = FindPool(fixture.Host, "proxy");
        var bodyNode = FindPool(fixture.Host, "body");
        Check(proxyNode.CastShadow == GeometryInstance3D.ShadowCastingSetting.ShadowsOnly &&
            bodyNode.CastShadow == GeometryInstance3D.ShadowCastingSetting.Off, "Explicit body/proxy shadow policy changed.");
        proxy.SetParameter(InstanceShaderNames.ShadowProxy, 1f);
        Check(proxyNode.GetInstanceShaderParameter(InstanceShaderNames.ShadowProxy).AsSingle() == 1f,
            "The scalar instance parameter did not reach the native proxy.");
        Check(bodyNode.GetInstanceShaderParameter(InstanceShaderNames.ShadowProxy).AsSingle() == 0f,
            "Changing a proxy contaminated a body sharing its material.");
        Throws<ArgumentException>(() => proxy.SetParameter(InstanceShaderNames.ShadowProxy, float.NaN));
        Throws<ArgumentException>(() => proxy.SetParameter(InstanceShaderNames.ShadowProxy, float.PositiveInfinity));
        Throws<ArgumentException>(() => proxy.SetParameter(InstanceShaderNames.FadeT0, 1f));
        Throws<ArgumentException>(() => proxy.SetParameter(InstanceShaderNames.PrecipD, 1f));
        Throws<ArgumentException>(() => proxy.SetParameter(InstanceShaderNames.ShadowProxy, new Godot.Vector4(1, 0, 0, 0)));
        proxy.Free();
        Throws<ObjectDisposedException>(() => proxy.SetParameter(InstanceShaderNames.ShadowProxy, 0f));
        body.Free();
    }

    private static GeometryBundle Surface(ToonSpec spec) => new(new(spec),
        [0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 1, 0], [0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1], [0, 1, 2, 0, 2, 3],
        Ao: [.3f, .5f, 1f, .125f], Uv: [0, 0, 1, 0, 1, 1, 0, 1], Uv2: [16777215, -1, 65537, 1048577, 4097, 3, 8388609, -4],
        Lods: [new(2f, [0, 2, 3])])
    {
        ColorIds = [23, 73, 101, 199],
        Flags = [1, 4, 8, 0],
        Reserved = [7, 11, 15, 3],
        Custom0 = Enumerable.Range(0, 16).Select(i => 100000.125f + i * .125f).ToArray(),
        Custom1 = Enumerable.Range(0, 16).Select(i => -200f - i * .25f).ToArray(),
        Custom2 = Enumerable.Range(0, 16).Select(i => 300f + i * .5f).ToArray()
    };

    private static GeometryPrototype Prototype(params GeometryBuffer[] surfaces) => new("native-pool-prototype", surfaces,
        new Box3(new N.Vector3(0, 0, 0), new N.Vector3(1, 1, 0)));
    private static GeometryInstancePoolSpec Spec(string id, GeometryPrototype prototype) => new(id, "WP-00", prototype,
        4, new Aabb(new Vector3(-100, -100, -100), new Vector3(200, 200, 200)), 1);
    private static float[] InstanceData(int count)
    {
        var result = new float[count * 16];
        for (var i = 0; i < count; i++)
        {
            var at = N.Matrix4x4.CreateScale(2, 3, 4) * N.Matrix4x4.CreateRotationY(.4f) *
                N.Matrix4x4.CreateTranslation(-67 + i, 5, 129 - i);
            InstanceStreamLayout.Write(result.AsSpan(i * 16, 16), at, new N.Vector4(i, .25f, -.5f, 7));
        }
        return result;
    }

    private static void CheckSurface(ArrayMesh mesh, int index, GeometryBuffer source)
    {
        var format = MeshSurfaceFormat.Read(mesh, index);
        var required = (ulong)(Mesh.ArrayFormat.FormatVertex | Mesh.ArrayFormat.FormatNormal | Mesh.ArrayFormat.FormatColor |
            Mesh.ArrayFormat.FormatTexUV | Mesh.ArrayFormat.FormatTexUV2 | Mesh.ArrayFormat.FormatIndex |
            Mesh.ArrayFormat.FormatCustom0 | Mesh.ArrayFormat.FormatCustom1 | Mesh.ArrayFormat.FormatCustom2);
        Check((format & required) == required && (format & (ulong)Mesh.ArrayFormat.FlagCompressAttributes) == 0,
            "A native surface dropped attributes or compressed fp32 UV2.");
        foreach (var shift in new[] { Mesh.ArrayFormat.FormatCustom0Shift, Mesh.ArrayFormat.FormatCustom1Shift, Mesh.ArrayFormat.FormatCustom2Shift })
            Check(((format >> (int)shift) & (ulong)Mesh.ArrayFormat.FormatCustomMask) == (ulong)Mesh.ArrayCustomFormat.RgbaFloat,
                "A custom channel was not RGBA_FLOAT.");
        using var arrays = mesh.SurfaceGetArrays(index); // Test-only native attribute readback.
        var vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
        var normals = arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
        var uv = arrays[(int)Mesh.ArrayType.TexUV].AsVector2Array();
        var uv2 = arrays[(int)Mesh.ArrayType.TexUV2].AsVector2Array();
        var colors = arrays[(int)Mesh.ArrayType.Color].AsColorArray();
        Check(arrays[(int)Mesh.ArrayType.Index].AsInt32Array().SequenceEqual(source.Index.ToArray()), "Triangle indices changed.");
        var count = source.Position.Length / 3;
        Check(vertices.Length == count && normals.Length == count && uv.Length == count && uv2.Length == count && colors.Length == count,
            "The native surface lost a vertex attribute record.");
        for (var i = 0; i < count; i++)
        {
            Check(vertices[i] == new Vector3(source.Position.Span[i * 3], source.Position.Span[i * 3 + 1], source.Position.Span[i * 3 + 2]),
                "Frozen positions changed after the producer reused its array.");
            Check(MathF.Abs(normals[i].X) < .0002f && MathF.Abs(normals[i].Y) < .0002f && MathF.Abs(normals[i].Z - 1f) < .0002f,
                "Normals were omitted or altered beyond the native normal packing tolerance.");
            Check(uv[i] == new Vector2(source.Uv!.Value.Span[i * 2], source.Uv.Value.Span[i * 2 + 1]), "UV changed.");
            Check(uv2[i] == new Vector2(source.Uv2!.Value.Span[i * 2], source.Uv2.Value.Span[i * 2 + 1]), "UV2 lost fp32 integer or negative precision.");
            Check(Byte(colors[i].R) == source.ColorIds!.Value.Span[i] && Byte(colors[i].G) == (byte)(source.Ao!.Value.Span[i] * 255f) &&
                Byte(colors[i].B) == source.Flags!.Value.Span[i] && Byte(colors[i].A) == source.Reserved!.Value.Span[i],
                "COLOR must retain R=id/G=truncated AO/B=flags/A=reserved without an AO inversion.");
        }
        Check(arrays[(int)Mesh.ArrayType.Custom0].AsFloat32Array().SequenceEqual(source.Custom0!.Value.ToArray()), "CUSTOM0 changed.");
        Check(arrays[(int)Mesh.ArrayType.Custom1].AsFloat32Array().SequenceEqual(source.Custom1!.Value.ToArray()), "CUSTOM1 changed.");
        Check(arrays[(int)Mesh.ArrayType.Custom2].AsFloat32Array().SequenceEqual(source.Custom2!.Value.ToArray()), "CUSTOM2 changed.");
    }
    private static int Byte(float value) => (int)MathF.Round(value * 255f);
    private static MultiMeshInstance3D FindPool(InstancePoolHost host, string id)
    {
        foreach (var child in host.GetChildren())
            if (child is MultiMeshInstance3D node && node.GetMeta("instance_pool_id").AsString() == id) return node;
        throw new InvalidOperationException("Missing native pool: " + id);
    }
    private static IMapContext Context(MapId id) => new TestMapContext(new MapDescriptor(
        Id: id, Name: default!, Kind: MapKind.Outdoor, Version: 1, Seed: 1, Geo: default!, Center: default,
        Bounds: default, DataRect: default!, Roam: default!, TileMin: default, TileMax: default,
        DefaultSpawn: "fixture", Spawns: [], World: default!, Env: default!,
        Cinematics: new Dictionary<string, string>(), Portals: []));
    private sealed class TestMapContext(MapDescriptor descriptor) : IMapContext
    {
        public MapDescriptor Descriptor => descriptor;
        public IWorldModel World => throw new NotSupportedException();
        public string DataRoot => "";
        public string BakeRoot => "";
    }

    private sealed class Fixture : IDisposable
    {
        private ShaderMaterial? _first;
        private ShaderMaterial? _second;
        private Node3D? _parent;
        private InstancePoolHost? _host;
        public RecordingProvider Provider { get; private set; } = null!;
        public RecordingSource Source { get; } = new();
        public Node3D Parent => _parent!;
        public InstancePoolHost Host => _host!;
        public static Fixture Create(PaletteCatalog? catalog = null)
        {
            var fixture = new Fixture();
            try
            {
                var tree = Engine.GetMainLoop() as SceneTree ?? throw new InvalidOperationException("Godot scene tree required.");
                fixture._first = new D0MaterialLibrary().Material;
                fixture._second = new D0MaterialLibrary().Material;
                fixture.Provider = new(fixture._first, fixture._second);
                fixture._parent = new Node3D();
                tree.Root.AddChild(fixture._parent);
                fixture._host = new InstancePoolHost(fixture.Provider, fixture.Source, catalog);
                fixture._parent.AddChild(fixture._host);
                return fixture;
            }
            catch { fixture.Dispose(); throw; }
        }
        public void Dispose()
        {
            if (GodotObject.IsInstanceValid(_parent)) _parent!.Free();
            _first?.Dispose();
            _second?.Dispose();
        }
    }

    private sealed class RecordingSource : IShaderMaterialSource
    {
        public List<ShaderMaterial> Created { get; } = [];
        public ShaderMaterial Create(ShaderId shader)
        {
            var material = new D0MaterialLibrary().Material; // Construction stays inside the existing Style factory.
            Created.Add(material);
            return material;
        }
    }
    private sealed class RecordingProvider(ShaderMaterial first, ShaderMaterial second) : IToonMaterialProvider
    {
        public ToonSpec FirstSpec { get; } = new(new("fixture.first.lit"), new("fixture.first.shade"));
        public ToonSpec SecondSpec { get; } = new(new("fixture.second.lit"), new("fixture.second.shade"));
        public ShaderMaterial FirstMaterial => first;
        public ShaderMaterial SecondMaterial => second;
        public bool Ready { get; set; }
        public int ThrowAtLayout { get; set; } = -1;
        public List<(Material Material, WarmDrawLayout Layout)> Layouts { get; } = [];
        private readonly HashSet<Material> _registered = [];
        public Material Get(ToonSpec spec)
        {
            var material = spec == FirstSpec ? first : spec == SecondSpec ? second : throw new ArgumentException("Unknown test spec.");
            _registered.Add(material);
            return material;
        }
        public byte PaletteId(PaletteToken token) => token == FirstSpec.Lit ? (byte)23 : (byte)73;
        public bool IsReady(ToonSpec spec) => Ready;
        public bool IsReady(ShaderId shader, Material material) => Ready && _registered.Contains(material);
        public void Track(ShaderId shader, Material material) => _registered.Add(material);
        public void TrackLayout(Material material, WarmDrawLayout layout)
        {
            if (!_registered.Contains(material)) throw new InvalidOperationException("Unregistered test material.");
            if (Layouts.Count + 1 == ThrowAtLayout) throw new InvalidOperationException("Injected second-surface preparation failure.");
            Layouts.Add((material, layout));
            Ready = false;
        }
        public ToonWarmBatch CapturePendingWarm() => throw new NotSupportedException("This recording double does not certify warm evidence.");
        public bool CompleteWarm(ToonWarmBatch batch) => throw new NotSupportedException("Use the real WP-04 warm-draw integration for evidence.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
