using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Maps.Qingshuihe.Core.Framework;
using S = Campus.Gen.Maps.Qingshuihe.Core.CoreStyle;

namespace Campus.Gen.Maps.Qingshuihe.Core.Library;

/// <summary>
/// The library roof (MAP-QSH-CORE): the octagonal low-cone skylight over the atrium (port of the original
/// libraryRoof.ts: eight facets to a central ring, main ribs on the facet seams, a triangular steel grid between them,
/// clear glass), the curved metal roofs alternating with the flat ring roof, and the raised mechanical box north of
/// the skylight. Octagon vertices sit half-way between the face normals of <see cref="LibraryPlan.FaceAngle"/>.
/// </summary>
public static class LibraryRoof
{
    /// <summary>Plan direction of face k (outward normal), k = 0…7 counterclockwise in (x, z).</summary>
    public static Vector2 FaceDirection(LibraryPlan plan, int k)
    {
        var a = plan.FaceAngle + k * (MathF.Tau / 8f);
        return new(MathF.Cos(a), MathF.Sin(a));
    }

    /// <summary>The registered north roof facet: the mechanical box stands there.</summary>
    public static int NorthFace(LibraryPlan plan) => plan.NorthFacet;

    /// <summary>Skylight base vertex k on the curb top (circumradius <see cref="LibraryPlan.SkylightRadius"/>).</summary>
    public static Vector3 Vertex(LibraryPlan plan, int k, float radius, float y)
    {
        var a = plan.FaceAngle + MathF.Tau / 16f + k * (MathF.Tau / 8f);
        return new(plan.Centre.X + MathF.Cos(a) * radius, y, plan.Centre.Y + MathF.Sin(a) * radius);
    }

    public static float SkylightBaseY(LibraryPlan plan) => plan.RoofY + LibraryStyle.SkylightCurb;
    public static float SkylightApexY(LibraryPlan plan) => SkylightBaseY(plan) + plan.SkylightRadius * LibraryStyle.SkylightRiseRatio;

    /// <summary>
    /// Curb, glass facets, ribs, grid and hub. Steel goes to <paramref name="steel"/> (its own instance, fog-exempt with
    /// the glass, MAP-QSH-CORE), the stone curb to the shared sink.
    /// </summary>
    public static void Skylight(LibraryPlan plan, GeometrySink shared, GeometrySink steel, GlassSink glass, int column, LodLevel lod)
    {
        var r = plan.SkylightRadius;
        var y0 = SkylightBaseY(plan);
        var apex = new Vector3(plan.Centre.X, SkylightApexY(plan), plan.Centre.Y);
        Curb(plan, shared, lod);
        var metal = S.Metal(LibraryStyle.SkylightSteel);
        var rows = LibraryStyle.SkylightRows(lod);
        var hub = LibraryStyle.SkylightRingRadius / r;
        for (var k = 0; k < 8; k++)
        {
            var a = Vertex(plan, k, r, y0);
            var b = Vertex(plan, (k + 1) % 8, r, y0);
            var n = VectorMath.Unit(Vector3.Cross(b - a, apex - a), 1e-8);
            if (n.Y < 0) n = -n;
            glass.Triangle(a, b, apex, n, column, 5, 0);
            // Main rib on the seam at vertex k: normal halfway between the two facets it joins.
            var prev = Vertex(plan, (k + 7) % 8, r, y0);
            var np = VectorMath.Unit(Vector3.Cross(a - prev, apex - prev), 1e-8);
            if (np.Y < 0) np = -np;
            Beam(steel, metal, a, Vector3.Lerp(a, apex, 1f - hub), VectorMath.Unit(n + np, 1e-8), LibraryStyle.RibWidth, LibraryStyle.RibDepth);
            Beam(steel, metal, a, b, n, LibraryStyle.EdgeBeam, LibraryStyle.RibDepth * 0.8f);
            if (rows < 2) continue;
            // Triangular grid: G(j, i) is point i of row j, row 0 on the base, rows − j + 1 points per row. Bars on the
            // base and on the two seams are the edge beam and the ribs, so they are skipped here.
            Vector3 G(int j, int i)
            {
                if (j == rows) return apex;
                var l = Vector3.Lerp(a, apex, j / (float)rows);
                var rr = Vector3.Lerp(b, apex, j / (float)rows);
                return Vector3.Lerp(l, rr, i / (float)(rows - j));
            }
            for (var j = 0; j < rows; j++)
                for (var i = 0; i < rows - j; i++)
                {
                    if (j > 0) Beam(steel, metal, G(j, i), G(j, i + 1), n, LibraryStyle.GridBar, LibraryStyle.GridDepth);
                    if (i > 0) Beam(steel, metal, G(j, i), G(j + 1, i), n, LibraryStyle.GridBar, LibraryStyle.GridDepth);
                    if (i < rows - j - 1) Beam(steel, metal, G(j, i + 1), G(j + 1, i), n, LibraryStyle.GridBar, LibraryStyle.GridDepth);
                }
        }
        // Central hub ring the ribs run into (小八角环).
        var hubTop = apex.Y + 0.28f;
        var ring = new Vector2[8];
        for (var k = 0; k < 8; k++) { var v = Vertex(plan, k, LibraryStyle.SkylightRingRadius, 0); ring[k] = new(v.X, v.Z); }
        Prism(steel, metal, ring, apex.Y - LibraryStyle.SkylightRingRadius * LibraryStyle.SkylightRiseRatio - 0.05f, hubTop);
    }

