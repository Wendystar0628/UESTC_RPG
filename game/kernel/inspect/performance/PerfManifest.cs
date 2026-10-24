using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Campus.Contracts;
using Campus.Contracts.Perf;
using Campus.Contracts.Quality;
using Campus.Contracts.Startup;
using Campus.Kernel.Frame;
using Campus.Kernel.Quality.Settings;
using Godot;
using Environment = System.Environment;

namespace Campus.Kernel.Inspect;

internal static class PerfManifest
{
    // Capture once on the main thread; the writer receives only this detached JSON value.
    public static JsonObject Capture(BootRunOptions options, InputDesktopState desktop, IQualityService quality)
    {
        var screen = DisplayServer.WindowGetCurrentScreen();
        var screenSize = DisplayServer.ScreenGetSize(screen);
        var windowSize = DisplayServer.WindowGetSize();
        var memory = OS.GetMemoryInfo();
        double? ramMiB = memory.TryGetValue("physical", out var physical) ? physical.AsInt64() / 1048576d : null;
        var effective = QualityValues(quality, requested: false);
        var requested = QualityValues(quality, requested: true);
        float renderScale = quality.Effective[QualityKnob.RenderScale];
        int threadModel = ProjectSettings.GetSettingWithOverride("rendering/driver/threads/thread_model").AsInt32();
        float refresh = DisplayServer.ScreenGetRefreshRate(screen);
        var vSync = DisplayServer.WindowGetVsyncMode();
        var pacing = FrameLimitPolicy.Resolve(quality.Effective[QualityKnob.FrameRateLimit],
            vSync != DisplayServer.VSyncMode.Disabled, refresh);
        var root = JsonSerializer.SerializeToNode(new
        {
            build = new
            {
                commit = (string?)null, dirty = (bool?)null,
                configuration = typeof(PerfManifest).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration,
                godot = Engine.GetVersionInfo()["string"].AsString(), dotnet = RuntimeInformation.FrameworkDescription,
                identityMissingReason = "No commit/dirty identity is provided by the existing build artifact."
            },
            machine = CaptureMachine(ramMiB),
            render = new { threadModel, frameQueueCount = (int?)null, swapchainImageCount = (int?)null },
            display = new
            {
                screen, screenCount = DisplayServer.GetScreenCount(), screenWidthPx = screenSize.X,
                screenHeightPx = screenSize.Y, refreshHz = float.IsFinite(refresh) && refresh > 0 ? (float?)refresh : null,
                dpiScale = DisplayServer.ScreenGetScale(screen), windowMode = DisplayServer.WindowGetMode(),
                windowWidthPx = windowSize.X, windowHeightPx = windowSize.Y,
                focused = desktop.WindowFocused, visible = DisplayServer.WindowCanDraw()
            },
            desktop = new
            {
                kind = desktop.IsInputDesktop is { } input ? input ? "interactive" : "isolated" : null,
                desktop.IsInputDesktop, desktop.WindowFocused, source = "IInputRouter.DesktopState (GAME-229)"
            },
            environment = CaptureEnvironment(),
            pacing = new
            {
                vsync = vSync, maxFps = Engine.MaxFps,
                requestedFrameLimit = quality.Effective.Requested(QualityKnob.FrameRateLimit),
                frameLimitReason = Engine.MaxFps == pacing.MaxFps ? pacing.Reason : null,
                reasonSource = "FrameLimitPolicy with current inputs, only when actual cap agrees",
                physicsHz = Engine.PhysicsTicksPerSecond
            },
            graphics = new
            {
                preset = quality.Effective.Preset, origin = (string?)null, requested, effective,
                internalWidthPx = (int)(windowSize.X * renderScale), internalHeightPx = (int)(windowSize.Y * renderScale)
            },
            run = new { args = OS.GetCmdlineUserArgs(), outputDirectory = options.PerfOutputDirectory, diagnostics = (object?)null },
            kernelBudgets = new
            {
                KernelBudgets.StreamMountMilliseconds, KernelBudgets.CollisionMountMilliseconds,
                KernelBudgets.StreamCoveredMountMilliseconds, KernelBudgets.PerfHangWatchdogSeconds,
                KernelBudgets.PerfAuthorRetainedSessions, KernelBudgets.PerfAuthorRetainedBytes
            },
            capabilities = new
            {
                frameStatistics = "recorded", eventContext = "notAttached", slowSampling = "notAttached",
                watchdog = "notAttached", nvml = "notAttached", streamMount = "notAttached", bakeCache = "notAttached"
            },
            missingReasons = new[]
            {
                "Build commit/dirty identity is not attached; missing configuration remains null.",
                "CPU P/E topology, dedicated VRAM and image/queue counts are not exposed by attached authorities; physical RAM is null if OS cannot report it.",
                "Power plan and HAGS capture is not attached. Software lists report fixed-name process presence, not whether an overlay or remote session is active.",
                "Frame-limit reason is null if current policy inputs do not match the actual cap; quality apply origin is not exposed.",
                "Owner command-event coverage is incomplete (eventCoverageMissing); contextComplete=false never certifies steady frames. Hardware producer attachment is reported separately.",
                "Desktop and focus facts remain null when GAME-229 cannot establish them; windowCanDraw is not desktop identity."
            }
        }, PerfFieldDictionary.JsonOptions)!.AsObject();
        return root;
    }

