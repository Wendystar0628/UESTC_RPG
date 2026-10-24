using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Kernel.Frame;
using Campus.Kernel.Lod;
using N = System.Numerics;

namespace Campus.Kernel.Stream;

internal sealed class StreamSelection
{
    private readonly StreamDirector _host;
    private readonly StreamSettings _settings = new();
    private readonly LodSpeedPolicy _speed = new();
    private readonly HashSet<StreamTile> _required = [], _refined = [], _destinationRequired = [], _destinationRefined = [], _visible = [];
    private readonly HashSet<StreamTile> _viewRequired = [], _displayRefined = [], _predictionRequired = [];
    private readonly HashSet<StreamTile>[] _predictionRefined;
    private readonly Func<StreamTile, bool> _tileReady;
    private readonly Action<StreamTile, bool, float> _changeVisibility;
    private TileVisibility _visibility;
    private LodTree? _tree;
    private TerrainPlan? _terrainPlan;
    private readonly List<TerrainPlan> _retiredPlans = [];
    internal bool IsIdle => _terrainPlan is null && _retiredPlans.Count == 0;
    internal int Preparations => _terrainPlan is null ? 0 : 1;
    internal int Retirements => _retiredPlans.Count;
    private IMapContext? _plannedContext;
    private IWorldModel? _plannedWorld;
    private Pose _plannedPose;
    private bool _plannedLive, _planReady;
    private long _plannedSequence, _interiorDestinationSequence = -1;
    private Pose? _destination;
    private ViewMode _destinationMode;
    private MapStreamDestination? _destinationState;
    private float _replan;
    private bool _destinationPlanned, _destinationConsumed;
    private long _destinationSequence;
    private bool _haveCamera, _selectionApplied;
    private N.Vector3 _lastCamera, _velocity;
    internal StreamSelection(StreamDirector host)
    {
        _host = host;
        _predictionRefined = new HashSet<StreamTile>[_settings.PrefetchSeconds.Length];
        for (var i = 0; i < _predictionRefined.Length; i++) _predictionRefined[i] = [];
        _tileReady = TileReady; _changeVisibility = ChangeVisibility;
        _visibility = new(_settings.FadeSeconds);
    }
    internal StreamSettings Settings => _settings;
    internal LodTree? Tree => _tree;
    internal IReadOnlySet<StreamTile> Required => _required;
    internal IReadOnlySet<StreamTile> Refined => _refined;
    internal IReadOnlySet<StreamTile> DestinationRequired => _destinationRequired;
    internal IReadOnlySet<StreamTile> DestinationRefined => _destinationRefined;
    internal TileVisibility Visibility => _visibility;
    internal N.Vector3 Velocity => _velocity;
    internal MapStreamDestination? Destination => _destinationState;
    internal bool HasPendingDestination => _destinationState is not null && !_destinationConsumed;
    internal int VisibleTiles => _visibility.Drawn.Count;
    internal bool ViewReady => SelectionReady(_viewRequired, _refined) && _host.Participants.ParticipantsReady(destination: false);

    internal bool DestinationReady => _destinationConsumed ? _host.Failure is null && _host.Context is not null : _destinationPlanned && _destination is { } destination &&
        SelectionReady(_destinationRequired, _destinationRefined) && _host.Participants.ParticipantsReady(destination: true) && (_host.Interiors?.DestinationReady ?? true) && (_destinationMode switch
        {
            ViewMode.Fly => true,
            ViewMode.Walk => _host.Collision.Ready(_destinationState?.CollisionFeet ?? destination.Position, WalkRules.ArrivalCollisionRadius),
            _ => throw new ArgumentOutOfRangeException(nameof(_destinationMode))
        });

    internal bool PlanCurrent => _planReady && _host.Context is not null && ReferenceEquals(_plannedContext, _host.Context) &&
        ReferenceEquals(_plannedWorld, _host.Context.World) && _plannedSequence == _destinationSequence && _plannedLive == _host.CanUseLiveCamera() &&
        _plannedPose == (_plannedLive ? _host.Camera.Pose : _destination ?? _host.Camera.Pose);

