using System.Globalization;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;

namespace Campus.Gen.Interior;

/// <summary>Mainline double-loaded dormitory wings. Complex wings and landmark halls stay explicitly authored.</summary>
public static class InteriorPlanGenerator
{
    public static InteriorPlan Generate(InteriorPlanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new Builder(request).Build();
    }

    private sealed class Builder
    {
        private readonly InteriorPlanRequest _request;
        private readonly DormitoryPlanOptions _options;
        private readonly FacadeGrid _grid;
        private readonly Frame _frame;
        private readonly StoreyInfo[] _storeys;
        private readonly FacadeBay[] _near;
        private readonly FacadeBay[] _far;
        private readonly List<ZoneVolume> _zones = [];
        private readonly List<PortalDecl> _portals = [];
        private readonly List<DoorLeafDecl> _doors = [];
        private readonly List<InteriorRoom> _rooms = [];
        private readonly List<InteriorWindow> _windows = [];
        private readonly List<InteriorWallSpan> _walls = [];
        private readonly List<InteriorSlab> _slabs = [];
        private readonly List<InteriorStair> _stairs = [];
        private readonly List<InteriorRoomUse> _usedRooms = [];
        private readonly Dictionary<int, int[]> _coreZones = [];
        private readonly float _left;
        private readonly float _right;
        private readonly float _corridorLow;
        private readonly float _corridorHigh;

        internal Builder(InteriorPlanRequest request)
        {
            _request = request;
            _options = request.Options;
            ValidateRequest(request);
            _grid = FacadeGridGenerator.Generate(request.Footprint, _options.Facade);
            _frame = Frame.From(_grid.Footprint);
            _storeys = request.Storeys.OrderBy(s => s.Index).Select(s => new StoreyInfo(s.Index,
                PlanGeometry.Quantize(s.FloorY), PlanGeometry.Quantize(s.Height), _grid.Footprint.Outer)).ToArray();
            _near = _grid.Bays.Where(b => b.Edge == _frame.Edge && b.Ring == 0).ToArray();
            _far = _grid.Bays.Where(b => b.Edge == (_frame.Edge + 2) % 4 && b.Ring == 0)
                .OrderBy(b => _frame.U(b.Center)).ToArray();
            if (_near.Length < 4 || _near.Length != _far.Length)
                throw new ArgumentException("A dormitory wing needs at least four paired facade bays.", nameof(request));
            _left = _frame.U(_near[0].B);
            _right = _frame.U(_near[^1].A);
            _corridorLow = (_frame.Depth - _options.CorridorClearWidth - _options.PartitionThickness) * 0.5f;
            _corridorHigh = (_frame.Depth + _options.CorridorClearWidth + _options.PartitionThickness) * 0.5f;
            float minimumCoreWidth = 2f * _options.StairFlightWidth + _options.StairWellWidth + 2f * _options.ExteriorWallThickness;
            if (_left < minimumCoreWidth - PlanGeometry.Tolerance || _frame.Length - _right < minimumCoreWidth - PlanGeometry.Tolerance)
                throw new ArgumentException("End bays are too narrow for the specified double-flight stairs.", nameof(request));
            if (_corridorLow - _options.PartitionThickness * 0.5f - _options.ExteriorWallThickness < 3f)
                throw new ArgumentException("The wing is too shallow for double-loaded rooms.", nameof(request));
            if ((_right - _left + _options.CorridorClearWidth) * 0.5f > 40f)
                throw new ArgumentException("This wing needs an additional traffic core to keep dormitory door-to-stair travel within 40 m.", nameof(request));
        }

