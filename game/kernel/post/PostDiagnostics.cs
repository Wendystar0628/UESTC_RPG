using System.Globalization;
using System.Text;

namespace Campus.Kernel.Post;

/// <summary>
/// One colour-decision-list value set (slope, offset, power per channel plus saturation) with the raw float bits.
/// Plain readonly value: safe to hand across threads.
/// </summary>
internal readonly record struct CdlSample(
    float SlopeX, float SlopeY, float SlopeZ,
    float OffsetX, float OffsetY, float OffsetZ,
    float PowerX, float PowerY, float PowerZ,
    float Saturation)
{
    internal static uint Bits(float value) => BitConverter.SingleToUInt32Bits(value);

    /// <summary>The ten floats in a fixed order: slope xyz, offset xyz, power xyz, saturation.</summary>
    internal float[] ToArray() => [SlopeX, SlopeY, SlopeZ, OffsetX, OffsetY, OffsetZ, PowerX, PowerY, PowerZ, Saturation];

    /// <summary>True when every one of the ten floats has identical bits (NaN-safe, -0 distinct from +0).</summary>
    internal bool BitsEqual(in CdlSample other)
    {
        var a = ToArray();
        var b = other.ToArray();
        for (var i = 0; i < a.Length; i++) if (Bits(a[i]) != Bits(b[i])) return false;
        return true;
    }
}

/// <summary>
/// Immutable read-only evidence of one FinishEffect render callback (the FramePostDraw boundary of PostChain's
/// frame): the CDL the main thread published for this frame, the CDL the render thread had baked into the LUT
/// before and after this callback's bake decision, and the LUT identity. The LUT identity is a bake counter and a
/// fingerprint of the bake parameters; the GPU texture contents are NOT read.
/// </summary>
internal sealed record PostDiagSnapshot(
    long Sequence,
    long WorldInstance,
    ulong MainFrame,
    int DrawFrame,
    CdlSample Current,
    bool BakedBeforeValid,
    CdlSample BakedBefore,
    CdlSample BakedAfter,
    bool Rebaked,
    long LutBakeCount,
    ulong LutParamFingerprint)
{
    /// <summary>The bake kept an older LUT although the current CDL differs from it bit-wise (inside the epsilon).</summary>
    internal bool StaleKept => !Rebaked && BakedBeforeValid && !Current.BitsEqual(BakedBefore);

    internal const string LogPrefix = "WP05_POST_DIAG ";

    private static readonly string[] Names =
        ["slopeX", "slopeY", "slopeZ", "offsetX", "offsetY", "offsetZ", "powerX", "powerY", "powerZ", "saturation"];

    /// <summary>One-line JSON (invariant culture, floats as shortest round-trip text plus raw bits as hex).</summary>
    internal string ToJson()
    {
        var sb = new StringBuilder(1024);
        sb.Append("{\"v\":1,\"seq\":").Append(Sequence.ToString(CultureInfo.InvariantCulture))
          .Append(",\"worldInstance\":").Append(WorldInstance.ToString(CultureInfo.InvariantCulture))
          .Append(",\"mainFrame\":").Append(MainFrame.ToString(CultureInfo.InvariantCulture))
          .Append(",\"drawFrame\":").Append(DrawFrame.ToString(CultureInfo.InvariantCulture))
          .Append(",\"current\":");
        AppendCdl(sb, Current);
        sb.Append(",\"bakedBeforeValid\":").Append(BakedBeforeValid ? "true" : "false").Append(",\"bakedBefore\":");
        AppendCdl(sb, BakedBefore);
        sb.Append(",\"bakedAfter\":");
        AppendCdl(sb, BakedAfter);
        sb.Append(",\"rebaked\":").Append(Rebaked ? "true" : "false")
          .Append(",\"staleKept\":").Append(StaleKept ? "true" : "false")
          .Append(",\"lut\":{\"bakeCount\":").Append(LutBakeCount.ToString(CultureInfo.InvariantCulture))
          .Append(",\"paramFingerprint\":\"0x").Append(LutParamFingerprint.ToString("x16", CultureInfo.InvariantCulture))
          .Append("\",\"kind\":\"bake-parameters\",\"gpuTextureRead\":false}}");
        return sb.ToString();
    }

    private static void AppendCdl(StringBuilder sb, in CdlSample s)
    {
        var values = s.ToArray();
        sb.Append('{');
        for (var i = 0; i < values.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append('"').Append(Names[i]).Append("\":{\"value\":");
            var v = values[i];
            if (float.IsFinite(v)) sb.Append(v.ToString("R", CultureInfo.InvariantCulture));
            else sb.Append('"').Append(v.ToString(CultureInfo.InvariantCulture)).Append('"');
            sb.Append(",\"bits\":\"0x").Append(CdlSample.Bits(v).ToString("x8", CultureInfo.InvariantCulture)).Append("\"}");
        }
        sb.Append('}');
    }
}

