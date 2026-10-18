using Campus.Contracts;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;

namespace Campus.Systems.Atlas;

/// <summary>Publishes a frozen CPU view. HUD code does not depend on bake products, files, or cache keys.</summary>
[KernelService(typeof(IAtlasBaseSource), Owner = "WP-48c")]
[KernelService(typeof(AtlasBaseSource), Owner = "WP-48c")]
public sealed class AtlasBaseSource : IAtlasBaseSource
{
    public long Rev { get; private set; }
    public MapId Map { get; private set; }
    public bool Ready => Snapshot is not null;
    public AtlasBaseSnapshot? Snapshot { get; private set; }

    public void Reset(MapId map)
    {
        Map = map;
        Snapshot = null;
        Rev++;
    }

    public void Publish(MapId map, MapRect pageRect, IReadOnlyList<AtlasRegion> regions, MapBakeProduct product)
    {
        ArgumentNullException.ThrowIfNull(product);
        if (Map != map || product.Manifest.Map != map.Value || product.Manifest.AtlasRect != pageRect)
            throw new InvalidDataException("Atlas bake belongs to another map or page rectangle.");
        if (product.Manifest.Version != 1 || product.Manifest.Format != "rgba8-linear"
            || product.Manifest.RowOrder != "z-increasing" || product.Manifest.BoundaryByte != 128
            || product.Manifest.DistanceRangeMetres != 8)
            throw new InvalidDataException("Unsupported Atlas distance field encoding.");

        // This is the only byte copy. Reading Snapshot/Rgba8 does not copy per frame.
        var sdfA = Freeze("map_sdf_a");
        var sdfB = Freeze("map_sdf_b");
        var sdfOut = Freeze("map_sdf_out");
        var sdfRing = Freeze("map_sdf_ring");
        var labelsReady = product.Labels.Ready && !string.IsNullOrWhiteSpace(product.Labels.FontMetricsHash);
        var bands = product.Labels.Bands.Select(band => new AtlasLabelBand(band.Language, band.ZoomBand,
            band.PixelsPerMetre, band.Labels.Select(label => new AtlasPlacedLabel(label.Id, label.Text,
                label.At, label.OffsetPixels, label.Size)).ToArray())).ToArray();

        var warnings = new SortedSet<string>(product.Manifest.Warnings, StringComparer.Ordinal);
        if (!labelsReady) warnings.Add("label-layout-awaits-runtime-font-measurements");
        // WP-48a has not produced the landmark MSDF atlas or its glyph layout yet.
        warnings.Add("landmark-glyphs-deferred");
        var placeIds = product.Regions.Ids;
        if (placeIds.Length is < 1 or > 256 || placeIds[0] != ""
            || placeIds.Distinct(StringComparer.Ordinal).Count() != placeIds.Length)
            throw new InvalidDataException("Invalid Atlas place index table.");
        var topIds = regions.Where(region => region.Parent is null).Select(region => region.Id)
            .Order(StringComparer.Ordinal).ToArray();
        if (topIds.Length > 64 || topIds.Distinct(StringComparer.Ordinal).Count() != topIds.Length)
            throw new InvalidDataException("Atlas reveal mask supports at most 64 unique regions.");
        var byId = regions.ToDictionary(region => region.Id, StringComparer.Ordinal);
        var placeToRegion = new byte[256];
        Array.Fill(placeToRegion, byte.MaxValue);
        var complete = topIds.Length > 0;
        for (var index = 1; index < placeIds.Length; index++)
        {
            if (!byId.TryGetValue(placeIds[index], out var region)) { complete = false; continue; }
            var bit = Array.IndexOf(topIds, region.Parent ?? region.Id);
            if (bit < 0) { complete = false; continue; }
            placeToRegion[index] = checked((byte)bit);
        }
        if (!complete) warnings.Add("region-place-table-incomplete");
        var grid = product.Regions;
        if (grid.Width <= 0 || grid.Height <= 0 || !float.IsFinite(grid.CellMetres) || grid.CellMetres <= 0
            || (long)grid.Width * grid.Height != grid.Cells.Length || grid.Rect != product.Manifest.DataRect)
            throw new InvalidDataException("Invalid Atlas region grid dimensions.");
        if (grid.Cells.Any(index => index >= placeIds.Length))
            throw new InvalidDataException("Atlas region grid contains an unknown place index.");
        var regionsReady = complete && grid.Cells.Any(index => index != 0)
            && !warnings.Contains("region-boundaries-missing-region-query-returns-unknown");
        if (!regionsReady) warnings.Add("regions-not-ready");

        // Base Ready only means these four SDF planes can be drawn. Each optional part reports its own readiness.
        Snapshot = new(map, pageRect, sdfA, sdfB, sdfOut, sdfRing, bands, labelsReady,
            glyphs: null, placeIds: placeIds, topRegionIds: topIds, placeToRegion: placeToRegion,
            regionsReady: regionsReady, warnings: warnings.ToArray());
        Rev++;
        return;

        AtlasBaseTexture Freeze(string name)
        {
            var found = product.Textures.Where(texture => texture.Name == name).ToArray();
            if (found.Length != 1) throw new InvalidDataException($"Atlas bake must contain one {name} texture.");
            var texture = found[0];
            return new(texture.Name, texture.Width, texture.Height, texture.Rect, texture.Rgba8);
        }
    }
}
