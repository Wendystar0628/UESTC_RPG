using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.Common;
using L = Campus.Gen.Zones.West.WestTune.Look;

namespace Campus.Gen.Zones.West.Kindergarten;

/// <summary>A short curved seat along the authored pocket garden's northern edge.</summary>
public sealed class PocketGardenSite : ZoneSite
{
    public override string Id => "19c.pocket-garden";

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        [SiteRanges.Bounds(world.Feature(WestIds.PocketSeat), 0.3f)];
    private static Vector2[] Seat(IWorldModel world) => world.Feature(WestIds.PocketSeat).Line().ToArray();

    public override void Build(SiteBuilder b)
    {
        var line = Seat(b.Context.World);
        for (var i = 1; i < line.Length; i++)
        {
            var a = line[i - 1]; var z = line[i];
            if (!b.Includes(SiteRanges.Bounds([a, z], 0.3f))) continue;
            var f = PlanFrame.Toward((a + z) * .5f, z);
            var length = Vector2.Distance(a, z);
            var y = b.Ground(f.Origin);
            GridFacade.Box(b, f, -length * .5f, length * .5f, y + .37f, y + .45f, -.28f, .28f, L.Deck, .025f);
            GridFacade.Box(b, f, -.08f, .08f, y, y + .37f, -.23f, .23f, L.Metal, .025f);
        }
    }

    public override void Collide(SiteColliders c)
    {
        var line = Seat(c.Context.World);
        for (var i = 1; i < line.Length; i++)
        {
            var a = line[i - 1]; var z = line[i];
            if (!c.Includes(SiteRanges.Bounds([a, z], 0.3f))) continue;
            var f = PlanFrame.Toward((a + z) * .5f, z);
            c.Box(f.At(0, c.Ground(f.Origin) + .225f, 0), new(Vector2.Distance(a, z) * .5f, .225f, .28f), SurfaceTag.Prop, FootMat.WoodDeck);
        }
    }
}
