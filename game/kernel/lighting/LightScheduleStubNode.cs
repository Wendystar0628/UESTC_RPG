using Campus.Contracts;
using Godot;
using N = System.Numerics;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Lighting;

/// <summary>
/// Stand-in for WP-39's lighting schedule node (same registration id, so WP-39's node replaces it).
/// Evaluates <see cref="LightScheduleFallback"/> each frame in the Channels step and submits (L, Δ).
/// </summary>
[KernelNode("light-schedule", Owner = "WP-03", IsStub = true)]
public partial class LightScheduleStubNode : Node
{
    private readonly float[] _values = new float[LightChannelCatalog.Count];
    private readonly N.Vector2[] _pairs = new N.Vector2[LightChannelCatalog.Count];
    private readonly ChannelDeltaTracker _delta = new();
    private IWorldState? _world;
    private ILightChannelSink? _sink;

    public override void _EnterTree() => ProcessPriority = FramePriority.Channels;

    public override void _Ready()
    {
        _world = Services.Get<IWorldState>();
        _sink = Services.Get<ILightChannelSink>();
        Evaluate(0);
    }

    public override void _Process(double delta) => Evaluate(delta);

    private void Evaluate(double delta)
    {
        if (_world is null || _sink is null) return;
        LightScheduleFallback.Evaluate(new ChannelEvalInput(_world.Clock, _world.Sun.AltDeg, _world.WeatherNow), _values);
        _delta.Track(_values, delta, _world.Instance, _pairs);
        _sink.Submit(_pairs);
    }
}
