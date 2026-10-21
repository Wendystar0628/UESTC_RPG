using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Kernel.Frame;
using Campus.Kernel.Renderer;
using Campus.Kernel.Stream;
using Godot;
using AppBoot = Campus.Kernel.Boot.Boot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Gates;

// G5/G20 core lifecycle: Title -> NewGame -> Walk/Fly/Walk -> Title -> Continue -> window close/save; a second process restores it.
internal sealed class CoreLifecycleDriver(IStageGateHost host)
{
    public async Task<nint> Run(string gate)
    {
        if (DisplayServer.GetName() == "headless")
            throw new InvalidOperationException("Startup/exit coverage requires a real window.");
        var stage = host.Stage;
        var flow = host.Flow;
        var saves = KernelServices.Get<ISaveService>();
        var camera = KernelServices.Get<ICameraRig>();
        var walk = KernelServices.Get<IWalkController>();
        var observer = KernelServices.Get<IObserverState>();
        var input = KernelServices.Get<IInputRouter>();
        var world = KernelServices.Get<IWorldState>();
        var control = KernelServices.Get<IWorldControl>();
        RequireFormalServices();
        // Keep the test freeze through the later close/save sequence without changing the user's saved rate.
        var testFreeze = control.AcquireFlow(FlowPriority.Pause, 0);
        stage.TreeExiting += testFreeze.Dispose;
        if (gate == "save-close") await RoundTrip(flow, saves, camera, walk, observer, input);
        else if (flow.State != AppState.Playing || saves.Active != SaveSlots.Auto[0])
            throw new InvalidOperationException("The second process did not fully restore its saved world.");
        await CaptureAndCheck(gate, stage, world);
        return PrepareWindowClose(stage, saves, input);
    }

    private static void RequireFormalServices()
    {
        foreach (var service in new[] { typeof(ICameraRig), typeof(IViewControl), typeof(ITransitDriver), typeof(IObserverState), typeof(IWalkController),
            typeof(ICollisionWorld), typeof(IMapService), typeof(IMapRuntime), typeof(ISaveService), typeof(IAppFlow) })
        {
            var registration = KernelServices.Services.Entries.SingleOrDefault(entry => entry.Kind == RegistrationKind.Service && entry.Id == service.FullName);
            if (registration is null || registration.IsStub)
                throw new InvalidOperationException("Core lifecycle requires the formal service: " + service.Name);
        }
    }

    private async Task RoundTrip(IAppFlow flow, ISaveService saves, ICameraRig camera,
        IWalkController walk, IObserverState observer, IInputRouter input)
    {
        if (flow.State != AppState.Title) throw new InvalidOperationException("Boot did not reach Title.");
        GD.Print("CORE_TITLE_READY");
        if (await flow.Enter(new(EnterKind.NewGame, SaveSlots.Auto[0])) != EnterResult.Entered)
            throw new InvalidOperationException("The production flow could not enter a new game.");
        host.CheckActive();
        if (flow.State != AppState.Playing || observer.Mode != ViewMode.Walk || !walk.Active)
            throw new InvalidOperationException("New game did not enter Playing in Walk mode.");
        await CoreWait(() => walk.Grounded && walk.CollisionReady && walk.IsSettled,
            "New game did not prepare its standing collision and settle.");
        await ExerciseModes(camera, walk, observer);
        await ExerciseInput(camera, walk, input);
        var saved = await saves.SaveNow(SaveReason.Test);
        if (saved.Status != SaveStatus.Saved) throw new InvalidOperationException("Core save failed: " + saved.Error);
        var beforeLoad = CoreRuntimeSnapshot.Capture();
        await flow.ReturnToTitle();
        host.CheckActive();
        if (flow.State != AppState.Title || Input.MouseMode == Input.MouseModeEnum.Captured)
            throw new InvalidOperationException("Return to title did not release playing input.");
        GD.Print($"CORE_RETURN_TITLE_READY {FrameClock.MonotonicMilliseconds:R}");
        if (await flow.Enter(new(EnterKind.Continue, SaveSlots.Auto[0])) != EnterResult.Entered)
            throw new InvalidOperationException("The production flow could not continue the saved game.");
        host.CheckActive();
        if (flow.State != AppState.Playing || observer.Mode != ViewMode.Walk || !walk.Active ||
            saves.Active != SaveSlots.Auto[0])
            throw new InvalidOperationException("Continue did not restore the saved Walk world.");
        await CoreWait(() => walk.Grounded && walk.CollisionReady && walk.IsSettled,
            "Continue did not prepare its standing collision and settle.");
        if (!CoreRuntimeSnapshot.Matches(beforeLoad, CoreRuntimeSnapshot.Capture()))
            throw new InvalidOperationException("Production return/continue did not restore the core runtime state.");
        GD.Print($"CORE_CONTINUE_READY {FrameClock.MonotonicMilliseconds:R}");
        GD.Print("CORE_ROUNDTRIP_READY");
    }

