using System.Reflection;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Kernel.Lod;
using Campus.Kernel.Stream;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class StreamLifecycleTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void ShutdownDrainsTheCatalogAndLateResultsDoNotPublish()
    {
        foreach (var fail in new[] { false, true })
        {
            var director = new StreamDirector();
            try
            {
                var context = Context();
                var work = new TaskCompletionSource<ModuleTileCatalog>();
                BindUnrenderedCatalog(director, context, work.Task);
                director.OnMapUnloading(context.Descriptor.Id);
                if (director.IsUnloaded) throw new InvalidOperationException("Unload did not retain its real pending CPU task.");
                if (fail) work.SetException(new InvalidOperationException("Retired catalog failed."));
                else work.SetResult(ModuleTileCatalog.Build(Build(context), []));
                director._Process(0);
                if (!director.IsUnloaded || director.ResidentReady || director.Error is not null || Field(director, "_catalog") is not null)
                    throw new InvalidOperationException("A retired catalog became active map data or an active-map error.");
                director._ExitTree();
            }
            finally { director.Free(); }
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task ExitDropsPendingCatalogSuccessAndObservesLateFaultWithoutNativeWork()
    {
        foreach (var fail in new[] { false, true })
        {
            var director = new StreamDirector();
            try
            {
                var context = Context();
                var work = new TaskCompletionSource<ModuleTileCatalog>();
                BindUnrenderedCatalog(director, context, work.Task);
                director._ExitTree();
                if (Field(director, "_catalogTask") is not null) throw new InvalidOperationException("Exited node retained catalog publication ownership.");
                if (fail) work.SetException(new InvalidOperationException("Exited catalog failed."));
                else work.SetResult(ModuleTileCatalog.Build(Build(context), []));
                await Task.Yield();
                director._Process(0);
                if (director.ResidentReady || Field(director, "_catalog") is not null)
                    throw new InvalidOperationException("A late result published after the real stream node exited.");
            }
            finally { director.Free(); }
        }
    }

    private static void BindUnrenderedCatalog(StreamDirector director, IMapContext context, Task<ModuleTileCatalog> task)
    {
        // Drive the actual node's CPU lifecycle without constructing a renderer, scene or parallel resident.
        Set(director, "_context", context); Set(director, "_mapGeneration", 1L); Set(director, "_catalogGeneration", 1L);
        Set(director, "_catalogTask", task); Set(director, "_mount", new StreamMountQueue(() => 0));
        Set(director, "_visibility", new TileVisibility(.25f)); Set(director, "_materials", new StreamMaterials(null!, null!));
    }
    private static object? Field(StreamDirector target, string name) =>
        typeof(StreamDirector).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target);
    private static void Set(StreamDirector target, string name, object value) =>
        typeof(StreamDirector).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
    private static IMapContext Context() => new TestMapContext(BackdropInputsTests.Descriptor(), new TestWorldModel(),
        BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>()), "res://fixture/");
    private static BuildContext Build(IMapContext context) => new()
    { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = context.World, Map = context.Descriptor, WorldSeed = 1 };
}
