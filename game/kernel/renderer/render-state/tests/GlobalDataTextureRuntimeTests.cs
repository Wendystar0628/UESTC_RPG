using Campus.Kernel.Renderer;
using GdUnit4;
using Godot;

namespace Campus.Tests;

[TestSuite]
public sealed class GlobalDataTextureRuntimeTests
{
    [TestCase(Timeout = 10000)]
    [TestCategory(TestReason.Contract)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task LeasesPreserveNativeTexturesAndShutdownDrainsTheirRelease()
    {
        Require(OS.GetThreadCallerId() == OS.GetMainThreadId(), "Run this native test on the Godot main thread.");
        using var image = Image.CreateFromData(1, 1, false, Image.Format.Rgba8, new byte[] { 0, 0, 0, 255 });
        var first = ImageTexture.CreateFromImage(image);
        var replacement = ImageTexture.CreateFromImage(image);
        var firstId = first.GetInstanceId();
        var replacementId = replacement.GetInstanceId();
        var registry = new GlobalDataTextureRegistry();
        IDisposable? firstRegistration = null;
        IDisposable? replacementRegistration = null;
        GlobalDataTextureLease? firstLease = null;
        GlobalDataTextureLease? replacementLease = null;

        try
        {
            firstRegistration = registry.RegisterOwned("palette", first, first.GetRid());
            Require(registry.TryAcquire("palette", out firstLease), "The first texture must be readable.");
            var retainedFirst = firstLease ?? throw new InvalidOperationException("A successful read must return a lease.");
            firstRegistration.Dispose();
            Require(!registry.TryAcquire("palette", out _), "Unregistration must immediately stop new reads.");

            // An actual render-thread round trip lets pending release work run; a pinned native object must survive it.
            await OnRenderThread(() => Require(GodotObject.IsInstanceIdValid(firstId),
                "Unregister disposed a native ImageTexture while an acquired reader still owned it."));

            replacementRegistration = registry.RegisterOwned("palette", replacement, replacement.GetRid());
            Require(registry.TryAcquire("palette", out replacementLease), "The same name must accept a new texture.");
            var retainedReplacement = replacementLease ?? throw new InvalidOperationException("A successful read must return a lease.");
            Require(retainedReplacement.TextureRid.Equals(replacement.GetRid()), "New readers must receive the replacement texture.");
            Require(retainedFirst.TextureRid.Equals(first.GetRid()), "The older reader must retain the original texture.");

            var shutdown = registry.ShutdownAsync();
            Require(!shutdown.IsCompleted, "Shutdown must wait for native textures held by outstanding readers.");
            Require(!registry.TryAcquire("palette", out _), "Shutdown must reject new readers.");
            Require(ReferenceEquals(shutdown, registry.ShutdownAsync()), "Repeated shutdown must await the same drain.");
            using (var rejected = ImageTexture.CreateFromImage(image))
            {
                var rejectedId = rejected.GetInstanceId();
                var registrationRejected = false;
                try { using var unexpected = registry.RegisterOwned("mat_table", rejected, rejected.GetRid()); }
                catch (ObjectDisposedException) { registrationRejected = true; }
                Require(registrationRejected, "Shutdown must reject a new registration.");
                Require(GodotObject.IsInstanceIdValid(rejectedId), "Rejected registration must leave native ownership with its caller.");
            }

            await OnRenderThread(() =>
            {
                try { Require(GodotObject.IsInstanceIdValid(firstId), "Shutdown released the original before its reader finished."); }
                finally { retainedFirst.Dispose(); }
            });
            Require(!shutdown.IsCompleted, "The replacement's outstanding reader must still delay shutdown.");

            await OnRenderThread(() =>
            {
                try { Require(GodotObject.IsInstanceIdValid(replacementId), "Shutdown released the replacement before its reader finished."); }
                finally { retainedReplacement.Dispose(); }
            });
            await shutdown;

            Require(!GodotObject.IsInstanceIdValid(firstId), "The original native ImageTexture must be disposed after its final reader.");
            Require(!GodotObject.IsInstanceIdValid(replacementId), "Shutdown completion must include native replacement disposal.");
        }
        finally
        {
            firstLease?.Dispose();
            replacementLease?.Dispose();
            firstRegistration?.Dispose();
            replacementRegistration?.Dispose();
            await registry.ShutdownAsync();
            // Only failed registrations leave ownership with this test.
            if (firstRegistration is null) first.Dispose();
            if (replacementRegistration is null) replacement.Dispose();
        }
    }

    private static Task OnRenderThread(Action action)
    {
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        RenderingServer.CallOnRenderThread(Callable.From(() =>
        {
            try { action(); completed.TrySetResult(); }
            catch (Exception error) { completed.TrySetException(error); }
        }));
        return completed.Task;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
