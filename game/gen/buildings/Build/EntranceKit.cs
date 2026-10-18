using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Buildings.Mesh;
using Campus.Gen.Buildings.Model;
using Campus.Gen.Buildings.Rules;
using Campus.Gen.Parts.Circulation;
using C = Campus.Gen.Parts.Circulation.CirculationStyle;

namespace Campus.Gen.Buildings.Build;

/// <summary>
/// The WP-11 parts in front of one entrance door, placed by PartCatalog ID (WLD-TERRAIN): a stone landing
/// (platform) at the door floor, and from its front edge the connection down to the outside landing, as TerrainDrop plans
/// it: nothing up to 0.04 m, a kerbed 1:12 slope up to 0.15 m, one flight of steps above that (cheek walls; handrails from
/// 0.6 m by the WP-11 rule) up to WalkRules.OutdoorFlightMaxRise; a landing more than 1.0 m up gets guard railings on its open sides
/// (§5.7). Doors in K1 and K2 take the deeper, wider landing and cheek walls on every flight; the rest a smaller landing
/// and bare-block steps below handrail height. The stone takes WP-14's trim
/// class, so the parts share the facade's material. Parts stand on the outside ground with +X along the facade and +Z back
/// toward the door; their colliders come along (SYS-WALK).
/// </summary>
public static class EntranceKit
{
    private static readonly PlatformPart Platform = new();
    private static readonly RampPart Ramp = new();
    private static readonly StepsPart Steps = new();
    private static readonly RailingPart Railing = new();

    /// <summary>One placed part: catalogue ID, parameters and row-vector placement.</summary>
    public sealed record Piece(string Part, BoxPartParams Params, Matrix4x4 At);

