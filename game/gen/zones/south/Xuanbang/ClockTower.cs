using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.South.Framework;
using X = Campus.Gen.Zones.South.SouthTune.Xuanbang;
using L = Campus.Gen.Zones.South.SouthTune.Look;
using W = Campus.Gen.Zones.South.SouthTune.Windows;

namespace Campus.Gen.Zones.South.Xuanbang;

/// <summary>
/// Xuanbang's clock tower (MAP-QSH-SOUTH §5.4, §5.9), 66.65 m: a square beige-stone shaft with corner quoins, a pair of
/// narrow windows per storey on every face and an arched portal at the foot of each (the south-east one is the open side
/// door xb.clock); a corniced setback stage carrying four metal-rimmed dials (the north-west one faces the lake and P09);
/// a cylindrical lantern of slit columns round a solid core; and the dark grey bullet dome. Lantern and dome together read
/// as the electron tube. The dome has a material of its own, so it stays a separate bundle for the retreat-line bias; the
/// hands are WP-26's moving geometry on the dial pivots (DialCentre). The clock room and the lantern floor and railings
/// are WP-31a's.
/// </summary>
internal static class ClockTower
{
    /// <summary>Frame at the footprint centre: +X along the north-east-running faces, +Z towards the south-east door face.</summary>
    internal static (PlanFrame Frame, float HalfX, float HalfZ) FrameOf(IReadOnlyList<Vector2> ring)
    {
        ring = Quadrilateral(ring);
        var centre = PlanMath.Centroid(ring);
        // The edge running closest to 42° (or 222°) gives +X; its neighbour gives the depth.
        var best = 0;
        var bestError = float.MaxValue;
        for (var i = 0; i < 4; i++)
        {
            var d = ring[(i + 1) % 4] - ring[i];
            var error = MathF.Abs(PlanMath.Turn(SouthSite.Bearing(d) - X.ClockAxis));
            error = MathF.Min(error, MathF.Abs(PlanMath.Turn(SouthSite.Bearing(d) - X.ClockAxis - 180f)));
            if (error < bestError) { bestError = error; best = i; }
        }
        var along = ring[(best + 1) % 4] - ring[best];
        var across = ring[(best + 2) % 4] - ring[(best + 1) % 4];
        if (MathF.Abs(PlanMath.Turn(SouthSite.Bearing(along) - X.ClockAxis)) > 90f) along = -along;
        var f = PlanFrame.Along(centre, along);
        return (f, Length(along) * 0.5f, Length(across) * 0.5f);
    }

    /// <summary>
    /// The footprint as four corners: a four-point ring is returned untouched; otherwise collinear points are dropped, and
    /// if the rest is still not four points the smallest-area bounding rectangle (edge-aligned) stands in.
    /// </summary>
    internal static IReadOnlyList<Vector2> Quadrilateral(IReadOnlyList<Vector2> ring)
    {
        if (ring.Count == 4) return ring;
        if (ring.Count < 3) throw new InvalidOperationException("The clock tower footprint needs at least three points.");
        var pts = new List<Vector2>(ring);
        for (var again = true; again && pts.Count > 3;)
        {
            again = false;
            for (var i = 0; i < pts.Count; i++)
            {
                var a = pts[(i + pts.Count - 1) % pts.Count];
                var b = pts[i];
                var c = pts[(i + 1) % pts.Count];
                var ab = b - a;
                var bc = c - b;
                var cross = ab.X * bc.Y - ab.Y * bc.X;
                var scale = Length(ab) * Length(bc);
                if (scale < 1e-6f || MathF.Abs(cross) <= 1e-3f * scale)
                {
                    pts.RemoveAt(i);
                    again = true;
                    break;
                }
            }
        }
        if (pts.Count == 4) return pts;
        var bestArea = float.MaxValue;
        Vector2[]? best = null;
        for (var i = 0; i < pts.Count; i++)
        {
            var e = pts[(i + 1) % pts.Count] - pts[i];
            var len = Length(e);
            if (len < 1e-6f) continue;
            var u = e / len;
            var w = new Vector2(-u.Y, u.X);
            float u0 = float.MaxValue, u1 = float.MinValue, w0 = float.MaxValue, w1 = float.MinValue;
            foreach (var p in pts)
            {
                var pu = Vector2.Dot(p, u);
                var pw = Vector2.Dot(p, w);
                u0 = MathF.Min(u0, pu); u1 = MathF.Max(u1, pu);
                w0 = MathF.Min(w0, pw); w1 = MathF.Max(w1, pw);
            }
            var area = (u1 - u0) * (w1 - w0);
            if (area < bestArea - 1e-4f)
            {
                bestArea = area;
                best = [u * u0 + w * w0, u * u1 + w * w0, u * u1 + w * w1, u * u0 + w * w1];
            }
        }
        return best ?? throw new InvalidOperationException("The clock tower footprint is degenerate.");
    }