    private void Replan(bool exempt)
    {
        _selectionApplied = false;
        var pose = _host.Camera.Pose;
        var live = _host.CanUseLiveCamera();
        if (!live && _destination is { } mounting) pose = mounting;
        if (!PreparePlanningTerrain(pose, live)) return;
        var tree = _tree ?? throw new InvalidOperationException("A map tree is required before planning.");
        if (!_destinationConsumed && _destination is { } accepted && _interiorDestinationSequence != _destinationSequence)
        {
            var space = _destinationState!.Space;
            if (space is null && _host.Context!.World.Spatial.MediumAt(accepted.Position) is { Kind: MediumKind.Interior, Feature: { } building } medium)
                space = new(building, checked((short)medium.Storey));
            _destinationState = _destinationState with { Space = space };
            _host.Interiors!.Prefetch(space, accepted.Position);
            _interiorDestinationSequence = _destinationSequence;
        }
        SelectEye(tree, ref pose, live, exempt);
        SelectPrediction(tree, pose, live, exempt);
        SelectArrival(tree);
        _host.Pins.Select();
        _host.Interiors!.RefreshPriorities();
        var requests = new List<StreamRequest>();
        _host.Residency.QueueGeometryRequests(requests, pose);
        _host.Participants.QueueParticipants(requests, pose);
        _host.Preparation.ReplaceRequests(requests);
        _host.RequestRetention();
        _plannedContext = _host.Context; _plannedWorld = _host.Context!.World; _plannedPose = pose;
        _plannedLive = live; _plannedSequence = _destinationSequence; _planReady = true;
    }

    private bool PreparePlanningTerrain(Pose pose, bool live)
    {
        var context = _host.Context!;
        var same = _terrainPlan is { } pending && ReferenceEquals(pending.Context, context) && ReferenceEquals(pending.World, context.World) &&
            pending.Generation == _host.MapGeneration && pending.Sequence == _destinationSequence && pending.Pose == pose && pending.Live == live;
        if (!same) CancelTerrainPlan();
        var points = new List<N.Vector2>(2);
        if (live || _destination is not null) points.Add(new(pose.Position.X, pose.Position.Z));
        if (!_destinationConsumed && _destination is { } destination)
        {
            var target = new N.Vector2(destination.Position.X, destination.Position.Z);
            if (!points.Contains(target)) points.Add(target);
        }
        if (points.All(point => _host.Terrain.IsPrepared(context.World, point))) { CancelTerrainPlan(); return true; }
        _planReady = false;
        if (_terrainPlan is null)
        {
            var cancellation = new CancellationTokenSource();
            _terrainPlan = new(context, context.World, _host.MapGeneration, _destinationSequence, pose, live,
                _host.Terrain.PreparePoints(context, points, cancellation.Token), cancellation);
            _host.Preparation.ClearRequests();
        }
        if (!_terrainPlan.Preparation.IsCompleted) return false;
        try { _terrainPlan.Preparation.GetAwaiter().GetResult(); }
        catch (Exception error) { _host.RecordFailure(error); CancelTerrainPlan(); return false; }
        if (!points.All(point => _host.Terrain.IsPrepared(context.World, point)))
            throw new InvalidOperationException("Terrain preparation completed without the current planning points.");
        CancelTerrainPlan(); return true;
    }

    internal void CancelTerrainPlan()
    {
        if (_terrainPlan is not { } pending) return;
        _terrainPlan = null;
        _retiredPlans.Add(pending);
        pending.Cancellation.Cancel();
    }

    internal void PollRetired()
    {
        for (var i = _retiredPlans.Count - 1; i >= 0; i--)
        {
            var pending = _retiredPlans[i];
            if (!pending.Preparation.IsCompleted) continue;
            try { pending.Preparation.GetAwaiter().GetResult(); }
            catch (OperationCanceledException) when (pending.Cancellation.IsCancellationRequested) { }
            catch (Exception error) { _host.RecordFailure(error); }
            finally { pending.Cancellation.Dispose(); _retiredPlans.RemoveAt(i); }
        }
    }

    internal async Task DrainAsync()
    {
        CancelTerrainPlan();
        foreach (var pending in _retiredPlans)
        {
            try { await pending.Preparation; }
            catch (OperationCanceledException) when (pending.Cancellation.IsCancellationRequested) { }
            catch (Exception error) { _host.RecordFailure(error); }
            finally { pending.Cancellation.Dispose(); }
        }
        _retiredPlans.Clear();
    }

    private void ResolveVisible()
    {
        if (_destination is null) { _visible.Clear(); return; }
        _displayRefined.Clear(); _displayRefined.UnionWith(_refined);
        // An active arrival must be presentable even when its required detail differs from the current eye.
        // Hidden pinned resources and predictions never refine the displayed frontier.
        if (!_destinationConsumed && _destinationPlanned) _displayRefined.UnionWith(_destinationRefined);
        _tree!.ResolveVisible(_displayRefined, _tileReady, _visible);
    }

    private bool SelectionReady(IReadOnlySet<StreamTile> required, IReadOnlySet<StreamTile> refined)
    {
        if (!_host.ResidentReady || !_selectionApplied || !PlanCurrent ||
            _tree is null || required.Count == 0 || !_visibility.IsSettled(FrameClock.FadeSeconds)) return false;
        foreach (var tile in required)
            if (!refined.Contains(tile) && !HasVisibleCoverage(tile)) return false;
        return true;
    }

