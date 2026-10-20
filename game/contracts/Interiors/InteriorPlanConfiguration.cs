using System.Numerics;
using System.Text.Json;

namespace Campus.Contracts;

public enum SealKind : byte { IronGate, FireDoor, GlassPartition, FoldingGate, RollerShutter, ShelfWall }
/// <summary>Direct catalog-owned closure; never an independently loaded overlay operation.</summary>
public sealed record SealOp(FeatureId Building, int Storey, Vector2 A, Vector2 B, SealKind Kind, string Reason);

/// <summary>One catalog-owned range of a real world part. Floors are spatially scoped, never entity-wide permission.</summary>
public sealed record InteriorOpenPart(FeatureId FeatureId, string SourceId, IReadOnlyList<int> Floors);

/// <summary>One detached catalog configuration. BuildingType is supplied only to a generated-plan provider.</summary>
public sealed record InteriorCatalogConfiguration(IReadOnlyList<FeatureId> Features, IReadOnlyList<InteriorOpenPart> OpenParts,
    IReadOnlyDictionary<FeatureId, IReadOnlyList<EntranceDecl>> Entrances, JsonElement? BuildingType, IReadOnlyList<SealOp> Seals)
{
    public IReadOnlyList<int> OpenFloors { get; } = Array.AsReadOnly(OpenParts.SelectMany(part => part.Floors).Distinct().Order().ToArray());
    public IReadOnlyList<int> FloorsFor(FeatureId feature) => Array.AsReadOnly(OpenParts.Where(part => part.FeatureId == feature)
        .SelectMany(part => part.Floors).Distinct().Order().ToArray());
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class InteriorProviderFactoryAttribute(string prefix) : RegistrationAttribute(RegistrationKind.Interior, prefix, RegistrationLifetime.Map) { }
/// <summary>The registry owns every produced instance in the same map scope as this stateless factory.</summary>
public interface IInteriorProviderFactory
{
    IInteriorProvider Create(string providerId);
}
