using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.World;
using Campus.Gen.Zones.West.Framework;
using L = Campus.Gen.Zones.West.WestTune.Look;
using S = Campus.Gen.Zones.West.WestTune.Six;
using W = Campus.Gen.Zones.West.WestTune.Windows;

namespace Campus.Gen.Zones.West.Six;

/// <summary>
/// 六号科研楼 (MAP-QSH-WEST), a landmark built by this zone instead of WP-14: blocks A (9 floors, the tallest), B and C
/// (7 floors) each stand on a two-storey dark glass base set back under the stone, with stone columns in the stone line;
/// above it deep square windows in a warm grey stone grid and one continuous dark louvre strip per floor. C carries the
/// crown sign over its east face (red emblem, silver characters) and the open east door (C 区东门, interiors.json six.c.east):
/// a glass vestibule out to the stone line, doors standing open, a granite threshold apron. The A–B link is two storeys of glass;
/// the B–C link is the 2F outdoor bridge over the drive-through, reaching B and C on narrow decks; the north tower is the
/// stone stair core. Every other entrance is shut: glass doors, a lobby card behind them, DoorClosed. Lit and shaded by the
/// live sun only; nothing here keys on the clock (§1.5). Each stone block is one named window grid (contract-29); the base
/// glass stays unnumbered.
/// </summary>
public sealed class SixSite : WestSite
{
    /// <summary>One stone block: its entity, its source part, its window grid key and its floors above the base.</summary>
    internal sealed record Block(string Entity, string Part, string Key, int Upper);

    internal static readonly IReadOnlyList<Block> Blocks =
        [new(S.EntityA, S.PartA, "a", S.UpperA), new(S.EntityB, S.PartB, "b", S.UpperB), new(S.EntityC, S.PartC, "c", S.UpperC)];

    /// <summary>The blocks' plan frame, X along <see cref="S.Axis"/>.</summary>
    internal static readonly PlanFrame Frame = PlanFrame.Along(Vector2.Zero, S.Axis);

    public override string Id => "19a.six";

    public override IReadOnlyList<Claim> Claims =>
        [.. new[] { S.EntityA, S.EntityB, S.EntityC }.Select(e => (Claim)new FeatureClaim(new(e), Aspects.Massing | Aspects.Facade | Aspects.Roof))];

    public override IEnumerable<ContentSourceTrace> TraceSources(BuildContext context, LodLevel lod)
    {
        foreach (var block in Blocks)
        {
            var contours = new List<PolygonRings> { Plan(context.World, block.Entity, block.Part) };
            if (block.Entity == S.EntityC)
                contours.AddRange(new[] { S.LinkAB, S.LinkBC, S.Tower }.Select(part => Plan(context.World, S.EntityC, part)));
            yield return SiteSourceTrace.Build(context, lod, $"{Id}/{block.Key}", new(block.Entity),
                b => Build(b, block.Entity), c => Collide(c, block.Entity), contours);
        }
    }

    internal static GridStyle Style(int upper) => new(L.Stone, L.Trim, L.Stone, L.Glass, L.Metal, L.RoofSlab)
    {
        Bay = S.Bay,
        Pier = S.Pier,
        PierDepth = S.PierDepth,
        BandDepth = S.PierDepth - 0.15f,
        GroundStorey = S.Storey,
        Storey = S.Storey,
        Storeys = upper,
        GroundSill = S.Sill,
        GroundWindow = S.Window,
        Sill = S.Sill,
        Window = S.Window,
        PlinthHeight = 0.6f,
        Cornice = 0.8f,
        CorniceProjection = 0.45f,
        Parapet = 0.9f,
        Band = L.DarkGrey,
        BandHeight = 1.0f,
        BandLift = 0.25f,
        BandProud = 0.2f,
        Louvres = 4,
    };

    // ---- Plan ----

    internal static PolygonRings Plan(IWorldModel world, string entity, string part)
    {
        if (!world.Features.TryGetValue(new(entity), out var feature))
            throw new InvalidOperationException($"六号科研楼 entity {entity} is missing from the world model.");
        return feature.Parts.FirstOrDefault(p => p.SourceId == part) is { } src && PlanGeometry.FacadeGeometryOf(src) is PolygonGeometry g
            ? g.Rings
            : throw new InvalidOperationException($"六号科研楼 part {part} is missing or not a single polygon.");
    }

    /// <summary>Plans of every closed volume of the building (blocks, A–B link, tower), for hiding faces that touch another.</summary>
    internal static IReadOnlyList<PolygonRings> Volumes(IWorldModel world) =>
        [.. Blocks.Select(k => Plan(world, k.Entity, k.Part)), Plan(world, S.EntityC, S.LinkAB), Plan(world, S.EntityC, S.Tower)];

    /// <summary>Inside a plan's outer ring and outside its courtyard holes.</summary>
    internal static bool In(PolygonRings plan, Vector2 p) => PlanGeometry.InsideByCrossings(plan.Outer, p) && !plan.Holes.Any(h => PlanGeometry.InsideByCrossings(h, p));

