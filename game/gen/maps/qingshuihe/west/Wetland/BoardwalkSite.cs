using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Circulation;
using Campus.Gen.Parts.Core;
using Campus.Gen.World;
using StreetStyle = Campus.Gen.Parts.Street.StreetStyle;
using Campus.Gen.Maps.Qingshuihe.West.Framework;
using Campus.Gen.Sites;
using L = Campus.Gen.Maps.Qingshuihe.West.WestTune.Look;
using W = Campus.Gen.Maps.Qingshuihe.West.WestTune.Wetland;

namespace Campus.Gen.Maps.Qingshuihe.West.Wetland;

/// <summary>
/// 湿地栈道 (MAP-QSH-WEST): a level timber boardwalk on piles through the wetland reeds, deck flush with the
/// paving at both mouths (A10 plaza, the footway at the 西门车行桥's east head). Close up (L0) the deck is laid in single
/// planks with fine gaps; further off it is one beveled slab. Both edges carry a 1.05 m timber railing (posts every 2 m,
/// handrail, one mid rail; barrier top 1.5 m, SYS-WALK) with a 2.2 m gap and timber steps down to the pebble wading beach
/// at the turn by the stream (§12 涉水区). Low lamps stand on the edge away from the stream every 16 m and light the planks
/// only, so both water sides stay dark for the fireflies (§10.1). Snow lies on the planks and the handrail tops by normal
/// (SYS-TIME); the deck only darkens when wet, it never pools (§11 天气要点).
/// </summary>
public sealed class BoardwalkSite : ZoneSite, ILightMetadataSource
{
    private static readonly StepsPart Steps = new();

    internal static Route Path(IWorldModel world) => new(Route.Smooth(W.Route(world), 2));

    internal static Route Path(BuildContext context) => context.Memo(typeof(Route), () => Path(context.World));

    public override string Id => "19c.boardwalk";

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        [SiteRanges.Bounds(W.Route(world), W.Width + W.StepTread * 2f),
         SiteRanges.Bounds(BeachFrame(world).Rect(-W.BeachHalfLength, -W.BeachHalfWidth,
             W.BeachHalfLength, W.BeachHalfWidth), W.BeachHalfLength * 0.06f + W.PebbleMax)];

    /// <summary>Deck top: the higher mouth's paving plus a small lip, constant along the boardwalk.</summary>
    internal static float DeckTop(IWorldModel world, Func<float, float, float> ground)
    {
        var p = Path(world).Points;
        return MathF.Max(ground(p[0].X, p[0].Y), ground(p[^1].X, p[^1].Y)) + W.PavingTop + W.Lip;
    }

    /// <summary>Arc length of the beach opening and the side (+1 right, −1 left) the stream and the beach lie on.</summary>
    internal static (float S, float Side) Beach(IWorldModel world)
    {
        var (s, lateral) = Path(world).Project(W.BeachCentre(world));
        return (s, lateral >= 0 ? 1f : -1f);
    }

    /// <summary>Rail runs of one side as arc-length intervals; the water side opens at the beach.</summary>
    internal static (float S0, float S1)[] RailRuns(IWorldModel world, float side)
    {
        var (s, waterSide) = Beach(world);
        var length = Path(world).Length;
        return side == waterSide ? [(0f, s - W.BeachGap * 0.5f), (s + W.BeachGap * 0.5f, length)] : [(0f, length)];
    }

    /// <summary>Lamp stations along the edge away from the stream.</summary>
    internal static float[] LampStations(IWorldModel world)
    {
        var list = new List<float>();
        var length = Path(world).Length;
        for (var s = W.LampStart; s < length - 2f; s += W.LampPitch) list.Add(s);
        return [.. list];
    }

    internal static float RailLateral => W.Width * 0.5f - W.RailInset;

