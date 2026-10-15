using Campus.Contracts;
using Campus.Contracts.AppFlow;

namespace Campus.Kernel.Stubs;

/// <summary>The legacy D0Stage owns its window shutdown; the formal stage replaces this composition input.</summary>
[KernelService(typeof(IAppExit), Owner = "WP-00", IsStub = true)]
public sealed class AppExitStub : IAppExit
{
    public Task FinishQuit() => Task.CompletedTask;
}
