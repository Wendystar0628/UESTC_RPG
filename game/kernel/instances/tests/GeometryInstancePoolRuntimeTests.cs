using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Kernel.Instances;
using Campus.Kernel.Materials;
using Campus.Kernel.Renderer;
using Campus.Style.Materials;
using GdUnit4;
using static GdUnit4.Assertions;
using Godot;
using N = System.Numerics;

namespace Campus.Tests;

[TestSuite]
public sealed class GeometryInstancePoolRuntimeTests
{
    [TestCase]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public void SurfacePatternPrototypeIsRefusedBeforeAllocatingAnInstancePool()
    {
        using var fixture = Fixture.Create();
        var surface = new GeometryBundle(new(fixture.Provider.FirstSpec with
        {
            SurfacePattern = new(7, new(2, 1), new(.01f, .01f))
        }), [0, 0, 0, 1, 0, 0, 0, 1, 0], [0, 0, 1, 0, 0, 1, 0, 0, 1], [0, 1, 2],
            Uv: [0, 0, 1, 0, 0, 1], Uv2: [1, 2, 1, 2, 1, 2]);
        var prototype = Prototype(GeometryBuffer.Freeze(surface));
        {
            var thrown = false;
            try { fixture.Host.Mount(Spec("unsupported-pattern", prototype)); }
            catch (NotSupportedException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(NotSupportedException).Name}.").IsTrue();
        }
        AssertThat(fixture.Host.GetChildCount() == 0 && fixture.Provider.Layouts.Count == 0 &&
            fixture.Host.GetStatistics().Count == 0).OverrideFailureMessage("Rejected surface pattern allocated/tracked a partial instance pool.").IsTrue();
    }

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
        first.ColorIndices![0] = 254;
        first.Custom0![0] = -999;
        first.Ao![0] = 1;

        var pool = fixture.Host.Mount(Spec("channels", prototype));
        var node = fixture.Host.GetChild<MultiMeshInstance3D>(0);
        var multi = node.Multimesh;
        var mesh = (ArrayMesh)multi.Mesh;
        AssertThat(mesh.GetSurfaceCount() == 2 && node.MaterialOverride is null).OverrideFailureMessage("The prototype did not retain two independent material slots.").IsTrue();
        AssertThat(ReferenceEquals(mesh.SurfaceGetMaterial(0), fixture.Provider.FirstMaterial)).OverrideFailureMessage("The Toon slot was replaced.").IsTrue();
        var ownedShaderMaterial = fixture.Source.Created.Single();
        AssertThat(ReferenceEquals(mesh.SurfaceGetMaterial(1), ownedShaderMaterial)).OverrideFailureMessage("The ShaderId slot was replaced.").IsTrue();
        AssertThat(!ReferenceEquals(mesh.SurfaceGetMaterial(0), mesh.SurfaceGetMaterial(1))).OverrideFailureMessage("Materials leaked across surface slots.").IsTrue();
        AssertThat(multi.UseCustomData && !multi.UseColors && multi.TransformFormat == MultiMesh.TransformFormatEnum.Transform3D).OverrideFailureMessage("The instance stream must have the single approved transform/custom format.").IsTrue();

        CheckSurface(mesh, 0, frozenFirst);
        CheckSurface(mesh, 1, frozenSecond);
        var formats = new[] { MeshSurfaceFormat.Read(mesh, 0), MeshSurfaceFormat.Read(mesh, 1) };
        var version = (ulong)RenderingServer.ArrayFormat.FlagFormatCurrentVersion;
        AssertThat(formats.All(format => (format & version) == version)).OverrideFailureMessage("The helper discarded native format-version bits.").IsTrue();
        AssertThat(fixture.Provider.Layouts.Count == 2).OverrideFailureMessage("Each real surface must register one actual layout.").IsTrue();
        for (var i = 0; i < 2; i++)
        {
            var tracked = fixture.Provider.Layouts[i];
            AssertThat(tracked.Layout == new WarmDrawLayout(formats[i], Instanced: true)).OverrideFailureMessage("Prewarm received a guessed or non-instanced layout.").IsTrue();
            AssertThat(ReferenceEquals(tracked.Material, mesh.SurfaceGetMaterial(i))).OverrideFailureMessage("Prewarm material and surface slot disagree.").IsTrue();
        }