    /// <summary>Faces as frames at the face centre on the footprint line, +Z outward; index 0 is the south-east (door) face.</summary>
    internal static IEnumerable<(PlanFrame Face, float Width, bool Door)> Faces(PlanFrame f, float hx, float hz)
    {
        yield return (new(f.P(0, hz), f.U), 2 * hx, true);
        yield return (new(f.P(0, -hz), -f.U), 2 * hx, false);
        yield return (new(f.P(hx, 0), -f.W), 2 * hz, false);
        yield return (new(f.P(-hx, 0), f.W), 2 * hz, false);
    }

    /// <summary>Centre of each dial (world) and its outward plan direction: the pivots WP-26 turns the hands about.</summary>
    internal static IEnumerable<(Vector3 Centre, Vector2 Normal)> DialCentres(IReadOnlyList<Vector2> ring, float y)
    {
        var (f, hx, hz) = FrameOf(ring);
        foreach (var (face, _, _) in Faces(f, hx - X.StageInset, hz - X.StageInset))
        {
            var p = face.P(0, X.DialProud);
            yield return (new(p.X, y + X.DialY, p.Y), face.W);
        }
    }

    /// <summary>Window grids: two slit columns on each of the four faces (face index * 2, left then right), one portal per face.</summary>
    internal const int ShaftColumns = 8, PortalColumns = 4;

    /// <summary>Glass planes behind the slits and the portals, in the face frame (the wall is 0.7 m, centred on the footprint line).</summary>
    private const float SlitGlassZ = -0.2f, PortalGlassZ = -0.15f;

    /// <summary>The slit panes in grid order: column face * 2 + (0 left, 1 right), row = storey; 2 cm of each pane runs into the wall.</summary>
    internal static IEnumerable<GlazedOpening> Slits(IReadOnlyList<Vector2> ring, float y)
    {
        var (f, hx, hz) = FrameOf(ring);
        var index = 0;
        foreach (var (g, _, _) in Faces(f, hx, hz))
        {
            var outward = new Vector3(g.W.X, 0, g.W.Y);
            for (var r = 0; r < X.SlitRows; r++)
                for (var c = 0; c < 2; c++)
                {
                    var p = g.P((c == 0 ? -1f : 1f) * (X.SlitGap * 0.5f + X.SlitWidth * 0.5f), SlitGlassZ);
                    yield return new(index * 2 + c, r, new(p.X, y + X.SlitFirst + r * X.SlitPitch - 0.02f, p.Y), outward, X.SlitWidth + 0.04f, X.SlitHeight + 0.04f);
                }
            index++;
        }
    }

