using System.Numerics;
using Campus.Contracts.Maps;

namespace Campus.Contracts.Atlas;

/// <summary>Pure CPU atlas bridge. Ready means four real SDF textures are available, not that labels/glyphs/regions are ready.</summary>
public interface IAtlasBaseSource
{
    long Rev { get; }
    MapId Map { get; }
    bool Ready { get; }
    AtlasBaseSnapshot? Snapshot { get; }
}

/// <summary>
/// Row-major linear RGBA8 with no header or mip levels. Column increases east (+X), row south (+Z).
/// Construction makes exactly one owned copy of the pixels; reads do not copy. Consumers must treat
/// the exposed memory as read-only. The producer must not make an additional pre-constructor clone.
/// </summary>
public sealed class AtlasBaseTexture
{
    private readonly byte[] _rgba8;
    public AtlasBaseTexture(string name, int width, int height, MapRect rect, ReadOnlySpan<byte> rgba8)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (width <= 0 || height <= 0 || (long)width * height > Array.MaxLength / 4)
            throw new ArgumentOutOfRangeException(nameof(width), "Texture dimensions must fit an RGBA8 array.");
        if (rgba8.Length != width * height * 4) throw new ArgumentException("RGBA8 length does not match the dimensions.", nameof(rgba8));
        AtlasBaseValidation.Rect(rect);
        Name = name; Width = width; Height = height; Rect = new(rect.Min, rect.Max);
        _rgba8 = rgba8.ToArray();
    }
    public string Name { get; }
    public int Width { get; }
    public int Height { get; }
    public MapRect Rect { get; }
    public ReadOnlyMemory<byte> Rgba8 => _rgba8;
}

public sealed record AtlasPlacedLabel(string Id, string Text, Vector2 At, Vector2 OffsetPixels, Vector2 Size);

/// <summary>One measured language/zoom band. Coordinates are world XZ; offset and size are logical pixels.</summary>
public sealed class AtlasLabelBand
{
    public AtlasLabelBand(string language, byte zoomBand, float pixelsPerMetre, IReadOnlyList<AtlasPlacedLabel> labels)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        ArgumentNullException.ThrowIfNull(labels);
        if (zoomBand > 4) throw new ArgumentOutOfRangeException(nameof(zoomBand));
        if (!float.IsFinite(pixelsPerMetre) || pixelsPerMetre <= 0) throw new ArgumentOutOfRangeException(nameof(pixelsPerMetre));
        var copy = labels.ToArray();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var label in copy)
        {
            ArgumentNullException.ThrowIfNull(label);
            if (string.IsNullOrWhiteSpace(label.Id) || !ids.Add(label.Id) || string.IsNullOrWhiteSpace(label.Text) ||
                !AtlasBaseValidation.Finite(label.At) || !AtlasBaseValidation.Finite(label.OffsetPixels) ||
                !AtlasBaseValidation.Finite(label.Size) || label.Size.X <= 0 || label.Size.Y <= 0)
                throw new ArgumentException("Labels require unique IDs, text, finite coordinates and positive measured bounds.", nameof(labels));
        }
        Language = language; ZoomBand = zoomBand; PixelsPerMetre = pixelsPerMetre;
        Labels = Array.AsReadOnly(copy);
    }
    public string Language { get; }
    public byte ZoomBand { get; }
    public float PixelsPerMetre { get; }
    public IReadOnlyList<AtlasPlacedLabel> Labels { get; }
}

/// <summary>
/// Immutable published atlas data. Textures are already owned and are not copied again; small lookup and
/// label-band collections are defensively copied once. UI reads no pipeline types, manifest paths or cache hashes.
/// The producer validates the font-measurement provenance before labelsReady=true. Only declared bands need
/// be ready; consumers select an available language/band rather than assume all languages and zooms exist.
/// </summary>
public sealed class AtlasBaseSnapshot
{
    public const byte UnknownRegion = byte.MaxValue;
    public const float SdfRangeMetres = 8;
    public const byte SdfBoundaryByte = 128;
    private readonly byte[] _placeToRegion;

