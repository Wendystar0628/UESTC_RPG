using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Street;
using Campus.Gen.World;
using S = Campus.Gen.Furniture.FurnitureStyle;
using static Campus.Gen.Furniture.FurniturePlacement;
using static Campus.Gen.Furniture.FurniturePlanner;
using static Campus.Gen.Furniture.FurnitureParts;
using static Campus.Gen.Furniture.FurniturePavingFit;

namespace Campus.Gen.Furniture;

/// <summary>Places atlas kiosks and posts with their pads and emitters.</summary>
internal static class FurnitureAnchors
{
    internal static void Plan(StreetSite site, List<FurnitureItem> items, List<BuildDiagnostic> diagnostics)
    {
        var terrain = site.World.Terrain;
        foreach (var feature in site.World.Features.Values.Where(f => f.Kind.Value == "anchor").OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            var file = feature.Zone is { } zone ? $"world/{zone.Value}.json" : "world/atlas.json";
            if (feature.Geometry is not PolygonGeometry { Rings.Outer: { Count: >= 3 } ring })
                throw new WorldDataException(file, feature.Id.Value, "Required anchor polygon is missing.");
            var form = WorldProps.TryText(feature.Props, "form");
            if (form is not ("kiosk" or "post"))
                throw new WorldDataException(file, feature.Id.Value, "Required anchor form must be kiosk or post.");
            if (WorldProps.TryNumber(feature.Props, "yawDeg") is not { } yaw || !float.IsFinite(yaw))
                throw new WorldDataException(file, feature.Id.Value, "Required finite yawDeg is missing.");
            var facing = Angles.Heading(yaw);
            var right = new Vector2(-facing.Y, facing.X);
            var centre = ring.Aggregate(Vector2.Zero, (a, p) => a + p) / ring.Count;
            var width = Extent(ring, right);
            var depth = Extent(ring, facing);
            if (!float.IsFinite(width) || width < (form == "kiosk" ? 1.4f : 0.16f))
                throw new WorldDataException(file, feature.Id.Value, "Anchor polygon width is missing or below the form's minimum size.");
            if (!float.IsFinite(depth) || depth < (form == "kiosk" ? 0.4f : 0.16f))
                throw new WorldDataException(file, feature.Id.Value, "Anchor polygon depth is missing or below the form's minimum size.");
            if (feature.Height is not { } height || !float.IsFinite(height) || height < (form == "kiosk" ? 2.2f : 0.6f))
                throw new WorldDataException(file, feature.Id.Value, "Required height is missing or below the form's minimum size.");
            var key = "anchor:" + feature.Id.Value;
            var ground = terrain.HeightAt(centre.X, centre.Y);
            var deck = form == "kiosk" && WorldProps.TryNumber(feature.Props, "groundY") is { } groundY && groundY > ground + S.DeckAbove
                ? groundY : (float?)null;
            // An authored elevated deck stands above ground-level outlines (SYS-ATLAS-TRAVEL).
            var landing = deck is not null ? Landing.Open : site.LandingAt(centre);
            if (landing != Landing.Open)
                throw new WorldDataException(file, feature.Id.Value, $"Anchor landing is {Where(landing)} (SYS-ATLAS-TRAVEL).");
            switch (form)
            {
                case "kiosk":
                    {
                        var p = S.Kiosk with { Width = width, Depth = depth, Height = height };
                        // Large entity (WLD-TERRAIN): on its pad, with a paved apron; a deck the height field does not carry (A06)
                        // comes from the overlay's groundY. An apron off the paving moves to its edge (a deck is paving already).
                        if (deck is null) (centre, facing) = KioskOnPaving(site, centre, facing, p.Width * 0.5f + S.ApronMargin, p.Depth * 0.5f + S.ApronMargin, diagnostics, feature.Id);
                        var level = deck ?? terrain.PadY(feature.Id);
                        var plan = Frame(centre, facing, 0f);
                        var apron = Land.Pad(terrain, plan, p.Width * 0.5f + S.ApronMargin, p.Depth * 0.5f + S.ApronMargin, level)!;
                        var at = Frame(centre, facing, apron.Top);
                        var kiosk = Item(key, FurnitureKind.AtlasKiosk, PartCatalog.AtlasKiosk, p, at, KioskDef.Collision(p), [AtlasKioskPart.Emitter(p, at)]);
                        items.Add(kiosk with { Pad = apron, Colliders = [.. kiosk.Colliders, apron.Collider()] });
                        site.Occupy(centre, MathF.Max(p.Width, p.Depth) * 0.5f + S.ApronMargin);
                        break;
                    }
                case "post":
                    {
                        var p = S.Post with { Width = width, Depth = depth, Height = height };
                        var radius = MathF.Max(p.Width, p.Depth) * 0.5f;
                        centre = ToPaving(site, centre, facing, radius, radius, diagnostics, feature.Id, form);
                        if (Land.Point(terrain, centre, radius) is not { } y) continue;
                        var at = Frame(centre, facing, y);
                        items.Add(Item(key, FurnitureKind.AtlasPost, PartCatalog.AtlasPost, p, at, PostDef.Collision(p), [AtlasPostPart.Emitter(p, at)]));
                        site.Occupy(centre, radius);
                        break;
                    }
            }
        }
    }

    private static string Where(Landing landing) => landing switch
    {
        Landing.Carriageway => "on a carriageway",
        Landing.Water => "in water",
        Landing.Building => "in a building",
        _ => throw new ArgumentOutOfRangeException(nameof(landing), landing, "Not a blocked anchor landing."),
    };
}
