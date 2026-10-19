using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Sport;
using Campus.Gen.Parts.Street;
using Campus.Gen.Sites;
using S = Campus.Gen.Parts.Sport.SportsStyle;

namespace Campus.Gen.Maps.Qingshuihe.CenterEast.Sports;

/// <summary>Court markings and furniture dress the ground producer's base surfaces; tree walls consume authored groups.</summary>
public sealed class CourtSites : ZoneSite
{
    public override string Id => "21d.courts";

    private static Campus.Contracts.Maps.MapRect AreaRange(WorldFeature feature) =>
        SiteRanges.Bounds(feature, S.CourtGateWidth + S.FenceGroundBand);

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        [.. Areas(world).Select(AreaRange)];

    internal static IEnumerable<WorldFeature> Areas(IWorldModel world) => SportsLayout.Courts(world)
        .Append(world.Feature(CenterEastIds.Sports.Track.Entity));

    internal static IEnumerable<(Vector2 A, Vector2 B, float Bottom, float Top, bool Closed)> FenceRuns(WorldFeature feature) =>
        FenceRuns(feature.Contour(), feature.RequiredProps().Pair("gate"), feature.Id.Value == CenterEastIds.Sports.Track.Entity ? S.TrackFence : S.CourtFence, S.CourtGateWidth, false);

    internal static IEnumerable<(Vector2 A, Vector2 B, float Bottom, float Top, bool Closed)> FenceRuns(IReadOnlyList<Vector2> ring, Vector2 gate, float height, float gateWidth, bool locked)
    {
        var gateEdge = SportsLayout.Edges(ring).OrderBy(e => PlanLines.SegmentDistanceSquared(gate, e.A, e.B)).First();
        gate = PlanLines.Project([gateEdge.A, gateEdge.B], gate).Point;
        foreach (var (a, b) in SportsLayout.Edges(ring))
        {
            if (a != gateEdge.A || b != gateEdge.B) { yield return (a, b, 0, height, false); continue; }
            var along = Vector2.Normalize(b - a);
            var left = gate - along * (gateWidth * 0.5f);
            var right = gate + along * (gateWidth * 0.5f);
            yield return (a, left, 0, height, false);
            yield return (right, b, 0, height, false);
            if (height > S.GateHeight) yield return (left, right, S.GateHeight, height, false);
            if (locked) { yield return (left, right, 0, S.GateHeight, true); continue; }
            // An open leaf lies back along the fence, leaving the full aperture walkable.
            yield return (left - along * gateWidth, left, 0, S.GateHeight, false);
        }
    }

    internal static IEnumerable<(Vector2 Position, float Height)> Lamps(IWorldModel world)
    {
        var unique = new HashSet<Vector2>();
        foreach (var feature in Areas(world))
        {
            var r = feature.Rectangle(SportsLayout.Axis(feature));
            var track = feature.Id.Value == CenterEastIds.Sports.Track.Entity;
            foreach (var p in new[] {r.Frame.P(-r.Width * 0.5f, -r.Depth * 0.5f), r.Frame.P(r.Width * 0.5f, -r.Depth * 0.5f),
                r.Frame.P(-r.Width * 0.5f, r.Depth * 0.5f), r.Frame.P(r.Width * 0.5f, r.Depth * 0.5f)})
            {
                // Adjacent courts share their corner standards, instead of overlapping poles.
                var point = new Vector2(MathF.Round(p.X, 1), MathF.Round(p.Y, 1));
                if (unique.Add(point)) yield return (point, track ? S.TrackLight : S.CourtLight);
            }
        }
    }