    /// <summary>
    /// The portal panes, one per face in face order: the glazed door's arch on the closed faces, the fanlight over the springing on
    /// the open door face. The wall around the arch hides each pane's corners.
    /// </summary>
    internal static IEnumerable<GlazedOpening> Portals(IReadOnlyList<Vector2> ring, float y)
    {
        var (f, hx, hz) = FrameOf(ring);
        var index = 0;
        foreach (var (g, _, door) in Faces(f, hx, hz))
        {
            var p = g.P(0, PortalGlassZ);
            var bottom = door ? X.PortalSpring : 0f;
            yield return new(index++, 0, new(p.X, y + bottom, p.Y), new(g.W.X, 0, g.W.Y), X.PortalWidth, X.PortalSpring + X.PortalWidth * 0.5f - bottom);
        }
    }

    internal static void Build(SiteBuilder b, IReadOnlyList<Vector2> ring, float y)
    {
        var (f, hx, hz) = FrameOf(ring);
        var shaft = XuanbangSite.ClockAllocation(b.Context, XuanbangSite.ShaftKey);
        var portals = XuanbangSite.ClockAllocation(b.Context, XuanbangSite.PortalKey);
        // Numbered panes once the map's window numbering is in the context; without one the faces keep their plain glass.
        if (b.Detail != SiteDetail.Mass)
        {
            if (shaft is { } s) WindowNumbers.Panes(b, Slits(ring, y), s, W.Research, W.Stair);
            if (portals is { } p) WindowNumbers.Panes(b, Portals(ring, y), p, W.Research, W.Corridor);
        }
        var detail = b.Detail;
        var coarse = b.Lod is LodLevel.L3 or LodLevel.L4;
        var segments = detail switch { SiteDetail.Full => 40, SiteDetail.Plain => 24, _ => 16 };
        var bevel = detail == SiteDetail.Full ? 0.03f : 0f;
        if (coarse)
            b.Add(PartGeometryKernel.BeveledBox(2 * hx, X.ShaftTop, 2 * hz, 0, L.Beige), f.At(0, y, 0));
        else
        {
            // Floor slab up to the threshold, level with the forecourt paving, so the side door opens straight onto it; the granite
            // base course stands under the quoins and, with the face skirts, either side of each portal.
            b.Add(PartGeometryKernel.BeveledBox(2 * hx + 0.3f, X.Threshold + 0.1f, 2 * hz + 0.3f, bevel, L.Granite), f.At(0, y - 0.1f, 0));
            foreach (var sx in new[] { -1f, 1f })
                foreach (var sz in new[] { -1f, 1f })
                    b.Add(PartGeometryKernel.BeveledBox(X.Quoin + 0.3f, 1.3f, X.Quoin + 0.3f, bevel, L.Granite), f.At(sx * hx, y - 0.1f, sz * hz));
            foreach (var (face, width, door) in Faces(f, hx, hz)) Face(b, face, width, door, y, shaft is null, portals is null);
            foreach (var sx in new[] { -1f, 1f })
                foreach (var sz in new[] { -1f, 1f })
                    b.Add(PartGeometryKernel.BeveledBox(X.Quoin, X.ShaftTop - 1.2f, X.Quoin, bevel, L.Beige), f.At(sx * hx, y + 1.2f, sz * hz));
        }
        // Shaft cornice, then the setback stage with its dials and cornice.
        b.Add(PartGeometryKernel.BeveledBox(2 * hx + 1.2f, 0.35f, 2 * hz + 1.2f, bevel, L.BeigeTrim), f.At(0, y + X.ShaftTop, 0));
        b.Add(PartGeometryKernel.BeveledBox(2 * hx + 1.7f, 0.45f, 2 * hz + 1.7f, bevel, L.BeigeTrim), f.At(0, y + X.ShaftTop + 0.35f, 0));
        float sxh = hx - X.StageInset, szh = hz - X.StageInset, stage0 = y + X.ShaftTop + 0.8f;
        b.Add(PartGeometryKernel.BeveledBox(2 * sxh, y + X.StageTop - stage0, 2 * szh, bevel, L.Beige), f.At(0, stage0, 0));
        if (!coarse)
        {
            foreach (var sx in new[] { -1f, 1f })
                foreach (var sz in new[] { -1f, 1f })
                    b.Add(PartGeometryKernel.BeveledBox(1.0f, y + X.StageTop - stage0, 1.0f, bevel, L.Beige), f.At(sx * sxh, stage0, sz * szh));
            foreach (var (face, _, _) in Faces(f, sxh, szh)) Dial(b, face, y, segments);
        }
        b.Add(PartGeometryKernel.BeveledBox(2 * sxh + 1.3f, 0.8f, 2 * szh + 1.3f, bevel, L.BeigeTrim), f.At(0, y + X.StageTop, 0));
        // Lantern: podium drum, solid core, a ring of slit columns, the entablature disc, the dome and its finial.
        var podium = y + X.StageTop + 0.8f;
        b.Add(Solids.Lathe([new(X.LanternRadius + 0.75f, 0), new(X.LanternRadius + 0.75f, y + X.LanternFloor - podium)], segments, L.Beige), f.At(0, podium, 0));
        if (!coarse)
        {
            b.Add(Solids.Lathe([new(X.LanternRadius + 0.95f, 0), new(X.LanternRadius + 0.95f, 0.18f), new(X.LanternRadius + 0.8f, 0.3f)], segments, L.BeigeTrim),
                f.At(0, y + X.LanternFloor - 0.3f, 0));
            b.Add(Solids.Lathe([new(X.CoreRadius, 0), new(X.CoreRadius, X.DomeBase - X.LanternFloor)], segments / 2, L.Beige), f.At(0, y + X.LanternFloor, 0));
            var height = X.LanternTop - X.LanternFloor;
            for (var k = 0; k < X.LanternColumns; k++)
            {
                var a = 2f * MathF.PI * (k + 0.5f) / X.LanternColumns;
                float c = DetMath.Cos(a), s = DetMath.Sin(a);
                var at = f.P(c * X.LanternRadius, s * X.LanternRadius);
                var tangent = f.U * -s + f.W * c;
                var col = PlanFrame.Along(at, tangent);
                b.Add(PartGeometryKernel.BeveledBox(X.LanternColumn, height, 0.75f, bevel, L.Beige), col.At(0, y + X.LanternFloor, 0));
            }
        }
        b.Add(Solids.Lathe([new(X.LanternRadius + 0.6f, 0), new(X.LanternRadius + 0.6f, X.DomeBase - X.LanternTop)], segments, L.BeigeTrim), f.At(0, y + X.LanternTop, 0));
        var dome = X.DomeProfile.Select(p => new Vector2(p.X * X.DomeRadius, p.Y * (X.DomeTop - X.DomeBase))).ToArray();
        b.Add(Solids.Lathe(dome, segments, L.DomeMetal), f.At(0, y + X.DomeBase, 0));
        if (detail != SiteDetail.Mass)
            b.Add(Solids.Lathe([new(0.32f, 0), new(0.2f, 0.1f), new(0.11f, 0.24f), new(0.16f, 0.3f), new(0.05f, 0.42f), new(0, X.Top - X.DomeTop)], 12, L.DomeMetal),
                f.At(0, y + X.DomeTop - 0.02f, 0));
    }

