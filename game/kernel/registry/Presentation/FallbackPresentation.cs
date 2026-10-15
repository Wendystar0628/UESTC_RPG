using System.Text.Json;
using Campus.Contracts;
using Godot;

namespace Campus.Kernel.Renderer;

/// <summary>D0 RGB material only. Formal VF-A rendering must wait for the real WP-04 provider.</summary>
[KernelService(typeof(IToonMaterialProvider), Owner = "WP-00", IsStub = true)]
public sealed class FallbackToonMaterialProvider(IPaletteRegistrySource palettes) : IToonMaterialProvider
{
    private readonly ToonWarmBatch _emptyBatch = new(0, Array.Empty<ToonWarmMaterial>());
    private Campus.Style.Stubs.D0MaterialLibrary? _materials;
    private IReadOnlyDictionary<string, Color>? _d0Colors;
    private Campus.Style.Stubs.D0MaterialLibrary Materials => _materials ??= new();

    public Material Get(ToonSpec spec)
    {
        Validate(spec);
        return Materials.Material;
    }

    public byte PaletteId(PaletteToken token) => palettes.Registry.Get(token).Id;
    // A stub has no evidence that production material pipelines have been drawn.
    public bool IsReady(ToonSpec spec) => false;
    public void Track(ShaderId shader, Material material) => throw new NotSupportedException("The D0 material provider cannot prewarm non-toon materials; install the WP-04 provider.");
    public void TrackLayout(Material material, WarmDrawLayout layout) => throw new NotSupportedException("The D0 material provider cannot provide draw-layout prewarm evidence; install the WP-04 provider.");
    public bool IsReady(ShaderId shader, Material material) => false;
    public ToonWarmBatch CapturePendingWarm() => _emptyBatch;
    public bool CompleteWarm(ToonWarmBatch batch) => ReferenceEquals(batch, _emptyBatch)
        && batch.Revision == _emptyBatch.Revision && batch.Materials.Count == 0;

    /// <summary>Preserves the existing D0 RGB path without inventing registry entries or silently accepting unknown names.</summary>
    public Color VertexColor(ToonSpec spec, byte? colorId)
    {
        if (colorId is { } id)
        {
            var color = palettes.Registry.Get(id).Albedo;
            return new(color.X, color.Y, color.Z);
        }
        if (D0Token(spec) is { } token)
        {
            _d0Colors ??= ReadD0Colors();
            return _d0Colors.TryGetValue(token, out var value) ? value
                : throw new KeyNotFoundException($"D0 palette token '{token}' is not registered.");
        }
        var albedo = palettes.Registry.Get(spec.Lit).Albedo;
        return new(albedo.X, albedo.Y, albedo.Z);
    }

    private void Validate(ToonSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        if (D0Token(spec) is null) palettes.Registry.Validate(spec);
    }

    private static IReadOnlyDictionary<string, Color> ReadD0Colors()
    {
        using var document = JsonDocument.Parse(Godot.FileAccess.GetFileAsBytes("res://data/d0/palette.json"));
        return document.RootElement.EnumerateObject().ToDictionary(entry => entry.Name,
            entry => new Color(entry.Value[0].GetSingle(), entry.Value[1].GetSingle(), entry.Value[2].GetSingle()), StringComparer.Ordinal);
    }

    private static string? D0Token(ToonSpec spec) => (spec.Lit.Value, spec.Shade.Value) switch
    {
        ("d0.building", "d0.building.shade") => "d0.building",
        ("d0.ground", "d0.ground.shade") => "d0.ground",
        ("d0.road", "d0.road.shade") => "d0.road",
        ("d0.water", "d0.water.shade") => "d0.water",
        ("interior.wall.lit", "interior.wall.shade") => "d0.building",
        _ => null
    };
}

/// <summary>D0Stage already draws its fallback veil; this bridge never creates a second overlay.</summary>
[KernelService(typeof(IVeilPresentation), Owner = "WP-00", IsStub = true)]
public sealed class FallbackVeilPresentation : IVeilPresentation
{
    public bool IsCovered => false;
    public void Present(in VeilFrame frame) { }
}

/// <summary>No reflection world exists until WP-24 supplies its registered implementation.</summary>
[KernelService(typeof(IReflectionWorld), Owner = "WP-00", IsStub = true)]
public sealed class FallbackReflectionWorld : IReflectionWorld
{
    public Rid Scenario => default;
    public Material ProxyMaterial => throw new InvalidOperationException("The D0 reflection world is unavailable; check Scenario before reading ProxyMaterial.");
    public event Action? Changed { add { } remove { } }
}