    internal static float BaseY(BuildContext ctx, string entity) => ctx.World.Terrain.PadY(new(entity));

    /// <summary>
    /// Hidden where a face lies against another volume; Blank when too short for a bay; otherwise plain windows. A base edge
    /// (set back by <paramref name="setBack"/>) looks past the stone line.
    /// </summary>
    internal static GridEdge Dress(GridEdgeInfo e, IReadOnlyList<PolygonRings> volumes, float setBack = 0f)
    {
        if (new[] { 0.1f, 0.5f, 0.9f }.All(t => volumes.Any(r => In(r, Vector2.Lerp(e.A, e.B, t) + e.Frame.W * (setBack + 0.6f)))))
            return new(EdgeKind.Hidden);
        return new(e.Length < 4.5f ? EdgeKind.Blank : EdgeKind.Plain);
    }

    /// <summary>Local x of a door point on this outer face of the stone line, when it lies on it.</summary>
    internal static float? On(GridEdgeInfo e, Vector2 door, float halfWidth)
    {
        var local = e.Frame.Local(door);
        return MathF.Abs(local.Y) < 0.6f && local.X > halfWidth && local.X < e.Length - halfWidth ? local.X : null;
    }

    /// <summary>The outer face carrying the east door, and the door's local x on it.</summary>
    internal static (GridEdgeInfo Face, float X) EastFace(PolygonRings c)
    {
        foreach (var (e, _, _) in GridFacade.Edges(c.Outer))
            if (On(e, S.EastDoor, S.VestibuleWidth * 0.5f) is { } x) return (e, x);
        throw new InvalidOperationException("The C east door does not lie on C's stone line.");
    }

    // ---- Window grids: one layout for geometry, collision and declarations ----

    private static GridNumbering Numbering(Block k) => new(new(k.Entity), k.Key, 0, W.Research, W.Lab) { AltCard = W.Office, AltShare = W.OfficeShare };

    public override IEnumerable<(FeatureId Feature, WindowGridSpec Grid)> WindowGrids(IWorldModel world)
    {
        var volumes = Volumes(world);
        foreach (var k in Blocks)
        {
            var layout = GridFacade.Layout(Plan(world, k.Entity, k.Part), Style(k.Upper), e => Dress(e, volumes));
            if (layout.Columns > 0) yield return (new(k.Entity), new(k.Key, layout.Columns, layout.Rows));
        }
    }

    public override void DescribeWindows(BuildContext ctx, ICollection<WindowFacadeDecl> facades, ICollection<WindowDecl> windows)
    {
        var volumes = Volumes(ctx.World);
        foreach (var k in Blocks)
        {
            var plan = Plan(ctx.World, k.Entity, k.Part);
            if (GridFacade.Layout(plan, Style(k.Upper), e => Dress(e, volumes)).Columns == 0) continue;
            GridFacade.Declare(facades, plan, BaseY(ctx, k.Entity) + S.BaseHeight, Style(k.Upper), e => Dress(e, volumes), Numbering(k),
                WindowNumbers.Required(ctx, new(k.Entity), k.Key));
        }
    }

    // ---- Build ----

    public override void Build(SiteBuilder b) => Build(b, null);

    private static void Build(SiteBuilder b, string? entity)
    {
        var world = b.Context.World;
        var volumes = Volumes(world);
        foreach (var k in Blocks.Where(k => entity is null || k.Entity == entity))
        {
            var plan = Plan(world, k.Entity, k.Part);
            var y = BaseY(b.Context, k.Entity);
            var style = Style(k.Upper);
            var windowed = GridFacade.Layout(plan, style, e => Dress(e, volumes)).Columns > 0;
            GridFacade.Build(b, plan, y + S.BaseHeight, style, e => Dress(e, volumes), windowed ? Numbering(k) : null);
            GlassBase(b, plan, y, style, volumes);
            var roof = y + S.BaseHeight + style.RoofTop;
            Plant(b, S.Plants[Blocks.ToList().IndexOf(k)], roof);
        }
        if (entity is not null && entity != S.EntityC)
        {
            foreach (var at in S.ShutDoors.Where(at => ShutEntity(b.Context.World, at) == entity)) ShutDoor(b, at);
            return;
        }
        var c = Plan(world, S.EntityC, S.PartC);
        var yc = BaseY(b.Context, S.EntityC);
        var (face, door) = EastFace(c);
        Vestibule(b, face, door, yc);
        Sign(b, face, yc + S.BaseHeight + Style(S.UpperC).Top);
        foreach (var at in S.ShutDoors.Where(at => entity is null || ShutEntity(world, at) == entity)) ShutDoor(b, at);
        LinkAB(b, Plan(world, S.EntityC, S.LinkAB).Outer, yc);
        LinkBC(b, world, yc);
        Tower(b, Plan(world, S.EntityC, S.Tower).Outer, yc);
    }

    // ---- The two-storey glass base set back under the stone ----

