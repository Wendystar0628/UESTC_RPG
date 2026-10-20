using Campus.Gen.Sites;
using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Circulation;
using Campus.Gen.Parts.Core;
using Campus.Gen.Maps.Qingshuihe.Core.Framework;
using S = Campus.Gen.Maps.Qingshuihe.Core.CoreStyle;

namespace Campus.Gen.Maps.Qingshuihe.Core.Axis;

/// <summary>
/// 时间广场 (MAP-QSH-CORE, MAP-QSH-TERRAIN): the big light-grey circle on the library axis. The paving itself is
/// WP-15's; this piece adds what makes it the time plaza: three dark concentric bands with the twelve hour marks on
/// the outer one and a gold emblem in the centre, the ring of steps and the two flank ramps down to the sunken floor,
/// three flagpoles at the library-side step foot, the outer ring of thin lamps (初灯组) and the benches under them.
/// <para>
/// Heights come from the terrain (D-180): the floor is the terrain at the centre, the rim the terrain just outside
/// the circle. On the flat stub both are 0, so the plaza is flat and the steps and ramps are not built; once WP-08's
/// evaluator applies <c>core.terrain.json</c> (floor −0.9) the six risers and the ramps appear without code changes.
/// </para>
/// </summary>
public sealed class TimePlaza : ZoneSite, ILightMetadataSource
{
    private readonly IWorldModel _world;
    private readonly IReadOnlyList<Vector2> _benches;

    private TimePlaza(IWorldModel world, Vector2 centre, float radius, Vector2 axis, IReadOnlyList<Vector2> benches)
    {
        _world = world;
        Centre = centre;
        Radius = radius;
        Axis = axis;
        _benches = benches;
        AxisAngle = MathF.Atan2(axis.Y, axis.X);
        var terrain = world.Terrain;
        FloorTop = terrain.HeightAt(centre.X, centre.Y) + GroundLevels.Paving;
        var rim = 0f;
        var ring = Campus.Gen.Sites.Shapes.Circle(Vector2.Zero, 1f, 8);
        foreach (var c in ring) rim += terrain.HeightAt(centre.X + c.X * (radius + 0.6f), centre.Y + c.Y * (radius + 0.6f));
        RimTop = rim / ring.Length + GroundLevels.Paving;
        Rise = RimTop - FloorTop;
        if (Rise >= PlazaStyle.StepMinRise)
        {
            Risers = TerrainDrop.RiserCount(Rise);
            Riser = Rise / Risers;
            Tread = TerrainDrop.TreadFor(Riser);
        }
        FootRadius = Radius - Risers * Tread;
    }

