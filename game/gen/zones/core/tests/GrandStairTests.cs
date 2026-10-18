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

using static GdUnit4.Assertions;

[TestSuite]
public sealed class GrandStairTests
{

    [TestCase]
    public void StairRisesToTheSecondFloorInTwoRunsOfShortFlights()
    {
        var world = RealWorld();
        var (stair, shell) = Pieces(world);
        AssertThat(MathF.Abs(stair.TopY - shell.Plan.SecondFloorY) < 0.01f).OverrideFailureMessage("the top landing is the 2F floor").IsTrue();
        AssertThat(MathF.Abs(stair.Risers * stair.Riser - (stair.TopY - stair.FootY)) < 0.01f).OverrideFailureMessage("risers close the rise").IsTrue();
        AssertThat(stair.Riser is >= 0.12f and <= 0.16f && MathF.Abs(stair.Tread - (0.65f - 2 * stair.Riser)) < 0.01f).OverrideFailureMessage($"R {stair.Riser}, T {stair.Tread}").IsTrue();
        AssertThat(stair.Flights.All(f => f.Risers * stair.Riser <= WalkRules.OutdoorFlightMaxRise + 0.01f)).OverrideFailureMessage("every flight rises at most 1.5 m").IsTrue();
        AssertThat(stair.Landings.Count(l => l.Middle) == 1 && stair.Landings.Count == stair.Flights.Count - 1).OverrideFailureMessage("two runs split by one middle landing").IsTrue();
        AssertThat(stair.Flights[0].Foot == stair.FrontS && MathF.Abs(stair.Flights[^1].Top(stair.Tread) - CoreStyle.TopLandingDepth) < 0.01f).OverrideFailureMessage("flights run from the foot to the top landing").IsTrue();
        // The data footprint (overlay/core.d/ reshape) is the built stair: the same rectangle within 0.15 m.
        var ring = Plan.OuterRing(world.Features[CoreFeatures.LibraryStair]);
        var h = GrandStair.HalfWidth;
        Vector2[] corners = [stair.At(0, -h), stair.At(0, h), stair.At(stair.FrontS, h), stair.At(stair.FrontS, -h)];
        Console.WriteLine("stair corners: " + string.Join(", ", corners.Select(c => $"[{c.X:F2}, {c.Y:F2}]")));
        foreach (var c in corners)
            AssertThat(ring.Min(p => Vector2.Distance(p, c)) < 0.15f).OverrideFailureMessage($"footprint corner {c} is in the overlay outline").IsTrue();
        // The axis is the front door's (interiors.json lib.main, WLD-INTERIORS).
        var entrance = world.Interiors.Get(CoreFeatures.Library)!.Entrances.Single(e => e.Open && e.Id == "lib.main");
        var centre = new Vector2(entrance.Center.X, entrance.Center.Z);
        var offAxis = MathF.Abs(Vector2.Dot(centre - stair.Origin, stair.Across));
        AssertThat(offAxis < 0.6f).OverrideFailureMessage($"the door centre is {offAxis} m off the stair axis").IsTrue();
        AssertThat(MathF.Abs(Shapes.BearingOf(stair.Out) - entrance.YawDeg) < 3f).OverrideFailureMessage($"axis bearing {Shapes.BearingOf(stair.Out)}").IsTrue();
    }

