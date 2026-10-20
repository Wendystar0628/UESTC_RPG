using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.World;
using S = Campus.Gen.Furniture.FurnitureStyle;
using static Campus.Gen.Furniture.FurniturePlacement;
using static Campus.Gen.Furniture.FurniturePlanner;
using static Campus.Gen.Furniture.FurnitureParts;

namespace Campus.Gen.Furniture;

/// <summary>Places shuttle-stop boards beside the nearest carriageway.</summary>
internal static class FurnitureStops
{
    internal static void Plan(StreetSite site, List<FurnitureItem> items, SignParams p)
    {
        var terrain = site.World.Terrain;
        foreach (var feature in site.World.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (WorldProps.TryText(feature.Props, "subcategory") != "bus") continue;
            var anchor = Anchor(feature);
            if (StreetNetwork.NearestCarriageway(site.World, anchor, 45f) is not { } near) continue;
            var normal = new Vector2(-near.Along.Y, near.Along.X);
            var side = Vector2.Dot(anchor - near.Point, normal) >= 0f ? 1f : -1f;
            var offset = StreetNetwork.HalfWidth(near.Line) + S.StopKerbGap;
            foreach (var shift in Shifts(S.LampMaxShift))
            {
                var pos = near.Point + near.Along * shift + normal * (side * offset);
                // The board runs along the kerb: test it as a thin capsule, not a disc of its width.
                var reach = near.Along * (p.Width * 0.5f - StopHalfDepth);
                if (!site.Free(pos - reach, pos + reach, StopHalfDepth) || Land.Point(terrain, pos, PoleRadius) is not { } y) continue;
                var at = Frame(pos, -normal * side, y);
                items.Add(Item("stop:" + feature.Id.Value, FurnitureKind.BusStop, PartCatalog.BusStop, p, at, StopDef.Collision(p), []));
                site.Occupy(pos - reach, pos + reach, StopHalfDepth);
                break;
            }
        }
    }
}
