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
    // Capture early application facts on the main thread; AI sample conditions remain absent until Begin.
    public static JsonObject Capture(BootRunOptions options, InputDesktopState desktop, IQualityService quality,
        RenderDimensions? dimensions)
    {
        var memory = OS.GetMemoryInfo();
        double? ramMiB = memory.TryGetValue("physical", out var physical) ? physical.AsInt64() / 1048576d : null;
        int threadModel = ProjectSettings.GetSettingWithOverride("rendering/driver/threads/thread_model").AsInt32();
        var root = JsonSerializer.SerializeToNode(new
        {
            build = CaptureBuild(),
            machine = CaptureMachine(ramMiB),
            render = new { threadModel, frameQueueCount = (int?)null, swapchainImageCount = (int?)null },
            desktop = new
            {
                kind = desktop.IsInputDesktop is { } input ? input ? "interactive" : "isolated" : null,
                desktop.IsInputDesktop, desktop.WindowFocused, source = "IInputRouter.DesktopState (GAME-229)"
            },
            environment = CaptureEnvironment(),
            run = new { args = OS.GetCmdlineUserArgs(), outputDirectory = options.PerfOutputDirectory },
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
                "CPU P/E topology and image/queue counts are not exposed by attached authorities; physical RAM is null if OS cannot report it. Rendering-adapter association is established separately by the hardware worker using actual D3D12 LUID and a unique PCI hardware-ID match.",
                "Power plan and HAGS capture is not attached. Software lists report fixed-name process presence, not whether an overlay or remote session is active.",
                "Frame-limit reason is null if current policy inputs do not match the actual cap; quality apply origin is not exposed.",
                "Required command-event coverage and optional notAttached sources are reported separately; contextComplete=false never certifies steady frames. Each hardware sampling fact reports its actual independent capability and timing.",
                "Desktop and focus facts remain null when GAME-229 cannot establish them; windowCanDraw is not desktop identity."
            }
        }, PerfFieldDictionary.JsonOptions)!.AsObject();
        root["samplingConditions"] = null;
        if (options.PerfCollection is not null)
        {
            root["display"] = root["graphics"] = root["pacing"] = null;
            root["conditionsMissingReason"] = "The AI collection has not accepted its sample Begin.";
        }
        else SetConditions(root, CaptureConditions(desktop, quality, dimensions));
        if (IncompleteReason(root) is { } missing) root["missingReasons"]!.AsArray().Add(missing);
        return root;
    }

    internal static JsonObject CaptureConditions(InputDesktopState desktop, IQualityService quality,
        RenderDimensions? dimensions, string? segment = null, ulong frame = 0, double time = 0)
    {
        int screen = DisplayServer.WindowGetCurrentScreen();
        var screenSize = DisplayServer.ScreenGetSize(screen);
        var windowSize = DisplayServer.WindowGetSize();
        float refresh = DisplayServer.ScreenGetRefreshRate(screen);
        var vSync = DisplayServer.WindowGetVsyncMode();
        var pacing = FrameLimitPolicy.Resolve(quality.Effective[QualityKnob.FrameRateLimit],
            vSync != DisplayServer.VSyncMode.Disabled, refresh);
        string? missing = dimensions is not { } actual ? "The main rendering owner has not published a live draw."
            : actual.TargetSizePx.X <= 0 || actual.TargetSizePx.Y <= 0 || actual.InternalSizePx.X <= 0 || actual.InternalSizePx.Y <= 0
                ? "The main rendering owner has not published positive target/internal dimensions."
            : actual.TargetSizePx != windowSize ? "The main render target does not match the current client size."
            : null;
        var result = JsonSerializer.SerializeToNode(new
        {
            display = new
            {
                screen, screenCount = DisplayServer.GetScreenCount(), screenWidthPx = screenSize.X,
                screenHeightPx = screenSize.Y, refreshHz = float.IsFinite(refresh) && refresh > 0 ? (float?)refresh : null,
                dpiScale = DisplayServer.ScreenGetScale(screen), windowMode = DisplayServer.WindowGetMode(),
                windowWidthPx = windowSize.X, windowHeightPx = windowSize.Y,
                focused = desktop.WindowFocused, visible = DisplayServer.WindowCanDraw()
            },
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
                preset = quality.Effective.Preset, origin = (string?)null,
                requested = QualityValues(quality, requested: true), effective = QualityValues(quality, requested: false),
                targetWidthPx = dimensions?.TargetSizePx.X, targetHeightPx = dimensions?.TargetSizePx.Y,
                internalWidthPx = dimensions?.InternalSizePx.X, internalHeightPx = dimensions?.InternalSizePx.Y
            }
        }, PerfFieldDictionary.JsonOptions)!.AsObject();
        result["conditionsMissingReason"] = segment is null ? null : missing;
        result["samplingConditions"] = segment is not null && missing is null ? JsonSerializer.SerializeToNode(new
        { segmentId = segment, collectionFrame = frame, monotonicMs = time, renderDrawFrame = dimensions!.Value.DrawFrame },
            PerfFieldDictionary.JsonOptions) : null;
        return result;
    }

    internal static void SetConditions(JsonObject manifest, JsonObject conditions)
    {
        foreach (string key in new[] { "display", "graphics", "pacing", "samplingConditions", "conditionsMissingReason" })
            manifest[key] = conditions[key]?.DeepClone();
        if (manifest["missingReasons"] is JsonArray reasons)
            for (int index = reasons.Count - 1; index >= 0; index--)
                if (reasons[index]?.GetValue<string>().StartsWith("samplingConditions: ", StringComparison.Ordinal) == true)
                    reasons.RemoveAt(index);
    }

    internal static string? IncompleteReason(JsonObject manifest)
    {
        var build = manifest["build"];
        if (build?["identityMissingReason"] is { } identityReason) return "build: " + identityReason.GetValue<string>();
        if (build?["commit"] is not JsonValue commit || !commit.TryGetValue<string>(out var revision) || string.IsNullOrWhiteSpace(revision)
            || build["dirty"] is not JsonValue dirty || !dirty.TryGetValue<bool>(out _)
            || build["moduleVersionId"] is not JsonValue module || !module.TryGetValue<string>(out var id) || !Guid.TryParse(id, out _))
            return "build: The accompanying assembly has no complete commit/dirty/module identity.";
        return manifest["conditionsMissingReason"] is { } reason ? "samplingConditions: " + reason.GetValue<string>() : null;
    }

    private static JsonObject CaptureBuild()
    {
        var assembly = typeof(PerfManifest).Assembly;
        var result = new JsonObject
        {
            ["moduleVersionId"] = assembly.ManifestModule.ModuleVersionId.ToString("D"),
            ["configuration"] = assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration,
            ["godot"] = Engine.GetVersionInfo()["string"].AsString(), ["dotnet"] = RuntimeInformation.FrameworkDescription
        };
        try
        {
            // DECISION-302: Godot dynamically loads the module without Assembly.Location.
            // Read its formal project artifact's sidecar and bind it to the actual loaded MVID.
            string configuration = result["configuration"]?.GetValue<string>()
                ?? throw new InvalidDataException("The loaded assembly has no build configuration.");
            string path = ProjectSettings.GlobalizePath($"res://.godot/mono/temp/bin/{configuration}/{assembly.GetName().Name}.dll.identity.json");
            var identity = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
            if (identity["moduleVersionId"]?.GetValue<string>() != result["moduleVersionId"]!.GetValue<string>())
                throw new InvalidDataException("The build identity belongs to a different assembly module.");
            if (identity["identityMissingReason"] is { } missing)
                result["identityMissingReason"] = missing.DeepClone();
            else
            {
                string commit = identity["commit"]!.GetValue<string>();
                if (string.IsNullOrWhiteSpace(commit)) throw new InvalidDataException("The build has no commit identity.");
                result["dirty"] = identity["dirty"]!.GetValue<bool>();
                result["commit"] = commit;
            }
        }
        catch (Exception error) { MissingBuild(result, error); }
        return result;
    }

    private static void MissingBuild(JsonObject build, [FailureHandler("ENG-PERF-DATA")] Exception error)
    {
        build.Remove("commit");
        build.Remove("dirty");
        build["identityMissingReason"] = error.ToString();
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
        manifest["missingReasons"] ??= new JsonArray();
        manifest["fieldDictionary"] = dictionary.Snapshot.DeepClone();
        manifest["clock"] = new JsonObject
        {
            ["monotonicUnit"] = "ms", ["origin"] = "process boot", ["recordingStartMs"] = null,
            ["recordingStopMs"] = null, ["anchors"] = new JsonArray()
        };
    }
}