    private bool HasVisibleCoverage(StreamTile tile)
    {
        if (_visibility.Drawn.Contains(tile) && _host.Residency.IsReady(tile)) return true;
        var children = _tree!.Children(tile);
        if (children.Count == 0) return false;
        foreach (var child in children)
            if (!HasVisibleCoverage(child)) return false;
        return true;
    }

    internal static N.Vector3? WalkingDestination(Pose pose, ViewMode mode) => mode switch
    {
        ViewMode.Walk => pose.Position,
        ViewMode.Fly => null,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    private void SelectEye(LodTree tree, ref Pose pose, bool live, bool exempt)
    {
        _required.Clear(); _required.Add(StreamTile.Root);
        _host.Participants.ResetParticipantDemand();
        if (live)
        {
            _host.Interiors!.Select(_host.Context!.World.Spatial.MediumAt(pose.Position), pose.Position);
            tree.Select(pose.Position, exempt ? 0 : _host.Camera.Speed, pose.FovDeg ?? 60, _speed.Minimum, _refined, _required, _refined);
            _host.Participants.SelectParticipants(pose, _required, destination: false, live: true, walkingCenter: _host.Observer.Mode switch
            {
                ViewMode.Walk => _host.Observer.Feet,
                ViewMode.Fly => null,
                _ => throw new ArgumentOutOfRangeException(nameof(_host.Observer.Mode))
            });
        }
        else if (_destination is { } mountingDestination)
        {
            pose = mountingDestination;
            _host.Interiors!.Select(_host.Context!.World.Spatial.MediumAt(pose.Position), pose.Position);
            tree.Select(pose.Position, 0, pose.FovDeg ?? 60, LodLevel.L0, _refined, _required, _refined);
            _host.Participants.SelectParticipants(pose, _required, destination: false, live: false,
                walkingDestination: _destinationState?.CollisionFeet ?? WalkingDestination(mountingDestination, _destinationMode));
        }
        else _refined.Clear();
    }
    private void SelectPrediction(LodTree tree, Pose pose, bool live, bool exempt)
    {
        // Prediction is scheduling demand, not geometry that must be presented at the current eye.
        _viewRequired.Clear(); _viewRequired.UnionWith(_required);
        if (live && !exempt)
            for (var i = 0; i < _settings.PrefetchSeconds.Length; i++)
            {
                var seconds = _settings.PrefetchSeconds[i];
                var map = _host.Context!.Descriptor;
                var predicted = pose.Position + _velocity * seconds;
                predicted.Y = Math.Clamp(predicted.Y, map.Roam.FloorY, map.Roam.CeilY);
                var offset = new N.Vector2(predicted.X, predicted.Z) - map.Center;
                if (offset.Length() > map.Roam.MaxRadius)
                {
                    var limit = map.Center + N.Vector2.Normalize(offset) * map.Roam.MaxRadius;
                    predicted.X = limit.X; predicted.Z = limit.Y;
                }
                var history = _predictionRefined[i];
                tree.Select(predicted, _host.Camera.Speed, pose.FovDeg ?? 60, _speed.Minimum, history, _predictionRequired, history);
                _host.Participants.SelectParticipants(pose with { Position = predicted }, _predictionRequired, destination: false, live: false);
                _required.UnionWith(_predictionRequired);
            }
        else
            foreach (var history in _predictionRefined) history.Clear();
    }
    private void SelectArrival(LodTree tree)
    {
        if (!_destinationConsumed && _destination is { } destination)
        {
            tree.Select(destination.Position, 0, destination.FovDeg ?? 60, LodLevel.L0, _destinationRefined, _destinationRequired, _destinationRefined);
            _host.Participants.SelectParticipants(destination, _destinationRequired, destination: true, live: false,
                walkingDestination: _destinationState?.CollisionFeet ?? WalkingDestination(destination, _destinationMode));
            _destinationPlanned = true;
        }
    }

    internal void Update(double delta)
    {
        if (!_destinationConsumed && _destination is { } target && DestinationReady &&
            _host.Camera.ActiveTransit is null && _host.Camera.Mode == _destinationMode &&
            _host.CanUseLiveCamera() && N.Vector3.DistanceSquared(_host.Camera.Pose.Position, target.Position) <= 4)
        {
            _destinationConsumed = true; _destinationRequired.Clear(); _destinationRefined.Clear();
            _host.Interiors?.Prefetch(null, target.Position); _replan = _settings.ReplanSeconds;
        }
        var step = (float)Math.Min(delta, .1);
        if (_haveCamera && step > 0)
        {
            var measured = (_host.Camera.Pose.Position - _lastCamera) / step;
            var length = measured.Length();
            var maximum = Math.Max(1, _host.Camera.Speed) * 1.5f;
            if (length > maximum) measured *= maximum / length;
            _velocity = N.Vector3.Lerp(_velocity, measured, step / (.3f + step));
        }
        _lastCamera = _host.Camera.Pose.Position; _haveCamera = true;
        var exempt = _host.Camera.Mode switch
        {
            ViewMode.Fly => _host.Camera.ActiveTransit is not null,
            ViewMode.Walk => true,
            _ => throw new ArgumentOutOfRangeException(nameof(_host.Camera.Mode))
        };
        _speed.Step(_host.Camera.Speed, (float)Math.Min(delta, .1), exempt);
        _replan += (float)Math.Min(delta, .1);
        if (_replan >= _settings.ReplanSeconds)
        {
            _replan = 0; Replan(exempt);
        }
    }

    internal void Prefetch(Pose destination, ViewMode mode, NavSpace? space = null, System.Numerics.Vector3? collisionFeet = null)
    {
        if (_host.Context is null) throw new InvalidOperationException("A map must be mounted before destination prefetch.");
        _destination = destination; _destinationMode = mode; _destinationState = new(destination, mode, space, collisionFeet);
        _selectionApplied = false; _planReady = false; CancelTerrainPlan();
        _destinationPlanned = false; _destinationConsumed = false; _destinationRequired.Clear(); _destinationRefined.Clear();
        _destinationSequence++; _host.Preparation.ClearRequests();
        _host.Interiors?.Prefetch(null, destination.Position);
        _replan = _settings.ReplanSeconds;
    }

    internal void ClearDestination()
    {
        CancelTerrainPlan();
        _destination = null; _destinationState = null;
        _plannedContext = null; _plannedWorld = null;
        _planReady = _selectionApplied = _destinationPlanned = _destinationConsumed = false;
        _destinationSequence++; _interiorDestinationSequence = -1;
        foreach (var tile in _visibility.Drawn)
            if (_host.Residency.Groups.TryGetValue(tile, out var group))
                foreach (var bundle in group.Bundles) bundle.SetVisible(false, 0);
        ClearDemand();
        _host.Interiors?.ClearDestination();
        _host.Preparation.ClearRequests();
        _haveCamera = false; _velocity = N.Vector3.Zero; _speed.Reset();
        RequestReplan();
    }

    internal void Load(IMapContext context)
    {
        ClearDemand();
        _speed.Reset(); _selectionApplied = false; _destinationConsumed = false;
        _haveCamera = false; _velocity = N.Vector3.Zero;
        _tree = new(context.Descriptor.Bounds, _settings);
        _visibility = new(_settings.FadeSeconds);
        RequestReplan();
    }
    internal void Unload()
    {
        _destination = null; _destinationState = null; _tree = null;
        _plannedContext = null; _plannedWorld = null;
        _selectionApplied = false; _destinationPlanned = false; _destinationConsumed = false;
        _destinationSequence++;
    }
    internal void ClearDemand()
    {
        _required.Clear(); _viewRequired.Clear(); _refined.Clear();
        _predictionRequired.Clear();
        foreach (var history in _predictionRefined) history.Clear();
        _destinationRequired.Clear(); _destinationRefined.Clear(); _displayRefined.Clear(); _visible.Clear(); _visibility.Clear();
    }
    internal void ResetWorld()
    {
        CancelTerrainPlan(); _planReady = false; _selectionApplied = false;
        _speed.Reset(); RequestReplan(); _haveCamera = false; _velocity = N.Vector3.Zero;
    }
    internal void BeginMapChange() { StopPlanning(); _interiorDestinationSequence = -1; }
    internal void StopPlanning() { CancelTerrainPlan(); _planReady = false; }
    internal void RequestReplan() => _replan = _settings.ReplanSeconds;
    internal void ApplyVisibility()
    {
        ResolveVisible();
        // Capture keeps the existing no-fade path and hides retired detail immediately.
        _visibility.Apply(_visible, FrameClock.FadeSeconds, !_host.CaptureMode, _changeVisibility);
        _selectionApplied = true;
    }
    private bool TileReady(StreamTile tile) => _host.Residency.IsReady(tile);
    private void ChangeVisibility(StreamTile tile, bool visible, float fade)
    {
        foreach (var bundle in _host.Residency.Groups[tile].Bundles) bundle.SetVisible(visible, fade);
    }
    private sealed record TerrainPlan(IMapContext Context, IWorldModel World, long Generation, long Sequence,
        Pose Pose, bool Live, Task Preparation, CancellationTokenSource Cancellation);

}
