using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.Core.Framework;
using S = Campus.Gen.Zones.Core.CoreStyle;

namespace Campus.Gen.Zones.Core.Library;

/// <summary>
/// The library roof (MAP-QSH-CORE §1.5): the octagonal low-cone skylight over the atrium (port of the legacy
/// libraryRoof.ts: eight facets to a central ring, main ribs on the facet seams, a triangular steel grid between them,
/// clear glass), the curved metal roofs alternating with the flat ring roof, and the raised mechanical box north of
/// the skylight. Octagon vertices sit half-way between the face normals of <see cref="LibraryPlan.FaceAngle"/>.
/// </summary>
public static class LibraryRoof
{
    /// <summary>Plan direction of face k (outward normal), k = 0…7 counterclockwise in (x, z).</summary>
    public static Vector2 FaceDirection(LibraryPlan plan, int k)
    {
        var a = plan.FaceAngle + k * (Shapes.Tau / 8f);
        return new(DetMath.Cos(a), DetMath.Sin(a));
    }

    /// <summary>The face whose normal points closest to true north (−z): the mechanical box stands there.</summary>
    public static int NorthFace(LibraryPlan plan)
    {
        var best = 0;
        for (var k = 1; k < 8; k++)
            if (FaceDirection(plan, k).Y < FaceDirection(plan, best).Y) best = k;
        return best;
    }

    /// <summary>Skylight base vertex k on the curb top (circumradius <see cref="LibraryPlan.SkylightRadius"/>).</summary>
    public static Vector3 Vertex(LibraryPlan plan, int k, float radius, float y)
    {
        var a = plan.FaceAngle + Shapes.Tau / 16f + k * (Shapes.Tau / 8f);
        return new(plan.Centre.X + DetMath.Cos(a) * radius, y, plan.Centre.Y + DetMath.Sin(a) * radius);
    }

    public static float SkylightBaseY(LibraryPlan plan) => plan.RoofY + S.SkylightCurb;
    public static float SkylightApexY(LibraryPlan plan) => SkylightBaseY(plan) + plan.SkylightRadius * S.SkylightRiseRatio;