    /// <summary>
    /// One shaft face: a stone wall (centred on the footprint line, 0.7 m) with the arched portal notched into its foot and a
    /// pair of slit windows per storey; glass sits 0.55 m back; sills under the slits; an archivolt round the portal.
    /// </summary>
    private static void Face(SiteBuilder b, PlanFrame g, float width, bool door, float y, bool slitGlass, bool portalGlass)
    {
        var detail = b.Detail;
        var segments = detail == SiteDetail.Full ? 16 : 8;
        var h = X.ShaftTop;
        var portal = Shapes.RoundArch(0, 0, X.PortalWidth, X.PortalSpring, segments);
        var outer = new List<Vector2> { new(-width * 0.5f, 0), portal[0] };
        for (var i = portal.Length - 1; i >= 2; i--) outer.Add(portal[i]);
        outer.Add(portal[1]);
        outer.AddRange([new(width * 0.5f, 0), new(width * 0.5f, h), new(-width * 0.5f, h)]);
        var slits = new List<IReadOnlyList<Vector2>>();
        for (var r = 0; r < X.SlitRows; r++)
        {
            var s0 = X.SlitFirst + r * X.SlitPitch;
            foreach (var side in new[] { -1f, 1f })
            {
                var cx = side * (X.SlitGap * 0.5f + X.SlitWidth * 0.5f);
                slits.Add([new(cx - X.SlitWidth * 0.5f, s0), new(cx + X.SlitWidth * 0.5f, s0), new(cx + X.SlitWidth * 0.5f, s0 + X.SlitHeight), new(cx - X.SlitWidth * 0.5f, s0 + X.SlitHeight)]);
            }
        }
        b.Add(PartGeometryKernel.WallPanel(new PolygonRings(outer, slits), 0.7f, L.Beige), g.At(0, y, 0));
        // Plinth skirt either side of the portal.
        var skirt = width * 0.5f - X.PortalWidth * 0.5f - 0.35f;
        foreach (var side in new[] { -1f, 1f })
            b.Add(PartGeometryKernel.BeveledBox(skirt, 1.2f, 0.95f, detail == SiteDetail.Full ? 0.03f : 0, L.Granite), g.At(side * (width * 0.5f - skirt * 0.5f), y - 0.1f, 0.12f));
        if (detail == SiteDetail.Mass) return;
        var top = X.SlitFirst + (X.SlitRows - 1) * X.SlitPitch + X.SlitHeight;
        if (slitGlass)
            b.Add(PartGeometryKernel.BeveledBox(X.SlitGap + 2 * X.SlitWidth + 0.4f, top - X.SlitFirst + 0.4f, 0.05f, 0, L.Glass), g.At(0, y + X.SlitFirst - 0.2f, SlitGlassZ));
        // Archivolt round the portal and a keystone.
        var ring = Shapes.RoundArch(0, 0, X.PortalWidth + 0.9f, X.PortalSpring, segments);
        var hole = Shapes.RoundArch(0, -0.01f, X.PortalWidth, X.PortalSpring + 0.01f, segments);
        var band = new List<Vector2> { ring[0], hole[0] };
        for (var i = hole.Length - 1; i >= 2; i--) band.Add(hole[i]);
        band.Add(hole[1]);
        band.Add(ring[1]);
        for (var i = 2; i < ring.Length; i++) band.Add(ring[i]);
        b.Add(PartGeometryKernel.WallPanel(new PolygonRings(band), 0.2f, L.BeigeTrim), g.At(0, y, 0.45f));
        if (detail == SiteDetail.Full)
        {
            for (var r = 0; r < X.SlitRows; r++)
                foreach (var side in new[] { -1f, 1f })
                    b.Add(PartGeometryKernel.BeveledBox(X.SlitWidth + 0.24f, 0.1f, 0.22f, 0.015f, L.BeigeTrim),
                        g.At(side * (X.SlitGap * 0.5f + X.SlitWidth * 0.5f), y + X.SlitFirst + r * X.SlitPitch - 0.1f, 0.42f));
        }
        // Portal infill: a glazed door, open on the door face (leaves back; mat, sign and lamp are WP-13's), closed elsewhere.
        var arch = Shapes.RoundArch(0, 0, X.PortalWidth, X.PortalSpring, segments);
        if (!door)
        {
            if (portalGlass) b.Add(PartGeometryKernel.WallPanel(new PolygonRings(arch), 0.05f, L.Glass), g.At(0, y, PortalGlassZ));
            b.Add(PartGeometryKernel.BeveledBox(0.1f, X.PortalSpring, 0.1f, 0, L.DarkFrame), g.At(0, y, -0.08f));
            return;
        }
        var fan = Shapes.RoundArch(0, X.PortalSpring - 0.01f, X.PortalWidth, 0.01f, segments);
        if (portalGlass) b.Add(PartGeometryKernel.WallPanel(new PolygonRings(fan), 0.05f, L.Glass), g.At(0, y, PortalGlassZ));
        b.Add(PartGeometryKernel.BeveledBox(X.PortalWidth, 0.12f, 0.12f, 0, L.DarkFrame), g.At(0, y + X.PortalSpring - 0.06f, -0.08f));
        foreach (var side in new[] { -1f, 1f })
            b.Add(PartGeometryKernel.BeveledBox(0.06f, X.PortalSpring - 0.1f, 1.1f, 0.012f, L.Wood), g.At(side * (X.PortalWidth * 0.5f - 0.05f), y, 0.2f));
    }