    internal static Dictionary<QualityKnob, float> QualityValues(IQualityService quality, bool requested) =>
        quality.Knobs.ToDictionary(knob => knob.Knob,
            knob => requested ? quality.Effective.Requested(knob.Knob) : quality.Effective[knob.Knob]);

    private static object CaptureMachine(double? ramMiB) => new
            {
                id = Environment.MachineName, label = Environment.MachineName, cpu = OS.GetProcessorName(),
                physicalCores = (int?)null, logicalCores = OS.GetProcessorCount(), ramMiB,
                cpuTopology = (object?)null, gpu = RenderingServer.GetVideoAdapterName(), gpuVendor = RenderingServer.GetVideoAdapterVendor(),
                gpuType = RenderingServer.GetVideoAdapterType(), vramMiB = (double?)null, driver = OS.GetVideoAdapterDriverInfo(),
                api = RenderingServer.GetCurrentRenderingDriverName(), apiVersion = RenderingServer.GetVideoAdapterApiVersion(),
                os = RuntimeInformation.OSDescription
            };

    private static object CaptureEnvironment() => new
    {
        powerPlan = (string?)null, hags = (bool?)null,
        overlaySoftware = FindRunning(["NVIDIA Share", "GameBar", "RTSS", "Discord", "steam", "obs64"]),
        remoteSoftware = FindRunning(["mstsc", "AnyDesk", "TeamViewer", "rustdesk", "parsecd", "sunshine", "Moonlight"])
    };

    private static string[] FindRunning(string[] names)
    {
        var running = new List<string>();
        foreach (string name in names)
        {
            var processes = Process.GetProcessesByName(name);
            if (processes.Length > 0) running.Add(name);
            foreach (var process in processes) process.Dispose();
        }
        return running.ToArray();
    }

    public static void SetSession(JsonObject manifest, PerfFieldDictionary dictionary, string id, PerfOrigin origin,
        DateTimeOffset startedAt)
    {
        manifest["schema"] = dictionary.Schema;
        manifest["id"] = id;
        manifest["origin"] = JsonNamingPolicy.CamelCase.ConvertName(origin.ToString());
        manifest["status"] = "open";
        manifest["complete"] = false;
        manifest["fault"] = "none";
        manifest["startedAt"] = startedAt.ToString("O");
        manifest["endedAt"] = null;
        manifest["endReason"] = null;
        manifest["recordedSeconds"] = 0;
        manifest["fieldDictionary"] = dictionary.Snapshot.DeepClone();
        manifest["clock"] = new JsonObject
        {
            ["monotonicUnit"] = "ms", ["origin"] = "process boot", ["recordingStartMs"] = null,
            ["recordingStopMs"] = null, ["anchors"] = new JsonArray()
        };
    }
}
