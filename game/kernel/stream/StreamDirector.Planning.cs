using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Bake;
using Campus.Kernel.Lod;
using N = System.Numerics;

namespace Campus.Kernel.Stream;

public partial class StreamDirector
{
    private void Replan(bool exempt)
    {
        _selectionApplied = false;
        var pose = _camera.Pose;
        var live = CanUseLiveCamera();
        if (!live && _destination is { } mounting) pose = mounting;
        if (!PreparePlanningTerrain(pose, live)) return;
        var tree = _tree ?? throw new InvalidOperationException("A map tree is required before planning.");
        var viewport = GetViewport();
        var height = viewport.GetVisibleRect().Size.Y * viewport.Scaling3DScale;
        if (!_destinationConsumed && _destination is { } accepted && _interiorDestinationSequence != _destinationSequence)
        {
            var space = _destinationState!.Space;
            if (space is null && _context!.World.Spatial.MediumAt(accepted.Position) is { Kind: MediumKind.Interior, Feature: { } building } medium)
                space = new(building, checked((short)medium.Storey));
            _destinationState = new(accepted, _destinationMode, space);
            _interiors!.Prefetch(space);
            _interiorDestinationSequence = _destinationSequence;
        }
        _required.Clear(); _refined.Clear(); _required.Add(StreamTile.Root);
        ResetAuxiliaryDemand();
        if (live)
        {
            _interiors!.Select(_context!.World.Spatial.MediumAt(pose.Position));
            tree.Select(pose.Position, exempt ? 0 : _camera.Speed, height, pose.FovDeg ?? 60, _speed.Minimum, _required, _refined);
            SelectAuxiliary(pose, _required, destination: false, live: true);
        }
        else if (_destination is { } mountingDestination)
        {
            pose = mountingDestination;
            _interiors!.Select(_context!.World.Spatial.MediumAt(pose.Position));
            tree.Select(pose.Position, 0, height, pose.FovDeg ?? 60, LodLevel.L0, _required, _refined);
            SelectAuxiliary(pose, _required, destination: false, live: false);
        }
        // Prediction is scheduling demand, not geometry that must be presented at the current eye.
        _viewRequired.Clear(); _viewRequired.UnionWith(_required);
        if (live && !exempt)
            foreach (var seconds in _settings.PrefetchSeconds)
            {
                var map = _context!.Descriptor;
                var predicted = pose.Position + _velocity * seconds;
                predicted.Y = Math.Clamp(predicted.Y, map.Roam.FloorY, map.Roam.CeilY);
                var offset = new N.Vector2(predicted.X, predicted.Z) - map.Center;
                if (offset.Length() > map.Roam.MaxRadius)
                {
                    var limit = map.Center + N.Vector2.Normalize(offset) * map.Roam.MaxRadius;
                    predicted.X = limit.X; predicted.Z = limit.Y;
                }
                tree.Select(predicted, _camera.Speed, height, pose.FovDeg ?? 60, _speed.Minimum, _predictionRequired, _predictionRefined);
                SelectAuxiliary(pose with { Position = predicted }, _predictionRequired, destination: false, live: false);
                _required.UnionWith(_predictionRequired);
            }
        if (!_destinationConsumed && _destination is { } destination)
        {
            tree.Select(destination.Position, 0, height, destination.FovDeg ?? 60, LodLevel.L0, _destinationRequired, _destinationRefined);
            SelectAuxiliary(destination, _destinationRequired, destination: true, live: false);
            _destinationPlanned = true;
        }
        var wanted = _required.Concat(_destinationRequired).Distinct().Order().ToArray();
        var requests = new List<StreamRequest>();
        foreach (var tile in wanted)
        {
            var group = GetGroup(tile);
            if (!group.GeometryRequested)
            {
                group.GeometryRequested = true; group.Expected = _catalog!.Modules(tile).Count;
                group.Mounted = group.Expected == 0; // Empty quadrants are valid geometry coverage.
            }
            var priority = tile == StreamTile.Root || _destinationRequired.Contains(tile) ? BakePriority.KnownDestination : BakePriority.Normal;
            if (group.Error is not null && priority == BakePriority.KnownDestination) _error ??= group.Error;
            group.Priority = priority;
            var distance = MathF.Sqrt(tree.Bounds(tile).DistanceSquared(pose.Position));
            var toTile = tree.Bounds(tile).Center - pose.Position;
            var approach = toTile.LengthSquared() > 0 ? N.Vector3.Dot(_velocity, N.Vector3.Normalize(toTile)) : 0;
            var radius = _settings.Distances[Math.Min((int)tile.Level, 3)];
            var neededIn = (distance - radius) / Math.Max(1, approach);
            foreach (var module in _catalog!.Modules(tile))
            {
                var key = BakeRequestKeys.Tile(_context!.Descriptor.Id, module, tile.Tile, tile.Level);
                group.Keys.Add(key);
                if (!_issued.Contains(key)) requests.Add(new(key, tile, priority, neededIn));
                // Re-Ensure promotes an already issued background request without restarting it.
                else
                {
                    PromotePreload(new(key, tile, priority, neededIn), group, null);
                    if (priority == BakePriority.KnownDestination) _bake.Ensure(key, priority);
                }
            }
        }
        QueueAuxiliary(requests, pose);
        QueuePinned(requests, height);
        _requests.Replace(requests);
        _checkRetention = true;
        _plannedContext = _context; _plannedWorld = _context!.World; _plannedPose = pose;
        _plannedLive = live; _plannedSequence = _destinationSequence; _planReady = true;
    }
}
