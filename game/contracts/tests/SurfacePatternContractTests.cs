using System.Numerics;
using System.Text.Json;
using Campus.Contracts.Bake;
using GdUnit4;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class SurfacePatternContractTests
{
    // Synthetic transport values, not dimensions selected for any real campus surface.
    private static readonly SurfacePatternSpec Pattern = new(37, new(2f, .75f), new(.012f, .006f));
    private static readonly ToonSpec Plain = new(new("fixture.lit"), new("fixture.shade"));
    private static readonly Box3 Bounds = new(Vector3.Zero, Vector3.One);

    [TestCase]
    public void ContentRoundTripAndFreezeRetainProfileMetresAndDiscreteFaces()
    {
        var source = Surface();
        var text = JsonSerializer.Serialize(new ContentOutput([source], []), ContractJson.Options);
        var restored = JsonSerializer.Deserialize<ContentOutput>(text, ContractJson.Options)!.Meshes[0];
        restored.ValidateVertexData();
        var frozen = GeometryBuffer.Freeze(restored);
        Require(frozen.Material.Spec.SurfacePattern == Pattern &&
            frozen.Uv!.Value.Span.SequenceEqual(source.Uv!) && frozen.Uv2!.Value.Span.SequenceEqual(source.Uv2!),
            "Content wire/Freeze lost layer, physical dimensions, directed metre coordinates or per-face K.");
        Array.Fill(restored.Uv!, 99f);
        Array.Fill(restored.Uv2!, 99f);
        Require(frozen.Uv!.Value.Span[0] == -128.125f && frozen.Uv2!.Value.Span[0] == 1f &&
            frozen.Uv2.Value.Span[7] == 4f && frozen.Material.Spec.SurfacePattern!.Value.JointMeters.Y == .006f,
            "Frozen pattern payload still aliases mutable generation storage.");
    }

    [TestCase]
    public void NullProfilePreservesExistingUv2AndOptionalChannels()
    {
        var legacy = Surface() with { Material = new(Plain), Uv = null, Uv2 = [65537, -1, 2, 9, 1, 8, 4, 7, 1, 6, 1, 5] };
        legacy.ValidateVertexData();
        var json = JsonSerializer.Serialize(Plain, ContractJson.Options);
        Require(!json.Contains("surfacePattern", StringComparison.Ordinal), "Null profile changed legacy JSON shape.");
        Require(JsonSerializer.Deserialize<ToonSpec>(json, ContractJson.Options)!.SurfacePattern is null,
            "Old material input must not invent a pattern.");
        var frozen = GeometryBuffer.Freeze(legacy);
        Require(frozen.Uv is null && frozen.Uv2!.Value.Span.SequenceEqual(legacy.Uv2!),
            "Absent profile must not reinterpret existing window/water-like UV2 values.");
    }

    [TestCase]
    public void ProfileDimensionsAreCheckedByGeometryAndBothPaletteCatalogs()
    {
        var registry = new PaletteRegistry([
            new(Plain.Lit, 64, Vector3.One, "fixture", false, PaletteRole.Lit),
            new(Plain.Shade, 65, Vector3.Zero, "fixture", false, PaletteRole.Shade)]);
        var catalog = PaletteCatalog.FromLegacy(registry);
        registry.Validate(Plain with { SurfacePattern = Pattern });
        catalog.Validate(Plain with { SurfacePattern = Pattern });
        SurfacePatternSpec[] invalid = [default, Pattern with { PeriodMeters = new(0, 1) },
            Pattern with { PeriodMeters = new(1, -1) }, Pattern with { PeriodMeters = new(float.NaN, 1) },
            Pattern with { PeriodMeters = new(1, float.PositiveInfinity) },
            Pattern with { JointMeters = new(-.1f, 0) }, Pattern with { JointMeters = new(0, float.NaN) },
            Pattern with { JointMeters = new(2, 0) }, Pattern with { JointMeters = new(0, 1) }];
        foreach (var pattern in invalid)
        {
            var spec = Plain with { SurfacePattern = pattern };
            Throws<ArgumentException>(() => (Surface() with { Material = new(spec) }).ValidateVertexData());
            Throws<ArgumentException>(() => registry.Validate(spec));
            Throws<ArgumentException>(() => catalog.Validate(spec));
        }
    }

    [TestCase]
    public void ProfileCannotReinterpretSpecialKindsShadersVariantsOrSemanticFlags()
    {
        foreach (var kind in Enum.GetValues<ToonKind>().Where(value => value != ToonKind.Static))
            Throws<ArgumentException>(() => (Surface() with { Material = new(Plain with { Kind = kind, SurfacePattern = Pattern }) }).ValidateVertexData());
        GeometryBundle[] invalid = [Surface() with { Shader = ShaderId.Water },
            Surface() with { Variant = new VariantTag() },
            Surface() with { Flags = [1, 1, 1, 0, 0, 0] },
            Surface() with { Flags = [4, 4, 4, 0, 0, 0] },
            Surface() with { Custom0 = new float[24] }, Surface() with { Custom1 = new float[24] },
            Surface() with { Custom2 = new float[24] }];
        foreach (var source in invalid) Throws<ArgumentException>(() => GeometryBuffer.Freeze(source));
        // Existing outskirts/no-shadow flags and Reserved bytes are not repurposed.
        (Surface() with { Flags = [2, 2, 2, 8, 8, 8], Reserved = [3, 3, 3, 7, 7, 7] }).ValidateVertexData();
    }

    [TestCase]
    public void MissingNonFiniteAndFractionalGateCoordinatesAreRejected()
    {
        GeometryBundle[] missing = [Surface() with { Uv = null }, Surface() with { Uv2 = null },
            Surface() with { Uv = new float[10] }, Surface() with { Uv2 = new float[10] }];
        foreach (var source in missing) Throws<ArgumentException>(() => source.ValidateVertexData());
        foreach (var (component, value) in new[] { (0, .5f), (0, -1f), (0, float.NaN), (1, 0f),
            (1, 4f), (1, 5f), (1, 6f), (1, 2.5f), (1, float.PositiveInfinity) })
        {
            var source = Surface();
            for (var vertex = 0; vertex < 3; vertex++) source.Uv2![vertex * 2 + component] = value;
            Throws<ArgumentException>(() => source.ValidateVertexData());
        }
        var nonFinite = Surface();
        nonFinite.Uv![0] = float.NegativeInfinity;
        Throws<ArgumentException>(() => nonFinite.ValidateVertexData());
        var off = Surface() with { Uv2 = new float[12] };
        off.ValidateVertexData(); // Explicit non-target faces; no missing-data default is generated here.
    }

    [TestCase]
    public void BaseAndLodTrianglesCannotBridgeGateOrFidelityBoundaries()
    {
        var source = Surface();
        source.Uv2![2] = 0;
        Throws<ArgumentException>(() => source.ValidateVertexData());
        Throws<ArgumentException>(() => (Surface() with { Lods = [new(1f, [0, 1, 3])] }).ValidateVertexData());
        var differentK = Surface() with { Uv2 = [1, 1, 1, 2, 1, 1, 0, 4, 0, 4, 0, 4] };
        Throws<ArgumentException>(() => differentK.ValidateVertexData());
        var validLod = Surface() with { Lods = [new(1f, [0, 1, 2])] };
        GeometryBuffer.Freeze(validLod);
    }

    [TestCase]
    public void PendingBakeCodecRejectsPatternsInsteadOfDroppingTheProfile()
    {
        _ = new BakedSurface(new(Plain), 0, BakedPrimitive.Triangles, 0, 0, default, default, default, Bounds);
        Throws<NotSupportedException>(() => new BakedSurface(new(Plain with { SurfacePattern = Pattern }),
            0, BakedPrimitive.Triangles, 0, 0, default, default, default, Bounds));
    }

    [TestCase]
    public void StaticInstanceStreamRefusesPatternsBeforeApplyingTransforms()
    {
        var prototype = new GeometryPrototype("pattern-fixture", [GeometryBuffer.Freeze(Surface())], Bounds);
        var data = new float[16];
        InstanceStreamLayout.Write(data, Matrix4x4.Identity, Vector4.Zero);
        Throws<NotSupportedException>(() => new GeometryInstanceStream("pattern-stream", prototype, data, Bounds));
    }

    private static GeometryBundle Surface() => new(new(Plain with { SurfacePattern = Pattern }),
        [0, 0, 0, 1, 0, 0, 0, 0, 1, 1, 0, 0, 1, 0, 1, 0, 0, 1],
        [0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0], [0, 1, 2, 3, 4, 5],
        Uv: [-128.125f, 64.25f, -127.125f, 64.25f, -128.125f, 65.25f, 20, 8, 21, 8, 20, 9],
        Uv2: [1, 2, 1, 2, 1, 2, 0, 4, 0, 4, 0, 4]);

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
