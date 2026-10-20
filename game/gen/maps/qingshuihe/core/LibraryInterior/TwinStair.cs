using Campus.Gen.Interior;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using S = Campus.Gen.Maps.Qingshuihe.Core.LibraryInterior.LibraryInteriorStyle;

namespace Campus.Gen.Maps.Qingshuihe.Core.LibraryInterior;

/// <summary>
/// The pair of open 1F↔2F stairs on the far side of the hall (八字楼梯): from feet either side of the motto plaque each rises
/// straight to the middle of the W or N gallery face, a lower flight on a solid white base, a landing, then an open upper
/// flight on a sloped waist. White stringers, red-brown wood balustrade panels with a white cap, light grey stone treads.
/// Collision as WLD-INTERIORS-BUILD: one ramp per flight through the nosings (top flush, foot buried 0.2 m), the landing slab,
/// sloped barriers 1.5 m above the nosings on both open sides, and walls closing the solid base.
/// </summary>
internal static class TwinStair
{
    /// <summary>Riser, run of one flight and the whole stair's run on plan, from the storey table.</summary>
    public static (float Riser, float Flight, float Total) Section(LibraryFrame f)
    {
        var riser = (f.FloorY(2) - f.FloorY(1)) / (2 * S.FlightRisers);
        if (riser is < 0.12f or > 0.18f) throw new InvalidOperationException($"Twin stair riser {riser} m is outside 0.12–0.18 m.");
        var flight = (S.FlightRisers - 1) * S.Tread;
        return (riser, flight, 2 * flight + S.StairLanding);
    }

    /// <summary>Distance of a flight's foot from the centre, along its face direction.</summary>
    public static float FootRadius(LibraryFrame f) => f.VoidApothem - Section(f).Total;

    public static void Build(LibraryFrame f, InteriorGeometry sink)
    {
        foreach (var j in S.StairFaces) Flight(f, sink, j);
    }

