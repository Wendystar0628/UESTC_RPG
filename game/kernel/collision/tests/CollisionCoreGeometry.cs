using System.Buffers.Binary;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Gen.Collision;
using Campus.Gen.Ground;
using Format = Godot.RenderingServer.ArrayFormat;

namespace Campus.Kernel.Collision.Tests;

public sealed partial class CollisionCoreTests
{
    private void CheckGround(CollisionTile collision, TileBundle visible)
    {
        var fields = collision.Colliders.OfType<HeightFieldCollider>().Where(field => field.Tile == collision.Tile).ToArray();
        if (fields.Length != 1) { Fail($"tile={collision.Tile} ground height fields={fields.Length}, expected one."); return; }
        var checkedPoints = 0;
        var maximum = 0f;
        Vector3 worst = default;
        foreach (var surface in visible.Surfaces)
            foreach (var triangle in Triangles(surface))
            {
                var normal = Vector3.Cross(triangle.B - triangle.A, triangle.C - triangle.A);
                if (MathF.Abs(normal.Y) < normal.Length() * .7f) continue; // Vertical skirts/kerb faces are not supporting ground.
                foreach (var point in Samples(triangle))
                {
                    var height = GroundCollision.SampleHeight(fields[0], point.X, point.Z);
                    if (!float.IsFinite(height)) continue; // An explicitly encoded height-field hole has no ground surface.
                    var error = MathF.Abs(point.Y - height);
                    checkedPoints++;
                    if (error > maximum) { maximum = error; worst = point; }
                }
            }
        if (checkedPoints == 0) Fail($"tile={collision.Tile} has no measured L0 supporting triangles.");
        if (maximum > .02f) Fail($"tile={collision.Tile} L0/C error={maximum:R}m at {worst}, limit=.02m.");
    }

    private void CheckBuildingEdges(IMapContext context, IReadOnlyDictionary<TileKey, CollisionTile> tiles)
    {
        var worldWalls = tiles.Values.SelectMany(tile => tile.Colliders.OfType<WallCollider>()
            .Select(wall => (WallCollider)CollisionGeometry.Transform(wall, Matrix4x4.CreateTranslation(CollisionGeometry.Origin(tile.Tile)))))
            .Where(wall => wall.Tag is SurfaceTag.Wall or SurfaceTag.DoorClosed).Distinct().ToArray();
        foreach (var feature in context.World.Features.Values.OrderBy(feature => feature.Id.Value, StringComparer.Ordinal))
            foreach (var footprint in CollisionTileBuilder.BuildingFootprints(feature))
                foreach (var ring in new[] { footprint.Rings.Outer }.Concat(footprint.Rings.Holes))
                    for (var edge = 0; edge < ring.Count; edge++)
                    {
                        var a = ring[edge]; var b = ring[(edge + 1) % ring.Count];
                        var length = Vector2.Distance(a, b);
                        if (length < .001f) continue;
                        var direction = (b - a) / length;
                        var matches = worldWalls.Where(wall =>
                            MathF.Abs(Cross(direction, wall.B - wall.A)) <= .001f * Vector2.Distance(wall.A, wall.B)
                            && DistanceLine(wall.A, a, direction) <= .3f && DistanceLine(wall.B, a, direction) <= .3f)
                            .Select(wall => (Wall: wall, Min: MathF.Min(Vector2.Dot(wall.A - a, direction), Vector2.Dot(wall.B - a, direction)),
                                Max: MathF.Max(Vector2.Dot(wall.A - a, direction), Vector2.Dot(wall.B - a, direction))))
                            .Where(wall => wall.Max >= 0 && wall.Min <= length).OrderBy(wall => wall.Min).ToArray();
                        var end = 0f;
                        foreach (var wall in matches)
                        {
                            if (wall.Min > end + WalkTuningTolerance) break;
                            end = MathF.Max(end, wall.Max);
                        }
                        if (end < length - WalkTuningTolerance)
                            Fail($"building={feature.Id.Value}/{footprint.SourceId} edge={edge} missing wall coverage at {end:R}/{length:R}m.");
                        foreach (var entrance in context.World.Interiors.Get(feature.Id)?.Entrances ?? [])
                        {
                            var center = new Vector2(entrance.Center.X, entrance.Center.Z);
                            if (DistanceLine(center, a, direction) > .5f) continue;
                            var at = Vector2.Dot(center - a, direction);
                            if (at <= 0 || at >= length) continue;
                            var doorWalls = matches.Where(wall => wall.Min <= at && wall.Max >= at).Select(wall => wall.Wall).ToArray();
                            var y = entrance.Center.Y + 1;
                            var open = entrance.Open && context.World.Interiors.Get(feature.Id) is { Open: true };
                            if (open && doorWalls.Any(wall => wall.Y0 <= y && wall.Y1 >= y))
                                Fail($"building={feature.Id.Value} open entrance={entrance.Id} has a solid wall at its center.");
                            if (!open && !doorWalls.Any(wall => wall.Tag == SurfaceTag.DoorClosed && wall.Y0 <= y && wall.Y1 >= y))
                                Fail($"building={feature.Id.Value} closed entrance={entrance.Id} lacks DoorClosed coverage.");
                        }
                    }
    }

