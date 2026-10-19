using System.Numerics;
using Campus.Contracts;
using Campus.Systems.Nightlight.Glow;
using Campus.Systems.Nightlight.Schedule;
using Campus.Systems.Nightlight.Windows;

namespace Campus.Systems.Nightlight.Lamps;

/// <summary>从原灯表与同一次窗光晕缓冲挑16个引用，不持有第二份灯位或开灯数据。</summary>
internal sealed class ReflectionLampSelection
{
    public const int Capacity = 16;
    public const int Rows = 2;
    private const float SecondLevelEnergy = 0.75f;
    private static readonly int ClockFaceSlot = LightChannelCatalog.Slot(new ChannelId("clock.face"));
    private readonly float[] _data = new float[Capacity * Rows * 4];

    // 行0=(type,ref,baseId,slot+32*cols)；行1=(窗中心x,y,z,外围标志)。type 0空、1灯具、2窗。
    public ReadOnlySpan<float> Data => _data;

    public void Clear() => Array.Clear(_data);

    private readonly record struct Candidate(int Kind, int Tile, int Index, int StableId, float Priority, float Distance);

    public bool Select(IReadOnlyList<LampRecord> lamps, IReadOnlyList<NightlightTileContent> tiles,
        ReadOnlySpan<Vector2> channels, ReadOnlySpan<byte> windowOverrides, Vector3 camera)
    {
        Span<Candidate> selected = stackalloc Candidate[Capacity];
        var count = 0;
        var eye = new Vector2(camera.X, camera.Z);
        for (var t = 0; t < tiles.Count; t++)
        {
            var tile = tiles[t];
            for (var n = 0; n < tile.LampIds.Count; n++)
            {
                var id = tile.LampIds[n];
                var lamp = lamps[id];
                var priority = lamp.Slot == ClockFaceSlot || lamp.Energy >= 1f ? 3 :
                    lamp.Energy >= SecondLevelEnergy ? 2 : 0;
                if (priority == 0) continue;
                var distance = Vector2.DistanceSquared(new Vector2(lamp.Base.X, lamp.Base.Z), eye);
                count = Insert(selected, count, new(1, t, id, id, priority, distance));
            }
            var windows = tile.Glow;
            for (var n = 0; n < tile.ReflectionWindowIndices.Count; n++)
            {
                var i = tile.ReflectionWindowIndices[n];
                var o = i * GlowSet.Stride;
                if (!WindowLit(windows.Buffer, o, channels, windowOverrides)) continue;
                var position = new Vector2(windows.Buffer[o + 3], windows.Buffer[o + 11]);
                count = Insert(selected, count, new(2, t, i, (int)windows.Buffer[o + 12], 1, Vector2.DistanceSquared(position, eye)));
            }
        }
        Span<float> next = stackalloc float[Capacity * Rows * 4];
        next.Clear();
        for (var i = 0; i < count; i++)
        {
            var candidate = selected[i];
            var r0 = i * 4;
            next[r0] = candidate.Kind;
            if (candidate.Kind == 1)
            {
                next[r0 + 1] = candidate.Index;
                continue;
            }
            var o = candidate.Index * GlowSet.Stride;
            var buffer = tiles[candidate.Tile].Glow.Buffer;
            next[r0 + 1] = buffer[o + 12];
            next[r0 + 2] = buffer[o + 13];
            next[r0 + 3] = buffer[o + 14];
            var r1 = (Capacity + i) * 4;
            next[r1] = buffer[o + 3];
            next[r1 + 1] = buffer[o + 7];
            next[r1 + 2] = buffer[o + 11];
            next[r1 + 3] = buffer[o + 15];
        }
        var changed = !next.SequenceEqual(_data);
        next.CopyTo(_data);
        return changed;
    }

    private static bool WindowLit(float[] buffer, int offset, ReadOnlySpan<Vector2> channels, ReadOnlySpan<byte> overrides)
    {
        var id = (int)buffer[offset + 12];
        var outskirts = ((int)buffer[offset + 15] & GlowSets.OutskirtsFlag) != 0;
        if (!outskirts && WindowOverrides.InRange(id))
        {
            var value = overrides[id * 4];
            if (value == (byte)WindowLight.ForceOn) return true;
            if (value == (byte)WindowLight.ForceOff) return false;
        }
        var packed = (int)buffer[offset + 14];
        // 只判当前候选是否发亮；阈值沿用同一个纯函数，实际亮度仍由 shader 的 lamp_window_on 求值。
        var threshold = ScheduleMath.WindowThreshold(id, (int)buffer[offset + 13], packed / LightChannelCatalog.Count);
        return channels[packed % LightChannelCatalog.Count].X > threshold;
    }

    private static int Insert(Span<Candidate> selected, int count, Candidate candidate)
    {
        var at = 0;
        while (at < count)
        {
            var other = selected[at];
            if (candidate.Priority > other.Priority ||
                (candidate.Priority == other.Priority && (candidate.Distance < other.Distance ||
                    candidate.Distance == other.Distance && candidate.StableId < other.StableId))) break;
            at++;
        }
        if (at == Capacity) return count;
        for (var j = Math.Min(count, Capacity - 1); j > at; j--) selected[j] = selected[j - 1];
        selected[at] = candidate;
        return Math.Min(count + 1, Capacity);
    }
}
