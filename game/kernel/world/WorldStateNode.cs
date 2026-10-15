using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Quality;
using Campus.Contracts.Save;
using Godot;
using AppBoot = Campus.Kernel.Boot.Boot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.World;

/// <summary>The sole frame driver of world time. Weather Step stays inside this ordered step.</summary>
[KernelNode("world-state", Owner = "WP-34")]
[QualityResponsive(Reads = new[] { QualityKnob.FrameCap, QualityKnob.Vrr })]
public partial class WorldStateNode : Node, IMapAware, IQualityAware
{
    private WorldStateService? _world;
    private IViewControl? _view;
    private int _stepRate = 12;
    private ISaveService? _save;

    public override void _Ready()
    {
        ProcessPriority = FramePriority.WorldState;
        _world = (WorldStateService)Services.Get<IWorldState>();
        _view = Services.Get<IViewControl>();
        _stepRate = Services.Get<IQualityService>().Effective.StepRate;
        _save = Services.Get<ISaveService>();
        _world.Settled += RequestSettled;
        WriteGlobals();
    }

    public override void _Process(double delta)
    {
        if (_world is null || _view is null) return;
        _world.SetFrameReadouts(_view.Mode, _stepRate);
        var freezeAnimation = AppBoot.Arguments.ContainsKey("shots") &&
            !AppBoot.Arguments.ContainsKey("cine") && !AppBoot.Arguments.ContainsKey("bench");
        _world.Tick(delta, freezeAnimation);
        WriteGlobals();
    }

    public override void _ExitTree()
    {
        if (_world is not null) _world.Settled -= RequestSettled;
    }

    public void OnMapUnloading(MapId from) => World.OnMapUnloading(from);
    public void ApplyQuality(QualityView view, in QualityChange change) => _stepRate = view.StepRate;
    public void OnMapLoaded(IMapContext context)
    {
        World.OnMapLoaded(context);
        WriteGlobals();
    }

    private WorldStateService World => _world ??= (WorldStateService)Services.Get<IWorldState>();
    private void RequestSettled() => _save?.Request(SaveReason.Settled);

    private void WriteGlobals()
    {
        var world = World;
        RenderingServer.GlobalShaderParameterSet(ShaderGlobals.Clock, (float)world.Clock);
        RenderingServer.GlobalShaderParameterSet(ShaderGlobals.AnimTime, (float)world.AnimTime);
        RenderingServer.GlobalShaderParameterSet(ShaderGlobals.Season, new Vector4((float)world.SeasonPos,
            (float)world.SeasonFrom, (float)world.SeasonTo, (float)world.SeasonProgress));
        RenderingServer.GlobalShaderParameterSet(ShaderGlobals.SunDir, new Vector3(world.Sun.Dir.X, world.Sun.Dir.Y, world.Sun.Dir.Z));
        RenderingServer.GlobalShaderParameterSet(ShaderGlobals.MoonDir, new Vector3(world.Moon.Dir.X, world.Moon.Dir.Y, world.Moon.Dir.Z));
        // Facing the viewer gives the authored full moon; phase does not flash through a calendar during a season change.
        RenderingServer.GlobalShaderParameterSet(ShaderGlobals.MoonLit, new Vector4(0, 0, 1, world.MoonPhase));
    }
}
