using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.World;
using Campus.Gen.Zones.Core.Entrance;
using Campus.Gen.Zones.Core.Framework;
using Campus.Gen.Zones.Core.Library;
using GdUnit4;

namespace Campus.Gen.Zones.Core.Tests;

[TestSuite]
public sealed class GrandStairTests
{
    [TestCase]
    public void StairRisesToTheSecondFloorInTwoRunsOfShortFlights()
    {
        var world = RealWorld();
        var (stair, shell) = Pieces(world);
        Require(MathF.Abs(stair.TopY - shell.Plan.SecondFloorY) < 1e-4f, "the top landing is the 2F floor");
        Require(MathF.Abs(stair.Risers * stair.Riser - (stair.TopY - stair.FootY)) < 1e-3f, "risers close the rise");
        Require(stair.Riser is >= 0.12f and <= 0.16f && MathF.Abs(stair.Tread - (0.65f - 2 * stair.Riser)) < 1e-3f, $"R {stair.Riser}, T {stair.Tread}");
        Require(stair.Flights.All(f => f.Risers * stair.Riser <= CoreStyle.FlightMaxRise + 1e-3f), "every flight rises at most 1.5 m");
        Require(stair.Landings.Count(l => l.Middle) == 1 && stair.Landings.Count == stair.Flights.Count - 1, "two runs split by one middle landing");
        Require(stair.Flights[0].Foot == stair.FrontS && MathF.Abs(stair.Flights[^1].Top(stair.Tread) - CoreStyle.TopLandingDepth) < 1e-3f,
            "flights run from the foot to the top landing");
        // The data footprint (core.overlay.json reshape) is the built stair: the same rectangle within 0.15 m.
        var ring = Plan.OuterRing(world.Features[CoreFeatures.LibraryStair]);
        var h = GrandStair.HalfWidth;
        Vector2[] corners = [stair.At(0, -h), stair.At(0, h), stair.At(stair.FrontS, h), stair.At(stair.FrontS, -h)];
        Console.WriteLine("stair corners: " + string.Join(", ", corners.Select(c => $"[{c.X:F2}, {c.Y:F2}]")));
        foreach (var c in corners)
            Require(ring.Min(p => Vector2.Distance(p, c)) < 0.15f, $"footprint corner {c} is in the overlay outline");
        // The axis is the front door's (interiors.json lib.main, WLD-INTERIORS §2.17).
        var entrance = world.Interiors.Get(CoreFeatures.Library)!.Entrances.Single(e => e.Open);
        var centre = new Vector2(entrance.Center.X, entrance.Center.Z);
        var offAxis = MathF.Abs(Vector2.Dot(centre - stair.Origin, stair.Across));
        Require(offAxis < 0.6f, $"the door centre is {offAxis} m off the stair axis");
        Require(MathF.Abs(Shapes.BearingOf(stair.Out) - entrance.YawDeg) < 3f, $"axis bearing {Shapes.BearingOf(stair.Out)}");
    }

