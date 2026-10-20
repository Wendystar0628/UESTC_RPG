using System.Text;
using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Bake;

/// <summary>Deterministic physical delivery parts; a logical render tile is published only after every part joins.</summary>
internal static class TileBundleParts
{
    internal sealed record Slice(int MultiMesh, int Start, int Total);
    internal sealed record Location(int Index, int Count, int SurfaceStart, int TotalSurfaces, int TotalMultiMeshes, Slice[] Instances);
    internal sealed record Part(TileBundle Tile, Location Location);
    private sealed record Unit(BakedSurface? Surface, BakedMultiMesh? Multi, Slice? Slice)
    {
        internal long Bytes => Surface?.ByteCount ?? Multi!.Surface.ByteCount + Multi.InstanceData.Length;
    }

    internal static Part Whole(TileBundle tile) => new(tile, new(0, 1, 0, tile.Surfaces.Count, tile.MultiMeshes.Count,
        tile.MultiMeshes.Select((multi, index) => new Slice(index, 0, multi.InstanceCount)).ToArray()));

    internal static void Validate(Part part)
    {
        var tile = part.Tile;
        var at = part.Location;
        if (tile.Stats.Bytes > BakeCacheStore.MaximumTileUploadBytes)
            throw new InvalidDataException($"Render part {tile.Key}: raw bytes={tile.Stats.Bytes} exceeds physical delivery limits.");
        if (at.Index < 0 || at.Count <= at.Index || at.SurfaceStart < 0 || at.TotalSurfaces < at.SurfaceStart ||
            tile.Surfaces.Count > at.TotalSurfaces - at.SurfaceStart || at.TotalMultiMeshes < 0 ||
            at.Instances is null || at.Instances.Length != tile.MultiMeshes.Count)
            throw new InvalidDataException("Invalid render part location.");
        for (var index = 0; index < at.Instances.Length; index++)
        {
            var slice = at.Instances[index];
            if (slice is null || slice.MultiMesh < 0 || slice.MultiMesh >= at.TotalMultiMeshes || slice.Start < 0 ||
                slice.Total < slice.Start || tile.MultiMeshes[index].InstanceCount > slice.Total - slice.Start)
                throw new InvalidDataException("Invalid render instance range.");
        }
    }

    internal static IReadOnlyList<byte[]> Encode(string identity, TileBundle tile, Func<byte[], bool> fits)
    {
        var units = DeliveryUnits(tile);
        var groups = GroupUnits(units);

        var result = new List<byte[]>();
        var firstSurface = 0;
        for (var index = 0; index < groups.Count; index++)
        {
            var part = MakePart(tile, groups[index], index, groups.Count, firstSurface);
            var encoded = TileBundleCodec.EncodePartCandidate(identity, part);
            if (encoded is not null && fits(encoded))
            {
                result.Add(encoded);
                firstSurface += part.Tile.Surfaces.Count;
                continue;
            }
            var (left, right) = SplitGroup(groups[index]);
            groups[index] = left;
            groups.Insert(index + 1, right);
            // A split changes every part's total count; check the final bytes of every ordinal again.
            result.Clear();
            firstSurface = 0;
            index = -1;
        }
        return result;
    }

    private static List<Unit> DeliveryUnits(TileBundle tile)
    {
        var units = new List<Unit>();
        foreach (var surface in tile.Surfaces) units.Add(new(surface, null, null));
        for (var index = 0; index < tile.MultiMeshes.Count; index++)
        {
            var multi = tile.MultiMeshes[index];
            var capacity = (BakeCacheStore.MaximumTileUploadBytes - multi.Surface.ByteCount) / BakedMultiMesh.InstanceStrideBytes;
            if (capacity < 0 || capacity == 0 && multi.InstanceCount > 0)
                throw new InvalidDataException("Instance prototype leaves no room in its physical delivery part.");
            var start = 0;
            do
            {
                var count = (int)Math.Min(multi.InstanceCount - start, capacity);
                units.Add(Instance(multi, index, start, count));
                start += count;
            } while (start < multi.InstanceCount);
        }
        return units;
    }

    private static List<List<Unit>> GroupUnits(IReadOnlyList<Unit> units)
    {
        var groups = new List<List<Unit>>();
        var current = new List<Unit>();
        long bytes = 0;
        foreach (var unit in units)
        {
            if (unit.Bytes > BakeCacheStore.MaximumTileUploadBytes)
                throw new InvalidDataException("A packed surface exceeds its physical delivery part limit.");
            if (current.Count != 0 && bytes + unit.Bytes > BakeCacheStore.MaximumTileUploadBytes)
            {
                groups.Add(current);
                current = [];
                bytes = 0;
            }
            current.Add(unit);
            bytes += unit.Bytes;
        }
        if (current.Count != 0 || groups.Count == 0) groups.Add(current);
        return groups;
    }

