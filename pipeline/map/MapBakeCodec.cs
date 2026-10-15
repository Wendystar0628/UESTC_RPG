using System.Buffers.Binary;
using System.Text;
using System.Text.Json;

namespace Campus.Pipeline.Map;

/// <summary>Fixed CPU format. The outer cache owner handles atomic writes and reference-manifest trust.</summary>
public static class MapBakeCodec
{
    public const int Version = 1;
    public const int MaximumBytes = 96 * 1024 * 1024;
    private static ReadOnlySpan<byte> Magic => "CMAP0001"u8;
    private static readonly string[] TextureNames = ["map_sdf_a", "map_sdf_b", "map_sdf_out", "map_sdf_ring"];

    /// <summary>Standalone files for inspection/consumers. Names are fixed, never caller-provided paths.</summary>
    public static IReadOnlyDictionary<string, byte[]> Files(MapBakeProduct product)
    {
        Validate(product);
        var files = new SortedDictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["map.manifest.json"] = JsonSerializer.SerializeToUtf8Bytes(product.Manifest, AtlasLabelData.JsonOptions),
            ["regions.u8"] = product.Regions.Cells,
            ["labels.bake"] = JsonSerializer.SerializeToUtf8Bytes(product.Labels, AtlasLabelData.JsonOptions)
        };
        foreach (var texture in product.Textures) files.Add(texture.Name + ".rgba8", texture.Rgba8);
        return files;
    }

    public static byte[] Serialize(MapBakeProduct product)
    {
        var files = Files(product);
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write(Magic);
        writer.Write(files.Count);
        foreach (var (name, data) in files.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            byte[] nameBytes = Encoding.ASCII.GetBytes(name);
            writer.Write(nameBytes.Length); writer.Write(nameBytes);
            writer.Write(data.Length); writer.Write(data);
        }
        if (stream.Length > MaximumBytes) throw new InvalidDataException("Map product exceeds its byte limit.");
        return stream.ToArray();
    }

    public static MapBakeProduct Deserialize(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length > MaximumBytes || bytes.Length < 12 || !bytes[..8].SequenceEqual(Magic))
            throw new InvalidDataException("Invalid map product header or length.");
        var reader = new Reader(bytes[8..]);
        int count = reader.Number(7, 7);
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        for (int i = 0; i < count; i++)
        {
            int nameLength = reader.Number(1, 64);
            string name = Encoding.ASCII.GetString(reader.Take(nameLength));
            if (!AllowedFile(name) || files.ContainsKey(name)) throw new InvalidDataException("Unknown or duplicate map file.");
            int limit = name.EndsWith(".rgba8", StringComparison.Ordinal) ? 16 * 1024 * 1024 : 4 * 1024 * 1024;
            files.Add(name, reader.Take(reader.Number(0, limit)).ToArray());
        }
        if (!reader.Empty) throw new InvalidDataException("Trailing map product bytes.");
        return FromFiles(files);
    }

    public static MapBakeProduct FromFiles(IReadOnlyDictionary<string, byte[]> files)
    {
        if (files.Count != 7 || files.Keys.Any(name => !AllowedFile(name))) throw new InvalidDataException("Unexpected map product files.");
        var manifest = Json<MapManifest>(Required("map.manifest.json"));
        var labels = Json<LabelsBake>(Required("labels.bake"));
        if (manifest.Textures is null || manifest.Textures.Count != 4 || manifest.Textures.Any(info => info is null) || manifest.Regions is null)
            throw new InvalidDataException("Missing map product metadata.");
        var textures = manifest.Textures.Select(info => new MapTexture(info.Name, info.Width, info.Height, info.Rect, Required(info.File))).ToArray();
        var r = manifest.Regions;
        var product = new MapBakeProduct(manifest, textures, new(r.Rect, r.Width, r.Height, r.CellMetres, r.Ids, Required(r.File)), labels);
        Validate(product);
        return product;

        byte[] Required(string name) => files.TryGetValue(name, out var value) && value is not null
            ? value : throw new InvalidDataException($"Missing map file {name}.");
    }

    public static void Validate(MapBakeProduct product)
    {
        var manifest = product.Manifest;
        if (manifest.Version != Version || manifest.Format != "rgba8-linear" || manifest.RowOrder != "z-increasing" ||
            manifest.DistanceRangeMetres != 8 || manifest.BoundaryByte != 128 ||
            manifest.Textures is null || product.Textures is null || manifest.Textures.Count != 4 || product.Textures.Count != 4 ||
            string.IsNullOrWhiteSpace(manifest.Map) || string.IsNullOrWhiteSpace(manifest.WorldModelHash) ||
            string.IsNullOrWhiteSpace(manifest.CodeHash) || string.IsNullOrWhiteSpace(manifest.LabelInputHash))
            throw new InvalidDataException("Unsupported/incomplete map manifest.");
        MapBaker.ValidateRect(manifest.DataRect); MapBaker.ValidateRect(manifest.AtlasRect);
        for (int i = 0; i < 4; i++)
        {
            var info = manifest.Textures[i];
            var texture = product.Textures[i];
            if (info is null || texture is null || info.Rect is null || info.Name != TextureNames[i] || info.File != info.Name + ".rgba8" || texture.Name != info.Name ||
                info.Width is < 1 or > 2048 || info.Height is < 1 or > 2048 || texture.Width != info.Width || texture.Height != info.Height ||
                texture.Rect != info.Rect || texture.Rgba8 is null || texture.Rgba8.Length != checked(info.Width * info.Height * 4) ||
                info.Rect != (i switch { 0 or 1 => manifest.Textures[0].Rect, 2 => manifest.DataRect, 3 => manifest.AtlasRect, _ => throw new InvalidDataException("Unknown texture.") }) ||
                info.Channels is null || info.Channels.Length != 4 ||
                MapBaker.Hash(texture.Rgba8) != info.Sha256)
                throw new InvalidDataException("Invalid map texture length, identity or checksum.");
            MapBaker.ValidateRect(info.Rect);
        }
        var grid = product.Regions;
        var r = manifest.Regions;
        if (r is null || grid is null || r.File != "regions.u8" || r.Rect != grid.Rect || r.Rect != manifest.DataRect ||
            grid.Width is < 1 or > 8192 || grid.Height is < 1 or > 8192 || r.Width != grid.Width || r.Height != grid.Height ||
            !float.IsFinite(grid.CellMetres) || grid.CellMetres is < 1 or > 64 || r.CellMetres != grid.CellMetres ||
            grid.Width != (int)Math.Ceiling((grid.Rect.Max.X - grid.Rect.Min.X) / grid.CellMetres) ||
            grid.Height != (int)Math.Ceiling((grid.Rect.Max.Y - grid.Rect.Min.Y) / grid.CellMetres) ||
            grid.Ids is null || grid.Ids.Length is < 1 or > 256 || grid.Ids[0] != "" || r.Ids is null || !r.Ids.SequenceEqual(grid.Ids) ||
            grid.Ids.Distinct(StringComparer.Ordinal).Count() != grid.Ids.Length || grid.Cells is null ||
            grid.Cells.Length != checked(grid.Width * grid.Height) || grid.Cells.Length > 4 * 1024 * 1024 ||
            grid.Cells.Any(index => index >= grid.Ids.Length) || MapBaker.Hash(grid.Cells) != r.Sha256)
            throw new InvalidDataException("Invalid map region grid.");
        if (product.Labels is null || product.Labels.Version != 1 || product.Labels.InputHash != manifest.LabelInputHash ||
            product.Labels.Bands is null || (product.Labels.Ready ? product.Labels.Bands.Count != 10 : product.Labels.Bands.Count != 0))
            throw new InvalidDataException("Invalid baked labels.");
        var bands = new HashSet<(string, byte)>();
        foreach (var band in product.Labels.Bands)
        {
            if (band is null || band.Language is not ("zh" or "en") || band.ZoomBand > 4 || !bands.Add((band.Language, band.ZoomBand)) ||
                !float.IsFinite(band.PixelsPerMetre) || band.PixelsPerMetre <= 0 || band.Labels is null || band.Labels.Count > 4096)
                throw new InvalidDataException("Invalid label language/zoom band.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var label in band.Labels)
            {
                if (label is null || string.IsNullOrWhiteSpace(label.Id) || !ids.Add(label.Id) || string.IsNullOrWhiteSpace(label.Text) || label.Text.Length > 4096 ||
                    !Finite(label.At) || !Finite(label.OffsetPixels) || !Finite(label.Size) || label.Size.X <= 0 || label.Size.Y <= 0 || label.Size.X > 4096 || label.Size.Y > 4096)
                    throw new InvalidDataException("Invalid placed label.");
            }
        }
    }

    private static bool Finite(System.Numerics.Vector2 point) => float.IsFinite(point.X) && float.IsFinite(point.Y);

    private static bool AllowedFile(string name) => name is "map.manifest.json" or "regions.u8" or "labels.bake" ||
        TextureNames.Any(texture => name == texture + ".rgba8");

    private static T Json<T>(byte[] bytes)
    {
        if (bytes.Length > 4 * 1024 * 1024) throw new InvalidDataException("Map metadata exceeds its byte limit.");
        try { return JsonSerializer.Deserialize<T>(bytes, AtlasLabelData.JsonOptions) ?? throw new InvalidDataException("Empty map metadata."); }
        catch (JsonException ex) { throw new InvalidDataException("Invalid map metadata.", ex); }
    }

    private ref struct Reader(ReadOnlySpan<byte> bytes)
    {
        private ReadOnlySpan<byte> _remaining = bytes;
        internal bool Empty => _remaining.IsEmpty;
        internal ReadOnlySpan<byte> Take(int length)
        {
            if (length < 0 || length > _remaining.Length) throw new InvalidDataException("Truncated map product.");
            var result = _remaining[..length]; _remaining = _remaining[length..]; return result;
        }
        internal int Number(int min, int max)
        {
            int value = BinaryPrimitives.ReadInt32LittleEndian(Take(4));
            if (value < min || value > max) throw new InvalidDataException("Map section length/count exceeds bounds.");
            return value;
        }
    }
}
