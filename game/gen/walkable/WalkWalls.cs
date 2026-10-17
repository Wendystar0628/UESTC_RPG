using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;

namespace Campus.Gen.Walkable;

internal static class WalkWalls
{
    internal static List<WallCollider> Extract(WalkRaster raster, ISpatialIndex spatial, Func<Vector2, float>? heightAt = null)
    {
        var walls = new List<WallCollider>();
        // Trace the boundary of the occupied 1 m cells, merging collinear runs exactly.
        // Outside is deliberately excluded: the campus fence owns the perimeter collision.
        for (var z = 1; z < raster.Height; z++)
        {
            var start = -1;
            for (var x = 0; x <= raster.Width; x++)
            {
                var edge = x < raster.Width && NeedsWall(raster.At(x, z - 1), raster.At(x, z));
                if (edge && start < 0) start = x;
                if (!edge && start >= 0)
                {
                    Add(new Vector2(start, z), new Vector2(x, z));
                    start = -1;
                }
            }
        }
        for (var x = 1; x < raster.Width; x++)
        {
            var start = -1;
            for (var z = 0; z <= raster.Height; z++)
            {
                var edge = z < raster.Height && NeedsWall(raster.At(x - 1, z), raster.At(x, z));
                if (edge && start < 0) start = z;
                if (!edge && start >= 0)
                {
                    Add(new Vector2(x, start), new Vector2(x, z));
                    start = -1;
                }
            }
        }
        return walls;

        void Add(Vector2 a, Vector2 b)
        {
            a = raster.Origin + a * raster.Step;
            b = raster.Origin + b * raster.Step;
            // Cap terrain sampling spans so a long merged edge cannot bridge over a local crest.
            var length = Math.Abs(b.X - a.X) + Math.Abs(b.Y - a.Y);
            var count = Math.Max(1, (int)MathF.Ceiling(length / 8f));
            for (var i = 0; i < count; i++)
            {
                var p = a + (b - a) * (i / (float)count);
                var q = a + (b - a) * ((i + 1f) / count);
                walls.Add(Create(p, q, spatial, SurfaceTag.Invisible, 2.2f, heightAt));
            }
        }
    }

    internal static WallCollider Create(Vector2 a, Vector2 b, ISpatialIndex spatial, SurfaceTag tag, float height,
        Func<Vector2, float>? heightAt = null)
    {
        var ya = heightAt?.Invoke(a) ?? spatial.GroundY(a.X, a.Y);
        var yb = heightAt?.Invoke(b) ?? spatial.GroundY(b.X, b.Y);
        var middle = (a + b) * .5f;
        var ym = heightAt?.Invoke(middle) ?? spatial.GroundY(middle.X, middle.Y);
        if (!float.IsFinite(ya) || !float.IsFinite(yb) || !float.IsFinite(ym))
            throw new InvalidOperationException("A walk wall has no finite terrain support.");
        return new WallCollider(a, b, .05f, Math.Min(ym, Math.Min(ya, yb)) - .25f,
            Math.Max(ym, Math.Max(ya, yb)) + height, tag)
        { Foot = FootMat.Concrete };
    }

    private static bool NeedsWall(WalkClass a, WalkClass b) =>
        IsAllowed(a) && IsBlocked(b) || IsAllowed(b) && IsBlocked(a);

    private static bool IsAllowed(WalkClass value) => value is WalkClass.Walk or WalkClass.Interior or WalkClass.Wade;
    private static bool IsBlocked(WalkClass value) => value is WalkClass.Forbidden or WalkClass.Water or WalkClass.Steep;
}
