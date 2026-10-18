using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Quality;
using Godot;
using Campus.Kernel.Registry;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.World;

/// <summary>The sole frame driver of world time. Weather Step stays inside this ordered step.</summary>
[KernelNode("world-state", Owner = "WP-34")]
[QualityResponsive(Reads = new[] { QualityKnob.FrameCap })]
public partial class WorldStateNode : Node, IMapAware, IQualityAware
{
    private WorldStateService? _world;
    private int _stepRate = WorldTime.DefaultStepRate;

    public override void _Ready()
    {
        ProcessPriority = FramePriority.WorldState;
        _world = (WorldStateService)Services.Get<IWorldState>();
        _stepRate = Services.Get<IQualityService>().Effective.StepRate;
        // Ready completes before the first rendered frame, including shader warm-up.
        RenderingServer.GlobalShaderParameterSet(ShaderGlobals.AnimLoopSeconds, WorldTime.AnimLoopSeconds);
        WriteGlobals();
    }

    public override void _Process(double delta)
    {
        if (_world is null) return;
        _world.SetStepRate(_stepRate);
        _world.Tick(delta, RegistryGateRunner.FreezesAnimation);
        WriteGlobals();
    }

    public void OnMapUnloading(MapId from) => World.OnMapUnloading(from);
    public void ApplyQuality(QualityView view, in QualityChange change) => _stepRate = view.StepRate;
    public void OnMapLoaded(IMapContext context)
    {
        World.OnMapLoaded(context);
        WriteGlobals();
    }

    private WorldStateService World => _world ??= (WorldStateService)Services.Get<IWorldState>();

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
