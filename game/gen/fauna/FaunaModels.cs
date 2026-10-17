using System.Numerics;
using Campus.Contracts;
using S = Campus.Gen.Fauna.FaunaStyle;

namespace Campus.Gen.Fauna;

public enum CatCoat : byte { GingerWhite, Calico, White, Black }
public enum CatPose : byte { Sit, Loaf, Curl }
public enum BirdPose : byte { Stand, Peck, Perch }
public enum SwanPose : byte { Float, Sleep }

/// <summary>
/// The 25a animal models (WLD-LIFE): cats in three poses and four coats, the plaza pigeon and the sparrow. Built in the
/// model frame (metres, +Z forward, +Y up, feet on y = 0) from soft lofts and ellipsoids, true-to-size before
/// <see cref="FaunaStyle.Scale"/>; cartoon-clean but with real proportions (head about a fifth of a sitting cat, ears and tail
/// carrying the silhouette). Static poses only: idle motion (pivot parts in vertex()) waits for the motion shader (SYS-MOTION).
/// </summary>
public static class FaunaModels
{
    private static readonly Vector3 Up = Vector3.UnitY;
    private static readonly Vector3 Fwd = Vector3.UnitZ;

    public static AnimalMesh Cat(CatCoat coat, CatPose pose) => pose switch
    {
        CatPose.Sit => CatSit(coat),
        CatPose.Loaf => CatLoaf(coat),
        CatPose.Curl => CatCurl(coat),
        _ => throw new ArgumentOutOfRangeException(nameof(pose), pose, "Unknown cat pose."),
    };

    public static AnimalMesh Pigeon(BirdPose pose, bool white) => pose switch
    {
        BirdPose.Stand or BirdPose.Perch => PigeonBuild(false, white),
        BirdPose.Peck => PigeonBuild(true, white),
        _ => throw new ArgumentOutOfRangeException(nameof(pose), pose, "Unknown pigeon pose."),
    };

    public static AnimalMesh Sparrow(BirdPose pose) => pose switch
    {
        BirdPose.Stand => SparrowBuild(false, false),
        BirdPose.Peck => SparrowBuild(true, false),
        BirdPose.Perch => SparrowBuild(false, true),
        _ => throw new ArgumentOutOfRangeException(nameof(pose), pose, "Unknown sparrow pose."),
    };

    /// <summary>Black swan on the water (origin on the water line): gliding with the neck in an S, or asleep with the head on the back.</summary>
    public static AnimalMesh Swan(SwanPose pose) => pose switch
    {
        SwanPose.Float => SwanBuild(false),
        SwanPose.Sleep => SwanBuild(true),
        _ => throw new ArgumentOutOfRangeException(nameof(pose), pose, "Unknown swan pose."),
    };

    /// <summary>Dusk crow perched on a parapet: a still silhouette, heavy bill, long tail.</summary>
    public static AnimalMesh Crow() => CrowBuild();

    // ---------------------------------------------------------------- cats

    /// <summary>Coat colours by region: back and top, chest bib and belly, socks, muzzle, tail, eyes, nose.</summary>
    private sealed record Coat(Paint Body, Paint Head, Paint Legs, Paint Tail, PaletteToken Muzzle, PaletteToken Eyes, PaletteToken Nose, PaletteToken Ears);

