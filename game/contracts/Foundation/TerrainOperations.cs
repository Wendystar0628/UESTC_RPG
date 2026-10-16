using System.Numerics;
using System.Text.Json.Serialization;

namespace Campus.Contracts;

public sealed record TerrainSide(string Fill, string Cut);
public sealed record TerrainAnchor(double S, double Y);
public enum TerrainProfile : byte { Hermite }
public enum TerrainBankKind : byte { Hard, Soft, Steps, Wetland, Swale }
/// <summary>
/// Wet banks use Kind and Slope only. Swale uses Inner and optional Outer, with no Slope:
/// Inner rises from each ditch-bottom edge to its crest; Outer falls from that crest to surrounding terrain.
/// The section is symmetric and independent of line direction or nearby roads. JSON outer=0 and omission
/// both mean no outer segment and normalize to Outer=null; serialization omits that optional field.
/// </summary>
public sealed record TerrainBank(TerrainBankKind Kind, string? Slope = null)
{
    public string? Inner { get; init; }
    public string? Outer { get; init; }
}
public sealed record TerrainBed(double MaxDepth, string Shelf = "1:7");
public enum TerrainWeirKind : byte { Weir, Gate, Culvert }

/// <summary>
/// A real structure joining two distinct constant-level water entities. S is station along this channel's
/// ordered centreline. Structure must exist with the matching kind and connect Upstream to Downstream;
/// Drop must equal WaterY(Upstream) minus WaterY(Downstream). Reference/topology checks belong to WP-08.
/// This record never creates water entities or introduces multiple levels inside one FeatureId.
/// </summary>
public sealed record TerrainWeir(double S, double Drop, TerrainWeirKind Kind,
    FeatureId Structure, FeatureId Upstream, FeatureId Downstream);

public enum TerrainEdgeKind : byte { EdgeBand, Terrace }
// StructureId must resolve to a real structure; a declaration alone does not prove a wall exists.
public sealed record TerrainPadEdge(string StructureId, TerrainEdgeKind Kind, double Riser, double Tread, int Tiers);

// The nested shape object's first field is its "shape" discriminator. Stage 1 has no polygon hill/basin.
[JsonPolymorphic(TypeDiscriminatorPropertyName = "shape", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(TerrainCircle), "circle")]
[JsonDerivedType(typeof(TerrainEllipse), "ellipse")]
public abstract record TerrainShape;
public sealed record TerrainCircle(Vector2 Center, double Radius) : TerrainShape;
// Yaw follows the project convention: 0 north, 90 east. Evaluate trig only through DetMath.
public sealed record TerrainEllipse(Vector2 Center, Vector2 Radii, double YawDeg) : TerrainShape;

// One platform operation covers one outer ring plus its holes. Independent pieces use distinct operation
// IDs with explicit matching elevations/parameters; they are never merged into an aggregate outer hull.
public sealed record TerrainPlatformOp(string Id, string Reason, PolygonRings Rings, double Y,
    TerrainSide Side, double Fillet, int Priority = 300) : TerrainOp(Id, Reason, Priority);

// Height is positive additive amplitude. Basin subtracts this positive amplitude, never negates an input.
public sealed record TerrainMoundOp(string Id, string Reason, TerrainShape Shape, double Height,
    TerrainProfile Profile = TerrainProfile.Hermite, int Priority = 100) : TerrainOp(Id, Reason, Priority);
public sealed record TerrainBasinOp(string Id, string Reason, TerrainShape Shape, double Height,
    TerrainProfile Profile = TerrainProfile.Hermite, int Priority = 100) : TerrainOp(Id, Reason, Priority);

/// <summary>
/// PolygonGeometry requires distinct From/To points and no Width. Project onto From-to-To, clamp the
/// normalized parameter to [0,1], then interpolate Y0/Y1; CrossFall is perpendicular to that axis.
/// PolylineGeometry requires Width and no From/To; line order establishes the Y0-to-Y1 direction.
/// MultiPolygonGeometry and PointGeometry are explicitly rejected in stage 1.
/// </summary>
public sealed record TerrainSlopeOp(string Id, string Reason, OverlayGeometry Geometry, double Y0, double Y1,
    double CrossFall, TerrainSide Side, double Fillet, Vector2? From = null, Vector2? To = null,
    double? Width = null, int Priority = 200) : TerrainOp(Id, Reason, Priority);

public sealed record TerrainTerraceOp(string Id, string Reason, Polyline Front, Vector2 Inward, double FrontY,
    int Tiers, double Riser, double Tread, IReadOnlyList<double> StairAt, int Priority = 300) : TerrainOp(Id, Reason, Priority);