    /// <summary>A dial on a stage face: pale disc, dark bezel ring, hour marks and the pivot boss (no hands: WP-26).</summary>
    private static void Dial(SiteBuilder b, PlanFrame g, float y, int segments)
    {
        var r = X.DialRadius;
        var cy = y + X.DialY;
        b.Add(Solids.Lathe([new(r, 0), new(r, 0.12f)], segments, L.Dial), PlanMath.AxisOut(g, 0, cy, 0));
        b.Add(Solids.Lathe([new(r - 0.04f, 0.1f), new(r + 0.3f, 0.1f), new(r + 0.3f, 0.28f), new(r + 0.12f, 0.36f), new(r - 0.04f, 0.3f)], segments, L.DialRim,
            capBottom: false, capTop: false), PlanMath.AxisOut(g, 0, cy, 0));
        if (b.Detail == SiteDetail.Mass) return;
        b.Add(Solids.Lathe([new(0.22f, 0), new(0.22f, 0.22f), new(0.12f, 0.3f), new(0, 0.32f)], 12, L.DialRim), PlanMath.AxisOut(g, 0, cy, X.DialProud - 0.2f));
        if (b.Detail != SiteDetail.Full) return;
        for (var k = 0; k < 12; k++)
        {
            var a = 2f * MathF.PI * k / 12f;
            float s = DetMath.Sin(a), c = DetMath.Cos(a);
            var major = k % 3 == 0;
            var size = major ? 0.42f : 0.24f;
            b.Add(PartGeometryKernel.BeveledBox(size, size, 0.06f, 0.01f, L.DialRim), g.At(s * r * 0.8f, cy + c * r * 0.8f - size * 0.5f, 0.15f));
        }
    }

