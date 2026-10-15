using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Atlas;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;

namespace Campus.Kernel.Stubs;

[KernelService(typeof(IAtlasService), Owner = "WP-00", IsStub = true)]
public sealed class AtlasServiceStub(ICameraRig camera, IRuleset ruleset, IAppFlow flow) : IAtlasService, IMapAware
{
    private readonly List<CustomPin> _pins = [];
    private AtlasMarker[] _waypointMarkers = [];
    private int _nextPinId = 1;
    private MapRect? _pageRect;
    private MapId _pageRectMap;
    public Func<string, byte[]>? ReadData { get; set; }
    public long Rev { get; private set; }
    public MapId Map { get; private set; }
    public MapRect PageRect => _pageRect is { } rect && _pageRectMap == Map ? rect :
        throw new InvalidOperationException("Atlas page rectangle is unavailable before map context is loaded.");
    public IReadOnlyList<AtlasMarker> Markers { get; private set; } = [];
    public IReadOnlyList<AtlasRegion> Regions => [];
    public event Action? Changed;
    public MarkerId? Tracked { get; private set; }
    public IReadOnlyList<CustomPin> Pins => _pins.AsReadOnly();
    public string PolicyId => ruleset.Current.UnlockPolicy;

    public void LoadFromBytes(MapId map, ReadOnlySpan<byte> bytes)
    {
        var file = JsonSerializer.Deserialize<WaypointFile>(bytes, ContractJson.Options)
            ?? throw new JsonException("Waypoint file is null.");
        if (file.Version != 1) throw new JsonException("Unknown waypoint version.");
        if (file.Waypoints.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != file.Waypoints.Count)
            throw new JsonException("Waypoint IDs must be unique.");
        Map = map;
        _pins.Clear(); _nextPinId = 1; Tracked = null;
        _waypointMarkers = file.Waypoints.Select(w => new AtlasMarker(new MarkerId(w.Id), MarkerKind.Viewpoint,
            w.Name, new Vector2(w.Fly.Position.X, w.Fly.Position.Z), "viewpoint", 0, 0, null,
            new TeleportSpec(w.Fly, w.Stand, w.Entry), WaypointId: w.Id)
        { Group = w.Group }).ToArray();
        Refresh();
    }

    public AtlasMarker? Find(MarkerId id) => Markers.FirstOrDefault(x => x.Id == id);
    public UnlockState StateOf(MarkerId id) => Find(id) is null ? UnlockState.Hidden : UnlockState.Unlocked;
    public string? RegionAt(float x, float z) => null;
    public bool CanTravel(MarkerId id, TravelStyle style, out TravelRefusal why)
    {
        _ = style switch { TravelStyle.Teleport => 0, TravelStyle.Fly => 0, _ => throw new ArgumentOutOfRangeException(nameof(style)) };
        var available = flow.State switch
        {
            AppState.Playing => true,
            AppState.Boot or AppState.Title or AppState.Entering or AppState.Leaving or AppState.Quitting => false,
            _ => throw new ArgumentOutOfRangeException(nameof(flow.State)),
        };
        if (!available) { why = TravelRefusal.Unavailable; return false; }
        if (Find(id)?.Teleport is null) { why = TravelRefusal.NotTravelable; return false; }
        why = TravelRefusal.None;
        return true;
    }
    public void Prefetch(MarkerId id) { }
    public ITransitHandle? Travel(MarkerId id, TravelStyle style)
    {
        if (!CanTravel(id, style, out _)) return null;
        var destination = Find(id)!.Teleport!;
        // D0 always uses the fly transit path, including requests from the teleport button.
        return camera.Transit(destination.Fly, new TransitOptions(destination.Entry, destination.Stand?.Feet));
    }
    public void Track(MarkerId? id)
    {
        if (id is { } marker && Find(marker) is null) throw new ArgumentException("Unknown marker.", nameof(id));
        if (Tracked == id) return;
        Tracked = id; Refresh();
    }
    public CustomPin? AddPin(Vector2 at, PinShape shape)
    {
        if (_pins.Count >= 50) return null;
        ValidateShape(shape);
        if (!float.IsFinite(at.X) || !float.IsFinite(at.Y)) throw new ArgumentOutOfRangeException(nameof(at));
        var pin = new CustomPin(_nextPinId++, at, shape);
        _pins.Add(pin); Refresh(); return pin;
    }
    public void MovePin(int id, Vector2 at)
    {
        if (!float.IsFinite(at.X) || !float.IsFinite(at.Y)) throw new ArgumentOutOfRangeException(nameof(at));
        var i = _pins.FindIndex(p => p.Id == id);
        if (i >= 0) { _pins[i] = _pins[i] with { At = at }; Refresh(); }
    }
    public void SetPinShape(int id, PinShape shape)
    {
        ValidateShape(shape);
        var i = _pins.FindIndex(p => p.Id == id);
        if (i >= 0) { _pins[i] = _pins[i] with { Shape = shape }; Refresh(); }
    }
    public void RemovePin(int id)
    {
        if (_pins.RemoveAll(p => p.Id == id) == 0) return;
        if (Tracked == new MarkerId($"pin:{id}")) Tracked = null;
        Refresh();
    }
    public void OnMapUnloading(MapId from)
    {
        _pageRect = null;
        _pageRectMap = default;
        _waypointMarkers = []; _pins.Clear(); _nextPinId = 1; Tracked = null; Map = default;
        Refresh();
    }
    public void OnMapLoaded(IMapContext ctx)
    {
        // Explicit D0 fallback until authored atlas markers are available: 1.2 times this map's data extent.
        var dataRect = ctx.Descriptor.DataRect;
        var center = (dataRect.Min + dataRect.Max) * 0.5f;
        var halfExtent = (dataRect.Max - dataRect.Min) * 0.6f;
        _pageRect = new(center - halfExtent, center + halfExtent);
        _pageRectMap = ctx.Descriptor.Id;
        if (ReadData is { } read) LoadFromBytes(ctx.Descriptor.Id, read(ctx.DataRoot + "waypoints.json"));
        else if (Map != ctx.Descriptor.Id)
        {
            _waypointMarkers = []; _pins.Clear(); _nextPinId = 1; Tracked = null; Map = ctx.Descriptor.Id;
            Refresh();
        }
    }
    private void Refresh()
    {
        Markers = Array.AsReadOnly(_waypointMarkers.Concat(_pins.Select(p => new AtlasMarker(
            new MarkerId($"pin:{p.Id}"), MarkerKind.Custom, new LocalizedName("", ""), p.At,
            "pin", 0, 0, null))).ToArray());
        Rev++; Changed?.Invoke();
    }
    private static void ValidateShape(PinShape shape)
    {
        _ = shape switch
        {
            PinShape.Diamond or PinShape.Circle or PinShape.Square or PinShape.Triangle or PinShape.Star or PinShape.Flag => 0,
            _ => throw new ArgumentOutOfRangeException(nameof(shape)),
        };
    }
}