    public override void Build(SiteBuilder b)
    {
        var world = b.Context.World;
        var y = DeckTop(b.Context.World, b.Ground);
        var path = Path(b.Context);
        var detail = b.Detail;
        // Deck: single planks close up, one beveled slab further off.
        if (detail == SiteDetail.Full) Planks(b, path, y);
        else b.Add(PartGeometryKernel.Sweep(PartGeometryKernel.BeveledRectangleProfile(W.Width, W.Plank, detail == SiteDetail.Mass ? 0f : 0.015f), path.Points, L.Deck, baseY: y - W.Plank), Matrix4x4.Identity);
        // Stringers along both edges: the deck's clean edge line (收边), a step darker.
        foreach (var side in new[] { -1f, 1f })
            b.Add(PartGeometryKernel.Sweep(PartGeometryKernel.BeveledRectangleProfile(W.StringerWidth, W.StringerDepth, detail == SiteDetail.Mass ? 0f : 0.02f),
                PlanGeometry.MitreOffset(path.Points, side * (W.Width * 0.5f + W.StringerWidth * 0.5f - 0.02f), closed: false), L.DeckDark, baseY: y - W.StringerDepth + 0.015f), Matrix4x4.Identity);
        // Granite sills at both mouths: the step from the paving onto the planks reads as a finished edge (收边).
        foreach (var (s, sign) in new[] { (0f, -1f), (path.Length, 1f) })
        {
            var (p, t) = path.At(s);
            var sill = PlanFrame.Along(p + t * sign * (W.Sill * 0.5f - 0.02f), Route.Right(t));
            GridFacade.Box(b, sill, -W.Width * 0.5f - 0.12f, W.Width * 0.5f + 0.12f, y - 0.25f, y - 0.005f, -W.Sill * 0.5f, W.Sill * 0.5f, L.Granite, detail == SiteDetail.Mass ? 0f : 0.02f);
        }
        if (detail == SiteDetail.Mass) return;
        Piles(b, path, y);
        foreach (var side in new[] { -1f, 1f })
            foreach (var (s0, s1) in RailRuns(world, side)) Railing(b, path, y, side, s0, s1, detail);
        foreach (var s in LampStations(world)) Lamp(b, path, y, s, -Beach(world).Side, detail);
        if (b.Includes(SiteRanges.Point(W.BeachCentre(world), W.BeachHalfLength + W.Width)))
        {
            BeachSteps(b, path, y);
            PebbleBeach(b, detail);
        }
    }

    // ---- Deck ----

    private static void Planks(SiteBuilder b, Route path, float y)
    {
        const float board = 0.14f, gap = 0.012f;
        var count = (int)MathF.Floor(path.Length / (board + gap));
        var pitch = path.Length / count;
        var plank = PartGeometryKernel.BeveledBox(W.Width - 0.04f, W.Plank, board, 0.008f, L.Deck);
        for (var i = 0; i < count; i++)
        {
            var (p, t) = path.At((i + 0.5f) * pitch);
            if (!b.Includes(SiteRanges.Point(p, W.Width))) continue;
            b.Add(plank, PlanFrame.Along(p, Route.Right(t)).At(0, y - W.Plank, 0));
        }
    }

    private static void Piles(SiteBuilder b, Route path, float y)
    {
        var pile = PartGeometryKernel.BeveledBox(W.Width - 0.3f, 0.12f, 0.1f, 0.015f, L.DeckDark);
        for (var s = 1f; s < path.Length - 0.5f; s += W.PilePitch)
        {
            var (p, t) = path.At(s);
            if (!b.Includes(SiteRanges.Point(p, W.Width))) continue;
            var r = Route.Right(t);
            // A joist across under the planks on each pile pair.
            b.Add(pile, PlanFrame.Along(p, r).At(0, y - W.Plank - 0.12f, 0));
            foreach (var side in new[] { -1f, 1f })
            {
                var q = p + r * side * (W.Width * 0.5f - 0.2f);
                var foot = b.Ground(q) - W.PileSink;
                var top = y - W.Plank - 0.1f;
                if (top - foot < 0.05f) continue;
                b.Add(Shapes.Shaft(W.PileRadius, W.PileRadius, top - foot, 8, L.DeckDark), Matrix4x4.CreateTranslation(q.X, foot, q.Y));
            }
        }
    }

    // ---- Railing ----