    private async Task ExerciseModes(ICameraRig camera, IWalkController walk, IObserverState observer)
    {
        // Hold idle through the loading fade; this runs before the first movement input.
        using var idle = camera.LockInput(InputLockReason.AppFlow);
        var eye = walk.EyePose.Position;
        await SwitchMode(ViewMode.Fly, camera, observer, eye);
        if (walk.Active) throw new InvalidOperationException("Fly retained an active walking body.");
        RequireModeArrival(ViewMode.Fly, observer);
        await SwitchMode(ViewMode.Walk, camera, observer);
        await CoreWait(() => walk.Grounded && walk.CollisionReady && walk.IsSettled,
            "Mode return did not prepare standing collision and settle.");
        if (!walk.Active || walk.ExitRequested)
            throw new InvalidOperationException("Mode return did not restore the production walk controller.");
        var arrival = RequireModeArrival(ViewMode.Walk, observer);
        if (!ReferenceEquals(arrival, walk.PlacementArrival) || arrival.Destination.CollisionFeet is not { } feet
            || !SameCorePosition(feet, walk.BodyPose.Position))
            throw new InvalidOperationException("Mode return did not retain its final walking placement arrival.");
        GD.Print("CORE_MODE_ROUNDTRIP_READY");
    }

    private async Task SwitchMode(ViewMode mode, ICameraRig camera, IObserverState observer,
        System.Numerics.Vector3? fixedEye = null)
    {
        var change = KernelServices.Get<IViewControl>().SetMode(mode, ModeSwitchVia.Key);
        var transit = camera.ActiveTransit
            ?? throw new InvalidOperationException("Playing mode switch did not use the production transit driver.");
        await CoreWait(() =>
        {
            if (fixedEye is { } eye && observer.Mode == mode && !SameCorePosition(eye, observer.Eye))
                throw new InvalidOperationException("Idle Walk to Fly moved away from the standing eye position.");
            return change.IsCompleted;
        }, "The production mode switch did not finish.");
        if (await change == ModeSwitchResult.Refused || await transit.Done != TransitResult.Arrived)
            throw new InvalidOperationException("The production mode switch was refused or cancelled.");
        host.CheckActive();
        if (host.Flow.State != AppState.Playing || camera.Mode != mode || observer.Mode != mode
            || camera.ActiveTransit is not null)
            throw new InvalidOperationException("Mode switch did not finish in the requested Playing mode.");
    }

    private IMapArrival RequireModeArrival(ViewMode mode, IObserverState observer)
    {
        var arrival = host.MapRuntime.Arrival;
        if (arrival is null || !arrival.Done.IsCompletedSuccessfully || arrival.Destination.Mode != mode
            || !SameCorePosition(arrival.Destination.Pose.Position, observer.Eye))
            throw new InvalidOperationException("Mode switch did not leave a ready arrival at its actual endpoint.");
        return arrival;
    }

    private static bool SameCorePosition(System.Numerics.Vector3 before, System.Numerics.Vector3 after) =>
        CoreRuntimeSnapshot.Matches(
            JsonSerializer.Serialize(new { position = new[] { before.X, before.Y, before.Z } }),
            JsonSerializer.Serialize(new { position = new[] { after.X, after.Y, after.Z } }));

    private async Task ExerciseInput(ICameraRig camera, IWalkController walk, IInputRouter input)
    {
        input.SetInterfaceMode(false);
        if (Input.MouseMode != Input.MouseModeEnum.Captured)
            throw new InvalidOperationException("Playing did not capture input.");
        var before = walk.BodyPose.Position;
        var forward = Angles.Forward(walk.BodyPose.YawDeg, 0);
        using (var press = new InputEventKey { PhysicalKeycode = (Key)input.PhysicalKey("move_forward"), Pressed = true })
            Input.ParseInputEvent(press);
        try
        {
            await CoreWait(() => System.Numerics.Vector3.Dot(walk.BodyPose.Position - before, forward) > .01f,
                "A real forward input event did not move the production walk body forward.");
        }
        finally
        {
            using var release = new InputEventKey { PhysicalKeycode = (Key)input.PhysicalKey("move_forward"), Pressed = false };
            Input.ParseInputEvent(release);
        }
        var moved = walk.BodyPose.Position - before;
        GD.Print(FormattableString.Invariant($"CORE_INPUT_READY distance={new System.Numerics.Vector2(moved.X, moved.Z).Length():R} forward={System.Numerics.Vector3.Dot(moved, forward):R}"));
        await CoreWait(() => camera.Speed < .0001f && walk.IsSettled,
            "The released movement input remained active.");
    }