    private static Coat CoatOf(CatCoat coat, float bibFront, float bibTop)
    {
        PaletteToken Bib(Vector3 p, Vector3 n, PaletteToken top) =>
            n.Y < -0.55f || (p.Z > bibFront && p.Y < bibTop && MathF.Abs(p.X) < 0.055f && n.Z > -0.1f) ? S.White : top;
        return coat switch
        {
            CatCoat.GingerWhite => new(
                (p, n) => Bib(p, n, S.Ginger),
                (p, n) => n.Y < -0.35f && n.Z > 0f ? S.White : S.Ginger,
                (p, n) => p.Y < 0.06f ? S.White : S.Ginger,
                (p, n) => S.Ginger, S.White, S.Ink, S.Rosy, S.Ginger),
            // Calico: white ground with three big clean patches (ginger saddle on the left, ink rump on the right, a split cap),
            // white chest, legs and muzzle; patch borders kept few so the outline stays quiet.
            CatCoat.Calico => new(
                (p, n) => Bib(p, n, p.X < -0.015f && p.Y > 0.05f && p.Z < 0.03f && p.Z > -0.14f ? S.Ginger
                    : p.X > 0.03f && p.Z < -0.03f && p.Y > 0.05f ? S.Ink : S.White),
                (p, n) => n.Y > 0.55f || (n.Z < -0.2f && n.Y > 0.1f) ? (p.X >= 0 ? S.Ink : S.Ginger) : S.White,
                (p, n) => S.White,
                (p, n) => S.Ink, S.White, S.Ink, S.Rosy, S.Ginger),
            CatCoat.White => new((p, n) => S.White, (p, n) => S.White, (p, n) => S.White, (p, n) => S.White, S.White, S.Amber, S.Rosy, S.White),
            CatCoat.Black => new((p, n) => S.Ink, (p, n) => S.Ink, (p, n) => S.Ink, (p, n) => S.Ink, S.Ink, S.Amber, S.Ink, S.Ink),
            _ => throw new ArgumentOutOfRangeException(nameof(coat), coat, "Unknown coat."),
        };
    }

    private static Paint Solid(PaletteToken token) => (_, _) => token;

    /// <summary>Head, cheeks, muzzle, nose, eyes and ears around a head centre; the face looks along +Z, tilted by <paramref name="lookDown"/>.</summary>
    private static void CatHead(OrganicMesh m, Coat c, Vector3 h, bool eyesClosed = false)
    {
        m.Blob(h, new(0.058f, 0.05f, 0.05f), c.Head, 14, 8);
        m.Blob(h + new Vector3(0, -0.014f, 0.006f), new(0.065f, 0.033f, 0.043f), c.Head, 14, 7);
        m.Blob(h + new Vector3(0, -0.017f, 0.043f), new(0.026f, 0.018f, 0.016f), Solid(c.Muzzle), 10, 6);
        m.Blob(h + new Vector3(0, -0.007f, 0.058f), new(0.008f, 0.0055f, 0.0045f), Solid(c.Nose), 6, 4);
        foreach (var side in new[] { -1f, 1f })
        {
            var eye = h + new Vector3(side * 0.024f, 0.006f, 0.045f);
            m.Blob(eye, eyesClosed ? new(0.012f, 0.0028f, 0.004f) : new(0.0082f, 0.0098f, 0.0045f), Solid(c.Eyes), 8, 5);
            var earBase = h + new Vector3(side * 0.033f, 0.036f, -0.006f);
            var earTip = h + new Vector3(side * 0.047f, 0.083f, -0.012f);
            m.Spike(earBase, earTip, 0.022f, 0.010f, Solid(c.Ears), 8);
        }
    }

    private static AnimalMesh CatSit(CatCoat coat)
    {
        var c = CoatOf(coat, 0.03f, 0.235f);
        var m = new OrganicMesh();
        m.Blob(new(0, 0.072f, -0.035f), new(0.085f, 0.072f, 0.088f), c.Body, 16, 9);                       // rump on the ground
        m.Loft([new(new(0, 0.07f, -0.035f), 0.082f, 0.076f), new(new(0, 0.125f, -0.002f), 0.074f, 0.07f),
            new(new(0, 0.17f, 0.022f), 0.064f, 0.062f), new(new(0, 0.212f, 0.036f), 0.048f, 0.048f)], c.Body, 16, 0.6f, 0.9f, Fwd);
        foreach (var side in new[] { -1f, 1f })
        {
            m.Blob(new(side * 0.058f, 0.07f, -0.04f), new(0.04f, 0.062f, 0.072f), c.Body, 12, 7);           // haunches
            m.Tube([new(side * 0.026f, 0.13f, 0.052f), new(side * 0.026f, 0.075f, 0.068f), new(side * 0.025f, 0.024f, 0.078f)],
                0.02f, 0.017f, c.Legs, 10);
            m.Blob(new(side * 0.03f, 0.014f, 0.086f), new(0.02f, 0.014f, 0.027f), c.Legs, 10, 6);           // front paws
            m.Blob(new(side * 0.062f, 0.012f, 0.022f), new(0.019f, 0.012f, 0.034f), c.Legs, 10, 6);         // hind feet
        }
        CatHead(m, c, new(0, 0.268f, 0.055f));
        m.Tube([new(0.03f, 0.03f, -0.115f), new(0.085f, 0.02f, -0.1f), new(0.115f, 0.016f, -0.04f), new(0.105f, 0.015f, 0.03f),
            new(0.072f, 0.015f, 0.082f)], 0.018f, 0.013f, c.Tail, 10, 0.85f);
        return m.Finish();
    }

