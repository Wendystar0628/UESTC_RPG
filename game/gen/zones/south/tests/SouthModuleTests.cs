using System.Numerics;
using Campus.Contracts;
using Campus.Gen.World;
using GdUnit4;

namespace Campus.Gen.Zones.South.Tests;

[TestSuite]
public sealed class SouthModuleTests
{
    private static readonly LodLevel[] Levels = [LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4];

    [TestCase]
    public void OverlayLoadsAndSetsTheMainBuildingHeights()
    {
        var world = World();
        Require(world.Features[new("qsh:way:687370085")].Height is 34.4f, "B2 height from the HeightOp");
        Require(world.Features[new("qsh:way:687370085")].Storeys == 8, "B2 storeys");
        Require(world.Features[new("qsh:way:687370086")].Storeys == 7, "B1 storeys");
        Require(world.Features[new("qsh:way:687370092")].Storeys == 5, "A1 storeys");
        Require(world.Features.ContainsKey(new("design:south-hongfeng-pavilion")), "pavilion design addition");
    }

    [TestCase]
    public void ClaimsNameExistingFeatures()
    {
        var world = World();
        var module = new SouthModule();
        Require(module.Claims.Count == 2, "gate and front steps are claimed");
        foreach (var claim in module.Claims.OfType<FeatureClaim>())
        {
            Require(world.Features.ContainsKey(claim.Feature), $"claimed feature {claim.Feature.Value} exists");
            Require((claim.Aspects & Aspects.Massing) != 0, "claims cover massing so WP-14 skips the extrusion");
        }
    }

    [TestCase]
    public void EveryLevelIsValidAndEveryItemLandsInOneCell()
    {
        var ctx = Context();
        var module = new SouthModule();
        var tiles = module.Tiles(ctx).ToArray();
        Require(tiles.Length > 0, "the module occupies cells");
        foreach (var lod in Levels)
        {
            var items = module.ItemsFor(ctx, lod);
            Require(items.Count > 0, $"{lod} has items");
            var size = 64 << (int)lod;
            var cells = items.Select(i => ((int)MathF.Floor(i.Anchor.X / size), (int)MathF.Floor(i.Anchor.Y / size))).Distinct().ToArray();
            var meshes = 0;
            var parts = 0;
            foreach (var (x, z) in lod == LodLevel.L4 ? [(0, 0)] : cells)
            {
                var output = module.Build(ctx, new(x, z), lod);
                foreach (var mesh in output.Meshes)
                {
                    mesh.ValidateVertexData();
                    Require(mesh.Position.Length / 3 <= 65536, "bundle vertex limit");
                    meshes++;
                }
                parts += output.Parts.Count;
            }
            Require(meshes > 0, $"{lod} emits meshes");
            Require(items.Count(i => i.Part is not null) == parts, $"{lod}: every placed part is emitted exactly once");
        }
    }

    [TestCase]
    public void HeaviestCellsStayInsideTheDeclaredBudget()
    {
        var ctx = Context();
        var module = new SouthModule();
        foreach (var (lod, limit) in new[] { (LodLevel.L0, module.Budget.TrianglesL0), (LodLevel.L1, module.Budget.TrianglesL1) })
        {
            var size = 64 << (int)lod;
            var cells = module.ItemsFor(ctx, lod).Select(i => ((int)MathF.Floor(i.Anchor.X / size), (int)MathF.Floor(i.Anchor.Y / size))).Distinct();
            foreach (var (x, z) in cells)
            {
                var output = module.Build(ctx, new(x, z), lod);
                var triangles = output.Meshes.Sum(m => m.Index.Length / 3);
                Require(triangles <= limit, $"{lod} cell ({x}, {z}) has {triangles} triangles, budget {limit}");
                Require(output.Meshes.Select(m => m.Material).Distinct().Count() <= module.Budget.DrawCalls, $"{lod} cell ({x}, {z}) material count");
            }
        }
    }

    [TestCase]
    public void BuildIsDeterministic()
    {
        var a = Bytes(Context());
        var b = Bytes(Context());
        Require(a.SequenceEqual(b), "two worlds give the same bytes");
    }

