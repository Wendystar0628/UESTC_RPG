using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Threading.Channels;
using Campus.Kernel.Diagnostics;
using Godot;

namespace Campus.Kernel.Inspect;

/// <summary>ENG-PERF-DATA diagnostic only. Native RD timestamps have no built-in session CSV writer.</summary>
internal sealed class PerfGpuStepsCapture
{
    private sealed record Capture(ulong Frame, int Segment, bool Valid, string Reason, string[] Names, ulong[] Cpu, ulong[] Gpu);
    private readonly object _gate = new();
    private readonly Channel<Capture> _rows = Channel.CreateBounded<Capture>(new BoundedChannelOptions(32)
        { SingleReader = true, SingleWriter = true, FullMode = BoundedChannelFullMode.Wait });
    private readonly Callable _read;
    private readonly Task _writer;
    private string[] _names = [];
    private ulong? _lastFrame;
    private int _segment;
    private bool _stopped;
    private bool _renderFailureReported, _writerFailureReported;
    private Exception? _error;

    public static bool Requested => OS.GetCmdlineArgs().Contains("--gpu-profile", StringComparer.Ordinal);

    public PerfGpuStepsCapture(string directory)
    {
        _read = Callable.From(ReadOnRenderThread);
        string path = Path.Combine(directory, "gpusteps.csv");
        _writer = Task.Run(() => WriteAsync(path));
    }

    public void CaptureFrame()
    {
        ThrowIfFailed();
        RenderingServer.CallOnRenderThread(_read);
    }

    private void ReadOnRenderThread()
    {
        lock (_gate)
        {
            if (_stopped || _error is not null) return;
            try
            {
                var device = RenderingServer.GetRenderingDevice() ?? throw new NotSupportedException("GPU steps require RenderingDevice.");
                ulong frame = device.GetCapturedTimestampsFrame();
                if (_lastFrame == frame) return;
                _lastFrame = frame;
                int count = checked((int)device.GetCapturedTimestampsCount());
                var names = new string[count];
                var cpu = new ulong[count];
                var gpu = new ulong[count];
                bool same = count > 1 && count == _names.Length;
                bool monotonic = true;
                for (uint i = 0; i < count; i++)
                {
                    names[i] = device.GetCapturedTimestampName(i);
                    cpu[i] = device.GetCapturedTimestampCpuTime(i);
                    gpu[i] = device.GetCapturedTimestampGpuTime(i);
                    same &= i < _names.Length && names[i] == _names[i];
                    if (i > 0 && (cpu[i] < cpu[i - 1] || gpu[i] < gpu[i - 1])) monotonic = false;
                }
                if (!same) { _segment++; _names = names; }
                // Re-read every name before reusing a segment's structure. Its first frame is invalid.
                bool valid = same && monotonic;
                string reason = count < 2 ? "no-timestamp-pairs" : !same ? "structure-changed-recheck" : !monotonic ? "timestamp-order-invalid" : "";
                if (!_rows.Writer.TryWrite(new(frame, _segment, valid, reason, same ? _names : names, cpu, gpu)))
                    throw new IOException("GPU diagnostic writer queue overflow; capture is incomplete.");
            }
            catch (Exception error) { RetainFailure(error); }
        }
    }

    // The main-thread owner receives the original render-callback failure at its next frame or drain.
    private void RetainFailure([Campus.Contracts.FailureHandler("ENG-PERF-DATA")] Exception error) => _error = error;

    private async Task WriteAsync(string path)
    {
        FileStream? stream = null;
        StreamWriter? writer = null;
        Exception? failure = null;
        try
        {
            stream = new FileStream(path, FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.Read, 65536, true);
            writer = new StreamWriter(stream);
            await WriteRowsAsync(writer).ConfigureAwait(false);
        }
        catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        finally
        {
            try
            {
                if (writer is not null) await writer.DisposeAsync().ConfigureAwait(false);
                else if (stream is not null) await stream.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private async Task WriteRowsAsync(StreamWriter writer)
    {
        await writer.WriteLineAsync("capturedTimestampsFrame,structureSegment,valid,reason,index,name,cpuTimestampUs,gpuTimestampNs,cpuFromFirstMs,gpuFromFirstMs,cpuSincePreviousMarkerMs,gpuSincePreviousMarkerMs");
        await foreach (var capture in _rows.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            if (capture.Names.Length == 0)
                await writer.WriteLineAsync($"{capture.Frame},{capture.Segment},false,{capture.Reason},null,null,null,null,null,null,null,null");
            for (int i = 0; i < capture.Names.Length; i++)
            {
                // Godot 4.7.2 RD uses ticks_usec on CPU; D3D12/Vulkan drivers return GPU nanoseconds.
                // Consecutive marker intervals are not named-pass durations (markers may be nested).
                string cpu = capture.Valid ? Ms(capture.Cpu[i] - capture.Cpu[0], 1000d) : "null";
                string gpu = capture.Valid ? Ms(capture.Gpu[i] - capture.Gpu[0], 1000000d) : "null";
                string dcpu = capture.Valid && i > 0 ? Ms(capture.Cpu[i] - capture.Cpu[i - 1], 1000d) : "null";
                string dgpu = capture.Valid && i > 0 ? Ms(capture.Gpu[i] - capture.Gpu[i - 1], 1000000d) : "null";
                await writer.WriteLineAsync($"{capture.Frame},{capture.Segment},{capture.Valid.ToString().ToLowerInvariant()},{capture.Reason},{i},{PerfFieldDictionary.Csv(capture.Names[i])},{capture.Cpu[i]},{capture.Gpu[i]},{cpu},{gpu},{dcpu},{dgpu}");
            }
        }
    }

    private static string Ms(ulong raw, double divisor) => (raw / divisor).ToString("R", CultureInfo.InvariantCulture);
    private void ThrowIfFailed()
    {
        var failure = TakeNewFailure(_writer.IsFaulted ? _writer.Exception : null);
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    // Both entry points hand failures to the same session owner; drain never re-delivers a frame-owned cause.
    private Exception? TakeNewFailure([Campus.Contracts.FailureHandler("ENG-PERF-DATA")] Exception? writerFailure)
    {
        lock (_gate)
        {
            Exception? failure = null;
            if (!_renderFailureReported && _error is { } renderFailure)
            {
                failure = renderFailure;
                _renderFailureReported = true;
            }
            if (!_writerFailureReported && writerFailure is not null)
            {
                failure = RetainedFailure.Append(failure, writerFailure);
                _writerFailureReported = true;
            }
            return failure;
        }
    }

    public void RequestStop()
    {
        lock (_gate) { _stopped = true; _rows.Writer.TryComplete(); }
    }

    public async Task DrainAsync()
    {
        RequestStop();
        Exception? writerFailure = null;
        try { await _writer.ConfigureAwait(false); }
        catch (Exception error) { writerFailure = RetainedFailure.Append(writerFailure, error); }
        var failure = TakeNewFailure(writerFailure);
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
