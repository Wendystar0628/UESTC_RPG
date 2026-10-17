using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Zones.Core.Framework;
using Campus.Gen.Zones.Core.Library;
using S = Campus.Gen.Zones.Core.CoreStyle;

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

    private AnnexSite(IReadOnlyList<Block> blocks, IReadOnlyList<WallPiece> pieces, IReadOnlyDictionary<string, (AnnexKind Kind, BuildingFrame Frame)> annexes)
    {
        Blocks = blocks;
        Pieces = pieces;
        Annexes = annexes;
        Courtyard = FindCourtyard();
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
        (CoreFeatures.NorthAnnex, AnnexKind.NorthAnnex),
        (CoreFeatures.CourtWing, AnnexKind.CourtWing),
        (CoreFeatures.LowBlock, AnnexKind.LowBlock),
        (CoreFeatures.NorthWestLink, AnnexKind.Link),
        (CoreFeatures.NorthWestJoint, AnnexKind.Joint),
    ];

    /// <summary>Covers built by others: their own data height above their pad.</summary>
    private static readonly FeatureId[] Covers = [CoreFeatures.NorthEastConnector, CoreFeatures.NorthEastBridge, CoreFeatures.CafeStore];

    public static AnnexSite? From(IWorldModel world, LibraryShell? library)
    {
        ArgumentNullException.ThrowIfNull(world);
        var blocks = new List<Block>();
        if (library is not null) blocks.AddRange(library.Plan.Blocks);
        var annexes = new SortedDictionary<string, (AnnexKind, BuildingFrame)>(StringComparer.Ordinal);
        foreach (var (feature, kind) in Members)
        {
            if (!world.Features.ContainsKey(feature)) continue;
            var frame = BuildingFrame.Of(world, feature, S.AnnexStoreys.Take(S.AnnexStoreysOf(kind)).ToArray());
            annexes.Add(feature.Value, (kind, frame));
            blocks.AddRange(frame.Footprints.Select(r => new Block(feature.Value, BlockKind.Annex, LibraryPlan.Ccw(r.Outer), frame.WallBaseY, frame.EavesY)));
        }
        if (annexes.Count == 0) return null;
        foreach (var cover in Covers)
        {
            if (!world.Features.TryGetValue(cover, out var f)) continue;
            var pad = world.Terrain.PadY(cover);
            var height = f.Height is > 0.5f and var h ? h : (f.Storeys ?? 2) * 3.5f;
            blocks.AddRange(Plan.Polygons(f.Geometry).Select(r => new Block(cover.Value, BlockKind.Neighbour, LibraryPlan.Ccw(r.Outer), pad - S.WallBelowPad, pad + height)));
        }
        return new AnnexSite(blocks, LibraryPlan.Exterior(blocks), annexes);
    }

    /// <summary>The wall pieces of one north block.</summary>
    public IEnumerable<WallPiece> PiecesOf(string feature) => Pieces.Where(p => p.Block.Kind == BlockKind.Annex && p.Block.Source == feature);

    /// <summary>
    /// Links the ground-standing pieces head to tail into loops. The plan's blocks are counterclockwise, so the outline of
    /// the whole group runs counterclockwise and every enclosed court clockwise; the court is the clockwise loop of more than
    /// 60 m² nearest the court wing.
    /// </summary>
    private IReadOnlyList<WallPiece>? FindCourtyard()
    {
        var ground = Pieces.Where(p => p.Y0 <= p.Block.Y0 + 0.01f).ToList();
        var used = new bool[ground.Count];
        var wing = Annexes.TryGetValue(CoreFeatures.CourtWing.Value, out var w) ? w.Frame.Centroid : (Vector2?)null;
        if (wing is null) return null;
        IReadOnlyList<WallPiece>? best = null;
        var bestDistance = float.MaxValue;
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
            var area = SignedArea(ring);
            if (area > -60f) continue;
            var distance = Vector2.Distance(Plan.Centroid([ring]), wing.Value);
            if (distance < bestDistance) { bestDistance = distance; best = loop.Select(i => ground[i]).ToArray(); }
        }
        return best;
    }

    public static float SignedArea(IReadOnlyList<Vector2> ring)
    {
        double area = 0;
        for (var i = 0; i < ring.Count; i++)
        {
            Vector2 a = ring[i], b = ring[(i + 1) % ring.Count];
            area += (double)a.X * b.Y - (double)b.X * a.Y;
        }
        return (float)(area * 0.5);
    }
}
