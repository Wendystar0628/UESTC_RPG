using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Circulation;
using Campus.Gen.World;
using Campus.Gen.Sites;
using Campus.Gen.Maps.Qingshuihe.West.Six;
using NetTopologySuite.Geometries;
using S = Campus.Gen.Maps.Qingshuihe.West.WestTune.Six;

namespace Campus.Gen.Maps.Qingshuihe.West.SixInterior;

/// <summary>Authored open areas are clipped to the same recessed building footprints as the exterior.</summary>
internal sealed record SixInteriorPlan(float GroundY, PolygonRings CBase, PolygonRings BBase,
    PolygonRings Hall, PolygonRings UpperC, PolygonRings PublicB, PlanFrame FormulaFrame,
    float FormulaWidth, float FormulaRun, PlanFrame ReturnFrame, float ReturnWidth, float ReturnDepth,
    IReadOnlyList<Vector2> CRoute)
{
    public float FloorY => GroundY + GroundLevels.Paving;
    public float UpperY => GroundY + S.BaseStorey;
    public float StairRise => UpperY - FloorY;
    public float LandingY => FloorY + StairRise * 0.5f;
    public float FormulaLanding => CirculationStyle.StairLandingMin;
    public float FormulaFlightRun => (FormulaRun - FormulaLanding) * 0.5f;
    public float CeilingY => GroundY + S.BaseHeight - 0.45f;
    public const float SlabThickness = CirculationStyle.LandingThickness;

    public static SixInteriorPlan Of(BuildContext ctx)
    {
        var c = SixLayout.BasePlan(SixLayout.Plan(ctx.World, S.EntityC, S.PartC));
        var b = SixLayout.BasePlan(SixLayout.Plan(ctx.World, S.EntityB, S.PartB));
        var data = ctx.World.Feature(S.EntityC).RequiredProps().GetProperty("interior");
        var formula = data.GetProperty("formula");
        var bottom = formula.Pair("bottom"); var top = formula.Pair("top");
        var returning = data.GetProperty("return");
        var axis = returning.Pair("axis");
        var upper = (Polygon)Polygon(Ring(data, "upperC")).Union(Polygon(Ring(data, "upperPlatform")));
        return new(SixLayout.BaseY(ctx, S.EntityC), c, b,
            Clip(c, Ring(data, "hall")), Clip(c, From(upper)), Clip(b, Ring(data, "publicB")),
            PlanFrame.Along((bottom + top) * 0.5f, new Vector2(top.Y - bottom.Y, bottom.X - top.X)),
            formula.Number("width"), Vector2.Distance(bottom, top),
            PlanFrame.Along(returning.Pair("center"), axis), returning.Number("width"), returning.Number("depth"),
            Points(data, "cRoute"));
    }

    public PolygonRings FormulaOpening => new(FormulaFrame.Rect(-FormulaWidth * 0.5f, -FormulaRun * 0.5f,
        FormulaWidth * 0.5f, FormulaRun * 0.5f));
    public PolygonRings ReturnOpening => new(ReturnFrame.Rect(-ReturnWidth * 0.5f, -ReturnDepth * 0.5f,
        ReturnWidth * 0.5f, ReturnDepth * 0.5f));

    public IReadOnlyList<PolygonRings> UpperFloors => Subtract(UpperC, FormulaOpening, ReturnOpening);

    /// <summary>Keep authored closures; the exterior already owns every source-boundary span.</summary>
    public IEnumerable<(Vector2 A, Vector2 B, bool Atrium)> InteriorEdges(PolygonRings area, PolygonRings source, bool gallery)
    {
        var border = Polygon(source).Boundary.Buffer(0.02);
        var hall = Polygon(Hall).Buffer(-0.02);
        foreach (var (edge, _, _) in GridFacade.Edges(area))
        {
            NetTopologySuite.Geometries.Geometry line = Factory.CreateLineString([
                new Coordinate(edge.A.X, edge.A.Y), new Coordinate(edge.B.X, edge.B.Y)]).Difference(border);
            if (!gallery)
            {
                foreach (var span in Lines(line)) yield return (span.A, span.B, false);
                continue;
            }
            line = line.Difference(Polygon(FormulaOpening).Union(Polygon(ReturnOpening)).Buffer(0.02));
            foreach (var span in Lines(line.Intersection(hall))) yield return (span.A, span.B, true);
            foreach (var span in Lines(line.Difference(hall))) yield return (span.A, span.B, false);
        }
    }

    public IEnumerable<(Vector2 A, Vector2 B, float Top)> LowerWallSegments(Vector2 a, Vector2 b)
    {
        var upper = Factory.CreateMultiPolygon(UpperFloors.Select(Polygon).ToArray()).Union().Buffer(0.02);
        var line = Factory.CreateLineString([new Coordinate(a.X, a.Y), new Coordinate(b.X, b.Y)]);
        foreach (var span in Lines(line.Intersection(upper))) yield return (span.A, span.B, UpperY - SlabThickness);
        foreach (var span in Lines(line.Difference(upper))) yield return (span.A, span.B, CeilingY);
    }

    private static IEnumerable<(Vector2 A, Vector2 B)> Lines(NetTopologySuite.Geometries.Geometry geometry)
    {
        if (geometry is LineString line)
        {
            var coordinates = line.Coordinates;
            for (var i = 1; i < coordinates.Length; i++)
            {
                var a = new Vector2((float)coordinates[i - 1].X, (float)coordinates[i - 1].Y);
                var b = new Vector2((float)coordinates[i].X, (float)coordinates[i].Y);
                if (Vector2.Distance(a, b) > 0.03f) yield return (a, b);
            }
        }
        else if (geometry is GeometryCollection collection)
            for (var i = 0; i < collection.NumGeometries; i++)
                foreach (var span in Lines(collection.GetGeometryN(i))) yield return span;
    }

    private static PolygonRings Ring(System.Text.Json.JsonElement data, string key) => new(Points(data, key));
    private static Vector2[] Points(System.Text.Json.JsonElement data, string key) =>
        data.GetProperty(key).EnumerateArray().Select(p => new Vector2(p[0].GetSingle(), p[1].GetSingle())).ToArray();

    private static readonly GeometryFactory Factory = new();
    private static Polygon Polygon(PolygonRings ring) => Factory.CreatePolygon(Linear(ring.Outer), ring.Holes.Select(Linear).ToArray());
    private static LinearRing Linear(IReadOnlyList<Vector2> ring) => Factory.CreateLinearRing(
        ring.Append(ring[0]).Select(p => new Coordinate(p.X, p.Y)).ToArray());
    private static PolygonRings From(Polygon polygon) => new(
        PlanGeometry.OpenRing(polygon.ExteriorRing.Coordinates.Select(c => new Vector2((float)c.X, (float)c.Y))),
        Enumerable.Range(0, polygon.NumInteriorRings).Select(i => PlanGeometry.OpenRing(
            polygon.GetInteriorRingN(i).Coordinates.Select(c => new Vector2((float)c.X, (float)c.Y)), false)).ToArray());
    private static PolygonRings Clip(PolygonRings source, PolygonRings area) =>
        Polygon(source).Intersection(Polygon(area)) is Polygon polygon ? From(polygon)
        : throw new InvalidOperationException("Six interior open area must be one connected part of its source block.");
    private static IReadOnlyList<PolygonRings> Subtract(PolygonRings source, params PolygonRings[] holes)
    {
        NetTopologySuite.Geometries.Geometry geometry = Polygon(source);
        foreach (var hole in holes) geometry = geometry.Difference(Polygon(hole));
        if (geometry is Polygon polygon) return [From(polygon)];
        return Enumerable.Range(0, geometry.NumGeometries).Select(i => From((Polygon)geometry.GetGeometryN(i))).ToArray();
    }
}