        // Exactly one complete native-surface dictionary read in this fixture, for calibration/LOD assertions only.
        using (var native = RenderingServer.MeshGetSurface(mesh.GetRid(), 0))
        {
            AssertThat(native["format"].AsUInt64() == formats[0]).OverrideFailureMessage("Cached 64-bit metadata differs from native surface format.").IsTrue();
            AssertThat(native.ContainsKey("lods")).OverrideFailureMessage("The original LOD index buffer was dropped.").IsTrue();
            using var lods = native["lods"].AsGodotArray();
            AssertThat(lods.Count == 1).OverrideFailureMessage("The original LOD surface count changed.").IsTrue();
            AssertThat(native["vertex_count"].AsInt32() == 4 && native["index_count"].AsInt32() == 6).OverrideFailureMessage("The native base surface must retain four vertices and two triangles.").IsTrue();
            // The locked Windows runtime stores <= 65536-vertex surface indices as little-endian uint16.
            AssertThat(native["index_data"].AsByteArray().SequenceEqual(new byte[] { 0, 0, 1, 0, 2, 0, 0, 0, 2, 0, 3, 0 })).OverrideFailureMessage("The complete native base index bytes changed.").IsTrue();
            using var lod = lods[0].AsGodotDictionary();
            AssertThat(lod["edge_length"].AsSingle() == 2f).OverrideFailureMessage("The native LOD error changed.").IsTrue();
            AssertThat(lod["index_data"].AsByteArray().SequenceEqual(new byte[] { 0, 0, 2, 0, 3, 0 })).OverrideFailureMessage("The native LOD must retain exactly triangle (0, 2, 3), including its fourth-vertex reference.").IsTrue();
        }

        {
            var thrown = false;
            try { pool.VisibleCount = 1; }
            catch (InvalidOperationException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(InvalidOperationException).Name}.").IsTrue();
        }
        var scratch = InstanceData(pool.Capacity);
        var submitted = (float[])scratch.Clone();
        pool.WriteBuffer(scratch);
        pool.VisibleCount = 3;
        AssertThat(!node.Visible && multi.VisibleInstanceCount == 3).OverrideFailureMessage("A requested prefix escaped the pending-prewarm visibility gate.").IsTrue();
        var meshIdentity = mesh.GetInstanceId();
        var materialIdentity = ownedShaderMaterial.GetInstanceId();
        System.Array.Fill(scratch, float.NaN);
        await fixture.Parent.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        AssertThat(multi.GetBuffer().SequenceEqual(submitted)).OverrideFailureMessage("Reusing caller scratch changed the already uploaded native buffer.").IsTrue();

