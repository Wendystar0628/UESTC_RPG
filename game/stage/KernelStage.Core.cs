using System.Reflection;
using System.Text;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Kernel.Boot;
using Campus.Kernel.Frame;
using Campus.Kernel.Inspect;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;
using AppBoot = Campus.Kernel.Boot.Boot;

namespace Campus.Stage;

// G5/G20 drive the production composition root; no second scene or service graph is created.
public partial class KernelStage
{
    private double? _coreCloseAt;
    private bool _coreAwaitingClose;

    private void TraceCoreClose(string phase)
    {
        if (_coreCloseAt is { } started)
            GD.Print($"CORE_CLOSE_PHASE {phase} {FrameClock.MonotonicMilliseconds - started:R}");
    }

    private async Task RunCoreLifecycle(string gate)
    {
        if (DisplayServer.GetName() == "headless")
            throw new InvalidOperationException("Startup/exit coverage requires a real window.");
        var saves = Services.Get<ISaveService>();
        var camera = Services.Get<ICameraRig>();
        var input = Services.Get<IInputRouter>();
        var maps = Services.Get<IMapService>();
        var world = Services.Get<IWorldState>();
        var control = Services.Get<IWorldControl>();
        var veil = Services.Get<IVeilService>();
        foreach (var service in new[] { typeof(ICameraRig), typeof(IObserverState), typeof(IWalkController),
            typeof(ICollisionWorld), typeof(IMapService), typeof(IMapRuntime), typeof(ISaveService), typeof(IAppFlow) })
        {
            var registration = Services.Services.Entries.SingleOrDefault(entry => entry.Kind == "service" && entry.Id == service.FullName);
            if (registration is null || registration.IsStub)
                throw new InvalidOperationException("Core lifecycle requires the formal service: " + service.Name);
        }

        if (gate == "save-close")
        {
            if (_flow!.State != AppState.Title) throw new InvalidOperationException("Boot did not reach Title.");
            GD.Print("CORE_TITLE_READY");
            if (await _flow.Enter(new(EnterKind.NewGame, new SlotId("s1"))) != EnterResult.Entered)
                throw new InvalidOperationException("The production flow could not enter a new game.");
            CheckStartupActive();
            control.SetFlow(0);
            input.SetInterfaceMode(false);
            if (Input.MouseMode != Input.MouseModeEnum.Captured)
                throw new InvalidOperationException("Playing did not capture input.");
            var before = camera.Pose.Position;
            using (var press = new InputEventKey { PhysicalKeycode = (Key)input.PhysicalKey("move_forward"), Pressed = true })
                Input.ParseInputEvent(press);
            try
            {
                await CoreWait(() => System.Numerics.Vector3.Distance(before, camera.Pose.Position) > .01f,
                    "A real input event did not move the production camera.", 2000);
            }
            finally
            {
                using var release = new InputEventKey { PhysicalKeycode = (Key)input.PhysicalKey("move_forward"), Pressed = false };
                Input.ParseInputEvent(release);
            }
            GD.Print("CORE_INPUT_READY");
            await CoreWait(() => camera.Speed < .0001f, "The released movement input remained active.", 2000);
            var saved = await saves.SaveNow(SaveReason.Test);
            if (saved.Status != SaveStatus.Saved) throw new InvalidOperationException("Core save failed: " + saved.Error);
            var beforeLoad = CaptureCoreSections();
            await veil.Cover(VeilReason.Cinematic, 0);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            var loaded = await saves.Load(new("s1"));
            if (loaded.Status != LoadStatus.Loaded || beforeLoad != CaptureCoreSections())
                throw new InvalidOperationException("Production save/load changed a persisted field.");
            await veil.Reveal(VeilReason.Cinematic, 0);
            GD.Print("CORE_ROUNDTRIP_READY");
        }
        else if (_flow!.State != AppState.Playing || saves.HasPendingPose || saves.Active != new SlotId("s1"))
            throw new InvalidOperationException("The second process did not fully restore its saved world.");

        if (world.Flow.Rate != 0) throw new InvalidOperationException("The restored core snapshot is not paused.");
        await CaptureBarrier.WaitForReadyAsync(this, _contentReadiness!, () =>
        {
            CheckStartupActive();
            return _mapRuntime!.ResidentReady && _mapRuntime.DestinationReady && _mapRuntime.WarmReady;
        }, _captureLifetime.Token, includeCanvas: false);
        var output = AppBoot.Options.Value("out");
        Directory.CreateDirectory(output);
        using (var image = GetViewport().GetTexture().GetImage())
        {
            if (image.IsEmpty() || image.SavePng(Path.Combine(output, gate + ".png")) != Error.Ok)
                throw new IOException("Core lifecycle screenshot failed.");
        }
        GD.Print("CORE_STATE " + CaptureCoreSections());
        var gen = saves.Slots.Single(info => info.Slot == new SlotId("s1")).Meta?.Gen
            ?? throw new InvalidOperationException("Core lifecycle has no readable saved generation.");
        GD.Print("CORE_GENERATION " + gen);
        input.SetInterfaceMode(false);
        if (Input.MouseMode != Input.MouseModeEnum.Captured)
            throw new InvalidOperationException("The closing window must have captured mouse input.");
        var window = (nint)DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle);
        if (window == 0) throw new InvalidOperationException("Core lifecycle requires a native window handle.");
        _coreAwaitingClose = true;
        GD.Print("CORE_READY_CLOSE " + window);
    }

    private string CaptureCoreSections()
    {
        var map = Services.Get<IMapService>();
        var moment = Services.Get<IWorldRestore>().GetSaveMoment();
        var entries = Services.Services.Entries.Where(entry => entry.Kind == "save")
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
                var section = (ISaveSection)Services.Services.Create(entry.Type);
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

    private async Task CoreWait(Func<bool> ready, string failure, double timeoutMilliseconds)
    {
        var start = FrameClock.MonotonicMilliseconds;
        while (!ready())
        {
            CheckStartupActive();
            if (FrameClock.MonotonicMilliseconds - start > timeoutMilliseconds) throw new TimeoutException(failure);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

}
