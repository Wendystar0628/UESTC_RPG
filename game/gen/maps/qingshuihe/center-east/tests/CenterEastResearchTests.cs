using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Sites;
using Campus.Gen.Sites.Tests;
using Campus.Tests;
using Campus.Gen.World;
using Campus.Gen.Maps.Qingshuihe.CenterEast.Framework;
using Campus.Gen.Maps.Qingshuihe.CenterEast.Pinxue;
using Campus.Gen.Maps.Qingshuihe.CenterEast.Research;
using Campus.Gen.Maps.Qingshuihe.CenterEast.Wenke;
using Campus.Gen.Tests.Support;
using GdUnit4;
using M = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Monuments;
using P = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Pinxue;
using R = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Research;
using F = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Friendship;
using K = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Wenke;

namespace Campus.Gen.Maps.Qingshuihe.CenterEast.Tests;
using static GdUnit4.Assertions;
using static Campus.Gen.Maps.Qingshuihe.CenterEast.Tests.CenterEastTestFixture;

[TestSuite]
public sealed class CenterEastResearchTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void ResearchRegistrationsMatchWhatIsBuilt()
    {
        var world = World();
        IReadOnlyList<Vector2> Outline(string id) => ((PolygonGeometry)world.Features[new(id)].Geometry).Rings.Outer;
        static float Area(IReadOnlyList<Vector2> ring) => MathF.Abs((float)PlanRings.SignedArea(ring));
        foreach (var site in CenterEastSites.All.OfType<ResearchSite>())
        {
            var built = Area(site.Footprints(world).Hall);
            AssertThat(MathF.Abs(Area(Outline(site.HallId)) - built) < TestTolerance.Proportion * built).OverrideFailureMessage($"{site.HallId} outline matches the built hall").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void ResearchBlocksStandFiveStoreysWithOneShutEntranceEach()
    {
        var ctx = Context();
        var module = new CenterEastModule();
        var colliders = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).ToArray();
        var top = ResearchSite.Style.Top;
        AssertThat(MathF.Abs(top - (R.GroundStorey + (R.Storeys - 1) * R.Storey + 1.3f)) < TestTolerance.Position).OverrideFailureMessage($"five storeys to the coping, got {top}").IsTrue();
        foreach (var site in CenterEastSites.All.OfType<ResearchSite>())
        {
            var plan = site.Footprints(ctx.World);
            var walls = colliders.OfType<WallCollider>().Where(w => plan.Blocks.Any(r => NearRing(r, (w.A + w.B) * 0.5f))).ToArray();
            AssertThat(walls.Any(w => w.Tag == SurfaceTag.Wall && MathF.Abs(w.Y1 - site.BaseY(ctx) - top) < TestTolerance.Position)).OverrideFailureMessage($"{site.Id} walls reach the coping above their terrain pad").IsTrue();
            var door = plan.Door ?? site.HallApex(plan).Apex;
            AssertThat(colliders.OfType<WallCollider>().Count(w => w.Tag == SurfaceTag.DoorClosed && Vector2.Distance((w.A + w.B) * 0.5f, door) < 3f) is >= 1 and <= 2).OverrideFailureMessage($"{site.Id} has its shut entrance at the door").IsTrue();
        }

        static bool NearRing(IReadOnlyList<Vector2> ring, Vector2 p) => PlanRings.DistanceToRing(p, ring) < 1f;
    }
}