    private static AnimalMesh CatLoaf(CatCoat coat)
    {
        var c = CoatOf(coat, 0.09f, 0.14f);
        var m = new OrganicMesh();
        m.Loft([new(new(0, 0.068f, -0.16f), 0.06f, 0.055f), new(new(0, 0.082f, -0.1f), 0.085f, 0.074f), new(new(0, 0.086f, -0.02f), 0.09f, 0.08f),
            new(new(0, 0.086f, 0.06f), 0.08f, 0.076f), new(new(0, 0.098f, 0.11f), 0.06f, 0.06f)], c.Body, 16, 0.7f, 0.6f);
        foreach (var side in new[] { -1f, 1f })
        {
            m.Blob(new(side * 0.056f, 0.072f, -0.09f), new(0.04f, 0.058f, 0.07f), c.Body, 12, 7);           // haunches
            m.Blob(new(side * 0.03f, 0.016f, 0.13f), new(0.02f, 0.015f, 0.03f), c.Legs, 10, 6);             // tucked front paws
        }
        CatHead(m, c, new(0, 0.168f, 0.14f));
        m.Tube([new(0.03f, 0.03f, -0.17f), new(0.08f, 0.02f, -0.145f), new(0.1f, 0.016f, -0.065f), new(0.096f, 0.015f, 0.02f),
            new(0.076f, 0.015f, 0.072f)], 0.018f, 0.013f, c.Tail, 10, 0.85f);
        return m.Finish();
    }

    private static AnimalMesh CatCurl(CatCoat coat)
    {
        var c = CoatOf(coat, 0.2f, 0.0f);   // curled: the bib is hidden, only the outer coat shows
        var m = new OrganicMesh();
        m.Blob(new(0.012f, 0.064f, -0.012f), new(0.108f, 0.064f, 0.128f), c.Body, 16, 9);
        m.Blob(new(0.05f, 0.06f, -0.07f), new(0.06f, 0.055f, 0.07f), c.Body, 12, 7);                         // haunch bulge
        var h = new Vector3(-0.058f, 0.07f, 0.078f);
        m.Blob(h, new(0.054f, 0.044f, 0.05f), c.Head, 14, 8);
        m.Blob(h + new Vector3(0.0f, -0.012f, 0.038f), new(0.024f, 0.016f, 0.015f), Solid(c.Muzzle), 10, 6);
        m.Blob(h + new Vector3(0, -0.004f, 0.052f), new(0.007f, 0.005f, 0.004f), Solid(c.Nose), 6, 4);
        foreach (var side in new[] { -1f, 1f })
        {
            m.Blob(h + new Vector3(side * 0.022f, 0.006f, 0.041f), new(0.012f, 0.0028f, 0.004f), Solid(c.Eyes), 8, 5);   // closed eyes
            m.Spike(h + new Vector3(side * 0.031f, 0.03f, -0.008f), h + new Vector3(side * 0.045f, 0.074f, -0.016f), 0.021f, 0.0095f, Solid(c.Ears), 8);
        }
        m.Tube([new(0.1f, 0.03f, -0.09f), new(0.125f, 0.025f, -0.005f), new(0.098f, 0.022f, 0.085f), new(0.03f, 0.02f, 0.13f),
            new(-0.04f, 0.018f, 0.134f)], 0.02f, 0.015f, c.Tail, 10, 0.85f);
        return m.Finish();
    }