    /// <summary>The octagonal stone curb the glass sits on: outer and inner faces and the top.</summary>
    private static void Curb(LibraryPlan plan, GeometrySink mesh, LodLevel lod)
    {
        var trim = S.Trim(LibraryStyle.LibraryTrim);
        float r0 = plan.SkylightRadius, r1 = r0 + LibraryStyle.SkylightCurbWidth / MathF.Cos(MathF.Tau / 16f);
        float y0 = plan.RoofY - (lod == LodLevel.L0 ? 0.02f : 0f), y1 = SkylightBaseY(plan);
        for (var k = 0; k < 8; k++)
        {
            Vector3 ia = Vertex(plan, k, r0, y0), ib = Vertex(plan, (k + 1) % 8, r0, y0), oa = Vertex(plan, k, r1, y0), ob = Vertex(plan, (k + 1) % 8, r1, y0);
            var up = new Vector3(0, y1 - y0, 0);
            var outward = VectorMath.Unit((oa + ob) * 0.5f - new Vector3(plan.Centre.X, y0, plan.Centre.Y), 1e-8) with { Y = 0 };
            mesh.Quad(trim, oa, ob, ob + up, oa + up, outward);
            mesh.Quad(trim, ia, ib, ib + up, ia + up, -outward);
            mesh.Quad(trim, ia + up, ib + up, ob + up, oa + up, Vector3.UnitY);
        }
    }

    /// <summary>A rectangular bar from a to b lying on a plane with normal <paramref name="up"/>: top and two sides.</summary>
    public static void Beam(GeometrySink mesh, in Paint paint, Vector3 a, Vector3 b, Vector3 up, float width, float depth)
    {
        var t = b - a;
        if (t.LengthSquared() < 1e-6f) return;
        t = Vector3.Normalize(t);
        var side = VectorMath.Unit(Vector3.Cross(up, t), 1e-8) * (width * 0.5f);
        var lift = VectorMath.Unit(up, 1e-8) * depth;
        var sink = VectorMath.Unit(up, 1e-8) * -0.02f;
        mesh.Quad(paint, a - side + lift, a + side + lift, b + side + lift, b - side + lift, up);
        mesh.Quad(paint, a + side + sink, b + side + sink, b + side + lift, a + side + lift, side);
        mesh.Quad(paint, a - side + sink, b - side + sink, b - side + lift, a - side + lift, -side);
    }

    /// <summary>A vertical prism over a convex plan ring, with its top.</summary>
    public static void Prism(GeometrySink mesh, in Paint paint, IReadOnlyList<Vector2> ring, float y0, float y1)
    {
        var c = Vector2.Zero;
        foreach (var p in ring) c += p;
        c /= ring.Count;
        for (var k = 0; k < ring.Count; k++)
        {
            Vector2 a = ring[k], b = ring[(k + 1) % ring.Count];
            var mid = (a + b) * 0.5f - c;
            var n = VectorMath.Unit(new Vector3(mid.X, 0, mid.Y), 1e-8);
            mesh.Quad(paint, new(a.X, y0, a.Y), new(b.X, y0, b.Y), new(b.X, y1, b.Y), new(a.X, y1, a.Y), n);
        }
        for (var k = 1; k + 1 < ring.Count; k++)
            mesh.Tri(paint, new(ring[0].X, y1, ring[0].Y), new(ring[k].X, y1, ring[k].Y), new(ring[k + 1].X, y1, ring[k + 1].Y), Vector3.UnitY);
    }

    // ------------------------------------------------------------------ ring roof

    /// <summary>Radial distance of the curved roofs' inner end and the mechanical box's inner face from the centre.</summary>
    public static float RingStart(LibraryPlan plan) => plan.SkylightRadius + LibraryStyle.SkylightCurbWidth + 1.6f;

