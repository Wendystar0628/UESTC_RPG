using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Frame;
using Campus.Kernel.Renderer;
using Godot;

namespace Campus.Kernel.Stream;

internal interface IStreamMountCancellationState
{
    bool IsCancelled { get; }
}

internal sealed class VisualMountWork(IReadOnlyList<PreparedTile> bundles, long revision, Rid scenario,
    BakePriority priority, StreamMaterials materials, IReflectionWorld reflections,
    Action mounted, Action cancelled, Action<string> fail) : IStreamMountWork, IStreamMountCancellationState
{
    private int _bundle, _draw;
    private bool _cancelled;
    public bool IsCancelled => _cancelled;
    public BakeKey Key => bundles[0].Key;
    public BakePriority Priority { get; } = priority;
    public long Revision { get; } = revision;
    public StreamMountKind Kind => StreamMountKind.Visual;
    public bool Step(double budgetMilliseconds)
    {
        if (_cancelled) return true;
        var start = FrameClock.MonotonicMilliseconds;
        try
        {
            do
            {
                if (_bundle >= bundles.Count) { mounted(); return true; }
                var bundle = bundles[_bundle];
                if (_draw >= bundle.Draws.Count) { _bundle++; _draw = 0; continue; }
                var draw = bundle.Draws[_draw];
                var reflectionScenario = reflections.Scenario;
                var reflectionMaterial = reflectionScenario.IsValid ? reflections.ProxyMaterial : null;
                bundle.MountOne(_draw++, scenario, materials.Resolve(draw).GetRid(), reflectionScenario,
                    reflectionMaterial);
            } while (FrameClock.MonotonicMilliseconds - start < budgetMilliseconds);
            return false;
        }
        catch (Exception error) { fail($"Tile mount failed: {Key}: {error.Message}"); Cancel(); return true; }
    }
    public void Cancel()
    {
        Abandon();
        foreach (var bundle in bundles) bundle.Dispose();
    }
    public void Abandon()
    {
        if (_cancelled) return; _cancelled = true; cancelled();
    }
}
