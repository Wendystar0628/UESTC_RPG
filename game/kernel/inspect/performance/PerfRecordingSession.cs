using System.Text.Json;
using System.Text.Json.Nodes;
using Campus.Contracts;
using Campus.Contracts.Perf;
using Campus.Kernel.Diagnostics;
using Campus.Kernel.Frame;

namespace Campus.Kernel.Inspect;

internal readonly record struct PerfClockAnchor(long UnixMs, double MonotonicMs, double ReadSpanMs);
internal sealed record PerfCapturedEvent(string Name, ulong? Frame, double Time, object Payload,
    PerfClockAnchor? Anchor = null);

/// <summary>One session owns its producers, bounded queues, exclusive directory lock and one IO task.</summary>
internal sealed class PerfRecordingSession
{
    private readonly PerfFieldDictionary _dictionary;
    private readonly JsonObject _manifest;
    private readonly string _root;
    private readonly PerfOrigin _origin;
    private readonly object _failureGate = new();
    private readonly List<Exception> _errors = [];
    private PerfFaultCode _fault;
    private readonly PerfValueChannel _events = new("main");
    private readonly TaskCompletionSource<bool> _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _startedWriting = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _stopped, _recording;
    private PerfEndReason _reason;
    private double _recordingStart, _recordingStop;
    private PerfClockAnchor _firstAnchor;
    private PerfClockAnchor _lastAnchor;
    private long _eventSequence, _writtenEvents, _writtenFrames;
    private readonly List<PerfValueChannel> _producers = [];
    private readonly Dictionary<string, long> _valueRows = new();
    private PerfHangWatchdog? _watchdog;
    private PerfHardwareCapture? _hardware;
    private int _hardwareRevision;
    private bool _captureAttached;
    public PerfFrameRing Frames { get; }
    public string? Id { get; private set; }
    public DateTimeOffset StartedAt { get; }
    public Task<bool> Ready => _ready.Task;
    public Task<bool> StartedWriting => _startedWriting.Task;
    public Task<PerfSessionEndResult> Completion { get; }
    public double RecordingStart => _recordingStart;
    public double RecordedSeconds => Math.Max(0, _recordingStop - _recordingStart) / 1000;
    public PerfFaultCode Fault { get { lock (_failureGate) return _fault; } }
    public Exception? HardwareError => _hardware?.Error;
    private static readonly PerfSamplingFact AwaitingHardware = new()
    { Capability = PerfFactCapability.Starting, Reason = "Awaiting recording hardware sample" };
    public PerfSamplingFacts SamplingFacts => _hardware?.SamplingFacts ??
        new(Id, null, null, AwaitingHardware, AwaitingHardware, AwaitingHardware, AwaitingHardware);
    public double? ProcessCpuFraction => _hardware?.ProcessCpuFraction;

    // Register before Begin: each channel belongs to exactly one producer thread.
    public PerfValueChannel AddProducer(string source, string stream = "events")
    {
        if (Volatile.Read(ref _recording) != 0) throw new InvalidOperationException("Producer registration is closed.");
        var channel = new PerfValueChannel(source, stream);
        _producers.Add(channel);
        return channel;
    }

    public void StartWatchdog(IFrameStatistics frames, PerfValueChannel output) => _watchdog = new(frames, output, this);
    public void StartHardware(RenderAdapterIdentity adapter) => _hardware = new(AddProducer("hardware.slow", "hw"),
        AddProducer("nvml.fast", "hwfast"), _dictionary.HardwareIntervalMilliseconds, Id!, adapter);
    public void AttachCapture() => _captureAttached = true;
    // Capture lifecycle is owned here; freeze its fact with the row before the writer can observe it.
    public bool TryFrame(in FrameSample sample, ReadOnlySpan<ViewportFrameSample> viewports, long contextFlags) =>
        Frames.TryWrite(in sample, viewports, contextFlags, _captureAttached);
    public void StopProducers()
    {
        _watchdog?.Stop();
        _hardware?.RequestStop();
    }
    public async Task DrainProducersAsync()
    {
        if (_watchdog is { } watchdog)
        {
            try { await watchdog.Completion; }
            catch (Exception error) { AddFailure(PerfFaultCode.ProducerFailed, error); }
            finally
            {
                try { watchdog.Release(); }
                catch (Exception error) { AddFailure(PerfFaultCode.ProducerFailed, error); }
                finally { _watchdog = null; }
            }
        }
        if (_hardware is { } hardware) await hardware.StopAsync();
    }

