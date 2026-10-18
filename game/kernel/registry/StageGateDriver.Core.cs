using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Kernel.Frame;
using Campus.Kernel.Inspect;
using Godot;
using AppBoot = Campus.Kernel.Boot.Boot;

namespace Campus.Kernel.Registry;

// G5/G20 core lifecycle: Boot -> Title -> new game -> save -> window close, then a second process restores it.
public sealed partial class StageGateDriver
{
    private async Task RunCoreLifecycle(string gate)
    {
        if (DisplayServer.GetName() == "headless")
            throw new InvalidOperationException("Startup/exit coverage requires a real window.");
        var stage = host.Stage;
        var flow = host.Flow;
        var saves = Kernel.Get<ISaveService>();
        var camera = Kernel.Get<ICameraRig>();
        var input = Kernel.Get<IInputRouter>();
        var world = Kernel.Get<IWorldState>();
        var control = Kernel.Get<IWorldControl>();
        var veil = Kernel.Get<IVeilService>();
        foreach (var service in new[] { typeof(ICameraRig), typeof(IObserverState), typeof(IWalkController),
            typeof(ICollisionWorld), typeof(IMapService), typeof(IMapRuntime), typeof(ISaveService), typeof(IAppFlow) })
        {
            var registration = Kernel.Services.Entries.SingleOrDefault(entry => entry.Kind == "service" && entry.Id == service.FullName);
            if (registration is null || registration.IsStub)
                throw new InvalidOperationException("Core lifecycle requires the formal service: " + service.Name);
        }
        // Keep the test freeze through the later close/save sequence without changing the user's saved rate.
        var testFreeze = control.AcquireFlow(FlowPriority.Pause, 0);
        stage.TreeExiting += testFreeze.Dispose;

        if (gate == "save-close")
        {
            if (flow.State != AppState.Title) throw new InvalidOperationException("Boot did not reach Title.");
            GD.Print("CORE_TITLE_READY");
            if (await flow.Enter(new(EnterKind.NewGame, new SlotId("s1"))) != EnterResult.Entered)
                throw new InvalidOperationException("The production flow could not enter a new game.");
            host.CheckActive();
            input.SetInterfaceMode(false);
            if (Input.MouseMode != Input.MouseModeEnum.Captured)
                throw new InvalidOperationException("Playing did not capture input.");
            var before = camera.Pose.Position;
            using (var press = new InputEventKey { PhysicalKeycode = (Key)input.PhysicalKey("move_forward"), Pressed = true })
                Input.ParseInputEvent(press);
            try
            {
                await CoreWait(() => System.Numerics.Vector3.Distance(before, camera.Pose.Position) > .01f,
                    "A real input event did not move the production camera.");
            }
            finally
            {
                using var release = new InputEventKey { PhysicalKeycode = (Key)input.PhysicalKey("move_forward"), Pressed = false };
                Input.ParseInputEvent(release);
            }
            GD.Print("CORE_INPUT_READY");
            await CoreWait(() => camera.Speed < .0001f, "The released movement input remained active.");
            var saved = await saves.SaveNow(SaveReason.Test);
            if (saved.Status != SaveStatus.Saved) throw new InvalidOperationException("Core save failed: " + saved.Error);
            var beforeLoad = CaptureCoreSections();
            using var cover = veil.Cover(VeilReason.Cinematic, 0);
            await cover.Covered;
            await stage.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            var loaded = await saves.Load(new("s1"));
            if (loaded.Status != LoadStatus.Loaded || !CoreSnapshotsMatch(beforeLoad, CaptureCoreSections()))
                throw new InvalidOperationException("Production save/load did not restore the core runtime state.");
            await cover.Reveal(0);
            GD.Print("CORE_ROUNDTRIP_READY");
        }
        else if (flow.State != AppState.Playing || saves.HasPendingPose || saves.Active != new SlotId("s1"))
            throw new InvalidOperationException("The second process did not fully restore its saved world.");

        if (world.Flow.Rate != 0) throw new InvalidOperationException("The core lifecycle lease did not pause the world.");
        var arrival = host.MapRuntime.Arrival ?? throw new InvalidOperationException("Core lifecycle has no map arrival.");
        await CaptureBarrier.WaitForReadyAsync(stage, host.Readiness, arrival, () =>
        {
            host.CheckActive();
            return true;
        }, host.Lifetime, includeCanvas: false);
        var output = AppBoot.Options.Value("out");
        Directory.CreateDirectory(output);
        using (var image = stage.GetViewport().GetTexture().GetImage())
        {
            if (image.IsEmpty() || image.SavePng(Path.Combine(output, gate + ".png")) != Error.Ok)
                throw new IOException("Core lifecycle screenshot failed.");
        }
        var snapshot = CaptureCoreSections();
        var baseline = Path.Combine(output, "save-close-state.json");
        if (gate == "save-close") File.WriteAllText(baseline, snapshot);
        else
        {
            if (!CoreSnapshotsMatch(File.ReadAllText(baseline), snapshot))
                throw new InvalidOperationException("The second process did not restore the core runtime state.");
            GD.Print("CORE_RUNTIME_MATCH");
        }
        var gen = saves.Slots.Single(info => info.Slot == new SlotId("s1")).Meta?.Gen
            ?? throw new InvalidOperationException("Core lifecycle has no readable saved generation.");
        GD.Print("CORE_GENERATION " + gen);
        input.SetInterfaceMode(false);
        if (Input.MouseMode != Input.MouseModeEnum.Captured)
            throw new InvalidOperationException("The closing window must have captured mouse input.");
        var window = (nint)DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle);
        if (window == 0) throw new InvalidOperationException("Core lifecycle requires a native window handle.");
        _awaitingClose = true;
        GD.Print("CORE_READY_CLOSE " + window);
    }

    private static string CaptureCoreSections()
    {
        var map = Kernel.Get<IMapService>();
        var moment = Kernel.Get<IWorldRestore>().GetSaveMoment();
        var entries = Kernel.Services.Entries.Where(entry => entry.Kind == "save")
            .OrderBy(entry => entry.Id, StringComparer.Ordinal).ThenBy(entry => entry.Scope).ToArray();
        if (!new[] { "world", "maps", "weather", "view", "walk" }.All(id => entries.Any(entry => entry.Id == id)))
            throw new InvalidOperationException("Formal core save sections are missing.");
        using var bytes = new MemoryStream();
        using (var writer = new Utf8JsonWriter(bytes))
        {
            writer.WriteStartObject();
            foreach (var entry in entries)
            {
                var attribute = entry.Type.GetCustomAttributes<SaveSectionAttribute>()
                    .Single(item => item.Id == entry.Id && item.Scope == entry.Scope);
                var section = (ISaveSection)Kernel.Services.Create(entry.Type);
                foreach (var sectionMap in attribute.Scope == SaveScope.Global ? [map.Current]
                    : map.Visits.Select(visit => visit.Map).Append(map.Current).Distinct().OrderBy(id => id.Value, StringComparer.Ordinal).ToArray())
                {
                    writer.WritePropertyName(attribute.Scope == SaveScope.Global ? entry.Id : "map:" + sectionMap.Value + "/" + entry.Id);
                    var context = new CaptureContext(SaveReason.Test, sectionMap, false) { Moment = moment };
                    JsonSerializer.Serialize(writer, section.Capture(in context), section.TypeInfo);
                }
            }
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    // Landing may settle any spatial position. Other values, numeric spelling and order remain exact.
    internal static bool CoreSnapshotsMatch(string saved, string restored) =>
        MatchCoreNode(JsonNode.Parse(saved), JsonNode.Parse(restored));

    private static bool MatchCoreNode(JsonNode? saved, JsonNode? restored)
    {
        if (saved is JsonObject before && restored is JsonObject after)
        {
            if (before.Count != after.Count) return false;
            using var properties = after.GetEnumerator();
            foreach (var property in before)
            {
                if (!properties.MoveNext() || property.Key != properties.Current.Key) return false;
                if (property.Key == "position")
                {
                    if (!MatchCorePosition(property.Value as JsonArray, properties.Current.Value as JsonArray)) return false;
                }
                else if (!MatchCoreNode(property.Value, properties.Current.Value)) return false;
            }
            return true;
        }
        if (saved is JsonArray beforeItems && restored is JsonArray afterItems)
            return beforeItems.Count == afterItems.Count
                && beforeItems.Zip(afterItems).All(pair => MatchCoreNode(pair.First, pair.Second));
        return saved?.ToJsonString(ContractJson.Options) == restored?.ToJsonString(ContractJson.Options);
    }

    private static bool MatchCorePosition(JsonArray? saved, JsonArray? restored)
    {
        const double positionTolerance = .02;
        if (saved is null || restored is null || saved.Count != 3 || restored.Count != 3) return false;
        double distanceSquared = 0;
        for (var i = 0; i < 3; i++)
        {
            if (saved[i] is not JsonValue savedValue || restored[i] is not JsonValue restoredValue
                || !savedValue.TryGetValue<double>(out var a) || !restoredValue.TryGetValue<double>(out var b)) return false;
            if (!double.IsFinite(a) || !double.IsFinite(b)) return false;
            var delta = a - b;
            distanceSquared += delta * delta;
        }
        return distanceSquared <= positionTolerance * positionTolerance;
    }

    private async Task CoreWait(Func<bool> ready, string failure)
    {
        // Detect a stuck lifecycle while allowing the workstation to be busy.
        const double timeoutMilliseconds = 30_000;
        var start = FrameClock.MonotonicMilliseconds;
        while (!ready())
        {
            host.CheckActive();
            if (FrameClock.MonotonicMilliseconds - start > timeoutMilliseconds) throw new TimeoutException(failure);
            await host.Stage.ToSignal(host.Stage.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }
}
