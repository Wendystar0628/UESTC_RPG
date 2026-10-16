using Campus.Gen.Walkable;

namespace Campus.Pipeline.Walk;

internal static class WalkInputHash
{
    internal static string Compute(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files) => WalkBakeInputHash.ComputeForMap(files);
}
