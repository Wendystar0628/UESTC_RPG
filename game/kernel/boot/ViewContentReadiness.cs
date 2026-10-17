using Campus.Contracts;
using Campus.Contracts.Startup;
using Godot;

namespace Campus.Kernel.Boot;

public readonly record struct PipelineCounts(double Surface, double Draw, double Canvas)
{
    public static PipelineCounts Read(bool canvas = true) => new(
        Performance.GetMonitor(Performance.Monitor.PipelineCompilationsSurface),
        Performance.GetMonitor(Performance.Monitor.PipelineCompilationsDraw),
        canvas ? Performance.GetMonitor(Performance.Monitor.PipelineCompilationsCanvas) : 0);
}

/// <summary>Snapshots the completed stage's producers once; polling reads their real mounted/uploaded state.</summary>
public sealed class ViewContentReadiness
{
    private readonly IViewContentReadiness[] _producers;
    private ViewContentReadiness(IViewContentReadiness[] producers) => _producers = producers;

    public static ViewContentReadiness Capture(Node stage) => new(Descendants(stage).OfType<IViewContentReadiness>().ToArray());

    public void PrepareCapture(Pose pose)
    {
        foreach (var producer in _producers.OfType<IViewCapturePreparation>()) producer.PrepareCapture(pose);
    }

    public bool IsReady(Pose pose)
    {
        var ready = true;
        foreach (var producer in _producers)
        {
            if (producer.Error is { } error)
                throw new InvalidOperationException($"Visible content failed ({producer.GetType().Name}): {error}");
            ready &= producer.IsReady(pose);
        }
        return ready;
    }

    public string Describe(Pose pose) => string.Join(',', _producers.Select(producer =>
        producer.GetType().Name + "=" + producer.IsReady(pose)));

    private static IEnumerable<Node> Descendants(Node host)
    {
        yield return host;
        foreach (var child in host.GetChildren())
            foreach (var descendant in Descendants(child)) yield return descendant;
    }
}
