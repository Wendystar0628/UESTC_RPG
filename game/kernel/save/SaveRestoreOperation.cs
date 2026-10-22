using System.Security.Cryptography;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Kernel.Save.Format;
using Campus.Kernel.Maps;
using Campus.Kernel.Diagnostics;

namespace Campus.Kernel.Save;

/// <summary>Owns the restoration transaction and its input lock; all mutation runs on the save frame.</summary>
internal sealed class SaveRestoreOperation(SaveRuntimeBindings runtime, Action faulted, Action succeeded) : IDisposable
{
    private readonly SaveRuntimeBindings _runtime = runtime;
    private bool _abortApplied;
    private IDisposable? _input;

    public Exception? Error { get; private set; }
    public bool IsActive { get; private set; }
    public bool MutationStarted { get; private set; }
    public bool Blocked { get; private set; }

    public Task<MapTravelResult>? Begin()
    {
        IsActive = true; MutationStarted = false; _abortApplied = false; Error = null;
        _input ??= _runtime.Camera.LockInput(InputLockReason.MapTravel);
        return _runtime.Maps.Transition?.Done;
    }

    public void RetainFailure([FailureHandler("SYS-SAVE-FLOW")] Exception error) => Error ??= error;

    public void RetainAbortFailure(Exception failure, [FailureHandler("SYS-SAVE-FLOW")] Exception abortError)
    {
        RetainFailure(failure);
        Error!.Data["SaveRestoreAbort"] = abortError;
        DiagnosticLog.Error("Save restoration abort failed; input remains blocked.", abortError);
    }

    public void MarkFault([FailureHandler("SYS-SAVE-FLOW")] Exception error)
    {
        RetainFailure(error);
        if (_abortApplied) return;
        Blocked = true; _abortApplied = true;
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
                    ? s with { Map = defaultMap.Id, Dto = s.Registration.Attribute.Phase == RestorePhase.Pose || s.Registration.Section.TypeInfo.Type == typeof(MapsSave) ? null : s.Dto }
                    : s).ToArray()
            };
        }
        RestorePhaseOf(prepared, RestorePhase.World, repairs);
        _runtime.WorldRestore.CompleteRestore();
        return (prepared, defaultMapSpawn);
    }

    public (PreparedSave Save, NewGameContext Defaults) PrepareDefaultWorld(SaveRuntimeOptions options,
        SaveSections sections, RulesetId defaultRuleset, SlotId? slot,
        uint? seed, WorldPreset? start, NewGameBoot? boot, Func<SlotId, ulong> nextGeneration)
    {
        MutationStarted = true;
        _runtime.SelectRuleset(defaultRuleset);
        var map = _runtime.Maps.Maps.First(m => m.Id == (boot?.Map ?? _runtime.Maps.Default));
        var context = new NewGameContext(boot?.Weather.Seed ?? seed ?? options.WeatherSeed ?? NewSeed(), map.Id,
            start ?? map.World.Default, boot?.Destination?.Mode ?? _runtime.Ruleset.Current.DefaultViewMode) { Boot = boot };
        var entries = sections.Registered.Select(r => new PreparedSection(SaveSections.Key(r.Attribute, map.Id), r,
            map.Id, r.Attribute.Phase == RestorePhase.World ? r.Section.CreateDefault(context) : null,
            r.Attribute.Version, null)).ToArray();
        var generation = slot is { } selected ? nextGeneration(selected) : 0;
        var meta = new SaveMeta(SaveContainer.CurrentFormat, slot ?? SaveSlots.Auto[0], SlotKind.Auto, generation, generation, 0, map.Id,
            context.Start.Clock, context.Start.Clock, context.Start.Season,
            context.Start.Weather ?? map.World.Default.Weather ?? throw new InvalidDataException("The map has no default weather."),
            context.Mode, boot?.Destination?.Pose ?? default, boot?.Destination?.Space, null, null, _runtime.UnixTime(), SaveReason.NewGame,
            _runtime.Ruleset.Current.Id.Value);
        var save = new PreparedSave(new(meta, new Dictionary<string, RawSaveSection>(), null), entries);
        RestorePhaseOf(save, RestorePhase.World, []); _runtime.WorldRestore.CompleteRestore();
        return (save, context);
    }

    public void RestorePhaseOf(PreparedSave prepared, RestorePhase phase, List<string> repairs, NewGameContext? defaults = null)
    {
        foreach (var item in prepared.Sections.Where(s => s.Registration.Attribute.Phase == phase))
        {
            var context = Context(item);
            var map = _runtime.Maps.Maps.FirstOrDefault(m => m.Id == item.Map);
            if (map is null && item.Registration.Attribute.Scope == SaveScope.PerMap)
            {
                if (item.Error is { } removedError) RemoveInvalidMapSection(item, removedError, repairs);
                else repairs.Add("Removed section for missing map: " + item.Key);
                continue;
            }
            var defaultContext = defaults ?? DefaultsFor(item);
            var dto = item.Error is { } error ? ReplaceInvalidSection(item, error, context, defaultContext)
                : item.Dto ?? item.Registration.Section.CreateDefault(defaultContext);
            item.Registration.Section.Restore(dto, context);
            repairs.AddRange(context.Repairs);
        }
    }

    public PreparedSection PreparePoseSection(PreparedSection item, List<string> repairs, NewGameContext? defaults)
    {
        var context = Context(item);
        var defaultContext = defaults ?? DefaultsFor(item);
        var dto = item.Error is { } error ? ReplaceInvalidSection(item, error, context, defaultContext)
            : item.Dto ?? item.Registration.Section.CreateDefault(defaultContext);
        repairs.AddRange(context.Repairs);
        return item with { Dto = dto, Error = null, Repair = null };
    }

    private static RestoreContext Context(PreparedSection item)
    {
        var context = new RestoreContext { Map = item.Map, FromVersion = item.FromVersion, Missing = item.Dto is null };
        if (item.Error is null && item.Repair is not null) context.Repair(item.Repair);
        return context;
    }

    private NewGameContext DefaultsFor(PreparedSection item)
    {
        var map = _runtime.Maps.Maps.FirstOrDefault(m => m.Id == item.Map)
            ?? _runtime.Maps.Maps.First(m => m.Id == _runtime.Maps.Default);
        return new(_runtime.World.WeatherSeed, item.Map, map.World.Default, _runtime.Ruleset.Current.DefaultViewMode);
    }

    private static object ReplaceInvalidSection(PreparedSection item,
        [FailureHandler("SYS-SAVE-FORMAT")] Exception error, RestoreContext context, NewGameContext defaults)
    {
        context.Repair(item.Repair ?? "Replaced invalid section: " + item.Key);
        var dto = item.Registration.Section.CreateDefault(defaults);
        DiagnosticLog.Warning("Invalid save section replaced with its default: " + item.Key, error);
        return dto;
    }

    private static void RemoveInvalidMapSection(PreparedSection item,
        [FailureHandler("SYS-SAVE-FORMAT")] Exception error, List<string> repairs)
    {
        repairs.Add("Removed section for missing map: " + item.Key);
        DiagnosticLog.Warning("Invalid save section removed with its missing map: " + item.Key, error);
    }

    private static uint NewSeed() => System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(RandomNumberGenerator.GetBytes(4));
}