    // ---------------------------------------------------------------- pigeon

    private static AnimalMesh PigeonBuild(bool peck, bool white)
    {
        var body = white ? S.White : S.Dove;
        var neck = white ? S.White : S.Slate;
        var bar = white ? S.White : S.Ink;
        var m = new OrganicMesh();
        Paint torso = (p, n) => p.Y > (peck ? 0.09f : 0.15f) && p.Z > (peck ? 0.06f : 0.045f) ? neck : body;
        if (!peck)
            m.Loft([new(new(0, 0.1f, -0.11f), 0.03f, 0.025f), new(new(0, 0.11f, -0.06f), 0.055f, 0.055f), new(new(0, 0.115f, 0f), 0.065f, 0.068f),
                new(new(0, 0.13f, 0.05f), 0.06f, 0.064f), new(new(0, 0.16f, 0.076f), 0.042f, 0.045f), new(new(0, 0.19f, 0.087f), 0.03f, 0.032f)],
                torso, 14, 0.7f, 0.5f);
        else
            m.Loft([new(new(0, 0.115f, -0.1f), 0.03f, 0.025f), new(new(0, 0.112f, -0.05f), 0.055f, 0.055f), new(new(0, 0.108f, 0.01f), 0.065f, 0.066f),
                new(new(0, 0.098f, 0.058f), 0.058f, 0.06f), new(new(0, 0.078f, 0.098f), 0.04f, 0.042f), new(new(0, 0.056f, 0.124f), 0.03f, 0.03f)],
                torso, 14, 0.7f, 0.5f);
        var head = peck ? new Vector3(0, 0.042f, 0.146f) : new Vector3(0, 0.215f, 0.095f);
        var look = peck ? Vector3.Normalize(new Vector3(0, -0.75f, 0.66f)) : Vector3.Normalize(new Vector3(0, -0.25f, 0.97f));
        m.Ellipsoid(head, look, Up, 0.034f, 0.028f, 0.03f, Solid(neck), 12, 7);
        var beakBase = head + look * 0.03f;
        m.Spike(beakBase, beakBase + look * 0.024f, 0.0085f, 0.0065f, Solid(S.Slate), 8);
        m.Blob(beakBase + Up * 0.006f - look * 0.002f, new(0.008f, 0.005f, 0.006f), Solid(S.White), 6, 4);    // cere
        foreach (var side in new[] { -1f, 1f })
        {
            m.Blob(head + new Vector3(side * 0.022f, 0.007f, 0) + look * 0.01f, new(0.006f, 0.007f, 0.006f), Solid(S.Coral), 6, 4);
            // Folded wing along the side, two dark bars across the coverts.
            var wingCentre = new Vector3(side * 0.05f, peck ? 0.122f : 0.128f, -0.035f);
            var wingAxis = Vector3.Normalize(peck ? new Vector3(0, 0.12f, -1f) : new Vector3(0, -0.25f, -1f));
            m.Ellipsoid(wingCentre, wingAxis, Up, 0.1f, 0.018f, 0.045f,
                (p, n) => MathF.Abs(n.X) > 0.3f && ((p.Z < -0.035f && p.Z > -0.05f) || (p.Z < -0.065f && p.Z > -0.08f)) ? bar : body, 12, 8);
            m.Tube([new(side * 0.018f, 0.062f, 0.0f), new(side * 0.02f, 0.008f, 0.012f)], 0.0065f, 0.0055f, Solid(S.Coral), 6);
            m.Spike(new(side * 0.02f, 0.005f, 0.012f), new(side * 0.023f, 0.004f, 0.042f), 0.005f, 0.003f, Solid(S.Coral), 6);
        }
        var tailY = peck ? new[] { 0.115f, 0.132f, 0.148f } : new[] { 0.105f, 0.095f, 0.088f };
        m.Loft([new(new(0, tailY[0], -0.1f), 0.035f, 0.012f), new(new(0, tailY[1], -0.16f), 0.042f, 0.01f), new(new(0, tailY[2], -0.205f), 0.045f, 0.008f)],
            (p, n) => p.Z < -0.18f ? bar : body, 10, 0.4f, 0.4f);
        return m.Finish();
    }

