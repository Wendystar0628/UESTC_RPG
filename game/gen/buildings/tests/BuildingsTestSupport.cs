using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Gen.Buildings.Build;
using Campus.Gen.Buildings.Model;
using Campus.Gen.Buildings.Plan;
using Campus.Gen.Tests.Support;

namespace Campus.Gen.Buildings.Tests;

/// <summary>Fixtures and assertion helpers shared by the building test suites.</summary>
internal static class BuildingsTestSupport
{
    internal static readonly LodLevel[] Levels = [LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4];

    // WP-10 curtain fabric; the WP-12 heat-pump shrouds share its shading but not its colours.
    internal static bool Fabric(GeometryBundle b) => b.Material.Spec.Normal == ToonNormal.Smooth && b.Material.Spec.Steps == ShadeSteps.TwoHalf &&
        !b.Material.Spec.Lit.Value.StartsWith("equip.", StringComparison.Ordinal);

    internal static IEnumerable<int> ColorVertices(GeometryBundle mesh, params string[] tokens)
    {
        var catalog = Campus.Contracts.Tests.PaletteFixture.Catalog;
        var indices = tokens.Select(token => catalog.Resolve(new PaletteToken(token)).Value).ToArray();
        return Enumerable.Range(0, mesh.Position.Length / 3)
            .Where(vertex => indices.Contains(mesh.ColorIndices?[vertex] ?? catalog.Resolve(mesh.Material.Spec.Lit).Value));
    }
    internal static bool HasColor(GeometryBundle mesh, string token) => ColorVertices(mesh, token).Any();
    internal static bool TriangleColor(GeometryBundle mesh, int offset, string token)
    {
        var index = Campus.Contracts.Tests.PaletteFixture.Catalog.Resolve(new PaletteToken(token)).Value;
        return Enumerable.Range(0, 3).All(corner => mesh.ColorIndices![mesh.Index[offset + corner]] == index);
    }