    internal static void Collide(SiteColliders c, IReadOnlyList<Vector2> ring, float y)
    {
        var (f, hx, hz) = FrameOf(ring);
        foreach (var (face, width, door) in Faces(f, hx, hz))
        {
            var half = width * 0.5f + 0.35f;
            var p = X.PortalWidth * 0.5f;
            c.Wall(face.P(-half, 0), face.P(-p, 0), 0.45f, y, y + X.ShaftTop + 0.8f, SurfaceTag.Wall, FootMat.StonePaving);
            c.Wall(face.P(p, 0), face.P(half, 0), 0.45f, y, y + X.ShaftTop + 0.8f, SurfaceTag.Wall, FootMat.StonePaving);
            // Above the portal the wall continues; the portal itself is open on the door face and closed elsewhere.
            c.Wall(face.P(-p, 0), face.P(p, 0), 0.45f, y + X.PortalSpring + p, y + X.ShaftTop + 0.8f, SurfaceTag.Wall, FootMat.StonePaving);
            if (!door) c.Wall(face.P(-p, 0), face.P(p, 0), 0.45f, y, y + X.PortalSpring + p, SurfaceTag.DoorClosed, FootMat.Glass);
        }
    }

    private static float Length(Vector2 v) => MathF.Sqrt(v.X * v.X + v.Y * v.Y);
}
