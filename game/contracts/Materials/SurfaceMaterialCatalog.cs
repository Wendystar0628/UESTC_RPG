using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Campus.Contracts;

/// <summary>
/// Frozen per-map identities for authored surface patterns. The caller supplies map input bytes;
/// direction and phase continue to belong to continuous metre UVs, not to this catalogue.
/// </summary>
public sealed class SurfaceMaterialCatalog
{
    public const string SourcePath = "materials.json";
    private static readonly ConditionalWeakTable<IReadOnlyDictionary<string, ReadOnlyMemory<byte>>, SurfaceMaterialCatalog> Cache = new();
    private readonly Dictionary<string, SurfacePatternSpec> _patterns;

    private SurfaceMaterialCatalog(Dictionary<string, SurfacePatternSpec> patterns) => _patterns = patterns;

    /// <summary>Parse once per caller-frozen input object; unloading that snapshot releases its cached catalogue.</summary>
    public static SurfaceMaterialCatalog FromInputs(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        return Cache.GetValue(inputs, Read);
    }

    /// <summary>Unknown identities fail explicitly; they never select an unrelated default recipe.</summary>
    public SurfacePatternSpec Get(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return _patterns.TryGetValue(id, out var pattern) ? pattern :
            throw new KeyNotFoundException($"No surface material '{id}' in {SourcePath}.");
    }

    private static SurfaceMaterialCatalog Read(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> inputs)
    {
        if (!inputs.TryGetValue(SourcePath, out var bytes))
            throw new KeyNotFoundException($"Missing preloaded build input: {SourcePath}");
        var document = JsonSerializer.Deserialize<MaterialDocument>(bytes.Span, ContractJson.Options)
            ?? throw new InvalidDataException($"{SourcePath} must contain a material catalogue.");
        if (document.Materials is null) throw new InvalidDataException($"{SourcePath} requires materials.");
        var patterns = new Dictionary<string, SurfacePatternSpec>(StringComparer.Ordinal);
        foreach (var material in document.Materials)
        {
            if (material is null || string.IsNullOrWhiteSpace(material.Id) || material.Id != material.Id.Trim())
                throw new InvalidDataException($"Surface material identities in {SourcePath} must be non-empty and trimmed.");
            // Reuse the sole CPU shape validator; registered shader layers remain the resource consumer's concern.
            new ToonSpec(default, default) { SurfacePattern = material.SurfacePattern }.ValidateSurfacePattern();
            if (!patterns.TryAdd(material.Id, material.SurfacePattern))
                throw new InvalidDataException($"Duplicate surface material '{material.Id}' in {SourcePath}.");
        }
        return new(patterns);
    }

    private sealed class MaterialDocument
    {
        public required MaterialEntry[] Materials { get; init; }
    }

    private sealed class MaterialEntry
    {
        public required string Id { get; init; }
        public required SurfacePatternSpec SurfacePattern { get; init; }
    }
}