    [TestCase]
    public void FrontDoorOpensTheSlot()
    {
        var world = RealWorld();
        var (stair, shell) = Pieces(world);
        var build = new CoreBuild(Campus.Contracts.Tests.PaletteFixture.Catalog) { PartLods = PartLodFixture.Catalog };
        stair.Build(build, LodLevel.L0);
        var output = build.Finish(LodLevel.L0);
        var door = output.Parts.Single(p => p.Part.Value == PartCatalog.DoorGlass);
        var p = (DoorParams)door.Params;
        AssertThat(p.OpenAngleDeg >= 89f && p.Leaves == 4 && p.Width / p.Leaves * 2 > 1.1f).OverrideFailureMessage("two inner leaves fixed open, clear width over 1.1 m").IsTrue();
        foreach (var part in new[] { PartCatalog.EntranceMat, PartCatalog.OpenSign, PartCatalog.EntranceLamp })
            AssertThat(output.Parts.All(x => x.Part.Value != part)).OverrideFailureMessage($"no {part} of the zone's own (WP-13 places the entrance kit)").IsTrue();
        AssertThat(output.Parts.Count(x => x.Part.Value == PartCatalog.DoorFire) == 2).OverrideFailureMessage("two closed service doors").IsTrue();
        AssertThat(output.Emitters is null or { Count: 0 }).OverrideFailureMessage("no lamp marker of the zone's own").IsTrue();
        // No glass in the door bay below the door transom: the doorway is open.
        var bay = shell.EntranceDoor!.Value;
        var lib = new CoreBuild(Campus.Contracts.Tests.PaletteFixture.Catalog) { PartLods = PartLodFixture.Catalog };
        shell.Build(lib, LodLevel.L0);
        var across = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, bay.Outward));
        foreach (var g in lib.Finish(LodLevel.L0).Meshes.Where(m => m.Material.Spec.Kind is ToonKind.Glass or ToonKind.GlassNear))
            for (var v = 0; v < g.Position.Length / 3; v++)
            {
                var q = new Vector3(g.Position[3 * v], g.Position[3 * v + 1], g.Position[3 * v + 2]) - bay.Origin;
                var inside = MathF.Abs(Vector3.Dot(q, across)) < bay.Width * 0.5f - 0.05f && MathF.Abs(Vector3.Dot(q, bay.Outward)) < 0.05f;
                AssertThat(!inside || q.Y > bay.Height - 0.01f).OverrideFailureMessage("slot glass stays above the door transom in the door bay").IsTrue();
            }
    }

    [TestCase]
    public void CanopySpansTheTowerFaceOverTheDoor()
    {
        var (stair, shell) = Pieces(RealWorld());
        var bay = shell.EntranceDoor!.Value;
        var build = new CoreBuild(Campus.Contracts.Tests.PaletteFixture.Catalog) { PartLods = PartLodFixture.Catalog };
        stair.Build(build, LodLevel.L0);
        var canopy = build.Finish(LodLevel.L0).Meshes.Where(IsCanopy).ToArray();
        AssertThat(canopy.Length > 0).OverrideFailureMessage("a canopy over the front door").IsTrue();
        var points = canopy.SelectMany(m => Enumerable.Range(0, m.Position.Length / 3).Where(i => m.ColorIndices![i] == Campus.Contracts.Tests.PaletteFixture.Catalog.Resolve(CanopySpec.Lit).Value).Select(i => new Vector3(m.Position[3 * i], m.Position[3 * i + 1], m.Position[3 * i + 2]))).ToArray();
        var f = bay.Face;
        var along = points.Select(p => Vector2.Dot(new Vector2(p.X, p.Z) - f.A, f.Along)).ToArray();
        var outward = points.Select(p => Vector2.Dot(new Vector2(p.X, p.Z) - f.A, f.Out)).ToArray();
        AssertThat(along.Min() < 0.05f && along.Max() > f.Length - 0.05f).OverrideFailureMessage($"the canopy spans the tower face ({along.Min()}…{along.Max()} of {f.Length})").IsTrue();
        AssertThat(MathF.Abs(outward.Max() - CoreStyle.CanopyDepth) < 0.02f && outward.Min() < -bay.Depth + 0.02f).OverrideFailureMessage("it projects in front of the face and runs back to the slot glass").IsTrue();
        // Over the door bay the frame is only the canopy, its soffit clear of the door transom; the piers stand on the terrace.
        var overDoor = points.Where(p => MathF.Abs(Vector2.Dot(new Vector2(p.X - bay.Origin.X, p.Z - bay.Origin.Z), f.Along)) < bay.Width).ToArray();
        AssertThat(overDoor.Min(p => p.Y) > bay.Origin.Y + bay.Height + 0.5f).OverrideFailureMessage("the canopy's soffit clears the door transom").IsTrue();
        AssertThat(points.Min(p => p.Y) < bay.Origin.Y).OverrideFailureMessage("the piers stand on the terrace").IsTrue();
        AssertThat(stair.Collide().OfType<WallCollider>().Count(w => MathF.Abs(w.HalfT - CoreStyle.CanopyPier * 0.5f) < 0.01f && w.Y0 < bay.Origin.Y) == 2).OverrideFailureMessage("the piers collide").IsTrue();
        foreach (var lod in new[] { LodLevel.L1, LodLevel.L2 })
        {
            var coarse = new CoreBuild(Campus.Contracts.Tests.PaletteFixture.Catalog) { PartLods = PartLodFixture.Catalog };
            stair.Build(coarse, lod);
            AssertThat(coarse.Finish(lod).Meshes.Any(IsCanopy)).OverrideFailureMessage($"the canopy reads at {lod}").IsTrue();
        }
    }

    private static bool IsCanopy(GeometryBundle mesh) =>
        (mesh.Material.Spec with { Lit = CanopySpec.Lit, Shade = CanopySpec.Shade }) == CanopySpec &&
        mesh.ColorIndices?.Contains(Campus.Contracts.Tests.PaletteFixture.Catalog.Resolve(CanopySpec.Lit).Value) == true;

    private static ToonSpec CanopySpec => CoreStyle.Spec(CoreMaterial.Sheet, CoreStyle.CanopyFamily);

    [TestCase]
    public void WpThirteenDressesTheLibraryFrontDoor()
    {
        var world = RealWorld();
        var zone = new WpId("WP-16");
        WpId? ClaimedBy(FeatureId feature, Aspects aspects) =>
            CoreSite.Claims.OfType<FeatureClaim>().Any(c => c.Feature == feature && (c.Aspects & aspects) == aspects) ? zone : null;
        AssertThat(ClaimedBy(CoreFeatures.Library, Aspects.Props) is null).OverrideFailureMessage("the zone leaves the library's props unclaimed").IsTrue();
        var entrance = world.Interiors.Get(CoreFeatures.Library)!.Entrances.Single(e => e.Open && e.Id == "lib.main");
        var layout = Campus.Gen.Furniture.FurniturePlanner.Build(world, ClaimedBy);
        AssertThat(layout.Items.Count(i => i.Key.StartsWith("door:" + entrance.Id + ":", StringComparison.Ordinal)) == 3).OverrideFailureMessage("WP-13 places mat, sign and lamp at the front door").IsTrue();
    }

    [TestCase]
    public void CollidersGuardEveryDropAndKeepTheFlightsWalkable()
    {
        var (stair, _) = Pieces(RealWorld());
        var colliders = stair.Collide();
        AssertThat(colliders.All(c => c.Foot != FootMat.Auto)).OverrideFailureMessage("every collider has a foot material").IsTrue();
        var ramps = colliders.OfType<RampCollider>().ToArray();
        AssertThat(ramps.Length == 3 * stair.Flights.Count && ramps.All(r => r.Stair is { Count: >= 3 })).OverrideFailureMessage($"one stair ramp per flight and lane ({ramps.Length})").IsTrue();
        var hedges = colliders.OfType<WallCollider>().Where(w => w.Tag == SurfaceTag.Barrier).ToArray();
        AssertThat(hedges.Length == 2 * 2 * 3 && hedges.All(w => w.Foot == FootMat.Hedge)).OverrideFailureMessage($"three hedges on each tier and side ({hedges.Length})").IsTrue();
        var guards = colliders.OfType<WallCollider>().Where(w => w.Tag == SurfaceTag.Wall).ToArray();
        AssertThat(guards.Max(w => w.Y1) >= stair.TopY + WalkRules.BarrierHeight - 0.01f).OverrideFailureMessage("the guard walls stand 1.5 m over the top landing").IsTrue();
        AssertThat(colliders.OfType<BoxCollider>().Any(b => b.Tag == SurfaceTag.DoorClosed)).OverrideFailureMessage("the service doors are closed doors").IsTrue();
    }

    // ---------------------------------------------------------------- fixtures

    private static (GrandStair Stair, LibraryShell Shell) Pieces(IWorldModel world)
    {
        var pieces = CoreSite.Pieces(world);
        return (pieces.OfType<GrandStair>().Single(), pieces.OfType<LibraryShell>().Single());
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
