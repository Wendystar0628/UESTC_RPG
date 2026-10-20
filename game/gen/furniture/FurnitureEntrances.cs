using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Street;
using S = Campus.Gen.Furniture.FurnitureStyle;
using static Campus.Gen.Furniture.FurniturePlacement;
using static Campus.Gen.Furniture.FurniturePlanner;
using static Campus.Gen.Furniture.FurnitureParts;

namespace Campus.Gen.Furniture;

/// <summary>Dresses unclaimed open entrances on their authored landing.</summary>
internal static class FurnitureEntrances
{
    internal static void Plan(StreetSite site, List<FurnitureItem> items, Func<FeatureId, Aspects, string?> claimedBy, SignParams sign)
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
}
