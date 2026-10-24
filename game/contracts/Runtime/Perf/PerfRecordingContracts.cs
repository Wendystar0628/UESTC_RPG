using Campus.Contracts.Settings;

namespace Campus.Contracts.Perf;

public enum PerfRecordingState : byte { Off, Starting, Recording, Stopping, Faulted }
public enum PerfFaultCode : byte
{
    None, InvalidDictionary, OpenFailed, WriteFailed,
    ProducerFailed, FinalizeFailed
}
public enum PerfOrigin : byte { Play, Ai }
public enum PerfEndReason : byte { Toggle, Exit, Failure }
public enum PerfMarkerPhase : byte { Instant, Begin, End }

public readonly record struct PerfRecordingSnapshot(
    bool Requested, PerfRecordingState State, PerfFaultCode Fault,
    PerfOrigin Origin, string? SessionId, DateTimeOffset? StartedAt,
    double RecordedSeconds);

public sealed record PerfSessionEndResult(
    string SessionId, PerfEndReason Reason, bool Closed,
    bool Complete, PerfFaultCode Fault, Exception? Error,
    double RecordedSeconds, long WrittenFrames, long DroppedFrames);

public readonly record struct PerfMarker(string Id, PerfMarkerPhase Phase);

public interface IPerformanceRecorder
{
    // 主线程读；RecordedSeconds 由服务按单调时钟现算，界面不读系统钟。
    PerfRecordingSnapshot Snapshot { get; }
    // 仅实际状态变化通知；主线程发布，不能被后台 writer 直接调用。
    event Action<PerfRecordingSnapshot>? StateChanged;
    // 完成清理后发布；失败结果保留原 Exception；诊断服务收到同一原异常。
    PerfSessionEndResult? LastEndResult { get; }
    event Action<PerfSessionEndResult>? SessionEnded;
    // 主线程请求；任务由调用方观察。只取消调用方等待，不撤销已交给服务的意图。
    Task<PerfRecordingSnapshot> SetRecordingAsync(
        bool requested, CancellationToken cancellationToken = default);
    // 主线程调用；仅 Recording 接收，使用记录器取得的当前单调时刻/采集帧号。
    // false = 没有被接收（非 Recording 或通道满）；不假称已经落盘。
    bool TryMark(in PerfMarker marker);
}

public static class PerfSettingDefinitions
{
    // 存 settings.json 的 ui 节，只由记录器读写；LabelKey 交界面方配文案。
    public static readonly SettingDefinition<bool> RecordPerformance =
        new("record_performance", false, "graphics.performance.record");
}