    // ---------------------------------------------------------------- black swan

    private static AnimalMesh SwanBuild(bool sleep)
    {
        var m = new OrganicMesh();
        var ink = Solid(S.Ink);
        // Hull: low and long, the water line at y = 0 crosses its lower third; the stern rises into the tail.
        m.Loft([new(new(0, 0.09f, -0.52f), 0.05f, 0.03f), new(new(0, 0.07f, -0.38f), 0.17f, 0.11f), new(new(0, 0.06f, -0.16f), 0.23f, 0.15f),
            new(new(0, 0.06f, 0.06f), 0.21f, 0.15f), new(new(0, 0.08f, 0.22f), 0.13f, 0.11f)], ink, 18, 0.8f, 0.7f);
        // Ruffled wing feathers on the back (the black swan's curled plumes), kept to three soft lobes.
        m.Blob(new(0.075f, 0.17f, -0.24f), new(0.07f, 0.055f, 0.13f), ink, 12, 7);
        m.Blob(new(-0.075f, 0.17f, -0.24f), new(0.07f, 0.055f, 0.13f), ink, 12, 7);
        m.Blob(new(0, 0.2f, -0.33f), new(0.06f, 0.06f, 0.1f), ink, 12, 7);
        Vector3 head, look;
        if (!sleep)
        {
            m.Tube([new(0, 0.14f, 0.2f), new(0, 0.3f, 0.29f), new(0, 0.47f, 0.27f), new(0, 0.6f, 0.23f), new(0, 0.68f, 0.26f), new(0, 0.71f, 0.31f)],
                0.05f, 0.03f, ink, 12);
            head = new(0, 0.715f, 0.33f);
            look = Vector3.Normalize(new Vector3(0, -0.22f, 1f));
        }
        else
        {
            m.Tube([new(0, 0.14f, 0.18f), new(0, 0.24f, 0.17f), new(0, 0.3f, 0.07f), new(0.03f, 0.29f, -0.04f), new(0.06f, 0.27f, -0.09f)],
                0.05f, 0.032f, ink, 12);
            head = new(0.075f, 0.27f, -0.13f);
            look = Vector3.Normalize(new Vector3(0.15f, -0.25f, -1f));
        }
        m.Ellipsoid(head, look, Up, 0.058f, 0.034f, 0.038f, ink, 12, 7);
        var billBase = head + look * 0.048f;
        var billTip = billBase + look * 0.085f;
        var tipBand = Vector3.Dot(billTip - Vector3.Zero, look) - 0.016f;
        m.Spike(billBase, billTip, 0.018f, 0.012f, (p, n) => Vector3.Dot(p, look) > tipBand ? S.White : S.Coral, 10);
        if (!sleep)
            foreach (var side in new[] { -1f, 1f })
                m.Blob(head + new Vector3(side * 0.028f, 0.008f, 0) + look * 0.012f, new(0.006f, 0.007f, 0.006f), Solid(S.Coral), 6, 4);
        return m.Finish();
    }

    // ---------------------------------------------------------------- crow

