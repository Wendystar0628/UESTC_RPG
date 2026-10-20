using System.Numerics;

namespace Campus.Contracts;

public enum FidelityZone : byte { K1 = 1, K2 = 2, K3 = 3, K4 = 4, K5 = 5 }
public sealed record FacadeSegment(FeatureId Building, int Index, Vector2 A, Vector2 B,
    FidelityZone Zone, FidelityZone GroundBand);
public sealed record RoofZone(FeatureId Building, FidelityZone Zone);

/// <summary>Build-time precision classification. Production worlds always publish one frozen result.</summary>
public interface IFidelityModel
{
    IReadOnlyList<FacadeSegment> Facades(FeatureId building);
    RoofZone Roof(FeatureId building);
    FidelityZone GroundAt(float x, float z);
    /// <summary>Explicit authored precision of this feature, including points and lines; null means no authored mark.</summary>
    FidelityZone? Declared(FeatureId feature);
    string InputHash { get; }
}

/// <summary>Row-major 2 m ground cells, building surfaces and explicit feature marks; persisted through the world build cache.</summary>
public sealed record FidelityFrozenData(Vector2 Min, int Width, int Height, byte[] Ground,
    IReadOnlyList<FacadeSegment> Facades, IReadOnlyList<RoofZone> Roofs,
    IReadOnlyDictionary<FeatureId, FidelityZone> FeatureZones, string InputHash);
