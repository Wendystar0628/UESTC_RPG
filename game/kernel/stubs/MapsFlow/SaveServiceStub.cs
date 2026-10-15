using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;

namespace Campus.Kernel.Stubs;

[KernelService(typeof(ISaveService), Owner = "WP-00", IsStub = true)]
public sealed class SaveServiceStub(IMapService maps) : ISaveService
{
    private readonly List<(ISaveSection Section, SaveSectionAttribute Metadata)> _sections = [];
    public Action<NewGameContext>? BeginNewInstance { get; set; }
    public bool Enabled => false;
    public IReadOnlyList<SlotInfo> Slots { get; } = Array.AsReadOnly(Enumerable.Range(1, 3)
        .Select(i => new SlotInfo(new SlotId($"s{i}"), SlotKind.Auto, SlotHealth.Empty, null, 0)).ToArray());
    public SlotId? Continue => null;
    public SlotId? Active { get; private set; }
    public bool Busy => false;
    public SaveState State { get; private set; } = new(null, null, null, null, ReadOnlyReason.SaveOff);
    public event Action? SlotsChanged;
    public bool HasPendingPose => false;

    public void RegisterSection(ISaveSection section, SaveSectionAttribute metadata)
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(metadata);
        if (_sections.Any(x => x.Metadata.Id == metadata.Id && x.Metadata.Scope == metadata.Scope))
            throw new ArgumentException($"Duplicate save section: {metadata.Id}", nameof(metadata));
        _ = metadata.Phase switch
        {
            RestorePhase.World or RestorePhase.Content or RestorePhase.Pose => 0,
            _ => throw new ArgumentOutOfRangeException(nameof(metadata)),
        };
        _sections.Add((section, metadata));
    }

    public void Request(SaveReason r) => MarkDisabled();
    public Task<SaveResult> SaveNow(SaveReason r) => Task.FromResult(MarkDisabled());
    public Task<LoadResult> Load(SlotId s, LoadScope scope = LoadScope.Full)
    {
        _ = scope switch { LoadScope.Full => 0, LoadScope.Preview => 0, _ => throw new ArgumentOutOfRangeException(nameof(scope)) };
        return Task.FromResult(new LoadResult(LoadStatus.Empty, s, 0, []));
    }
    public Task CompleteResume() => Task.CompletedTask;
    public Task<LoadResult> LoadFile(string path) => Task.FromResult(new LoadResult(LoadStatus.Empty, null, 0, []));

    public Task<LoadResult> NewGame(SlotId s, uint? seed = null, WorldPreset? start = null)
    {
        if (!Slots.Any(slot => slot.Slot == s))
            return Task.FromResult(new LoadResult(LoadStatus.Failed, s, 0, [], "Unknown automatic slot."));
        if (maps.Context is not { } context)
            return Task.FromResult(new LoadResult(LoadStatus.Failed, s, 0, [], "No map is loaded."));
        if (BeginNewInstance is null)
            return Task.FromResult(new LoadResult(LoadStatus.Failed, s, 0, [], "World instance initializer is not connected."));
        var c = new NewGameContext(seed ?? context.Descriptor.Seed, maps.Current,
            start ?? context.Descriptor.World.NewGameStart ?? context.Descriptor.World.Default);
        var repairs = new List<string>();
        var sections = _sections.OrderBy(x => x.Metadata.Phase).ThenBy(x => x.Metadata.Order)
            .ThenBy(x => x.Metadata.Id, StringComparer.Ordinal).ToArray();
        foreach (var entry in sections.Where(x => x.Metadata.Phase == RestorePhase.World)) Restore(entry);
        BeginNewInstance(c);
        foreach (var entry in sections.Where(x => x.Metadata.Phase is RestorePhase.Content or RestorePhase.Pose)) Restore(entry);
        Active = s;
        State = new(s, SaveStatus.Disabled, null, null, ReadOnlyReason.SaveOff);
        SlotsChanged?.Invoke();
        return Task.FromResult(new LoadResult(LoadStatus.Loaded, s, 0, repairs.AsReadOnly()));

        void Restore((ISaveSection Section, SaveSectionAttribute Metadata) entry)
        {
            var restore = new RestoreContext { Map = c.Map, FromVersion = entry.Metadata.Version, Missing = true };
            entry.Section.Restore(entry.Section.CreateDefault(c), restore);
            repairs.AddRange(restore.Repairs);
        }
    }

    public Task<SaveResult> FlushForQuit(SaveReason r, TimeSpan timeout)
    {
        _ = r switch { SaveReason.Quit => 0, SaveReason.ToTitle => 0, _ => throw new ArgumentOutOfRangeException(nameof(r)) };
        return Task.FromResult(MarkDisabled());
    }
    public byte[]? Thumbnail(SlotId s) => null;
    private SaveResult MarkDisabled()
    {
        var next = State with { Last = SaveStatus.Disabled };
        if (next != State) { State = next; SlotsChanged?.Invoke(); }
        return new(SaveStatus.Disabled, Active, 0);
    }
}