    public PerfRecordingSession(string root, PerfOrigin origin, DateTimeOffset startedAt,
        PerfFieldDictionary dictionary, JsonObject manifest)
    {
        _root = root;
        _origin = origin;
        StartedAt = startedAt;
        _dictionary = dictionary;
        _manifest = manifest;
        Frames = new();
        _producers.Add(_events);
        Completion = Task.Run(WriteAsync);
    }

    public void Begin(PerfClockAnchor anchor)
    {
        FlushEvents();
        _firstAnchor = anchor;
        _lastAnchor = anchor;
        _recordingStart = anchor.MonotonicMs;
        Volatile.Write(ref _recording, 1);
    }

    public bool TryEvent(PerfCapturedEvent item) => _events.TryEvent(item);
    public void FlushEvents() => _events.Flush();

    public void Stop(PerfEndReason reason, double time)
    {
        if (Volatile.Read(ref _stopped) != 0) return;
        if (_hardware is { Joined: false }) throw new InvalidOperationException("Hardware sampling must release and join before value channels close.");
        _reason = reason;
        _recordingStop = Volatile.Read(ref _recording) == 0 ? _recordingStart : time;
        foreach (var producer in _producers) producer.Stop();
        Volatile.Write(ref _stopped, 1);
    }

    public void AddFailure(PerfFaultCode code, [FailureHandler("ENG-PERF-DATA")] Exception error)
    {
        lock (_failureGate)
        {
            if (_fault == PerfFaultCode.None) _fault = code;
            _errors.Add(error);
            _startedWriting.TrySetResult(false);
        }
    }

    private async Task<PerfSessionEndResult> WriteAsync()
    {
        var outputs = new Dictionary<string, StreamWriter>();
        FileStream? sessionLock = null;
        string? directory = null;
        bool closed = false;
        try
        {
            try
            {
                if (_origin == PerfOrigin.Play) PerfSessionStorage.PruneAuthor(_root);
                var opened = PerfSessionStorage.Open(_root, StartedAt, _origin);
                Id = opened.Id;
                directory = opened.Directory;
                sessionLock = opened.Lock;
                OpenOutputs(outputs, directory);
                _ready.SetResult(true);
            }
            catch (Exception error)
            {
                AddFailure(PerfFaultCode.OpenFailed, error);
                _ready.TrySetResult(false);
            }

            while (Volatile.Read(ref _recording) == 0 && Volatile.Read(ref _stopped) == 0 && Fault == PerfFaultCode.None)
                await Task.Delay(10).ConfigureAwait(false);
            if (Fault == PerfFaultCode.None && Volatile.Read(ref _recording) != 0)
            {
                try
                {
                    _manifest["clock"]!["recordingStartMs"] = _recordingStart;
                    _manifest["startedAt"] = DateTimeOffset.FromUnixTimeMilliseconds(_firstAnchor.UnixMs).ToString("O");
                    WriteEvent(outputs["events"], "session.start", null, _recordingStart,
                        new { origin = Camel(_origin), sessionId = Id });
                    WriteAnchor(outputs["events"], _firstAnchor);
                    outputs["events"].Flush();
                    // A root watcher sees only an actually written recording, never a reserved directory.
                    _manifest["status"] = "recording";
                    SetCaptureCapabilities();
                    SetStreams();
                    PerfFieldDictionary.PublishJson(Path.Combine(directory!, "manifest.pending.json"), _manifest);
                    PublishLatest("recording");
                    _startedWriting.TrySetResult(true);
                    await DrainFramesAsync(outputs, directory!).ConfigureAwait(false);
                }
                catch (Exception error) { AddFailure(PerfFaultCode.WriteFailed, error); }
            }

            // The main-thread owner stops and detaches producers even if IO failed first.
            while (Volatile.Read(ref _stopped) == 0) await Task.Delay(10).ConfigureAwait(false);
            if (Fault == PerfFaultCode.None)
            {
                try
                {
                    WriteGap(outputs["events"], "frames", Frames.TailGap);
                    foreach (var producer in _producers) WriteGap(outputs["events"], producer.Stream, producer.TailGap);
                    WriteEvent(outputs["events"], "session.end", null, _recordingStop,
                        new { reason = Camel(_reason), complete = IsComplete, fault = Camel(Fault) });
                }
                catch (Exception error) { AddFailure(PerfFaultCode.WriteFailed, error); }
            }
        }
        catch (Exception error)
        {
            AddFailure(PerfFaultCode.WriteFailed, error);
            _ready.TrySetResult(false);
            while (Volatile.Read(ref _stopped) == 0) await Task.Delay(10).ConfigureAwait(false);
        }
        finally { _startedWriting.TrySetResult(false); closed = CloseOutputs(outputs, directory, sessionLock); }
        Exception? failure;
        lock (_failureGate)
        {
            failure = null;
            foreach (var error in _errors) failure = RetainedFailure.Append(failure, error);
        }
        if (HardwareError is { } hardwareError) failure = RetainedFailure.Append(failure, hardwareError);
        return new(Id ?? "", Fault == PerfFaultCode.None ? _reason : PerfEndReason.Failure, closed,
            IsComplete, Fault, failure, RecordedSeconds, _writtenFrames, Frames.DroppedFrames);
    }