        internal InteriorPlan Build()
        {
            foreach (StoreyInfo storey in _storeys) BuildStorey(storey);
            if (_usedRooms.Count != _request.Rooms.Count)
                throw new ArgumentException("A room selection references an unknown storey, short facade or reserved stair bay.");
            for (int i = 0; i + 1 < _storeys.Length; i++)
                for (int side = 0; side < 2; side++) BuildStair(_storeys[i], _storeys[i + 1], side);
            foreach (StoreyInfo storey in _storeys)
            {
                BuildFloorSlabs(storey);
                BuildExterior(storey);
            }
            AddSeals();
            EntranceDecl[] entrances = _request.Entrances.OrderBy(e => e.Id, StringComparer.Ordinal).Select(e => e with
            {
                Center = PlanGeometry.Quantize(e.Center),
                Outside = PlanGeometry.Quantize(e.Outside),
                Inside = PlanGeometry.Quantize(e.Inside),
                ClearWidth = PlanGeometry.Quantize(e.ClearWidth),
                YawDeg = PlanGeometry.Quantize(e.YawDeg)
            }).ToArray();
            InteriorPlan plan = new(_request.Feature, _request.WingId, _grid, _storeys, _zones.ToArray(),
                _portals.ToArray(), _doors.ToArray(), entrances,
                _rooms.ToArray(), _windows.ToArray(), _walls.ToArray(), _slabs.ToArray(), _stairs.ToArray(), 0);
            InteriorPlanValidator.Validate(plan, _options);
            return plan with { PlanHash = InteriorPlanHash.Compute(_request.WorldSeed, plan) };
        }

        private void BuildStorey(StoreyInfo s)
        {
            float half = _options.PartitionThickness * 0.5f;
            int corridor = AddZone(s, ZoneKind.Corridor,
                _frame.Ring(_left + half, _corridorLow + half, _right - half, _corridorHigh - half), true, "corridor");
            int leftStair = AddZone(s, ZoneKind.Stair,
                _frame.Ring(_options.ExteriorWallThickness, _options.ExteriorWallThickness,
                    _left - half, _frame.Depth - _options.ExteriorWallThickness), true, "stair.left");
            int rightStair = AddZone(s, ZoneKind.Stair,
                _frame.Ring(_right + half, _options.ExteriorWallThickness,
                    _frame.Length - _options.ExteriorWallThickness, _frame.Depth - _options.ExteriorWallThickness), true, "stair.right");
            _coreZones.Add(s.Index, [corridor, leftStair, rightStair]);
            for (int side = 0; side < 2; side++)
            {
                float u = side == 0 ? _left : _right;
                int stair = side == 0 ? leftStair : rightStair;
                float center = _frame.Depth * 0.5f;
                DoorWall(s, _frame.Point(u, _options.ExteriorWallThickness), _frame.Point(u, _frame.Depth - _options.ExteriorWallThickness),
                    _frame.Point(u, center - _options.DoorClearWidth * 0.5f), _frame.Point(u, center + _options.DoorClearWidth * 0.5f),
                    corridor, stair, true, DoorLeafKind.FireDoor, "stair." + side.ToString(CultureInfo.InvariantCulture));
            }
            for (int row = 0; row < 2; row++)
                for (int bay = 1; bay + 1 < _near.Length; bay++) BuildRoom(s, corridor, row, bay);
            foreach (FacadeBay bay in _grid.Bays)
            {
                var room = _rooms.LastOrDefault(r => r.Storey == s.Index && r.FacadeBayIndex == bay.Index);
                int zone;
                FacadeWindowKind kind;
                if (room is not null)
                {
                    zone = room.ZoneId;
                    kind = _zones.Single(z => z.Id == zone).Kind switch
                    {
                        ZoneKind.Room or ZoneKind.Lobby or ZoneKind.Gallery => FacadeWindowKind.Room,
                        ZoneKind.Service or ZoneKind.Wc => FacadeWindowKind.Service,
                        _ => throw new InvalidOperationException("Unexpected ordinary room kind.")
                    };
                }
                else
                {
                    zone = _frame.U(bay.Center) < _frame.Length * 0.5f ? leftStair : rightStair;
                    kind = FacadeWindowKind.Stair;
                }
                _windows.Add(new(s.Index, bay.Index, zone, bay.WindowKind == FacadeWindowKind.Blank ? FacadeWindowKind.Blank : kind));
            }
        }