/// <summary>
/// A wet channel's Water is always a real, separately registered constant-level water segment. Optional Line supplies its
/// explicit centreline (and then Width is required); omission resolves geometry/width from that water's sources.
/// Resolve all genuine water parts with their separate PolygonRings and holes, not a parent's aggregate hull.
/// A wet channel applies as a line-shaped operation and later its water/shore constraint in the fixed water group.
/// Dry=true is explicit: Water/WaterY must be null, Line/Width and Swale bank are required, and Weirs is empty.
/// A dry channel never adds a water-table entry or water constraint. Stage 1 Crossings are dry-channel-only;
/// wet channels keep their existing network bridge/culvert and Weirs rules. Crossings use the two forms below;
/// the compiler must resolve the named real road and its actual centreline/width, never infer an unrelated road.
/// </summary>
public sealed record TerrainChannelOp(string Id, string Reason,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] FeatureId? Water,
    double Depth, TerrainBank Bank,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] double? WaterY,
    IReadOnlyList<TerrainWeir> Weirs, Polyline? Line = null, double? Width = null,
    int Priority = 50, IReadOnlyList<TerrainCrossing> Crossings = null!) : TerrainOp(Id, Reason, Priority)
{
    public bool Dry { get; init; }
    // Bind the optional collection to the constructor: .NET 10 source generation otherwise overwrites an
    // unmatched init-only initializer with null when omitted. Explicit JSON null remains forbidden by the
    // non-nullable constructor parameter plus TerrainOverlayJson's RespectNullableAnnotations setting.
    public IReadOnlyList<TerrainCrossing> Crossings { get; init; } = Crossings ?? [];
}

public abstract record TerrainCrossing(FeatureId Road);

/// <summary>
/// JSON {road,at:[x,z]}. At selects the nearest real intersection of the named road centreline and this
/// channel line; projection is allowed only to a real intersection within 1 m of the authored At point.
/// Two independent projections onto disjoint lines are not an intersection and must fail.
/// The channel passes under the unchanged road profile; its bed remains continuous but the buried segment
/// is not rendered. End-wall parts belong to the zone. This is not permission to invent a road or culvert mapping.
/// </summary>
public sealed record TerrainUnderpassCrossing(FeatureId Road, Vector2 At) : TerrainCrossing(Road);

/// <summary>
/// JSON {road,endBefore:true}. This channel stops before the named road rather than passing underneath it.
/// Resolve the real road edge/half-width, finish 1 m outside that edge, and use a semicircular end cap;
/// the bed rises back to ground over the final 2 m. Missing or ambiguous source mapping must fail.
/// </summary>
public sealed record TerrainEndBeforeCrossing(FeatureId Road) : TerrainCrossing(Road)
{
    public bool EndBefore => true;
}

// Fixed design values from contract-25, Opus 2026-10-16 07:49; they are not author-editable overrides.
public static class TerrainCrossingRules
{
    public const double IntersectionToleranceMetres = 1.0;
    public const double EndBeforeRoadEdgeClearanceMetres = 1.0;
    public const double EndBeforeRecoveryMetres = 2.0;
}

// Water lookup retains every water part and hole. Wade is a list of independent polygons, each with holes.
// A single bank kind is supported first; stable per-shore-segment addressing is a later contract addition.
public sealed record TerrainLakeOp(string Id, string Reason, FeatureId Water, double WaterY, TerrainBank Bank,
    TerrainBed Bed, IReadOnlyList<PolygonRings> Wade, int Priority = 50) : TerrainOp(Id, Reason, Priority);

// Missing PadY requests the evaluator's approved pre-pad median rule; it is not a zero-height default.
// Grade has no normative default and remains required. The caller must supply actual edge structure evidence.
public sealed record TerrainPadOp(string Id, string Reason, FeatureId Feature, double Grade, double? PadY = null,
    double Apron = 0.8, TerrainPadEdge? Edge = null, int Priority = 50) : TerrainOp(Id, Reason, Priority);

/// <summary>Absolute metres along the resolved source road in its original point order; never rebased to a clipped fragment.</summary>
/// <remarks>Requires finite 0 &lt;= S0 &lt; S1. Source-road length, real zone containment and overlap checks belong to WP-08.</remarks>
public sealed record TerrainStationRange(double S0, double S1);

// S is cumulative metres along the resolved source road's original ordered centreline.
// With no Range, anchors run from zero through the actual full length as before. With Range,
// first/last anchors equal S0/S1 and all steps remain inside it. The compiler builds one final profile
// per source road from all authored intervals and automatic gaps; it never fits independent profiles
// then splices them. Fixed anchors cannot move. Unknown zone geometry is not verified containment.
public sealed record TerrainCorridorOp(string Id, string Reason, FeatureId Road, IReadOnlyList<TerrainAnchor> Anchors,
    double CrossFall, double Shoulder, int Priority = 50,
    IReadOnlyList<TerrainCorridorStep> Steps = null!) : TerrainOp(Id, Reason, Priority)
{
    // The null constructor default represents omission only; an explicit JSON null is rejected before construction.
    public IReadOnlyList<TerrainCorridorStep> Steps { get; init; } = Steps ?? [];
    /// <summary>Optional explicit authoring interval. Null/omitted preserves the full-source-road contract.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TerrainStationRange? Range { get; init; }
}

// Along the same ordered centreline, S0 through S1 is handled by the named real stair structure.
// Like a bridge interval, it neither clamps the terrain nor participates in the corridor longitudinal-grade limit.
// The evaluator must verify the structure, its extent and the two end connections, rather than accept a bare exemption.
public sealed record TerrainCorridorStep(double S0, double S1, FeatureId Structure);
