using Campus.Kernel.Boot;
using GdUnit4;
using Godot;

namespace Campus.Tests;

[TestSuite]
public sealed class MapRuntimeFailureTests
{
    [TestCase, TestCategory(TestReason.Regression)]
    [RequireGodotRuntime]
    public void VisibleReadinessPreservesTheProducerException()
    {
        var stage = new Node();
        var producer = new MapRuntimeReadinessProducer();
        try
        {
            stage.AddChild(producer);
            var readiness = ViewContentReadiness.Capture(stage);
            var original = new InvalidDataException("visible producer failed");
            producer.Failure = original;
            try { readiness.IsReady(new(System.Numerics.Vector3.Zero, 0, 0)); throw new InvalidOperationException("Visible failure was ignored."); }
            catch (InvalidDataException actual) when (ReferenceEquals(actual, original)) { }
        }
        finally { stage.Free(); }
    }
}
