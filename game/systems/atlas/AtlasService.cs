using System.Collections.ObjectModel;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Atlas;
using Campus.Contracts.Audio;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;

namespace Campus.Systems.Atlas;

[KernelService(typeof(IAtlasService), Owner = "WP-48c")]
[SaveSection("atlas", Owner = "WP-48c", Version = 1, Scope = SaveScope.PerMap, Phase = RestorePhase.Content, Order = 40)]
public sealed class AtlasService(ICameraRig camera, IObserverState observer, IRuleset ruleset, ITransitDriver? transitDriver = null) : IAtlasService, IMapAware, ISaveSection<AtlasSave>
{
    private readonly Dictionary<string, AtlasMarker> _byId = new(StringComparer.Ordinal);
    private readonly SortedDictionary<string, UnlockState> _unlock = new(StringComparer.Ordinal);
    private readonly SortedSet<string> _visited = new(StringComparer.Ordinal);
    private readonly HashSet<string> _inside = new(StringComparer.Ordinal);
    private readonly List<CustomPin> _pins = [];
    private IReadOnlyList<AtlasMarker> _baseMarkers = [];
    private IMapContext? _context;
    private IUnlockPolicy _policy = AtlasPolicies.Create(ruleset.Current.UnlockPolicy);
    private MapRect? _atlasRect;
    private Func<float, float, string?>? _regionAt;
    private IMapService? _maps;
    private IAppFlow? _flow;
    private IAudioService? _audio;
    private ISaveService? _saves;
    private ITransitHandle? _transit;
    private IDisposable? _transitInput;
    private MarkerId? _destination;
    private TravelStyle _travelStyle;
    private int _nextPinId = 1;
    private AtlasSave? _snapshot;

    public long Rev { get; private set; }
    public MapId Map { get; private set; }
    public MapRect PageRect => _atlasRect ?? throw new InvalidOperationException("Atlas has no active map.");
    public IReadOnlyList<AtlasMarker> Markers { get; private set; } = [];
    public IReadOnlyList<AtlasRegion> Regions { get; private set; } = [];
    public event Action? Changed;
    public MarkerId? Tracked { get; private set; }
    public IReadOnlyList<CustomPin> Pins => _pins.AsReadOnly();
    public string PolicyId => _policy.Id;
    public bool IsSettled => _transit is null;
    public JsonTypeInfo<AtlasSave> TypeInfo => AtlasJsonContext.Contract.AtlasSave;

    // Runtime-only services are connected after discovery to avoid Save -> Atlas -> Save cycles.
    public void Connect(IMapService maps, IAppFlow flow, IAudioService audio, ISaveService saves)
    {
        _maps = maps; _flow = flow; _audio = audio; _saves = saves;
    }

    public void OnMapLoaded(IMapContext context)
    {
        if (ReferenceEquals(_context, context)) return;
        AtlasData.Load(this, context);
    }

