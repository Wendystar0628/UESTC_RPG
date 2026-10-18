using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Circulation;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.Common;
using Campus.Gen.World;
using H = Campus.Gen.Zones.North.NorthTune.Hall;
using L = Campus.Gen.Zones.North.NorthTune.Look;

namespace Campus.Gen.Zones.North.Xuezhi;

/// <summary>
/// 学知苑 1 栋's open front door toward the 学知一 courtyard (MAP-QSH-NORTH; WLD-INTERIORS). The
/// building's massing, storeys and windows stay with WP-14; the interior is WP-31c's. This site adds the entrance the
/// interior is entered through: a granite landing at the raised ground floor reaching past the teleport landing, 6–8 steps
/// down to the courtyard with cheek walls and handrails, and a vestibule block (门斗) whose through doorway carries the
/// double glass leaves fixed open against its reveals, and a canopy. The open-entrance set (mat, green open sign, door
/// lamp) is placed by WP-13 for every open entrance, not here (main session 12:15). The door centre, facing and floor level come from the interior index (the `xz1.hall` EntranceDecl), so the porch and
/// WP-31c's hall meet at the same doorway; WP-43 opens the facade wall there by EntranceDecl.Open.
/// </summary>
public sealed class HallEntranceSite : ZoneSite
{
    private static readonly StepsPart Steps = new();

    public override string Id => "20a.xz1-hall";

    public override IReadOnlyList<InspectionProbe> Probes =>
        [Probe("Q4", NorthFeatureIds.Hall.Entity, NorthTune.Probes.WallEye, NorthTune.Probes.WallLook, -20f, 0.5f)];

    /// <summary>Porch frame and levels: origin on the footprint edge under the door, +Z out to the courtyard.</summary>
    internal sealed record Porch(PlanFrame Frame, float Ground, float Floor, int Risers, EntranceDecl Entrance)
    {
        public float Rise => Floor - Ground;
        public float FrontZ => H.WallFace + H.LandingDepth;
        public StairParams Flight => new(H.StepsWidth, Rise, Risers * H.Tread, Risers) { Material = L.Granite, Foot = FootMat.StonePaving };
    }

    /// <summary>The porch, or null when the world has no such building or no open entrance for it.</summary>
    internal static Porch? Locate(IWorldModel world)
    {
        var id = new FeatureId(NorthFeatureIds.Hall.Entity);
        if (!world.Features.TryGetValue(id, out var feature)) return null;
        if (world.Interiors.Get(id)?.Entrances.FirstOrDefault(e => e.Open) is not { } entrance) return null;
        var edge = world.ContourSegment(NorthFeatureIds.HallFacade);
        var door = new Vector2(entrance.Center.X, entrance.Center.Z);
        var foot = PlanLines.ClosestPoint(door, edge[0], edge[^1], 1e-12f);
        var f = PlanFrame.Along(foot, edge[0] - edge[^1]);
        var ground = world.Terrain.HeightAt(f.P(0, H.WallFace + H.LandingDepth + 2f).X, f.P(0, H.WallFace + H.LandingDepth + 2f).Y);
        // The floor follows the terrace (+3.0 → 4.0, MAP-QSH-TERRAIN) even while the shared entrance still reads 1.0.
        var floor = MathF.Max(entrance.Center.Y, ground + H.FloorRaise);
        var risers = Math.Clamp((int)MathF.Round((floor - ground) / 0.15f), H.MinRisers, H.MaxRisers);
        return new(f, ground, floor, risers, entrance);
    }

