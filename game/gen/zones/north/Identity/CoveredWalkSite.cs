using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Circulation;
using Campus.Gen.Parts.Street;
using Campus.Gen.Zones.Common;

namespace Campus.Gen.Zones.North.Identity;

/// <summary>The authored sheltered walking line from 学知一组团 to 银杏苑.</summary>
public sealed class CoveredWalkSite : ZoneSite
{
    public override string Id => "20c.covered-walk";

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world)
    {
        var record = world.Feature(NorthFeatureIds.CoveredWalk);
        var line = Route(record);
        var half = record.Number("width") * .5f;
        return [SiteRanges.Bounds(PlanGeometry.MitreOffset(line, half, false)
            .Concat(PlanGeometry.MitreOffset(line, -half, false)).Concat(line), record.Number("post") * MathF.Sqrt(2))];
    }

    private static IReadOnlyList<Vector2> Route(WorldFeature record)
    {
        var input = record.Line();
        var result = new List<Vector2> { input[0] };
        for (var i = 1; i + 1 < input.Count; i++)
        {
            var before = input[i] - input[i - 1]; var after = input[i + 1] - input[i];
            var incoming = Vector2.Normalize(before); var outgoing = Vector2.Normalize(after);
            var angle = MathF.Acos(Math.Clamp(Vector2.Dot(incoming, outgoing), -1, 1));
            if (angle < CirculationStyle.Joint) { result.Add(input[i]); continue; }
            var trim = MathF.Min(record.Number("width"), MathF.Min(before.Length(), after.Length()) * .45f);
            var a = input[i] - incoming * trim; var z = input[i] + outgoing * trim;
            var chord = z - a; var along = Vector2.Normalize(chord);
            var towardCorner = Vector2.Normalize(input[i] - (a + z) * .5f);
            var radius = trim / MathF.Tan(angle * .5f);
            var rise = radius * (1 - MathF.Cos(angle * .5f));
            foreach (var point in ArcSection.Sample(chord.Length(), rise, 8))
                result.Add((a + z) * .5f + along * point.X + towardCorner * point.Y);
        }
        result.Add(input[^1]);
        return result;
    }

    public override void Build(SiteBuilder b)
    {
        var record = b.Context.World.Feature(NorthFeatureIds.CoveredWalk);
        var line = Route(record);
        var width = record.Number("width");
        var clear = record.Number("clear");
        var roof = new RoofParams(); var post = record.Number("post");
        var steel = ZoneLooks.S(record.Text("steel"));
        var top = ZoneLooks.S(record.Text("roof"));
        var pitch = record.Number("postPitch");
        var right = PlanGeometry.MitreOffset(line, width * .5f, false);
        var left = PlanGeometry.MitreOffset(line, -width * .5f, false);
        for (var i = 0; i + 1 < line.Count; i++)
        {
            if (!b.Includes(SiteRanges.Bounds([left[i], left[i + 1], right[i], right[i + 1]], post))) continue;
            var a = line[i]; var z = line[i + 1];
            var length = Vector2.Distance(a, z);
            var frame = PlanFrame.Toward((a + z) * .5f, z);
            var ya = b.Ground(a) + clear; var yz = b.Ground(z) + clear;
            var gradient = (z - a) * ((yz - ya) / (length * length));
            var offset = ya - Vector2.Dot(gradient, a);
            var prism = PartGeometryKernel.Extrude(new([left[i], left[i + 1], right[i + 1], right[i]]), roof.Height, top);
            b.Add(ArchedCanopy.WithGrade(prism, gradient, offset), Matrix4x4.Identity);
            var lens = ZoneLooks.S(StreetStyle.Diffuser, line: LineClass.None) with { Emissive = new("lamp.first") };
            var half = length * .5f;
            foreach (var side in new[] { -1f, 1f })
            {
                Vector2[] strip = [new(-half, ya - CirculationStyle.TopRailHeight), new(half, yz - CirculationStyle.TopRailHeight),
                    new(half, yz), new(-half, ya)];
                b.Add(PartGeometryKernel.WallPanel(new(strip), CirculationStyle.TopRailHeight, lens),
                    frame.At(0, 0, side * (width - CirculationStyle.TopRailHeight) * .5f));
            }
        }
        var total = PlanLines.Length(line);
        var count = Math.Max(1, (int)Math.Ceiling(total / pitch));
        for (var k = 0; k <= count; k++)
        {
            var sample = PlanLines.Sample(line, total * k / count);
            var frame = PlanFrame.Along(sample.Point, sample.Tangent);
            foreach (var sign in new[] { -1f, 1f })
            {
                var side = sign * (width - post) * .5f;
                var p = frame.P(0, side);
                if (!b.Includes(SiteRanges.Point(p, post * 2))) continue;
                var ground = b.Ground(p);
                var y = b.Ground(sample.Point) + clear;
                NorthIdentityGeometry.Box(b, frame, 0, ground, side, post, y - ground, post, steel);
                if (b.Detail == SiteDetail.Full)
                    NorthIdentityGeometry.Box(b, frame, 0, ground, side, post * 2, post * .25f, post * 2, steel);
            }
        }
    }

    public override void Collide(SiteColliders c)
    {
        var record = c.Context.World.Feature(NorthFeatureIds.CoveredWalk);
        var line = Route(record);
        var width = record.Number("width"); var post = record.Number("post");
        var total = PlanLines.Length(line);
        var count = Math.Max(1, (int)Math.Ceiling(total / record.Number("postPitch")));
        for (var k = 0; k <= count; k++)
        {
            var sample = PlanLines.Sample(line, total * k / count);
            var frame = PlanFrame.Along(sample.Point, sample.Tangent);
            foreach (var side in new[] { -1f, 1f })
            {
                var p = frame.P(0, side * (width - post) * .5f);
                if (!c.Includes(SiteRanges.Point(p, post))) continue;
                var y = c.Ground(p);
                c.Box(frame.At(0, y + record.Number("clear") * .5f,
                        side * (width - post) * .5f), new(post * .5f, record.Number("clear") * .5f, post * .5f),
                    SurfaceTag.Prop, FootMat.Metal);
            }
        }
    }
}