    private static void Railing(SiteBuilder b, Route path, float y, float side, float s0, float s1, SiteDetail detail)
    {
        if (s1 - s0 < 0.5f) return;
        var lateral = side * RailLateral;
        var line = PlanGeometry.MitreOffset(path.Sub(s0, s1), lateral, closed: false);
        if (!b.Includes(SiteRanges.Bounds(line, W.HandrailWidth))) return;
        var bevel = detail == SiteDetail.Full ? 0.025f : 0f;
        b.Add(PartGeometryKernel.Sweep(PartGeometryKernel.BeveledRectangleProfile(W.HandrailWidth, W.HandrailDepth, bevel), line, L.Deck, baseY: y + W.RailHeight - W.HandrailDepth), Matrix4x4.Identity);
        b.Add(PartGeometryKernel.Sweep(PartGeometryKernel.BeveledRectangleProfile(W.MidRailSize, W.MidRailSize, detail == SiteDetail.Full ? 0.01f : 0f), line, L.DeckDark, baseY: y + W.MidRail), Matrix4x4.Identity);
        var count = Math.Max(1, (int)MathF.Round((s1 - s0) / W.PostPitch));
        var post = PartGeometryKernel.BeveledBox(W.PostSize, W.RailHeight - W.HandrailDepth + 0.02f, W.PostSize, detail == SiteDetail.Full ? 0.012f : 0f, L.DeckDark);
        // End posts are a little stouter, with a cap above the handrail: the rail's ends read as finished (收边).
        var newel = PartGeometryKernel.BeveledBox(W.PostSize * 1.4f, W.RailHeight + 0.05f, W.PostSize * 1.4f, detail == SiteDetail.Full ? 0.02f : 0f, L.DeckDark);
        for (var i = 0; i <= count; i++)
        {
            var (p, t) = path.At(s0 + (s1 - s0) * i / count);
            var q = p + Route.Right(t) * lateral;
            var end = i == 0 || i == count;
            b.Add(end ? newel : post, PlanFrame.Along(q, t).At(0, y - 0.02f, 0));
        }
    }

    // ---- Low lamps ----

    public IReadOnlyList<LightEmitter> DescribeLights(BuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var y = DeckTop(context.World, context.World.Terrain.HeightAt);
        return LampStations(context.World).Select(s => LampLayout(Path(context), y, s, -Beach(context.World).Side).Emitter).ToArray();
    }

    private static (Vector2 Foot, LightEmitter Emitter) LampLayout(Route path, float y, float s, float side)
    {
        var (p, t) = path.At(s);
        var r = Route.Right(t);
        var q = p + r * side * (W.Width * 0.5f - W.LampInset);
        // The marker sits under the cap, nudged toward the deck's middle (contract-12).
        var m = q - r * side * 0.08f;
        return (q, new(StreetStyle.Landscape, new(m.X, y + W.LampHeight - 0.13f, m.Y), StreetStyle.LampWarm, W.LampEnergy, W.LampRange));
    }

    private static void Lamp(SiteBuilder b, Route path, float y, float s, float side, SiteDetail detail)
    {
        var (q, emitter) = LampLayout(path, y, s, side);
        if (!b.Includes(SiteRanges.Point(q, W.LampRadius * 1.3f))) return;
        var segments = detail == SiteDetail.Full ? 14 : 8;
        var h = W.LampHeight;
        b.Add(Shapes.Shaft(W.LampRadius * 1.25f, W.LampRadius * 1.15f, 0.04f, segments, L.LampBody), Matrix4x4.CreateTranslation(q.X, y, q.Y));
        b.Add(Shapes.Shaft(W.LampRadius, W.LampRadius, h - 0.16f, segments, L.LampBody), Matrix4x4.CreateTranslation(q.X, y, q.Y));
        b.Add(Shapes.Shaft(W.LampRadius * 0.92f, W.LampRadius * 0.92f, 0.08f, segments, L.LampLens), Matrix4x4.CreateTranslation(q.X, y + h - 0.16f, q.Y));
        b.Add(Shapes.Shaft(W.LampRadius * 1.3f, W.LampRadius * 1.1f, 0.08f, segments, L.LampBody), Matrix4x4.CreateTranslation(q.X, y + h - 0.08f, q.Y));
        b.Light(emitter);
    }