    public override void Build(SiteBuilder b)
    {
        if (Locate(b.Context.World) is not { } porch) return;
        var f = porch.Frame;
        float g = porch.Ground, floor = porch.Floor;
        var mass = b.Detail == SiteDetail.Mass;

        // Landing: granite body from the wall face to the stair head, a coping band and a plinth line.
        float z0 = H.WallFace - 0.1f, z1 = porch.FrontZ, d = z1 - z0, zc = (z0 + z1) * 0.5f;
        b.Add(PartGeometryKernel.BeveledBox(H.LandingWidth, floor - g + 0.1f - 0.12f, d, 0.02f, L.Granite), f.At(0, g - 0.1f, zc));
        b.Add(PartGeometryKernel.BeveledBox(H.LandingWidth + 0.06f, 0.12f, d + 0.03f, 0.015f, L.Trim), f.At(0, floor - 0.12f, zc + 0.015f));
        if (!mass) b.Add(PartGeometryKernel.BeveledBox(H.LandingWidth + 0.08f, 0.35f, d + 0.04f, 0.02f, L.Plinth), f.At(0, g - 0.1f, zc + 0.02f));

        // Steps down to the courtyard; the part climbs toward its +Z, so it is reversed to climb toward the door.
        var flight = porch.Flight;
        b.Add(Steps.Lods[b.Lod](flight), f.AtReversed(0, g, z1 + flight.Depth * 0.5f));

        // Vestibule block with the doorway notched through it, a cap band, and a canopy over the door.
        float vw = H.VestibuleWidth * 0.5f, vh = H.VestibuleHeight, vd = H.VestibuleDepth + H.WallFace + 0.1f;
        var front = H.WallFace + H.VestibuleDepth;
        Vector2[] outer =
        [
            new(-vw, 0), new(-H.DoorWidth * 0.5f, 0), new(-H.DoorWidth * 0.5f, H.DoorHeight), new(H.DoorWidth * 0.5f, H.DoorHeight),
            new(H.DoorWidth * 0.5f, 0), new(vw, 0), new(vw, vh), new(-vw, vh),
        ];
        b.Add(PartGeometryKernel.WallPanel(new PolygonRings(outer), vd, L.HouseWall), f.At(0, floor, front - vd * 0.5f));
        b.Add(PartGeometryKernel.BeveledBox(2 * vw + 0.2f, 0.25f, vd + 0.1f, 0.02f, L.Trim), f.At(0, floor + vh, front - vd * 0.5f + 0.05f));
        b.Add(PartGeometryKernel.BeveledBox(2 * vw + 0.6f, H.CanopyThickness, H.CanopyDepth, 0.03f, L.Trim),
            f.At(0, floor + H.DoorHeight + 0.45f, front + H.CanopyDepth * 0.5f - 0.05f));
        if (mass) return;

        // Door surround, the glass screen at the back of the doorway, and the two leaves held open against the reveals.
        b.Add(PartGeometryKernel.BeveledBox(H.DoorWidth + 0.24f, 0.12f, 0.08f, 0.01f, L.Stainless), f.At(0, floor + H.DoorHeight, front + 0.02f));
        foreach (var side in new[] { -1f, 1f })
            b.Add(PartGeometryKernel.BeveledBox(0.12f, H.DoorHeight, 0.08f, 0.01f, L.Stainless), f.At(side * (H.DoorWidth * 0.5f + 0.06f), floor, front + 0.02f));
        b.Add(PartGeometryKernel.BeveledBox(H.DoorWidth, H.DoorHeight, 0.03f, 0, L.Glass), f.At(0, floor, H.WallFace + 0.02f));
        var leaf = H.DoorWidth * 0.5f - 0.05f;
        foreach (var side in new[] { -1f, 1f })
        {
            var x = side * (H.DoorWidth * 0.5f - 0.04f);
            var z = front - leaf * 0.5f - 0.05f;
            b.Add(PartGeometryKernel.BeveledBox(0.05f, H.DoorHeight - 0.1f, leaf, 0.01f, L.Stainless), f.At(x, floor + 0.02f, z));
            b.Add(PartGeometryKernel.BeveledBox(0.02f, H.DoorHeight - 0.4f, leaf - 0.16f, 0, L.Glass), f.At(x - side * 0.02f, floor + 0.2f, z));
        }
    }

    public override void Collide(SiteColliders c)
    {
        if (Locate(c.Context.World) is not { } porch) return;
        var f = porch.Frame;
        float floor = porch.Floor, z0 = H.WallFace - 0.1f, z1 = porch.FrontZ;
        // Landing top, the flight as one ramp, and the vestibule piers either side of the doorway.
        c.Box(f.At(0, floor - 0.15f, (z0 + z1) * 0.5f), new(H.LandingWidth * 0.5f, 0.15f, (z1 - z0) * 0.5f), SurfaceTag.Ground, FootMat.StonePaving);
        c.Add(Steps.Collision(porch.Flight), f.AtReversed(0, porch.Ground, z1 + porch.Flight.Depth * 0.5f));
        var front = H.WallFace + H.VestibuleDepth;
        var pier = H.VestibuleWidth * 0.5f - H.DoorWidth * 0.5f;
        foreach (var side in new[] { -1f, 1f })
            c.Box(f.At(side * (H.DoorWidth * 0.5f + pier * 0.5f), floor + H.VestibuleHeight * 0.5f, (H.WallFace + front) * 0.5f),
                new(pier * 0.5f, H.VestibuleHeight * 0.5f, H.VestibuleDepth * 0.5f), SurfaceTag.Wall, FootMat.Concrete);
    }
}
