using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using S = Campus.Gen.Parts.Street.StreetStyle;

namespace Campus.Gen.Parts.Street;

/// <summary>One end of a car body rounded off: how far along the car the rounding runs, and how much it takes off the top, the sides and the underside.</summary>
public readonly record struct CarRounding(float Along, float Top, float Side, float Bottom);

/// <summary>A parked car's body shape (<see cref="StreetStyle.Saloon"/>, <see cref="StreetStyle.Hatch"/>; the numbers are explained there).</summary>
public sealed record CarShape(float Belt, float HoodNose, float TailTop, float Cowl, float RoofFront, float RoofRear, float RearBase,
    float FrontAxle, float RearAxle, CarRounding Nose, CarRounding Tail, float BPillar, float RearGlassTop, float RearGlassFoot);

/// <summary>
/// The parked car's geometry (Opus review 13c2b 21:05): a lofted lower body with rounded nose and tail, chamfered shoulders and a tucked
/// sill, cut by wheel arches over 0.65 m wheels, its underside 0.15 m off the ground; a glasshouse set in from the belt behind a ledge and
/// leaning in, its roof edges chamfered; flat glass panes on it inside body-colour pillars, sill strip and roof rail. Nose at −Z.
/// </summary>
internal static class CarBody
{
    private readonly record struct Station(float Z, float Inset, int End, bool Crease);