        private void BuildRoom(StoreyInfo s, int corridor, int row, int bay)
        {
            FacadeBay facade = row == 0 ? _near[bay] : _far[bay];
            InteriorRoomUse? use = _request.Rooms.SingleOrDefault(r => r.Storey == s.Index && r.FacadeBayIndex == facade.Index);
            if (use is not null) _usedRooms.Add(use);
            float u0 = _frame.U(_near[bay].A), u1 = _frame.U(_near[bay].B);
            float wallV = row == 0 ? _corridorLow : _corridorHigh;
            float half = _options.PartitionThickness * 0.5f;
            float v0 = row == 0 ? _options.ExteriorWallThickness : wallV + half;
            float v1 = row == 0 ? wallV - half : _frame.Depth - _options.ExteriorWallThickness;
            Vector2[] clear = _frame.Ring(u0 + half, v0, u1 - half, v1);
            bool open = use?.Open ?? false;
            string key = "room." + facade.Key;
            int zone = AddZone(s, use?.Kind ?? ZoneKind.Room, clear, open, key);
            _rooms.Add(new(s.Index, zone, facade.Index, use?.Type ?? "dorm.four", clear, open));
            float width = open ? _options.DoorClearWidth : 0.95f;
            float center = (u0 + u1) * 0.5f;
            DoorWall(s, _frame.Point(u0, wallV), _frame.Point(u1, wallV),
                _frame.Point(center - width * 0.5f, wallV), _frame.Point(center + width * 0.5f, wallV),
                corridor, zone, open, use?.DoorKind ?? DoorLeafKind.RoomDoor, key);
            if (bay + 2 < _near.Length)
                Wall(s, _frame.Point(u1, v0), _frame.Point(u1, v1), s.FloorY, s.FloorY + s.Height, InteriorWallRole.Partition);
        }

        private int AddZone(StoreyInfo s, ZoneKind kind, Vector2[] ring, bool open, string role)
        {
            int id = checked(_request.ZoneIdBase + _zones.Count);
            uint hash = StableHash.Hash32(_request.WorldSeed, _request.Feature.Value, "interior", "light", _request.WingId,
                s.Index.ToString(CultureInfo.InvariantCulture), role);
            _zones.Add(new(s.Index, id, kind, ring, _options.LightChannel, hash, open));
            return id;
        }

        private void DoorWall(StoreyInfo s, Vector2 a, Vector2 b, Vector2 doorA, Vector2 doorB,
            int zoneA, int zoneB, bool open, DoorLeafKind kind, string key)
        {
            Wall(s, a, doorA, s.FloorY, s.FloorY + s.Height, InteriorWallRole.Partition);
            Wall(s, doorB, b, s.FloorY, s.FloorY + s.Height, InteriorWallRole.Partition);
            Wall(s, doorA, doorB, s.FloorY + _options.DoorHeight, s.FloorY + s.Height, InteriorWallRole.Partition);
            _portals.Add(new(s.Index, zoneA, zoneB, At(doorA, s.FloorY), At(doorB, s.FloorY),
                _options.DoorHeight, open ? PortalKind.Door : PortalKind.Sealed));
            if (!open)
            {
                Wall(s, doorA, doorB, s.FloorY, s.FloorY + _options.DoorHeight, InteriorWallRole.LockedDoor, 0.05f, leafKind: kind);
                return;
            }
            Vector2 d = doorB - doorA;
            float yaw = DetMath.Atan2(-d.Y, d.X) * (180f / float.Pi);
            string id = string.Join('/', _request.WingId, s.Index.ToString(CultureInfo.InvariantCulture), key);
            // ClosedYawDeg rotates the local +X hinge-to-latch axis about +Y.
            _doors.Add(new(id, s.Index, zoneA, zoneB, At(doorA, s.FloorY), PlanGeometry.Quantize(yaw),
                90f, PlanGeometry.Quantize(PlanGeometry.Length(d)), _options.DoorHeight, kind));
        }

