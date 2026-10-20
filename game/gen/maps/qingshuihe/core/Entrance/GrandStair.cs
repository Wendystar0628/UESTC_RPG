using Campus.Gen.Sites;
using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Circulation;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Openings;
using Campus.Gen.Maps.Qingshuihe.Core.Framework;
using Campus.Gen.Maps.Qingshuihe.Core.Library;
using Campus.Gen.World;
using S = Campus.Gen.Maps.Qingshuihe.Core.CoreStyle;

namespace Campus.Gen.Maps.Qingshuihe.Core.Entrance;

/// <summary>One straight flight, climbing toward the library: its foot edge at <see cref="Foot"/> (s), base level, risers.</summary>
public readonly record struct StairFlight(float Foot, float BaseY, int Risers)
{
    public float Top(float tread) => Foot - Risers * tread;
}

/// <summary>A level landing between two flights, from <see cref="S0"/> (library side) to <see cref="S1"/> (plaza side).</summary>
public readonly record struct StairLanding(float S0, float S1, float Y, bool Middle);

/// <summary>
/// 大台阶与二楼正门 (16c, MAP-QSH-CORE; 8398): the grand stair from the axis paving up to the 2F terrace and the
/// front door in the tower's glass slot. Across the door axis, symmetric: the two-run main stair in the middle, a lawn
/// terrace (草坪台地) on each side of it, a side stair (侧梯) on each flank and a solid stone guard wall outside each side
/// stair; behind them the top landing joins the podium terrace (16b leaves the podium parapet open there).
/// <para>
/// Section (WLD-TERRAIN): risers near 0.15 m, tread clamp(0.65 − 2R, 0.30, 0.40), flights of at most 1.5 m
/// rise. The two runs (两跑) are split by one deep middle landing; inside each run a short rest landing. The terraces step
/// with the stair: the lower tier at the middle landing's level, the upper tier at the top, each ringed by a clipped hedge
/// (the lower tier's front band is the dark purple-red loropetalum, 红花檵木). Flights are WP-11's catalogue steps
/// (cheek walls, copings, side and middle handrails); everything else is the stair building's own stone.
/// </para>
/// <para>
/// Frame: s runs from the podium edge (0) outward, square to it, toward the plaza; w runs across, 0 on the door's axis.
/// Heights: the top is the library's 2F floor (<see cref="LibraryPlan.SecondFloorY"/>), the foot the terrain at the stair
/// foot plus the paving lift, so the flat stub and WP-08's terrain both work without code changes.
/// </para>
/// </summary>
public sealed class GrandStair : ZoneSite
{
    private static readonly StepsPart Steps = new();
    private readonly IWorldModel _world;
    private readonly (Vector3 Origin, Vector3 Outward, float Width, float Height, Face Face, float Depth)? _door;

    private GrandStair(IWorldModel world, LibraryShell shell, Vector2 origin, Vector2 outward)
    {
        _world = world;
        _door = shell.EntranceDoor;
        Origin = origin;
        Out = outward;
        Across = new(-outward.Y, outward.X);
        TopY = shell.Plan.SecondFloorY;
        // The foot level: the axis paving about one 5.1 m run (≈ 17 m) out from the top landing.
        var guess = Origin + Out * (S.TopLandingDepth + 17f);
        FootY = world.Terrain.HeightAt(guess.X, guess.Y) + S.StairFootLift;
        var rise = TopY - FootY;
        Risers = TerrainDrop.RiserCount(rise);
        Riser = rise / Risers;
        Tread = TerrainDrop.TreadFor(Riser);
        var count = Math.Max(1, (int)MathF.Ceiling(rise / WalkRules.OutdoorFlightMaxRise - 1e-4f));
        while ((Risers + count - 1) / count * Riser > WalkRules.OutdoorFlightMaxRise + 1e-4f) count++;
        // Bottom-up risers (the extra ones low), then laid out from the top landing outward.
        var risers = Enumerable.Range(0, count).Select(f => Risers / count + (f < Risers % count ? 1 : 0)).ToArray();
        var landings = new float[count - 1];
        for (var f = 0; f + 1 < count; f++) landings[f] = f + 1 == count / 2 ? S.MiddleLanding : S.RestLanding;
        var run = risers.Sum() * Tread + landings.Sum();
        FrontS = S.TopLandingDepth + run;
        var flights = new List<StairFlight>();
        var stops = new List<StairLanding>();
        float s = FrontS, y = FootY;
        for (var f = 0; f < count; f++)
        {
            flights.Add(new(s, y, risers[f]));
            s -= risers[f] * Tread; y += risers[f] * Riser;
            if (f + 1 == count) break;
            stops.Add(new(s - landings[f], s, y, f + 1 == count / 2));
            s -= landings[f];
        }
        Flights = flights;
        Landings = stops;
    }

