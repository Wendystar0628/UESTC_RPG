using System.Security.Cryptography;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Kernel.Save.Format;

namespace Campus.Kernel.Save;

/// <summary>Owns the restoration transaction and its input lock; all mutation runs on the save frame.</summary>
internal sealed class SaveRestoreOperation(SaveRuntimeBindings runtime, Action faulted, Action succeeded) : IDisposable
{
    private readonly SaveRuntimeBindings _runtime = runtime;
    private bool _abortApplied;
    private IDisposable? _input;

    public bool IsActive { get; private set; }
    public bool MutationStarted { get; private set; }
    public bool Blocked { get; private set; }
    public PreparedSave? PendingPose { get; private set; }

    public Task<MapTravelResult>? Begin()
    {
        IsActive = true; MutationStarted = false; _abortApplied = false;
        _input ??= _runtime.Camera.LockInput(InputLockReason.MapTravel);
        return _runtime.Maps.Transition?.Done;
    }

    public void BeginMutation() => MutationStarted = true;
    public void DeferPose(PreparedSave prepared) => PendingPose = prepared;
    public void ClearPendingPose() => PendingPose = null;

    public void MarkFault()
    {
        if (_abortApplied) return;
        Blocked = true; PendingPose = null; _abortApplied = true;
        // Abort before the next World tick can observe the failed transaction's pending state.
        try { _runtime.WorldRestore.AbortRestore(); }
        finally { faulted(); }
    }

    public void Succeed()
    {
        if (_runtime.WorldRestore.RestoreFaulted)
            throw new InvalidOperationException("World restore has not committed successfully.");
        Blocked = false;
        succeeded();
    }

    public void End()
    {
        IsActive = false; MutationStarted = false;
        if (Blocked || _runtime.WorldRestore.RestoreFaulted) return;
        Dispose();
    }

    public void Dispose() { _input?.Dispose(); _input = null; }

    public (PreparedSave Save, string? DefaultMapSpawn) PrepareLoadedWorld(PreparedSave prepared, List<string> repairs)
    {
        var meta = prepared.Document.Meta;
        string? defaultMapSpawn = null;
        MutationStarted = true;
        _runtime.SelectRuleset(new(meta.Ruleset));
        if (!_runtime.Maps.Maps.Any(m => m.Id == meta.Map))
        {
            var defaultMap = _runtime.Maps.Maps.First(m => m.Id == _runtime.Maps.Default);
            defaultMapSpawn = defaultMap.DefaultSpawn;
            repairs.Add("Missing map replaced with the default map: " + meta.Map.Value);
            meta = meta with { Map = defaultMap.Id, Mode = _runtime.Ruleset.Current.DefaultViewMode, Space = null };
            prepared = prepared with
            {
                Document = prepared.Document with { Meta = meta },
                Sections = prepared.Sections.Select(s => s.Registration.Attribute.Scope == SaveScope.Global
                    ? s with { Map = defaultMap.Id, Dto = s.Registration.Attribute.Phase == RestorePhase.Pose || s.Registration.Attribute.Id == "maps" ? null : s.Dto }
                    : s).ToArray()
            };
        }
        RestorePhaseOf(prepared, RestorePhase.World, repairs);
        _runtime.WorldRestore.CompleteRestore();
        return (prepared, defaultMapSpawn);
    }

    public (PreparedSave Save, NewGameContext Defaults) PrepareDefaultWorld(SaveRuntimeOptions options,
        SaveSections sections, RulesetId defaultRuleset, SlotId metadataSlot, SlotId? slot,
        uint? seed, WorldPreset? start, Func<SlotId, ulong> nextGeneration)
    {
        MutationStarted = true;
        _runtime.SelectRuleset(defaultRuleset);
        var map = _runtime.Maps.Maps.First(m => m.Id == _runtime.Maps.Default);
        var context = new NewGameContext(seed ?? options.WeatherSeed ?? (slot.HasValue ? NewSeed() : map.Seed), map.Id,
            start ?? map.World.Default, _runtime.Ruleset.Current.DefaultViewMode);
        var entries = sections.Registered.Select(r => new PreparedSection(SaveSections.Key(r.Attribute, map.Id), r,
            map.Id, r.Attribute.Phase == RestorePhase.World ? r.Section.CreateDefault(context) : null,
            r.Attribute.Version, null)).ToArray();
        var generation = slot is { } selected ? nextGeneration(selected) : 0;
        var meta = new SaveMeta(1, options.Build, slot ?? metadataSlot, SlotKind.Auto, generation, generation, 0, map.Id,
            context.Start.Clock, context.Start.Clock, context.Start.Season,
            context.Start.Weather ?? map.World.Default.Weather ?? throw new InvalidDataException("The map has no default weather."),
            context.Mode, default, null, null, null, _runtime.UnixTime(), SaveReason.NewGame,
            _runtime.Ruleset.Current.Id.Value);
        var save = new PreparedSave(new(meta, new Dictionary<string, RawSaveSection>(), null), entries);
        RestorePhaseOf(save, RestorePhase.World, []); _runtime.WorldRestore.CompleteRestore();
        return (save, context);
    }

    public void RestorePhaseOf(PreparedSave prepared, RestorePhase phase, List<string> repairs, NewGameContext? defaults = null)
    {
        foreach (var item in prepared.Sections.Where(s => s.Registration.Attribute.Phase == phase))
        {
            var context = new RestoreContext { Map = item.Map, FromVersion = item.FromVersion, Missing = item.Dto is null };
            if (item.Repair is not null) context.Repair(item.Repair);
            var map = _runtime.Maps.Maps.FirstOrDefault(m => m.Id == item.Map);
            if (map is null && item.Registration.Attribute.Scope == SaveScope.PerMap)
            { repairs.Add("Removed section for missing map: " + item.Key); continue; }
            map ??= _runtime.Maps.Maps.First(m => m.Id == _runtime.Maps.Default);
            var dto = item.Dto ?? item.Registration.Section.CreateDefault(defaults ??
                new(_runtime.World.WeatherSeed, item.Map, map.World.Default,
                    _runtime.Ruleset.Current.DefaultViewMode));
            item.Registration.Section.Restore(dto, context);
            repairs.AddRange(context.Repairs);
        }
    }

    private static uint NewSeed() => System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(RandomNumberGenerator.GetBytes(4));
}
