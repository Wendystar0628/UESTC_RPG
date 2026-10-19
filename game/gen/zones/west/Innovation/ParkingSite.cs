using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.Common;
using L = Campus.Gen.Zones.West.WestTune.Look;

namespace Campus.Gen.Zones.West.Innovation;

/// <summary>Two double-row parking groups, divided by the authored service-road entrance.</summary>
public sealed class ParkingSite : ZoneSite
{
    public override string Id => "19b.innovation-parking";

    private static (PlanFrame Frame, float Length) Edge(IWorldModel world)
    {
        var (a, z) = world.Feature(WestIds.ParkingFront).PointPair();
        return (PlanFrame.Toward(a, z), Vector2.Distance(a, z));
    }

    public override void Build(SiteBuilder b)
    {
        var world = b.Context.World;
        var (f, length) = Edge(world);
        var layout = world.Feature(WestIds.ParkingFront);
        var gap = layout.Number("entranceWidth");
        var bay = layout.Number("bayWidth");
        var depth = layout.Number("bayDepth");
        var clear = layout.Number("clearHeight");
        // Two double rows fit the authored 36 m band, with six-metre aisles between each pair.
        foreach (var row in new[] { 0f, 11f, 20f, 31f })
            foreach (var (lo, hi) in new[] { (0f, length * .5f - gap * .5f), (length * .5f + gap * .5f, length) })
                for (var x = lo; x + bay <= hi + .01f; x += bay)
                {
                    var y = b.Ground(f.P(x, row)) + .11f;
                    GridFacade.Box(b, f, x, x + .1f, y, y + .008f, row, row + depth, L.WhiteFrame, 0);
                    GridFacade.Box(b, f, x, x + bay, y, y + .008f, row + depth - .1f, row + depth, L.WhiteFrame, 0);
                }
        // +Z points into the parking band; leave the middle seven metres entirely free.
        foreach (var (lo, hi) in new[] { (0f, length * .5f - gap * .5f), (length * .5f + gap * .5f, length) })
        {
            for (var x = lo + bay * .5f; x + bay * .5f <= hi + .01f; x += bay)
            {
                var y = b.Ground(f.P(x, 0));
                GridFacade.Box(b, f, x - bay * .5f, x + bay * .5f, y + clear, y + clear + .18f, 0, depth, L.Metal, .025f);
                b.Place(PartCatalog.SolarPanel, new PanelParams(bay - .08f, .08f, depth - .08f), f.At(x, y + clear + .18f, depth * .5f));
                GridFacade.Box(b, f, x - .09f, x + .09f, y, y + clear, depth - .3f, depth - .12f, L.Metal, .025f);
            }
        }
        var (a, z) = world.Feature(WestIds.ParkingBack).PointPair();
        var back = PlanFrame.Toward(a, z);
        var backLength = Vector2.Distance(a, z);
        for (var x = 0f; x < backLength; x += 2f)
        {
            var end = MathF.Min(x + 2f, backLength);
            var y = b.Ground(back.P((x + end) * .5f, 0));
            GridFacade.Box(b, back, x, end, y, y + .45f, -.3f, .3f, L.Granite, .025f);
            if (b.Detail == SiteDetail.Mass) continue;
            // Recycled pale blocks within the open steel cage; no texture noise or fake letters.
            for (var gx = x; gx <= end; gx += .25f)
                foreach (var side in new[] { -.31f, .31f })
                    GridFacade.Box(b, back, gx - .008f, gx + .008f, y, y + .46f, side - .008f, side + .008f, L.Metal, 0);
            foreach (var h in new[] { .05f, .25f, .45f })
                foreach (var side in new[] { -.31f, .31f })
                    GridFacade.Box(b, back, x, end, y + h, y + h + .015f, side - .008f, side + .008f, L.Metal, 0);
        }
        // Pebble rims follow terrain beside the shallow grass drain.
        var drain = world.Feature(WestIds.ParkingDrain).Line();
        foreach (var side in new[] { -.75f, .75f })
        {
            var line = drain.Select(p => p + back.W * side).ToArray();
            for (var i = 1; i < line.Length; i++)
            {
                var count = Math.Max(1, (int)(Vector2.Distance(line[i - 1], line[i]) / .4f));
                for (var k = 0; k <= count; k++)
                {
                    var at = Vector2.Lerp(line[i - 1], line[i], (float)k / count);
                    b.Add(Shapes.Shaft(.11f, .09f, .09f, 6, L.Pebble), Matrix4x4.CreateTranslation(at.X, b.Ground(at) - .02f, at.Y));
                }
            }
        }
        var grate = Vector2.Lerp(drain[0], drain[^1], .5f);
        b.Place(PartCatalog.Drain, new TrimParams(2f, .12f, .3f),
            back.At(back.Local(grate).X, b.Ground(grate) + .1f, back.Local(grate).Y));
    }

    public override void Collide(SiteColliders c)
    {
        var (f, length) = Edge(c.Context.World);
        var layout = c.Context.World.Feature(WestIds.ParkingFront);
        var gap = layout.Number("entranceWidth");
        var bay = layout.Number("bayWidth");
        var depth = layout.Number("bayDepth");
        var height = layout.Number("clearHeight");
        foreach (var (lo, hi) in new[] { (0f, length * .5f - gap * .5f), (length * .5f + gap * .5f, length) })
            for (var x = lo + bay * .5f; x + bay * .5f <= hi + .01f; x += bay)
                c.Box(f.At(x, c.Ground(f.P(x, depth - .21f)) + height * .5f, depth - .21f), new(.09f, height * .5f, .09f), SurfaceTag.Prop, FootMat.Metal);
        var (a, z) = c.Context.World.Feature(WestIds.ParkingBack).PointPair();
        var back = PlanFrame.Toward(a, z);
        var len = Vector2.Distance(a, z);
        for (var x = 0f; x < len; x += 2f)
        {
            var end = MathF.Min(x + 2f, len);
            var mid = (x + end) * .5f;
            c.Box(back.At(mid, c.Ground(back.P(mid, 0)) + .225f, 0), new((end - x) * .5f, .225f, .3f), SurfaceTag.Prop, FootMat.StonePaving);
        }
    }
}