    /// <summary>The stair, or null without the stair base, the library shell or its front door bay.</summary>
    public static GrandStair? From(IWorldModel world, LibraryShell? shell)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (shell is null || !world.Features.ContainsKey(CoreIds.LibraryStair)) return null;
        if (shell.EntranceDoor is not { } door || shell.Plan.Landing.Count == 0) return null;
        // The stair stands square to the podium edge it lands on; its axis is the door's foot on that edge.
        var landing = world.ContourSegment(CoreIds.LibraryLanding);
        var a = landing[0]; var b = landing[^1];
        var along = Vector2.Normalize(b - a);
        var outward = new Vector2(along.Y, -along.X);
        if (Vector2.Dot(outward, new Vector2(door.Outward.X, door.Outward.Z)) < 0) outward = -outward;
        var doorPlan = new Vector2(door.Origin.X, door.Origin.Z);
        return new GrandStair(world, shell, a + along * Vector2.Dot(doorPlan - a, along), outward);
    }

    public override string Id => "library-entrance";
    public Vector2 Anchor => At(FrontS * 0.5f, 0f);
    public static IReadOnlyList<Claim> DeclaredClaims { get; } = [new FeatureClaim(CoreIds.LibraryStair, Aspects.Massing | Aspects.Facade | Aspects.Roof)];
    public override IReadOnlyList<Claim> Claims => DeclaredClaims;

    /// <summary>On the door axis at the podium edge.</summary>
    public Vector2 Origin { get; }
    /// <summary>Outward (toward the plaza) along the axis.</summary>
    public Vector2 Out { get; }
    /// <summary>Across the axis; with <see cref="Out"/> and up it frames the catalogue steps (their +X).</summary>
    public Vector2 Across { get; }
    public float TopY { get; }
    public float FootY { get; }
    public int Risers { get; }
    public float Riser { get; }
    public float Tread { get; }
    /// <summary>Distance of the stair foot (first riser) from the podium edge.</summary>
    public float FrontS { get; }
    /// <summary>Flights bottom-up.</summary>
    public IReadOnlyList<StairFlight> Flights { get; }
    /// <summary>Landings bottom-up.</summary>
    public IReadOnlyList<StairLanding> Landings { get; }

    public static float HalfWidth => S.MainStairWidth * 0.5f + S.TerraceWidth + S.SideStairWidth + S.GuardWallWidth;

    /// <summary>Plan point at (s, w).</summary>
    public Vector2 At(float s, float w) => Origin + Out * s + Across * w;

    private Vector3 At(float s, float w, float y) { var p = At(s, w); return new(p.X, y, p.Y); }

    private Vector3 Out3 => new(Out.X, 0, Out.Y);
    private Vector3 Across3 => new(Across.X, 0, Across.Y);

    /// <summary>The stairs side by side: (centre w, outer width) of the main stair and the two side stairs.</summary>
    public IEnumerable<(float W, float Width)> Lanes()
    {
        yield return (0f, S.MainStairWidth);
        var side = S.MainStairWidth * 0.5f + S.TerraceWidth + S.SideStairWidth * 0.5f;
        yield return (-side, S.SideStairWidth);
        yield return (side, S.SideStairWidth);
    }

    /// <summary>The terrace tiers: (s from, s to, top level); the lower tier is in front, at the middle landing's level.</summary>
    public IEnumerable<(float S0, float S1, float Y, bool Lower)> Tiers()
    {
        var middle = Landings.Where(l => l.Middle).Select(l => (StairLanding?)l).FirstOrDefault();
        if (middle is not { } m)
        {
            yield return (S.TopLandingDepth, FrontS, TopY, false);
            yield break;
        }
        yield return (S.TopLandingDepth, m.S0, TopY, false);
        yield return (m.S0, FrontS, m.Y, true);
    }

    /// <summary>Ground height under plan point (s, w) (the stair's pad on the flat stub).</summary>
    private float Ground(float s, float w) { var p = At(s, w); return _world.Terrain.HeightAt(p.X, p.Y); }

    /// <summary>Low line of the stair's solids: below the lowest ground under its footprint.</summary>
    private float Base => MathF.Min(MathF.Min(Ground(0, -HalfWidth), Ground(0, HalfWidth)), MathF.Min(Ground(FrontS, -HalfWidth), Ground(FrontS, HalfWidth))) - S.Sink;

    // ------------------------------------------------------------------ build

    public void Build(CoreBuild build, LodLevel lod)
    {
        if (lod == LodLevel.L4) return;
        var mesh = build.Mesh;
        TopLanding(mesh);
        foreach (var (w, width) in Lanes())
        {
            foreach (var flight in Flights)
                mesh.Add(Steps.Lods[lod](FlightParams(flight, width) with { Variation = lod == LodLevel.L0 ? 0 : 1 }), FlightAt(flight, w), PaintOf);
            foreach (var landing in Landings) Landing(mesh, landing, w, width, lod);
        }
        foreach (var side in new[] { -1f, 1f })
        {
            foreach (var tier in Tiers()) Terrace(mesh, tier, side, lod);
            Guard(mesh, side, lod);
        }
        if (lod <= LodLevel.L1) Door(build);
        if (lod <= LodLevel.L2)
        {
            ServiceDoors(build);
            Canopy(mesh, lod);
        }
    }

    /// <summary>Catalogue steps in the stair's granite, with the side and middle handrails (variation 0; beyond L0 the handrails drop).</summary>
    private StairParams FlightParams(StairFlight f, float width) =>
        new(width, f.Risers * Riser, f.Risers * Tread, f.Risers)
        { Variation = 0, Material = S.Spec(CoreMaterial.Stone, S.StairStone), Foot = FootMat.StonePaving };

    /// <summary>Steps climb toward their local +Z: here toward the library (−s), centred on the flight, base at its foot level.</summary>
    private Matrix4x4 FlightAt(StairFlight f, float w)
    {
        var centre = At(f.Foot - f.Risers * Tread * 0.5f, w);
        return CoreShapes.Facing(new(centre.X, f.BaseY, centre.Y), -Out);
    }

    /// <summary>Repaints the steps' surfaces: their stone in the stair granite, copings in the library trim, rails in aluminium.</summary>
    private static Paint PaintOf(ToonSpec spec) =>
        spec.Lit.Value.StartsWith("frame.alu", StringComparison.Ordinal) ? S.Metal("frame.alu")
        : spec == CirculationStyle.Trim ? S.Trim(S.StairTrim)
        : S.Stone(S.StairStone);

    private void TopLanding(GeometrySink mesh)
    {
        var paving = S.Roof(S.LibraryTerrace);
        var h = HalfWidth - S.GuardWallWidth;
        mesh.Quad(paving, At(0, -h, TopY), At(0, h, TopY), At(S.TopLandingDepth, h, TopY), At(S.TopLandingDepth, -h, TopY), Vector3.UnitY);
    }

    /// <summary>A landing across one stair: paving between the cheeks, and the cheeks carried level at the flights' top height.</summary>
    private void Landing(GeometrySink mesh, StairLanding l, float w, float width, LodLevel lod)
    {
        var stone = S.Stone(S.StairStone);
        float half = width * 0.5f, cheek = CirculationStyle.CheekWidth, inner = half - cheek;
        mesh.Quad(stone, At(l.S0, w - inner, l.Y), At(l.S0, w + inner, l.Y), At(l.S1, w + inner, l.Y), At(l.S1, w - inner, l.Y), Vector3.UnitY);
        var top = l.Y + CirculationStyle.CheekRise;
        foreach (var side in new[] { -1f, 1f })
        {
            float a = w + side * inner, b = w + side * half;
            Block(mesh, stone, l.S0, l.S1, MathF.Min(a, b), MathF.Max(a, b), l.Y - 0.3f, top, open: Faces.Top | (side < 0 ? Faces.PlusW : Faces.MinusW));
            if (lod <= LodLevel.L1)
                Block(mesh, S.Trim(S.StairTrim), l.S0 - 0.01f, l.S1 + 0.01f, MathF.Min(a, b) - CirculationStyle.CopingOverhang * (side < 0 ? 0 : 1),
                    MathF.Max(a, b) + CirculationStyle.CopingOverhang * (side < 0 ? 1 : 0), top, top + CirculationStyle.CopingThickness,
                    Faces.Top | Faces.MinusW | Faces.PlusW);
        }
    }

    [Flags]
    private enum Faces { None = 0, Top = 1, MinusS = 2, PlusS = 4, MinusW = 8, PlusW = 16, All = 31 }

    /// <summary>An axis-aligned block in (s, w, y) with the chosen faces.</summary>
    private void Block(GeometrySink mesh, in Paint paint, float s0, float s1, float w0, float w1, float y0, float y1, Faces open)
    {
        if ((open & Faces.Top) != 0) mesh.Quad(paint, At(s0, w0, y1), At(s0, w1, y1), At(s1, w1, y1), At(s1, w0, y1), Vector3.UnitY);
        if ((open & Faces.PlusS) != 0) mesh.Quad(paint, At(s1, w0, y0), At(s1, w1, y0), At(s1, w1, y1), At(s1, w0, y1), Out3);
        if ((open & Faces.MinusS) != 0) mesh.Quad(paint, At(s0, w0, y0), At(s0, w1, y0), At(s0, w1, y1), At(s0, w0, y1), -Out3);
        if ((open & Faces.PlusW) != 0) mesh.Quad(paint, At(s0, w1, y0), At(s1, w1, y0), At(s1, w1, y1), At(s0, w1, y1), Across3);
        if ((open & Faces.MinusW) != 0) mesh.Quad(paint, At(s0, w0, y0), At(s1, w0, y0), At(s1, w0, y1), At(s0, w0, y1), -Across3);
    }

    /// <summary>The terrace span across w on one side: from the main stair's edge to the side stair's.</summary>
    private static (float W0, float W1) TerraceSpan(float side)
    {
        float a = side * S.MainStairWidth * 0.5f, b = side * (S.MainStairWidth * 0.5f + S.TerraceWidth);
        return (MathF.Min(a, b), MathF.Max(a, b));
    }

    /// <summary>One tier of a lawn terrace: stone retaining faces, a coping round the open edges, the lawn and its hedge ring.</summary>
    private void Terrace(GeometrySink mesh, (float S0, float S1, float Y, bool Lower) tier, float side, LodLevel lod)
    {
        var (w0, w1) = TerraceSpan(side);
        var wall = S.Wall(S.StairWall);
        Block(mesh, wall, tier.S0, tier.S1, w0, w1, Base, tier.Y, Faces.PlusS | Faces.MinusW | Faces.PlusW);
        // The lawn top shares the hedges' foliage class, so each planted terrace is one batch (16d, L1 budget).
        mesh.Quad(S.Foliage(S.TerraceLawn), At(tier.S0, w0, tier.Y - S.LawnDrop), At(tier.S0, w1, tier.Y - S.LawnDrop),
            At(tier.S1, w1, tier.Y - S.LawnDrop), At(tier.S1, w0, tier.Y - S.LawnDrop), Vector3.UnitY);
        float c = S.TerraceCoping, p = S.TerraceCopingProject, band = 0.3f;
        var trim = S.Trim(S.StairTrim);
        // Coping on the front edge and down both sides (the back edge meets the upper tier's wall or the top landing).
        Block(mesh, trim, tier.S1 - band, tier.S1 + p, w0 - p, w1 + p, tier.Y - c, tier.Y, Faces.Top | Faces.PlusS | Faces.MinusW | Faces.PlusW);
        Block(mesh, trim, tier.S0, tier.S1 - band, w0 - p, w0 + band, tier.Y - c, tier.Y, Faces.Top | Faces.MinusW | Faces.PlusW);
        Block(mesh, trim, tier.S0, tier.S1 - band, w1 - band, w1 + p, tier.Y - c, tier.Y, Faces.Top | Faces.MinusW | Faces.PlusW);
        // Plinth band (勒脚) along the foot of the lower tier's front wall.
        if (tier.Lower)
            Block(mesh, trim, tier.S1 - 0.2f, tier.S1 + S.PlinthProject, w0 - S.PlinthProject, w1 + S.PlinthProject, Base,
                Ground(tier.S1, (w0 + w1) * 0.5f) + S.StairPlinth, Faces.Top | Faces.PlusS | Faces.MinusW | Faces.PlusW);
        // Hedges are near detail: L0 and L1 only (their colliders stay at every level).
        if (lod >= LodLevel.L2) return;
        var (rings, sides) = lod == LodLevel.L0 ? (4, 12) : (2, 8);
        foreach (var (a, b, family) in Hedges(tier, side))
            CoreShapes.Hedge(mesh, S.Foliage(family), a, b, tier.Y - S.LawnDrop, S.HedgeWidth, S.HedgeHeight, rings, sides);
    }

    /// <summary>Hedge lines of one tier: the front band and the two sides, set in from the coping.</summary>
    public IEnumerable<(Vector2 A, Vector2 B, string Family)> Hedges((float S0, float S1, float Y, bool Lower) tier, float side)
    {
        var (w0, w1) = TerraceSpan(side);
        float i = S.HedgeInset + 0.3f + S.HedgeWidth * 0.5f, back = tier.S0 + (tier.Lower ? S.HedgeWidth * 0.5f : 0.3f);
        var front = tier.S1 - i;
        yield return (At(front, w0 + i - S.HedgeWidth * 0.5f), At(front, w1 - i + S.HedgeWidth * 0.5f), tier.Lower ? S.LoropetalumBand : S.BoxHedge);
        yield return (At(back, w0 + i), At(front - S.HedgeWidth * 0.5f, w0 + i), S.BoxHedge);
        yield return (At(back, w1 - i), At(front - S.HedgeWidth * 0.5f, w1 - i), S.BoxHedge);
    }

    /// <summary>
    /// The guard line over one side stair, in (s, y): the walking line (nosings over the flights, level over the landings
    /// and the top landing) raised by <see cref="CoreStyle.GuardHeight"/>, from the podium edge to the stair foot.
    /// </summary>
    public IReadOnlyList<Vector2> GuardTop()
    {
        var line = new List<Vector2> { new(0, TopY + S.GuardHeight) };
        for (var f = Flights.Count - 1; f >= 0; f--)
        {
            var flight = Flights[f];
            line.Add(new(flight.Top(Tread) + Tread, flight.BaseY + flight.Risers * Riser + S.GuardHeight));
            line.Add(new(flight.Foot, flight.BaseY + S.GuardHeight));
        }
        return line;
    }

    /// <summary>The solid stone guard wall outside one side stair, with a coping following its top line.</summary>
    private void Guard(GeometrySink mesh, float side, LodLevel lod)
    {
        float w1 = side * HalfWidth, w0 = side * (HalfWidth - S.GuardWallWidth);
        var top = GuardTop();
        var section = new List<Vector2>(top) { new(FrontS, Base), new(0, Base) };
        var coping = lod <= LodLevel.L2 ? S.TerraceCoping : 0f;
        var body = coping > 0 ? [.. top.Select(p => p with { Y = p.Y - coping }), new(FrontS, Base), new(0, Base)] : section;
        Prism(mesh, S.Wall(S.StairWall), body, MathF.Min(w0, w1), MathF.Max(w0, w1));
        // Plinth band (勒脚) along the outer face and round the foot end, broken at the service door.
        var plinth = S.PlinthProject;
        float gap0 = S.ServiceDoorS - S.ServiceDoorWidth * 0.5f - 0.1f, gap1 = S.ServiceDoorS + S.ServiceDoorWidth * 0.5f + 0.1f;
        var outer = side < 0 ? Faces.MinusW : Faces.PlusW;
        foreach (var (a, b, faces) in new[] { (0f, gap0, Faces.Top | Faces.PlusS | outer), (gap1, FrontS + plinth, Faces.Top | Faces.PlusS | Faces.MinusS | outer) })
            Block(mesh, S.Trim(S.StairTrim), a, b, MathF.Min(w0, w1) - plinth, MathF.Max(w0, w1) + plinth, Base, Ground(FrontS, w1) + S.StairPlinth, faces);
        // String courses on the outer face, each running out to where the falling top line comes within 0.3 m of it.
        var face = side < 0 ? MathF.Min(w0, w1) : MathF.Max(w0, w1);
        foreach (var course in S.GuardCourses)
        {
            var y = Ground(0, w1) + course;
            var end = CourseEnd(top, y + 0.3f);
            if (end > 0.5f)
                Block(mesh, S.Trim(S.StairTrim), 0f, end, side < 0 ? face - S.CourseRelief : face - 0.01f, side < 0 ? face + 0.01f : face + S.CourseRelief,
                    y - S.CourseThickness * 0.5f, y + S.CourseThickness * 0.5f, Faces.Top | Faces.PlusS | (side < 0 ? Faces.MinusW : Faces.PlusW) | Faces.MinusS);
        }
        if (coping <= 0) return;
        var p = S.TerraceCopingProject;
        var strip = new List<Vector2>(top);
        for (var k = top.Count - 1; k >= 0; k--) strip.Add(top[k] with { Y = top[k].Y - coping });
        Prism(mesh, S.Trim(S.StairTrim), strip, MathF.Min(w0, w1) - p, MathF.Max(w0, w1) + p);
    }

    /// <summary>The s where the guard's top line (falling from the podium toward the foot) first drops to <paramref name="y"/>.</summary>
    private static float CourseEnd(IReadOnlyList<Vector2> top, float y)
    {
        for (var k = 0; k + 1 < top.Count; k++)
        {
            Vector2 a = top[k], b = top[k + 1];
            if (b.Y > y) continue;
            return a.Y <= y ? a.X : a.X + (b.X - a.X) * (a.Y - y) / (a.Y - b.Y);
        }
        return top[^1].X;
    }

    /// <summary>A prism over a simple section in (s, y) between w0 and w1: both side faces and every edge face.</summary>
    private void Prism(GeometrySink mesh, in Paint paint, IReadOnlyList<Vector2> section, float w0, float w1)
    {
        var ring = section;
        var sign = PlanGeometry.SignedArea(ring) >= 0 ? 1f : -1f;
        for (var i = 0; i < ring.Count; i++)
        {
            Vector2 a = ring[i], b = ring[(i + 1) % ring.Count];
            if ((b - a).LengthSquared() < 1e-8f) continue;
            var d = Vector2.Normalize(b - a);
            var n = new Vector2(d.Y, -d.X) * sign;
            var normal = VectorMath.Unit(Out3 * n.X + Vector3.UnitY * n.Y, Vector3.UnitY, 1e-8);
            mesh.Quad(paint, At(a.X, w0, a.Y), At(b.X, w0, b.Y), At(b.X, w1, b.Y), At(a.X, w1, a.Y), normal);
        }
        var polygon = PolygonTriangulator.Triangulate(new PolygonRings([.. ring]));
        for (var t = 0; t < polygon.Indices.Length; t += 3)
        {
            Vector2 a = polygon.Vertices[polygon.Indices[t]], b = polygon.Vertices[polygon.Indices[t + 1]], c = polygon.Vertices[polygon.Indices[t + 2]];
            mesh.Tri(paint, At(a.X, w0, a.Y), At(b.X, w0, b.Y), At(c.X, w0, c.Y), -Across3);
            mesh.Tri(paint, At(a.X, w1, a.Y), At(b.X, w1, b.Y), At(c.X, w1, c.Y), Across3);
        }
    }

    // ------------------------------------------------------------------ door and its presentation

    /// <summary>
    /// The front door (WLD-INTERIORS): WP-10's glass door over the slot's door bay, its two inner leaves fixed open at
    /// 90°. The entrance kit in front of it (mat, open sign, door lamp) is WP-13's, placed from the interior index.
    /// </summary>
    private void Door(CoreBuild build)
    {
        if (_door is not { } d) return;
        build.Parts.Add(new PlacedPart(new PartId(PartCatalog.DoorGlass),
            new DoorParams(d.Width, d.Height, S.DoorDepth, S.DoorOpenDeg, S.DoorLeaves) { Foot = FootMat.Glass },
            OpeningPlacement.Frame(d.Origin, d.Outward)));
    }

    /// <summary>
    /// The porch frame (门斗式外框, 16d) over the door: a flat canopy across the whole width of the front tower face on two
    /// side piers standing on the terrace, so the 2F door reads under the 25 m glass tower at mid range. Over the slot the
    /// canopy runs back into the recess to the glass (top and soffit only; the slot's reveals close its ends). L0 adds a
    /// metal drip strip under the fascia.
    /// </summary>
    private void Canopy(GeometrySink mesh, LodLevel lod)
    {
        if (_door is not { } d) return;
        var f = d.Face;
        float y0 = CanopySoffit(d.Origin.Y, d.Height), y1 = y0 + S.CanopyThick;
        var paint = S.Sheet(S.CanopyFamily);
        LibraryFacade.Box(mesh, paint, f, 0f, f.Length, 0f, S.CanopyDepth, y0, y1);
        foreach (var (a, b) in CanopyPiers(f.Length))
            LibraryFacade.Box(mesh, paint, f, a, b, 0f, S.CanopyDepth, d.Origin.Y - S.Sink, y0);
        var centre = Vector2.Dot(new Vector2(d.Origin.X, d.Origin.Z) - f.A, f.Along);
        float u0 = centre - S.SlotWidth * 0.5f, u1 = centre + S.SlotWidth * 0.5f, s = -d.Depth;
        mesh.Quad(paint, f.At(u0, s, y1), f.At(u1, s, y1), f.At(u1, 0f, y1), f.At(u0, 0f, y1), Vector3.UnitY);
        mesh.Quad(paint, f.At(u0, s, y0), f.At(u1, s, y0), f.At(u1, 0f, y0), f.At(u0, 0f, y0), -Vector3.UnitY);
        if (lod == LodLevel.L0)
            LibraryFacade.Box(mesh, S.Metal("frame.alu"), f, -0.01f, f.Length + 0.01f, S.CanopyDepth - S.CanopyDrip, S.CanopyDepth + 0.01f, y0 - S.CanopyDrip, y0 + 0.02f);
    }

    private static float CanopySoffit(float floor, float doorHeight) => floor + doorHeight + S.CanopyGap;

    /// <summary>The two piers' spans along the tower face, flush with its ends.</summary>
    private static (float U0, float U1)[] CanopyPiers(float length) => [(0f, S.CanopyPier), (length - S.CanopyPier, length)];

    /// <summary>The two closed service doors (检修铁门) at the foot of the outer guard walls, into the plant rooms under the stair.</summary>
    private void ServiceDoors(CoreBuild build)
    {
        foreach (var (at, p) in ServiceDoorPlacements())
            build.Parts.Add(new PlacedPart(new PartId(PartCatalog.DoorFire), p, at));
    }

    private IEnumerable<(Matrix4x4 At, DoorParams Params)> ServiceDoorPlacements()
    {
        foreach (var side in new[] { -1f, 1f })
        {
            var w = side * HalfWidth;
            var foot = At(S.ServiceDoorS, w);
            var origin = new Vector3(foot.X, Ground(S.ServiceDoorS, w), foot.Y);
            yield return (OpeningPlacement.Frame(origin, Across3 * side), new DoorParams(S.ServiceDoorWidth, S.ServiceDoorHeight, 0.08f) { Foot = FootMat.Metal });
        }
    }

    // ------------------------------------------------------------------ collision

    /// <summary>
    /// Flights from the catalogue part's own colliders; landings and the top landing as stone slabs; terrace tiers as
    /// boxes (lawn tops); every hedge and guard wall collided 1.5 m above its walking surface (SYS-WALK).
    /// </summary>
    public IReadOnlyList<Collider> Collide()
    {
        var list = new List<Collider>();
        foreach (var (w, width) in Lanes())
        {
            foreach (var flight in Flights)
                list.AddRange(GeometryPlacement.Colliders(Steps.Collision(FlightParams(flight, width)), FlightAt(flight, w)));
            foreach (var l in Landings)
                list.Add(Slab(l.S0, l.S1, w - width * 0.5f, w + width * 0.5f, l.Y, SurfaceTag.Stair, FootMat.StonePaving));
        }
        var h = HalfWidth - S.GuardWallWidth;
        list.Add(Slab(0, S.TopLandingDepth, -h, h, TopY, SurfaceTag.Ground, FootMat.StonePaving));
        foreach (var side in new[] { -1f, 1f })
        {
            var (w0, w1) = TerraceSpan(side);
            foreach (var tier in Tiers())
            {
                list.Add(Slab(tier.S0, tier.S1, w0, w1, tier.Y - S.LawnDrop, SurfaceTag.Ground, FootMat.Grass));
                foreach (var (a, b, _) in Hedges(tier, side))
                    list.Add(new WallCollider(a, b, S.HedgeWidth * 0.5f, tier.Y - S.LawnDrop - 0.1f, tier.Y + WalkRules.BarrierHeight, SurfaceTag.Barrier) { Foot = FootMat.Hedge });
            }
            var top = GuardTop();
            var wc = side * (HalfWidth - S.GuardWallWidth * 0.5f);
            for (var k = 0; k + 1 < top.Count; k++)
            {
                var high = MathF.Max(top[k].Y, top[k + 1].Y) - S.GuardHeight + WalkRules.BarrierHeight;
                list.Add(new WallCollider(At(top[k].X, wc), At(top[k + 1].X, wc), S.GuardWallWidth * 0.5f, Base, high, SurfaceTag.Wall) { Foot = FootMat.StonePaving });
            }
        }
        var service = new FireDoorPart();
        foreach (var (at, p) in ServiceDoorPlacements())
            list.AddRange(GeometryPlacement.Colliders(service.Collision(p), at));
        // The porch frame's two piers on the terrace (the canopy is out of reach).
        if (_door is { } d)
            foreach (var (u0, u1) in CanopyPiers(d.Face.Length))
            {
                var c = d.Face.At((u0 + u1) * 0.5f, 0f, 0f);
                var tip = d.Face.At((u0 + u1) * 0.5f, S.CanopyDepth, 0f);
                var high = CanopySoffit(d.Origin.Y, d.Height);
                list.Add(new WallCollider(new(c.X, c.Z), new(tip.X, tip.Z), S.CanopyPier * 0.5f, d.Origin.Y - S.Sink, high, SurfaceTag.Wall) { Foot = FootMat.StonePaving });
            }
        return list;
    }

    /// <summary>A slab whose top is <paramref name="y"/>, reaching down to the stair's base, over (s, w) ranges.</summary>
    private BoxCollider Slab(float s0, float s1, float w0, float w1, float y, SurfaceTag tag, FootMat foot)
    {
        var c = At((s0 + s1) * 0.5f, (w0 + w1) * 0.5f);
        var bottom = Base;
        var frame = CoreShapes.Facing(new(c.X, (y + bottom) * 0.5f, c.Y), Out);
        return new BoxCollider(frame, new(MathF.Abs(w1 - w0) * 0.5f, (y - bottom) * 0.5f, MathF.Abs(s1 - s0) * 0.5f), tag) { Foot = foot };
    }
    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        CoreRanges.ForFeature(world, CoreIds.LibraryStair) is { } range ? [range] : [];

    public override void Build(SiteBuilder builder)
    {
        if (!builder.Includes(Anchor)) return;
        var build = CoreBuild.For(builder.Context);
        Build(build, builder.Lod);
        var output = build.Finish(builder.Lod);
        foreach (var mesh in output.Meshes) builder.Add(mesh, System.Numerics.Matrix4x4.Identity, Anchor);
        foreach (var part in output.Parts) builder.Place(part, Anchor);
        foreach (var emitter in output.Emitters ?? []) builder.Light(emitter, Anchor);
    }

    public override void Collide(SiteColliders colliders)
    {
        if (!colliders.Includes(Anchor)) return;
        foreach (var collider in Collide()) colliders.Add(collider, Anchor);
    }

    public override IEnumerable<ContentSourceTrace> TraceSources(BuildContext context, LodLevel lod)
    {
        var build = CoreBuild.For(context);
        Build(build, lod);
        yield return new($"core/{Id}", CoreIds.LibraryStair, build.Finish(lod), Collide(), []);
    }

}
