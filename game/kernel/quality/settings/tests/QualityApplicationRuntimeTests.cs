using System.Security.Cryptography;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Quality;
using Campus.Contracts.Startup;
using Campus.Kernel.Quality.Settings;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;
using static Campus.Tests.QualityApplicationTestFixture;

[TestSuite]
public sealed class QualityApplicationRuntimeTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    [RequireGodotRuntime]
    public async Task RuntimeExitCancelsFrameQueueAndRejectsNewFrames()
    {
        var runtime = new QualityRuntime();
        try
        {
            var pending = runtime.NextFrame(CancellationToken.None);
            runtime._ExitTree();
            await AssertThrown(pending.WaitAsync(TimeSpan.FromSeconds(5))).ContinueWith(result => { AssertThat(result.Result).OverrideFailureMessage("Runtime shutdown did not cancel its request.").IsNotNull(); result.Result!.IsInstanceOf<OperationCanceledException>(); });
            await AssertThrown(runtime.NextFrame(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5))).ContinueWith(result => { AssertThat(result.Result).OverrideFailureMessage("Runtime shutdown did not cancel its request.").IsNotNull(); result.Result!.IsInstanceOf<OperationCanceledException>(); });
        }
        finally { runtime.Free(); }
    }
}
