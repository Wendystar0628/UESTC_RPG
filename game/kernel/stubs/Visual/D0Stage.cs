using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using Campus.Contracts.Quality;
using Campus.Contracts.Save;
using Campus.Kernel.Collision.Stubs;
using Campus.Kernel.Registry;
using Campus.Kernel.Renderer;
using Campus.Kernel.Stubs.AudioUi;
using Campus.Kernel.Stubs.Foundation;
using Campus.Systems.Weather;
using Godot;
using N = System.Numerics;
using AppBoot = Campus.Kernel.Boot.Boot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Stubs.Visual;

/// <summary>D0 composition root. Each real work package replaces its registered fallback service.</summary>
public partial class D0Stage : Node3D
{
    private Camera3D? _camera;
    private ColorRect? _veilRect;
    private Control? _debugPanel;
    private Label? _readout;
    private CameraRigStub? _rig;
    private VeilServiceStub? _veil;
    private bool _ready;
    private bool _finishing;
    private int _frames;
    private N.Vector2 _look;

    public override async void _Ready()
    {
        if (AppBoot.Failed) return;
        try { await Initialize(); }
        catch (Exception error)
        {
            GD.PushError($"D0 startup failed: {error}");
            GetTree().Quit(2);
        }
    }

    private async Task Initialize()
    {
        var registry = Services.Services;
        if (AppBoot.Value("gate") == "registry")
        {
            var report = RegistryGateRunner.Run(registry, ProjectSettings.GetSetting("campus/maps/index").AsString(),
                AppBoot.Value("map"), Read, [typeof(IRuleset), typeof(IRulesetActivation), typeof(PaletteRegistry), typeof(IRenderHost)]);
            GD.Print("G5_D0_REGISTRY_RESULT " + JsonSerializer.Serialize(report, ContractJson.Options));
            GD.Print($"G5 registry D0-scope PASS maps={report.Maps} declarations={report.Declarations}; runtime checks NOT_IMPLEMENTED={report.NotImplemented.Count}");
            GetTree().Quit();
            return;
        }
        var renderer = new D0RenderHost(this);
        registry.Add<IRenderHost>(renderer);
        registry.InstantiateServices();
        var indexBytes = Read(ProjectSettings.GetSetting("campus/maps/index").AsString());
        var index = JsonSerializer.Deserialize<MapIndex>(indexBytes, ContractJson.Options)!;
        var requested = AppBoot.Value("map", index.DefaultMap.Value);
        if (requested != "*" && !index.Maps.Any(m => m.Id.Value == requested))
            throw new InvalidDataException($"Unknown map: {requested}");
        var entry = index.Maps.Single();
        var interiorsBytes = Read(entry.Dir + "interiors.json");
        using var interiorsDocument = JsonDocument.Parse(interiorsBytes);
        var open = interiorsDocument.RootElement.GetProperty("interiors").EnumerateArray()
            .SelectMany(i => i.GetProperty("featureIds").EnumerateArray())
            .Select(i => new FeatureId(i.GetString()!)).ToHashSet();
        var campusBytes = Read(entry.Dir + "campus.json");
        var campus = await Task.Run(() => D0CampusLoader.FromBytes(campusBytes, open));
        var world = (StubWorldModel)registry.Get<IWorldModel>();
        world.Configure(campus);
        var waypointsBytes = Read(entry.Dir + "waypoints.json");
        var waypoints = JsonSerializer.Deserialize<WaypointFile>(waypointsBytes, ContractJson.Options)!.Waypoints;
        var maps = (MapServiceStub)registry.Get<IMapService>();
        maps.LoadFromBytes(indexBytes, Read(entry.Dir + "map.json"), world, campus.Geo, campus.Center,
            campus.Bounds, campus.TileMin, campus.TileMax, "res://bake/" + entry.Id.Value + "/boot/", waypoints);
        var context = maps.Context ?? throw new InvalidDataException("Map context not initialized.");
        InitializeWorld(context);
        var providers = registry.ForMap(entry.Id.Value).Where(r => r.Kind == "interior")
            .ToDictionary(r => r.Id, r => (IInteriorProvider)registry.Create(r.Type), StringComparer.Ordinal);
        var buildContext = new BuildContext { Map = context.Descriptor, World = world, WorldSeed = context.Descriptor.Seed };
        var interiors = await Task.Run(() => D0InteriorSetup.Build(interiorsBytes, buildContext, providers));
        ((InteriorIndexStub)registry.Get<IInteriorIndex>()).Configure(interiors.Entries);
        ((NavGraphStub)registry.Get<INavGraph>()).Configure(interiors.NavNodes, interiors.NavEdges, interiors.NavPortals);
        var colliders = campus.Colliders.Concat(interiors.Colliders).ToArray();
        ((CollisionWorldStub)registry.Get<ICollisionWorld>()).Configure(colliders);
        _rig = (CameraRigStub)registry.Get<ICameraRig>();
        _rig.ConfigureBounds(campus.Bounds);
        var save = (SaveServiceStub)registry.Get<ISaveService>();
        save.BeginNewInstance = _ =>
        {
            if (registry.Get<IWorldState>() is WorldStateStub stub) stub.BeginNewInstance();
            else registry.Get<IWorldRestore>().CompleteRestore();
        };
        foreach (var registration in registry.Entries.Where(r => r.Kind == "save"))
        {
            var instance = registry.Create(registration.Type);
            foreach (var attribute in registration.Type.GetCustomAttributes<SaveSectionAttribute>())
                save.RegisterSection((ISaveSection)instance, attribute);
        }
        // Production atlas initializes through its map-aware node below; only the D0 fallback needs this fixture data.
        if (registry.Get<IAtlasService>() is AtlasServiceStub atlasStub)
            atlasStub.LoadFromBytes(entry.Id, waypointsBytes);
        foreach (var map in index.Maps) registry.ValidateMap(map.Id.Value);
        if (world.Features.Keys.Any(id => new[] { "npc:", "item:", "quest:" }.Any(prefix => id.Value.StartsWith(prefix, StringComparison.Ordinal))))
            throw new InvalidDataException("RPG entity prefixes are reserved in sightseeing data.");
        var priorities = typeof(FramePriority).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral).Select(f => (int)f.GetRawConstantValue()!).ToArray();
        if (priorities.Length != 18 || priorities.Distinct().Count() != priorities.Length ||
            FramePriority.Animate <= FramePriority.Systems || FramePriority.Animate >= FramePriority.Audio)
            throw new InvalidDataException("Frame priority registry is incomplete or ambiguous.");
        var knobs = registry.Get<IQualityService>().Knobs.Select(k => k.Knob).ToArray();
        if (knobs.Distinct().Count() != Enum.GetValues<QualityKnob>().Length)
            throw new InvalidDataException("Quality catalog does not cover every knob.");
        using (var paletteData = JsonDocument.Parse(Read("res://data/d0/palette.json")))
        {
            var tokens = paletteData.RootElement.EnumerateObject().Select(p => p.Name).ToArray();
            foreach (var mesh in campus.Content.Meshes)
            {
                var lit = Array.IndexOf(tokens, mesh.Material.Spec.Lit.Value);
                var shade = Array.IndexOf(tokens, mesh.Material.Spec.Shade.Value);
                if (lit < 0 || shade != lit + 1) throw new InvalidDataException("D0 palette Lit/Shade must be adjacent.");
            }
        }
        var embedded = typeof(ContractJson).Assembly.GetManifestResourceNames();
        if (!embedded.Any(n => n.EndsWith("map.schema.json", StringComparison.Ordinal)))
            throw new InvalidOperationException("Contract schemas missing from assembly.");
        if (AppBoot.Value("gate") == "registry")
        {
            GD.Print($"G5 registry PASS maps={index.Maps.Count} services={registry.Entries.Count(r => r.Kind == "service")} parts={registry.Entries.Count(r => r.Kind == "part")} interiors={providers.Count} schemas={embedded.Length}");
            GetTree().Quit();
            return;
        }
        if (AppBoot.Value("gate") is { Length: > 0 } unknown)
            throw new NotSupportedException($"D0 runtime gate not implemented: {unknown}");
        var palette = new Campus.Style.Stubs.D0MaterialLibrary();
        AddChild(new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color,
                BackgroundColor = palette.ColorOf(new("d0.sky")),
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = palette.ColorOf(new("d0.ambient")),
                AmbientLightEnergy = 0.7f,
                TonemapMode = Godot.Environment.ToneMapper.Linear
            }
        });
        if (!registry.ForMap(entry.Id.Value).Any(r => r.Kind == "node" && r.Id == "key-light" && !r.IsStub))
            AddChild(new DirectionalLight3D { RotationDegrees = new(-5.5f, -61f, 0), LightEnergy = 0.85f, ShadowEnabled = true });
        renderer.Mount(campus.Content, new("WP-00"));
        renderer.Mount(interiors.Content, new("WP-00"));
        D0CollisionMount.Mount(this, colliders);
        _camera = new Camera3D { Current = true, Near = 0.05f, Far = 8000f };
        AddChild(_camera);
        var nodes = new List<Node>();
        foreach (var registration in registry.ForMap(entry.Id.Value).Where(r => r.Kind == "node"))
        {
            var node = registry.Create(registration.Type) as Node
                ?? throw new InvalidOperationException($"KernelNode must inherit Node: {registration.Type}");
            AddChild(node);
            nodes.Add(node);
        }
        foreach (var node in nodes)
        {
            if (node is IMapAware mapAware) mapAware.OnMapLoaded(context);
            if (node is IQualityAware qualityAware)
                qualityAware.ApplyQuality(registry.Get<IQualityService>().Effective, new QualityChange(1, ulong.MaxValue, ApplyOrigin.Startup, true));
        }
        var rect = context.Descriptor.DataRect;
        RenderingServer.GlobalShaderParameterSet(ShaderGlobals.MapRect,
            new Vector4(rect.Min.X, rect.Min.Y, 1f / (rect.Max.X - rect.Min.X), 1f / (rect.Max.Y - rect.Min.Y)));
        ApplyInitialArguments(waypoints);
        BuildDebugUi(palette);
        _veil = (VeilServiceStub)registry.Get<IVeilService>();
        GD.Print($"D0 world ready: {campus.SourceEntityCount} entities / {interiors.Entries.Count} interior entities / {colliders.Length} colliders");
        await ((AppFlowStub)registry.Get<IAppFlow>()).Initialize();
        _ready = true;
    }

    private static byte[] Read(string path)
    {
        var bytes = Godot.FileAccess.GetFileAsBytes(path);
        if (bytes.Length == 0) throw new FileNotFoundException($"Missing required data: {path}");
        return bytes;
    }

    private static void InitializeWorld(IMapContext context)
    {
        var defaults = context.Descriptor.World.Default;
        var clock = double.Parse(AppBoot.Value("clock", defaults.Clock.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);
        var season = Enum.Parse<Season>(AppBoot.Value("season", defaults.Season.ToString()), true);
        var anim = double.Parse(AppBoot.Value("anim", "240"), CultureInfo.InvariantCulture);
        var seed = uint.Parse(AppBoot.Value("seed", context.Descriptor.Seed.ToString(CultureInfo.InvariantCulture)),
            CultureInfo.InvariantCulture);
        var weatherBoot = WeatherBootOptions.Parse(AppBoot.Arguments, seed, season);

        // Freeze supplies this fresh world's initial values before any map-aware node can enter weather.
        // It is not a clock jump or a season transition. The first normal World Step binds the empty
        // weather timeline to this season before applying WeatherBoot (including winter snow).
        Services.Get<IWorldControl>().Freeze(clock, season, anim);
        var weather = Services.Get<IWeatherProcess>();
        weather.Reset(weatherBoot);
        weather.Enter(context.Descriptor.Id, context.Descriptor.World.WeatherProfile, Services.Get<IWorldState>().WorldHours);
    }

    private void ApplyInitialArguments(IReadOnlyList<Waypoint> waypoints)
    {
        if (_rig is null) return;
        var poseId = AppBoot.Value("pose");
        if (poseId.Length > 0)
        {
            var selected = waypoints.FirstOrDefault(w => w.Id == poseId);
            if (selected is not null) _rig.SetPose(selected.Fly);
            else if (poseId != "V1") throw new ArgumentException($"Unknown D0 pose: {poseId}");
        }
        Services.Get<IWorldControl>().SetFlow(int.Parse(AppBoot.Value("flow", "0"), CultureInfo.InvariantCulture));
        if (AppBoot.Value("mode", "fly") == "walk") Services.Get<IViewControl>().SetMode(ViewMode.Walk, ModeSwitchVia.Key).GetAwaiter().GetResult();
    }

    private void BuildDebugUi(Campus.Style.Stubs.D0MaterialLibrary palette)
    {
        if (!Services.Services.Entries.Any(r => r.Kind == "node" && r.Id == "post.veil" && !r.IsStub))
        {
            var veilLayer = new CanvasLayer { Layer = 5 };
            _veilRect = new ColorRect { Color = palette.ColorOf(new("d0.fog")), MouseFilter = Control.MouseFilterEnum.Ignore };
            veilLayer.AddChild(_veilRect); AddChild(veilLayer);
            _veilRect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        }
        var attribution = new CanvasLayer { Layer = 11 }; AddChild(attribution);
        attribution.AddChild(new Label { Text = "© OpenStreetMap contributors", Position = new(24, 1020), Modulate = palette.ColorOf(new("d0.text")) });
        if (AppBoot.Value("ui") != "debug" && Services.Services.Entries.Any(r => r.Kind == "node" && r.Id == "hud" && !r.IsStub))
            return;
        var ui = new CanvasLayer { Layer = 10 }; AddChild(ui);
        var panel = new VBoxContainer { Position = new(24, 24) };
        _debugPanel = panel; ui.AddChild(panel);
        panel.AddChild(new Label { Text = "ELECTRIC YEARS / D0 DEVELOPMENT STAGE" });
        _readout = new Label(); panel.AddChild(_readout);
        AddButton(panel, "+1 hour", () => Services.Get<IWorldControl>().Advance(1));
        AddButton(panel, "Next season", () => Services.Get<IWorldControl>().SetSeason((Season)(((int)Services.Get<IWorldState>().SeasonTarget + 1) % 4)));
        AddButton(panel, "Clear weather", () => Services.Get<IWorldControl>().SetWeather(WeatherKind.Clear));
        AddButton(panel, "Fly / walk readout", ToggleMode);
        AddButton(panel, "Quit", () => _ = CloseAsync());
        panel.AddChild(new Label { Text = "Click scene: mouse capture / Esc: release\nWASD + Space/Shift: move / Ctrl: faster\nD0 only: save disabled, geometry is placeholder." });
        panel.Visible = AppBoot.Value("ui", "1") != "0" &&
            (AppBoot.Value("ui") == "debug" || !Services.Services.Entries.Any(r => r.Kind == "node" && r.Id == "hud" && !r.IsStub));
    }
    private static void AddButton(Node parent, string text, Action action)
    {
        var button = new Button { Text = text };
        button.Pressed += action;
        parent.AddChild(button);
    }
    private void ToggleMode()
    {
        var mode = Services.Get<IViewControl>().Mode switch { ViewMode.Fly => ViewMode.Walk, ViewMode.Walk => ViewMode.Fly, _ => throw new InvalidOperationException() };
        _ = Services.Get<IViewControl>().SetMode(mode, ModeSwitchVia.Ui);
    }
    public override void _Process(double delta)
    {
        _veil?.Advance(delta);
        if (_veilRect is not null && _veil is not null)
        {
            var color = _veilRect.Color; color.A = _veil.Opacity; _veilRect.Color = color;
        }
        if (_camera is null || _rig is null) return;
        if (_ready)
        {
            var move = new N.Vector3(Axis("move_left", "move_right"), Axis("move_down", "move_up"), Axis("move_forward", "move_backward"));
            _rig.IntegrateInput(move, _look, (float)Math.Min(delta, .1), Input.IsActionPressed("sprint"));
            _look = N.Vector2.Zero;
        }
        var pose = _rig.Pose;
        _camera.Position = pose.Position.ToGodot();
        _camera.RotationDegrees = new(pose.PitchDeg, -pose.YawDeg, 0);
        _camera.Fov = pose.FovDeg ?? 60;
        if (_readout is not null)
        {
            var state = Services.Get<IWorldState>();
            _readout.Text = $"{BuildInfo.Build}  |  {state.SeasonTarget} {state.Clock:F2}  |  {state.ViewMode}";
        }
        if (_ready && !_finishing && int.TryParse(AppBoot.Value("frames"), out var limit) && ++_frames >= limit)
        { _finishing = true; _ = FinishRun(); }
    }
    private static float Axis(string negative, string positive) => (Input.IsActionPressed(positive) ? 1 : 0) - (Input.IsActionPressed(negative) ? 1 : 0);
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("menu")) Input.MouseMode = Input.MouseModeEnum.Visible;
        else if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) Input.MouseMode = Input.MouseModeEnum.Captured;
        else if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
            _look += new N.Vector2(motion.ScreenRelative.X, -motion.ScreenRelative.Y) * .15f;
        else if (@event.IsActionPressed("toggle_mode")) ToggleMode();
    }
    private async Task FinishRun()
    {
        try
        {
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            if (AppBoot.Value("shots") is { Length: > 0 } shots)
            {
                var destination = AppBoot.Value("out");
                if (!Path.IsPathFullyQualified(destination)) throw new ArgumentException("--out must be an absolute directory.");
                Directory.CreateDirectory(destination);
                using var image = GetViewport().GetTexture().GetImage();
                var file = Path.Combine(destination, shots.Split(',')[0] + ".png");
                if (image.SavePng(file) != Error.Ok) throw new IOException("Screenshot could not be saved.");
                GD.Print($"D0 screenshot: {file}");
            }
            await CloseAsync();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(2); }
    }
    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest) _ = CloseAsync();
    }
    private async Task CloseAsync()
    {
        await Services.Get<IAppFlow>().Quit();
        if (Services.Get<IAppFlow>().State == AppState.Quitting) GetTree().Quit();
    }
}