    /// <summary>
    /// A ring moved <paramref name="by"/> metres into the block along mitred bisectors; a courtyard hole grows into the block.
    /// Positive-area rings have their outside on the right of the path, so their block lies on the left.
    /// </summary>
    internal static Vector2[] Inset(IReadOnlyList<Vector2> ring, float by, bool hole)
    {
        var n = ring.Count;
        var left = PlanGeometry.SignedArea(ring) > 0 != hole;
        var result = new Vector2[n];
        for (var i = 0; i < n; i++)
        {
            Vector2 p = ring[(i + n - 1) % n], q = ring[i], r = ring[(i + 1) % n];
            Vector2 na = Inward(q - p, left), nb = Inward(r - q, left);
            var dot = na.X * nb.X + na.Y * nb.Y;
            result[i] = dot > -0.8f ? q + (na + nb) * (by / (1f + dot)) : q + na * by;
        }
        return result;
    }

    private static Vector2 Inward(Vector2 d, bool left)
    {
        var length = MathF.Sqrt(d.X * d.X + d.Y * d.Y);
        var normal = new Vector2(-d.Y / length, d.X / length);
        return left ? normal : -normal;
    }

    internal static PolygonRings BasePlan(PolygonRings plan) =>
        new(Inset(plan.Outer, S.Recess, false), [.. plan.Holes.Select(h => (IReadOnlyList<Vector2>)Inset(h, S.Recess, true))]);

    /// <summary>
    /// The base: dark glass on the set-back ring up to the stone's soffit, a dark kerb at its foot, the first-floor slab line
    /// behind the glass, slim mullions (L0), a stone soffit over the recess, and square stone columns in the stone line under
    /// every second pier (and at every convex corner).
    /// </summary>
    private static void GlassBase(SiteBuilder b, PolygonRings plan, float y, GridStyle style, IReadOnlyList<PolygonRings> volumes)
    {
        var soffit = y + S.BaseHeight - 0.45f;
        var inner = BasePlan(plan);
        var coarse = b.Lod is LodLevel.L3 or LodLevel.L4;
        b.Add(PartGeometryKernel.Extrude(inner, soffit - y + 0.1f, coarse ? L.DarkGrey : L.Glass, y - 0.1f), Matrix4x4.Identity);
        b.Add(PartGeometryKernel.Extrude(plan, 0.45f, L.Trim, soffit), Matrix4x4.Identity);
        if (coarse) return;
        // Grey paving under the overhang, from the set-back glass out to the stone line (门前灰色铺装, §1.4), one slab per face
        // carried past both corners so convex and reflex corners are covered.
        foreach (var (e, _, _) in GridFacade.Edges(plan))
            if (Dress(e, volumes).Kind != EdgeKind.Hidden)
                GridFacade.Box(b, e.Frame, -S.Recess, e.Length + S.Recess, y - 0.1f, y + S.PavingTop + 0.01f, -S.Recess - 0.1f, 0.05f, L.Granite, 0);
        var doors = S.ShutDoors.Append(S.EastDoor).ToArray();
        foreach (var (e, _, _) in GridFacade.Edges(inner))
        {
            if (Dress(e, volumes, S.Recess).Kind == EdgeKind.Hidden) continue;
            var f = e.Frame;
            GridFacade.Box(b, f, 0, e.Length, y - 0.1f, y + 0.3f, -0.05f, 0.12f, L.DarkGrey, 0);
            GridFacade.Box(b, f, 0, e.Length, y + S.BaseStorey - 0.25f, y + S.BaseStorey + 0.2f, -0.05f, 0.1f, L.DarkGrey, 0);
            if (b.Detail != SiteDetail.Full) continue;
            var count = Math.Max(1, (int)MathF.Ceiling(e.Length / S.MullionPitch));
            for (var i = 1; i < count; i++)
            {
                var x = e.Length * i / count;
                if (doors.Any(d => Vector2.Distance(f.P(x, 0), d) < S.VestibuleWidth * 0.5f + 0.6f)) continue;
                GridFacade.Box(b, f, x - 0.035f, x + 0.035f, y + 0.3f, soffit, 0, 0.12f, L.Metal, 0);
            }
            GridFacade.Box(b, f, 0, e.Length, y + 3.1f, y + 3.17f, 0, 0.1f, L.Metal, 0);
        }
        foreach (var (x, f) in Columns(plan, style, volumes))
        {
            var bevel = b.Detail == SiteDetail.Full ? 0.04f : 0f;
            var z = S.PierDepth * 0.5f - 0.1f;
            var h = S.ColumnSize * 0.5f;
            GridFacade.Box(b, f, x - h, x + h, y - 0.1f, soffit, z - h, z + h, L.Stone, bevel);
            GridFacade.Box(b, f, x - h - 0.06f, x + h + 0.06f, y - 0.1f, y + 0.45f, z - h - 0.06f, z + h + 0.06f, L.DarkGrey, bevel * 0.5f);
        }
    }