        private void BuildStair(StoreyInfo lower, StoreyInfo upper, int side)
        {
            float halfRise = (upper.FloorY - lower.FloorY) * 0.5f;
            int steps = (int)Math.Ceiling(halfRise / _options.MaximumRiserHeight);
            if (steps is < 3 or > 18) throw new ArgumentException("Each stair flight must contain 3 to 18 risers.");
            float run = steps * _options.StairTreadDepth;
            float start = _frame.Depth * 0.5f + _options.StairLandingDepth * 0.5f;
            float end = start + run;
            float wellEnd = end + _options.StairLandingDepth;
            if (wellEnd > _frame.Depth - _options.ExteriorWallThickness)
                throw new ArgumentException("The wing does not have enough depth for the stair landing and flights.");
            float u0 = side == 0 ? _options.ExteriorWallThickness : _right + _options.PartitionThickness * 0.5f;
            float u1 = u0 + _options.StairFlightWidth;
            float u2 = u1 + _options.StairWellWidth;
            float u3 = u2 + _options.StairFlightWidth;
            float middleY = lower.FloorY + halfRise;
            float buryRun = 0.2f * run / halfRise;
            RampCollider flight1 = Ramp(u0, u1, start - buryRun, end, lower.FloorY - 0.2f, middleY, steps, halfRise);
            RampCollider flight2 = Ramp(u2, u3, end + buryRun, start, middleY - 0.2f, upper.FloorY, steps, halfRise);
            InteriorSlab landing = new(lower.Index, _frame.Ring(u0, end, u3, wellEnd), PlanGeometry.Quantize(middleY), _options.SlabThickness);
            Vector2[] opening = _frame.Ring(u0, start, u3, wellEnd);
            int lowerZone = _coreZones[lower.Index][side + 1], upperZone = _coreZones[upper.Index][side + 1];
            _stairs.Add(new(lower.Index, upper.Index, lowerZone, upperZone, flight1, flight2, landing, opening));
            _portals.Add(new(upper.Index, lowerZone, upperZone, _frame.Point3(u2, upper.FloorY, start),
                _frame.Point3(u3, upper.FloorY, start), 2.2f, PortalKind.StairOpening));
            Wall(lower, _frame.Point((u1 + u2) * 0.5f, start), _frame.Point((u1 + u2) * 0.5f, end),
                lower.FloorY, upper.FloorY + 1.5f, InteriorWallRole.StairBarrier, _options.StairWellWidth);
            Wall(upper, _frame.Point(u0, wellEnd), _frame.Point(u3, wellEnd),
                upper.FloorY, upper.FloorY + 1.5f, InteriorWallRole.StairBarrier, 0.1f);
            // Intermediate landings reuse this lane for the next upward flight.
            // Only the terminal storey needs a barrier across the first-flight entrance.
            if (upper.Index == _storeys[^1].Index)
                Wall(upper, _frame.Point(u0, start), _frame.Point(u2, start),
                    upper.FloorY, upper.FloorY + 1.5f, InteriorWallRole.StairBarrier, 0.1f);

            RampCollider Ramp(float left, float right, float from, float to, float bottom, float top, int count, float rise) =>
                new(_frame.Point3(left, bottom, from), _frame.Point3(right, bottom, from),
                    _frame.Point3(right, top, to), _frame.Point3(left, top, to), _options.SlabThickness, SurfaceTag.Stair)
                { Foot = FootMat.Concrete, Stair = new(rise / count, _options.StairTreadDepth, count) };
        }