/// <summary>
/// Diagnostic switch plus the one-slot immutable hand-off from the render thread to the main thread. Created once
/// by <see cref="PostChain"/>; when <see cref="Enabled"/> is false <see cref="Capture"/> returns at once, allocates
/// nothing and publishes nothing, so the render behaviour is unchanged.
/// </summary>
internal sealed class PostDiagSink
{
    internal PostDiagSink(bool enabled, bool everyFrame)
    {
        Enabled = enabled;
        EveryFrame = everyFrame;
    }

    internal bool Enabled { get; }

    /// <summary>Log every snapshot instead of only the first, changes of bake state and a slow heartbeat.</summary>
    internal bool EveryFrame { get; }

    private PostDiagSnapshot? _latest;
    private long _sequence;
    private long _bakeCount;
    private long _loggedSequence;
    private long _loggedInstance = -1;
    private long _loggedBakeCount = -1;
    private long _loggedHeartbeatAt;

    /// <summary>Latest snapshot; safe from any thread (the snapshot is immutable).</summary>
    internal PostDiagSnapshot? Latest => Volatile.Read(ref _latest);

    /// <summary>
    /// Render thread only. Builds the snapshot of this callback and publishes it; <paramref name="rebaked"/> says
    /// whether this callback issued a LUT bake.
    /// </summary>
    internal void Capture(long worldInstance, ulong mainFrame, int drawFrame, in CdlSample current,
        bool bakedBeforeValid, in CdlSample bakedBefore, in CdlSample bakedAfter, bool rebaked,
        int lutSize, float baseSaturation, float splitToning)
    {
        if (!Enabled) return;
        if (rebaked) _bakeCount++;
        var snapshot = new PostDiagSnapshot(++_sequence, worldInstance, mainFrame, drawFrame, current,
            bakedBeforeValid, bakedBefore, bakedAfter, rebaked, _bakeCount,
            Fingerprint(bakedAfter, lutSize, baseSaturation, splitToning));
        Volatile.Write(ref _latest, snapshot);
    }

    /// <summary>FNV-1a 64 over the bits of the parameters the LUT bake dispatch consumes.</summary>
    internal static ulong Fingerprint(in CdlSample baked, int lutSize, float baseSaturation, float splitToning)
    {
        var hash = 14695981039346656037UL;
        foreach (var v in baked.ToArray()) hash = Mix(hash, CdlSample.Bits(v));
        hash = Mix(hash, (uint)lutSize);
        hash = Mix(hash, CdlSample.Bits(baseSaturation));
        return Mix(hash, CdlSample.Bits(splitToning));
    }

    private static ulong Mix(ulong hash, uint word)
    {
        for (var i = 0; i < 4; i++)
        {
            hash ^= (word >> (8 * i)) & 0xFFu;
            hash *= 1099511628211UL;
        }
        return hash;
    }

    /// <summary>
    /// Main thread. Returns the line to log for the newest snapshot, or null. Policy: the first snapshot, any change of
    /// world instance or LUT bake count, a heartbeat every 600 snapshots, or every snapshot when
    /// <see cref="EveryFrame"/>.
    /// </summary>
    internal string? NextLogLine()
    {
        var s = Latest;
        if (s is null || s.Sequence == _loggedSequence) return null;
        var due = EveryFrame || _loggedSequence == 0 || s.WorldInstance != _loggedInstance
            || s.LutBakeCount != _loggedBakeCount || s.Sequence - _loggedHeartbeatAt >= 600;
        if (!due) return null;
        _loggedSequence = s.Sequence;
        _loggedInstance = s.WorldInstance;
        _loggedBakeCount = s.LutBakeCount;
        _loggedHeartbeatAt = s.Sequence;
        return PostDiagSnapshot.LogPrefix + s.ToJson();
    }
}
