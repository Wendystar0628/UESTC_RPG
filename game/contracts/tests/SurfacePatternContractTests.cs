using Campus.Tests;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts.Bake;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class SurfacePatternContractTests
{
    // Synthetic transport values, not dimensions selected for any real campus surface.
    private static readonly SurfacePatternSpec Pattern = new(37, new(2f, .75f), new(.012f, .006f));
    private static readonly ToonSpec Plain = new(new("fixture.lit"), new("fixture.shade"));
    private static readonly Box3 Bounds = new(Vector3.Zero, Vector3.One);

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ContentRoundTripAndFreezeRetainProfileMetresAndDiscreteFaces()
    {
        var source = Surface();
        var text = JsonSerializer.Serialize(new ContentOutput([source], []), ContractJson.Options);
        var restored = JsonSerializer.Deserialize<ContentOutput>(text, ContractJson.Options)!.Meshes[0];
        restored.ValidateVertexData();
        var frozen = GeometryBuffer.Freeze(restored);
        AssertThat(frozen.Material.Spec.SurfacePattern == Pattern &&
            frozen.Uv!.Value.Span.SequenceEqual(source.Uv!) && frozen.Uv2!.Value.Span.SequenceEqual(source.Uv2!)).OverrideFailureMessage("Content wire/Freeze lost layer, physical dimensions, directed metre coordinates or per-face K.").IsTrue();
        Array.Fill(restored.Uv!, 99f);
        Array.Fill(restored.Uv2!, 99f);
        AssertThat(frozen.Uv!.Value.Span[0] == -128.125f && frozen.Uv2!.Value.Span[0] == 1f &&
            frozen.Uv2.Value.Span[7] == 4f && frozen.Material.Spec.SurfacePattern!.Value.JointMeters.Y == .006f).OverrideFailureMessage("Frozen pattern payload still aliases mutable generation storage.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void NullProfilePreservesExistingUv2AndOptionalChannels()
    {
        var legacy = Surface() with { Material = new(Plain), Uv = null, Uv2 = [65537, -1, 2, 9, 1, 8, 4, 7, 1, 6, 1, 5] };
        legacy.ValidateVertexData();
        var json = JsonSerializer.Serialize(Plain, ContractJson.Options);
        AssertThat(!json.Contains("surfacePattern", StringComparison.Ordinal)).OverrideFailureMessage("Null profile changed legacy JSON shape.").IsTrue();
        AssertThat(JsonSerializer.Deserialize<ToonSpec>(json, ContractJson.Options)!.SurfacePattern is null).OverrideFailureMessage("Old material input must not invent a pattern.").IsTrue();
        var frozen = GeometryBuffer.Freeze(legacy);
        AssertThat(frozen.Uv is null && frozen.Uv2!.Value.Span.SequenceEqual(legacy.Uv2!)).OverrideFailureMessage("Absent profile must not reinterpret existing window/water-like UV2 values.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ProfileDimensionsAreCheckedByGeometryAndPaletteCatalog()
    {
        var registry = new PaletteRegistry([
            new(Plain.Lit, new(64), Vector3.One, "fixture", false, PaletteRole.Lit),
            new(Plain.Shade, new(65), Vector3.Zero, "fixture", false, PaletteRole.Shade)]);
        var catalog = new PaletteCatalog(registry, []);
        catalog.Validate(Plain with { SurfacePattern = Pattern });
        SurfacePatternSpec[] invalid = [default, Pattern with { PeriodMeters = new(0, 1) },
            Pattern with { PeriodMeters = new(1, -1) }, Pattern with { PeriodMeters = new(float.NaN, 1) },
            Pattern with { PeriodMeters = new(1, float.PositiveInfinity) },
            Pattern with { JointMeters = new(-.1f, 0) }, Pattern with { JointMeters = new(0, float.NaN) },
            Pattern with { JointMeters = new(2, 0) }, Pattern with { JointMeters = new(0, 1) }];
        foreach (var pattern in invalid)
        {
            var spec = Plain with { SurfacePattern = pattern };
            AssertThrown(() => (Surface() with { Material = new(spec) }).ValidateVertexData()).IsInstanceOf<ArgumentException>();
            AssertThrown(() => catalog.Validate(spec)).IsInstanceOf<ArgumentException>();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ProfileCannotReinterpretSpecialKindsShadersVariantsOrSemanticFlags()
    {
        foreach (var kind in Enum.GetValues<ToonKind>().Where(value => value != ToonKind.Static))
            AssertThrown(() => (Surface() with { Material = new(Plain with { Kind = kind, SurfacePattern = Pattern }) }).ValidateVertexData()).IsInstanceOf<ArgumentException>();
        GeometryBundle[] invalid = [Surface() with { Shader = ShaderId.Water },
            Surface() with { Variant = new VariantTag() },
            Surface() with { Flags = [1, 1, 1, 0, 0, 0] },
            Surface() with { Flags = [4, 4, 4, 0, 0, 0] },
            Surface() with { Custom0 = new float[24] }, Surface() with { Custom1 = new float[24] },
            Surface() with { Custom2 = new float[24] }];
        foreach (var source in invalid) AssertThrown(() => GeometryBuffer.Freeze(source)).IsInstanceOf<ArgumentException>();
        // Existing outskirts/no-shadow flags and Reserved bytes are not repurposed.
        (Surface() with { Flags = [2, 2, 2, 8, 8, 8], Reserved = [3, 3, 3, 7, 7, 7] }).ValidateVertexData();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void MissingNonFiniteAndFractionalGateCoordinatesAreRejected()
    {
        GeometryBundle[] missing = [Surface() with { Uv = null }, Surface() with { Uv2 = null },
            Surface() with { Uv = new float[10] }, Surface() with { Uv2 = new float[10] }];
        foreach (var source in missing) AssertThrown(() => source.ValidateVertexData()).IsInstanceOf<ArgumentException>();
        foreach (var (component, value) in new[] { (0, .5f), (0, -1f), (0, float.NaN), (1, 0f),
            (1, 4f), (1, 5f), (1, 6f), (1, 2.5f), (1, float.PositiveInfinity) })
        {
            var source = Surface();
            for (var vertex = 0; vertex < 3; vertex++) source.Uv2![vertex * 2 + component] = value;
            AssertThrown(() => source.ValidateVertexData()).IsInstanceOf<ArgumentException>();
        }
        var nonFinite = Surface();
        nonFinite.Uv![0] = float.NegativeInfinity;
        AssertThrown(() => nonFinite.ValidateVertexData()).IsInstanceOf<ArgumentException>();
        var off = Surface() with { Uv2 = new float[12] };
        off.ValidateVertexData(); // Explicit non-target faces; no missing-data default is generated here.
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void BaseAndLodTrianglesCannotBridgeGateOrFidelityBoundaries()
    {
        var source = Surface();
        source.Uv2![2] = 0;
        AssertThrown(() => source.ValidateVertexData()).IsInstanceOf<ArgumentException>();
        AssertThrown(() => (Surface() with { Lods = [new(1f, [0, 1, 3])] }).ValidateVertexData()).IsInstanceOf<ArgumentException>();
        var differentK = Surface() with { Uv2 = [1, 1, 1, 2, 1, 1, 0, 4, 0, 4, 0, 4] };
        AssertThrown(() => differentK.ValidateVertexData()).IsInstanceOf<ArgumentException>();
        var validLod = Surface() with { Lods = [new(1f, [0, 1, 2])] };
        GeometryBuffer.Freeze(validLod);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void BakedStaticSurfaceRetainsAndValidatesThePatternProfile()
    {
        _ = new BakedSurface(new(Plain), 0, BakedPrimitive.Triangles, 0, 0, default, default, default, Bounds);
        var baked = new BakedSurface(new(Plain with { SurfacePattern = Pattern }),
            0, BakedPrimitive.Triangles, 0, 0, default, default, default, Bounds);
        AssertThat(baked.Material.Spec.SurfacePattern == Pattern).OverrideFailureMessage("Baked material lost the static profile.").IsTrue();
        AssertThrown(() => new BakedSurface(new(Plain with { SurfacePattern = default(SurfacePatternSpec) }),
            0, BakedPrimitive.Triangles, 0, 0, default, default, default, Bounds)).IsInstanceOf<ArgumentException>();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void StaticInstanceStreamRefusesPatternsBeforeApplyingTransforms()
    {
        var prototype = new GeometryPrototype("pattern-fixture", [GeometryBuffer.Freeze(Surface())], Bounds);
        var data = new float[16];
        InstanceStreamLayout.Write(data, Matrix4x4.Identity, Vector4.Zero);
        AssertThrown(() => new GeometryInstanceStream("pattern-stream", prototype, data, Bounds)).IsInstanceOf<NotSupportedException>();
    }

    private static GeometryBundle Surface() => new(new(Plain with { SurfacePattern = Pattern }),
        [0, 0, 0, 1, 0, 0, 0, 0, 1, 1, 0, 0, 1, 0, 1, 0, 0, 1],
        [0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0], [0, 1, 2, 3, 4, 5],
        Uv: [-128.125f, 64.25f, -127.125f, 64.25f, -128.125f, 65.25f, 20, 8, 21, 8, 20, 9],
        Uv2: [1, 2, 1, 2, 1, 2, 0, 4, 0, 4, 0, 4]);
}