    // ---- Wading beach and its steps ----

    /// <summary>The steps' frame: origin on the deck edge at the beach opening, local +Z back toward the deck (the upper level).</summary>
    internal static (PlanFrame Frame, float Foot, StairParams? Flight) BeachFlight(IWorldModel world, Func<float, float, float> ground)
    {
        var y = DeckTop(world, ground);
        var (s, side) = Beach(world);
        var (p, t) = Path(world).At(s);
        var outward = Route.Right(t) * side;
        var edge = p + outward * (W.Width * 0.5f);
        var frame = new PlanFrame(edge, new(-outward.Y, outward.X));
        // Two treads out from the deck edge is where the stoop lands; read the ground there.
        var probe = edge + outward * (W.StepTread * 2f);
        var foot = ground(probe.X, probe.Y);
        var rise = y - foot;
        var n = (int)MathF.Ceiling(rise / 0.17f);
        if (rise < 0.08f || n < 1) return (frame, foot, null);
        n = Math.Max(n, (int)MathF.Ceiling(rise / 0.2f));
        while (rise / n < 0.08f && n > 1) n--;
        return (frame, foot, new StairParams(W.BeachGap - 0.2f, rise, n * W.StepTread, n) { Variation = 2, Material = L.Deck, Foot = FootMat.WoodDeck });
    }

    private static void BeachSteps(SiteBuilder b, Route path, float y)
    {
        var (frame, foot, flight) = BeachFlight(b.Context.World, b.Ground);
        if (flight is null) return;
        b.Place(PartCatalog.Steps, flight, frame.At(0, foot, -flight.Depth * 0.5f));
    }

    /// <summary>The beach's frame: origin at its centre, local X along the stream.</summary>
    internal static PlanFrame BeachFrame(IWorldModel world) => PlanFrame.Along(W.BeachCentre(world), W.BeachAxis);

    private static void PebbleBeach(SiteBuilder b, SiteDetail detail)
    {
        var f = BeachFrame(b.Context.World);
        int rings = detail == SiteDetail.Full ? 5 : 3, segments = detail == SiteDetail.Full ? 32 : 16;
        var m = new Shapes.Writer(L.Pebble);
        var up = Vector3.UnitY;
        int Vertex(float u, float v, float k)
        {
            var w = f.P(u * W.BeachHalfLength, v * W.BeachHalfWidth);
            // A low crown inside, feathered to the ground at the rim so the beach has no cut edge.
            var y = b.Ground(w) + W.GrassTop + 0.02f + 0.05f * (1f - k * k);
            return m.Vertex(new(w.X, y, w.Y), up, new(w.X, w.Y));
        }
        var centre = Vertex(0, 0, 0);
        var previous = new int[segments];
        for (var k = 1; k <= rings; k++)
        {
            var r = k / (float)rings;
            var ring = new int[segments];
            for (var i = 0; i < segments; i++)
            {
                var a = 2f * MathF.PI * i / segments;
                // A slightly irregular outline: a fixed ripple, not a perfect ellipse.
                var wobble = 1f + 0.06f * MathF.Sin(3f * a + 0.7f) * r;
                ring[i] = Vertex(MathF.Cos(a) * r * wobble, MathF.Sin(a) * r * wobble, r);
            }
            for (var i = 0; i < segments; i++)
            {
                var j = (i + 1) % segments;
                if (k == 1) m.Triangle(centre, ring[i], ring[j], up);
                else m.Quad(previous[i], previous[j], ring[j], ring[i], up);
            }
            previous = ring;
        }
        b.Add(m.Finish(), Matrix4x4.Identity);
        if (detail != SiteDetail.Full) return;
        // Pebbles strewn over it: low rounded domes in two pale greys, from a fixed seed.
        var rng = b.Context.Seeded(new(W.Entity), new("19c.beach"), "pebbles");
        for (var i = 0; i < W.Pebbles; i++)
        {
            float u, v;
            do { u = rng.NextFloat() * 2f - 1f; v = rng.NextFloat() * 2f - 1f; } while (u * u + v * v > 0.8f);
            var size = W.PebbleMin + (W.PebbleMax - W.PebbleMin) * rng.NextFloat() * rng.NextFloat();
            var stretch = 0.7f + 0.5f * rng.NextFloat();
            var turn = rng.NextFloat();
            var w = f.P(u * W.BeachHalfLength, v * W.BeachHalfWidth);
            var dir = new Vector2(MathF.Cos(turn * 6.2831855f), MathF.Sin(turn * 6.2831855f));
            var y = b.Ground(w) + W.GrassTop + 0.02f + 0.05f * (1f - (u * u + v * v));
            Vector2[] dome = [new(1f, 0f), new(0.92f, 0.32f * size), new(0.62f, 0.58f * size), new(0f, 0.68f * size)];
            b.Add(Solids.Lathe(dome, 9, i % 3 == 0 ? L.Granite : L.Pebble, size * stretch, size), PlanFrame.Along(w, dir).At(0, y - size * 0.18f, 0));
        }
    }

