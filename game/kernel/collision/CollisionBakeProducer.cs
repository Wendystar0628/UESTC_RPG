using System.Globalization;
using System.Reflection;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Gen.Collision;
using Campus.Kernel.Registry;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Collision;

/// <summary>Snapshots content/part generators on the main thread. Interiors come only from each captured map context.</summary>
// Registration activates with the formal Boot composition.
[BakeProducer("collision.world", Owner = "WP-43")]
public sealed class CollisionBakeProducer : IBakeProducer
{
    public const string Module = "collision.world";
    private readonly (Registration Registration, object Instance)[] _sources;

    public CollisionBakeProducer()
    {
        var registry = Services.Services;
        // Interior instances belong to one MapBuild, not to the global service registry.
        _sources = registry.Entries.Where(r => r.Kind is "content" or "part")
            .OrderBy(r => r.Kind, StringComparer.Ordinal).ThenBy(r => r.Id, StringComparer.Ordinal)
            .Select(r => (r, registry.Create(r.Type))).ToArray();
    }

    public bool CanBuild(BakeKey key) => key.Module == Module && key.Detail.StartsWith("C/", StringComparison.Ordinal);

    public object Build(BakeKey key, BuildContext context)
    {
        if (key.Map != context.Map.Id || !CanBuild(key)) throw new ArgumentException("Collision bake key does not match its map.");
        var tile = ParseTile(key.Detail);
        var sources = _sources.Where(s => string.IsNullOrEmpty(s.Registration.Map) || s.Registration.Map == key.Map.Value).ToArray();
        var parts = sources.Where(s => s.Registration.Kind == "part").ToDictionary(s => s.Registration.Id, StringComparer.Ordinal);
        IReadOnlyList<Collider> PartCollision(PlacedPart part)
        {
            if (!parts.TryGetValue(part.Part.Value, out var registration)) throw new InvalidOperationException($"Unregistered collision part: {part.Part.Value}.");
            var contract = registration.Instance.GetType().GetInterfaces().Single(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IPartDef<>));
            if (!contract.GenericTypeArguments[0].IsInstanceOfType(part.Params)) throw new ArgumentException($"Wrong parameters for part {part.Part.Value}.");
            var method = contract.GetMethod("Collision") ?? throw new MissingMethodException(contract.FullName, "Collision");
            try { return (IReadOnlyList<Collider>)(method.Invoke(registration.Instance, [part.Params]) ?? throw new InvalidOperationException("Part collision returned null.")); }
            catch (TargetInvocationException e) { throw new InvalidOperationException($"Collision generation failed for {part.Part.Value}.", e.InnerException ?? e); }
        }
        var builder = new CollisionTileBuilder(
            sources.Where(s => s.Registration.Kind == "content").Select(s => new CollisionModule(s.Registration.Id, (IContentModule)s.Instance)).ToArray(),
            PartCollision);
        var colliders = builder.Build(context, tile);
        var origin = CollisionGeometry.Origin(tile);
        var bounds = new Box3(new(origin.X, context.Map.Bounds.Min.Y, origin.Z), new(origin.X + 64, context.Map.Bounds.Max.Y, origin.Z + 64));
        return new CollisionTile(key, tile, bounds, colliders);
    }

    public static BakeKey Key(MapId map, TileKey tile) => new(map, Module, FormattableString.Invariant($"C/{tile.X}/{tile.Z}"));
    public static TileKey ParseTile(string detail)
    {
        var parts = detail.Split('/');
        if (parts.Length != 3 || parts[0] != "C" || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) ||
            !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var z)) throw new ArgumentException("Invalid collision tile key.");
        return new(x, z);
    }
}