    public override void Build(SiteBuilder b)
    {
        foreach (var feature in Areas(b.Context.World).Where(feature => b.Includes(AreaRange(feature))))
        {
            if (feature.Id.Value != CenterEastIds.Sports.Track.Entity)
            {
                var court = feature.Rectangle(SportsLayout.Axis(feature));
                var y = b.Ground(court.Frame.Origin) + GroundLevels.Paving;
                var tennis = feature.Props.TryText("sport") == "tennis";
                var practice = feature.OptionalNumber("practiceWallHeight") is not null;
                b.Add(CourtEquipment.Surface(court.Width, court.Depth, tennis, practice), court.Frame.At(0, y + S.Bevel, 0));
                if (b.Detail != SiteDetail.Mass)
                {
                    if (tennis && !practice) b.Add(CourtEquipment.Net(court.Depth * 0.8f, b.Detail == SiteDetail.Full), court.Frame.At(0, y, 0));
                    if (!tennis)
                    {
                        var baseline = MathF.Min(court.Width * 0.45f, S.BasketballLength * 0.5f);
                        b.Add(CourtEquipment.Hoop(b.Detail == SiteDetail.Full), court.Frame.At(-baseline, y, 0));
                        b.Add(CourtEquipment.Hoop(b.Detail == SiteDetail.Full), court.Frame.AtReversed(baseline, y, 0));
                    }
                }
            }
            foreach (var (a, end, bottom, top, _) in FenceRuns(feature))
            {
                var length = Vector2.Distance(a, end);
                if (length <= S.Post) continue;
                b.Add(SportsParts.Fence(length, top - bottom, b.Detail == SiteDetail.Full), PlanFrame.Toward(a, end).At(0, b.Ground(a) + bottom, 0));
            }
            if (feature.OptionalNumber("practiceWallHeight") is { } wall)
            {
                var r = feature.Rectangle(SportsLayout.Axis(feature));
                var side = r.Frame.W.Y >= 0 ? -1 : 1;
                var a = r.Frame.P(-r.Width * 0.5f, side * r.Depth * 0.5f);
                var end = r.Frame.P(r.Width * 0.5f, side * r.Depth * 0.5f);
                b.Add(Campus.Gen.Parts.Core.PartGeometryKernel.BeveledBox(r.Width, wall, S.Slab, S.Bevel, S.Green), PlanFrame.Toward(a, end).At(r.Width * 0.5f, b.Ground(a), 0));
            }
        }
        if (b.Lod is LodLevel.L3 or LodLevel.L4) return;
        foreach (var (p, height) in Lamps(b.Context.World).Where(lamp => b.Includes(SiteRanges.Point(lamp.Position, S.Cover + StreetStyle.ArmHeadLength))))
        {
            var y = b.Ground(p);
            b.Add(SportsParts.Floodlight(height, b.Detail == SiteDetail.Full), Matrix4x4.CreateTranslation(p.X, y, p.Y));
            if (b.Lod == LodLevel.L0) b.Light(new(new("lamp.court"), new(p.X, y + height, p.Y), new("lamp.cool"), StreetStyle.GradeTwoEnergy, StreetStyle.GradeOneRange));
        }
    }

    public override void Collide(SiteColliders c)
    {
        foreach (var feature in Areas(c.Context.World).Where(feature => c.Includes(AreaRange(feature))))
        {
            var court = feature.Rectangle(SportsLayout.Axis(feature));
            if (feature.Props.TryText("sport") == "tennis" && feature.OptionalNumber("practiceWallHeight") is null)
            {
                var a = court.Frame.P(0, -court.Depth * 0.4f); var b = court.Frame.P(0, court.Depth * 0.4f);
                c.Wall(a, b, S.Wire, c.Ground(a), c.Ground(a) + GroundLevels.Paving + S.TennisNet, SurfaceTag.Prop, FootMat.Metal);
            }
            if (feature.Props.TryText("sport") == "basketball")
            {
                var baseline = MathF.Min(court.Width * 0.45f, S.BasketballLength * 0.5f);
                foreach (var side in new[] { -1f, 1f })
                {
                    var p = court.Frame.P(side * baseline, 0);
                    c.Add(new PostCollider(p, S.Post * 2, c.Ground(p), c.Ground(p) + GroundLevels.Paving + S.HoopHeight, SurfaceTag.Prop) { Foot = FootMat.Metal });
                }
            }
            foreach (var (a, b, bottom, top, _) in FenceRuns(feature))
                if (Vector2.Distance(a, b) > S.Post)
                {
                    var ground = SportsLayout.FenceGround(c.Context.World, a, b);
                    c.Wall(a, b, S.Post, ground.Low + bottom, ground.High + top, SurfaceTag.Prop, FootMat.Metal);
                }
            if (feature.OptionalNumber("practiceWallHeight") is { } wall)
            {
                var r = feature.Rectangle(SportsLayout.Axis(feature));
                var side = r.Frame.W.Y >= 0 ? -1 : 1;
                var a = r.Frame.P(-r.Width * 0.5f, side * r.Depth * 0.5f);
                var b = r.Frame.P(r.Width * 0.5f, side * r.Depth * 0.5f);
                c.Wall(a, b, S.Slab * 0.5f, c.Ground(a), c.Ground(a) + wall, SurfaceTag.Prop, FootMat.Concrete);
            }
        }
        foreach (var (p, height) in Lamps(c.Context.World).Where(lamp => c.Includes(SiteRanges.Point(lamp.Position, S.PoleRadius)))) c.Add(new PostCollider(p, S.PoleRadius, c.Ground(p), c.Ground(p) + height, SurfaceTag.Prop) { Foot = FootMat.Metal });
    }
}