    /// <summary>The landing and connection of an entrance (none where the door stands alone or sits at the ground).</summary>
    public static IReadOnlyList<Piece> Pieces(BuildingPlan plan, PlanEntrance entrance)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(entrance);
        var door = plan.DoorOf(entrance);
        var rise = door.Y0 - entrance.GroundY;
        if (!entrance.Landing || rise < EntranceRules.MinLanding) return [];
        var layout = plan.Facades[entrance.Facade];
        var skin = plan.SkinAt(layout.Ring);
        var f = EdgeFrame.Of(skin, layout.Edge);
        var full = plan.ZoneOf(entrance.Facade, plan.DoorIndex(entrance)) <= EntranceRules.FullUpTo;
        var depth = full ? EntranceRules.LandingDepth : EntranceRules.SimpleLandingDepth;
        var side = full ? EntranceRules.LandingSide : EntranceRules.SimpleLandingSide;
        var u0 = MathF.Max(door.U0 - side, EntranceRules.LandingEnd);
        var u1 = MathF.Min(door.U1 + side, skin.Length[layout.Edge] - EntranceRules.LandingEnd);
        var (width, uc) = (u1 - u0, (u0 + u1) * 0.5f);
        var stone = Materials.Spec(MaterialKind.Trim, Families.StoneBeige);
        var ground = entrance.GroundY;
        var pieces = new List<Piece>
        {
            new(PartCatalog.Platform, new PanelParams(width, rise, depth) { Material = stone, Foot = FootMat.StonePaving }, Toward(f, uc, depth * 0.5f, ground)),
        };
        // Guard railings along the landing's open sides once it stands high (the front is the flight's).
        if (rise > EntranceRules.GuardFrom)
        {
            var guard = new RailingParams(depth - 2f * EntranceRules.GuardInset, EntranceRules.GuardHeight, 0.08f);
            foreach (var u in new[] { u0 + EntranceRules.GuardInset, u1 - EntranceRules.GuardInset })
                pieces.Add(new(PartCatalog.Railing, guard, Outward(f, u, depth * 0.5f, door.Y0)));
        }
        if (rise > WalkRules.OutdoorFlightMaxRise) return pieces;
        var drop = TerrainDrop.Plan(rise, 0f, mainNetwork: false, clearWidth: width - 2f * C.CheekWidth);
        switch (drop.Method)
        {
            case DropMethod.Absorb:
                break;
            case DropMethod.SlopedCurb:
                {
                    // The 1:12 slope as a kerbed ramp deck; very low ones keep the part's shortest run.
                    var run = MathF.Max(drop.Run, 0.6f);
                    pieces.Add(new(PartCatalog.Ramp, new RampParams(width, rise, run) { Variation = 1, Material = stone, Foot = FootMat.StonePaving },
                        Toward(f, uc, depth + run * 0.5f, ground)));
                    break;
                }
            case DropMethod.Steps:
                {
                    // The plan's frame: origin at the foot of the flight, +Z climbing back to the landing.
                    var foot = Toward(f, uc, depth + drop.Run, ground);
                    foreach (var part in drop.Parts)
                    {
                        var p = (StairParams)part.Params;
                        var variation = full || p.Height >= C.HandrailMinRise ? 0 : 2;
                        pieces.Add(new(PartCatalog.Steps, p with { Variation = variation, Material = stone, Foot = FootMat.StonePaving },
                            Shift(foot, part.At.M41, part.At.M42, part.At.M43)));
                    }
                    break;
                }
            case DropMethod.Ramp:
            case DropMethod.Terraces:
                throw new InvalidOperationException($"No ramp or terrace in front of a door: a {rise} m rise planned as {drop.Method}.");
            default:
                throw new ArgumentOutOfRangeException(nameof(entrance), drop.Method, "Unknown drop method.");
        }
        return pieces;
    }

    /// <summary>The pieces' surfaces at a part level, appended to the building's streams.</summary>
    public static void Build(MeshSink sink, IReadOnlyList<Piece> pieces, LodLevel lod)
    {
        foreach (var piece in pieces)
            foreach (var mesh in Geometry(piece, lod).Meshes) sink.Append(mesh, piece.At);
    }

    /// <summary>The pieces' colliders in world space.</summary>
    public static IEnumerable<Collider> Colliders(IReadOnlyList<Piece> pieces) =>
        pieces.SelectMany(piece => Collision(piece).Select(c => GeometryPlacement.Transform(c, piece.At)));

    private static PartGeometry Geometry(Piece piece, LodLevel lod) => piece.Params switch
    {
        PanelParams p => Platform.Lods[lod](p),
        RampParams p => Ramp.Lods[lod](p),
        StairParams p => Steps.Lods[lod](p),
        RailingParams p => Railing.Lods[lod](p),
        _ => throw new ArgumentException($"No entrance part for {piece.Part}.", nameof(piece)),
    };

    private static IReadOnlyList<Collider> Collision(Piece piece) => piece.Params switch
    {
        PanelParams p => Platform.Collision(p),
        RampParams p => Ramp.Collision(p),
        StairParams p => Steps.Collision(p),
        RailingParams p => Railing.Collision(p),
        _ => throw new ArgumentException($"No entrance part for {piece.Part}.", nameof(piece)),
    };

    /// <summary>Row-vector placement at (u, s, y) of the facade frame: +X along the facade, +Y up, +Z back toward the wall.</summary>
    private static Matrix4x4 Toward(EdgeFrame f, float u, float s, float y)
    {
        var o = f.Out;
        var at = f.At(u, s, y);
        return new(-o.Z, 0f, o.X, 0f, 0f, 1f, 0f, 0f, -o.X, 0f, -o.Z, 0f, at.X, at.Y, at.Z, 1f);
    }

    /// <summary>Row-vector placement at (u, s, y) of the facade frame with +X pointing out of the building, +Y up.</summary>
    private static Matrix4x4 Outward(EdgeFrame f, float u, float s, float y)
    {
        var o = f.Out;
        var at = f.At(u, s, y);
        return new(o.X, 0f, o.Z, 0f, 0f, 1f, 0f, 0f, -o.Z, 0f, o.X, 0f, at.X, at.Y, at.Z, 1f);
    }

    /// <summary>The frame moved by (x, y, z) along its own axes.</summary>
    private static Matrix4x4 Shift(Matrix4x4 m, float x, float y, float z) => m with
    {
        M41 = m.M41 + x * m.M11 + y * m.M21 + z * m.M31,
        M42 = m.M42 + x * m.M12 + y * m.M22 + z * m.M32,
        M43 = m.M43 + x * m.M13 + y * m.M23 + z * m.M33,
    };

}