        private void BuildFloorSlabs(StoreyInfo s)
        {
            var holes = _stairs.Where(stair => stair.UpperStorey == s.Index)
                .Select(stair => (U0: stair.FloorOpening.Min(_frame.U), U1: stair.FloorOpening.Max(_frame.U),
                    V0: stair.FloorOpening.Min(_frame.V), V1: stair.FloorOpening.Max(_frame.V)))
                .OrderBy(h => h.U0).ToArray();
            float cursor = 0f;
            foreach (var hole in holes)
            {
                Slab(cursor, 0, hole.U0, _frame.Depth);
                Slab(hole.U0, 0, hole.U1, hole.V0);
                Slab(hole.U0, hole.V1, hole.U1, _frame.Depth);
                cursor = hole.U1;
            }
            Slab(cursor, 0, _frame.Length, _frame.Depth);
            void Slab(float u0, float v0, float u1, float v1)
            {
                if (u1 - u0 <= 0.002f || v1 - v0 <= 0.002f) return;
                _slabs.Add(new(s.Index, _frame.Ring(u0, v0, u1, v1), s.FloorY, _options.SlabThickness));
            }
        }

        private void BuildExterior(StoreyInfo s)
        {
            for (int edge = 0; edge < 4; edge++)
            {
                Vector2 a = _grid.Footprint.Outer[edge], b = _grid.Footprint.Outer[(edge + 1) % 4];
                Vector2 tangent = PlanGeometry.Unit(b - a), inward = new(-tangent.Y, tangent.X);
                float length = PlanGeometry.Length(b - a);
                var openings = new List<Opening>();
                foreach (EntranceDecl entry in _request.Entrances.Where(e => MathF.Abs(e.Center.Y - s.FloorY) <= PlanGeometry.Tolerance)
                    .OrderBy(e => e.Id, StringComparer.Ordinal))
                {
                    Vector2 center = new(entry.Center.X, entry.Center.Z);
                    if (!PlanGeometry.OnSegment(center, a, b)) continue;
                    float distance = (float)PlanGeometry.Dot(center - a, tangent);
                    float start = distance - entry.ClearWidth * 0.5f, end = distance + entry.ClearWidth * 0.5f;
                    if (start < 0.05f || end > length - 0.05f) throw new ArgumentException("An entrance crosses a wing corner.");
                    int zone = ZoneAt(s.Index, center + inward * (_options.ExteriorWallThickness + 0.1f));
                    if (entry.Open && !_zones.Single(z => z.Id == zone).Open) throw new ArgumentException("An open entrance leads into a locked room.");
                    openings.Add(new(start, end, 0f, _options.DoorHeight, entry.Open ? null : InteriorWallRole.LockedDoor));
                    _portals.Add(new(s.Index, -1, zone, At(PlanGeometry.Quantize(a + tangent * start), s.FloorY),
                        At(PlanGeometry.Quantize(a + tangent * end), s.FloorY), _options.DoorHeight,
                        entry.Open ? PortalKind.Door : PortalKind.Sealed));
                }
                foreach (FacadeBay bay in _grid.Bays.Where(bay => bay.Edge == edge && bay.WindowKind != FacadeWindowKind.Blank))
                {
                    float start = (float)PlanGeometry.Dot(bay.OpeningA - a, tangent);
                    float end = (float)PlanGeometry.Dot(bay.OpeningB - a, tangent);
                    if (openings.Any(o => o.Start < end && o.End > start))
                    {
                        int windowIndex = _windows.FindIndex(w => w.Storey == s.Index && w.FacadeBayIndex == bay.Index);
                        _windows[windowIndex] = _windows[windowIndex] with { Kind = FacadeWindowKind.Blank };
                        continue;
                    }
                    openings.Add(new(start, end, bay.SillHeight, bay.SillHeight + bay.WindowHeight, InteriorWallRole.WindowGlass));
                    int zone = _windows.Single(w => w.Storey == s.Index && w.FacadeBayIndex == bay.Index).ZoneId;
                    _portals.Add(new(s.Index, zone, -1, At(bay.OpeningA, s.FloorY + bay.SillHeight),
                        At(bay.OpeningB, s.FloorY + bay.SillHeight), bay.WindowHeight, PortalKind.Window));
                }
                openings = openings.OrderBy(o => o.Start).ToList();
                float cursor = 0f;
                foreach (Opening opening in openings)
                {
                    if (opening.Start < cursor - PlanGeometry.Tolerance) throw new ArgumentException("Exterior openings overlap.");
                    Exterior(cursor, opening.Start, 0f, s.Height, InteriorWallRole.Exterior);
                    Exterior(opening.Start, opening.End, 0f, opening.Bottom, InteriorWallRole.Exterior);
                    Exterior(opening.Start, opening.End, opening.Top, s.Height, InteriorWallRole.Exterior);
                    if (opening.Fill is InteriorWallRole role) Exterior(opening.Start, opening.End, opening.Bottom, opening.Top, role);
                    cursor = opening.End;
                }
                Exterior(cursor, length, 0f, s.Height, InteriorWallRole.Exterior);
                void Exterior(float from, float to, float bottom, float top, InteriorWallRole role)
                {
                    if (to - from <= 0.002f || top - bottom <= 0.002f) return;
                    Vector2 offset = inward * (role == InteriorWallRole.WindowGlass ? 0.01f : _options.ExteriorWallThickness * 0.5f);
                    Wall(s, a + tangent * from + offset, a + tangent * to + offset,
                        s.FloorY + bottom, s.FloorY + top, role,
                        role == InteriorWallRole.WindowGlass ? 0.02f : _options.ExteriorWallThickness);
                }
            }
        }

