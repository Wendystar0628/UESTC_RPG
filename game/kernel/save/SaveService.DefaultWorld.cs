using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Kernel.Save.Format;

namespace Campus.Kernel.Save;

public sealed partial class SaveService
{
    public Task<LoadResult> NewGame(SlotId slot, uint? seed = null, WorldPreset? start = null)
    {
        if (!AutoSlots.Contains(slot)) throw new ArgumentOutOfRangeException(nameof(slot));
        return RestoreDefaultWorld(slot, seed, start);
    }

    public Task<LoadResult> RestoreTitlePreview() => RestoreDefaultWorld(null, null, null);

    private async Task<LoadResult> RestoreDefaultWorld(SlotId? slot, uint? seed, WorldPreset? start)
    {
        await _operation.WaitAsync().ConfigureAwait(false);
        try
        {
            await BeginRestoreOperation().ConfigureAwait(false);
            NewGameContext? defaults = null;
            var prepared = await OnFrame(() =>
            {
                _restoreMutationStarted = true;
                _runtime.SelectRuleset(_defaultRuleset);
                var map = _runtime.Maps.Maps.First(m => m.Id == _runtime.Maps.Default);
                var context = new NewGameContext(seed ?? _options.WeatherSeed ?? (slot.HasValue ? NewSeed() : map.Seed), map.Id,
                    start ?? map.World.Default, _runtime.Ruleset.Current.DefaultViewMode);
                defaults = context;
                var entries = _sections.Registered.Select(r => new PreparedSection(SaveSections.Key(r.Attribute, map.Id), r,
                    map.Id, r.Attribute.Phase == RestorePhase.World ? r.Section.CreateDefault(context) : null,
                    r.Attribute.Version, null)).ToArray();
                var generation = slot is { } selected ? checked(_slots[selected].MaxGeneration + 1) : 0;
                var meta = new SaveMeta(1, _options.Build, slot ?? AutoSlots[0], SlotKind.Auto, generation, generation, 0, map.Id,
                    context.Start.Clock, context.Start.Clock, context.Start.Season,
                    context.Start.Weather ?? map.World.Default.Weather ?? throw new InvalidDataException("The map has no default weather."),
                    context.Mode, default, null, null, null, _runtime.UnixTime(), SaveReason.NewGame,
                    _runtime.Ruleset.Current.Id.Value);
                var save = new PreparedSave(new(meta, new Dictionary<string, RawSaveSection>(), null), entries);
                RestorePhaseOf(save, RestorePhase.World, []); _runtime.WorldRestore.CompleteRestore();
                Active = slot.HasValue ? _options.WriteSlot ?? slot : null; _newLine = slot.HasValue; _line = 0; _playSeconds = 0;
                _authorizedOverwrite = slot.HasValue && Active == slot;
                _thumbnail = null; _unknown = new Dictionary<string, RawSaveSection>(); _pendingPose = null;
                return save;
            }).ConfigureAwait(false);
            await Travel(prepared.Document.Meta.Map, null, null, _runtime.Maps.Context is null ? MapTravelCause.Boot : MapTravelCause.NewGame).ConfigureAwait(false);
            await OnFrame(() => RestorePhaseOf(prepared, RestorePhase.Content, [], defaults)).ConfigureAwait(false);
            prepared = await PrepareViewPose(prepared, [], defaults).ConfigureAwait(false);
            var wait = await OnFrame(() =>
            {
                RestorePhaseOf(prepared, RestorePhase.Pose, [], defaults);
                return _runtime.WaitDestination(_runtime.Camera.Pose, _runtime.Observer.Space, _runtime.Observer.Mode);
            }).ConfigureAwait(false);
            await wait.ConfigureAwait(false);
            await OnFrame(RestoreSucceeded).ConfigureAwait(false);
            return new(LoadStatus.Loaded, Active, 0, []);
        }
        catch (Exception error) { return await RestoreFailed(slot, error).ConfigureAwait(false); }
        finally { await OnFrame(EndRestoreOperation).ConfigureAwait(false); _operation.Release(); }
    }

}