    /// <summary>Every opening of every plan (or of one key, or of those <paramref name="where"/> takes) with its number and card, in plan order.</summary>
    internal static string Layout(BuildingSet set, string? key = null, Func<BuildingPlan, bool>? where = null)
    {
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < set.Plans.Count; i++)
        {
            var (plan, block) = (set.Plans[i], set.Blocks[i]);
            if ((key is not null && plan.Key != key) || where?.Invoke(plan) == false) continue;
            sb.Append(ci, $"P {plan.Key} base={block.Base} cols={block.Columns} rows={block.Rows}\n");
            for (var fi = 0; fi < plan.Facades.Count; fi++)
                foreach (var o in plan.Openings[fi])
                {
                    var n = FacadeOpenings.Number(plan, block, plan.Facades[fi], o);
                    sb.Append(ci, $"  {fi} b{o.Bay} r{o.Row} {o.Kind} {o.U0:R} {o.U1:R} {o.Y0:R} {o.Y1:R} #{n} {FacadeOpenings.Card(plan, o, n)}\n");
                }
        }
        return sb.ToString();
    }

    /// <summary>Plan position of an entrance door's centre on the glass plane (where the interior index puts it).</summary>
    internal static Vector2 GlassCentre(BuildingPlan plan, PlanEntrance e)
    {
        var door = plan.DoorOf(e);
        var skin = plan.SkinAt(plan.Facades[e.Facade].Ring);
        var edge = plan.Facades[e.Facade].Edge;
        return skin.Points[edge] + skin.Dir[edge] * ((door.U0 + door.U1) * 0.5f) - skin.Normal[edge] * plan.Reveal;
    }

    internal static bool Blocks(WallCollider wall, Vector2 point, float y)
    {
        var edge = wall.B - wall.A;
        var t = Math.Clamp(Vector2.Dot(point - wall.A, edge) / edge.LengthSquared(), 0f, 1f);
        return y > wall.Y0 && y < wall.Y1 && Vector2.Distance(point, wall.A + edge * t) <= wall.HalfT + 0.0001f;
    }

    /// <summary>An open entrance of a feature: door centre (floor height in Y), facing yaw (0 north, 90 east), outside landing 2.5 m out.</summary>
    internal static (string Feature, EntranceDecl Door) Anchor(string feature, string id, Vector3 centre, float yawDeg, float? outsideY = null)
    {
        var heading = Angles.Heading(yawDeg);
        var facing = new Vector3(heading.X, 0f, heading.Y);
        var outside = centre + facing * 2.5f;
        outside.Y = outsideY ?? centre.Y;
        return (feature, new EntranceDecl(id, outside, centre - facing * 3f, 1.1f, true) { Center = centre, YawDeg = yawDeg });
    }

    internal static Vector3 P(GeometryBundle b, int i) => new(b.Position[3 * i], b.Position[3 * i + 1], b.Position[3 * i + 2]);
    internal static Vector3 N(GeometryBundle b, int i) => new(b.Normal[3 * i], b.Normal[3 * i + 1], b.Normal[3 * i + 2]);


    internal static readonly MapDescriptor TestMap = new(new MapId("test"), new LocalizedName("测试", "Test"), MapKind.Outdoor, 1, 1u,
        new GeoFrame(0d, 0d, 32648, new(-1000f, -1000f), new(1000f, 1000f)), Vector2.Zero,
        new Box3(new(-1024f, -28f, -1024f), new(1024f, 2000f, 1024f)), new MapRect(new(-1024f, -1024f), new(1024f, 1024f)),
        new RoamLimits(0.9f, 30f, 3000f, -28f, 2000f, 30f, 40f, 150f), new TileKey(-16, -16), new TileKey(15, 15), "P00", [],
        new MapWorldRules(MapTimeRule.Shared, new WorldPreset(17.5, Season.Autumn), new LocalizedName("落金初灯", "Golden Hour"), true, "chengdu"),
        new MapEnvironment("sky.json", "backdrop.json", []), new Dictionary<string, string>(), []);

    /// <summary>A build context as the kernel hands it out: <paramref name="claims"/> are facade claims of a zone pack.</summary>
    internal static BuildContext Ctx(IWorldModel world, uint seed, WindowNumberingSnapshot? numbering = null, string[]? claims = null) => new()
    {
        Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog,
        World = world,
        Map = TestMap,
        WorldSeed = seed,
        WindowNumbering = numbering,
        ClaimedBy = (feature, aspect) => aspect == Aspects.Facade && claims?.Contains(feature.Value) == true ? new WpId("WP-18") : null,
    };

    /// <summary>The kernel's prepass stand-in until WP-02 runs it: the provider's snapshot, published in the context.</summary>
    internal static BuildContext Prepared(BuildContext ctx) => new()
    {
        Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog,
        World = ctx.World,
        Map = ctx.Map,
        WorldSeed = ctx.WorldSeed,
        ClaimedBy = ctx.ClaimedBy,
        WindowNumbering = new BuildingsModule().PrepareWindowNumbering(ctx, []),
    };

    /// <summary>
    /// The claims ledger of the real campus (the kernel's MapClaimLedger, rebuilt): every zone pack's claims under its owner.
    /// The other content modules (furniture, ground, vegetation, water) claim nothing.
    /// </summary>
    internal static Func<FeatureId, Aspects, WpId?> RealClaims(IWorldModel world)
    {
        IContentModule[] zones = [new Campus.Gen.Zones.Core.CoreModule(), new Campus.Gen.Zones.South.SouthModule(),
            new Campus.Gen.Zones.CenterEast.CenterEastModule(), new Campus.Gen.Zones.North.NorthModule()];
        var owners = new Dictionary<(FeatureId, Aspects), WpId>();
        foreach (var zone in zones)
        {
            var owner = new WpId(zone.GetType().GetCustomAttributes(typeof(ContentModuleAttribute), false).OfType<ContentModuleAttribute>().Single().Owner);
            foreach (var claim in zone.Claims)
            {
                FeatureId[] claimed = claim switch
                {
                    FeatureClaim f => world.Features.ContainsKey(f.Feature) ? [f.Feature] : Array.Empty<FeatureId>(),
                    AreaClaim a => [.. world.Features.Values.Where(f => f.Zone == a.Zone).Select(f => f.Id)],
                    _ => throw new InvalidOperationException("Unknown claim kind."),
                };
                foreach (var feature in claimed)
                    foreach (var aspect in Enum.GetValues<Aspects>().Where(a => (claim.Aspects & a) != 0)) owners.Add((feature, aspect), owner);
            }
        }
        return (feature, aspect) => owners.TryGetValue((feature, aspect), out var owner) ? owner : null;
    }

    internal static ClaimedWindowLayout Layout(string feature, params (string Key, int Columns, int Rows)[] grids) =>
        new(new FeatureId(feature), [.. grids.Select(g => new WindowGridSpec(g.Key, g.Columns, g.Rows))]);

    internal static List<Vector2> Rect(float x, float z, float w, float d) => [new(x, z), new(x + w, z), new(x + w, z + d), new(x, z + d)];

    internal static List<Vector2> LShape(float x, float z) =>
        [new(x, z), new(x + 50, z), new(x + 50, z + 15), new(x + 16, z + 15), new(x + 16, z + 45), new(x, z + 45)];

    internal static PolygonRings Courtyard(float x, float z) =>
        new(Rect(x, z, 50, 40), [new List<Vector2> { new(x + 12, z + 12), new(x + 12, z + 28), new(x + 38, z + 28), new(x + 38, z + 12) }]);

    internal static FakeWorld World(params WorldFeature[] features) => Of(features);

    internal static FakeWorld World(FakeInteriors entrances, params WorldFeature[] features) => Of(features, entrances);

    internal static FakeWorld World(FakeInteriors entrances, ITerrain terrain, params WorldFeature[] features) => Of(features, entrances, terrain);

    internal static FakeWorld World(ZonedFidelity zones, params WorldFeature[] features)
    {
        var map = features.ToDictionary(f => f.Id);
        return Of(features, fidelity: new FakeFidelity(building =>
        {
            var ring = ((PolygonGeometry)map[building].Geometry).Rings.Outer;
            return [.. ring.Select((a, i) => new FacadeSegment(building, i, a, ring[(i + 1) % ring.Count], zones.Zone, zones.Ground))];
        }, zones.Roof));
    }

    /// <summary>Every footprint edge one segment: Zone above the ground band, Ground within it; roofs in Roof.</summary>
    internal sealed record ZonedFidelity(FidelityZone Zone, FidelityZone Ground, FidelityZone Roof = FidelityZone.K4);

    /// <summary>The open entrances of the fixture's features (the interior index's EntranceDecl).</summary>
    internal static FakeInteriors Entrances(params (string Feature, EntranceDecl Door)[] doors) =>
        new([.. doors.GroupBy(d => d.Feature).Select(g => new InteriorIndexEntry(new FeatureId(g.Key), [], [.. g.Select(d => d.Door)], true, "test"))]);

    /// <summary>Level at 0 (pads too) north of <paramref name="edge"/> (z), <paramref name="drop"/> lower south of it.</summary>
    internal static FormulaTerrain Falling(float edge, float drop) => new((x, z) => z < edge ? -drop : 0f);

    /// <summary>A loaded world with its features rewritten (as an overlay op would), everything else as loaded.</summary>
    internal static FakeWorld Styled(IWorldModel inner, Func<WorldFeature, WorldFeature> rewrite) =>
        new(inner) { Features = inner.Features.ToDictionary(p => p.Key, p => rewrite(p.Value)) };

    /// <summary>Flat ground; every fixture feature is a candidate of every query (the planner tests containment itself).</summary>
    private static FakeWorld Of(WorldFeature[] features, FakeInteriors? interiors = null, ITerrain? terrain = null, IFidelityModel? fidelity = null)
    {
        var map = features.ToDictionary(f => f.Id);
        return new FakeWorld
        {
            Features = map,
            Spatial = new FlatSpatial(candidates: map),
            Interiors = interiors ?? new FakeInteriors(),
            Terrain = terrain ?? FlatTerrain.Default,
            Fidelity = fidelity,
        };
    }
}