        private void AddSeals()
        {
            foreach (SealOp seal in _request.Seals.OrderBy(s => s.Storey).ThenBy(s => s.A.X).ThenBy(s => s.A.Y)
                .ThenBy(s => s.B.X).ThenBy(s => s.B.Y).ThenBy(s => s.Kind))
            {
                StoreyInfo storey = _storeys.Single(s => s.Index == seal.Storey);
                if (!PlanGeometry.Contains(_grid.Footprint.Outer, seal.A) || !PlanGeometry.Contains(_grid.Footprint.Outer, seal.B))
                    throw new ArgumentException("A seal is outside its wing.");
                if (_portals.Any(p => p.Storey == seal.Storey && p.Kind == PortalKind.Door &&
                    PlanGeometry.SegmentsTouch(seal.A, seal.B, new(p.A.X, p.A.Z), new(p.B.X, p.B.Z))))
                    throw new ArgumentException("A seal intersects a declared open doorway; resolve the opening in the caller configuration.");
                if (_zones.Any(z => z.Storey == seal.Storey && z.Open &&
                    new[] { 0.25d, 0.5d, 0.75d }.Any(t => PlanGeometry.Contains(z.Ring, PlanGeometry.Lerp(seal.A, seal.B, t), false))))
                    throw new ArgumentException("A seal divides an open zone; split that space in the owning provider rather than claiming false connectivity.");
                Wall(storey, seal.A, seal.B, storey.FloorY, storey.FloorY + storey.Height, InteriorWallRole.Seal, closure: seal.Kind);
                _portals.Add(new(storey.Index, -1, -1, At(seal.A, storey.FloorY), At(seal.B, storey.FloorY), storey.Height, PortalKind.Sealed));
            }
        }

        private int ZoneAt(int storey, Vector2 point)
        {
            ZoneVolume? zone = _zones.FirstOrDefault(z => z.Storey == storey && PlanGeometry.Contains(z.Ring, point));
            return zone?.Id ?? throw new ArgumentException("An entrance lands in a wall or outside the generated clear zones.");
        }

