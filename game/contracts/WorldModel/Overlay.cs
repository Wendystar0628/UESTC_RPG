using System.Text.Json;
using System.Text.Json.Serialization;

namespace Campus.Contracts;

public sealed record OverlayFile(ZoneId Zone, WpId Owner, IReadOnlyList<OverlayOp> Ops)
{
    public MapId? Map { get; init; }
}
public enum OverlayLevel { Minor, Design }

[JsonPolymorphic(TypeDiscriminatorPropertyName = "op")]
[JsonDerivedType(typeof(ReshapeOp), "reshape")]
[JsonDerivedType(typeof(HeightOp), "height")]
[JsonDerivedType(typeof(StyleOp), "style")]
[JsonDerivedType(typeof(HideOp), "hide")]
[JsonDerivedType(typeof(HidePartOp), "hidePart")]
[JsonDerivedType(typeof(AddOp), "add")]
public abstract record OverlayOp;
public sealed record ReshapeOp(FeatureId Target, PolygonRings Rings, OverlayLevel Level, string Reason) : OverlayOp;
public sealed record HeightOp(FeatureId Target, float? Height, int? Storeys, string Reason) : OverlayOp;
public sealed record StyleOp(FeatureId Target, BuildingFamily Family, FacadePreset? Facade) : OverlayOp;
public sealed record HideOp(FeatureId Target, string Reason) : OverlayOp;
/// <summary>Removes one exact source part from an existing entity; at least one part must remain. Target does not accept aliases.</summary>
public sealed record HidePartOp(FeatureId Target, string SourceId, string Reason) : OverlayOp;
public sealed record AddOp(FeatureId Id, DesignKind Kind, OverlayGeometry Geometry,
    float? Height, JsonElement? Props, string Reason) : OverlayOp;