    public static PartGeometry Build(VehicleParams p, LodLevel lod, Box3 bounds)
    {
        var shape = p.Variation == 1 ? S.Hatch : S.Saloon;
        var near = lod == LodLevel.L0;
        var far = lod >= LodLevel.L2;
        var body = S.Casing(S.BodyColour(p.Material, S.CarBodies[0]));
        var glass = S.Board(S.CarGlass);
        var d = p.Depth; var h = p.Height;
        var zN = -d * 0.5f; var zT = d * 0.5f;
        var hw = p.Width * 0.5f - S.CarMirrorOut;
        var zCowl = zN + shape.Cowl * d; var zRoofF = zN + shape.RoofFront * d;
        var zRoofR = zT - shape.RoofRear * d; var zRear = zT - shape.RearBase * d;
        float[] axles = [zN + shape.FrontAxle * d, zT - shape.RearAxle * d];
        var belt = shape.Belt;
        var r = S.WheelRadius; var ra = S.ArchRadius;
        var b = new PartBuilder();

        // ---- Lower body: hood, sides, boot; the arches cut its underside over the wheels ----
        float Top(float z) => z <= zCowl ? Lerp(shape.HoodNose, belt, (z - zN) / (zCowl - zN))
            : z <= zRear ? belt : Lerp(belt, shape.TailTop, (z - zRear) / (zT - zRear));
        float Under(float z)
        {
            if (!far)
                foreach (var a in axles)
                    if (MathF.Abs(z - a) <= ra + 1e-4f) return r + (float)Math.Sqrt(Math.Max(0.0, (double)ra * ra - (double)(z - a) * (z - a)));
            return S.CarClearance;
        }
        var ends = near ? 3 : far ? 1 : 2;
        var arc = near ? 6 : 3;
        var stations = new List<Station>();
        for (var s = ends; s >= 0; s--)
        {
            var theta = 0.5 * Math.PI * s / ends;
            stations.Add(new(zN + shape.Nose.Along * (1f - (float)DetMath.Sin(theta)), 1f - (float)DetMath.Cos(theta), -1, false));
            stations.Add(new(zT - shape.Tail.Along * (1f - (float)DetMath.Sin(theta)), 1f - (float)DetMath.Cos(theta), 1, false));
        }
        if (!far)
            foreach (var a in axles)
            {
                stations.Add(new(a - ra - S.ArchFoot, 0, 0, true));
                stations.Add(new(a + ra + S.ArchFoot, 0, 0, true));
                for (var k = 0; k <= arc; k++)
                    stations.Add(new(a - ra * (float)DetMath.Cos(Math.PI * k / arc), 0, 0, k == 0 || k == arc));
            }
        stations.Add(new(zCowl, 0, 0, true));
        stations.Add(new(zRear, 0, 0, true));
        var lower = Merge(stations);
        foreach (var st in lower)
            if (st.End == 0 && (st.Z < zN + shape.Nose.Along || st.Z > zT - shape.Tail.Along)) throw new InvalidOperationException("A car's arches or windscreen reach into its rounded ends.");
        var rings = new List<Vector2[]>();
        foreach (var st in lower)
        {
            var round = st.End < 0 ? shape.Nose : st.End > 0 ? shape.Tail : default;
            var x = hw - round.Side * st.Inset;
            var top = Top(st.Z) - round.Top * st.Inset;
            var under = Under(st.Z) + round.Bottom * st.Inset;
            if (near)
            {
                var ct = Math.Min(S.CarShoulder, (top - under) * 0.3f); var cb = Math.Min(S.CarSillTuck, (top - under) * 0.3f);
                rings.Add([new(x, under + cb), new(x, top - ct), new(x - ct, top), new(-x + ct, top), new(-x, top - ct), new(-x, under + cb), new(-x + cb, under), new(x - cb, under)]);
            }
            else rings.Add([new(x, under), new(x, top), new(-x, top), new(-x, under)]);
        }
        bool[] lowerChamfers = near ? [false, true, false, true, false, true, false, true] : [false, false, false, false];
        b.Add(Loft.Solid([.. lower.Select(s => s.Z)], rings, lowerChamfers, [.. lower.Select(s => s.Crease)], roundEnds: true, body));

        // ---- Glasshouse: in from the body side behind a ledge, leaning in; feet sunk into the body at the windscreen and rear window ----
        var yB = belt - 0.03f;
        float XAt(float y) => hw - S.CarLedge - (y - yB) * S.CarTumblehome;
        var footTop = belt + 0.025f; var footRoof = belt + 0.04f; const float footEdge = 0.012f;
        var railTop = h - S.CarRoofEdge;
        float[] gz = [zCowl, zRoofF, zRoofR, zRear];
        float[] gTop = [footTop, railTop, railTop, footTop];
        float[] gRoof = [footRoof, h, h, footRoof];
        float[] gEdge = [footEdge, S.CarRoofEdge, S.CarRoofEdge, footEdge];
        var house = new List<Vector2[]>();
        for (var i = 0; i < 4; i++)
        {
            var xb = XAt(yB);
            if (near)
            {
                var xt = XAt(gTop[i]);
                house.Add([new(xb, yB), new(xt, gTop[i]), new(xt - gEdge[i], gRoof[i]), new(-xt + gEdge[i], gRoof[i]), new(-xt, gTop[i]), new(-xb, yB)]);
            }
            else
            {
                var xr = XAt(gRoof[i]);
                house.Add([new(xb, yB), new(xr, gRoof[i]), new(-xr, gRoof[i]), new(-xb, yB)]);
            }
        }
        bool[] houseChamfers = near ? [false, true, false, true, false, false] : [false, false, false, false];
        b.Add(Loft.Solid(gz, house, houseChamfers, [true, true, true, true], roundEnds: false, body));

        // ---- Glass: windscreen and rear window inside their surrounds, side glass between the sill strip and the roof rail ----
        Vector3 Band(int i, float side) => new(side * (XAt(gTop[i]) - gEdge[i]), gRoof[i], gz[i]);
        void Screen(int i0, int i1, float footGap, float headGap, bool footFirst)
        {
            var a0 = Band(i0, 1f); var a1 = Band(i1, 1f);
            var len = Vector2.Distance(new(a0.Y, a0.Z), new(a1.Y, a1.Z));
            var v0 = (footFirst ? footGap : headGap) / len; var v1 = 1f - (footFirst ? headGap : footGap) / len;
            Vector3 At(float v, float side)
            {
                var half = Lerp(a0.X, a1.X, v) - S.PillarA;
                return new(side * half, Lerp(a0.Y, a1.Y, v), Lerp(a0.Z, a1.Z, v));
            }
            var normal = new Vector3(0, a1.Z - a0.Z, -(a1.Y - a0.Y));
            Pane(b, [At(v0, 1f), At(v1, 1f), At(v1, -1f), At(v0, -1f)], normal, glass);
        }
        Screen(0, 1, 0.03f, 0.05f, footFirst: true);
        Screen(2, 3, 0.03f, 0.05f, footFirst: false);
        var ySill = belt + S.WindowSill; var yTop = railTop - S.WindowRail;
        // The A and C pillar lines in the side plane (z, y), moved a pillar's width into the side.
        (Vector2 P, float Slope) Offset(Vector2 from, Vector2 to)
        {
            var dir = Vector2.Normalize(to - from);
            var inward = new Vector2(MathF.Abs(dir.Y), -MathF.Abs(dir.X));
            if (dir.Y < 0) inward = new(-MathF.Abs(dir.Y), -MathF.Abs(dir.X));
            return (from + inward * S.PillarA, dir.X / dir.Y);
        }
        var aLine = Offset(new(zCowl, footTop), new(zRoofF, railTop));
        var cLine = Offset(new(zRoofR, railTop), new(zRear, footTop));
        float On((Vector2 P, float Slope) line, float y) => line.P.X + (y - line.P.Y) * line.Slope;
        var zB = Lerp(zRoofF, zRoofR, shape.BPillar);
        Vector2[] front = [new(Math.Max(On(aLine, ySill), zCowl + S.MirrorSail), ySill), new(On(aLine, yTop), yTop), new(zB - S.PillarB * 0.5f, yTop), new(zB - S.PillarB * 0.5f, ySill)];
        Vector2[] rear = [new(zB + S.PillarB * 0.5f, ySill), new(zB + S.PillarB * 0.5f, yTop), new(Math.Min(On(cLine, yTop), zRoofR - shape.RearGlassTop), yTop),
            new(Math.Min(On(cLine, ySill), zRoofR - shape.RearGlassFoot), ySill)];
        Vector2[] whole = [front[0], front[1], rear[2], rear[3]];
        foreach (var side in new[] { 1f, -1f })
        {
            var normal = new Vector3(side, S.CarTumblehome, 0);
            Vector3[] Map(Vector2[] outline) => [.. outline.Select(q => new Vector3(side * XAt(q.Y), q.Y, q.X))];
            if (far) Pane(b, Map(whole), normal, glass);
            else { Pane(b, Map(front), normal, glass); Pane(b, Map(rear), normal, glass); }
        }

        // ---- Wheels under the arches, dark wells inboard of them ----
        var w = S.WheelWidth;
        var wheelX = hw - S.WheelInset - w * 0.5f;
        var tyre = S.Pole(S.Tyre);
        foreach (var a in axles)
        {
            if (far)
            {
                // Beyond L1 the arches close: a dark block under the body stands for each axle's wheels.
                b.AddBox(new(-wheelX - w * 0.5f, 0, a - r * 0.92f), new(wheelX + w * 0.5f, S.CarClearance + 0.06f, a + r * 0.92f), 0, S.Casing(S.Tyre), VertexFlag.NoShadow);
                continue;
            }
            if (near)
                b.AddBox(new(-wheelX + w * 0.5f + 0.005f, r - 0.04f, a - ra + 0.005f), new(wheelX - w * 0.5f - 0.005f, r + ra + 0.04f, a + ra - 0.005f), 0, S.Casing(S.DarkSteel), VertexFlag.NoShadow);
            foreach (var sx in new[] { -1f, 1f })
            {
                // XUp turns the lathe's axis (local Y) onto −X: the outer face (local −Y) faces +X, so flip for −X.
                var at = (sx > 0 ? Xf.XUp : Xf.XUp.Then(Xf.RotY(180f))).Then(Xf.Move(sx * wheelX, r, a));
                b.AddLathe(Shapes.Creased(new(0, -w * 0.5f), new(r, -w * 0.5f), new(r, w * 0.5f), new(0, w * 0.5f)), near ? 18 : 6, tyre, at);
                if (near)
                    b.AddLathe(Shapes.Creased(new(0, -w * 0.5f - 0.008f), new(r * S.HubRatio, -w * 0.5f - 0.008f), new(r * S.HubRatio, -w * 0.5f + 0.004f), new(0, -w * 0.5f + 0.004f)),
                        12, S.Pole(S.GreyMetal), at, VertexFlag.NoShadow);
            }
        }
        if (!near) return b.Finish(bounds);

        // ---- Near only: bumpers parted from the nose and tail by a line, lamps (never lit, WLD-LIFE §3.4), the intake, wing mirrors ----
        var noseX = hw - shape.Nose.Side; var noseTop = shape.HoodNose - shape.Nose.Top; var noseUnder = S.CarClearance + shape.Nose.Bottom;
        var tailX = hw - shape.Tail.Side; var tailTop = shape.TailTop - shape.Tail.Top; var tailUnder = S.CarClearance + shape.Tail.Bottom;
        var bump = S.CarBumper;
        // Unbevelled: the convex edge line stands in at this size (ART-STYLE §4.7), and the car keeps to its 1200 triangles.
        b.AddBox(new(-noseX + 0.06f, noseUnder + 0.06f, zN - bump), new(noseX - 0.06f, noseUnder + 0.24f, zN + 0.12f), 0, body, VertexFlag.NoShadow);
        b.AddBox(new(-tailX + 0.06f, tailUnder + 0.06f, zT - 0.12f), new(tailX - 0.06f, tailUnder + 0.24f, zT + bump), 0, body, VertexFlag.NoShadow);
        b.AddBox(new(-noseX + 0.3f, noseUnder + 0.1f, zN - bump - 0.008f), new(noseX - 0.3f, noseUnder + 0.19f, zN + 0.04f), 0, S.Casing(S.DarkSteel), VertexFlag.NoShadow);
        foreach (var sx in new[] { -1f, 1f })
        {
            float X0(float outer, float span) => sx > 0 ? outer - span : -outer;
            float X1(float outer, float span) => sx > 0 ? outer : -outer + span;
            b.AddBox(new(X0(noseX - 0.07f, 0.27f), noseTop - 0.13f, zN - 0.012f), new(X1(noseX - 0.07f, 0.27f), noseTop - 0.05f, zN + 0.04f), 0, S.Board(S.HeadLamp), VertexFlag.NoShadow);
            b.AddBox(new(X0(tailX - 0.05f, 0.36f), tailTop - 0.16f, zT - 0.04f), new(X1(tailX - 0.05f, 0.36f), tailTop - 0.05f, zT + 0.012f), 0, S.Board(S.TailLamp), VertexFlag.NoShadow);
            var mirror = XAt(belt + 0.1f);
            b.AddBox(new(sx > 0 ? mirror - 0.03f : -p.Width * 0.5f, belt + 0.06f, zCowl + 0.12f), new(sx > 0 ? p.Width * 0.5f : -mirror + 0.03f, belt + 0.15f, zCowl + 0.27f), 0, body, VertexFlag.NoShadow);
        }
        return b.Finish(bounds);
    }