    public AtlasBaseSnapshot(MapId map, MapRect pageRect,
        AtlasBaseTexture sdfA, AtlasBaseTexture sdfB, AtlasBaseTexture sdfOut, AtlasBaseTexture sdfRing,
        IReadOnlyList<AtlasLabelBand>? labelBands = null, bool labelsReady = false,
        AtlasBaseTexture? glyphs = null, IReadOnlyList<string>? placeIds = null,
        IReadOnlyList<string>? topRegionIds = null, ReadOnlyMemory<byte> placeToRegion = default,
        bool regionsReady = false, IReadOnlyList<string>? warnings = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(map.Value);
        AtlasBaseValidation.Rect(pageRect);
        RequireTexture(sdfA, "map_sdf_a"); RequireTexture(sdfB, "map_sdf_b");
        RequireTexture(sdfOut, "map_sdf_out"); RequireTexture(sdfRing, "map_sdf_ring");
        if (sdfA.Rect != sdfB.Rect || sdfRing.Rect != pageRect)
            throw new ArgumentException("A/B must share their campus rectangle; ring must cover PageRect.");
        if (glyphs is not null) RequireTexture(glyphs, "map_glyphs");
        var bands = labelBands?.ToArray() ?? [];
        var bandKeys = new HashSet<(string, byte)>();
        foreach (var band in bands)
        {
            ArgumentNullException.ThrowIfNull(band);
            if (!bandKeys.Add((band.Language, band.ZoomBand))) throw new ArgumentException("Duplicate label language/band.", nameof(labelBands));
        }
        if (labelsReady && bands.Length == 0) throw new ArgumentException("Ready labels require declared, measured bands.", nameof(labelsReady));
        var places = placeIds?.ToArray() ?? [""];
        var regions = topRegionIds?.ToArray() ?? [];
        if (places.Length is < 1 or > 256 || places[0] != "" || places.Skip(1).Any(string.IsNullOrWhiteSpace) ||
            places.Distinct(StringComparer.Ordinal).Count() != places.Length)
            throw new ArgumentException("PlaceIds must preserve the source byte index with an empty ID at zero.", nameof(placeIds));
        if (regions.Length > 64 || regions.Any(string.IsNullOrWhiteSpace) ||
            regions.Distinct(StringComparer.Ordinal).Count() != regions.Length || !regions.SequenceEqual(regions.Order(StringComparer.Ordinal)))
            throw new ArgumentException("TopRegionIds must be unique ordinal-sorted IDs for at most 64 mask bits.", nameof(topRegionIds));
        _placeToRegion = placeToRegion.IsEmpty ? Enumerable.Repeat(UnknownRegion, 256).ToArray() : placeToRegion.ToArray();
        if (_placeToRegion.Length != 256 || _placeToRegion[0] != UnknownRegion)
            throw new ArgumentException("PlaceToRegion needs 256 entries and unknown at source index zero.", nameof(placeToRegion));
        for (var place = 1; place < _placeToRegion.Length; place++)
        {
            var bit = _placeToRegion[place];
            if (bit != UnknownRegion && (place >= places.Length || bit >= regions.Length))
                throw new ArgumentException("PlaceToRegion contains an unknown place or invalid top-region bit.", nameof(placeToRegion));
            if (regionsReady && place < places.Length && bit == UnknownRegion)
                throw new ArgumentException("Ready regions cannot contain unresolved registered place IDs.", nameof(regionsReady));
        }
        if (regionsReady && (regions.Length == 0 || places.Length == 1))
            throw new ArgumentException("Ready regions require a real region/place mapping.", nameof(regionsReady));
        Map = map; PageRect = new(pageRect.Min, pageRect.Max);
        SdfA = sdfA; SdfB = sdfB; SdfOut = sdfOut; SdfRing = sdfRing; Glyphs = glyphs;
        LabelBands = Array.AsReadOnly(bands); LabelsReady = labelsReady;
        PlaceIds = Array.AsReadOnly(places); TopRegionIds = Array.AsReadOnly(regions); RegionsReady = regionsReady;
        Warnings = Array.AsReadOnly(warnings?.ToArray() ?? []);
    }

    public MapId Map { get; }
    public MapRect PageRect { get; }
    public AtlasBaseTexture SdfA { get; }
    public AtlasBaseTexture SdfB { get; }
    public AtlasBaseTexture SdfOut { get; }
    public AtlasBaseTexture SdfRing { get; }
    public AtlasBaseTexture? Glyphs { get; }
    public bool GlyphsReady => Glyphs is not null;
    public IReadOnlyList<AtlasLabelBand> LabelBands { get; }
    public bool LabelsReady { get; }
    // The producer additionally requires authoritative boundaries and actual nonzero region-grid samples.
    // Missing boundary input / all-unknown region data MUST keep this false even when the lookup exists.
    public bool RegionsReady { get; }
    public IReadOnlyList<string> PlaceIds { get; }
    public IReadOnlyList<string> TopRegionIds { get; }
    // Index = original source place byte; value = TopRegionIds/reveal bit 0-63, or 255 unknown.
    // Source place zero is unknown/outside and bypasses mask lookup. UI uses this table only when RegionsReady.
    public ReadOnlyMemory<byte> PlaceToRegion => _placeToRegion;
    public IReadOnlyList<string> Warnings { get; }

    private static void RequireTexture(AtlasBaseTexture texture, string name)
    {
        ArgumentNullException.ThrowIfNull(texture);
        if (texture.Name != name) throw new ArgumentException($"Expected atlas texture {name}.", nameof(texture));
    }
}

// SDF byte = clamp(round(128 + distanceMetres * 16), 0, 255), negative inside.
// A: water/green/road/building SDF. B: source place byte / class byte / plaza-sport SDF / campus-boundary SDF.
// B class: 0 none, 1 building, 2 landmark, 3 plaza, 4 sport, 5 bridge; categories sample nearest base level.
// Out/Ring: outskirts block / road / water / cut-line SDF. No glyph atlas is implied by these four textures.
internal static class AtlasBaseValidation
{
    internal static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
    internal static void Rect(MapRect rect)
    {
        ArgumentNullException.ThrowIfNull(rect);
        if (!Finite(rect.Min) || !Finite(rect.Max) || rect.Min.X >= rect.Max.X || rect.Min.Y >= rect.Max.Y)
            throw new ArgumentException("Atlas rectangles must be finite and nonempty.", nameof(rect));
    }
}
