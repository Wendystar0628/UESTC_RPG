using System.Text.Json;

namespace Campus.Kernel.Inspect;

internal static class PerfFrameWriter
{
    // The dictionary owns order; these bindings own the translation from the published value contract.
    private static readonly Dictionary<string, Func<PerfFrameSlot, object?>> Values = new()
    {
        ["sequence"] = row => row.Sequence,
        ["collectionFrame"] = row => row.Sample.CollectionFrame,
        ["monotonicMs"] = row => row.Sample.MonotonicMilliseconds,
        ["dtMs"] = row => row.Sample.FrameMilliseconds,
        ["phaseValidity"] = row => (byte)row.Sample.Phases.Validity,
        ["physMs"] = row => row.Sample.Phases.PhysicsMilliseconds,
        ["procMs"] = row => row.Sample.Phases.ProcessMilliseconds,
        ["lateMs"] = row => row.Sample.Phases.LateMilliseconds,
        ["drawMs"] = row => row.Sample.Phases.DrawMilliseconds,
        ["tailMs"] = row => row.Sample.Phases.TailMilliseconds,
        ["physicsSteps"] = row => row.Sample.Phases.PhysicsSteps,
        ["mainCycles"] = row => row.Sample.MainCycles,
        ["drawCycles"] = row => row.Sample.DrawCycles,
        ["observerYawDeg"] = row => row.Sample.ObserverYawDegrees,
        ["cameraPitchDeg"] = row => row.Sample.CameraPitchDegrees,
        ["mainViewportCpuMs"] = row => row.Sample.MainViewport.CpuRenderMilliseconds,
        ["mainViewportGpuMs"] = row => row.Sample.MainViewport.GpuRenderMilliseconds,
        ["frameSetupCpuMs"] = row => row.Sample.FrameSetupCpuMilliseconds,
        ["frameSetupDrawFrame"] = row => row.Sample.FrameSetupDrawFrame,
        ["visibleDrawCalls"] = row => row.Sample.RenderCounts.VisibleDrawCalls,
        ["visiblePrimitives"] = row => row.Sample.RenderCounts.VisiblePrimitives,
        ["shadowDrawCalls"] = row => row.Sample.RenderCounts.ShadowDrawCalls,
        ["shadowPrimitives"] = row => row.Sample.RenderCounts.ShadowPrimitives,
        ["pipelineCanvas"] = row => row.Sample.Pipelines.Canvas,
        ["pipelineMesh"] = row => row.Sample.Pipelines.Mesh,
        ["pipelineSurface"] = row => row.Sample.Pipelines.Surface,
        ["pipelineDraw"] = row => row.Sample.Pipelines.Draw,
        ["pipelineSpecialization"] = row => row.Sample.Pipelines.Specialization,
        ["gcPauseMs"] = row => row.Sample.Gc.PauseMilliseconds,
        ["gcGen0"] = row => row.Sample.Gc.Gen0Collections,
        ["gcGen1"] = row => row.Sample.Gc.Gen1Collections,
        ["gcGen2"] = row => row.Sample.Gc.Gen2Collections,
        ["allocatedBytes"] = row => row.Sample.Gc.AllocatedBytes,
        ["managedHeapMiB"] = row => row.Sample.Gc.ManagedHeapBytes / 1048576d,
        ["jitCompiledMethods"] = row => row.Sample.Jit.CompiledMethods,
        ["jitCompiledIlBytes"] = row => row.Sample.Jit.CompiledIlBytes,
        ["jitCompilationMs"] = row => row.Sample.Jit.CompilationMilliseconds,
        ["appState"] = row => JsonNamingPolicy.CamelCase.ConvertName(row.Sample.Context.State.ToString()),
        ["appActivity"] = row => (int)row.Sample.Context.Activity,
        ["windowFocused"] = row => row.Sample.Context.WindowFocused,
        ["windowCanDraw"] = row => row.Sample.Context.WindowCanDraw,
        ["treePaused"] = row => row.Sample.Context.TreePaused,
        ["contextFlags"] = row => row.ContextFlags,
        ["contextComplete"] = row => row.ContextComplete,
        ["gapBefore"] = row => row.Gap.Rows > 0,
        ["viewportSamplesJson"] = Viewports,
        ["streamRevision"] = row => row.Sample.Stream?.Revision,
        ["pendingStreamRequests"] = row => row.Sample.Stream?.PendingRequests,
        ["pendingStreamBakes"] = row => row.Sample.Stream?.PendingBakes,
        ["pendingStreamPreparations"] = row => row.Sample.Stream?.PendingPreparations,
        ["pendingStreamMounts"] = row => row.Sample.Stream?.PendingMounts,
        ["pendingStreamRetirements"] = row => row.Sample.Stream?.PendingRetirements,
        ["streamMountMs"] = row => row.Sample.Stream?.MountMilliseconds,
        ["streamWatchdogTrips"] = row => row.Sample.Stream?.WatchdogTrips,
        ["streamMountedBytes"] = row => row.Sample.Stream?.MountedBytes,
        ["streamMountedTilesDelta"] = row => row.Sample.Stream?.MountedTilesDelta,
        ["streamMountedTiles"] = row => row.Sample.Stream?.MountedTiles
    };

    public static void ValidateColumns(string[] columns)
    {
        if (columns.Length != Values.Count + 1 || columns.Count(name => name == "schema") != 1 ||
            columns.Any(name => name != "schema" && !Values.ContainsKey(name)))
            throw new InvalidDataException("frames.csv columns do not match the frame contract bindings.");
    }

    public static void Write(StreamWriter writer, PerfFieldDictionary dictionary, PerfFrameSlot row) =>
        writer.WriteLine(string.Join(',', dictionary.Columns["frames"].Select(name =>
            PerfFieldDictionary.Csv(name == "schema" ? dictionary.Schema : Values[name](row)))));

    private static object Viewports(PerfFrameSlot row)
    {
        var values = new object[row.ViewportCount];
        for (int index = 0; index < values.Length; index++)
        {
            var value = row.Viewports[index];
            values[index] = new
            {
                value.ViewportId, value.Role, value.DrewThisFrame, value.ResultUpdated, value.SourceDrawFrame,
                sourceCpuBeginUs = value.SourceCpuBeginMicroseconds, sourceCpuEndUs = value.SourceCpuEndMicroseconds,
                cpuRenderMs = Finite(value.CpuRenderMilliseconds), gpuRenderMs = Finite(value.GpuRenderMilliseconds)
            };
        }
        return JsonSerializer.Serialize(values, PerfFieldDictionary.JsonOptions);
    }

    private static double? Finite(double? value) => value is { } number && double.IsFinite(number) ? number : null;
}