    /// <summary>Base columns: local x on each built outer face's frame, under every <see cref="S.ColumnEvery"/>th pier, clear of the doors.</summary>
    internal static IEnumerable<(float X, PlanFrame Frame)> Columns(PolygonRings plan, GridStyle style, IReadOnlyList<PolygonRings> volumes)
    {
        var doors = S.ShutDoors.Append(S.EastDoor).ToArray();
        foreach (var run in GridFacade.Layout(plan, style, e => Dress(e, volumes)).Runs)
        {
            if (run.Edge.Kind == EdgeKind.Hidden) continue;
            var f = run.Info.Frame;
            var xs = run.Bays == 0
                ? new[] { S.ColumnSize * 0.5f, run.Info.Length - S.ColumnSize * 0.5f }
                : Enumerable.Range(0, run.Bays + 1).Where(i => i % S.ColumnEvery == 0 || i == run.Bays).Select(i => i * run.Pitch + S.Pier * 0.5f).ToArray();
            foreach (var x in xs)
                if (!doors.Any(d => Vector2.Distance(f.P(x, 0), d) < S.VestibuleWidth * 0.5f + 0.8f)) yield return (x, f);
        }
    }

    // ---- C 区东门: the open entrance ----

    /// <summary>
    /// A glass vestibule from the set-back glass out to the stone line under the soffit: two glazed cheeks, a stone roof
    /// with a fascia, a transom light over the open doors and side lights either side; a granite threshold apron in front.
    /// </summary>
    private static void Vestibule(SiteBuilder b, GridEdgeInfo face, float x, float y)
    {
        var f = face.Frame;
        float hw = S.VestibuleWidth * 0.5f, top = y + S.VestibuleHeight, back = -S.Recess - 0.2f;
        var bevel = b.Detail == SiteDetail.Full ? 0.04f : 0f;
        // Roof slab and fascia, and the cheeks' stone frames at the stone line.
        GridFacade.Box(b, f, x - hw - 0.3f, x + hw + 0.3f, top, top + 0.55f, back, 0.35f, L.Trim, bevel);
        foreach (var s in new[] { -1f, 1f })
            GridFacade.Box(b, f, x + s * hw - 0.2f, x + s * hw + 0.2f, y - 0.1f, top, -0.25f, 0.25f, L.Metal, bevel * 0.5f);
        if (b.Detail == SiteDetail.Mass) return;
        foreach (var s in new[] { -1f, 1f })
        {
            // Cheek glazing, running back to the set-back glass.
            var cheek = PlanFrame.Toward(f.P(x + s * hw, back), f.P(x + s * hw, -0.2f));
            b.Place(PartCatalog.CurtainWall, new WindowParams(-back - 0.2f, top - y - 0.1f, 0.1f, 2, 2), cheek.At((-back - 0.2f) * 0.5f, y, 0));
        }
        var side = hw - S.DoorWidth * 0.5f - 0.2f;
        foreach (var s in new[] { -1f, 1f })
            b.Place(PartCatalog.CurtainWall, new WindowParams(side, top - y - 0.1f, 0.1f, 1, 2), f.AtReversed(x + s * (S.DoorWidth * 0.5f + 0.1f + side * 0.5f), y, 0));
        b.Place(PartCatalog.CurtainWall, new WindowParams(S.DoorWidth, top - y - S.DoorHeight - 0.1f, 0.1f, 2, 1), f.AtReversed(x, y + S.DoorHeight, 0));
        // The doors stand open into the vestibule (WLD-INTERIORS: an open entrance's leaves are fixed open).
        b.Place(PartCatalog.DoorGlass, new DoorParams(S.DoorWidth, S.DoorHeight, 0.08f, 90f, 2) { Foot = FootMat.Glass }, f.AtReversed(x, y, 0));
        GridFacade.Box(b, f, x - S.ApronWidth * 0.5f, x + S.ApronWidth * 0.5f, y - 0.1f, y + S.PavingTop + 0.03f, -0.1f, S.ApronDepth, L.Granite, 0.02f);
    }

    // ---- Shut entrances ----

    private static void ShutDoor(SiteBuilder b, Vector2 at)
    {
        if (b.Detail == SiteDetail.Mass) return;
        var (face, x, y) = ShutFace(b.Context, at);
        var f = face.Frame;
        var z = -S.Recess;
        b.Place(PartCatalog.DoorGlass, new DoorParams(S.ShutDoorWidth, S.ShutDoorHeight, 0.08f, 0f, 2) { Foot = FootMat.Glass }, f.AtReversed(x, y, z));
        GridFacade.Box(b, f, x - S.ShutDoorWidth * 0.5f - 0.12f, x + S.ShutDoorWidth * 0.5f + 0.12f, y + S.ShutDoorHeight, y + S.ShutDoorHeight + 0.18f, z - 0.05f, z + 0.12f, L.Metal, 0);
        // The lobby card behind the shut glass (WLD-INTERIORS): hall light, gate standby lamps, floor index.
        b.Place(PartCatalog.InteriorCard, new PanelParams(S.ShutDoorWidth + 1.6f, S.ShutDoorHeight + 0.6f, 0.06f) { Variation = 1 }, f.AtReversed(x, y, z - 1.4f));
    }

    private static string ShutEntity(IWorldModel world, Vector2 at) =>
        Blocks.First(k => GridFacade.Edges(Plan(world, k.Entity, k.Part).Outer).Any(edge => On(edge.Item1, at, S.ShutDoorWidth) is not null)).Entity;

