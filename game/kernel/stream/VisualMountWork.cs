using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Diagnostics;
using Campus.Kernel.Frame;
using Campus.Kernel.Renderer;
using Godot;

namespace Campus.Kernel.Stream;

internal sealed class VisualMountWork(BakeKey key, IReadOnlyList<PreparedTile> bundles, long revision, Rid scenario,
    BakePriority priority, StreamMaterials materials, IReflectionWorld reflections,
    Action mounted, Action cancelled, Action<Exception> fail, Action<PreparedTile> detach, Func<bool> detached) : IStreamMountWork, IStreamMountCancellationState
{
    // Membership belongs to this request, rather than the residency list that retirement clears and reuses.
    private readonly PreparedTile[] _bundles = bundles.ToArray();
    private readonly PreparedTile.Draw[] _bindings = bundles.SelectMany(bundle => bundle.Draws).ToArray();
    private int _bundle, _draw, _release;
    private bool _released;
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
        if (budgetMilliseconds <= 0) return false;
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
        if (_cancelled) return;
        _cancelled = true;
        foreach (var bundle in _bundles) detach(bundle);
    }
    public bool ReleaseStep(double budgetMilliseconds)
    {
        Cancel();
        if (_released) return true;
        if (!detached()) return false;
        var start = FrameClock.MonotonicMilliseconds;
        if (_release < _bundles.Length)
            do { if (_bundles[_release].ReleaseOne()) _release++; }
            while (_release < _bundles.Length && FrameClock.MonotonicMilliseconds - start < budgetMilliseconds);
        if (_release != _bundles.Length) return false;
        materials?.Forget(_bindings);
        _released = true;
        cancelled();
        return true;
    }
}