    private void OpenOutputs(Dictionary<string, StreamWriter> outputs, string directory)
    {
        PerfManifest.SetSession(_manifest, _dictionary, Id!, _origin, StartedAt);
        foreach (string name in _dictionary.Columns.Keys)
        {
            var writer = PerfFieldDictionary.OpenText(Path.Combine(directory, name + (name == "events" ? ".jsonl" : ".csv")));
            outputs.Add(name, writer);
            if (name != "events") writer.WriteLine(string.Join(',', _dictionary.Columns[name]));
            writer.Flush();
        }
        SetStreams();
        PerfFieldDictionary.PublishJson(Path.Combine(directory, "manifest.pending.json"), _manifest);
    }

    private bool CloseOutputs(Dictionary<string, StreamWriter> outputs, string? directory, FileStream? sessionLock)
    {
        bool closed = false, resourcesClosed = true;
        foreach (var writer in outputs.Values)
        {
            try { writer.Flush(); }
            catch (Exception error) { AddFailure(PerfFaultCode.FinalizeFailed, error); }
            try { writer.Dispose(); }
            catch (Exception error) { AddFailure(PerfFaultCode.FinalizeFailed, error); resourcesClosed = false; }
        }
        while (Frames.Peek() is not null) Frames.Release();
        foreach (var producer in _producers) while (producer.TryRead(out _)) { }
        if (directory is not null && resourcesClosed)
        {
            try
            {
                FinishManifest();
                PerfFieldDictionary.PublishJson(Path.Combine(directory, "manifest.json"), _manifest);
                closed = true;
                PublishLatest("closed");
                File.Delete(Path.Combine(directory, "manifest.pending.json"));
            }
            catch (Exception error)
            {
                AddFailure(PerfFaultCode.FinalizeFailed, error);
                // A latest-pointer failure does not reopen closed streams. Preserve it in the final manifest.
                if (closed)
                {
                    try { FinishManifest(); PerfFieldDictionary.PublishJson(Path.Combine(directory, "manifest.json"), _manifest); }
                    catch (Exception publishError) { AddFailure(PerfFaultCode.FinalizeFailed, publishError); closed = false; }
                }
            }
        }
        try { sessionLock?.Dispose(); }
        catch (Exception error) { AddFailure(PerfFaultCode.FinalizeFailed, error); closed = false; }
        return closed;
    }

