using System.Numerics;
using Campus.Contracts;
using Campus.Gen.World;
using Campus.Gen.Zones.South.Framework;
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
        Require(module.Claims.Count == 4, "gate, front steps, Xuanbang and Liren's connector are claimed");
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
            Require(item.Anchor.X is > -1000 and < 1000 && item.Anchor.Y is > 90 and < 1000, $"item at {item.Anchor} is in the south zone");
    }


    [TestCase]
    public void OverlaySetsLirenAndRegistersTheWadeStep()
    {
        var world = World();
        foreach (var id in new[] { Liren.LirenSite.BlockA, Liren.LirenSite.BlockB })
        {
            Require(world.Features[new(id)].Storeys == 4, $"{id} has 4 storeys");
            Require(world.Features[new(id)].Family?.Value == "modern", $"{id} is in the white family");
        }
        Require(world.Features.ContainsKey(new(EastLake.EastLakeSite.WadeStep)), "wade step design addition");
    }

    [TestCase]
    public void XuanbangKeepsItsSkyline()
    {
        var ctx = Context();
        var items = new SouthModule().ItemsFor(ctx, LodLevel.L0).Where(i => i.Mesh is not null).ToArray();
        var f = Xuanbang.XuanbangSite.Rings(ctx.World);
        float Top(IReadOnlyList<Vector2> ring) => items.Where(i => Near(ring, i.Anchor, 3f)).Max(i => MaxY(i.Mesh!));
        Require(MathF.Abs(Top(f.Clock) - SouthTune.Xuanbang.Top) < 0.05f, $"clock tower top {Top(f.Clock)}");
        Require(MathF.Abs(Top(f.Tower) - 73.85f) < 0.05f, $"tower top {Top(f.Tower)}");
        var main = items.Where(i => Near(f.Main, i.Anchor, 1f) && !Near(f.Tower, i.Anchor, 3f) && !Near(f.Clock, i.Anchor, 3f)
            && Vector2.Distance(i.Anchor, SouthTune.Xuanbang.Skylight) > 10f).Max(i => MaxY(i.Mesh!));
        Require(main > 35.6f && main < 38.8f, $"main wing roof line {main} (35.7 m, gables up to 38.3 m)");
        var all = items.Max(i => MaxY(i.Mesh!));
        Require(all < 73.9f, $"the Xuanbang tower stays the highest point of the zone ({all})");
    }

    [TestCase]
    public void LirenGatewayIsOpenUnderTheBridge()
    {
        var ctx = Context();
        var module = new SouthModule();
        var f = Liren.LirenSite.Rings(ctx.World);
        var walls = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).Where(c => c.Tag is SurfaceTag.Wall or SurfaceTag.DoorClosed).ToArray();
        var open = Framework.GridFacade.Edges(f.Bridge).Select(e => e.Info).First(e => Liren.LirenSite.Dress(e, f.BridgeNeighbours).Kind != Framework.EdgeKind.Hidden);
        var centre = Framework.PlanMath.Centroid(f.Bridge);
        for (var t = -14f; t <= 14f; t += 0.25f)
        {
            var p = centre + open.Frame.W * t;
            Require(!walls.Any(c => Covers(c, p, 1.0f)), $"the gateway passage is clear at {p}");
        }
        Require(SouthTune.Liren.Soffit >= 10.5f, "the void is about three storeys high");
    }

    [TestCase]
    public void HardBankBalustradeGuardsTheWaterButTheWadeStep()
    {
        var ctx = Context();
        var module = new SouthModule();
        var barriers = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).OfType<WallCollider>().Where(w => w.Tag == SurfaceTag.Barrier).ToArray();
        var water = EastLake.EastLakeSite.Water(ctx.World);
        Require(EastLake.EastLakeSite.InWater(water, EastLake.EastLakeSite.WadeFrame.P(EastLake.EastLakeSite.WadeLength * 0.5f, 1f)), "the wade frame looks over the water");
        Require(SouthTune.Lake.Tread2 < -0.5f && SouthTune.Lake.Tread2 > -0.8f, "the wade tread is under the -0.5 m water by less than 0.3 m");
        Require(SouthTune.Lake.RailTop >= 1.05f, "balustrade at least 1.05 m");
        foreach (var (f, l, i) in EastLake.EastLakeSite.BankSegments(water))
        {
            var openings = EastLake.EastLakeSite.Openings(i, l);
            for (var u = 0.3f; u <= l - 0.3f; u += 0.25f)
            {
                var p = f.P(u, -SouthTune.Lake.RailSetback);
                var inOpening = openings.Any(o => u > o.U0 + 0.5f && u < o.U1 - 0.5f);
                var nearOpening = openings.Any(o => u > o.U0 - 0.5f && u < o.U1 + 0.5f);
                var guarded = barriers.Any(w => w.Y1 >= 1.5f && Covers(w, p, 1.0f));
                if (inOpening) Require(!guarded, $"the wade step at {p} is open");
                else if (!nearOpening) Require(guarded, $"the bank at {p} is guarded");
            }
        }
    }


    private static bool Covers(Collider c, Vector2 p, float y) => c switch
    {
        BoxCollider box => InBox(box, p) && MathF.Abs(y - box.At.M42) <= box.Half.Y,
        WallCollider wall => DistanceToSegment(p, wall.A, wall.B) <= wall.HalfT + 0.26f && y >= wall.Y0 && y <= wall.Y1,
        PostCollider post => Vector2.Distance(p, post.C) <= post.R + 0.26f && y >= post.Y0 && y <= post.Y1,
        _ => false,
    };

    private static bool Near(IReadOnlyList<Vector2> ring, Vector2 p, float margin) =>
        p.X >= ring.Min(q => q.X) - margin && p.X <= ring.Max(q => q.X) + margin && p.Y >= ring.Min(q => q.Y) - margin && p.Y <= ring.Max(q => q.Y) + margin;

    private static float MaxY(GeometryBundle mesh)
    {
        var y = float.MinValue;
        for (var i = 1; i < mesh.Position.Length; i += 3) y = MathF.Max(y, mesh.Position[i]);
        return y;
    }

    private static float MinY(GeometryBundle mesh)
    {
        var y = float.MaxValue;
        for (var i = 1; i < mesh.Position.Length; i += 3) y = MathF.Min(y, mesh.Position[i]);
        return y;
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