    [TestCase]
    public void GateClosureHasNoGapAboveHalfAMetre()
    {
        var ctx = Context();
        var module = new SouthModule();
        var boundary = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).Where(c => c.Tag == SurfaceTag.Boundary).ToArray();
        Require(boundary.Length >= 11, "houses, islands, carriageway and both turnstile rows are boundary colliders");
        // March along the gate line between the guard houses' walls: every 0.25 m sample is inside some boundary primitive.
        var along = Vector2.Normalize(SouthTune.Gate.HouseEast - SouthTune.Gate.HouseWest);
        var west = SouthTune.Gate.HouseWest + along * (SouthTune.Gate.HouseSize * 0.5f);
        var east = SouthTune.Gate.HouseEast - along * (SouthTune.Gate.HouseSize * 0.5f);
        // The barrier arms stand 1.4 m inside the turnstile rows; sample on the arms' line, which crosses every element.
        var inward = new Vector2(-along.Y, along.X) * SouthTune.Gate.BarrierZ;
        for (var s = 0f; s <= 1f; s += 0.25f / Vector2.Distance(west, east))
        {
            var p = Vector2.Lerp(west, east, s) + inward;
            Require(boundary.Any(c => Covers(c, p)), $"gate line covered at {p}");
        }
        Require(boundary.All(c => c is not BoxCollider box || box.Half.Y * 2 >= 1.5f), "boundary boxes reach 1.5 m");
    }

    [TestCase]
    public void SitesStayInsideTheSouthZone()
    {
        var ctx = Context();
        var module = new SouthModule();
        foreach (var item in module.ItemsFor(ctx, LodLevel.L0))
            Require(item.Anchor.X is > -1000 and < 1000 && item.Anchor.Y is > 150 and < 1000, $"item at {item.Anchor} is in the south zone");
    }

    private static bool Covers(Collider c, Vector2 p) => c switch
    {
        BoxCollider box => InBox(box, p),
        WallCollider wall => DistanceToSegment(p, wall.A, wall.B) <= wall.HalfT + 0.26f,
        _ => false,
    };

    private static bool InBox(BoxCollider box, Vector2 p)
    {
        var ux = new Vector2(box.At.M11, box.At.M13);
        var uz = new Vector2(box.At.M31, box.At.M33);
        var d = p - new Vector2(box.At.M41, box.At.M43);
        return MathF.Abs(Vector2.Dot(d, ux)) <= box.Half.X + 0.26f && MathF.Abs(Vector2.Dot(d, uz)) <= box.Half.Z + 0.26f;
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var t = Math.Clamp(Vector2.Dot(p - a, ab) / ab.LengthSquared(), 0f, 1f);
        return Vector2.Distance(p, a + ab * t);
    }

    private static byte[] Bytes(BuildContext ctx)
    {
        var module = new SouthModule(SouthSites.All.ToArray());
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        foreach (var tile in module.Tiles(ctx))
        {
            var output = module.Build(ctx, tile, LodLevel.L0);
            foreach (var mesh in output.Meshes)
            {
                foreach (var v in mesh.Position) writer.Write(v);
                foreach (var v in mesh.Normal) writer.Write(v);
                foreach (var v in mesh.Index) writer.Write(v);
            }
            foreach (var part in output.Parts) writer.Write(part.Part.Value + part.At.ToString());
        }
        return stream.ToArray();
    }

    private static BuildContext Context()
    {
        // The south sites never read the map descriptor; the world model and seed are their whole input.
        return new BuildContext { World = World(), Map = null!, WorldSeed = 1 };
    }

    private static IWorldModel World()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe", "campus.json"))) directory = directory.Parent;
        if (directory is null) throw new InvalidOperationException("Repository campus fixture not found.");
        var data = Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe");
        var files = new[] { "campus.json", "network.json", "interiors.json", "map.json", "overlay/south.overlay.json" }
            .ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        return WorldModelLoader.Load(new("qingshuihe"), files).World;
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
