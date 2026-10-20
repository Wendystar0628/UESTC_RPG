using System.Numerics;
using System.Text.Json;

namespace Campus.Contracts;

public enum SealKind : byte { IronGate, FireDoor, GlassPartition, FoldingGate, RollerShutter, ShelfWall }
/// <summary>Direct catalog-owned closure; never an independently loaded overlay operation.</summary>
public sealed record SealOp(FeatureId Building, int Storey, Vector2 A, Vector2 B, SealKind Kind, string Reason);

/// <summary>The access domain controls generation, medium and stream participation.</summary>
public enum InteriorOpenPartKind : byte { IndoorFloor, OutdoorHighPlace }

/// <summary>Indoor ranges reference a world part and floors; outdoor ranges reference an authored area source only.</summary>
public sealed record InteriorOpenPart(FeatureId FeatureId, string SourceId, IReadOnlyList<int> Floors, InteriorOpenPartKind Kind)
{
    public static IReadOnlyList<int> IndoorFloorsFor(IReadOnlyList<InteriorOpenPart> parts, FeatureId feature) =>
        Array.AsReadOnly(parts.Where(part => part.FeatureId == feature && IsIndoor(part.Kind))
            .SelectMany(part => part.Floors).Distinct().Order().ToArray());

    public static bool IsIndoor(InteriorOpenPartKind kind) => kind switch
    {
        InteriorOpenPartKind.IndoorFloor => true,
        InteriorOpenPartKind.OutdoorHighPlace => false,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}

/// <summary>One detached configuration of indoor features. Outdoor permission never configures a provider.</summary>
public sealed record InteriorCatalogConfiguration(IReadOnlyList<FeatureId> Features, IReadOnlyList<InteriorOpenPart> OpenParts,
    IReadOnlyDictionary<FeatureId, IReadOnlyList<EntranceDecl>> Entrances, JsonElement? BuildingType, IReadOnlyList<SealOp> Seals)
{
    public IReadOnlyList<int> FloorsFor(FeatureId feature) => InteriorOpenPart.IndoorFloorsFor(OpenParts, feature);
}

/// <summary>Build-time source geometry shared by the existing visible site, its collision and outdoor access.</summary>
public sealed record OutdoorAccessArea(string Id, FeatureId Feature, IReadOnlyList<StoreyInfo> Areas);
public interface IOutdoorAccessSource
{
    IReadOnlyList<OutdoorAccessArea> DescribeOutdoorAccess(BuildContext context);
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class InteriorProviderFactoryAttribute(string prefix) : RegistrationAttribute(RegistrationKind.Interior, prefix, RegistrationLifetime.Map) { }
/// <summary>The registry owns every produced instance in the same map scope as this stateless factory.</summary>
public interface IInteriorProviderFactory
{
    IInteriorProvider Create(string providerId);
}
