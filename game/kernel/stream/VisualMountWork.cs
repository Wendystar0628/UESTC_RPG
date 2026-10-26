using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Diagnostics;
using Campus.Kernel.Frame;
using Campus.Kernel.Renderer;
using Godot;

namespace Campus.Kernel.Stream;

internal sealed class VisualMountWork(BakeKey key, IReadOnlyList<PreparedTile> bundles, long revision, Rid scenario,
    BakePriority priority, StreamMaterials materials, IReflectionWorld reflections,
    Action mounted, Action cancelled, Action<Exception> fail) : IStreamMountWork, IStreamMountCancellationState
{
    // Membership belongs to this request, rather than the residency list that retirement clears and reuses.
    private readonly PreparedTile[] _bundles = bundles.ToArray();
    private int _bundle, _draw;
    private bool _initializing = true;
    private bool _cancelled, _complete;
    public bool IsCancelled => _cancelled;
    public BakeKey Key { get; } = key;
    public BakePriority Priority { get; set; } = priority;
    public long Revision { get; } = revision;
    public StreamMountKind Kind => StreamMountKind.Visual;
    public bool Step(double budgetMilliseconds)
    {
        if (_cancelled || _complete) return true;
        try
        {
            var start = FrameClock.MonotonicMilliseconds;
            do
            {
                if (_bundle >= _bundles.Length)
                {
                    if (_initializing) { _initializing = false; _bundle = _draw = 0; continue; }
                    mounted(); _complete = true; return true;
                }
                var bundle = _bundles[_bundle];
                if (_draw >= bundle.Draws.Count) { _bundle++; _draw = 0; continue; }
                if (_initializing)
                {
                    if (bundle.InitializeDrawOne(_draw)) _draw++;
                    continue;
                }
                var draw = bundle.Draws[_draw];
                var reflectionScenario = reflections.Scenario;
                var reflectionMaterial = reflectionScenario.IsValid ? reflections.ProxyMaterial : null;
                bundle.MountOne(_draw++, scenario, materials.Resolve(draw).GetRid(), reflectionScenario,
                    reflectionMaterial);
            } while (FrameClock.MonotonicMilliseconds - start < budgetMilliseconds);
            return false;
        }
        catch (Exception error)
        {
            try { fail(error); }
            catch (Exception reporting)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(RetainedFailure.Append(error, reporting)).Throw();
                throw;
            }
            throw;
        }
    }
    public void Cancel()
    {
        if (_complete || _cancelled) return;
        List<Exception>? failures = null;
        try { Abandon(); }
        catch (Exception error) { (failures ??= []).Add(error); }
        foreach (var bundle in _bundles)
            try { bundle.Dispose(); }
            catch (Exception error) { (failures ??= []).Add(error); }
        if (failures is not null) throw new AggregateException("Visual mount release failed.", failures);
    }
    public void Abandon()
    {
        if (_cancelled) return;
        _cancelled = true;
        if (_complete) foreach (var bundle in _bundles) bundle.BeginRetirement();
        cancelled();
    }
}
