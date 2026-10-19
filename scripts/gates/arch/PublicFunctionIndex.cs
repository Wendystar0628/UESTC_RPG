using Microsoft.CodeAnalysis;

namespace Campus.Arch.Tests;

// The public-layer index. A family is a name AND a parameter shape, not a global reservation of a common English word.
internal static class PublicFunctionIndex
{
    internal sealed record Family(string[] Names, string[] Owners, Func<IMethodSymbol, bool> Shape);
    private const string Scalar = "Campus.Contracts.ScalarMath";
    private const string Vector = "Campus.Contracts.VectorMath";
    private const string Angles = "Campus.Contracts.Angles";
    private const string Plan = "Campus.Gen.Geometry.PlanGeometry";
    private const string Lines = "Campus.Contracts.PlanLines";
    private const string Reads = "Campus.Gen.Sites.FeatureReads";
    private const string Features = "Campus.Gen.World.WorldFeatureGeometry";
    private const string Props = "Campus.Gen.World.WorldProps";

    public static readonly Family[] All =
    [
        new(["Lerp"], [Scalar], m => m.Parameters.Length == 3 && m.Parameters.All(p => IsScalar(p.Type)) && IsScalar(m.ReturnType)),
        new(["Smooth", "SmoothStep", "Smoothstep", "Smootherstep"], [Scalar],
            m => m.Parameters.Length is 1 or 3 && m.Parameters.All(p => IsScalar(p.Type)) && IsScalar(m.ReturnType)),
        new(["Finite"], [Vector], m => m.Parameters.Length == 1 && IsVector(m.Parameters[0].Type)),
        new(["Unit", "Dot", "Cross"], [Vector, Plan], m => m.Parameters.Length > 0 && IsVector(m.Parameters[0].Type)),
        // Quantized plan interpolation deliberately differs from scalar and engine vector interpolation.
        new(["Lerp"], [Plan], m => m.Parameters.Length == 3 && IsVector(m.Parameters[0].Type) && IsVector(m.Parameters[1].Type)),
        new(["WrapDegrees", "CompassYawToGodotRadians"], [Angles],
            m => m.Parameters.Length == 1 && IsScalar(m.Parameters[0].Type) && IsScalar(m.ReturnType)),
        new(["SignedDelta"], [Angles], m => m.Parameters.Length == 2 && m.Parameters.All(p => IsScalar(p.Type)) && IsScalar(m.ReturnType)),
        new(["Heading"], [Angles], m => m.Parameters.Length == 1 && IsScalar(m.Parameters[0].Type) && SourceModel.Name(m.ReturnType) == "System.Numerics.Vector2"),
        new(["Forward"], [Angles], m => m.Parameters.Length == 2 && m.Parameters.All(p => IsScalar(p.Type)) && SourceModel.Name(m.ReturnType) == "System.Numerics.Vector3"),
        new(["Bearing"], [Angles], m => m.Parameters.Length == 1 && IsVector(m.Parameters[0].Type)),
        new(["Centroid", "CleanRing", "DeduplicateRing", "OpenRing", "SignedArea", "FanSignedArea", "SelfIntersects"], [Plan],
            m => m.Parameters.Length > 0 && IsRing(m.Parameters[0].Type)),
        new(["Sample", "Project", "Length", "Subline", "PolylineDistanceSquared", "CleanPath"], [Lines],
            m => m.Parameters.Length > 0 && IsRing(m.Parameters[0].Type)),
        new(["PointInRing", "InsideByCrossings", "DistanceToRing", "DistanceToEdges"], [Plan],
            m => m.Parameters.Length > 1 && (IsVector(m.Parameters[0].Type) || IsRing(m.Parameters[0].Type))),
        new(["SegmentDistanceSquared", "ClosestPoint"], [Lines], m => m.Parameters.Length > 1 && IsVector(m.Parameters[0].Type)),
        new(["SegmentDistance"], [Lines], m => m.Parameters.Length == 4 && IsScalar(m.Parameters[0].Type) &&
            IsScalar(m.Parameters[1].Type) && IsVector(m.Parameters[2].Type) && IsVector(m.Parameters[3].Type)),
        new(["LineIntersection", "SegmentIntersection", "SegmentHit", "SegmentContact"], [Lines],
            m => m.Parameters.Length >= 4 && m.Parameters.Take(4).All(p => IsVector(p.Type))),
        new(["Feature", "Part", "Levels", "Point", "Outer", "Outers", "Line", "Contour", "ContourGeometry", "PointPair", "Rectangle", "ContourSegment", "ContourFrame", "TerrainLine"], [Reads],
            m => m.Parameters.Length > 0 && SourceModel.Name(m.Parameters[0].Type) is
                "Campus.Contracts.WorldFeature" or "Campus.Contracts.WorldFeaturePart" or "Campus.Contracts.IWorldModel"),
        new(["Polygons"], [Features], m => m.Parameters.Length == 1 && SourceModel.Name(m.Parameters[0].Type) == "Campus.Contracts.WorldFeature"),
        new(["PolygonsOf"], [Features], m => m.Parameters.Length == 1 && SourceModel.Name(m.Parameters[0].Type) == "Campus.Contracts.WorldGeometry"),
        new(["FacadePolygonsOf"], [Features], m => m.Parameters.Length is 1 or 2 && SourceModel.Name(m.Parameters[0].Type) == "Campus.Contracts.WorldFeature"),
        new(["FacadeGeometryOf"], [Features], m => m.Parameters.Length == 1 && SourceModel.Name(m.Parameters[0].Type) == "Campus.Contracts.WorldFeaturePart"),
        new(["Text", "OptionalText", "Number", "OptionalNumber", "Numbers", "Pair", "TryText", "TryNumber"], [Props],
            m => m.Parameters.Length == 2 && m.Parameters[1].Type is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_String } &&
                SourceModel.Name(m.Parameters[0].Type) is "Campus.Contracts.WorldFeature" or "System.Text.Json.JsonElement" or "System.Text.Json.JsonElement?"),
        new(["RequiredProps"], [Props], m => m.Parameters.Length == 1 && SourceModel.Name(m.Parameters[0].Type) == "Campus.Contracts.WorldFeature"),
        // Mix(uint)->WindowCard is weighted material selection; only uint->uint is the noise avalanche.
        new(["Mix"], ["Campus.Contracts.IntegerNoise"], m => m.Parameters.Length == 1 &&
            m.Parameters[0].Type.SpecialType == SpecialType.System_UInt32 && m.ReturnType.SpecialType == SpecialType.System_UInt32),
        new(["Hash1D", "Hash2D", "Hash3D", "High24", "Low24", "Value1D", "Value2D", "TileableValue2D", "PuddleValue2D"],
            ["Campus.Contracts.IntegerNoise"], m => m.Parameters.Length > 0 &&
                m.Parameters.All(p => IsScalar(p.Type) || p.Type.SpecialType is SpecialType.System_UInt32 or SpecialType.System_Int32))
    ];

    private static bool IsScalar(ITypeSymbol type) => type.SpecialType is SpecialType.System_Single or SpecialType.System_Double;
    private static bool IsVector(ITypeSymbol type) => SourceModel.Name(type) is
        "System.Numerics.Vector2" or "System.Numerics.Vector3" or "System.Numerics.Vector4";
    private static bool IsRing(ITypeSymbol type) => SourceModel.Name(type) == "Campus.Contracts.PolygonRings" ||
        type is IArrayTypeSymbol array && (IsVector(array.ElementType) || IsRing(array.ElementType)) ||
        type is INamedTypeSymbol named && named.TypeArguments.Any(argument => IsVector(argument) || IsRing(argument));
}
