using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Core;
using Campus.Gen.Sites;
using L = Campus.Gen.Maps.Qingshuihe.West.WestTune.Look;
using S = Campus.Gen.Maps.Qingshuihe.West.WestTune.Six;
using static Campus.Gen.Maps.Qingshuihe.West.Six.SixLayout;
using static Campus.Gen.Maps.Qingshuihe.West.Six.SixOpenings;

namespace Campus.Gen.Maps.Qingshuihe.West.Six;

/// <summary>六号科研楼的退台玻璃底座：玻璃皮、铺装、饰条、竖梃与石柱。</summary>
internal static class SixGlassBase
{
    internal static void Build(SiteBuilder b, string entity, PolygonRings plan, float y, GridStyle style, IReadOnlyList<PolygonRings> volumes)
    {
        var soffit = y + S.BaseHeight - 0.45f;
        var inner = BasePlan(plan);
        var coarse = b.Lod is LodLevel.L3 or LodLevel.L4;
        GlassSkin(b, entity, inner, y, soffit, volumes, coarse);
        b.Add(PartSolids.Extrude(plan, 0.45f, L.Trim, soffit), Matrix4x4.Identity);
        if (coarse) return;
        Paving(b, plan, y, volumes);
        var east = EastDoor(b.Context.World);
        Details(b, entity, inner, y, soffit, volumes, east);
        Columns(b, plan, y, soffit, style, volumes, east);
    }

    private static void GlassSkin(SiteBuilder b, string entity, PolygonRings inner, float y, float soffit, IReadOnlyList<PolygonRings> volumes, bool coarse)
    {
        // A facade is a skin. A closed extrusion also capped the open hall and both bridge doors.
        foreach (var (e, _, _) in GridFacade.Edges(inner))
        {
            if (Dress(e, volumes, S.Recess).Kind == EdgeKind.Hidden) continue;
            var openings = BaseOpenings(b.Context.World, entity, e);
            var levels = openings.SelectMany(o => new[] { y + o.Bottom, y + o.Top })
                .Append(y - 0.1f).Append(soffit).Where(h => h >= y - 0.1f && h <= soffit).Distinct().Order().ToArray();
            for (var j = 1; j < levels.Length; j++)
            {
                var middle = (levels[j - 1] + levels[j]) * 0.5f - y;
                foreach (var (x0, x1) in GridFacade.Open(0, e.Length,
                    openings.Where(o => middle >= o.Bottom && middle <= o.Top).Select(o => (o.From, o.To))))
                {
                    if (coarse)
                    {
                        GridFacade.Box(b, e.Frame, x0, x1, levels[j - 1], levels[j], -0.05f, 0.05f, L.DarkGrey, 0);
                        continue;
                    }
                    GlassPanel(b, e.Frame, x0, x1, levels[j - 1], levels[j]);
                }
            }
        }
    }

    private static void GlassPanel(SiteBuilder b, PlanFrame frame, float x0, float x1, float bottom, float top)
    {
        // One outward-facing sheet: a closed thin box adds an inward-facing front face,
        // so the glass shader never reaches its transparent back face from inside the hall.
        var mesh = new PartShapeWriter(L.Glass);
        float width = x1 - x0, height = top - bottom;
        var lowerLeft = mesh.Vertex(Vector3.Zero, Vector3.UnitZ, new(width, 0));
        var lowerRight = mesh.Vertex(new(width, 0, 0), Vector3.UnitZ, Vector2.Zero);
        var upperRight = mesh.Vertex(new(width, height, 0), Vector3.UnitZ, new(0, height));
        var upperLeft = mesh.Vertex(new(0, height, 0), Vector3.UnitZ, new(width, height));
        mesh.Triangle(lowerLeft, lowerRight, upperRight, Vector3.UnitZ);
        mesh.Triangle(lowerLeft, upperRight, upperLeft, Vector3.UnitZ);
        b.Add(mesh.Finish(), frame.At(x0, bottom, 0.05f));
    }

    private static void Paving(SiteBuilder b, PolygonRings plan, float y, IReadOnlyList<PolygonRings> volumes)
    {
        // Grey paving under the overhang, from the set-back glass out to the stone line (门前灰色铺装), one slab per face
        // carried past both corners so convex and reflex corners are covered.
        foreach (var (e, _, _) in GridFacade.Edges(plan))
            if (Dress(e, volumes).Kind != EdgeKind.Hidden)
                GridFacade.Box(b, e.Frame, -S.Recess, e.Length + S.Recess, y - 0.1f, y + S.PavingTop + 0.01f, -S.Recess - 0.1f, 0.05f, L.Granite, 0);
    }

    private static void Details(SiteBuilder b, string entity, PolygonRings inner, float y, float soffit, IReadOnlyList<PolygonRings> volumes, Vector2 east)
    {
        var doors = S.ShutDoors(b.Context.World).Append(east).ToArray();
        foreach (var (e, _, _) in GridFacade.Edges(inner))
        {
            if (Dress(e, volumes, S.Recess).Kind == EdgeKind.Hidden) continue;
            var f = e.Frame;
            var openings = BaseOpenings(b.Context.World, entity, e);
            void Band(float bottom, float top, float depth, ToonSpec material)
            {
                foreach (var (x0, x1) in GridFacade.Open(0, e.Length,
                    openings.Where(o => o.Bottom < top && o.Top > bottom).Select(o => (o.From, o.To))))
                    GridFacade.Box(b, f, x0, x1, y + bottom, y + top, -0.05f, depth, material, 0);
            }
            Band(-0.1f, 0.3f, 0.12f, L.DarkGrey);
            Band(S.BaseStorey - 0.25f, S.BaseStorey + 0.2f, 0.1f, L.DarkGrey);
            if (b.Detail != SiteDetail.Full) continue;
            var count = Math.Max(1, (int)MathF.Ceiling(e.Length / S.MullionPitch));
            for (var i = 1; i < count; i++)
            {
                var x = e.Length * i / count;
                if (doors.Any(d => Vector2.Distance(f.P(x, 0), d) < S.VestibuleWidth * 0.5f + 0.6f)) continue;
                if (openings.Any(o => x >= o.From && x <= o.To)) continue;
                GridFacade.Box(b, f, x - 0.035f, x + 0.035f, y + 0.3f, soffit, 0, 0.12f, L.Metal, 0);
            }
            Band(3.1f, 3.17f, 0.1f, L.Metal);
        }
    }

    private static void Columns(SiteBuilder b, PolygonRings plan, float y, float soffit, GridStyle style, IReadOnlyList<PolygonRings> volumes, Vector2 east)
    {
        foreach (var (x, f) in SixLayout.Columns(plan, style, volumes, east, b.Context.World))
        {
            var bevel = b.Detail == SiteDetail.Full ? 0.04f : 0f;
            var z = S.PierDepth * 0.5f - 0.1f;
            var h = S.ColumnSize * 0.5f;
            GridFacade.Box(b, f, x - h, x + h, y - 0.1f, soffit, z - h, z + h, L.Stone, bevel);
            GridFacade.Box(b, f, x - h - 0.06f, x + h + 0.06f, y - 0.1f, y + 0.45f, z - h - 0.06f, z + h + 0.06f, L.DarkGrey, bevel * 0.5f);
        }
    }
}