    /// <summary>The plaza, or null when the map has no time plaza feature.</summary>
    public static TimePlaza? From(IWorldModel world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!world.Features.TryGetValue(CoreIds.TimePlaza, out var plaza)) return null;
        var (centre, radius) = Plan(plaza);
        // The axis points from the plaza centre to the library's grand stair (MAP-QSH-CORE).
        var axis = world.Features.TryGetValue(CoreIds.LibraryStair, out var stair)
            ? Vector2.Normalize(PlanPolygons.Centroid(WorldFeatureGeometry.PolygonsOf(stair.Parts.Count > 0 ? stair.Parts[0].Geometry : stair.Geometry).Select(r => r.Outer)) - centre)
            : Angles.Heading(PlazaStyle.AxisBearingFallback);
        var benches = world.Features.Values
            .Where(f => f.Id.Value.StartsWith(CoreIds.PlazaBenchPrefix, StringComparison.Ordinal) && f.Geometry is PointGeometry)
            .OrderBy(f => f.Id.Value, StringComparer.Ordinal)
            .Select(f => { var p = ((PointGeometry)f.Geometry).Position; return new Vector2(p.X, p.Z); })
            .ToArray();
        return new TimePlaza(world, centre, radius, axis, benches);
    }

    internal static (Vector2 Centre, float Radius) Plan(WorldFeature plaza)
    {
        var ring = FeatureShapes.Contour(plaza);
        var centre = Vector2.Zero;
        foreach (var p in ring) centre += p;
        centre /= ring.Count;
        var radius = 0f;
        foreach (var p in ring) radius += Vector2.Distance(p, centre);
        radius /= ring.Count;
        return (centre, radius);
    }

    public override string Id => "time-plaza";
    public Vector2 Anchor => Centre;
    public static IReadOnlyList<Claim> DeclaredClaims { get; } = [new FeatureClaim(CoreIds.TimePlaza, Aspects.Props), new FeatureClaim(CoreIds.PlazaRingSteps, Aspects.Massing)];
    public override IReadOnlyList<Claim> Claims => DeclaredClaims;

    public Vector2 Centre { get; }
    /// <summary>Plaza edge: the mean radius of the OSM circle (about 27 m).</summary>
    public float Radius { get; }
    /// <summary>Unit plan direction from the centre toward the library stair.</summary>
    public Vector2 Axis { get; }
    /// <summary>Angle of <see cref="Axis"/> in the x = cos θ, z = sin θ parameterisation.</summary>
    public float AxisAngle { get; }
    public float FloorTop { get; }
    public float RimTop { get; }
    public float Rise { get; }
    public int Risers { get; }
    public float Riser { get; }
    public float Tread { get; }
    /// <summary>Radius of the first riser's foot; equals <see cref="Radius"/> on a flat plaza.</summary>
    public float FootRadius { get; }
    public bool Sunken => Risers > 0;

    /// <summary>Left of the axis, seen facing the library (where the blue flag stands).</summary>
    public Vector2 Left => new(Axis.Y, -Axis.X);

    public void Build(CoreBuild build, LodLevel lod)
    {
        if (lod == LodLevel.L4) return;
        Inlays(build.Mesh, lod);
        if (Sunken)
        {
            foreach (var arc in StepArcs(lod))
                build.Mesh.Add(S.Stone(S.Granite), StepsAlong.Build(arc, false, FloorTop, Risers, Riser, Tread, StepsFinish.Stone, lod), Matrix4x4.Identity);
            foreach (var ramp in Ramps())
                BuildRamp(build.Mesh, ramp, lod);
        }
        Flags(build, lod);
        if (lod <= LodLevel.L2) Lamps(build);
        build.Emitters.AddRange(Lights(lod));
    }

    /// <summary>Pure lamp layout shared by resident metadata and the geometry output at each level.</summary>
    public IReadOnlyList<LightEmitter> Lights(LodLevel lod)
    {
        if (lod == LodLevel.L4) return [];
        var lights = new List<LightEmitter>();
        foreach (var pole in Poles())
            lights.Add(new(PlazaStyle.FlagLightChannel, new(pole.Foot.X - Axis.X * 0.9f, FloorTop + 0.05f, pole.Foot.Y - Axis.Y * 0.9f), PlazaStyle.LampColor, PlazaStyle.FlagLightEnergy, PlazaStyle.FlagLightRange));
        if (lod <= LodLevel.L2)
            foreach (var p in LampPositions())
            {
                var ground = _world.Terrain.HeightAt(p.X, p.Y) + GroundLevels.Lawn;
                lights.Add(new(PlazaStyle.RimLampChannel, new(p.X, ground + PlazaStyle.RimLampLightY, p.Y), PlazaStyle.LampColor, PlazaStyle.RimLampEnergy, PlazaStyle.RimLampRange));
            }
        return lights;
    }

    public IReadOnlyList<Collider> Collide()
    {
        var list = new List<Collider>();
        if (Sunken)
        {
            foreach (var arc in StepArcs(LodLevel.L0))
                list.AddRange(StepsAlong.Collide(arc, false, FloorTop, Risers, Riser, Tread, StepsFinish.Stone, FootMat.StonePaving));
            foreach (var ramp in Ramps())
                foreach (var wall in RampWalls(ramp))
                    for (var i = 0; i + 1 < wall.Stations.Count; i++)
                    {
                        var a = Station(wall.Radius, wall.Stations[i].Angle); var b = Station(wall.Radius, wall.Stations[i + 1].Angle);
                        var top = MathF.Max(wall.Stations[i].Top, wall.Stations[i + 1].Top);
                        list.Add(new WallCollider(a, b, PlazaStyle.CheekWidth * 0.5f, FloorTop - S.Sink, top, SurfaceTag.Wall) { Foot = FootMat.StonePaving });
                    }
        }
        var plinth = FlagPlinth();
        var plinthAt = Matrix4x4.CreateTranslation(0, (plinth.Y0 + plinth.Y1) * 0.5f, 0) * CoreShapes.Facing(new(plinth.Centre.X, 0, plinth.Centre.Y), Axis);
        var plinthHalf = new Vector3(PlazaStyle.FlagPlinthLength * 0.5f, (plinth.Y1 - plinth.Y0) * 0.5f, PlazaStyle.FlagPlinthDepth * 0.5f);
        list.Add(new BoxCollider(plinthAt, plinthHalf, SurfaceTag.Prop) { Foot = FootMat.StonePaving });
        foreach (var pole in Poles())
            list.Add(new PostCollider(pole.Foot, PlazaStyle.FlagCollar, plinth.Y1, plinth.Y1 + pole.Height, SurfaceTag.Prop) { Foot = FootMat.Metal });
        return list;
    }

    // ------------------------------------------------------------------ inlays

    private float Floor(float x, float z) => _world.Terrain.HeightAt(x, z) + GroundLevels.Paving;

    private void Inlays(GeometrySink mesh, LodLevel lod)
    {
        var band = S.Inlay(S.BandStone);
        var step = lod == LodLevel.L0 ? 1f : 2f * (1 << (int)lod);
        foreach (var (inner, outer) in PlazaStyle.Bands)
            CoreShapes.Annulus(mesh, band, Centre, FootRadius * inner, FootRadius * outer, 0, MathF.Tau, Floor, GroundLevels.Decal, step);
        if (lod <= LodLevel.L1) HourMarks(mesh, lod);
        if (lod <= LodLevel.L2) Emblem(mesh, lod);
    }

    /// <summary>十二时辰 on the outer band: 子 points true north (the sundial's gnomon direction), one mark every 30°.</summary>
    private void HourMarks(GeometrySink mesh, LodLevel lod)
    {
        var paint = S.Inlay(PlazaStyle.MarkStone);
        var (inner, outer) = PlazaStyle.Bands[0];
        float r0 = FootRadius * inner, r1 = FootRadius * outer;
        var north = MathF.Atan2(-1f, 0f);
        for (var k = 0; k < 12; k++)
        {
            var a = north + k * (MathF.Tau / 12f);
            var u = new Vector2(MathF.Cos(a), MathF.Sin(a));
            CoreShapes.FlatStrip(mesh, paint, Centre + u * (r0 + 0.04f), Centre + u * (r1 - 0.04f), PlazaStyle.HourMarkWidth, Floor, GroundLevels.Decal * 1.5f);
            if (lod != LodLevel.L0 || k % 3 != 0) continue;
            // 子、卯、午、酉 get a small diamond just outside the band.
            var c = Centre + u * (r1 + PlazaStyle.HourDiamond);
            var t = new Vector2(-u.Y, u.X);
            CoreShapes.FlatPolygon(mesh, S.Inlay(S.BandStone), [c - u * PlazaStyle.HourDiamond * 0.7f, c + t * PlazaStyle.HourDiamond * 0.45f, c + u * PlazaStyle.HourDiamond * 0.7f, c - t * PlazaStyle.HourDiamond * 0.45f], Floor, GroundLevels.Decal);
        }
    }

    /// <summary>
    /// The original emblem (ART-STYLE) as a single-colour gold inlay: a thin outer ring, an octagon whose corner
    /// points along the library axis, and eight circuit traces from its corners ending in square pads.
    /// </summary>
    private void Emblem(GeometrySink mesh, LodLevel lod)
    {
        var gold = S.Inlay(PlazaStyle.EmblemGold);
        CoreShapes.Annulus(mesh, gold, Centre, PlazaStyle.EmblemRingInner, PlazaStyle.EmblemRingOuter, 0, MathF.Tau, Floor, GroundLevels.Decal, lod == LodLevel.L0 ? 0.5f : 1.5f);
        var corners = new Vector2[8];
        for (var k = 0; k < 8; k++)
        {
            var a = AxisAngle + k * (MathF.Tau / 8f);
            corners[k] = new(MathF.Cos(a), MathF.Sin(a));
        }
        for (var k = 0; k < 8; k++)
            CoreShapes.FlatStrip(mesh, gold, Centre + corners[k] * PlazaStyle.EmblemOctagon, Centre + corners[(k + 1) % 8] * PlazaStyle.EmblemOctagon, PlazaStyle.EmblemLine, Floor, GroundLevels.Decal);
        if (lod != LodLevel.L0) return;
        for (var k = 0; k < 8; k++)
        {
            var u = corners[k]; var t = new Vector2(-u.Y, u.X);
            CoreShapes.FlatStrip(mesh, gold, Centre + u * PlazaStyle.EmblemOctagon, Centre + u * PlazaStyle.EmblemTraceEnd, PlazaStyle.EmblemLine * 0.75f, Floor, GroundLevels.Decal);
            var c = Centre + u * (PlazaStyle.EmblemTraceEnd + PlazaStyle.EmblemPad * 0.5f);
            var h = PlazaStyle.EmblemPad * 0.5f;
            CoreShapes.FlatPolygon(mesh, gold, [c - u * h - t * h, c + u * h - t * h, c + u * h + t * h, c - u * h + t * h], Floor, GroundLevels.Decal);
        }
        CoreShapes.Annulus(mesh, gold, Centre, 0.0001f, PlazaStyle.EmblemPad, 0, MathF.Tau, Floor, GroundLevels.Decal, 0.25f);
    }

    // ------------------------------------------------------------------ steps and ramps

    private Vector2 Station(float r, float angle) => Centre + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * r;

    /// <summary>One flank ramp: arc centre angle, the ±1 direction from its top end to its bottom end, and its runs.</summary>
    private sealed record Ramp(float CentreAngle, float Direction, float Span, float Radius, float Length, IReadOnlyList<(float S0, float S1, float Y0, float Y1)> Runs);

    /// <summary>
    /// The two 1:12 ramps on the axis' flanks (NE and SW), along the step band: they start level with the rim on the
    /// avenue side and arrive on the floor on the library side, with a 1.5 m landing every 0.75 m of rise.
    /// </summary>
    private IReadOnlyList<Ramp> Ramps()
    {
        if (!Sunken) return [];
        var runs = Math.Max(1, (int)MathF.Ceiling(Rise / PlazaStyle.RampMaxRunRise - 1e-4f));
        var drop = Rise / runs; var run = drop / PlazaStyle.RampGrade;
        var list = new List<(float, float, float, float)>();
        float s = 0, y = RimTop;
        for (var i = 0; i < runs; i++)
        {
            list.Add((s, s + run, y, y - drop));
            s += run; y -= drop;
            if (i + 1 < runs) { list.Add((s, s + PlazaStyle.RampLanding, y, y)); s += PlazaStyle.RampLanding; }
        }
        var radius = FootRadius + PlazaStyle.CheekWidth + PlazaStyle.RampWidth * 0.5f;
        var span = s / radius;
        // Toward the library side means toward AxisAngle: on the +90° flank the bottom end is the lower angle.
        return [new(AxisAngle + MathF.Tau / 4f, -1f, span, radius, s, list), new(AxisAngle - MathF.Tau / 4f, 1f, span, radius, s, list)];
    }

    private float DeckAt(Ramp ramp, float s)
    {
        foreach (var (s0, s1, y0, y1) in ramp.Runs)
            if (s <= s1 + 1e-4f) return y0 + (y1 - y0) * Math.Clamp((s - s0) / (s1 - s0), 0, 1);
        return ramp.Runs[^1].Y1;
    }

    /// <summary>Angle at plan distance s from the ramp's top end.</summary>
    private static float AngleAt(Ramp ramp, float s) => ramp.CentreAngle - ramp.Direction * ramp.Span * 0.5f + ramp.Direction * s / ramp.Radius;

    /// <summary>The step ring, cut where the ramps and their cheeks run: two open arcs climbing outward.</summary>
    private IReadOnlyList<IReadOnlyList<Vector2>> StepArcs(LodLevel lod)
    {
        var ramps = Ramps();
        var gap = ramps.Count == 0 ? 0 : ramps[0].Span * 0.5f + (PlazaStyle.CheekWidth + 0.02f) / FootRadius;
        var sides = S.CircleSides(lod);
        var arcs = new List<IReadOnlyList<Vector2>>();
        // Library side from −90° + gap to +90° − gap, then the avenue side; angles rise so the steps climb outward.
        foreach (var (from, to) in new[] { (AxisAngle - MathF.Tau / 4f + gap, AxisAngle + MathF.Tau / 4f - gap), (AxisAngle + MathF.Tau / 4f + gap, AxisAngle + 3 * MathF.Tau / 4f - gap) })
        {
            var count = Math.Max(2, (int)MathF.Ceiling((to - from) / (MathF.Tau / sides)));
            var arc = new Vector2[count + 1];
            for (var i = 0; i <= count; i++) arc[i] = Station(FootRadius, from + (to - from) * i / count);
            arcs.Add(arc);
        }
        return arcs;
    }

    private sealed record CheekStation(float Angle, float Top);
    private sealed record Cheek(float Radius, IReadOnlyList<CheekStation> Stations);

    /// <summary>Stone cheek walls on both edges of a ramp: inner one above the floor, outer one retaining the rim lawn.</summary>
    private IReadOnlyList<Cheek> RampWalls(Ramp ramp)
    {
        var count = Math.Max(4, (int)MathF.Ceiling(ramp.Length / 0.75f));
        var inner = new CheekStation[count + 1]; var outer = new CheekStation[count + 1];
        for (var i = 0; i <= count; i++)
        {
            var s = ramp.Length * i / count;
            var deck = DeckAt(ramp, s);
            inner[i] = new(AngleAt(ramp, s), deck + PlazaStyle.CheekCoping);
            outer[i] = new(AngleAt(ramp, s), MathF.Max(RimTop, deck) + PlazaStyle.CheekCoping);
        }
        var half = PlazaStyle.RampWidth * 0.5f + PlazaStyle.CheekWidth * 0.5f;
        return [new(ramp.Radius - half, inner), new(ramp.Radius + half, outer)];
    }

    private void BuildRamp(GeometrySink mesh, Ramp ramp, LodLevel lod)
    {
        var stone = S.Stone(S.Granite);
        var count = Math.Max(8, (int)MathF.Ceiling(ramp.Length / 0.5f));
        var deck = new Vector3[count + 1];
        for (var i = 0; i <= count; i++)
        {
            var s = ramp.Length * i / count;
            var p = Station(ramp.Radius, AngleAt(ramp, s));
            deck[i] = new(p.X, DeckAt(ramp, s), p.Y);
        }
        mesh.Add(stone, RampEdges.Build(deck, PlazaStyle.RampWidth, lod, rails: lod <= LodLevel.L1), Matrix4x4.Identity);
        foreach (var wall in RampWalls(ramp)) CheekWall(mesh, stone, wall, lod);
    }

    /// <summary>A curved wall of constant width whose top follows the stations, with chamfered top edges.</summary>
    private void CheekWall(GeometrySink mesh, in Paint paint, Cheek wall, LodLevel lod)
    {
        var b = lod == LodLevel.L0 ? PlazaStyle.CheekBevel : 0f;
        float r0 = wall.Radius - PlazaStyle.CheekWidth * 0.5f, r1 = wall.Radius + PlazaStyle.CheekWidth * 0.5f, bottom = FloorTop - S.Sink;
        // Section corners counterclockwise in (radius, height); Top marks heights measured down from the station's top.
        (float R, bool Top, float Y)[] section = b > 0
            ? [(r0, false, bottom), (r1, false, bottom), (r1, true, -b), (r1 - b, true, 0), (r0 + b, true, 0), (r0, true, -b)]
            : [(r0, false, bottom), (r1, false, bottom), (r1, true, 0), (r0, true, 0)];
        var n = wall.Stations.Count;
        Vector3 P(int i, (float R, bool Top, float Y) q)
        {
            var a = wall.Stations[i].Angle;
            return new(Centre.X + MathF.Cos(a) * q.R, q.Top ? wall.Stations[i].Top + q.Y : q.Y, Centre.Y + MathF.Sin(a) * q.R);
        }
        for (var i = 0; i + 1 < n; i++)
            for (var k = 0; k < section.Length; k++)
            {
                var q0 = section[k]; var q1 = section[(k + 1) % section.Length];
                var mid = (wall.Stations[i].Angle + wall.Stations[i + 1].Angle) * 0.5f;
                var radial = new Vector3(MathF.Cos(mid), 0, MathF.Sin(mid));
                var p0 = P(i, q0); var p1 = P(i, q1);
                // Outward normal of a counterclockwise section edge (dr, dy) is (dy, −dr).
                var normal = VectorMath.Unit(radial * (p1.Y - p0.Y) - Vector3.UnitY * (q1.R - q0.R), Vector3.UnitY, 1e-8);
                mesh.Quad(paint, p0, p1, P(i + 1, q1), P(i + 1, q0), normal);
            }
        foreach (var (i, sign) in new[] { (0, -1f), (n - 1, 1f) })
        {
            var a = wall.Stations[i].Angle;
            var along = new Vector3(-MathF.Sin(a), 0, MathF.Cos(a)) * (sign * MathF.Sign(wall.Stations[n - 1].Angle - wall.Stations[0].Angle));
            for (var k = 1; k + 1 < section.Length; k++)
                mesh.Tri(paint, P(i, section[0]), P(i, section[k]), P(i, section[k + 1]), along);
        }
    }

    // ------------------------------------------------------------------ flags

    private sealed record Pole(Vector2 Foot, float Height, float Hang, string Colour);

    private (Vector2 Centre, float Y0, float Y1) FlagPlinth()
    {
        var centre = Centre + Axis * (FootRadius - PlazaStyle.FlagSetback);
        return (centre, FloorTop - S.Sink, FloorTop + PlazaStyle.FlagPlinthHeight);
    }

    /// <summary>National flag in the middle (tallest), blue flag on the left and school flag on the right, facing the library.</summary>
    private IReadOnlyList<Pole> Poles()
    {
        var c = FlagPlinth().Centre;
        return
        [
            new(c + Left * PlazaStyle.FlagSpacing, PlazaStyle.FlagSidePoleHeight, PlazaStyle.FlagSideHang, PlazaStyle.FlagBlue),
            new(c, PlazaStyle.FlagPoleHeight, PlazaStyle.FlagHang, PlazaStyle.FlagRed),
            new(c - Left * PlazaStyle.FlagSpacing, PlazaStyle.FlagSidePoleHeight, PlazaStyle.FlagSideHang, S.FlagWhite),
        ];
    }

    private void Flags(CoreBuild build, LodLevel lod)
    {
        var mesh = build.Mesh;
        var plinth = FlagPlinth();
        var granite = S.Stone(S.Granite);
        var box = PartSolids.BeveledBox(PlazaStyle.FlagPlinthLength, plinth.Y1 - plinth.Y0, PlazaStyle.FlagPlinthDepth, lod == LodLevel.L0 ? PlazaStyle.FlagPlinthBevel : 0f, CoreStyle.Spec(CoreMaterial.Stone, S.Granite));
        mesh.Add(granite, box, CoreShapes.Facing(new(plinth.Centre.X, plinth.Y0, plinth.Centre.Y), Axis));
        var steel = S.Metal(S.Steel);
        var sides = S.SmallSides(lod);
        foreach (var pole in Poles())
        {
            var foot = new Vector3(pole.Foot.X, plinth.Y1, pole.Foot.Y);
            var frame = Matrix4x4.CreateTranslation(foot);
            CoreShapes.Lathe(mesh, granite, frame, CoreShapes.Tier(PlazaStyle.FlagCollar, -0.01f, PlazaStyle.FlagCollarHeight, lod == LodLevel.L0 ? 0.03f : 0f), sides);
            CoreShapes.Lathe(mesh, steel, frame, [new(0, 0), new(PlazaStyle.FlagPoleBase, 0), new(PlazaStyle.FlagPoleTop, pole.Height), new(0, pole.Height)], sides);
            if (lod <= LodLevel.L1)
            {
                // Finial ball on top.
                var ball = new Vector2[7];
                for (var i = 0; i < ball.Length; i++)
                {
                    var a = -MathF.Tau / 4f + i * (MathF.Tau / 2f) / (ball.Length - 1);
                    ball[i] = new(MathF.Max(0f, MathF.Cos(a) * PlazaStyle.FlagFinial), pole.Height + PlazaStyle.FlagFinial + MathF.Sin(a) * PlazaStyle.FlagFinial);
                }
                ball[0] = ball[0] with { X = 0 }; ball[^1] = ball[^1] with { X = 0 };
                CoreShapes.Lathe(mesh, steel, frame, ball, sides);
            }
            Drape(mesh, pole, foot, lod);
        }
    }

    /// <summary>
    /// A flag at rest: the cloth hangs from the top of the pole in a few vertical folds, on the plaza side so it reads
    /// from P02. Both faces are emitted (thin cloth is seen from both sides).
    /// </summary>
    private void Drape(GeometrySink mesh, Pole pole, Vector3 foot, LodLevel lod)
    {
        var paint = S.Cloth(pole.Colour);
        var folds = lod == LodLevel.L0 ? PlazaStyle.FlagFolds : 2;
        var top = foot.Y + pole.Height - 0.25f;
        var bottom = top - pole.Hang;
        var width = folds * 2 * PlazaStyle.FlagFoldWidth;
        var outward = -Axis;
        var origin = new Vector2(foot.X, foot.Z) + outward * (PlazaStyle.FlagPoleBase + 0.03f);
        var across = Left;
        var columns = folds * 2 + 1;
        for (var row = 0; row < 2; row++)
        {
            // The lower edge narrows a little and swings off the pole, like cloth gathered at the top.
            float y0 = row == 0 ? top : top - pole.Hang * 0.55f, y1 = row == 0 ? top - pole.Hang * 0.55f : bottom;
            float w0 = row == 0 ? 1f : 0.85f, w1 = row == 0 ? 0.85f : 0.62f;
            for (var i = 0; i + 1 < columns; i++)
            {
                Vector3 V(int k, float y, float w)
                {
                    var x = (k / (float)(columns - 1) - 0.5f) * width * w;
                    var depth = (k % 2 == 1 ? PlazaStyle.FlagFoldDepth : 0f) * w + (1f - w) * 0.25f;
                    var p = origin + across * x + outward * (depth + width * 0.5f * w * 0.4f);
                    return new(p.X, y, p.Y);
                }
                var a = V(i, y0, w0); var b = V(i + 1, y0, w0); var c = V(i + 1, y1, w1); var d = V(i, y1, w1);
                var n = VectorMath.Unit(Vector3.Cross(b - a, d - a), Vector3.UnitY, 1e-8);
                mesh.Quad(paint, a, b, c, d, n);
                mesh.Quad(paint, a, b, c, d, -n);
            }
        }
    }

    // ------------------------------------------------------------------ lamps and benches

    /// <summary>Rim lamp positions: a ring outside the plaza edge, the first half a step off the axis so none stands on it.</summary>
    public IReadOnlyList<Vector2> LampPositions()
    {
        var list = new Vector2[PlazaStyle.RimLamps];
        for (var i = 0; i < list.Length; i++)
            list[i] = Station(Radius + PlazaStyle.RimLampOffset, AxisAngle + (i + 0.5f) * MathF.Tau / PlazaStyle.RimLamps);
        return list;
    }

    private void Lamps(CoreBuild build)
    {
        var parameters = new LampParams(PlazaStyle.RimLampWidth, PlazaStyle.RimLampHeight, PlazaStyle.RimLampWidth) { Material = S.PartMaterial(PlazaStyle.LampMetal) };
        foreach (var p in LampPositions())
        {
            var ground = _world.Terrain.HeightAt(p.X, p.Y) + GroundLevels.Lawn;
            build.Parts.Add(new PlacedPart(new PartId(PartCatalog.StreetLamp), parameters, Matrix4x4.CreateTranslation(p.X, ground, p.Y)));
        }
        var bench = BenchParameters;
        foreach (var p in _benches)
        {
            var ground = _world.Terrain.HeightAt(p.X, p.Y) + GroundLevels.Lawn;
            build.Parts.Add(new PlacedPart(new PartId(PartCatalog.Bench), bench, CoreShapes.Facing(new(p.X, ground, p.Y), Vector2.Normalize(Centre - p))));
        }
    }

    internal static FurnitureParams BenchParameters { get; } = new(1.8f, 0.8f, 0.6f) { Material = S.PartMaterial(PlazaStyle.LampMetal) };
    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        CoreRanges.ForFeature(world, CoreIds.TimePlaza) is { } range ? [range] : [];

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
        yield return new($"core/{Id}", CoreIds.TimePlaza, build.Finish(lod), Collide(), []);
    }

    public IReadOnlyList<LightEmitter> DescribeLights(BuildContext context) => Lights(LodLevel.L0);

}
