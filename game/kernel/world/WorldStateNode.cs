using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Quality;
using Godot;
using Campus.Contracts.Startup;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.World;

/// <summary>The sole frame driver of world time. Weather Step stays inside this ordered step.</summary>
[KernelNode("world-state")]
[QualityResponsive(Reads = new[] { QualityKnob.FrameCap })]
public partial class WorldStateNode : Node, IMapAware, IQualityAware
{
    private IWorldFrame? _world;
    private IWorldTransitionPresentation? _transition;
    private IMotionSettings? _motion;
    private ulong _transitionFrame = ulong.MaxValue;
    private int _stepRate = WorldTime.DefaultStepRate;
    private bool _freezeAnimation;

    public override void _Ready()
    {
        ProcessPriority = FramePriority.WorldState;
        _freezeAnimation = KernelServices.Get<BootRunOptions>().FreezeAnimation;
        _world = KernelServices.Get<IWorldFrame>();
        _transition = KernelServices.Get<IWorldTransitionPresentation>();
        _motion = KernelServices.Get<IMotionSettings>();
        _world.ReducedTransitionRequested += OnReducedTransition;
        _stepRate = KernelServices.Get<IQualityService>().Effective.StepRate;
        // Ready completes before the first rendered frame, including shader warm-up.
        RenderingServer.GlobalShaderParameterSet(ShaderGlobals.AnimLoopSeconds, WorldTime.AnimLoopSeconds);
        WriteGlobals();
    }

    public override void _ExitTree()
    {
        if (_world is not null) _world.ReducedTransitionRequested -= OnReducedTransition;
    }

    private void OnReducedTransition()
    {
        var frame = Engine.GetProcessFrames();
        if (_transitionFrame == frame) return;
        _transitionFrame = frame;
        _transition!.CapturePreviousFrame();
        _ = Crossfade();
    }

    private async Task Crossfade()
    {
        try { await _transition!.Crossfade(.5f); }
        catch (OperationCanceledException) when (!IsInsideTree()) { }
        catch (Exception error) { GD.PushError($"[WP-34] reduced-motion crossfade failed: {error}"); }
    }

    public override void _Process(double delta)
    {
        if (_world is null) return;
        _world.SetStepRate(_stepRate);
        _world.Tick(delta, _freezeAnimation);
        WriteGlobals();
    }

    public void OnMapUnloading(MapId from) => World.OnMapUnloading(from);
    public void ApplyQuality(QualityView view, in QualityChange change) => _stepRate = view.StepRate;
    public void OnMapLoaded(IMapContext context)
    {
        World.OnMapLoaded(context);
        WriteGlobals();
    }

    private IWorldFrame World => _world ??= KernelServices.Get<IWorldFrame>();

    private void WriteGlobals()
    {
        var world = World;
        RenderingServer.GlobalShaderParameterSet(ShaderGlobals.Clock, (float)world.Clock);
        RenderingServer.GlobalShaderParameterSet(ShaderGlobals.AnimTime, (float)world.AnimTime);
        var season = (float)world.SeasonPos;
        RenderingServer.GlobalShaderParameterSet(ShaderGlobals.Season, _motion?.ReduceMotion == true
            ? new Vector4(season, season, season, 1f)
            : new Vector4(season, (float)world.SeasonFrom, (float)world.SeasonTo, (float)world.SeasonProgress));
        RenderingServer.GlobalShaderParameterSet(ShaderGlobals.SeasonAtmosphere, world.SeasonAtmosphere);
        RenderingServer.GlobalShaderParameterSet(ShaderGlobals.SunDir, new Vector3(world.Sun.Dir.X, world.Sun.Dir.Y, world.Sun.Dir.Z));
        RenderingServer.GlobalShaderParameterSet(ShaderGlobals.MoonDir, new Vector3(world.Moon.Dir.X, world.Moon.Dir.Y, world.Moon.Dir.Z));
        // Facing the viewer gives the authored full moon; phase does not flash through a calendar during a season change.
        RenderingServer.GlobalShaderParameterSet(ShaderGlobals.MoonLit, new Vector4(0, 0, 1, world.MoonPhase));
    }
}
