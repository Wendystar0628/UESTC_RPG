using System.Numerics;
using System.Text.Json.Serialization;

namespace Campus.Contracts;

public enum FidelityZone : byte { K1 = 1, K2 = 2, K3 = 3, K4 = 4, K5 = 5 }
public sealed record FacadeSegment(FeatureId Building, int Index, Vector2 A, Vector2 B,
    FidelityZone Zone, FidelityZone GroundBand, string? Raised = null);
public sealed record RoofZone(FeatureId Building, FidelityZone Zone, string? Raised = null);
public interface IFidelityModel
{
    IReadOnlyList<FacadeSegment> Facades(FeatureId building);
    RoofZone Roof(FeatureId building);
    FidelityZone GroundAt(float x, float z);
    float WalkDistanceAt(float x, float z);
    string InputHash { get; }
}

public sealed record FidelityOverlayFile(WpId Owner, IReadOnlyList<FidelityOverlayOp> Ops)
{
    public MapId? Map { get; init; }
}
[JsonPolymorphic(TypeDiscriminatorPropertyName = "op")]
[JsonDerivedType(typeof(FidelityK1Op), "k1")]
[JsonDerivedType(typeof(FidelityRaiseOp), "raise")]
public abstract record FidelityOverlayOp(string Id, string Reason);
public sealed record FidelityK1Op(string Id, string Reason, FeatureId? Feature,
    IReadOnlyList<Vector2>? Path = null, float HalfWidth = 0f) : FidelityOverlayOp(Id, Reason);
public sealed record FidelityRaiseOp(string Id, string Reason, FidelityZone To,
    FeatureId? Building = null, int? Segment = null, bool Roof = false,
    IReadOnlyList<Vector2>? Area = null) : FidelityOverlayOp(Id, Reason);
