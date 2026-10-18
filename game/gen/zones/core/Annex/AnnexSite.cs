using Campus.Gen.Zones.Common;
using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Zones.Core.Framework;
using Campus.Gen.Zones.Core.Library;
using Campus.Gen.World;
using S = Campus.Gen.Zones.Core.CoreStyle;
using FacadeRules = Campus.Gen.Buildings.Rules.FacadeRules;

namespace Campus.Gen.Zones.Core.Annex;

/// <summary>
/// The library's north blocks as one plan (16e, MAP-QSH-CORE): every block of the library, the five north blocks this
/// zone builds and the north-east connector, bridge and store front (16f's) as covers, so a wall standing against any
/// other block is not built. The ground-floor wall pieces of all of them close round the 宁静苑 courtyard, which is found
/// here as the one hole of that plan next to the court wing; nothing is placed by coordinates.
/// </summary>
public sealed class AnnexSite
{
    private const float Join = 0.12f;

    private AnnexSite(IWorldModel world, IReadOnlyList<Block> blocks, IReadOnlyList<WallPiece> pieces, IReadOnlyDictionary<string, (AnnexKind Kind, BuildingFrame Frame)> annexes)
    {
        Blocks = blocks;
        Pieces = pieces;
        Annexes = annexes;
        Courtyard = FindCourtyard(world);
    }

    public IReadOnlyList<Block> Blocks { get; }
    /// <summary>Exterior wall pieces of every block in the plan (covers included).</summary>
    public IReadOnlyList<WallPiece> Pieces { get; }
    /// <summary>The north blocks in this world by feature ID: kind and building frame.</summary>
    public IReadOnlyDictionary<string, (AnnexKind Kind, BuildingFrame Frame)> Annexes { get; }
    /// <summary>The courtyard's wall pieces in loop order (each piece's Out points into the court), or null when it does not close.</summary>
    public IReadOnlyList<WallPiece>? Courtyard { get; }

    public static readonly IReadOnlyList<(FeatureId Feature, AnnexKind Kind)> Members =
    [
        (CoreIds.NorthAnnex, AnnexKind.NorthAnnex),
        (CoreIds.CourtWing, AnnexKind.CourtWing),
        (CoreIds.LowBlock, AnnexKind.LowBlock),
        (CoreIds.NorthWestLink, AnnexKind.Link),
        (CoreIds.NorthWestJoint, AnnexKind.Joint),
    ];

    /// <summary>Covers built by others: their own data height above their pad.</summary>
    private static readonly FeatureId[] Covers = [CoreIds.NorthEastConnector, CoreIds.NorthEastBridge, CoreIds.CafeStore];

    public static AnnexSite? From(IWorldModel world, LibraryShell? library)
    {
        ArgumentNullException.ThrowIfNull(world);
        var blocks = new List<Block>();
        if (library is not null) blocks.AddRange(library.Plan.Blocks);
        var annexes = new SortedDictionary<string, (AnnexKind, BuildingFrame)>(StringComparer.Ordinal);
        foreach (var (feature, kind) in Members)
        {
            if (!world.Features.TryGetValue(feature, out var source)) continue;
            // D-239: the storey count is the data's; each storey's height is the library's own table (the data's 3.5 m a storey would break the shared floor lines).
            var count = source.Storeys is > 0 and var n ? n : throw new InvalidOperationException($"北辅楼 {feature.Value} has no storey count in the world model.");
            var heights = Enumerable.Range(0, count).Select(i => S.LibraryStoreys[Math.Min(i, S.LibraryStoreys.Length - 1)]).ToArray();
            var frame = BuildingFrame.Of(world, feature, heights);
            annexes.Add(feature.Value, (kind, frame));
            blocks.AddRange(frame.Footprints.Select(r => new Block(feature.Value, BlockKind.Annex, PlanGeometry.OpenRing(r.Outer), frame.WallBaseY, frame.EavesY)));
        }
        if (annexes.Count == 0) return null;
        foreach (var cover in Covers)
        {
            if (!world.Features.TryGetValue(cover, out var f)) continue;
            var pad = world.Terrain.PadY(cover);
            var height = f.Height is > 0.5f and var h ? h : (f.Storeys ?? 2) * 3.5f;
            blocks.AddRange(WorldFeatureGeometry.PolygonsOf(f.Geometry).Select(r => new Block(cover.Value, BlockKind.Neighbour, PlanGeometry.OpenRing(r.Outer), pad - FacadeRules.WallBelowPad, pad + height)));
        }
        return new AnnexSite(world, blocks, LibraryPlan.Exterior(blocks), annexes);
    }

    /// <summary>The wall pieces of one north block.</summary>
    public IEnumerable<WallPiece> PiecesOf(string feature) => Pieces.Where(p => p.Block.Kind == BlockKind.Annex && p.Block.Source == feature);

    /// <summary>
    /// Links the ground-standing pieces head to tail into loops. The plan's blocks are counterclockwise, so the outline of
    /// the whole group runs counterclockwise and every enclosed court clockwise; the court is the clockwise loop of more than
    /// 60 m² containing the registered pergola wall.
    /// </summary>
    private IReadOnlyList<WallPiece>? FindCourtyard(IWorldModel world)
    {
        var ground = Pieces.Where(p => p.Y0 <= p.Block.Y0 + 0.01f).ToList();
        var used = new bool[ground.Count];
        if (!Annexes.ContainsKey(CoreIds.CourtWing.Value)) return null;
        var selectedWall = CoreContours.Wall(world, CoreIds.GardenPergolaFace,
            ground.Where(piece => piece.Block.Source == CoreIds.CourtWing.Value));
        for (var start = 0; start < ground.Count; start++)
        {
            if (used[start]) continue;
            var loop = new List<int> { start };
            used[start] = true;
            var closed = false;
            while (loop.Count <= ground.Count)
            {
                var end = ground[loop[^1]].B;
                if (loop.Count > 2 && Vector2.Distance(end, ground[start].A) < Join) { closed = true; break; }
                var next = -1;
                var gap = Join;
                for (var k = 0; k < ground.Count; k++)
                {
                    if (used[k]) continue;
                    var d = Vector2.Distance(ground[k].A, end);
                    if (d < gap) { gap = d; next = k; }
                }
                if (next < 0) break;
                used[next] = true;
                loop.Add(next);
            }
            if (!closed) continue;
            var ring = loop.Select(i => ground[i].A).ToArray();
            var area = (float)PlanGeometry.SignedArea(ring);
            if (area > -60f) continue;
            if (loop.Any(i => ReferenceEquals(ground[i], selectedWall))) return loop.Select(i => ground[i]).ToArray();
        }
        return null;
    }
}
