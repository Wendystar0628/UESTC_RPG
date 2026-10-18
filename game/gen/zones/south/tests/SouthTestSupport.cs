using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Gen.World;
using Campus.Gen.Zones.South.Framework;
using Campus.Gen.Tests.Support;
using GdUnit4;

namespace Campus.Gen.Zones.South.Tests;

internal static class SouthTestSupport
{
    public static bool Covers(Collider c, Vector2 p, float y) => c switch
    {
        BoxCollider box => InBox(box, p) && MathF.Abs(y - box.At.M42) <= box.Half.Y,
        WallCollider wall => DistanceToSegment(p, wall.A, wall.B) <= wall.HalfT + 0.26f && y >= wall.Y0 && y <= wall.Y1,
        PostCollider post => Vector2.Distance(p, post.C) <= post.R + 0.26f && y >= post.Y0 && y <= post.Y1,
        _ => false,
    };

    public static bool Near(IReadOnlyList<Vector2> ring, Vector2 p, float margin) =>
        p.X >= ring.Min(q => q.X) - margin && p.X <= ring.Max(q => q.X) + margin && p.Y >= ring.Min(q => q.Y) - margin && p.Y <= ring.Max(q => q.Y) + margin;

    public static float MaxY(GeometryBundle mesh)
    {
        var y = float.MinValue;
        for (var i = 1; i < mesh.Position.Length; i += 3) y = MathF.Max(y, mesh.Position[i]);
        return y;
    }

    public static bool Covers(Collider c, Vector2 p) => c switch
    {
        BoxCollider box => InBox(box, p),
        WallCollider wall => DistanceToSegment(p, wall.A, wall.B) <= wall.HalfT + 0.26f,
        _ => false,
    };

    public static bool InBox(BoxCollider box, Vector2 p)
    {
        var ux = new Vector2(box.At.M11, box.At.M13);
        var uz = new Vector2(box.At.M31, box.At.M33);
        var d = p - new Vector2(box.At.M41, box.At.M43);
        return MathF.Abs(Vector2.Dot(d, ux)) <= box.Half.X + 0.26f && MathF.Abs(Vector2.Dot(d, uz)) <= box.Half.Z + 0.26f;
    }

    public static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var t = Math.Clamp(Vector2.Dot(p - a, ab) / ab.LengthSquared(), 0f, 1f);
        return Vector2.Distance(p, a + ab * t);
    }

    public static BuildContext Context()
    {
        // The south sites never read the map descriptor; the world model and seed are their whole input.
        return new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = World(), Map = null!, WorldSeed = 1 };
    }

    public static string DataRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe", "campus.json"))) directory = directory.Parent;
        if (directory is null) throw new InvalidOperationException("Repository campus fixture not found.");
        return Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe");
    }

    public static IWorldModel World()
    {
        var data = DataRoot();
        var files = new[] { "campus.json", "network.json", "interiors.json", "map.json" }
            .Concat(Directory.GetFiles(Path.Combine(data, "overlay", "south.d"), "*.json").Order(StringComparer.Ordinal)
                .Select(path => Path.GetRelativePath(data, path).Replace('\\', '/')))
            .ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        return WorldModelLoader.Load(new("qingshuihe"), files).World;
    }

    public static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    /// <summary>The map as the kernel loads it: every authored overlay, not only the south one.</summary>
    public static IWorldModel FullWorld()
    {
        var data = DataRoot();
        var files = new[] { "campus.json", "network.json", "interiors.json", "map.json" }
            .Concat(Directory.GetDirectories(Path.Combine(data, "overlay"), "*.d")
                    .Where(dir => Path.GetFileName(dir) is not "interior.d" and not "terrain.d" and not "outskirts.d" and not "walkable.d" and not "fidelity.d")
                    .SelectMany(dir => Directory.GetFiles(dir, "*.json")).Select(p => Path.GetRelativePath(data, p).Replace('\\', '/')))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        return WorldModelLoader.Load(new("qingshuihe"), files).World;
    }
}