    /// <summary>
    /// Light grey curved metal roofs (浅灰弧形金属小坡顶) on the face axes, alternating with the flat dark roof between them;
    /// the north axis carries the mechanical box instead.
    /// </summary>
    public static void Vaults(LibraryPlan plan, GeometrySink mesh, LodLevel lod)
    {
        var north = NorthFace(plan);
        var sheet = S.Sheet(LibraryStyle.LibraryVault);
        var spec = S.Spec(CoreMaterial.Sheet, LibraryStyle.LibraryVault);
        var segments = lod switch { LodLevel.L0 => 12, LodLevel.L1 => 8, _ => 4 };
        var half = LibraryStyle.VaultWidth * 0.5f;
        var profile = new List<Vector2> { new(-half, 0), new(half, 0), new(half, LibraryStyle.VaultCurb) };
        for (var i = 1; i < segments; i++)
        {
            var t = MathF.Tau * 0.5f * i / segments;
            profile.Add(new(MathF.Cos(t) * half, LibraryStyle.VaultCurb + MathF.Sin(t) * LibraryStyle.VaultRise));
        }
        profile.Add(new(-half, LibraryStyle.VaultCurb));
        var section = new SweepProfile(profile);
        var start = RingStart(plan);
        for (var k = 0; k < 8; k++)
        {
            if (k == north) continue;
            var u = FaceDirection(plan, k);
            var path = new[] { plan.Centre + u * start, plan.Centre + u * (start + LibraryStyle.VaultLength) };
            mesh.Add(sheet, PartSweeps.Sweep(section, path, spec, false, plan.RoofY - 0.02f), Matrix4x4.Identity);
        }
    }

    /// <summary>
    /// Roof plant in two fenced enclosures (外环屋面设备进围挡, MAP-QSH-RENOVATION C-01) on the gaps either side of the
    /// mechanical box, away from the plaza views: WP-12's fence boards and heat pumps, placed from the catalogue.
    /// </summary>
    public static void Plant(LibraryPlan plan, CoreBuild build)
    {
        var north = NorthFace(plan);
        float hl = LibraryStyle.PlantLength * 0.5f, hd = LibraryStyle.PlantDepth * 0.5f;
        foreach (var side in new[] { -1.5f, 1.5f })
        {
            var a = plan.FaceAngle + (north + side) * (MathF.Tau / 8f);
            var u = new Vector2(MathF.Cos(a), MathF.Sin(a));
            var t = new Vector2(-u.Y, u.X);
            var c = plan.Centre + u * LibraryStyle.PlantRadius;
            Vector2[] corners = [c - t * hl - u * hd, c + t * hl - u * hd, c + t * hl + u * hd, c - t * hl + u * hd];
            for (var k = 0; k < 4; k++)
            {
                Vector2 p = corners[k], q = corners[(k + 1) % 4];
                var mid = (p + q) * 0.5f;
                var along = Vector2.Normalize(q - p);
                var fence = new RailingParams(Vector2.Distance(p, q), LibraryStyle.PlantFence, 0.06f, 1.2f) { Variation = k == 0 ? 1 : 0 };
                build.Parts.Add(new PlacedPart(new PartId(PartCatalog.EquipmentFence), fence,
                    CoreShapes.Frame(new(mid.X, plan.RoofY, mid.Y), Vector3.UnitY, new(-along.Y, 0, along.X))));
            }
            foreach (var offset in new[] { -hl * 0.45f, hl * 0.45f })
            {
                var p = c + t * offset;
                build.Parts.Add(new PlacedPart(new PartId(PartCatalog.HeatPump), new EquipmentParams(2.2f, 1.6f, 1.1f),
                    CoreShapes.Frame(new(p.X, plan.RoofY, p.Y), Vector3.UnitY, new(u.X, 0, u.Y))));
            }
        }
    }

    /// <summary>Plan frame of the mechanical box: inner long face, outer long face and the two ends, as faces.</summary>
    public static IReadOnlyList<Face> MechFaces(LibraryPlan plan)
    {
        var u = FaceDirection(plan, NorthFace(plan));
        var t = new Vector2(-u.Y, u.X);
        var c = plan.Centre + u * (RingStart(plan) + LibraryStyle.MechDepth * 0.5f);
        float hl = LibraryStyle.MechLength * 0.5f, hd = LibraryStyle.MechDepth * 0.5f;
        return
        [
            new Face(c + u * hd - t * hl, t, u, LibraryStyle.MechLength),
            new Face(c - u * hd + t * hl, -t, -u, LibraryStyle.MechLength),
            new Face(c + t * hl + u * hd, -u, t, LibraryStyle.MechDepth),
            new Face(c - t * hl - u * hd, u, -t, LibraryStyle.MechDepth),
        ];
    }

    /// <summary>The mechanical box's window openings: two rows of small windows on each long face (W-MECH).</summary>
    public static IEnumerable<(Face Face, Rect Hole)> MechWindows(LibraryPlan plan)
    {
        var faces = MechFaces(plan);
        foreach (var f in faces.Take(2))
            foreach (var row in new[] { LibraryStyle.MechRow0, LibraryStyle.MechRow1 })
                for (var i = 0; i < LibraryStyle.MechWindows; i++)
                {
                    var pitch = (LibraryStyle.MechLength - 1.6f) / LibraryStyle.MechWindows;
                    var u = 0.8f + pitch * (i + 0.5f);
                    yield return (f, new Rect(u - LibraryStyle.MechWindow * 0.5f, u + LibraryStyle.MechWindow * 0.5f, plan.RoofY + row, plan.RoofY + row + LibraryStyle.MechWindow * 0.9f));
                }
    }
}
