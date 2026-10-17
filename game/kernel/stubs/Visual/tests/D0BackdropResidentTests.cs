using Campus.Contracts;
using Campus.Kernel.Stubs.Foundation;
using Campus.Kernel.Stubs.Visual;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class D0BackdropResidentTests
{
    [TestCase]
    public async Task OffOriginMapBuildsOneWholeMapL4AndDuplicateLoadDoesNotRemount()
    {
        var renderer = new Renderer();
        var module = new Module();
        var queue = new WorkQueue();
        using var resident = new D0BackdropResident(renderer, _ => throw new InvalidOperationException(), queue.Run);
        var context = Context();
        var pending = resident.Load(context, module, new("WP-22"));
        Check(ReferenceEquals(pending, resident.Load(context, module, new("WP-22"))), "Duplicate load launched new work.");
        Check(!resident.Ready && renderer.Mounts == 0, "Resident was declared ready before Build.");
        queue.Complete(0);
        await pending;
        Check(module.Calls == 1 && renderer.Mounts == 1 && renderer.Owner == new WpId("WP-22") && resident.Ready,
            "Whole-map output was not mounted exactly once under its owner.");
        Check(module.Tile == new TileKey(0, 0) && module.Lod == LodLevel.L4 && module.TileQueries == 0,
            "The L0 discovery cell was used as the L4 product key.");
        resident.Unload();
        Check(renderer.Unmounts == 1 && !resident.Ready, "Unloading did not release the resident.");
    }

    [TestCase]
    public async Task SameIdReentryDropsLateCompletionAndObservesLateFaults()
    {
        var renderer = new Renderer();
        var queue = new WorkQueue();
        var discarded = new List<Exception>();
        using var resident = new D0BackdropResident(renderer, discarded.Add, queue.Run);
        var old = resident.Load(Context(), new Module(), new("WP-22"));
        resident.Unload();
        var current = resident.Load(Context(), new Module(), new("WP-22"));
        queue.Complete(1);
        await current;
        queue.Complete(0);
        await old;
        Check(renderer.Mounts == 1 && resident.Ready, "A late same-ID epoch mounted into the new map.");
        var lateFault = resident.Load(Context(), new Module(), new("WP-22"));
        resident.Unload();
        queue.Fail(2);
        await lateFault;
        Check(discarded.Count == 1 && renderer.Unmounts == 1 && !resident.Ready, "A discarded worker fault was lost.");
    }

    [TestCase]
    public async Task ExitDropsLateSuccessAndReportsLateFaultWithoutMounting()
    {
        var renderer = new Renderer();
        var queue = new WorkQueue();
        var discarded = new List<Exception>();
        var resident = new D0BackdropResident(renderer, discarded.Add, queue.Run);
        var success = resident.Load(Context(), new Module(), new("WP-22"));
        var fault = resident.Load(Context(), new Module(), new("WP-22"));
        resident.Dispose();
        queue.Complete(0);
        queue.Fail(1);
        await Task.WhenAll(success, fault);
        Check(renderer.Mounts == 0 && discarded.Count == 1 && !resident.Ready, "Exited resident accepted a late result.");
    }

    [TestCase]
    public async Task ActiveBuildAndMountFailuresRemainFailuresAndReleaseOldMap()
    {
        var renderer = new Renderer();
        var queue = new WorkQueue();
        using var resident = new D0BackdropResident(renderer, _ => throw new InvalidOperationException(), queue.Run);
        var first = resident.Load(Context(), new Module(), new("WP-22"));
        queue.Complete(0);
        await first;
        var failure = resident.Load(Context(), new Module(), new("WP-22"));
        queue.Fail(1);
        await Fails(failure);
        Check(renderer.Unmounts == 1 && !resident.Ready, "Build failure retained old resident resources or reported ready.");
        renderer.FailMount = true;
        var mountFailure = resident.Load(Context(), new Module(), new("WP-22"));
        queue.Complete(2);
        await Fails(mountFailure);
        Check(!resident.Ready, "Mount failure was treated as success.");
    }

    [TestCase]
    public async Task DefaultExecutorRunsBuildOnWorkerAndEmptyOutputDoesNotMount()
    {
        var renderer = new Renderer();
        var module = new Module { Output = ContentOutput.Empty };
        using var resident = new D0BackdropResident(renderer, _ => throw new InvalidOperationException());
        await resident.Load(Context(), module, new("WP-22"));
        Check(module.WorkerThread && renderer.Mounts == 0 && resident.Ready, "Empty/absent backdrop did not finish honestly on a worker.");
    }

    private static BuildContext Context()
    {
        var world = new StubWorldModel(new StubSpatialIndex(), new Campus.Kernel.Stubs.InteriorIndexStub(), new StubFidelityModel());
        return new() { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, Map = BackdropInputsTests.Descriptor(), WorldSeed = 1 };
    }
    private static async Task Fails(Task task)
    {
        try { await task; }
        catch (InvalidOperationException) { return; }
        throw new Exception("Expected the active resident task to propagate its failure.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    private sealed class Module : IContentModule
    {
        public int Calls { get; private set; }
        public int TileQueries { get; private set; }
        public TileKey Tile { get; private set; }
        public LodLevel Lod { get; private set; }
        public bool WorkerThread { get; private set; }
        public ContentOutput Output { get; init; } = new(
            [new(new(new(new("wall.lit"), new("wall.shade"))), [0, 0, 0, 1, 0, 0, 0, 0, 1], [0, 1, 0, 0, 1, 0, 0, 1, 0], [0, 1, 2])], []);
        public IReadOnlyList<Claim> Claims => [];
        public ModuleBudget Budget => new(0, 0, 0);
        public IEnumerable<TileKey> Tiles(BuildContext context) { ++TileQueries; return [new(79, -108)]; }
        public ContentOutput Build(BuildContext context, TileKey tile, LodLevel lod)
        { ++Calls; Tile = tile; Lod = lod; WorkerThread = Thread.CurrentThread.IsThreadPoolThread; return Output; }
    }
    private sealed class Renderer : IRenderHost
    {
        public int Mounts { get; private set; }
        public int Unmounts { get; private set; }
        public WpId Owner { get; private set; }
        public bool FailMount { get; set; }
        public RenderHandle Mount(ContentOutput content, WpId owner)
        { if (FailMount) throw new InvalidOperationException("mount failed"); Owner = owner; return new((ulong)++Mounts); }
        public void Unmount(RenderHandle handle) => ++Unmounts;
    }
    private sealed class WorkQueue
    {
        private readonly List<(Func<ContentOutput> Build, TaskCompletionSource<ContentOutput> Done)> _jobs = [];
        public Task<ContentOutput> Run(Func<ContentOutput> build)
        {
            var done = new TaskCompletionSource<ContentOutput>();
            _jobs.Add((build, done));
            return done.Task;
        }
        public void Complete(int index) => _jobs[index].Done.SetResult(_jobs[index].Build());
        public void Fail(int index) => _jobs[index].Done.SetException(new InvalidOperationException("worker failed"));
    }
}