        private void Wall(StoreyInfo s, Vector2 a, Vector2 b, float bottom, float top, InteriorWallRole role, float? thickness = null,
            SealKind? closure = null, DoorLeafKind? leafKind = null)
        {
            a = PlanGeometry.Quantize(a); b = PlanGeometry.Quantize(b);
            if (PlanGeometry.Length(b - a) <= 0.002f || top - bottom <= 0.002f) return;
            _walls.Add(new(s.Index, a, b, PlanGeometry.Quantize(thickness ?? _options.PartitionThickness),
                PlanGeometry.Quantize(bottom), PlanGeometry.Quantize(top), role)
            {
                Closure = closure,
                LeafKind = leafKind
            });
        }

        private static Vector3 At(Vector2 point, float y) => PlanGeometry.Quantize(new Vector3(point.X, y, point.Y));
        private sealed record Opening(float Start, float End, float Bottom, float Top, InteriorWallRole? Fill);
    }

    private static void ValidateRequest(InteriorPlanRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Feature.Value) || string.IsNullOrWhiteSpace(r.WingId) || r.WingId.Contains('/'))
            throw new ArgumentException("A feature and slash-free wing ID are required.", nameof(r));
        ArgumentNullException.ThrowIfNull(r.Options);
        ArgumentNullException.ThrowIfNull(r.Storeys);
        ArgumentNullException.ThrowIfNull(r.Rooms);
        ArgumentNullException.ThrowIfNull(r.Entrances);
        ArgumentNullException.ThrowIfNull(r.Seals);
        var o = r.Options;
        ArgumentNullException.ThrowIfNull(o.Facade);
        if (r.ZoneIdBase < 0 || r.ZoneIdBase > int.MaxValue - 65536)
            throw new ArgumentOutOfRangeException(nameof(r), "Reserve room for the wing's zone IDs.");
        float[] dimensions = [o.CorridorClearWidth, o.ExteriorWallThickness, o.PartitionThickness, o.SlabThickness,
            o.DoorClearWidth, o.DoorHeight, o.StairFlightWidth, o.StairWellWidth, o.StairLandingDepth, o.StairTreadDepth, o.MaximumRiserHeight];
        if (dimensions.Any(d => !float.IsFinite(d) || d <= 0f) || o.CorridorClearWidth < 2.2f || o.DoorClearWidth < 1.1f ||
            o.DoorHeight < 2f || o.StairFlightWidth < 1.2f || o.StairLandingDepth < MathF.Max(o.StairFlightWidth, 1.5f) ||
            o.StairWellWidth > 0.2f || o.MaximumRiserHeight > 0.165f || o.StairTreadDepth < 0.27f)
            throw new ArgumentException("Dormitory dimensions violate the mainline clearances.", nameof(r));
        StoreyInfo[] storeys = r.Storeys.OrderBy(s => s.Index).ToArray();
        if (storeys.Length is < 1 or > 32 || storeys.Select(s => s.Index).Distinct().Count() != storeys.Length)
            throw new ArgumentException("Supply 1 to 32 distinct consecutive storeys.", nameof(r));
        for (int i = 0; i < storeys.Length; i++)
        {
            StoreyInfo s = storeys[i];
            if (!float.IsFinite(s.FloorY) || !float.IsFinite(s.Height) || s.Height < 3.6f || s.Height > 5f ||
                o.DoorHeight >= s.Height - o.SlabThickness || o.Facade.SillHeight + o.Facade.WindowHeight >= s.Height - o.SlabThickness)
                throw new ArgumentException("Invalid dormitory floor elevation, height or opening clearance.", nameof(r));
            if (i > 0 && (s.Index != storeys[i - 1].Index + 1 ||
                MathF.Abs(s.FloorY - storeys[i - 1].FloorY - storeys[i - 1].Height) > PlanGeometry.Tolerance))
                throw new ArgumentException("The mainline generator only joins consecutive, vertically aligned storeys.", nameof(r));
        }
        if (r.Rooms.Select(room => (room.Storey, room.FacadeBayIndex)).Distinct().Count() != r.Rooms.Count)
            throw new ArgumentException("Room selections must be unique.", nameof(r));
        foreach (InteriorRoomUse room in r.Rooms)
        {
            _ = room.Kind switch
            {
                ZoneKind.Room or ZoneKind.Service or ZoneKind.Wc or ZoneKind.Lobby or ZoneKind.Gallery => 0,
                _ => throw new ArgumentException("A room selection may not replace a corridor or traffic core.", nameof(r))
            };
            _ = room.DoorKind switch
            {
                DoorLeafKind.RoomDoor or DoorLeafKind.FireDoor or DoorLeafKind.GlassSwing => 0,
                _ => throw new ArgumentOutOfRangeException(nameof(r))
            };
            if (string.IsNullOrWhiteSpace(room.Type)) throw new ArgumentException("Room template keys must be non-empty.", nameof(r));
        }
        foreach (EntranceDecl entrance in r.Entrances)
        {
            if (string.IsNullOrWhiteSpace(entrance.Id) || !float.IsFinite(entrance.ClearWidth) || entrance.ClearWidth < 1.1f ||
                !storeys.Any(s => MathF.Abs(s.FloorY - entrance.Center.Y) <= PlanGeometry.Tolerance))
                throw new ArgumentException("Entrances must reference a supplied floor and have at least 1.1 m clearance.", nameof(r));
        }
        if (r.Entrances.Select(e => e.Id).Distinct(StringComparer.Ordinal).Count() != r.Entrances.Count)
            throw new ArgumentException("Entrance IDs must be unique.", nameof(r));
        foreach (SealOp seal in r.Seals)
            if (seal.Building != r.Feature || !storeys.Any(s => s.Index == seal.Storey) || PlanGeometry.Length(seal.B - seal.A) < 0.5f)
                throw new ArgumentException("A seal must reference this feature and a supplied storey.", nameof(r));
    }

    private sealed record Frame(Vector2 Origin, Vector2 Along, Vector2 Inward, float Length, float Depth, int Edge)
    {
        internal float U(Vector2 p) => (float)PlanGeometry.Dot(p - Origin, Along);
        internal float V(Vector2 p) => (float)PlanGeometry.Dot(p - Origin, Inward);
        internal Vector2 Point(float u, float v) => PlanGeometry.Quantize(Origin + Along * u + Inward * v);
        internal Vector3 Point3(float u, float y, float v) { Vector2 p = Point(u, v); return PlanGeometry.Quantize(new Vector3(p.X, y, p.Y)); }
        internal Vector2[] Ring(float u0, float v0, float u1, float v1) => [Point(u0, v0), Point(u1, v0), Point(u1, v1), Point(u0, v1)];
        internal static Frame From(PolygonRings footprint)
        {
            if (footprint.Outer.Count != 4 || footprint.Holes.Count != 0)
                throw new ArgumentException("Supply an authored rectangular wing; arbitrary footprints require explicit decomposition.");
            IReadOnlyList<Vector2> ring = footprint.Outer;
            int edge = Enumerable.Range(0, 4).OrderByDescending(i => PlanGeometry.Length(ring[(i + 1) % 4] - ring[i])).ThenBy(i => i).First();
            Vector2 origin = ring[edge], along = PlanGeometry.Unit(ring[(edge + 1) % 4] - origin), inward = new(-along.Y, along.X);
            float length = PlanGeometry.Length(ring[(edge + 1) % 4] - origin);
            float depth = (float)PlanGeometry.Dot(ring[(edge + 3) % 4] - origin, inward);
            Frame frame = new(origin, along, inward, length, depth, edge);
            if (depth <= 0f || PlanGeometry.Length(frame.Point(length, depth) - ring[(edge + 2) % 4]) > 0.005f ||
                PlanGeometry.Length(frame.Point(0f, depth) - ring[(edge + 3) % 4]) > 0.005f)
                throw new ArgumentException("The supplied wing is not rectangular within 5 mm.");
            return frame;
        }
    }
}