    private static void Flight(LibraryFrame f, InteriorGeometry sink, int j)
    {
        var (riser, run, total) = Section(f);
        var foot = FootRadius(f);
        float y0 = f.FloorY(1), landing = y0 + S.FlightRisers * riser, y2 = f.FloorY(2);
        var up = f.Dir(j);
        var half = S.StairWidth * 0.5f;
        Vector2 P(float x, float t) => f.At(j, foot + x, t);
        var stone = S.Stone(S.Floor);
        var white = S.Wall(S.WallWhite);

        // Lower flight and landing: solid stepped base from the floor.
        for (var i = 1; i < S.FlightRisers; i++)
            sink.Box(stone, P((i - 0.5f) * S.Tread, 0f), up, S.Tread, S.StairWidth - 2 * S.Stringer, y0, y0 + i * riser);
        sink.Box(stone, P(run + S.StairLanding * 0.5f, 0f), up, S.StairLanding, S.StairWidth - 2 * S.Stringer, y0, landing);
        // Upper flight: step blocks on a sloped waist.
        var x0 = run + S.StairLanding;
        for (var i = 1; i < S.FlightRisers; i++)
            sink.Box(stone, P(x0 + (i - 0.5f) * S.Tread, 0f), up, S.Tread, S.StairWidth - 2 * S.Stringer, landing + (i - 1) * riser, landing + i * riser);
        float w = half - S.Stringer;
        Vector3 Q(float x, float t, float y) => LibraryFrame.V(P(x, t), y);
        sink.Hexa(white, [Q(x0, -w, landing - S.Waist), Q(total, -w, y2 - riser - S.Waist), Q(total, w, y2 - riser - S.Waist), Q(x0, w, landing - S.Waist)],
            [Q(x0, -w, landing), Q(total, -w, y2 - riser), Q(total, w, y2 - riser), Q(x0, w, landing)]);

        foreach (var s in new[] { -1f, 1f })
        {
            var t = s * (half - S.Stringer * 0.5f);
            // Nosing line + 0.1 is the stringer top; the base below the lower flight and landing is solid to the floor.
            Side(sink, Q, t, 0f, run, y0, y0, y0 + riser, landing);
            Side(sink, Q, t, run, x0, y0, y0, landing, landing);
            Side(sink, Q, t, x0, total, landing - S.Waist, y2 - riser - S.Waist, landing + riser, y2);
        }

        // Collision.
        var bury = (riser + 0.2f) * S.Tread / riser;
        sink.Colliders.Add(new RampCollider(Q(-bury, -half, y0 - 0.2f), Q(-bury, half, y0 - 0.2f), Q(run, half, landing), Q(run, -half, landing),
            S.Waist, SurfaceTag.Stair) { Foot = FootMat.PolishedStone, Stair = new(riser, S.Tread, S.FlightRisers) });
        sink.Colliders.Add(new SlabCollider([P(run, -half), P(run, half), P(x0, half), P(x0, -half)], landing, S.SlabThick, SurfaceTag.Ground)
            { Foot = FootMat.PolishedStone });
        sink.Colliders.Add(new RampCollider(Q(x0 - bury, -half, landing - 0.2f), Q(x0 - bury, half, landing - 0.2f), Q(total, half, y2), Q(total, -half, y2),
            S.Waist, SurfaceTag.Stair) { Foot = FootMat.PolishedStone, Stair = new(riser, S.Tread, S.FlightRisers) });
        foreach (var s in new[] { -1f, 1f })
        {
            var t = s * (half - S.Stringer * 0.5f);
            var barrier = S.Stringer * 0.5f;
            sink.Colliders.Add(new SlopedWallCollider(P(0f, t), P(run, t), barrier, y0, y0 + riser + WalkRules.BarrierHeight, y0, landing + WalkRules.BarrierHeight,
                SurfaceTag.Barrier) { Foot = FootMat.WoodDoor });
            sink.Wall(P(run, t), P(x0, t), barrier, y0, landing + WalkRules.BarrierHeight, SurfaceTag.Barrier, FootMat.WoodDoor);
            sink.Colliders.Add(new SlopedWallCollider(P(x0, t), P(total, t), barrier, landing - S.Waist, landing + riser + WalkRules.BarrierHeight,
                y2 - riser - S.Waist, y2 + WalkRules.BarrierHeight, SurfaceTag.Barrier) { Foot = FootMat.WoodDoor });
        }
        // The back of the solid base, under the start of the upper flight.
        sink.Wall(P(x0, -half), P(x0, half), 0.05f, y0, landing - 0.05f, SurfaceTag.Wall, FootMat.Concrete);
    }

    /// <summary>
    /// One side of one stretch from x a to x b: the white stringer from its bottom line to 0.1 m over the nosing line, the
    /// wood panel above it and the white cap at <see cref="LibraryInteriorStyle.StairRail"/> over the nosings.
    /// </summary>
    private static void Side(InteriorGeometry sink, Func<float, float, float, Vector3> q, float t, float a, float b,
        float bottomA, float bottomB, float nosingA, float nosingB)
    {
        float h = S.Stringer * 0.5f, p = S.RailInfill * 0.5f, cap = S.RailCapWidth * 0.5f;
        Vector3[] Band(float half, float lowA, float lowB) =>
            [q(a, t - half, lowA), q(b, t - half, lowB), q(b, t + half, lowB), q(a, t + half, lowA)];
        sink.Hexa(S.Wall(S.WallWhite), Band(h, bottomA, bottomB), Band(h, nosingA + 0.1f, nosingB + 0.1f));
        sink.Hexa(S.Trim(S.Wood), Band(p, nosingA + 0.1f, nosingB + 0.1f), Band(p, nosingA + S.StairRail - S.RailCap, nosingB + S.StairRail - S.RailCap));
        sink.Hexa(S.Trim(S.WallWhite), Band(cap, nosingA + S.StairRail - S.RailCap, nosingB + S.StairRail - S.RailCap),
            Band(cap, nosingA + S.StairRail, nosingB + S.StairRail));
    }
}
