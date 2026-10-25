using System.Text.Json;
using System.Text.Json.Nodes;
using Campus.Contracts.Perf;
using Campus.Contracts.Quality;
using Campus.Contracts.Startup;
using Campus.Kernel.Inspect;
using Campus.Kernel.Quality.Settings;
using Campus.Kernel.Settings;
using GdUnit4;
using Godot;

namespace Campus.Tests;

[TestSuite]
public sealed class PerformanceHardwareRegressionTests
{
    [TestCase, TestCategory(TestReason.Regression), TestCategory("NativeRender"), RequireGodotRuntime]
    public async Task ManifestUsesRealWindowScaleAndTheSharedEnumConverter()
    {
        string root = PerformanceTestSupport.TemporaryDirectory();
        PerfRecordingSession? session = null;
        try
        {
            await using var settings = new SettingsStore(Path.Combine(root, "settings.json"));
            var catalog = new QualityCatalog(File.ReadAllText(RepositoryFixture.FilePath("game", "data", "quality-presets.json")));
            await using var quality = new QualityService(catalog, new GraphicsSettingsStore(settings, catalog));
            var window = DisplayServer.WindowGetSize();
            if (window.X <= 0 || window.Y <= 0) throw new InvalidOperationException("The native window-size trigger is unavailable.");
            var captured = PerfManifest.Capture(new BootRunOptions(), new(true, true), quality);
            if (captured["machine"]!["vramMiB"] is not null)
                throw new InvalidOperationException("The fixture did not start before hardware capacity was attached.");
            var dictionary = new PerfFieldDictionary(File.ReadAllText(RepositoryFixture.FilePath("game", "data", "perf-fields.json")));
            session = new(root, PerfOrigin.Ai, DateTimeOffset.UnixEpoch, dictionary, captured);
            if (!await session.Ready) throw new InvalidOperationException("The real manifest writer did not open.");
            var adapter = await RenderAdapterIdentity.CaptureAsync(CancellationToken.None);
            session.StartHardware(adapter);
            session.Begin(new(0, 0, 0));
            if (!await session.StartedWriting) throw new InvalidOperationException("The real manifest writer did not start.");
            using var hardwareWait = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            while (session.SamplingFacts.WholeGpuUtilFraction.Capability == PerfFactCapability.Starting)
                await Task.Delay(10, hardwareWait.Token);
            session.StopProducers();
            await session.DrainProducersAsync();
            session.Stop(PerfEndReason.Toggle, 1);
            var ended = await session.Completion;
            if (!ended.Closed || ended.Fault != PerfFaultCode.None)
                throw new InvalidOperationException("The real hardware writer did not close.", ended.Error);
            var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(root, ended.SessionId, "manifest.json")))!;
            var hardware = manifest["hardware"]!;
            string? uuid = hardware["samplingFacts"]!["renderGpuUuid"]?.GetValue<string>();
            if (uuid is null || hardware["renderDeviceAssociation"]?.GetValue<string>() != "matched" ||
                manifest["machine"]!["vramMiB"] is null)
                throw new InvalidOperationException("Real rendering-GPU memory capability unavailable: " + manifest["missingReasons"]);
            var device = hardware["devices"]!.AsArray().Single(value => value!["gpuUuid"]?.GetValue<string>() == uuid)!;
            double expected = device["totalMemoryMiB"]!.GetValue<double>();
            if (expected <= 0 || device["fields"]!["totalMemoryMiB"]!["capability"]!.GetValue<string>() != "supported" ||
                manifest["machine"]!["vramMiB"]!.GetValue<double>() != expected ||
                manifest["missingReasons"]!.AsArray().Any(reason => reason!.GetValue<string>().StartsWith("machine.vramMiB: ", StringComparison.Ordinal)))
                throw new InvalidOperationException("The final manifest lost the matched worker's actual NVML total or kept a stale missing reason.");
            float scale = quality.Effective[QualityKnob.RenderScale];
            if (manifest["graphics"]!["internalWidthPx"]!.GetValue<int>() != (int)(window.X * scale) ||
                manifest["graphics"]!["internalHeightPx"]!.GetValue<int>() != (int)(window.Y * scale))
                throw new InvalidOperationException("The manifest lost the real internal resolution.");
            string? expectedPreset = JsonSerializer.SerializeToElement(quality.Effective.Preset, PerfFieldDictionary.JsonOptions).GetString();
            string? expectedVsync = JsonSerializer.SerializeToElement(DisplayServer.WindowGetVsyncMode(), PerfFieldDictionary.JsonOptions).GetString();
            if (manifest["graphics"]!["preset"]!.GetValue<string>() != expectedPreset ||
                manifest["pacing"]!["vsync"]!.GetValue<string>() != expectedVsync)
                throw new InvalidOperationException("Manifest enums bypassed the shared camel-case converter.");
        }
        finally
        {
            if (session is not null)
            {
                session.StopProducers();
                await session.DrainProducersAsync();
                session.Stop(PerfEndReason.Exit, 1);
                await session.Completion;
            }
            Directory.Delete(root, true);
        }
    }
}
