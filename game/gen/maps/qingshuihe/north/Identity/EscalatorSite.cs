using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Circulation;
using Campus.Gen.Parts.Core;
using Campus.Gen.Sites;

namespace Campus.Gen.Maps.Qingshuihe.North.Identity;

/// <summary>One registered 30-degree escalator, level landings and open-ended glazed cover.</summary>
public sealed class EscalatorSite(string recordId) : ZoneSite
{
    private static readonly FoldingGatePart Gate = new();
    public override string Id => "20c." + recordId;

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world)
    {
        var record = world.Feature(recordId);
        return [SiteRanges.Bounds(record, MathF.Max(record.Number("coverSpan"), new StairParams().Width) * .5f + new RailingParams().Depth)];
    }

    internal sealed record Layout(PlanFrame Frame, float Length, float Base, float Rise, float Landing,
        float Width, WorldFeature Record)
    {
        internal float Run => Length - Landing * 2;
        internal float Slope => Rise / Run;
        internal Vector3 Point(float x, float z, float lift = 0)
        {
            var p = Frame.P(x, z);
            return new(p.X, Base + Math.Clamp((z - Landing) / Run, 0, 1) * Rise + lift, p.Y);
        }
    }

    internal static Layout Locate(IWorldModel world, string id)
    {
        var record = world.Feature(id); var (start, end) = record.PointPair();
        var axis = Vector2.Normalize(end - start);
        var frame = new PlanFrame(start, new(axis.Y, -axis.X));
        var length = Vector2.Distance(start, end);
        var floor = world.Terrain.PadY(new(record.Text("building"))) + record.Number("floorBase");
        var building = new FeatureId(record.Text("building"));
        var rise = record.Number("open") != 0
            ? (world.Interiors.Get(building)?.Entrances.Single(e => e.Id == record.Text("entrance"))
                ?? throw new InvalidOperationException($"{id}: the receiving entrance is missing.")).Center.Y - floor
            : record.Number("rise");
        // Both fixed endpoints and the receiving floor are authoritative. The platforms share the remaining run.
        var run = rise / (float)Math.Tan(record.Number("slopeDeg") * Math.PI / 180);
        var landing = (length - run) * .5f;
        if (landing < record.Number("minLanding"))
            throw new InvalidOperationException($"{id}: fixed door and 30-degree flight leave less than the minimum landing.");
        return new(frame, length, floor, rise, landing, new StairParams().Width, record);
    }

    public override void Build(SiteBuilder b)
    {
        var p = Locate(b.Context.World, recordId); var f = p.Frame;
        var half = p.Length * .5f; var start = p.Landing - half; var end = half - p.Landing;
        var thick = CirculationStyle.LandingThickness;
        Vector2[] belt = [new(-half, -thick), new(-half, 0), new(start, 0), new(end, p.Rise),
            new(half, p.Rise), new(half, p.Rise - thick), new(end, p.Rise - thick), new(start, -thick)];
        b.Add(PartSolids.WallPanel(new(belt), p.Width, NorthTune.Look.Steel), f.AtTurned(0, p.Base, half));
        Belt(b, p);
        var rail = new RailingParams();
        Vector2[] panel = [new(-half, 0), new(start, 0), new(end, p.Rise), new(half, p.Rise),
            new(half, p.Rise + rail.Height), new(end, p.Rise + rail.Height), new(start, rail.Height), new(-half, rail.Height)];
        foreach (var side in new[] { -1f, 1f })
        {
            b.Add(PartSolids.WallPanel(new(panel), rail.Depth, NorthTune.Look.Glass),
                f.AtTurned(side * p.Width * .5f, p.Base, half));
            if (b.Detail == SiteDetail.Mass) continue;
            var builder = new PartBuilder();
            var stations = new[] { 0f, p.Landing, p.Length - p.Landing, p.Length };
            for (var i = 0; i + 1 < stations.Length; i++)
                builder.AddPipe(p.Point(side * p.Width * .5f, stations[i], rail.Height),
                    p.Point(side * p.Width * .5f, stations[i + 1], rail.Height), rail.Depth * .5f,
                    8, NorthTune.Look.Steel);
            var bounds = builder.Measure();
            b.Add(builder.Finish(new(bounds.Min, bounds.Max)), Matrix4x4.Identity);
        }
        Cover(b, p, 0, p.Landing, 0, 0);
        Cover(b, p, p.Landing, p.Run, 0, p.Rise);
        Cover(b, p, p.Length - p.Landing, p.Landing, p.Rise, p.Rise);
        if (p.Record.Number("open") == 0)
            b.Add(Gate.Lods[b.Lod](GateParams(p.Width)), f.At(0, p.Base, 0));
    }

    private static DoorParams GateParams(float width) => new(width, WalkRules.DefaultDoorHeight, new DoorParams().Depth)
        { Material = NorthTune.Look.Stainless, Foot = FootMat.Metal };

    private static void Belt(SiteBuilder b, Layout p)
    {
        var material = NorthTune.Look.Stainless with { Fx = NorthTune.Look.Stainless.Fx | ToonFx.Escalator, Line = LineClass.None };
        var stations = new[] { 0f, p.Landing, p.Length - p.Landing, p.Length };
        var v = 0f;
        for (var i = 0; i + 1 < stations.Length; i++)
        {
            var a = p.Point(-p.Width * .5f, stations[i], CirculationStyle.FaceInset);
            var z = p.Point(-p.Width * .5f, stations[i + 1], CirculationStyle.FaceInset);
            var run = z - a;
            var normal = Vector3.Normalize(Vector3.Cross(run, new(p.Frame.U.X, 0, p.Frame.U.Y)));
            var side = new Vector3(p.Frame.U.X * p.Width, 0, p.Frame.U.Y * p.Width);
            var geometry = PartShapes.FlatQuad(a, a + side, z + side, z, normal, material);
            var end = v + run.Length();
            foreach (var mesh in geometry.Meshes)
                b.Add(mesh with { Uv = [0, v, p.Width, v, p.Width, end, 0, end] }, Matrix4x4.Identity);
            v = end;
        }
    }

    private static void Cover(SiteBuilder b, Layout p, float z, float length, float y0, float y1)
    {
        var record = p.Record;
        var cover = new ArchedCanopyParams(record.Number("coverSpan"), record.Number("coverStraight"),
            record.Number("coverRise"), length, record.Number("ribPitch"), NorthTune.Look.Glass, NorthTune.Look.Steel)
            { Grade = (y1 - y0) / length };
        b.Add(ArchedCanopy.Build(cover, b.Lod), p.Frame.At(0, p.Base + (y0 + y1) * .5f, z + length * .5f));
    }

    public override void Collide(SiteColliders c)
    {
        var p = Locate(c.Context.World, recordId);
        var half = p.Width * .5f;
        c.Add(new RampCollider(p.Point(-half, p.Landing), p.Point(half, p.Landing),
            p.Point(half, p.Length - p.Landing), p.Point(-half, p.Length - p.Landing),
            CirculationStyle.StepColliderThickness, SurfaceTag.Ground) { Foot = FootMat.Metal });
        foreach (var z in new[] { p.Landing * .5f, p.Length - p.Landing * .5f })
            c.Box(p.Frame.At(0, p.Point(0, z).Y - CirculationStyle.LandingThickness * .5f, z),
                new(half, CirculationStyle.LandingThickness * .5f, p.Landing * .5f), SurfaceTag.Ground, FootMat.Metal);
        foreach (var side in new[] { -1f, 1f })
        {
            var stations = new[] { 0f, p.Landing, p.Length - p.Landing, p.Length };
            for (var i = 0; i + 1 < stations.Length; i++)
            {
                var a = p.Point(side * half, stations[i]); var z = p.Point(side * half, stations[i + 1]);
                c.Add(new SlopedWallCollider(new(a.X, a.Z), new(z.X, z.Z), new RailingParams().Depth * .5f,
                    a.Y - CirculationStyle.Sink, a.Y + WalkRules.BarrierHeight,
                    z.Y - CirculationStyle.Sink, z.Y + WalkRules.BarrierHeight, SurfaceTag.Barrier) { Foot = FootMat.Metal });
            }
        }
        if (p.Record.Number("open") == 0) c.Add(Gate.Collision(GateParams(p.Width)), p.Frame.At(0, p.Base, 0));
    }
}