    [TestCase]
    public void FrontDoorOpensTheSlot()
    {
        var world = RealWorld();
        var (stair, shell) = Pieces(world);
        var build = new CoreBuild(null);
        stair.Build(build, LodLevel.L0);
        var output = build.Finish(LodLevel.L0);
        var door = output.Parts.Single(p => p.Part.Value == PartCatalog.DoorGlass);
        var p = (DoorParams)door.Params;
        Require(p.OpenAngleDeg >= 89f && p.Leaves == 4 && p.Width / p.Leaves * 2 > 1.1f, "two inner leaves fixed open, clear width over 1.1 m");
        foreach (var part in new[] { PartCatalog.EntranceMat, PartCatalog.OpenSign, PartCatalog.EntranceLamp })
            Require(output.Parts.All(x => x.Part.Value != part), $"no {part} of the zone's own (WP-13 places the entrance kit)");
        Require(output.Parts.Count(x => x.Part.Value == PartCatalog.DoorFire) == 2, "two closed service doors");
        Require(output.Emitters is null or { Count: 0 }, "no lamp marker of the zone's own");
        // No glass in the door bay below the door transom: the doorway is open.
        var bay = shell.EntranceDoor!.Value;
        var lib = new CoreBuild(null);
        shell.Build(lib, LodLevel.L0);
        var across = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, bay.Outward));
        foreach (var g in lib.Finish(LodLevel.L0).Meshes.Where(m => m.Material.Spec.Kind is ToonKind.Glass or ToonKind.GlassNear))
            for (var v = 0; v < g.Position.Length / 3; v++)
            {
                var q = new Vector3(g.Position[3 * v], g.Position[3 * v + 1], g.Position[3 * v + 2]) - bay.Origin;
                var inside = MathF.Abs(Vector3.Dot(q, across)) < bay.Width * 0.5f - 0.05f && MathF.Abs(Vector3.Dot(q, bay.Outward)) < 0.05f;
                Require(!inside || q.Y > bay.Height - 0.01f, "slot glass stays above the door transom in the door bay");
            }
    }

    [TestCase]
    public void WpThirteenDressesTheLibraryFrontDoor()
    {
        var world = RealWorld();
        var zone = new WpId("WP-16");
        WpId? ClaimedBy(FeatureId feature, Aspects aspects) =>
            CoreSite.Claims.OfType<FeatureClaim>().Any(c => c.Feature == feature && (c.Aspects & aspects) == aspects) ? zone : null;
        Require(ClaimedBy(CoreFeatures.Library, Aspects.Props) is null, "the zone leaves the library's props unclaimed");
        var entrance = world.Interiors.Get(CoreFeatures.Library)!.Entrances.Single(e => e.Open);
        var layout = Campus.Gen.Furniture.FurniturePlanner.Build(world, ClaimedBy);
        Require(layout.Items.Count(i => i.Key.StartsWith("door:" + entrance.Id + ":", StringComparison.Ordinal)) == 3, "WP-13 places mat, sign and lamp at the front door");
    }

    [TestCase]
    public void CollidersGuardEveryDropAndKeepTheFlightsWalkable()
    {
        var (stair, _) = Pieces(RealWorld());
        var colliders = stair.Collide();
        Require(colliders.All(c => c.Foot != FootMat.Auto), "every collider has a foot material");
        var ramps = colliders.OfType<RampCollider>().ToArray();
        Require(ramps.Length == 3 * stair.Flights.Count && ramps.All(r => r.Stair is { Count: >= 3 }), $"one stair ramp per flight and lane ({ramps.Length})");
        var hedges = colliders.OfType<WallCollider>().Where(w => w.Tag == SurfaceTag.Barrier).ToArray();
        Require(hedges.Length == 2 * 2 * 3 && hedges.All(w => w.Foot == FootMat.Hedge), $"three hedges on each tier and side ({hedges.Length})");
        var guards = colliders.OfType<WallCollider>().Where(w => w.Tag == SurfaceTag.Wall).ToArray();
        Require(guards.Max(w => w.Y1) >= stair.TopY + CoreStyle.GuardCollision - 1e-3f, "the guard walls stand 1.5 m over the top landing");
        Require(colliders.OfType<BoxCollider>().Any(b => b.Tag == SurfaceTag.DoorClosed), "the service doors are closed doors");
    }

    // ---------------------------------------------------------------- fixtures

    private static (GrandStair Stair, LibraryShell Shell) Pieces(IWorldModel world)
    {
        var pieces = CoreSite.Pieces(world);
        return (pieces.OfType<GrandStair>().Single(), pieces.OfType<LibraryShell>().Single());
    }

    private static void Require(bool condition, string what)
    {
        if (!condition) throw new InvalidOperationException("Expected: " + what);
    }

    private static IWorldModel RealWorld()
    {
        var dir = Path.Combine(FindRepository(), "game", "data", "maps", "qingshuihe");
        var files = new SortedDictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal);
        foreach (var name in new[] { "campus.json", "map.json", "network.json", "interiors.json" })
            files[name] = File.ReadAllBytes(Path.Combine(dir, name));
        foreach (var file in Directory.EnumerateFiles(Path.Combine(dir, "overlay"), "*.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
            files["overlay/" + Path.GetRelativePath(Path.Combine(dir, "overlay"), file).Replace('\\', '/')] = File.ReadAllBytes(file);
        return WorldModelLoader.Load(new MapId("qingshuihe"), files).World;
    }

    private static string FindRepository()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ownership.json"))) return current.FullName;
            current = current.Parent;
        }
        throw new InvalidOperationException("repository root not found");
    }
}