    private async Task DrainFramesAsync(Dictionary<string, StreamWriter> outputs, string directory)
    {
        while (true)
        {
            while (Frames.Peek() is { } slot)
            {
                WriteGap(outputs["events"], "frames", slot.Gap);
                PerfFrameWriter.Write(outputs["frames"], _dictionary, slot);
                _writtenFrames++;
                Frames.Release();
            }
            foreach (var producer in _producers)
                while (producer.TryRead(out var message))
                {
                    WriteGap(outputs["events"], producer.Stream, message!.Gap);
                    if (message.Event is { } item)
                    {
                        if (item.Anchor is { } anchor) WriteAnchor(outputs["events"], anchor);
                        else WriteEvent(outputs["events"], item.Name, item.Frame, item.Time, item.Payload, producer.Source);
                    }
                    if (message.Row is { } row) WriteValue(outputs[row.Stream], row, message.Sequence);
                    if (message.FastBatch is { } batch)
                    {
                        long sequence = message.Sequence;
                        foreach (var fastRow in PerfHardwareCapture.FastRows(batch.Identity, batch.Values))
                            WriteValue(outputs[fastRow.Stream], fastRow, sequence++);
                    }
                }
            if (_hardware is { } hardware && hardware.Revision != _hardwareRevision)
            {
                _hardwareRevision = hardware.Revision;
                SetCaptureCapabilities();
                SetStreams();
                PerfFieldDictionary.PublishJson(Path.Combine(directory, "manifest.pending.json"), _manifest);
            }
            if (Volatile.Read(ref _stopped) != 0 && Frames.Empty && _producers.All(p => p.Complete)) break;
            if (Fault != PerfFaultCode.None) break;
            await Task.Delay(10).ConfigureAwait(false);
        }
    }

    private bool IsComplete => Fault == PerfFaultCode.None && Frames.DroppedFrames == 0 && _producers.All(p => p.Dropped == 0);

    private void WriteValue(StreamWriter writer, PerfCapturedRow row, long sequence)
    {
        writer.WriteLine(string.Join(',', _dictionary.Columns[row.Stream].Select(name => PerfFieldDictionary.Csv(name switch
        {
            "schema" => _dictionary.Schema, "sequence" => sequence,
            "collectionFrame" => row.Frame, "monotonicMs" => row.Time,
            _ => row.Values.TryGetValue(name, out var value) ? CsvValue(value) : null
        }))));
        _valueRows[row.Stream] = _valueRows.GetValueOrDefault(row.Stream) + 1;
    }

    private static object? CsvValue(object? value) => value is null or string or bool or IFormattable
        ? value : JsonSerializer.Serialize(value, PerfFieldDictionary.JsonOptions);

    private void FinishManifest()
    {
        SetCaptureCapabilities();
        _manifest["status"] = "closed";
        _manifest["complete"] = IsComplete;
        _manifest["fault"] = Camel(Fault);
        _manifest["endReason"] = Camel(Fault == PerfFaultCode.None ? _reason : PerfEndReason.Failure);
        _manifest["recordedSeconds"] = RecordedSeconds;
        _manifest["clock"]!["recordingStopMs"] = Volatile.Read(ref _recording) == 0 ? null : JsonValue.Create(_recordingStop);
        if (Volatile.Read(ref _recording) != 0)
            _manifest["endedAt"] = DateTimeOffset.FromUnixTimeMilliseconds(_lastAnchor.UnixMs)
                .AddMilliseconds(_recordingStop - _lastAnchor.MonotonicMs).ToString("O");
        _manifest["counts"] = new JsonObject { ["writtenFrames"] = _writtenFrames, ["droppedFrames"] = Frames.DroppedFrames };
        SetStreams();
    }

