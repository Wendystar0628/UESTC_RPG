using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Street;
using Campus.Gen.World;
using S = Campus.Gen.Furniture.FurnitureStyle;

namespace Campus.Gen.Furniture;

public static partial class FurniturePlanner
{
    // ---------------------------------------------------------------------------------------------------------
    // Open entrances: mat, standing open sign and door lamp on the entrance anchor.
    // ---------------------------------------------------------------------------------------------------------

    private static void Entrances(StreetSite site, List<FurnitureItem> items, Func<FeatureId, Aspects, string?> claimedBy, SignParams sign)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var feature in site.World.Features.Keys.OrderBy(f => f.Value, StringComparer.Ordinal))
        {
            if (site.World.Interiors.Get(feature) is not { } entry) continue;
            // A zone that builds the doorway and dresses it claims the building's props; its own kit stands there instead.
            if (claimedBy(entry.Feature, Aspects.Props) is { } owner && owner != Self) continue;
            foreach (var door in entry.Entrances.OrderBy(e => e.Id, StringComparer.Ordinal))
            {
                if (!door.Open || !seen.Add(door.Id)) continue;
                var facing = Angles.Heading(door.YawDeg);
                var plan = new Vector2(door.Center.X, door.Center.Z);
                // The entrance floor, never below the paving in front of the door (WLD-TERRAIN: mat and sign on the landing).
                var front = plan + facing * (S.MatDepth * 0.5f + S.MatGap);
                var floor = Frame(plan, facing, MathF.Max(door.Center.Y, site.World.Terrain.HeightAt(front.X, front.Y) + GroundLevels.Paving));
                var key = "door:" + door.Id;
                var matWidth = Math.Clamp(door.ClearWidth + S.MatExtra, S.MatMinWidth, S.MatMaxWidth);
                var mat = new PanelParams(matWidth, S.MatThickness, S.MatDepth);
                items.Add(Item(key + ":mat", FurnitureKind.EntranceMat, PartCatalog.EntranceMat, mat,
                    Xf.Move(0, 0, -(S.MatDepth * 0.5f + S.MatGap)).Then(floor), [], []));
                var signAt = Xf.Move(matWidth * 0.5f + S.SignBeside + sign.Width * 0.5f, 0, -S.SignOut).Then(floor);
                items.Add(Item(key + ":sign", FurnitureKind.OpenSign, PartCatalog.OpenSign, sign, signAt, SignDef.Collision(sign), []));
                // The door lamp hangs above head height on the wall, whose collider stands in for it (visual only, like the part).
                var lamp = S.DoorLamp;
                var lampAt = Xf.Move(0, S.DoorLampY, -lamp.Depth * 0.5f).Then(floor);
                items.Add(Item(key + ":lamp", FurnitureKind.EntranceLamp, PartCatalog.EntranceLamp, lamp, lampAt, [],
                    [EntranceLampPart.Emitter(lamp, lampAt)]));
                site.Occupy(front, matWidth * 0.5f + 0.5f);
                site.Occupy(new(signAt.M41, signAt.M43), sign.Width * 0.5f);
            }
        }
    }

    // ---------------------------------------------------------------------------------------------------------
    // Shuttle stops: a sign at the kerb of the carriageway nearest each bus feature (WLD-LIFE).
    // ---------------------------------------------------------------------------------------------------------

    private static void Stops(StreetSite site, List<FurnitureItem> items, SignParams p)
    {
        var terrain = site.World.Terrain;
        foreach (var feature in site.World.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (WorldProps.TryText(feature.Props, "subcategory") != "bus") continue;
            var anchor = Anchor(feature);
            if (site.NearestCarriageway(anchor, 45f) is not { } near) continue;
            var normal = new Vector2(-near.Along.Y, near.Along.X);
            var side = Vector2.Dot(anchor - near.Point, normal) >= 0f ? 1f : -1f;
            var offset = StreetSite.HalfWidth(near.Line) + S.StopKerbGap;
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
