using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Circulation;
using Campus.Gen.Sites;
using L = Campus.Gen.Maps.Qingshuihe.West.WestTune.Look;
using T = Campus.Gen.Maps.Qingshuihe.West.WestTune.LeveeSteps;

namespace Campus.Gen.Maps.Qingshuihe.West.Wetland;

/// <summary>
/// 渠堤台阶 (19a 接续, MAP-QSH-TERRAIN): stone garden steps down the levee's east flank where the stone-bridge footway
/// and the lake-mouth path leave the +0.9 levee top. Each flight lies along its path, spans the flank from the crest edge to
/// the slope foot, and takes its rise from the terrain at those two points, so it fits whatever game/gen/world/'s evaluator makes of
/// west.terrain.json; on flat ground (the stub) there is no rise and no flight. Cheek walls with copings and side
/// handrails (StepsPart variation 0), granite like the zone's other aprons, StonePaving underfoot.
/// </summary>
public sealed class LeveeStepsSite : ZoneSite
{
    private static readonly StepsPart Steps = new();

    public override string Id => "19c.levee-steps";

    private static Campus.Contracts.Maps.MapRect JunctionRange(IWorldModel world, (string Key, Vector2 Top, Vector2 Toward) junction)
    {
        var direction = Vector2.Normalize(junction.Toward - junction.Top);
        var across = MathF.Max(0.5f, Square(world.TerrainLine(WestIds.Levee), junction.Top, direction));
        var run = T.LeveeRise(world) * T.SideRun(world) / across;
        var foot = junction.Top + direction * (T.CrestHalf(world) / across + run);
        return SiteRanges.Bounds([junction.Top, junction.Toward, foot], T.Width);
    }

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        [.. T.Junctions(world).Select(junction => JunctionRange(world, junction))];

    /// <summary>One junction's flight: its frame (origin at the slope foot on the path, +Z up the path toward the levee), foot level and part.</summary>
    internal static (PlanFrame Frame, float Foot, StairParams Flight)? Flight(IWorldModel world, (string Key, Vector2 Top, Vector2 Toward) j, Func<float, float, float> ground)
    {
        var d = Vector2.Normalize(j.Toward - j.Top);
        // The flank lies between CrestHalf and CrestHalf + rise × SideRun off the levee centreline, measured square to it;
        // along a path that meets the levee at an angle those distances grow by 1 / sin. The levee's local direction comes
        // from the centreline's own neighbours (west.terrain.json), so a skew path still lands on the flank.
        var across = MathF.Max(0.5f, Square(world.TerrainLine(WestIds.Levee), j.Top, d));
        var crest = T.CrestHalf(world) / across;
        var run = T.LeveeRise(world) * T.SideRun(world) / across;
        var top = j.Top + d * crest;
        var foot = j.Top + d * (crest + run);
        float yTop = ground(top.X, top.Y), yFoot = ground(foot.X, foot.Y);
        var rise = yTop - yFoot;
        if (rise < T.MinRise) return null;
        var n = Math.Max(2, (int)MathF.Round(rise / T.Riser));
        var tread = Math.Clamp(run / n, 0.28f, 0.55f);
        var frame = new PlanFrame(foot, new(-d.Y, d.X));
        return (frame, yFoot, new StairParams(T.Width, rise, tread * n, n) { Variation = 0, Material = L.Granite, Foot = FootMat.StonePaving });
    }

    /// <summary>|sin| of the angle between the path and the levee centreline at the junction (1 when square).</summary>
    private static float Square(IReadOnlyList<Vector2> line, Vector2 top, Vector2 d)
    {
        var best = 0;
        for (var i = 1; i < line.Count; i++)
            if (Vector2.DistanceSquared(line[i], top) < Vector2.DistanceSquared(line[best], top)) best = i;
        var a = line[Math.Max(0, best - 1)];
        var b = line[Math.Min(line.Count - 1, best + 1)];
        var t = Vector2.Normalize(b - a);
        return MathF.Abs(PlanRings.CrossFloat(t, d));
    }

    public override void Build(SiteBuilder b)
    {
        foreach (var j in T.Junctions(b.Context.World))
            if (b.Includes(JunctionRange(b.Context.World, j)) && Flight(b.Context.World, j, b.Ground) is { } f)
                b.Place(PartCatalog.Steps, f.Flight, f.Frame.At(0, f.Foot, f.Flight.Depth * 0.5f));
    }

    public override void Collide(SiteColliders c)
    {
        foreach (var j in T.Junctions(c.Context.World))
            if (c.Includes(JunctionRange(c.Context.World, j)) && Flight(c.Context.World, j, c.Ground) is { } f)
                c.Add(Steps.Collision(f.Flight), f.Frame.At(0, f.Foot, f.Flight.Depth * 0.5f));
    }
}