        fixture.Provider.Ready = true; // Simulates the provider's issued evidence; this test does not certify a warm draw.
        fixture.Host._Process(0);
        AssertThat(node.Visible && fixture.Host.GetStatistics().Single().MaterialReady).OverrideFailureMessage("Completed provider readiness did not reveal the pool.").IsTrue();
        pool.WriteBuffer(submitted);
        pool.VisibleCount = 2;
        AssertThat(mesh.GetInstanceId() == meshIdentity && ownedShaderMaterial.GetInstanceId() == materialIdentity && fixture.Source.Created.Count == 1).OverrideFailureMessage("An instance-buffer update rebuilt the prototype or material.").IsTrue();
        var statistics = fixture.Host.GetStatistics().Single();
        AssertThat(statistics.SurfaceCount == 2 && statistics.MeshTriangles == 4 && statistics.VisibleTriangles == 8 &&
            statistics.InstanceBufferBytes == pool.Capacity * 64L).OverrideFailureMessage("Multi-surface allocation statistics are wrong.").IsTrue();
        fixture.Provider.Ready = false;
        fixture.Host._Process(0);
        AssertThat(!node.Visible && fixture.Host.GetStatistics().Single().ReadyVisibleTriangles == 0).OverrideFailureMessage("Invalidated readiness left a pool visible.").IsTrue();
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
        AssertThat(fixture.Source.Created.Count == 1).OverrideFailureMessage("The same ShaderId was created again for another live pool.").IsTrue();
        AssertThat(mappedNode.GetMeta("map_id").AsString() == mapA.Value).OverrideFailureMessage("The native pool lost map attribution.").IsTrue();
        fixture.Host.OnMapUnloading(mapA);
        fixture.Host.OnMapUnloading(mapA);
        mapped.Free();
        AssertThat(!GodotObject.IsInstanceValid(mappedNode) && !GodotObject.IsInstanceValid(mappedMulti) &&
            !GodotObject.IsInstanceValid(mappedMesh)).OverrideFailureMessage("Map cleanup retained owned native resources.").IsTrue();
        AssertThat(fixture.Host.GetStatistics().Count == 1 && fixture.Host.GetStatistics()[0].Map is null).OverrideFailureMessage("Map cleanup freed a global pool or retained the mapped pool.").IsTrue();
        AssertThat(GodotObject.IsInstanceValid(fixture.Provider.FirstMaterial) && GodotObject.IsInstanceValid(fixture.Source.Created.Single())).OverrideFailureMessage("Map cleanup disposed a borrowed or still-shared cached material.").IsTrue();
        {
            var thrown = false;
            try { mapped.WriteBuffer(InstanceData(mapped.Capacity)); }
            catch (ObjectDisposedException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ObjectDisposedException).Name}.").IsTrue();
        }
        {
            var thrown = false;
            try { fixture.Host.Mount(Spec("late", prototype) with { Map = mapA }); }
            catch (InvalidOperationException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(InvalidOperationException).Name}.").IsTrue();
        }

        fixture.Host.OnMapLoaded(Context(mapB));
        var reloaded = fixture.Host.Mount(Spec("mapped", prototype) with { Map = mapB });
        AssertThat(fixture.Host.GetStatistics().Single(stat => stat.Id == "mapped").Map == mapB).OverrideFailureMessage("Reload retained old map attribution.").IsTrue();
        reloaded.Free();
        reloaded.Free();
        fixture.Host.Free();
        global.Free();
        AssertThat(!GodotObject.IsInstanceValid(fixture.Source.Created.Single())).OverrideFailureMessage("Host exit did not dispose its cached ShaderMaterial.").IsTrue();
        AssertThat(GodotObject.IsInstanceValid(fixture.Provider.FirstMaterial) && GodotObject.IsInstanceValid(fixture.Provider.SecondMaterial)).OverrideFailureMessage("Host exit disposed provider-owned Toon materials.").IsTrue();
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
            ColorIndices = [255, 256, 510, 511],
            Flags = [4, 5, 6, 15],
            Reserved = null,
            DecalBaseColorIndices = [511, 255, 256, 511]
        });
        var pool = fixture.Host.Mount(Spec("catalog", Prototype(frozen)));
        var mesh = (ArrayMesh)FindPool(fixture.Host, "catalog").Multimesh.Mesh;
        using var arrays = mesh.SurfaceGetArrays(0);
        var colors = arrays[(int)Mesh.ArrayType.Color].AsColorArray();
        for (var i = 0; i < 4; i++)
        {
            var flags = (byte)Byte(colors[i].B);
            AssertThat(PaletteEncoding.DecodeColor((byte)Byte(colors[i].R), flags).Value == frozen.ColorIndices!.Value.Span[i] &&
                PaletteEncoding.DecodeDecalBase((byte)Byte(colors[i].A), flags).Value == frozen.DecalBaseColorIndices!.Value.Span[i] &&
                (flags & 15) == frozen.Flags!.Value.Span[i]).OverrideFailureMessage("A geometry pool lost bank bits or changed semantic flags.").IsTrue();
        }
        pool.Free();
    }

    [TestCase]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public void UnsupportedGeometryAndFailedPreparationNeverPublishPartialPools()
    {
        using var fixture = Fixture.Create();
        var basic = Surface(fixture.Provider.FirstSpec);
        var encoding2 = GeometryBuffer.Freeze(basic with { ColorIndices = [300, 301, 302, 303] });
        {
            var thrown = false;
            try { fixture.Host.Mount(Spec("unregistered", Prototype(encoding2))); }
            catch (KeyNotFoundException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(KeyNotFoundException).Name}.").IsTrue();
        }
        {
            var thrown = false;
            try { fixture.Host.Mount(Spec("reflect", Prototype(GeometryBuffer.Freeze(basic with { Reflect = true })))); }
            catch (NotSupportedException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(NotSupportedException).Name}.").IsTrue();
        }
        {
            var thrown = false;
            try { fixture.Host.Mount(Spec("variant", Prototype(GeometryBuffer.Freeze(basic with { Variant = new VariantTag() })))); }
            catch (NotSupportedException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(NotSupportedException).Name}.").IsTrue();
        }
        var normal = Prototype(GeometryBuffer.Freeze(basic));
        {
            var thrown = false;
            try { fixture.Host.Mount(Spec("capacity", normal) with { Capacity = int.MaxValue }); }
            catch (ArgumentOutOfRangeException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentOutOfRangeException).Name}.").IsTrue();
        }
        {
            var thrown = false;
            try { fixture.Host.Mount(Spec("shadows", normal) with { Shadows = (GeometryInstance3D.ShadowCastingSetting)99 }); }
            catch (ArgumentOutOfRangeException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentOutOfRangeException).Name}.").IsTrue();
        }
        foreach (var invalidLod in new MeshLod[] { new(0f, [0, 2, 3]), new(2f, []), new(2f, [0, 1, 2, 0, 2, 3]) })
        {
            var invalidPrototype = Prototype(GeometryBuffer.Freeze(basic with { Lods = [invalidLod] }));
            {
                var thrown = false;
                try { fixture.Host.Mount(Spec("skipped-lod", invalidPrototype)); }
                catch (ArgumentException) { thrown = true; }
                AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentException).Name}.").IsTrue();
            }
        }
        AssertThat(fixture.Host.GetChildCount() == 0 && fixture.Host.GetStatistics().Count == 0 && fixture.Provider.Layouts.Count == 0).OverrideFailureMessage("Unsupported input allocated or published a partial pool.").IsTrue();

        fixture.Provider.ThrowAtLayout = 2;
        var two = Prototype(GeometryBuffer.Freeze(basic), GeometryBuffer.Freeze(Surface(fixture.Provider.SecondSpec)));
        {
            var thrown = false;
            try { fixture.Host.Mount(Spec("failed-layout", two)); }
            catch (InvalidOperationException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(InvalidOperationException).Name}.").IsTrue();
        }
        AssertThat(fixture.Host.GetChildCount() == 0 && fixture.Host.GetStatistics().Count == 0).OverrideFailureMessage("A later surface preparation failure published a partial pool.").IsTrue();
        AssertThat(GodotObject.IsInstanceValid(fixture.Provider.FirstMaterial) && GodotObject.IsInstanceValid(fixture.Provider.SecondMaterial)).OverrideFailureMessage("Rollback disposed a borrowed material.").IsTrue();
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
        AssertThat(proxyNode.CastShadow == GeometryInstance3D.ShadowCastingSetting.ShadowsOnly &&
            bodyNode.CastShadow == GeometryInstance3D.ShadowCastingSetting.Off).OverrideFailureMessage("Explicit body/proxy shadow policy changed.").IsTrue();
        proxy.SetParameter(InstanceShaderNames.ShadowProxy, 1f);
        AssertThat(proxyNode.GetInstanceShaderParameter(InstanceShaderNames.ShadowProxy).AsSingle() == 1f).OverrideFailureMessage("The scalar instance parameter did not reach the native proxy.").IsTrue();
        AssertThat(bodyNode.GetInstanceShaderParameter(InstanceShaderNames.ShadowProxy).AsSingle() == 0f).OverrideFailureMessage("Changing a proxy contaminated a body sharing its material.").IsTrue();
        {
            var thrown = false;
            try { proxy.SetParameter(InstanceShaderNames.ShadowProxy, float.NaN); }
            catch (ArgumentException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentException).Name}.").IsTrue();
        }
        {
            var thrown = false;
            try { proxy.SetParameter(InstanceShaderNames.ShadowProxy, float.PositiveInfinity); }
            catch (ArgumentException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentException).Name}.").IsTrue();
        }
        {
            var thrown = false;
            try { proxy.SetParameter(InstanceShaderNames.FadeT0, 1f); }
            catch (ArgumentException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentException).Name}.").IsTrue();
        }
        {
            var thrown = false;
            try { proxy.SetParameter(InstanceShaderNames.PrecipD, 1f); }
            catch (ArgumentException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentException).Name}.").IsTrue();
        }
        {
            var thrown = false;
            try { proxy.SetParameter(InstanceShaderNames.ShadowProxy, new Godot.Vector4(1, 0, 0, 0)); }
            catch (ArgumentException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentException).Name}.").IsTrue();
        }
        proxy.Free();
        {
            var thrown = false;
            try { proxy.SetParameter(InstanceShaderNames.ShadowProxy, 0f); }
            catch (ObjectDisposedException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ObjectDisposedException).Name}.").IsTrue();
        }
        body.Free();
    }

    private static GeometryBundle Surface(ToonSpec spec) => new(new(spec),
        [0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 1, 0], [0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1], [0, 1, 2, 0, 2, 3],
        Ao: [.3f, .5f, 1f, .125f], Uv: [0, 0, 1, 0, 1, 1, 0, 1], Uv2: [16777215, -1, 65537, 1048577, 4097, 3, 8388609, -4],
        Lods: [new(2f, [0, 2, 3])])
    {
        ColorIndices = [23, 73, 101, 199],
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
        AssertThat((format & required) == required && (format & (ulong)Mesh.ArrayFormat.FlagCompressAttributes) == 0).OverrideFailureMessage("A native surface dropped attributes or compressed fp32 UV2.").IsTrue();
        foreach (var shift in new[] { Mesh.ArrayFormat.FormatCustom0Shift, Mesh.ArrayFormat.FormatCustom1Shift, Mesh.ArrayFormat.FormatCustom2Shift })
            AssertThat(((format >> (int)shift) & (ulong)Mesh.ArrayFormat.FormatCustomMask) == (ulong)Mesh.ArrayCustomFormat.RgbaFloat).OverrideFailureMessage("A custom channel was not RGBA_FLOAT.").IsTrue();
        using var arrays = mesh.SurfaceGetArrays(index); // Test-only native attribute readback.
        var vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
        var normals = arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
        var uv = arrays[(int)Mesh.ArrayType.TexUV].AsVector2Array();
        var uv2 = arrays[(int)Mesh.ArrayType.TexUV2].AsVector2Array();
        var colors = arrays[(int)Mesh.ArrayType.Color].AsColorArray();
        AssertThat(arrays[(int)Mesh.ArrayType.Index].AsInt32Array().SequenceEqual(source.Index.ToArray())).OverrideFailureMessage("Triangle indices changed.").IsTrue();
        var count = source.Position.Length / 3;
        AssertThat(vertices.Length == count && normals.Length == count && uv.Length == count && uv2.Length == count && colors.Length == count).OverrideFailureMessage("The native surface lost a vertex attribute record.").IsTrue();
        for (var i = 0; i < count; i++)
        {
            AssertThat(vertices[i] == new Vector3(source.Position.Span[i * 3], source.Position.Span[i * 3 + 1], source.Position.Span[i * 3 + 2])).OverrideFailureMessage("Frozen positions changed after the producer reused its array.").IsTrue();
            AssertThat(MathF.Abs(normals[i].X) < .0002f && MathF.Abs(normals[i].Y) < .0002f && MathF.Abs(normals[i].Z - 1f) < .0002f).OverrideFailureMessage("Normals were omitted or altered beyond the native normal packing tolerance.").IsTrue();
            AssertThat(uv[i] == new Vector2(source.Uv!.Value.Span[i * 2], source.Uv.Value.Span[i * 2 + 1])).OverrideFailureMessage("UV changed.").IsTrue();
            AssertThat(uv2[i] == new Vector2(source.Uv2!.Value.Span[i * 2], source.Uv2.Value.Span[i * 2 + 1])).OverrideFailureMessage("UV2 lost fp32 integer or negative precision.").IsTrue();
            AssertThat(Byte(colors[i].R) == source.ColorIndices!.Value.Span[i] && Byte(colors[i].G) == (byte)(source.Ao!.Value.Span[i] * 255f) &&
                Byte(colors[i].B) == source.Flags!.Value.Span[i] && Byte(colors[i].A) == source.Reserved!.Value.Span[i]).OverrideFailureMessage("COLOR must retain R=id/G=truncated AO/B=flags/A=reserved without an AO inversion.").IsTrue();
        }
        AssertThat(arrays[(int)Mesh.ArrayType.Custom0].AsFloat32Array().SequenceEqual(source.Custom0!.Value.ToArray())).OverrideFailureMessage("CUSTOM0 changed.").IsTrue();
        AssertThat(arrays[(int)Mesh.ArrayType.Custom1].AsFloat32Array().SequenceEqual(source.Custom1!.Value.ToArray())).OverrideFailureMessage("CUSTOM1 changed.").IsTrue();
        AssertThat(arrays[(int)Mesh.ArrayType.Custom2].AsFloat32Array().SequenceEqual(source.Custom2!.Value.ToArray())).OverrideFailureMessage("CUSTOM2 changed.").IsTrue();
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

    private static PaletteCatalog Catalog() => new(new PaletteRegistry(
        Enumerable.Range(0, 256).Select(i => new PaletteEntry(new("fixture.color." + i), (byte)i,
            new N.Vector3(.5f), "fixture", false, i < 32 ? PaletteRole.Frame : i < 40 ? PaletteRole.Retired :
                i < 64 ? PaletteRole.Weather : (i & 1) == 0 ? PaletteRole.Lit : PaletteRole.Shade))
            .Select(entry => entry.Id switch
            {
                64 => entry with { Token = new("fixture.first.lit") },
                65 => entry with { Token = new("fixture.first.shade") },
                66 => entry with { Token = new("fixture.second.lit") },
                67 => entry with { Token = new("fixture.second.shade") },
                _ => entry
            })), []);

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
                fixture._first = new ShaderMaterialSource().Create(ShaderId.VegTrunk);
                fixture._second = new ShaderMaterialSource().Create(ShaderId.VegTrunk);
                fixture.Provider = new(fixture._first, fixture._second);
                fixture._parent = new Node3D();
                tree.Root.AddChild(fixture._parent);
                fixture._host = new InstancePoolHost(fixture.Provider, fixture.Source, catalog ?? Catalog());
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
            var material = new ShaderMaterialSource().Create(ShaderId.VegTrunk); // The real whitelist factory owns shader selection.
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

}