    private async Task CaptureAndCheck(string gate, Node stage, IWorldState world)
    {
        if (world.Flow.Rate != 0) throw new InvalidOperationException("The core lifecycle lease did not pause the world.");
        var arrival = host.MapRuntime.Arrival ?? throw new InvalidOperationException("Core lifecycle has no map arrival.");
        await host.Readiness.WaitForReadyAsync(stage, arrival, () =>
        {
            host.CheckActive();
            return true;
        }, host.Lifetime);
        var output = AppBoot.Options.Capture.Output ?? "";
        Directory.CreateDirectory(output);
        var capture = await KernelServices.Get<IGameCapture>().CaptureAsync(
            new(Path.Combine(output, gate + ".png"), HideUi: false), host.Lifetime);
        if (capture.Status != CaptureStatus.Saved) throw new IOException("Core lifecycle screenshot failed: " + capture.Status);
        var snapshot = CoreRuntimeSnapshot.Capture();
        var baseline = Path.Combine(output, "save-close-state.json");
        if (gate == "save-close") File.WriteAllText(baseline, snapshot);
        else
        {
            if (!CoreRuntimeSnapshot.Matches(File.ReadAllText(baseline), snapshot))
                throw new InvalidOperationException("The second process did not restore the core runtime state.");
            GD.Print("CORE_RUNTIME_MATCH");
        }
    }

    private static nint PrepareWindowClose(Node stage, ISaveService saves, IInputRouter input)
    {
        var gen = saves.Slots.Single(info => info.Slot == SaveSlots.Auto[0]).Meta?.Gen
            ?? throw new InvalidOperationException("Core lifecycle has no readable saved generation.");
        GD.Print("CORE_GENERATION " + gen);
        input.SetInterfaceMode(false);
        if (Input.MouseMode != Input.MouseModeEnum.Captured)
            throw new InvalidOperationException("The closing window must have captured mouse input.");
        var window = (nint)DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle);
        if (window == 0) throw new InvalidOperationException("Core lifecycle requires a native window handle.");
        GD.Print("CORE_INSTANCE_PARAMETERS " + JsonSerializer.Serialize(CaptureInstanceParameters(stage)));
        return window;
    }

    private static object CaptureInstanceParameters(Node stage)
    {
        // Godot ed1daf0bf: InstanceUniforms::materials_finish requests one fixed 16-slot block per
        // instance with parameters, shared with global uniforms. This is declared demand, not allocator occupancy.
        const int slotsPerBlock = 16;
        var capacity = Math.Max(4096L,
            ProjectSettings.GetSetting("rendering/limits/global_shader_variables/buffer_size").AsInt64());
        var instances = new HashSet<Rid>();
        var nodes = new HashSet<ulong>();
        var spatialRids = 0;
        var canvasRids = 0;
        var spatialBlocks = 0;
        var canvasBlocks = 0;
        var streamed = PreparedTile.CaptureParameterInstances();
        foreach (var instance in streamed.Instances) AddSpatial(instance);
        Visit(stage.GetTree().Root);
        var blocks = spatialBlocks + canvasBlocks;
        var slots = (long)blocks * slotsPerBlock;
        return new
        {
            Phase = "pre-close",
            NativeActualSlots = (long?)null,
            NativeActualStatus = "Unavailable: native allocator occupancy has no public API",
            CapacitySlots = capacity,
            SlotsPerParameterBlock = slotsPerBlock,
            TrackedDeclaredParameterBlocks = blocks,
            TrackedDeclaredParameterSlots = slots,
            TrackedDeclaredDemandRatio = blocks > 0 ? (double?)slots / capacity : null,
            Coverage = new
            {
                SpatialRids = spatialRids, CanvasRids = canvasRids,
                SpatialParameterBlocks = spatialBlocks, CanvasParameterBlocks = canvasBlocks,
                DetachedRetiringSpatialRids = streamed.Detached,
                Sources = "Live PreparedTile owners (hidden/retiring/reflection); scene-tree geometry and CanvasItems (including instance pools/UI); RenderHost reflection; active/retiring warmup roots; RID-deduplicated",
                Limits = "Current native parameter declarations only; global uniforms, invalid retained parameter blocks, allocator failures/fragmentation and earlier peaks are not counted. Ratio is tracked demand over shared capacity, not native utilization."
            }
        };

        void Visit(Node node)
        {
            if (!GodotObject.IsInstanceValid(node) || !nodes.Add(node.GetInstanceId())) return;
            if (node is GeometryInstance3D geometry) AddSpatial(geometry.GetInstance());
            if (node is CanvasItem canvas && instances.Add(canvas.GetCanvasItem()))
            {
                canvasRids++;
                var parameters = RenderingServer.CanvasItemGetInstanceShaderParameterList(canvas.GetCanvasItem());
                if (parameters.Count > 0) canvasBlocks++;
            }
            if (node is RenderHost renderHost)
                foreach (var instance in renderHost.CaptureReflectionInstances()) AddSpatial(instance);
            var children = node.GetChildren(includeInternal: true);
            foreach (var child in children) Visit(child);
        }
        void AddSpatial(Rid instance)
        {
            if (!instance.IsValid || !instances.Add(instance)) return;
            spatialRids++;
            var parameters = RenderingServer.InstanceGeometryGetShaderParameterList(instance);
            if (parameters.Count > 0) spatialBlocks++;
        }
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