    // ---- Colliders ----

    public override void Collide(SiteColliders c)
    {
        var world = c.Context.World;
        var path = Path(c.Context);
        var y = DeckTop(c.Context.World, c.Ground);
        // Deck: 2 m slabs, each a box whose top is the deck (SYS-WALK), timber underfoot.
        var slabs = Math.Max(1, (int)MathF.Ceiling(path.Length / W.SlabLength));
        var piece = path.Length / slabs;
        for (var i = 0; i < slabs; i++)
        {
            var (p, t) = path.At((i + 0.5f) * piece);
            if (!c.Includes(SiteRanges.Point(p, W.Width))) continue;
            c.Box(PlanFrame.Along(p, t).At(0, y - W.SlabThick * 0.5f, 0), new(piece * 0.5f + 0.05f, W.SlabThick * 0.5f, W.Width * 0.5f), SurfaceTag.Ground, FootMat.WoodDeck);
        }
        // Railings: one barrier per polyline segment, top max(1.05, 1.5).
        foreach (var side in new[] { -1f, 1f })
            foreach (var (s0, s1) in RailRuns(world, side))
            {
                if (s1 - s0 < 0.5f) continue;
                var line = PlanGeometry.MitreOffset(path.Sub(s0, s1), side * RailLateral, closed: false);
                for (var i = 0; i + 1 < line.Length; i++)
                    if (c.Includes(SiteRanges.Bounds([line[i], line[i + 1]], W.PostSize * 0.5f))) c.Wall(line[i], line[i + 1], W.PostSize * 0.5f, y - 0.1f, y + WalkRules.BarrierHeight, SurfaceTag.Barrier, FootMat.WoodDeck);
            }
        foreach (var s in LampStations(world))
        {
            var (p, t) = path.At(s);
            var q = p + Route.Right(t) * (-Beach(world).Side) * (W.Width * 0.5f - W.LampInset);
            if (!c.Includes(SiteRanges.Point(q, W.LampRadius * 1.3f))) continue;
            c.Add(new PostCollider(q, W.LampRadius * 1.3f, y, y + W.LampHeight, SurfaceTag.Prop) { Foot = FootMat.Metal });
        }
        if (!c.Includes(SiteRanges.Point(W.BeachCentre(world), W.BeachHalfLength + W.Width))) return;
        var (frame, foot, flight) = BeachFlight(c.Context.World, c.Ground);
        if (flight is not null) c.Add(Steps.Collision(flight), frame.At(0, foot, -flight.Depth * 0.5f));
    }

    /// <summary>Q2 (walk, K1, MAP-QSH-WEST 近景巡检点 2): P11's spot on the deck at eye height, looking 250° over the reeds.</summary>
    public override IReadOnlyList<InspectionProbe> Probes =>
        [Probe("Q2", W.Entity, new(W.P11.X, W.PavingTop + W.Lip + WalkRules.StandingEye, W.P11.Y), Angles.Heading(W.P11Bearing), -4f, 12f)];
}