    public void LoadMap(IMapContext context, AtlasMarkerFile file, AtlasLabelFile labels, WaypointFile waypoints)
    {
        if (file.Version != 1 || labels.Version != 1 || waypoints.Version != 1)
            throw new JsonException("Unsupported Atlas input version.");
        if (!Finite(file.AtlasRect.Min) || !Finite(file.AtlasRect.Max)
            || file.AtlasRect.Min.X >= file.AtlasRect.Max.X || file.AtlasRect.Min.Y >= file.AtlasRect.Max.Y)
            throw new JsonException("Invalid Atlas page rectangle.");
        var hidden = new HashSet<string>(file.Hidden, StringComparer.Ordinal);
        var markers = new List<AtlasMarker>();
        foreach (var marker in file.Markers)
        {
            if (marker.Kind is MarkerKind.Interior or MarkerKind.Custom)
                throw new JsonException("Interior and custom markers are generated, not authored in markers.json.");
            var point = waypoints.Waypoints.SingleOrDefault(w => w.Id == marker.WaypointId);
            if (marker.Kind == MarkerKind.Viewpoint)
            {
                if (point is null) throw new JsonException($"Missing waypoint: {marker.WaypointId}");
                if (marker.Teleport is not null) throw new JsonException("A viewpoint must reference its waypoint pose.");
                markers.Add(marker with { At = Plan(point.Fly.Position), Teleport = new(point.Fly, point.Stand, point.Entry), Group = point.Group, Recommend = point.Recommend });
            }
            else markers.Add(marker with { Group = point?.Group, Recommend = point?.Recommend });
        }
        foreach (var point in waypoints.Waypoints)
            if (!markers.Any(m => m.WaypointId == point.Id))
                markers.Add(new(new(point.Id), MarkerKind.Viewpoint, point.Name, Plan(point.Fly.Position),
                    "viewpoint", 50, 1, null, new(point.Fly, point.Stand, point.Entry), WaypointId: point.Id)
                { Group = point.Group, Recommend = point.Recommend });
        // The world interior index is the authority for open entrances. No duplicate entrance table.
        foreach (var feature in context.World.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            var interior = context.World.Interiors.Get(feature.Id);
            if (interior is not { Open: true }) continue;
            foreach (var door in interior.Entrances.Where(e => e.Open).OrderBy(e => e.Id, StringComparer.Ordinal))
            {
                var angle = door.YawDeg * float.Pi / 180f;
                var outward = new Vector3(DetMath.Sin(angle), 0, -DetMath.Cos(angle));
                var yaw = (door.YawDeg + 180f) % 360f;
                var fly = new Pose(door.Outside + outward * 8f + Vector3.UnitY * 6f, yaw, -12f);
                var name = feature.Name ?? door.Id;
                markers.Add(new(new("door:" + door.Id), MarkerKind.Interior, new(name, name), Plan(door.Center),
                    "interior", 40, 2, null, new(fly, new(door.Outside, yaw)), feature.Id));
            }
        }
        var visible = markers.Where(m => !hidden.Contains(m.Id.Value)).OrderBy(m => m.Id.Value, StringComparer.Ordinal).ToArray();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var marker in visible)
        {
            if (string.IsNullOrWhiteSpace(marker.Id.Value)) throw new JsonException("Atlas marker ID is empty.");
            if (!ids.Add(marker.Id.Value))
            {
                var first = visible.First(item => item.Id == marker.Id);
                throw new JsonException($"Duplicate Atlas marker ID '{marker.Id.Value}' from features '{first.Feature?.Value}' and '{marker.Feature?.Value}'.");
            }
            if (!Finite(marker.At) || marker.MinZoomBand > 4)
                throw new JsonException($"Atlas marker '{marker.Id.Value}' has invalid coordinates or zoom band.");
            if (marker.Teleport is not null && !Travelable(marker.Kind)) throw new JsonException("This marker kind cannot travel.");
            if (marker.Kind == MarkerKind.Anchor && (marker.Form is null || marker.Feature is null || marker.Teleport is null))
                throw new JsonException("An anchor needs a form, a world feature, and a teleport destination.");
            if (marker.Form is { } form && !Enum.IsDefined(form)) throw new JsonException("Unknown anchor form.");
            if (marker.Teleport is { } destination)
            {
                var pose = destination.Fly;
                if (!Finite(Plan(pose.Position)) || !float.IsFinite(pose.Position.Y)
                    || !float.IsFinite(pose.YawDeg) || !float.IsFinite(pose.PitchDeg) || pose.PitchDeg is < -90 or > 90
                    || (pose.FovDeg is { } fov && (!float.IsFinite(fov) || fov <= 0 || fov >= 180)))
                    throw new JsonException("Invalid Atlas flight pose.");
                if (destination.Stand is { } stand && (!Finite(Plan(stand.Feet))
                    || !float.IsFinite(stand.Feet.Y) || !float.IsFinite(stand.YawDeg)))
                    throw new JsonException("Invalid Atlas standing pose.");
            }
        }
        var regions = labels.Regions.Concat(labels.Places).OrderBy(r => r.Id, StringComparer.Ordinal).ToArray();
        if (regions.Select(r => r.Id).Distinct(StringComparer.Ordinal).Count() != regions.Length)
            throw new JsonException("Atlas region and place IDs must be unique.");
        ReleaseTransit();
        _context = context; Map = context.Descriptor.Id; _atlasRect = file.AtlasRect; _regionAt = null;
        _baseMarkers = Array.AsReadOnly(visible); Regions = Array.AsReadOnly(regions);
        _policy = AtlasPolicies.Create(ruleset.Current.UnlockPolicy);
        _pins.Clear(); _unlock.Clear(); _visited.Clear(); _inside.Clear(); _nextPinId = 1; Tracked = null;
        RefreshMarkers();
        Publish();
    }

    public void SetRegionLookup(Func<float, float, string?> lookup)
    {
        _regionAt = lookup;
        foreach (var id in _visited.ToArray())
            if (Find(new(id)) is { Form: AnchorForm.Kiosk } kiosk) Visit(kiosk);
        Publish();
    }

    public void OnMapUnloading(MapId from)
    {
        if (_context is null || from != Map) return;
        _transit?.Cancel();
        ReleaseTransit();
        _context = null; _atlasRect = null; _regionAt = null; _baseMarkers = []; Regions = []; Map = default;
        _pins.Clear(); _unlock.Clear(); _visited.Clear(); _inside.Clear(); _nextPinId = 1; Tracked = null;
        RefreshMarkers(); Publish();
    }

    public AtlasMarker? Find(MarkerId id) => id.Value is not null && _byId.TryGetValue(id.Value, out var marker) ? marker : null;
    public UnlockState StateOf(MarkerId id) => Find(id) is not { } marker ? UnlockState.Hidden
        : _unlock.TryGetValue(id.Value, out var state) ? state : _policy.Initial(marker);
    public string? RegionAt(float x, float z) => float.IsFinite(x) && float.IsFinite(z) ? _regionAt?.Invoke(x, z) : null;

    public bool CanTravel(MarkerId id, TravelStyle style, out TravelRefusal why)
    {
        ValidateStyle(style);
        // A D0 immediate-completion driver is not a working WP-36 travel implementation.
        if (transitDriver?.IsReady != true || !IsPlaying() || _context is null || _maps?.Context is null || _maps.Current != Map
            || MapTransitioning()) return Refuse(TravelRefusal.Unavailable, out why);
        if (_transit is not null || camera.ActiveTransit is { Done.IsCompleted: false }) return Refuse(TravelRefusal.Busy, out why);
        var marker = Find(id);
        if (marker?.Teleport is null || !Travelable(marker.Kind)) return Refuse(TravelRefusal.NotTravelable, out why);
        if (StateOf(id) != UnlockState.Unlocked) return Refuse(TravelRefusal.Locked, out why);
        var target = observer.Mode switch
        {
            ViewMode.Fly => marker.Teleport.Fly.Position,
            ViewMode.Walk => marker.Teleport.Stand?.Feet ?? marker.Teleport.Fly.Position,
            _ => throw new InvalidOperationException("Unknown observer mode."),
        };
        if (Vector3.DistanceSquared(observer.Feet, target) <= 400f) return Refuse(TravelRefusal.Here, out why);
        why = TravelRefusal.None; return true;
    }

    public void Prefetch(MarkerId id)
    {
        // Destination streaming belongs to WP-36/WP-02. Do not invent a bake key or report readiness here.
        if (Find(id)?.Teleport is null) return;
    }

    public ITransitHandle? Travel(MarkerId id, TravelStyle style)
    {
        if (!CanTravel(id, style, out _)) return null;
        var spec = Find(id)!.Teleport!;
        var stand = observer.Mode switch
        {
            ViewMode.Walk => spec.Stand,
            ViewMode.Fly => null,
            _ => throw new InvalidOperationException("Unknown observer mode."),
        };
        var options = new TransitOptions(style == TravelStyle.Teleport ? null : spec.Entry, stand?.Feet)
        {
            Prefer = style switch
            {
                TravelStyle.Teleport => TransitPreference.Teleport,
                TravelStyle.Fly => TransitPreference.Auto,
                _ => throw new ArgumentOutOfRangeException(nameof(style)),
            },
        };
        // Teleport retains the map's input exclusion through the veil. Fly must remain interruptible.
        var input = style switch
        {
            TravelStyle.Teleport => camera.LockInput(InputLockReason.Atlas),
            TravelStyle.Fly => null,
            _ => throw new ArgumentOutOfRangeException(nameof(style)),
        };
        try
        {
            _transit = camera.Transit(stand is null ? spec.Fly : spec.Fly with { YawDeg = stand.YawDeg }, options);
            _transitInput = input; _destination = id; _travelStyle = style;
            if (style == TravelStyle.Teleport) _audio?.PlayUi(UiCue.TeleportConfirm);
            return _transit;
        }
        catch { input?.Dispose(); throw; }
    }

    // Called only from the Systems frame step. Completion never mutates state on a thread-pool continuation.
    public void Tick()
    {
        if (_transit is { Done.IsCompleted: true } handle)
        {
            var arrived = handle.Done.IsCompletedSuccessfully && (handle.Done.Result switch
            {
                TransitResult.Arrived => true,
                TransitResult.Cancelled => false,
                _ => throw new InvalidOperationException("Unknown transit result."),
            });
            var destination = _destination;
            var style = _travelStyle;
            ReleaseTransit();
            if (arrived)
            {
                if (destination is { } id && Find(id) is { Kind: MarkerKind.Anchor } anchor)
                { Visit(anchor); Publish(); }
                if (style == TravelStyle.Teleport) _audio?.PlayUi(UiCue.TeleportArrive);
                _saves?.Request(SaveReason.Settled);
            }
        }
        if (_context is null || !IsPlaying() || _transit is not null || MapTransitioning()) return;
        var walk = observer.Mode switch
        {
            ViewMode.Walk => true,
            ViewMode.Fly => false,
            _ => throw new InvalidOperationException("Unknown observer mode."),
        };
        var position = observer.Feet;
        if (!Finite(Plan(position)) || !float.IsFinite(position.Y)) return;
        var height = position.Y - _context.World.Spatial.GroundY(position.X, position.Z);
        if (!float.IsFinite(height)) return;
        var changed = false;
        foreach (var marker in _baseMarkers)
        {
            if (marker.Kind != MarkerKind.Anchor) continue;
            var dist = Vector2.DistanceSquared(Plan(position), marker.At);
            if (_inside.Contains(marker.Id.Value))
            {
                var exit = walk ? 12f : 35f;
                if (dist > exit * exit || (!walk && height >= 30f)) _inside.Remove(marker.Id.Value);
                continue;
            }
            var enter = walk ? 8f : 25f;
            if (dist > enter * enter || (!walk && (height < 0f || height >= 25f))) continue;
            _inside.Add(marker.Id.Value);
            changed |= Visit(marker);
        }
        if (changed) Publish();
    }

    private bool Visit(AtlasMarker marker)
    {
        var changed = _visited.Add(marker.Id.Value);
        changed |= SetState(marker, _policy.OnVisit(marker, StateOf(marker.Id)));
        if (_policy is not ProximityUnlockPolicy || marker.Form != AnchorForm.Kiosk) return changed;
        var district = District(marker);
        if (district is null) return changed;
        foreach (var post in _baseMarkers)
            if (post.Kind == MarkerKind.Anchor && post.Form == AnchorForm.Post && District(post) == district
                && StateOf(post.Id) == UnlockState.Hidden)
                changed |= SetState(post, UnlockState.Known);
        return changed;
    }

    private string? District(AtlasMarker marker)
    {
        var id = marker.Region ?? RegionAt(marker.At.X, marker.At.Y);
        for (var limit = Regions.Count; id is not null && limit > 0; limit--)
        {
            var parent = Regions.FirstOrDefault(r => r.Id == id)?.Parent;
            if (parent is null) return id;
            id = parent;
        }
        return null;
    }

    private bool SetState(AtlasMarker marker, UnlockState value)
    {
        if (StateOf(marker.Id) == value) return false;
        if (_policy.Initial(marker) == value) _unlock.Remove(marker.Id.Value); else _unlock[marker.Id.Value] = value;
        return true;
    }

    public AtlasSave Capture(in CaptureContext context)
    {
        if (_context is null || context.Map != Map) throw new InvalidOperationException("Atlas capture requires the active map.");
        var snapshot = _snapshot ??= Snapshot();
        if (!context.Forced || _transit is null || _destination is not { } target
            || Find(target) is not { Kind: MarkerKind.Anchor } marker) return snapshot;
        if (_transit.Done.IsCompleted && (!_transit.Done.IsCompletedSuccessfully || _transit.Done.Result == TransitResult.Cancelled))
            return snapshot;
        // A forced save uses the same destination as the view section without committing a live visit.
        var visited = new SortedSet<string>(snapshot.VisitedAnchors, StringComparer.Ordinal) { marker.Id.Value };
        var unlock = new SortedDictionary<string, UnlockState>(_unlock, StringComparer.Ordinal);
        var state = _policy.OnVisit(marker, StateOf(marker.Id));
        if (state == _policy.Initial(marker)) unlock.Remove(marker.Id.Value); else unlock[marker.Id.Value] = state;
        if (_policy is ProximityUnlockPolicy && marker.Form == AnchorForm.Kiosk && District(marker) is { } district)
            foreach (var post in _baseMarkers)
                if (post.Form == AnchorForm.Post && District(post) == district && StateOf(post.Id) == UnlockState.Hidden)
                    unlock[post.Id.Value] = UnlockState.Known;
        return snapshot with { Unlock = new ReadOnlyDictionary<string, UnlockState>(unlock), VisitedAnchors = Array.AsReadOnly(visited.ToArray()) };
    }

    public AtlasSave CreateDefault(NewGameContext context) => new(ruleset.Current.UnlockPolicy, [], 1, null,
        new ReadOnlyDictionary<string, UnlockState>(new SortedDictionary<string, UnlockState>(StringComparer.Ordinal)), []);

    public void Restore(AtlasSave dto, RestoreContext context)
    {
        if (_context is null || context.Map != Map) throw new InvalidOperationException("Atlas restore requires the active map.");
        _transit?.Cancel();
        ReleaseTransit(); _inside.Clear(); _pins.Clear(); _unlock.Clear(); _visited.Clear();
        if (!AtlasPolicies.IsSupported(dto.Policy)) context.Repair("atlas.policy: unknown policy replaced from ruleset");
        _policy = AtlasPolicies.Create(AtlasPolicies.IsSupported(dto.Policy) ? dto.Policy : ruleset.Current.UnlockPolicy);
        if (dto.Pins is null) context.Repair("atlas.pins: missing list replaced with empty");
        if (dto.Unlock is null) context.Repair("atlas.unlock: missing table replaced with empty");
        if (dto.VisitedAnchors is null) context.Repair("atlas.visitedAnchors: missing list replaced with empty");
        var ids = new HashSet<int>();
        foreach (var pin in dto.Pins ?? [])
        {
            if (_pins.Count >= 50 || pin is null || pin.Id <= 0 || pin.Id == int.MaxValue
                || !Contains(pin.At) || !Enum.IsDefined(pin.Shape) || !ids.Add(pin.Id))
            { context.Repair("atlas.pins: invalid, duplicate, outside, or excess pin discarded"); continue; }
            _pins.Add(pin);
        }
        _pins.Sort((a, b) => a.Id.CompareTo(b.Id));
        var minimum = _pins.Count == 0 ? 1 : _pins[^1].Id + 1;
        _nextPinId = Math.Max(dto.NextPinId, minimum);
        if (_nextPinId != dto.NextPinId) context.Repair("atlas.nextPinId: advanced beyond existing IDs");
        RefreshMarkers();
        foreach (var entry in (dto.Unlock ?? new Dictionary<string, UnlockState>()).OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            var marker = Find(new(entry.Key));
            if (marker is null || !Enum.IsDefined(entry.Value)) { context.Repair("atlas.unlock: unknown marker or state discarded"); continue; }
            if (_policy is AllUnlockedPolicy && entry.Value != UnlockState.Unlocked)
            { context.Repair("atlas.unlock: all-unlocked policy restored to its initial state"); continue; }
            SetState(marker, entry.Value);
        }
        foreach (var id in dto.VisitedAnchors ?? [])
            if (Find(new(id)) is { Kind: MarkerKind.Anchor }) _visited.Add(id);
            else context.Repair("atlas.visitedAnchors: unknown anchor discarded");
        Tracked = dto.Tracked is not null && Find(new(dto.Tracked)) is not null ? new(dto.Tracked) : null;
        if (dto.Tracked is not null && Tracked is null) context.Repair("atlas.tracked: missing target cleared");
        Publish();
    }

    // The contract retains pin/track operations; their UI is outside this wave.
    public void Track(MarkerId? id)
    {
        if (id is { } value && Find(value) is null) throw new ArgumentException("Unknown marker.", nameof(id));
        if (Tracked == id) return;
        Tracked = id; Publish();
    }
    public CustomPin? AddPin(Vector2 at, PinShape shape)
    {
        ValidatePin(at, shape);
        if (_pins.Count >= 50 || _nextPinId == int.MaxValue) return null;
        var pin = new CustomPin(_nextPinId++, at, shape); _pins.Add(pin); RefreshMarkers(); Publish(); return pin;
    }
    public void MovePin(int id, Vector2 at)
    {
        if (!Contains(at)) throw new ArgumentOutOfRangeException(nameof(at));
        var index = _pins.FindIndex(p => p.Id == id);
        if (index < 0 || _pins[index].At == at) return;
        _pins[index] = _pins[index] with { At = at }; RefreshMarkers(); Publish();
    }
    public void SetPinShape(int id, PinShape shape)
    {
        if (!Enum.IsDefined(shape)) throw new ArgumentOutOfRangeException(nameof(shape));
        var index = _pins.FindIndex(p => p.Id == id);
        if (index < 0 || _pins[index].Shape == shape) return;
        _pins[index] = _pins[index] with { Shape = shape }; RefreshMarkers(); Publish();
    }
    public void RemovePin(int id)
    {
        if (_pins.RemoveAll(p => p.Id == id) == 0) return;
        if (Tracked == PinId(id)) Tracked = null;
        RefreshMarkers(); Publish();
    }

    private void RefreshMarkers()
    {
        var markers = _baseMarkers.Concat(_pins.Select(pin => new AtlasMarker(PinId(pin.Id), MarkerKind.Custom,
            new("自定义标记", "Custom marker"), pin.At, pin.Shape.ToString().ToLowerInvariant(), 100, 0, null)))
            .OrderBy(m => m.Id.Value, StringComparer.Ordinal).ToArray();
        _byId.Clear(); foreach (var marker in markers) _byId.Add(marker.Id.Value, marker);
        Markers = Array.AsReadOnly(markers);
    }
    private AtlasSave Snapshot() => new(PolicyId, Array.AsReadOnly(_pins.ToArray()), _nextPinId, Tracked?.Value,
        new ReadOnlyDictionary<string, UnlockState>(new SortedDictionary<string, UnlockState>(_unlock, StringComparer.Ordinal)),
        Array.AsReadOnly(_visited.ToArray()));
    private void Publish() { _snapshot = null; Rev++; Changed?.Invoke(); }
    private void ReleaseTransit()
    {
        _transit = null; _destination = null; _transitInput?.Dispose(); _transitInput = null;
    }
    private bool Contains(Vector2 at) => Finite(at) && _atlasRect is { } rect
        && at.X >= rect.Min.X && at.X <= rect.Max.X && at.Y >= rect.Min.Y && at.Y <= rect.Max.Y;
    private void ValidatePin(Vector2 at, PinShape shape)
    {
        if (!Contains(at)) throw new ArgumentOutOfRangeException(nameof(at));
        if (!Enum.IsDefined(shape)) throw new ArgumentOutOfRangeException(nameof(shape));
    }
    private static bool Finite(Vector2 at) => float.IsFinite(at.X) && float.IsFinite(at.Y);
    private static Vector2 Plan(Vector3 at) => new(at.X, at.Z);
    private static MarkerId PinId(int id) => new("pin:" + id.ToString(System.Globalization.CultureInfo.InvariantCulture));
    private static bool Refuse(TravelRefusal value, out TravelRefusal why) { why = value; return false; }
    private bool IsPlaying() => _flow?.State switch
    {
        AppState.Playing => true,
        null or AppState.Boot or AppState.Title or AppState.Entering or AppState.Leaving or AppState.Quitting => false,
        _ => throw new InvalidOperationException("Unknown application state."),
    };
    private bool MapTransitioning() => _maps?.Transition?.Stage switch
    {
        null or MapStage.Done => false,
        MapStage.VeilIn or MapStage.Snapshot or MapStage.Unload or MapStage.LoadData or MapStage.Mount
            or MapStage.Warm or MapStage.Spawn or MapStage.VeilOut => true,
        _ => throw new InvalidOperationException("Unknown map transition stage."),
    };
    private static void ValidateStyle(TravelStyle style) => _ = style switch
    { TravelStyle.Teleport or TravelStyle.Fly => 0, _ => throw new ArgumentOutOfRangeException(nameof(style)) };
    private static bool Travelable(MarkerKind kind) => kind switch
    {
        MarkerKind.Anchor or MarkerKind.Viewpoint or MarkerKind.Interior or MarkerKind.Route => true,
        MarkerKind.Landmark or MarkerKind.Facility or MarkerKind.Custom or MarkerKind.Quest => false,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