    private static Part MakePart(TileBundle tile, IReadOnlyList<Unit> group, int index, int count, int surfaceStart) => new(
        new(tile.Key, tile.Tile, tile.Level, tile.Bounds,
            group.Where(unit => unit.Surface is not null).Select(unit => unit.Surface!).ToArray(),
            group.Where(unit => unit.Multi is not null).Select(unit => unit.Multi!).ToArray(), tile.StatisticsJson),
        new(index, count, surfaceStart, tile.Surfaces.Count, tile.MultiMeshes.Count,
            group.Where(unit => unit.Slice is not null).Select(unit => unit.Slice!).ToArray()));

    private static (List<Unit> Left, List<Unit> Right) SplitGroup(List<Unit> group)
    {
        if (group.Count > 1)
        {
            var middle = group.Count / 2;
            return (group.GetRange(0, middle), group.GetRange(middle, group.Count - middle));
        }
        if (group.Count == 1 && group[0] is { Multi: { InstanceCount: > 1 } multi, Slice: { } slice })
        {
            var first = multi.InstanceCount / 2;
            return ([Instance(multi, slice.MultiMesh, 0, first, slice.Start, slice.Total)],
                [Instance(multi, slice.MultiMesh, first, multi.InstanceCount - first, slice.Start, slice.Total)]);
        }
        throw new InvalidDataException("A packed surface cannot fit the compressed physical delivery limit.");
    }

    private static Unit Instance(BakedMultiMesh multi, int index, int start, int count, int origin = 0, int? total = null) =>
        new(null, new(multi.Surface, count, multi.InstanceData.Slice(checked(start * BakedMultiMesh.InstanceStrideBytes),
            checked(count * BakedMultiMesh.InstanceStrideBytes)), multi.Bounds), new(index, checked(origin + start), total ?? multi.InstanceCount));

    internal static TileBundle Join(IReadOnlyList<Part> parts)
    {
        if (parts.Count == 0) throw new InvalidDataException("Missing render parts.");
        var first = parts[0];
        var tile = first.Tile;
        var surfaces = new List<BakedSurface>();
        var multis = new List<BakedMultiMesh>();
        BakedMultiMesh? pending = null;
        Slice? pendingSlice = null;
        using var instances = new MemoryStream();
        byte[]? prototype = null;
        for (var index = 0; index < parts.Count; index++)
        {
            var part = parts[index];
            Validate(part);
            var location = part.Location;
            if (location.Index != index || location.Count != parts.Count || location.TotalSurfaces != first.Location.TotalSurfaces ||
                location.TotalMultiMeshes != first.Location.TotalMultiMeshes || location.SurfaceStart != surfaces.Count ||
                part.Tile.Key != tile.Key || part.Tile.Tile != tile.Tile || part.Tile.Level != tile.Level || part.Tile.Bounds != tile.Bounds ||
                part.Tile.StatisticsJson != tile.StatisticsJson || part.Tile.Surfaces.Count > 0 && (multis.Count > 0 || pending is not null))
                throw new InvalidDataException("Render parts are incomplete, out of order, or from different logical tiles.");
            surfaces.AddRange(part.Tile.Surfaces);
            for (var slot = 0; slot < part.Tile.MultiMeshes.Count; slot++)
            {
                var multi = part.Tile.MultiMeshes[slot];
                var slice = location.Instances[slot];
                var multiPrototype = SurfaceBytes(multi.Surface);
                if (slice.MultiMesh != multis.Count || slice.Start != instances.Length / BakedMultiMesh.InstanceStrideBytes ||
                    pendingSlice is not null && (slice.Total != pendingSlice.Total || multi.Bounds != pending!.Bounds ||
                        !multiPrototype.AsSpan().SequenceEqual(prototype)))
                    throw new InvalidDataException("Render instance slices are incomplete or inconsistent.");
                pending ??= multi;
                pendingSlice ??= slice;
                prototype ??= multiPrototype;
                instances.Write(multi.InstanceData.Span);
                if (instances.Length / BakedMultiMesh.InstanceStrideBytes != slice.Total) continue;
                multis.Add(new(pending.Surface, slice.Total, instances.ToArray(), pending.Bounds));
                pending = null;
                pendingSlice = null;
                prototype = null;
                instances.SetLength(0);
                instances.Position = 0;
            }
        }
        if (pending is not null || surfaces.Count != first.Location.TotalSurfaces || multis.Count != first.Location.TotalMultiMeshes)
            throw new InvalidDataException("Render tile is missing surfaces or instance slices.");
        return new(tile.Key, tile.Tile, tile.Level, tile.Bounds, surfaces, multis, tile.StatisticsJson);
    }

    private static byte[] SurfaceBytes(BakedSurface surface)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, new UTF8Encoding(false, true), true);
        TileRenderSection.WriteSurface(writer, surface);
        return stream.ToArray();
    }
}
