using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Circulation;
using Campus.Gen.Zones.West.Framework;
using L = Campus.Gen.Zones.West.WestTune.Look;
using T = Campus.Gen.Zones.West.WestTune.LeveeSteps;

namespace Campus.Gen.Zones.West.Wetland;

/// <summary>
/// 渠堤台阶 (19a 接续, MAP-QSH-TERRAIN §6.3): stone garden steps down the levee's east flank where the stone-bridge footway
/// and the lake-mouth path leave the +0.9 levee top. Each flight lies along its path, spans the flank from the crest edge to
/// the slope foot, and takes its rise from the terrain at those two points, so it fits whatever WP-08's evaluator makes of
/// west.terrain.json; on flat ground (the stub) there is no rise and no flight. Cheek walls with copings and side
/// handrails (StepsPart variation 0), granite like the zone's other aprons, StonePaving underfoot.
/// </summary>
public sealed class LeveeStepsSite : WestSite
{
    private static readonly StepsPart Steps = new();

    public override string Id => "19c.levee-steps";

    /// <summary>One junction's flight: its frame (origin at the slope foot on the path, +Z up the path toward the levee), foot level and part.</summary>
    internal static (PlanFrame Frame, float Foot, StairParams Flight)? Flight((string Key, Vector2 Top, Vector2 Toward) j, Func<float, float, float> ground)
    {
        var d = Vector2.Normalize(j.Toward - j.Top);
        // The flank lies between CrestHalf and CrestHalf + rise × SideRun off the levee centreline, measured square to it;
        // along a path that meets the levee at an angle those distances grow by 1 / sin. The levee's local direction comes
        // from the centreline's own neighbours (west.terrain.json), so a skew path still lands on the flank.
        var across = MathF.Max(0.5f, Square(j.Top, d));
        var crest = T.CrestHalf / across;
        var run = T.LeveeRise * T.SideRun / across;
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
    private static float Square(Vector2 top, Vector2 d)
    {
        var line = Levee;
        var best = 0;
        for (var i = 1; i < line.Length; i++)
            if (Vector2.DistanceSquared(line[i], top) < Vector2.DistanceSquared(line[best], top)) best = i;
        var a = line[Math.Max(0, best - 1)];
        var b = line[Math.Min(line.Length - 1, best + 1)];
        var t = Vector2.Normalize(b - a);
        return MathF.Abs(t.X * d.Y - t.Y * d.X);
    }

    /// <summary>The levee centreline near the two junctions (west.terrain.json design:terrain:west-levee, first points).</summary>
    private static readonly Vector2[] Levee =
        [new(-308.84f, -147.52f), new(-307.28f, -150.63f), new(-303.98f, -154.91f), new(-296.28f, -164.94f), new(-293.69f, -168.33f), new(-281.59f, -184.05f)];

    public override void Build(SiteBuilder b)
    {
        foreach (var j in T.Junctions)
            if (Flight(j, b.Ground) is { } f)
                b.Place(PartCatalog.Steps, f.Flight, f.Frame.At(0, f.Foot, f.Flight.Depth * 0.5f));
    }

    public override void Collide(SiteColliders c)
    {
        foreach (var j in T.Junctions)
            if (Flight(j, c.Ground) is { } f)
                c.Add(Steps.Collision(f.Flight), f.Frame.At(0, f.Foot, f.Flight.Depth * 0.5f));
    }
}
