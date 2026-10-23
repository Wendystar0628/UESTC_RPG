using Campus.Contracts;
using static Campus.Gen.Vegetation.MotherModels;
using static Campus.Gen.Vegetation.CrownMasses;

namespace Campus.Gen.Vegetation;

/// <summary>
/// Fits one model's selected masses to the existing level budget while retaining card hosts and core masses. L1 starts from
/// the masses L0 kept and the far stand-in from the masses L1 kept; each keeps the most exposed of them (see
/// <see cref="CrownMasses.BlobsFor"/>), so every coarser set is a subset of the finer one with the same surfaces.
/// </summary>
internal sealed class CrownMassBudget
{
    private readonly TreePlan _plan;
    private readonly HashSet<int> _hosts;
    private readonly int _baseTriangles;
    private readonly int _twigTriangles;

    /// <summary>The masses of one level, fitted to its budget.</summary>
    /// <param name="finer">The masses L0 kept, required at L1 (its starting set); null at L0+ and L0.</param>
    public CrownMassBudget(TreePlan plan, VegLevel level, HashSet<int> hosts, int baseTriangles, int twigTriangles,
        IReadOnlyList<(Blob Blob, int Index, float Scale, int Detail)>? finer)
        // L1 may step masses down to bare icosahedra, whose flat faces sit deeper than a level-1 facet.
        : this(plan, BlobsFor(plan, level, finer), hosts, baseTriangles, twigTriangles, level == VegLevel.L1 ? IcosahedronSag : FacetSag)
    {
        if (level == VegLevel.L0Plus) Fit(MaxTrianglesL0Plus - 100, minDetail: 1);
        else if (level == VegLevel.L0) FitMiddleLevel();
        else Fit(TrianglesL1, minDetail: 0);
    }

    private CrownMassBudget(TreePlan plan, List<(Blob Blob, int Index, float Scale, int Detail)> blobs, HashSet<int> hosts,
        int baseTriangles, int twigTriangles, float sag)
    {
        _plan = plan; _hosts = hosts;
        _baseTriangles = baseTriangles; _twigTriangles = twigTriangles;
        Blobs = blobs;
        Hidden = new HiddenFaces(plan, Blobs, sag);
    }

    /// <summary>
    /// The far stand-in's masses (GAME-195): the masses L1 kept as bare icosahedra, the least exposed removed until the kept
    /// faces fit <see cref="FarMassTriangles"/>. It has no cards, so every non-core mass may go.
    /// </summary>
    public static CrownMassBudget FarStandIn(TreePlan plan, IReadOnlyList<(Blob Blob, int Index, float Scale, int Detail)> l1)
    {
        var far = new CrownMassBudget(plan, l1.Select(b => (b.Blob, b.Index, b.Scale, 0)).ToList(), [], 0, 0, IcosahedronSag);
        far.Fit(FarMassTriangles, minDetail: 0);
        return far;
    }

    public List<(Blob Blob, int Index, float Scale, int Detail)> Blobs { get; }
    public HiddenFaces Hidden { get; }
    private int Total() => _baseTriangles + _twigTriangles + Hidden.Cost(Blobs);
    private List<int> Order() => ByExposure(_plan, Blobs).Concat(Enumerable.Range(0, Blobs.Count).Where(i => Blobs[i].Blob.Core)).ToList();

    // Step down the least exposed masses first; only then remove a free non-core mass and update hidden faces.
    private void Fit(int limit, int minDetail)
    {
        var order = Order();
        while (Total() > limit)
        {
            var step = order.FindIndex(i => Blobs[i].Detail > minDetail);
            if (step >= 0)
            {
                var (blob, index, scale, detail) = Blobs[order[step]];
                Blobs[order[step]] = (blob, index, scale, detail - 1);
                continue;
            }
            var free = order.FindIndex(i => !Blobs[i].Blob.Core && !_hosts.Contains(Blobs[i].Index));
            if (free < 0) break;
            Blobs.RemoveAt(order[free]);
            Hidden.Reset(Blobs);
            order = Order();
        }
    }

    private void FitMiddleLevel()
    {
        var cloud = _plan.Species.Crown == CrownType.Cloud;
        if (cloud)
        {
            Prune(_plan, Blobs, _hosts, CloudTrianglesL0 - _baseTriangles, minDetail: 0, () => Hidden.Cost(Blobs));
            Hidden.Reset(Blobs);
        }
        var limit = cloud ? CloudTrianglesL0Outline : CardTrianglesL0;
        if (!cloud) Fit(limit, minDetail: 0);
        var order = ByExposure(_plan, Blobs);
        order.Reverse();
        order.InsertRange(0, Enumerable.Range(0, Blobs.Count).Where(i => Blobs[i].Blob.Core));
        foreach (var i in order)
        {
            var (blob, index, scale, detail) = Blobs[i];
            if (detail >= 2) continue;
            Blobs[i] = (blob, index, scale, 2);
            if (Total() > limit)
            {
                Blobs[i] = (blob, index, scale, detail);
                break;
            }
        }
    }
}
