using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.World;
using Campus.Gen.Zones.Common;
using L = Campus.Gen.Zones.West.WestTune.Look;
using S = Campus.Gen.Zones.West.WestTune.Six;
using W = Campus.Gen.Zones.West.WestTune.Windows;

namespace Campus.Gen.Zones.West.Six;

/// <summary>六号科研楼的构建主流程、屋顶设备围挡与退台玻璃底座（含石柱）。</summary>
public sealed partial class SixSite
{
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
            var style = Style(k.Upper(world));
            var windowed = GridFacade.Layout(plan, style, e => Dress(e, volumes)).Columns > 0;
            GridFacade.Build(b, plan, y + S.BaseHeight, style, e => Dress(e, volumes), windowed ? Numbering(k) : null);
            GlassBase(b, plan, y, style, volumes);
            var roof = y + S.BaseHeight + style.RoofTop;
            Plant(b, S.Plants(world)[Blocks.ToList().IndexOf(k)], roof);
        }
        if (entity is not null && entity != S.EntityC)
        {
            foreach (var at in S.ShutDoors(world).Where(at => ShutEntity(b.Context.World, at) == entity)) ShutDoor(b, at);
            return;
        }
        var c = Plan(world, S.EntityC, S.PartC);
        var yc = BaseY(b.Context, S.EntityC);
        var (face, door) = EastFace(world, EastDoor(world));
        Vestibule(b, face, door, yc);
        Sign(b, face, yc + S.BaseHeight + Style(S.UpperC).Top);
        foreach (var at in S.ShutDoors(world).Where(at => entity is null || ShutEntity(world, at) == entity)) ShutDoor(b, at);
        LinkAB(b, Plan(world, S.EntityC, S.LinkAB).Outer, yc);
        LinkBC(b, world, yc);
        Tower(b, Plan(world, S.EntityC, S.Tower).Outer, yc);
    }

    // ---- The two-storey glass base set back under the stone ----

    /// <summary>
    /// A ring moved <paramref name="by"/> metres into the block along mitred bisectors; a courtyard hole grows into the block.
    /// Positive-area rings have their outside on the right of the path, so their block lies on the left.
    /// </summary>
    internal static Vector2[] Inset(IReadOnlyList<Vector2> ring, float by, bool hole) =>
        PlanGeometry.MitreOffset(ring, PlanGeometry.SignedArea(ring) > 0 != hole ? -by : by, closed: true);

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
        var east = EastDoor(b.Context.World);
        var doors = S.ShutDoors(b.Context.World).Append(east).ToArray();
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
        foreach (var (x, f) in Columns(plan, style, volumes, east, b.Context.World))
        {
            var bevel = b.Detail == SiteDetail.Full ? 0.04f : 0f;
            var z = S.PierDepth * 0.5f - 0.1f;
            var h = S.ColumnSize * 0.5f;
            GridFacade.Box(b, f, x - h, x + h, y - 0.1f, soffit, z - h, z + h, L.Stone, bevel);
            GridFacade.Box(b, f, x - h - 0.06f, x + h + 0.06f, y - 0.1f, y + 0.45f, z - h - 0.06f, z + h + 0.06f, L.DarkGrey, bevel * 0.5f);
        }
    }

    /// <summary>Base columns: local x on each built outer face's frame, under every <see cref="S.ColumnEvery"/>th pier, clear of the doors.</summary>
    internal static IEnumerable<(float X, PlanFrame Frame)> Columns(PolygonRings plan, GridStyle style, IReadOnlyList<PolygonRings> volumes, Vector2 eastDoor, IWorldModel world)
    {
        var doors = S.ShutDoors(world).Append(eastDoor).ToArray();
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
}