    private void SetCaptureCapabilities()
    {
        var hardware = _hardware?.Snapshot();
        _manifest["contextComplete"] = _captureAttached;
        if (_manifest["capabilities"] is JsonObject capabilities)
        {
            capabilities["slowSampling"] = _captureAttached ? "recorded" : "notAttached";
            capabilities["watchdog"] = _captureAttached ? "recorded" : "notAttached";
            capabilities["nvml"] = hardware?["state"]?.DeepClone() ?? JsonValue.Create("notAttached");
            capabilities["samplingFacts"] = hardware?["samplingFacts"]?.DeepClone();
            var rules = _dictionary.Snapshot["nonSteadyRules"]!;
            _manifest["requiredCoverage"] = rules["requiredCoverage"]?.DeepClone();
            _manifest["notAttached"] = rules["notAttached"]?.DeepClone();
            capabilities["eventContext"] = _captureAttached ? "recorded" : "notAttached";
        }
        if (hardware is not null) _manifest["hardware"] = hardware;
    }

    private void PublishLatest(string state)
    {
        if (_origin != PerfOrigin.Play) return;
        PerfFieldDictionary.PublishJson(Path.Combine(_root, "latest.json"), new JsonObject
        {
            ["schema"] = _dictionary.Schema, ["id"] = Id,
            ["manifest"] = Id + (state == "recording" ? "/manifest.pending.json" : "/manifest.json"),
            ["state"] = state, ["startedAt"] = _manifest["startedAt"]?.DeepClone()
        });
    }

    private void SetStreams()
    {
        var streams = new JsonArray();
        foreach (string name in _dictionary.Columns.Keys)
        {
            var spec = _dictionary.Snapshot["streams"]![name]!;
            streams.Add(new JsonObject
            {
                ["file"] = spec["file"]!.DeepClone(), ["kind"] = name == "events" ? "jsonl" : "csv",
                ["clock"] = "monotonicMs", ["rateHz"] = spec["rateHz"]?.DeepClone(),
                ["state"] = name is "hw" or "hwfast" ? _hardware?.StreamState(name) ?? "notAttached" :
                    name is "frames" or "events" || name == "samples" && _captureAttached ? "recorded" : "notAttached",
                ["writtenRows"] = name == "frames" ? _writtenFrames : name == "events" ? _writtenEvents : _valueRows.GetValueOrDefault(name),
                ["droppedRows"] = name == "frames" ? Frames.DroppedFrames :
                    _producers.Where(p => p.Stream == name).Sum(p => p.Dropped)
            });
        }
        _manifest["streams"] = streams;
    }

    private void WriteAnchor(StreamWriter writer, PerfClockAnchor anchor)
    {
        _lastAnchor = anchor;
        var payload = new { anchor.UnixMs, anchor.MonotonicMs, anchor.ReadSpanMs };
        _manifest["clock"]!["anchors"]!.AsArray().Add(JsonSerializer.SerializeToNode(payload, PerfFieldDictionary.JsonOptions));
        WriteEvent(writer, "clock.anchor", null, anchor.MonotonicMs, payload);
    }

    private void WriteGap(StreamWriter writer, string stream, PerfGap gap)
    {
        if (gap.Rows == 0) return;
        if (stream == "events")
        {
            // Writer lifecycle rows share this stream. Assign lost rows their place in its output sequence.
            gap = gap with { FirstSequence = _eventSequence + 1, LastSequence = _eventSequence + gap.Rows };
            _eventSequence += gap.Rows;
        }
        WriteEvent(writer, "record.gap", gap.LastCollectionFrame, gap.ToMs, new
        {
            stream, gap.FirstSequence, gap.LastSequence, gap.Rows, gap.FirstCollectionFrame,
            gap.LastCollectionFrame, gap.FromMs, gap.ToMs
        });
    }

    private void WriteEvent(StreamWriter writer, string name, ulong? frame, double time, object payload, string source = "writer")
    {
        writer.WriteLine(JsonSerializer.Serialize(new
        {
            schema = _dictionary.Schema, sequence = ++_eventSequence, collectionFrame = frame,
            monotonicMs = time, @event = name, source, payload
        }, PerfFieldDictionary.JsonOptions));
        _writtenEvents++;
    }

    private static string Camel<T>(T value) where T : Enum => JsonNamingPolicy.CamelCase.ConvertName(value.ToString());
}