    /// <summary>Stations in Z order; two closer than 12 mm become one (a crease if either was).</summary>
    private static List<Station> Merge(List<Station> stations)
    {
        var sorted = stations.OrderBy(s => s.Z).ThenBy(s => -Math.Abs(s.End)).ToList();
        var kept = new List<Station>();
        foreach (var s in sorted)
        {
            if (kept.Count > 0 && s.Z - kept[^1].Z < 0.012f)
            {
                kept[^1] = kept[^1] with { Crease = kept[^1].Crease || s.Crease };
                continue;
            }
            kept.Add(s);
        }
        return kept;
    }

    /// <summary>
    /// A flat glass pane on a body face: the convex <paramref name="outline"/> (any winding) as a closed slab <see cref="StreetStyle.PaneProud"/>
    /// proud of the face along <paramref name="outward"/> and as deep into it.
    /// </summary>
    private static void Pane(PartBuilder b, IReadOnlyList<Vector3> outline, Vector3 outward, ToonSpec material)
    {
        var n = Xf.Unit(outward);
        var ring = outline.ToList();
        var newell = Vector3.Zero;
        for (var i = 0; i < ring.Count; i++) newell += Cross(ring[i], ring[(i + 1) % ring.Count]);
        if (Vector3.Dot(newell, n) < 0) ring.Reverse();
        var writer = new MeshWriter(material);
        var top = ring.Select(q => q + n * S.PaneProud).ToArray();
        var bottom = ring.Select(q => q - n * S.PaneProud).ToArray();
        void Fan(Vector3[] face, Vector3 normal)
        {
            var ids = face.Select(q => writer.Vertex(q, normal, new(q.Z, q.Y))).ToArray();
            for (var i = 1; i + 1 < ids.Length; i++) writer.Triangle(ids[0], ids[i], ids[i + 1], normal);
        }
        Fan(top, n);
        Fan(bottom, -n);
        for (var i = 0; i < ring.Count; i++)
        {
            var j = (i + 1) % ring.Count;
            var side = Xf.Unit(Cross(ring[j] - ring[i], n));
            var a = writer.Vertex(top[i], side, Vector2.Zero); var c = writer.Vertex(top[j], side, Vector2.Zero);
            var e = writer.Vertex(bottom[j], side, Vector2.Zero); var f = writer.Vertex(bottom[i], side, Vector2.Zero);
            writer.Triangle(a, c, e, side); writer.Triangle(a, e, f, side);
        }
        b.Add(writer.Finish(), VertexFlag.NoShadow);
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    internal static Vector3 Cross(Vector3 a, Vector3 b) => new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
}

/// <summary>
/// A closed solid lofted through sections along Z (car bodies). Every station's ring has the same count of points, counterclockwise
/// seen from +Z; the ends are flat caps. Band k runs from ring point k to k + 1: a chamfer band's two edges take the normals of the faces
/// beside it, a face band's its own (ART-MODELING §7.2). Along the loft, normals follow the neighbouring stations, one-sided at a crease;
/// with rounded ends the end stations take the caps' normals (the ends round off into them).
/// </summary>
internal static class Loft
{
    public static PartGeometry Solid(IReadOnlyList<float> z, IReadOnlyList<Vector2[]> rings, IReadOnlyList<bool> chamfer, IReadOnlyList<bool> crease, bool roundEnds, ToonSpec material)
    {
        var m = z.Count; var n = rings[0].Length;
        if (m < 2 || rings.Count != m || crease.Count != m || chamfer.Count != n || rings.Any(r => r.Length != n)) throw new ArgumentException("A loft needs matching stations, rings and flags.");
        for (var i = 1; i < m; i++)
            if (!(z[i] > z[i - 1])) throw new ArgumentException("Loft stations must ascend along Z.");
        var w = new MeshWriter(material);
        Vector3 P(int i, int j) { var q = rings[i][(j % n + n) % n]; return new(q.X, q.Y, z[i]); }
        Vector3 Normal(int i, int j, int band, int gap)
        {
            if (roundEnds && (i == 0 || i == m - 1)) return new(0, 0, i == 0 ? -1 : 1);
            var lo = i == 0 || (crease[i] && gap == i) ? i : i - 1;
            var hi = i == m - 1 || (crease[i] && gap == i - 1) ? i : i + 1;
            var along = P(hi, j) - P(lo, j);
            int a, b;
            if (!chamfer[band]) { a = band; b = band + 1; }
            else if (((j - band) % n + n) % n == 0) { a = band - 1; b = band; }
            else { a = band + 1; b = band + 2; }
            return Xf.Unit(CarBody.Cross(P(i, b) - P(i, a), along));
        }
        for (var g = 0; g + 1 < m; g++)
            for (var k = 0; k < n; k++)
            {
                var A = P(g, k); var B = P(g, k + 1); var C = P(g + 1, k + 1); var D = P(g + 1, k);
                var ia = w.Vertex(A, Normal(g, k, k, g), new(A.Z, A.Y)); var ib = w.Vertex(B, Normal(g, k + 1, k, g), new(B.Z, B.Y));
                var ic = w.Vertex(C, Normal(g + 1, k + 1, k, g), new(C.Z, C.Y)); var id = w.Vertex(D, Normal(g + 1, k, k, g), new(D.Z, D.Y));
                w.Triangle(ia, ib, ic, CarBody.Cross(B - A, C - A));
                w.Triangle(ia, ic, id, CarBody.Cross(C - A, D - A));
            }
        foreach (var e in new[] { 0, m - 1 })
        {
            var normal = new Vector3(0, 0, e == 0 ? -1 : 1);
            // A fan from the section's centre: a ring may run straight through several points (a side over its waist at the tip).
            var ids = new int[n];
            var centre = Vector3.Zero;
            for (var j = 0; j < n; j++) { var q = P(e, j); ids[j] = w.Vertex(q, normal, new(q.X, q.Y)); centre += q; }
            centre /= n;
            var middle = w.Vertex(centre, normal, new(centre.X, centre.Y));
            for (var j = 0; j < n; j++) w.Triangle(middle, ids[j], ids[(j + 1) % n], normal);
        }
        return w.Finish();
    }
}
