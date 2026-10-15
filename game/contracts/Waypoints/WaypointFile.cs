namespace Campus.Contracts;

public sealed record WaypointFile(int Version, IReadOnlyList<Waypoint> Waypoints);
