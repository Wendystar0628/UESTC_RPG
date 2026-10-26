using Campus.Kernel.Boot;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Bake;
using Campus.Contracts.Quality;
using Campus.Contracts.Vegetation;
using Campus.Kernel.Instances;
using Campus.Kernel.Renderer;
using Campus.Kernel.Stream;
using Campus.Systems.Vegetation;
using Campus.Style.Materials;
using GdUnit4;
using static GdUnit4.Assertions;
using Godot;
using N = System.Numerics;

namespace Campus.Tests;

[TestSuite]
public sealed class GeometryInstancePoolRuntimeTests
{
    // 本组建的着色器与材质要等包装终结才归还原生引用；整组跑完在这里收齐，单跑本组时才不漏到引擎退出（DroppedNatives）。
    [After]
    public void ReleaseDroppedNatives() => DroppedNatives.Release();

    private static readonly StringName _shaderInstanceFadeT0 = InstanceShaderParameters.FadeT0;
    private static readonly StringName _shaderInstancePrecipD = InstanceShaderParameters.PrecipD;
    private static readonly StringName _shaderInstanceShadowProxy = InstanceShaderParameters.ShadowProxy;
    private static readonly StringName _shaderInstanceVegLevel = InstanceShaderParameters.VegLevel;

    [TestCase]
    [TestCategory(TestReason.Contract)]
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
        AssertThat(fixture.Host.GetChildCount() == 0 &&
            fixture.Host.GetStatistics().Count == 0).OverrideFailureMessage("Rejected surface pattern allocated/tracked a partial instance pool.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task FrozenMultiSurfaceGeometryRetainsNativeChannelsSlotsAndInstanceBytes()
    {
        using var fixture = Fixture.Create();
        var map = new MapId("pool-fixture-shared");
        fixture.Host.OnMapLoaded(Context(map));
        var first = Surface(fixture.Provider.FirstSpec);
        var second = Surface(fixture.Provider.SecondSpec) with { Shader = ShaderId.VegTrunk };
        var frozenFirst = GeometryBuffer.Freeze(first);
        var frozenSecond = GeometryBuffer.Freeze(second);
        var prototype = Prototype(frozenFirst, frozenSecond);
        first.Position[0] = 999;
        first.ColorIndices![0] = 254;
        first.Custom0![0] = -999;
        first.Ao![0] = 1;

        var pool = fixture.Host.Mount(Spec("channels", prototype) with { Map = map });
        AssertThat(!pool.IsReady).OverrideFailureMessage("A pool without its complete buffer must not be ready.").IsTrue();
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
        AssertThat(pool.IsReady && node.Visible && multi.VisibleInstanceCount == 3).OverrideFailureMessage("A complete buffer and visible prefix did not appear immediately.").IsTrue();
        var meshIdentity = mesh.GetInstanceId();
        var materialIdentity = ownedShaderMaterial.GetInstanceId();
        System.Array.Fill(scratch, float.NaN);
        await fixture.Parent.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        AssertThat(multi.GetBuffer().SequenceEqual(submitted)).OverrideFailureMessage("Reusing caller scratch changed the already uploaded native buffer.").IsTrue();

        AssertThat(mesh.GetInstanceId() == meshIdentity && ownedShaderMaterial.GetInstanceId() == materialIdentity && fixture.Source.Created.Count == 1).OverrideFailureMessage("An instance-buffer update rebuilt the prototype or material.").IsTrue();
        pool.VisibleCount = 2;
        var statistics = fixture.Host.GetStatistics().Single();
        AssertThat(statistics.SurfaceCount == 2 && statistics.MeshTriangles == 4 && statistics.VisibleTriangles == 8 &&
            statistics.InstanceBufferBytes == pool.Capacity * 64L).OverrideFailureMessage("Multi-surface allocation statistics are wrong.").IsTrue();
        // Separate decoded prototype objects with the same complete Id share the native mesh.
        fixture.Provider.ThrowAtGet = 1;
        var shared = fixture.Host.Mount(Spec("shared-ready", Prototype(frozenFirst, frozenSecond)) with
        {
            Map = map, Capacity = 2, Layers = 2, Shadows = GeometryInstance3D.ShadowCastingSetting.ShadowsOnly
        });
        var sharedNode = FindPool(fixture.Host, "shared-ready");
        var sharedMulti = sharedNode.Multimesh;
        AssertThat(sharedMulti.Mesh.GetInstanceId() == meshIdentity && sharedMulti.Mesh.GetRid() == mesh.GetRid() &&
            sharedMulti.GetInstanceId() != multi.GetInstanceId()).OverrideFailureMessage("Mapped pools did not share one native mesh with independent MultiMeshes.").IsTrue();
        AssertThat(!shared.IsReady).OverrideFailureMessage("Ready shared materials must not bypass the complete buffer requirement.").IsTrue();
        shared.WriteBuffer(new float[shared.Capacity * InstanceStreamLayout.FloatsPerInstance]);
        shared.VisibleCount = 1;
        var sharedBounds = new Aabb(Vector3.Zero, Vector3.One);
        shared.SetAabb(sharedBounds);
        shared.SetParameter(_shaderInstanceShadowProxy, 1f);
        AssertThat(shared.IsReady && sharedNode.Visible && sharedMulti.InstanceCount == shared.Capacity &&
            multi.InstanceCount == pool.Capacity && multi.VisibleInstanceCount == pool.VisibleCount &&
            sharedMulti.VisibleInstanceCount == shared.VisibleCount && multi.GetBuffer().SequenceEqual(submitted) &&
            sharedMulti.GetBuffer().All(value => value == 0)).OverrideFailureMessage("Sharing the mesh leaked instance capacity, visibility or buffer state.").IsTrue();
        AssertThat(sharedNode.CustomAabb == sharedBounds && sharedMulti.CustomAabb == sharedBounds &&
            node.CustomAabb != sharedBounds && multi.CustomAabb != sharedBounds && node.Layers != sharedNode.Layers &&
            node.CastShadow != sharedNode.CastShadow && sharedNode.GetInstanceShaderParameter(_shaderInstanceShadowProxy).AsSingle() == 1f &&
            node.GetInstanceShaderParameter(_shaderInstanceShadowProxy).AsSingle() == 0f).OverrideFailureMessage("Sharing the mesh leaked bounds, layers, shadow policy or instance uniforms.").IsTrue();
        pool.Free();
        pool.Free();
        AssertThat(GodotObject.IsInstanceValid(mesh) && !GodotObject.IsInstanceValid(multi)).OverrideFailureMessage("Freeing one borrower disposed the shared mesh or kept its own MultiMesh.").IsTrue();
        shared.WriteBuffer(InstanceData(shared.Capacity));
        shared.Free();
        shared.Free();
        AssertThat(!GodotObject.IsInstanceValid(mesh) && !GodotObject.IsInstanceValid(sharedMulti) &&
            GodotObject.IsInstanceValid(fixture.Provider.FirstMaterial)).OverrideFailureMessage("The last borrower retained its native mesh or disposed a provider material.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public void MapCleanupIsIdempotentAndNeverDisposesBorrowedMaterials()
    {
        using var fixture = Fixture.Create();
        var prototype = Prototype(GeometryBuffer.Freeze(Surface(fixture.Provider.FirstSpec)),
            GeometryBuffer.Freeze(Surface(fixture.Provider.SecondSpec) with { Shader = ShaderId.VegTrunk }));
        var mapA = new MapId("pool-fixture-a");
        fixture.Host.OnMapLoaded(Context(mapA));
        var mapped = fixture.Host.Mount(Spec("mapped", prototype) with { Map = mapA });
        var mappedPeer = fixture.Host.Mount(Spec("mapped-peer", prototype) with { Map = mapA });
        var global = fixture.Host.Mount(Spec("global", prototype));
        var mappedNode = FindPool(fixture.Host, "mapped");
        var mappedMulti = mappedNode.Multimesh;
        var mappedMesh = mappedMulti.Mesh;
        var mappedMeshIdentity = mappedMesh.GetInstanceId();
        var globalMesh = FindPool(fixture.Host, "global").Multimesh.Mesh;
        AssertThat(globalMesh.GetInstanceId() != mappedMeshIdentity).OverrideFailureMessage("A global pool borrowed a map-owned mesh.").IsTrue();
        AssertThat(fixture.Source.Created.Count == 1).OverrideFailureMessage("The same ShaderId was created again for another live pool.").IsTrue();
        AssertThat(mappedNode.GetMeta("map_id").AsString() == mapA.Value).OverrideFailureMessage("The native pool lost map attribution.").IsTrue();
        fixture.Host.OnMapUnloading(mapA);
        fixture.Host.OnMapUnloading(mapA);
        AssertThat(GodotObject.IsInstanceValid(mappedNode) && GodotObject.IsInstanceValid(mappedMesh)).OverrideFailureMessage("Stopping the host stole the consumer-owned pools.").IsTrue();
        mapped.Free();
        mappedPeer.Free();
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

        global.Free();
        fixture.Host.Free();
        global.Free();
        AssertThat(!GodotObject.IsInstanceValid(globalMesh)).OverrideFailureMessage("Host exit retained the global pool mesh.").IsTrue();
        AssertThat(!GodotObject.IsInstanceValid(fixture.Source.Created.Single())).OverrideFailureMessage("Host exit did not dispose its cached ShaderMaterial.").IsTrue();
        AssertThat(GodotObject.IsInstanceValid(fixture.Provider.FirstMaterial) && GodotObject.IsInstanceValid(fixture.Provider.SecondMaterial)).OverrideFailureMessage("Host exit disposed provider-owned Toon materials.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public void CatalogPoolsUploadBothBanksWithoutReinterpretingTheirFrozenVersion()
    {
        var bank0 = new PaletteRegistry([
            new(new("fixture.first.lit"), new(64), new N.Vector3(.5f), "fixture", false, PaletteRole.Lit),
            new(new("fixture.first.shade"), new(65), new N.Vector3(.5f), "fixture", false, PaletteRole.Shade),
            new(new("fixture.edge.lit"), new(254), new N.Vector3(.5f), "fixture", false, PaletteRole.Lit),
            new(new("fixture.edge.shade"), new(255), new N.Vector3(.5f), "fixture", false, PaletteRole.Shade)]);
        var catalog = new PaletteCatalog(bank0, new PaletteEntry[] {
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
    [TestCategory(TestReason.Contract)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public void UnsupportedGeometryAndFailedMaterialResolutionNeverPublishPartialPools()
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
        AssertThat(fixture.Host.GetChildCount() == 0 && fixture.Host.GetStatistics().Count == 0).OverrideFailureMessage("Unsupported input allocated or published a partial pool.").IsTrue();

        var map = new MapId("pool-fixture-failed-upload");
        fixture.Host.OnMapLoaded(Context(map));
        fixture.Provider.ThrowAtGet = 2;
        var two = Prototype(GeometryBuffer.Freeze(basic), GeometryBuffer.Freeze(Surface(fixture.Provider.SecondSpec)));
        {
            var thrown = false;
            try { fixture.Host.Mount(Spec("failed-material", two) with { Map = map }); }
            catch (InvalidOperationException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(InvalidOperationException).Name}.").IsTrue();
        }
        AssertThat(fixture.Host.GetChildCount() == 0 && fixture.Host.GetStatistics().Count == 0).OverrideFailureMessage("A later material resolution failure published a partial pool.").IsTrue();
        AssertThat(GodotObject.IsInstanceValid(fixture.Provider.FirstMaterial) && GodotObject.IsInstanceValid(fixture.Provider.SecondMaterial)).OverrideFailureMessage("Rollback disposed a borrowed material.").IsTrue();
        fixture.Provider.ThrowAtGet = -1;
        var retry = fixture.Host.Mount(Spec("failed-material", two) with { Map = map });
        AssertThat(FindPool(fixture.Host, "failed-material").Multimesh.Mesh.GetSurfaceCount() == two.Surfaces.Count).OverrideFailureMessage("A failed first upload left a partial shared mesh for retry.").IsTrue();
        retry.Free();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task ShadowProxyIsAValidatedScalarOnTheInstanceOnly()
    {
        using var fixture = Fixture.Create();
        var prototype = Prototype(GeometryBuffer.Freeze(Surface(fixture.Provider.FirstSpec)));
        var proxy = fixture.Host.Mount(Spec("proxy", prototype) with { Shadows = GeometryInstance3D.ShadowCastingSetting.ShadowsOnly });
        var body = fixture.Host.Mount(Spec("body", prototype));
        var proxyNode = FindPool(fixture.Host, "proxy");
        var bodyNode = FindPool(fixture.Host, "body");
        // Godot populates instance shader defaults when it updates dirty render instances.
        await fixture.Parent.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        AssertThat(proxyNode.Multimesh.Mesh.GetInstanceId() != bodyNode.Multimesh.Mesh.GetInstanceId()).OverrideFailureMessage("Pools without Map must keep exclusive native meshes even for the same prototype.").IsTrue();
        AssertThat(proxyNode.CastShadow == GeometryInstance3D.ShadowCastingSetting.ShadowsOnly &&
            bodyNode.CastShadow == GeometryInstance3D.ShadowCastingSetting.Off).OverrideFailureMessage("Explicit body/proxy shadow policy changed.").IsTrue();
        proxy.SetParameter(_shaderInstanceShadowProxy, 1f);
        var proxyShadow = proxyNode.GetInstanceShaderParameter(_shaderInstanceShadowProxy);
        var bodyShadow = bodyNode.GetInstanceShaderParameter(_shaderInstanceShadowProxy);
        AssertThat(proxyShadow.VariantType == Variant.Type.Float && proxyShadow.AsSingle() == 1f).OverrideFailureMessage("The scalar instance parameter did not reach the native proxy.").IsTrue();
        AssertThat(bodyShadow.VariantType == Variant.Type.Float && bodyShadow.AsSingle() == 0f).OverrideFailureMessage("Changing a proxy contaminated a body sharing its material.").IsTrue();
        body.SetParameter(_shaderInstanceVegLevel, (float)Campus.Contracts.Vegetation.VegPool.L0);
        var bodyLevel = bodyNode.GetInstanceShaderParameter(_shaderInstanceVegLevel);
        var proxyLevel = proxyNode.GetInstanceShaderParameter(_shaderInstanceVegLevel);
        AssertThat(bodyLevel.VariantType == Variant.Type.Float && bodyLevel.AsSingle() == (float)Campus.Contracts.Vegetation.VegPool.L0 &&
            proxyLevel.VariantType == Variant.Type.Float && proxyLevel.AsSingle() == (float)Campus.Contracts.Vegetation.VegPool.L1).OverrideFailureMessage("A pool level must reach its instance without replacing another pool's L1 shader default.").IsTrue();
        {
            var thrown = false;
            try { proxy.SetParameter(_shaderInstanceShadowProxy, float.NaN); }
            catch (ArgumentException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentException).Name}.").IsTrue();
        }
        {
            var thrown = false;
            try { proxy.SetParameter(_shaderInstanceShadowProxy, float.PositiveInfinity); }
            catch (ArgumentException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentException).Name}.").IsTrue();
        }
        {
            var thrown = false;
            try { proxy.SetParameter(_shaderInstanceFadeT0, 1f); }
            catch (ArgumentException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentException).Name}.").IsTrue();
        }
        {
            var thrown = false;
            try { proxy.SetParameter(_shaderInstancePrecipD, 1f); }
            catch (ArgumentException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentException).Name}.").IsTrue();
        }
        {
            var thrown = false;
            try { proxy.SetParameter(_shaderInstanceShadowProxy, new Godot.Vector4(1, 0, 0, 0)); }
            catch (ArgumentException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ArgumentException).Name}.").IsTrue();
        }
        proxy.Free();
        {
            var thrown = false;
            try { proxy.SetParameter(_shaderInstanceShadowProxy, 0f); }
            catch (ObjectDisposedException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage($"Expected {typeof(ObjectDisposedException).Name}.").IsTrue();
        }
        body.Free();
    }

    [TestCase, TestCategory(TestReason.Regression), TestCategory("NativeRender"), RequireGodotRuntime]
    public void VegetationRefreshDuringPartialMountStillUploadsTheActualTrees()
    {
        using var fixture = Fixture.Create();
        using var transitions = new VegetationTransitionMap();
        var stream = new StreamDirector();
        var runtime = new VegetationRuntime();
        IStreamMountWork? work = null;
        try
        {
            var map = new MapId("vegetation-mount-fixture");
            fixture.Host.OnMapLoaded(Context(map));
            var world = new TestWorldState(new TestWeatherProcess(), new TestRuleset());
            world.Freeze(17.5, Season.Autumn, WorldTime.InitialAnimTime); world.CompleteRestore();
            var distance = (VegetationBuckets.L0PlusDistance + VegetationBuckets.L0Distance) * QualityRules.DetailDistanceScale * .5f;
            var foot = new N.Vector3(2, 0, 3);
            var eye = foot + new N.Vector3(0, VegetationBuckets.ReferenceTreeHeight * .5f, distance);
            // Bind the isolated owner's existing services without registering a second application or entering the scene tree.
            foreach (var (name, value) in new (string, object)[]
            {
                ("_host", fixture.Host), ("_streamFrame", stream), ("_world", world),
                ("_rig", new TestCameraRig(pose: new Pose(eye, 0, 0))), ("_map", map), ("_transitionMap", transitions)
            }) typeof(VegetationRuntime).GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(runtime, value);
            var records = new float[InstanceStreamLayout.FloatsPerInstance];
            InstanceStreamLayout.Write(records, N.Matrix4x4.CreateTranslation(foot), new(0, 0, 0, VegetationBuckets.ReferenceTreeHeight));
            var prototype = Prototype(GeometryBuffer.Freeze(Surface(fixture.Provider.FirstSpec)));
            var prototypes = new[] { prototype, prototype, prototype, prototype };
            var key = new BakeKey(map, BakeModuleIds.VegetationTrees, "L0/0/0");
            var content = new VegetationTileContent(key, default, [new("fixture", records, prototypes, [new(foot, foot + N.Vector3.One)])]);
            work = runtime.CreateMount(key, content, BakePriority.Normal, 1);
            AssertThat(VegetationBuckets.Target(distance, 1f, VegetationBuckets.NotNear) == (byte)VegPool.L0 &&
                !work.Step(1) && fixture.Host.GetChildCount() > 0 && fixture.Host.GetChildCount() < prototypes.Length && !runtime.IsReady(key))
                .OverrideFailureMessage("The fixture must refresh a partial tile before its tree's target pool exists.").IsTrue();
            runtime._Process(.1);
            while (!work.Step(1)) { }
            runtime._Process(.1);
            if (runtime.Failure is { } failure) throw failure;
            var visible = fixture.Host.GetChildren().OfType<MultiMeshInstance3D>().Where(node => node.Multimesh.VisibleInstanceCount > 0).ToArray();
            AssertThat(runtime.IsReady(key) && visible.Sum(node => node.Multimesh.VisibleInstanceCount) == content.Mothers.Sum(mother => mother.Bounds.Count) &&
                visible.All(node => node.Multimesh.GetBuffer().SequenceEqual(records)))
                .OverrideFailureMessage("The visible native prefixes must contain the actual tree records after a partial-mount refresh.").IsTrue();
        }
        finally
        {
            if (work is not null) while (!work.ReleaseStep(0)) { }
            runtime.Free(); stream.Free();
        }
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
    private static GeometryInstancePoolSpec Spec(string id, GeometryPrototype prototype) => new("fixture." + id, prototype,
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
            AssertThat(MathF.Abs(normals[i].X) < TestTolerance.FloatRelative * 2 && MathF.Abs(normals[i].Y) < TestTolerance.FloatRelative * 2 && MathF.Abs(normals[i].Z - 1f) < TestTolerance.FloatRelative * 2).OverrideFailureMessage("Normals were omitted or altered beyond the native normal packing tolerance.").IsTrue();
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
            if (child is MultiMeshInstance3D node && node.GetMeta("instance_pool_id").AsString() == "fixture." + id) return node;
        throw new InvalidOperationException("Missing native pool: " + id);
    }
    private static IMapContext Context(MapId id) => new TestMapContext(new MapDescriptor(
        Id: id, Name: default!, Kind: MapKind.Outdoor, Version: 1, Seed: 1, Geo: default!, Center: default,
        Bounds: default, DataRect: default!, Roam: default!, TileMin: default, TileMax: default,
        DefaultSpawn: "fixture", Spawns: [], World: default!, Env: default!,
        Portals: []));

    private static PaletteCatalog Catalog() => new(new PaletteRegistry(
        Enumerable.Range(0, 256).Select(i => new PaletteEntry(new("fixture.color." + i), new(i),
            new N.Vector3(.5f), "fixture", false, i < 32 ? PaletteRole.Frame : i < 40 ? PaletteRole.Retired :
                i < 64 ? PaletteRole.Weather : (i & 1) == 0 ? PaletteRole.Lit : PaletteRole.Shade))
            .Select(entry => entry.Index.Value switch
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
        public static Fixture Create(PaletteCatalog? catalog = null, IToonMaterialProvider? materials = null)
        {
            var fixture = new Fixture();
            try
            {
                var tree = Engine.GetMainLoop() as SceneTree ?? throw new InvalidOperationException("Godot scene tree required.");
                fixture._first = new ShaderMaterialSource(new RequiredResources()).Create(ShaderId.VegTrunk);
                fixture._second = new ShaderMaterialSource(new RequiredResources()).Create(ShaderId.VegTrunk);
                fixture.Provider = new(fixture._first, fixture._second);
                fixture._parent = new Node3D();
                tree.Root.AddChild(fixture._parent);
                fixture._host = new InstancePoolHost(materials ?? fixture.Provider, fixture.Source, new Palettes(catalog ?? Catalog()));
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

    private sealed class Palettes(PaletteCatalog catalog) : IPaletteCatalogSource
    {
        public PaletteCatalog Catalog { get; } = catalog;
    }

    private sealed class RecordingSource : IShaderMaterialSource
    {
        public List<ShaderMaterial> Created { get; } = [];
        public ShaderMaterial Create(ShaderId shader)
        {
            var material = new ShaderMaterialSource(new RequiredResources()).Create(ShaderId.VegTrunk); // The real whitelist factory owns shader selection.
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
        private int _gets;
        private int _throwAtGet = -1;
        public int ThrowAtGet
        {
            get => _throwAtGet;
            set { _throwAtGet = value; _gets = 0; }
        }
        public Material Get(ToonSpec spec)
        {
            if (++_gets == ThrowAtGet) throw new InvalidOperationException("Injected second-surface material failure.");
            return spec == FirstSpec ? first : spec == SecondSpec ? second : throw new ArgumentException("Unknown test spec.");
        }
        public Material CreatePipelineMaterial(ToonKind kind) => throw new NotSupportedException();
    }

}