    /// <summary>
    /// Curb, glass facets, ribs, grid and hub. Steel goes to <paramref name="steel"/> (its own instance, fog-exempt with
    /// the glass, MAP-QSH-CORE §7), the stone curb to the shared sink.
    /// </summary>
    public static void Skylight(LibraryPlan plan, CoreMesh shared, CoreMesh steel, GlassSink glass, int column, LodLevel lod)
    {
        var r = plan.SkylightRadius;
        var y0 = SkylightBaseY(plan);
        var apex = new Vector3(plan.Centre.X, SkylightApexY(plan), plan.Centre.Y);
        Curb(plan, shared, lod);
        var metal = S.Metal(S.SkylightSteel);
        var rows = S.SkylightRows(lod);
        var hub = S.SkylightRingRadius / r;
        for (var k = 0; k < 8; k++)
        {
            var a = Vertex(plan, k, r, y0);
            var b = Vertex(plan, (k + 1) % 8, r, y0);
            var n = Shapes.Normalize(Vector3.Cross(b - a, apex - a));
            if (n.Y < 0) n = -n;
            glass.Triangle(a, b, apex, n, column, 5, 0);
            // Main rib on the seam at vertex k: normal halfway between the two facets it joins.
            var prev = Vertex(plan, (k + 7) % 8, r, y0);
            var np = Shapes.Normalize(Vector3.Cross(a - prev, apex - prev));
            if (np.Y < 0) np = -np;
            Beam(steel, metal, a, Vector3.Lerp(a, apex, 1f - hub), Shapes.Normalize(n + np), S.RibWidth, S.RibDepth);
            Beam(steel, metal, a, b, n, S.EdgeBeam, S.RibDepth * 0.8f);
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
                    if (j > 0) Beam(steel, metal, G(j, i), G(j, i + 1), n, S.GridBar, S.GridDepth);
                    if (i > 0) Beam(steel, metal, G(j, i), G(j + 1, i), n, S.GridBar, S.GridDepth);
                    if (i < rows - j - 1) Beam(steel, metal, G(j, i + 1), G(j + 1, i), n, S.GridBar, S.GridDepth);
                }
        }
        // Central hub ring the ribs run into (小八角环).
        var hubTop = apex.Y + 0.28f;
        var ring = new Vector2[8];
        for (var k = 0; k < 8; k++) { var v = Vertex(plan, k, S.SkylightRingRadius, 0); ring[k] = new(v.X, v.Z); }
        Prism(steel, metal, ring, apex.Y - S.SkylightRingRadius * S.SkylightRiseRatio - 0.05f, hubTop);
    }

    /// <summary>The octagonal stone curb the glass sits on: outer and inner faces and the top.</summary>
    private static void Curb(LibraryPlan plan, CoreMesh mesh, LodLevel lod)
    {
        var trim = S.Trim(S.LibraryTrim);
        float r0 = plan.SkylightRadius, r1 = r0 + S.SkylightCurbWidth / DetMath.Cos(Shapes.Tau / 16f);
        float y0 = plan.RoofY - (lod == LodLevel.L0 ? 0.02f : 0f), y1 = SkylightBaseY(plan);
        for (var k = 0; k < 8; k++)
        {
            Vector3 ia = Vertex(plan, k, r0, y0), ib = Vertex(plan, (k + 1) % 8, r0, y0), oa = Vertex(plan, k, r1, y0), ob = Vertex(plan, (k + 1) % 8, r1, y0);
            var up = new Vector3(0, y1 - y0, 0);
            var outward = Shapes.Normalize((oa + ob) * 0.5f - new Vector3(plan.Centre.X, y0, plan.Centre.Y)) with { Y = 0 };
            mesh.Quad(trim, oa, ob, ob + up, oa + up, outward);
            mesh.Quad(trim, ia, ib, ib + up, ia + up, -outward);
            mesh.Quad(trim, ia + up, ib + up, ob + up, oa + up, Vector3.UnitY);
        }
    }

    /// <summary>A rectangular bar from a to b lying on a plane with normal <paramref name="up"/>: top and two sides.</summary>
    public static void Beam(CoreMesh mesh, in Paint paint, Vector3 a, Vector3 b, Vector3 up, float width, float depth)
    {
        var t = b - a;
        if (t.LengthSquared() < 1e-6f) return;
        t = Vector3.Normalize(t);
        var side = Shapes.Normalize(Vector3.Cross(up, t)) * (width * 0.5f);
        var lift = Shapes.Normalize(up) * depth;
        var sink = Shapes.Normalize(up) * -0.02f;
        mesh.Quad(paint, a - side + lift, a + side + lift, b + side + lift, b - side + lift, up);
        mesh.Quad(paint, a + side + sink, b + side + sink, b + side + lift, a + side + lift, side);
        mesh.Quad(paint, a - side + sink, b - side + sink, b - side + lift, a - side + lift, -side);
    }

    /// <summary>A vertical prism over a convex plan ring, with its top.</summary>
    public static void Prism(CoreMesh mesh, in Paint paint, IReadOnlyList<Vector2> ring, float y0, float y1)
    {
        var c = Vector2.Zero;
        foreach (var p in ring) c += p;
        c /= ring.Count;
        for (var k = 0; k < ring.Count; k++)
        {
            Vector2 a = ring[k], b = ring[(k + 1) % ring.Count];
            var mid = (a + b) * 0.5f - c;
            var n = Shapes.Normalize(new Vector3(mid.X, 0, mid.Y));
            mesh.Quad(paint, new(a.X, y0, a.Y), new(b.X, y0, b.Y), new(b.X, y1, b.Y), new(a.X, y1, a.Y), n);
        }
        for (var k = 1; k + 1 < ring.Count; k++)
            mesh.Tri(paint, new(ring[0].X, y1, ring[0].Y), new(ring[k].X, y1, ring[k].Y), new(ring[k + 1].X, y1, ring[k + 1].Y), Vector3.UnitY);
    }

    // ------------------------------------------------------------------ ring roof

    /// <summary>Radial distance of the curved roofs' inner end and the mechanical box's inner face from the centre.</summary>
    public static float RingStart(LibraryPlan plan) => plan.SkylightRadius + S.SkylightCurbWidth + 1.6f;

    /// <summary>
    /// Light grey curved metal roofs (浅灰弧形金属小坡顶) on the face axes, alternating with the flat dark roof between them;
    /// the north axis carries the mechanical box instead.
    /// </summary>
    public static void Vaults(LibraryPlan plan, CoreMesh mesh, LodLevel lod)
    {
        var north = NorthFace(plan);
        var sheet = S.Sheet(S.LibraryVault);
        var spec = S.Spec(CoreMaterial.Sheet, S.LibraryVault);
        var segments = lod switch { LodLevel.L0 => 12, LodLevel.L1 => 8, _ => 4 };
        var half = S.VaultWidth * 0.5f;
        var profile = new List<Vector2> { new(-half, 0), new(half, 0), new(half, S.VaultCurb) };
        for (var i = 1; i < segments; i++)
        {
            var t = Shapes.Tau * 0.5f * i / segments;
            profile.Add(new(DetMath.Cos(t) * half, S.VaultCurb + DetMath.Sin(t) * S.VaultRise));
        }
        profile.Add(new(-half, S.VaultCurb));
        var section = new SweepProfile(profile);
        var start = RingStart(plan);
        for (var k = 0; k < 8; k++)
        {
            if (k == north) continue;
            var u = FaceDirection(plan, k);
            var path = new[] { plan.Centre + u * start, plan.Centre + u * (start + S.VaultLength) };
            mesh.Add(sheet, PartGeometryKernel.Sweep(section, path, spec, false, plan.RoofY - 0.02f), Matrix4x4.Identity);
        }
    }

    /// <summary>
    /// Roof plant in two fenced enclosures (外环屋面设备进围挡, MAP-QSH-RENOVATION C-01) on the gaps either side of the
    /// mechanical box, away from the plaza views: WP-12's fence boards and heat pumps, placed from the catalogue.
    /// </summary>
    public static void Plant(LibraryPlan plan, CoreBuild build)
    {
        var north = NorthFace(plan);
        float hl = S.PlantLength * 0.5f, hd = S.PlantDepth * 0.5f;
        foreach (var side in new[] { -1.5f, 1.5f })
        {
            var a = plan.FaceAngle + (north + side) * (Shapes.Tau / 8f);
            var u = new Vector2(DetMath.Cos(a), DetMath.Sin(a));
            var t = new Vector2(-u.Y, u.X);
            var c = plan.Centre + u * S.PlantRadius;
            Vector2[] corners = [c - t * hl - u * hd, c + t * hl - u * hd, c + t * hl + u * hd, c - t * hl + u * hd];
            for (var k = 0; k < 4; k++)
            {
                Vector2 p = corners[k], q = corners[(k + 1) % 4];
                var mid = (p + q) * 0.5f;
                var along = Vector2.Normalize(q - p);
                var fence = new RailingParams(Vector2.Distance(p, q), S.PlantFence, 0.06f, 1.2f) { Variation = k == 0 ? 1 : 0 };
                build.Parts.Add(new PlacedPart(new PartId(PartCatalog.EquipmentFence), fence,
                    Shapes.Frame(new(mid.X, plan.RoofY, mid.Y), Vector3.UnitY, new(-along.Y, 0, along.X))));
            }
            foreach (var offset in new[] { -hl * 0.45f, hl * 0.45f })
            {
                var p = c + t * offset;
                build.Parts.Add(new PlacedPart(new PartId(PartCatalog.HeatPump), new EquipmentParams(2.2f, 1.6f, 1.1f),
                    Shapes.Frame(new(p.X, plan.RoofY, p.Y), Vector3.UnitY, new(u.X, 0, u.Y))));
            }
        }
    }

    /// <summary>Plan frame of the mechanical box: inner long face, outer long face and the two ends, as faces.</summary>
    public static IReadOnlyList<Face> MechFaces(LibraryPlan plan)
    {
        var u = FaceDirection(plan, NorthFace(plan));
        var t = new Vector2(-u.Y, u.X);
        var c = plan.Centre + u * (RingStart(plan) + S.MechDepth * 0.5f);
        float hl = S.MechLength * 0.5f, hd = S.MechDepth * 0.5f;
        return
        [
            new Face(c + u * hd - t * hl, t, u, S.MechLength),
            new Face(c - u * hd + t * hl, -t, -u, S.MechLength),
            new Face(c + t * hl + u * hd, -u, t, S.MechDepth),
            new Face(c - t * hl - u * hd, u, -t, S.MechDepth),
        ];
    }

    /// <summary>The mechanical box's window openings: two rows of small windows on each long face (W-MECH).</summary>
    public static IEnumerable<(Face Face, Rect Hole)> MechWindows(LibraryPlan plan)
    {
        var faces = MechFaces(plan);
        foreach (var f in faces.Take(2))
            foreach (var row in new[] { S.MechRow0, S.MechRow1 })
                for (var i = 0; i < S.MechWindows; i++)
                {
                    var pitch = (S.MechLength - 1.6f) / S.MechWindows;
                    var u = 0.8f + pitch * (i + 0.5f);
                    yield return (f, new Rect(u - S.MechWindow * 0.5f, u + S.MechWindow * 0.5f, plan.RoofY + row, plan.RoofY + row + S.MechWindow * 0.9f));
                }
    }
}