    private const float WalkTuningTolerance = .01f;
    private readonly record struct Triangle(Vector3 A, Vector3 B, Vector3 C);
    private static IEnumerable<Vector3> Samples(Triangle triangle) =>
        [triangle.A, triangle.B, triangle.C, (triangle.A + triangle.B) / 2, (triangle.B + triangle.C) / 2,
            (triangle.C + triangle.A) / 2, (triangle.A + triangle.B + triangle.C) / 3];

    private static IEnumerable<Triangle> Triangles(BakedSurface surface)
    {
        Require(surface.Primitive == BakedPrimitive.Triangles, "G15 has no decoder for a non-triangle collision source.");
        var compressed = (surface.Format & (ulong)Format.FlagCompressAttributes) != 0;
        var positions = new Vector3[surface.VertexCount];
        for (var vertex = 0; vertex < positions.Length; vertex++)
        {
            var bytes = surface.VertexData.Span[(vertex * (compressed ? 8 : 12))..];
            positions[vertex] = compressed
                ? surface.Bounds.Min + (surface.Bounds.Max - surface.Bounds.Min) * new Vector3(
                    BinaryPrimitives.ReadUInt16LittleEndian(bytes) / 65535f,
                    BinaryPrimitives.ReadUInt16LittleEndian(bytes[2..]) / 65535f,
                    BinaryPrimitives.ReadUInt16LittleEndian(bytes[4..]) / 65535f)
                : new(BinaryPrimitives.ReadSingleLittleEndian(bytes), BinaryPrimitives.ReadSingleLittleEndian(bytes[4..]), BinaryPrimitives.ReadSingleLittleEndian(bytes[8..]));
            positions[vertex] = Vector3.Transform(positions[vertex], surface.At);
        }
        var index = new int[surface.IndexCount];
        Require(surface.IndexData.Length == index.Length * 2, "G15 expected the current UInt16 bake index format.");
        for (var i = 0; i < index.Length; i++) index[i] = BinaryPrimitives.ReadUInt16LittleEndian(surface.IndexData.Span[(i * 2)..]);
        for (var i = 0; i < index.Length; i += 3) yield return new(positions[index[i]], positions[index[i + 1]], positions[index[i + 2]]);
    }

    private static IEnumerable<Triangle> Triangles(GeometryBundle mesh, Matrix4x4 at)
    {
        Vector3 Point(int index) => Vector3.Transform(new(mesh.Position[index * 3], mesh.Position[index * 3 + 1], mesh.Position[index * 3 + 2]), at);
        for (var i = 0; i < mesh.Index.Length; i += 3) yield return new(Point(mesh.Index[i]), Point(mesh.Index[i + 1]), Point(mesh.Index[i + 2]));
    }

    private static float DistanceLine(Vector2 point, Vector2 a, Vector2 direction) => MathF.Abs(Cross(point - a, direction));
    private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;
    private static float DistanceSquared(Vector2 point, Triangle triangle)
    {
        var a = new Vector2(triangle.A.X, triangle.A.Z); var b = new Vector2(triangle.B.X, triangle.B.Z); var c = new Vector2(triangle.C.X, triangle.C.Z);
        var ab = Cross(b - a, point - a); var bc = Cross(c - b, point - b); var ca = Cross(a - c, point - c);
        var area = Cross(b - a, c - a);
        if (MathF.Abs(area) > 1e-8f && (ab >= 0 && bc >= 0 && ca >= 0 || ab <= 0 && bc <= 0 && ca <= 0)) return 0;
        return MathF.Min(SegmentDistanceSquared(point, a, b), MathF.Min(SegmentDistanceSquared(point, b, c), SegmentDistanceSquared(point, c, a)));
    }
    private static float SegmentDistanceSquared(Vector2 point, Vector2 a, Vector2 b)
    {
        var edge = b - a;
        var t = edge.LengthSquared() == 0 ? 0 : Math.Clamp(Vector2.Dot(point - a, edge) / edge.LengthSquared(), 0, 1);
        return Vector2.DistanceSquared(point, a + edge * t);
    }
}