    private static AnimalMesh CrowBuild()
    {
        var m = new OrganicMesh();
        var ink = Solid(S.Ink);
        var tilt = Vector3.Normalize(new Vector3(0, 0.75f, 1f));     // upright perch, tail hanging behind the coping
        var body = new Vector3(0, 0.13f, 0);
        m.Ellipsoid(body, tilt, Up, 0.12f, 0.07f, 0.075f, ink, 14, 8);
        var head = body + new Vector3(0, 0.13f, 0.07f);
        m.Blob(head, new(0.045f, 0.045f, 0.05f), ink, 12, 7);
        m.Spike(head + new Vector3(0, -0.005f, 0.04f), head + new Vector3(0, -0.015f, 0.105f), 0.016f, 0.02f, ink, 8);   // heavy bill
        m.Loft([new(body - tilt * 0.09f, 0.04f, 0.015f), new(body - tilt * 0.14f + new Vector3(0, 0.01f, -0.03f), 0.05f, 0.012f),
            new(body - tilt * 0.2f + new Vector3(0, 0.03f, -0.08f), 0.05f, 0.01f)], ink, 10, 0.4f, 0.4f);
        foreach (var side in new[] { -1f, 1f })
        {
            m.Ellipsoid(body + new Vector3(side * 0.05f, 0.0f, -0.03f), Vector3.Normalize(new Vector3(0, -0.5f, -1f)), Up, 0.12f, 0.02f, 0.055f, ink, 10, 6);
            m.Tube([new(side * 0.025f, 0.06f, 0.02f), new(side * 0.027f, 0.004f, 0.03f)], 0.009f, 0.007f, ink, 6);
        }
        return m.Finish();
    }

    // ---------------------------------------------------------------- sparrow

    private static AnimalMesh SparrowBuild(bool peck, bool perch)
    {
        var m = new OrganicMesh();
        var lift = perch ? 0.006f : 0.016f;   // perched: legs folded, the belly sits low on the wall top
        var tilt = peck ? Vector3.Normalize(new Vector3(0, -0.3f, 1f)) : Vector3.Normalize(new Vector3(0, 0.38f, 1f));
        var bodyCentre = new Vector3(0, 0.044f + lift, 0);
        m.Ellipsoid(bodyCentre, tilt, Up, 0.048f, 0.032f, 0.034f,
            (p, n) => n.Y < -0.2f || (n.Z > 0.5f && p.Y < bodyCentre.Y + 0.01f) ? S.Cream : S.Wren, 14, 8);
        var head = peck ? bodyCentre + new Vector3(0, -0.012f, 0.052f) : bodyCentre + new Vector3(0, 0.03f, 0.036f);
        var look = peck ? Vector3.Normalize(new Vector3(0, -0.6f, 0.8f)) : Vector3.UnitZ;
        m.Ellipsoid(head, look, Up, 0.025f, 0.024f, 0.024f, (p, n) =>
            n.Y > 0.45f || n.Z < -0.3f ? S.Crown
            : MathF.Abs(p.X) < 0.012f && n.Z > 0.25f && p.Y < head.Y - 0.006f ? S.Ink
            : S.Cream, 12, 7);
        var beakBase = head + look * 0.022f;
        m.Spike(beakBase, beakBase + look * 0.014f, 0.0055f, 0.005f, Solid(S.Slate), 6);
        foreach (var side in new[] { -1f, 1f })
        {
            m.Blob(head + new Vector3(side * 0.018f, 0.004f, 0) + look * 0.009f, new(0.0045f, 0.005f, 0.0045f), Solid(S.Ink), 6, 4);
            m.Ellipsoid(bodyCentre + new Vector3(side * 0.024f, 0.006f, -0.012f), Vector3.Normalize(tilt * new Vector3(1, -1.2f, 1)), Up,
                0.04f, 0.011f, 0.022f, Solid(S.Wren), 10, 6);
            if (!perch)
                m.Tube([new(side * 0.01f, bodyCentre.Y - 0.022f, 0.004f), new(side * 0.011f, 0.003f, 0.008f)], 0.003f, 0.0026f, Solid(S.Crown), 5);
        }
        var tailBase = bodyCentre - tilt * 0.04f;
        var tailEnd = tailBase + Vector3.Normalize(new Vector3(0, peck ? 0.45f : 0.1f, -1f)) * 0.05f;
        m.Loft([new(tailBase, 0.013f, 0.006f), new(tailEnd, 0.017f, 0.004f)], Solid(S.Wren), 8, 0.4f, 0.5f);
        return m.Finish();
    }
}