    /// <summary>The block face carrying a shut door, the door's local x on it and the block's base.</summary>
    internal static (GridEdgeInfo Face, float X, float Y) ShutFace(BuildContext ctx, Vector2 at)
    {
        foreach (var k in Blocks)
            foreach (var (e, _, _) in GridFacade.Edges(Plan(ctx.World, k.Entity, k.Part).Outer))
                if (On(e, at, S.ShutDoorWidth) is { } x) return (e, x, BaseY(ctx, k.Entity));
        throw new InvalidOperationException($"Shut door {at} does not lie on a 六号 stone line.");
    }

    // ---- Crown sign: red emblem and silver characters ----

    /// <summary>A rigid placement on a face: local X along the face, local Y out of it, local Z down (an upright disc's plane).</summary>
    private static Matrix4x4 Upright(PlanFrame f, float x, float y, float z)
    {
        var p = f.P(x, z);
        return new(f.U.X, 0, f.U.Y, 0, f.W.X, 0, f.W.Y, 0, 0, -1, 0, 0, p.X, y, p.Y, 1);
    }

    private static void Sign(SiteBuilder b, GridEdgeInfo face, float top)
    {
        var f = face.Frame;
        var x = face.Length * 0.5f;
        float hw = S.SignWidth * 0.5f, y0 = top - S.SignDrop, y1 = top + S.SignRise, z1 = S.SignProud;
        var bevel = b.Detail == SiteDetail.Full ? 0.05f : 0f;
        GridFacade.Box(b, f, x - hw, x + hw, y0, y1, 0, z1, L.Stone, bevel);
        GridFacade.Box(b, f, x - hw - 0.12f, x + hw + 0.12f, y1, y1 + 0.22f, -0.1f, z1 + 0.12f, L.Trim, bevel);
        GridFacade.Box(b, f, x - hw - 0.08f, x + hw + 0.08f, y0 - 0.25f, y0, -0.1f, z1 + 0.08f, L.Trim, bevel);
        if (b.Detail == SiteDetail.Mass) return;
        // The emblem: a thin outer ring, an octagon and circuit traces ending in round pads, single colour (楼顶红).
        var r = S.EmblemDiameter * 0.5f;
        var centre = Upright(f, x, y1 - 0.45f - r, z1);
        var d = S.EmblemDepth;
        b.Add(Shapes.Ring(Vector2.Zero, r - 0.09f, 0.18f, d, 48, L.Red), centre);
        b.Add(Shapes.Ring(Vector2.Zero, r * 0.66f, 0.15f, d, 8, L.Red), centre);
        b.Add(Shapes.Ring(Vector2.Zero, r * 0.17f, 0.12f, d, 16, L.Red), centre);
        for (var i = 0; i < 6; i++)
        {
            var a = MathF.PI / 6f + i * MathF.PI / 3f;
            float c = MathF.Cos(a), s = MathF.Sin(a);
            var length = r * 0.38f;
            var mid = r * (0.23f + 0.19f);
            var at = new Matrix4x4(c, 0, s, 0, 0, 1, 0, 0, -s, 0, c, 0, c * mid, 0, s * mid, 1);
            b.Add(PartGeometryKernel.BeveledBox(length, d, 0.1f, 0, L.Red), GeometryPlacement.Multiply(at, centre));
            var pad = new Matrix4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, c * r * 0.62f, 0, s * r * 0.62f, 1);
            b.Add(Shapes.Shaft(0.11f, 0.11f, d, 12, L.Red), GeometryPlacement.Multiply(pad, centre));
        }
        // Text hook: left blank until the scene text atlas (contract-26) arrives. Then add here, under the emblem, the silver
        // line "基础与前沿研究院" (SignCharacters of CharacterSize, CharacterGap, Look.Silver) and one English line
        // (EnglishHeight, EnglishLength) below it. No placeholder strokes are drawn: scene text is real Chinese or nothing.
    }

    // ---- Links and tower ----

    /// <summary>The A–B link: two storeys of dark glass with slim mullions, a stone fascia and coping.</summary>
    private static void LinkAB(SiteBuilder b, IReadOnlyList<Vector2> ring, float y)
    {
        var top = y + S.BaseHeight;
        var plan = new PolygonRings(ring);
        b.Add(PartGeometryKernel.Extrude(plan, top - S.LinkFascia - y + 0.1f, b.Lod is LodLevel.L3 or LodLevel.L4 ? L.DarkGrey : L.Glass, y - 0.1f), Matrix4x4.Identity);
        b.Add(PartGeometryKernel.Extrude(plan, S.LinkFascia, L.Stone, top - S.LinkFascia), Matrix4x4.Identity);
        if (b.Detail == SiteDetail.Mass) return;
        b.Add(PartGeometryKernel.Sweep(PartGeometryKernel.BeveledRectangleProfile(0.5f, 0.16f, 0.03f), PlanMath.OutsideRight(ring), L.Trim, closed: true, baseY: top), Matrix4x4.Identity);
        if (b.Detail != SiteDetail.Full) return;
        foreach (var (e, _, _) in GridFacade.Edges(ring))
        {
            var count = Math.Max(1, (int)MathF.Ceiling(e.Length / S.MullionPitch));
            for (var i = 1; i < count; i++)
                GridFacade.Box(b, e.Frame, e.Length * i / count - 0.035f, e.Length * i / count + 0.035f, y, top - S.LinkFascia, 0, 0.1f, L.Metal, 0);
            GridFacade.Box(b, e.Frame, 0, e.Length, y + S.BaseStorey - 0.2f, y + S.BaseStorey + 0.15f, 0, 0.1f, L.DarkGrey, 0);
        }
    }

    /// <summary>
    /// The B–C link (§1.6): an open covered bridge on stone piers over the drive-through: a deck at the first-floor line with
    /// glass railings, a roof at the base top, and narrow covered decks across to B's and C's faces at the same level.
    /// </summary>
    private static void LinkBC(SiteBuilder b, IWorldModel world, float y)
    {
        var ring = Plan(world, S.EntityC, S.LinkBC).Outer;
        var plan = new PolygonRings(ring);
        float deck = y + S.LinkDeck, roof = y + S.BaseHeight;
        var bevel = b.Detail == SiteDetail.Full ? 0.04f : 0f;
        b.Add(PartGeometryKernel.Extrude(plan, 0.7f, L.Trim, deck - 0.7f), Matrix4x4.Identity);
        b.Add(PartGeometryKernel.Extrude(plan, 0.55f, L.Trim, roof - 0.55f), Matrix4x4.Identity);
        foreach (var (e, _, _) in GridFacade.Edges(ring))
        {
            var f = e.Frame;
            var h = S.LinkPier * 0.5f;
            foreach (var x in new[] { h, e.Length * 0.5f })
                GridFacade.Box(b, f, x - h, x + h, y - 0.1f, roof - 0.55f, -S.LinkPier, 0, L.Stone, bevel);
            GridFacade.Box(b, f, 0, e.Length, deck - 0.75f, deck + 0.05f, -0.05f, 0.12f, L.Stone, bevel);
            GridFacade.Box(b, f, -0.15f, e.Length + 0.15f, roof - 0.6f, roof + 0.1f, -0.05f, 0.3f, L.Stone, bevel);
            if (b.Detail != SiteDetail.Mass) Rail(b, f, S.LinkPier + 0.2f, e.Length - S.LinkPier - 0.2f, deck);
        }
        foreach (var (from, to) in Bridges(world))
        {
            var f = PlanFrame.Toward(from, to);
            var length = Vector2.Distance(from, to);
            GridFacade.Box(b, f, 0, length, deck - 0.7f, deck, -1.7f, 1.7f, L.Trim, bevel);
            GridFacade.Box(b, f, 0, length, roof - 0.5f, roof - 0.1f, -2.0f, 2.0f, L.Trim, bevel);
            if (b.Detail == SiteDetail.Mass) continue;
            foreach (var s in new[] { -1f, 1f })
            {
                var side = PlanFrame.Toward(f.P(0, s * 1.6f), f.P(length, s * 1.6f));
                Rail(b, side, 0, length, deck);
                GridFacade.Box(b, side, length * 0.5f - 0.2f, length * 0.5f + 0.2f, deck, roof - 0.5f, -0.2f, 0.2f, L.Metal, 0);
            }
        }
    }

    /// <summary>The two narrow decks: from the link's centre line out to the facing faces of B and C.</summary>
    internal static IEnumerable<(Vector2 From, Vector2 To)> Bridges(IWorldModel world)
    {
        var ring = Plan(world, S.EntityC, S.LinkBC).Outer;
        var centre = PlanMath.Centroid(ring);
        var across = new Vector2(-S.Axis.Y, S.Axis.X);
        foreach (var (entity, part) in new[] { (S.EntityB, S.PartB), (S.EntityC, S.PartC) })
        {
            var target = Plan(world, entity, part).Outer;
            foreach (var sign in new[] { -1f, 1f })
            {
                var hit = Cast(ring, centre, across * sign);
                var far = Cast(target, centre, across * sign);
                if (hit is { } h && far is { } t && t > h + 0.5f && t - h < 30f)
                    yield return (centre + across * sign * h, centre + across * sign * t);
            }
        }
    }

    /// <summary>Distance along a ray to the nearest crossing of a ring's edges, if any.</summary>
    private static float? Cast(IReadOnlyList<Vector2> ring, Vector2 origin, Vector2 dir)
    {
        float? best = null;
        for (var i = 0; i < ring.Count; i++)
        {
            Vector2 a = ring[i], e = ring[(i + 1) % ring.Count] - a;
            var den = dir.X * e.Y - dir.Y * e.X;
            if (MathF.Abs(den) < 1e-6f) continue;
            var w = a - origin;
            var t = (w.X * e.Y - w.Y * e.X) / den;
            var u = (w.X * dir.Y - w.Y * dir.X) / den;
            if (t > 0.01f && u >= 0 && u <= 1 && (best is null || t < best)) best = t;
        }
        return best;
    }

    /// <summary>Glass railing from local x0 to x1 on a frame's line: glass panes, posts, an aluminium handrail.</summary>
    private static void Rail(SiteBuilder b, PlanFrame f, float x0, float x1, float deck)
    {
        if (x1 - x0 < 0.5f) return;
        GridFacade.Box(b, f, x0, x1, deck + 0.08f, deck + S.RailHeight - 0.08f, -0.02f, 0.02f, L.Glass, 0);
        GridFacade.Box(b, f, x0, x1, deck + S.RailHeight - 0.08f, deck + S.RailHeight, -0.04f, 0.04f, L.Metal, 0);
        if (b.Detail != SiteDetail.Full) return;
        var n = Math.Max(1, (int)MathF.Ceiling((x1 - x0) / 1.5f));
        for (var i = 0; i <= n; i++)
        {
            var x = x0 + (x1 - x0) * i / n;
            GridFacade.Box(b, f, x - 0.03f, x + 0.03f, deck, deck + S.RailHeight, -0.03f, 0.03f, L.Metal, 0);
        }
    }

    /// <summary>The north tower: a stone stair core with a dark plinth, a tall glazed slot on each long face and a coping.</summary>
    private static void Tower(SiteBuilder b, IReadOnlyList<Vector2> ring, float y)
    {
        var plan = new PolygonRings(ring);
        b.Add(PartGeometryKernel.Extrude(plan, S.TowerHeight - 0.4f + 0.1f, L.Stone, y - 0.1f), Matrix4x4.Identity);
        b.Add(PartGeometryKernel.Sweep(PartGeometryKernel.BeveledRectangleProfile(0.7f, 0.4f, 0.04f), PlanMath.OutsideRight(ring), L.Trim, closed: true, baseY: y + S.TowerHeight - 0.4f), Matrix4x4.Identity);
        b.Add(PartGeometryKernel.Sweep(PartGeometryKernel.BeveledRectangleProfile(0.3f, 0.7f, 0.03f), PlanMath.OutsideRight(ring), L.DarkGrey, closed: true, baseY: y - 0.1f), Matrix4x4.Identity);
        if (b.Detail == SiteDetail.Mass) return;
        foreach (var (e, _, _) in GridFacade.Edges(ring))
        {
            if (e.Length < 12f) continue;
            var x = e.Length * 0.5f;
            GridFacade.Box(b, e.Frame, x - 0.8f, x + 0.8f, y + 1.2f, y + S.TowerHeight - 2.0f, -0.05f, 0.04f, L.Glass, 0);
            GridFacade.Box(b, e.Frame, x - 1.0f, x + 1.0f, y + S.TowerHeight - 2.0f, y + S.TowerHeight - 1.8f, -0.05f, 0.12f, L.Trim, 0);
        }
    }

    /// <summary>A fenced plant enclosure on a roof, set out along the blocks' axis (屋顶设备在围挡里).</summary>
    private static void Plant(SiteBuilder b, Vector2 at, float roof)
    {
        if (b.Detail == SiteDetail.Mass) return;
        var local = Frame.Local(at);
        float hx = S.PlantSize.X * 0.5f, hz = S.PlantSize.Y * 0.5f;
        foreach (var s in new[] { -1f, 1f })
        {
            b.Place(PartCatalog.EquipmentFence, new RailingParams(S.PlantSize.X, S.PlantFence, 0.06f, 1.4f) { Variation = s > 0 ? 1 : 0 }, Frame.At(local.X, roof, local.Y + s * hz));
            b.Place(PartCatalog.EquipmentFence, new RailingParams(S.PlantSize.Y, S.PlantFence, 0.06f, 1.4f), Frame.AtTurned(local.X + s * hx, roof, local.Y));
        }
        foreach (var dx in new[] { -2.4f, 0f, 2.4f })
            b.Place(PartCatalog.HeatPump, new EquipmentParams(1.6f, 1.9f, 0.9f), Frame.At(local.X + dx, roof, local.Y));
    }

    // ---- Collision ----

    public override void Collide(SiteColliders c) => Collide(c, null);

    private static void Collide(SiteColliders c, string? entity)
    {
        var world = c.Context.World;
        var volumes = Volumes(world);
        var east = (Face: default(GridEdgeInfo), X: 0f);
        foreach (var k in Blocks.Where(k => entity is null || k.Entity == entity))
        {
            var plan = Plan(world, k.Entity, k.Part);
            var y = BaseY(c.Context, k.Entity);
            var style = Style(k.Upper);
            GridFacade.Collide(c, plan, y + S.BaseHeight, style, e => Dress(e, volumes), FootMat.StonePaving);
            if (k.Entity == S.EntityC) east = EastFace(plan);
            // The set-back glass at walking height: open between the vestibule cheeks, DoorClosed across each shut door.
            foreach (var (e, _, _) in GridFacade.Edges(BasePlan(plan)))
            {
                if (Dress(e, volumes, S.Recess).Kind == EdgeKind.Hidden) continue;
                var cuts = new List<(float U0, float U1, SurfaceTag Tag)>();
                var door = e.Frame.Local(S.EastDoor);
                if (k.Entity == S.EntityC && MathF.Abs(door.Y - S.Recess) < 1.2f && door.X > 0 && door.X < e.Length)
                    cuts.Add((door.X - S.VestibuleWidth * 0.5f, door.X + S.VestibuleWidth * 0.5f, SurfaceTag.Ground));
                foreach (var shut in S.ShutDoors)
                {
                    var p = e.Frame.Local(shut);
                    if (MathF.Abs(p.Y - S.Recess) < 1.2f && p.X > 0 && p.X < e.Length)
                        cuts.Add((p.X - S.ShutDoorWidth * 0.5f, p.X + S.ShutDoorWidth * 0.5f, SurfaceTag.DoorClosed));
                }
                foreach (var (u0, u1) in GridFacade.Open(0, e.Length, cuts.Select(cut => (cut.U0, cut.U1))))
                    c.Wall(e.Frame.P(u0, 0.1f), e.Frame.P(u1, 0.1f), 0.15f, y, y + S.BaseHeight, SurfaceTag.Wall, FootMat.Glass);
                foreach (var cut in cuts.Where(cut => cut.Tag == SurfaceTag.DoorClosed))
                    c.Wall(e.Frame.P(cut.U0, 0.1f), e.Frame.P(cut.U1, 0.1f), 0.15f, y, y + S.BaseHeight, SurfaceTag.DoorClosed, FootMat.Glass);
            }
            foreach (var (x, f) in Columns(plan, style, volumes))
                c.Box(f.At(x, y + S.BaseHeight * 0.5f, S.PierDepth * 0.5f - 0.1f), new(S.ColumnSize * 0.5f + 0.06f, S.BaseHeight * 0.5f, S.ColumnSize * 0.5f + 0.06f), SurfaceTag.Wall, FootMat.StonePaving);
        }
        if (entity is not null && entity != S.EntityC) return;
        // Vestibule cheeks; the doorway between them stays open for WP-31b's hall (EntranceDecl.Open).
        var yc = BaseY(c.Context, S.EntityC);
        var vf = east.Face.Frame;
        foreach (var s in new[] { -1f, 1f })
            c.Wall(vf.P(east.X + s * S.VestibuleWidth * 0.5f, -S.Recess), vf.P(east.X + s * S.VestibuleWidth * 0.5f, 0), 0.15f, yc, yc + S.VestibuleHeight, SurfaceTag.Wall, FootMat.Glass);
        // Links and tower: the A–B link and the tower are closed volumes; the B–C link stands on its piers.
        foreach (var ring in new[] { Plan(world, S.EntityC, S.LinkAB).Outer, Plan(world, S.EntityC, S.Tower).Outer })
            foreach (var (e, _, _) in GridFacade.Edges(ring))
                c.Wall(e.Frame.P(0, -0.15f), e.Frame.P(e.Length, -0.15f), 0.15f, yc, yc + S.TowerHeight, SurfaceTag.Wall, FootMat.StonePaving);
        foreach (var (e, _, _) in GridFacade.Edges(Plan(world, S.EntityC, S.LinkBC).Outer))
            foreach (var x in new[] { S.LinkPier * 0.5f, e.Length * 0.5f })
                c.Box(e.Frame.At(x, yc + S.BaseHeight * 0.5f, -S.LinkPier * 0.5f), new(S.LinkPier * 0.5f, S.BaseHeight * 0.5f, S.LinkPier * 0.5f), SurfaceTag.Wall, FootMat.StonePaving);
    }

    // ---- Inspection probes (MAP-QSH-WEST Q4, Q8a, Q8c) ----

    public override IReadOnlyList<InspectionProbe> Probes
    {
        get
        {
            // Q4 (ground-floor close-up, K1): 0.5 m from C's set-back glass beside the east door, on the walk, looking along it.
            var door = S.EastDoor;
            var outward = PlanMath.Heading(77f);
            var along = new Vector2(-outward.Y, outward.X);
            var eye4 = door + outward * (-S.Recess + 0.5f) + along * (S.VestibuleWidth * 0.5f + 2.0f);
            var q4 = Probe("Q4", S.EntityC, new(eye4.X, 1.62f, eye4.Y), along, 6f, 0.5f);
            // Q8a (baseline, fly only): 2 m over A's parapet at the corner of its two wings, looking along the south wing.
            var corner = new Vector2(-840.26f, 133.59f) + S.Axis * 3f + new Vector2(-S.Axis.Y, S.Axis.X) * 3f;
            var q8a = Probe("Q8a", S.EntityA, new(corner.X, 41.5f + 2f, corner.Y), S.Axis, -20f, 2f);
            // Q8c (baseline, fly only): 2 m below ground at C's corner by the motto plaza, looking up at the corner.
            var c8 = new Vector2(-583.28f, 251.04f) + outward * 1.5f;
            var q8c = Probe("Q8c", S.EntityC, new(c8.X, -2f, c8.Y), -outward, 30f, 1.5f);
            return [q4, q8a, q8c];
        }
    }
}
