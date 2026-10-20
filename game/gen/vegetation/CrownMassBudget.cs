using Campus.Contracts;
using static Campus.Gen.Vegetation.MotherModels;
using static Campus.Gen.Vegetation.CrownMasses;

namespace Campus.Gen.Vegetation;

/// <summary>Fits one model's selected masses to the existing level budget while retaining card hosts.</summary>
internal sealed class CrownMassBudget
{
    private readonly TreePlan _plan;
    private readonly VegLevel _level;
    private readonly HashSet<int> _hosts;
    private readonly int _baseTriangles;
    private readonly int _twigTriangles;

    public CrownMassBudget(TreePlan plan, VegLevel level, HashSet<int> hosts, int baseTriangles, int twigTriangles)
    {
        _plan = plan; _level = level; _hosts = hosts;
        _baseTriangles = baseTriangles; _twigTriangles = twigTriangles;
        Blobs = BlobsFor(plan, level);
        Hidden = level == VegLevel.L1 ? null : new HiddenFaces(plan, Blobs);
    }

    public List<(Blob Blob, int Index, float Scale, int Detail)> Blobs { get; }
    public HiddenFaces? Hidden { get; }
    private int Total() => _baseTriangles + _twigTriangles + (Hidden?.Cost(Blobs) ?? CostOf(Blobs));
    private List<int> Order() => ByExposure(_plan, Blobs).Concat(Enumerable.Range(0, Blobs.Count).Where(i => Blobs[i].Blob.Core)).ToList();

    public void FitLevel()
    {
        if (_level == VegLevel.L0Plus) Fit(MaxTrianglesL0Plus - 100, minDetail: 1);
        else if (_level == VegLevel.L0) FitMiddleLevel();
        else if (_plan.Species.Deciduous) Fit(TrianglesL1, minDetail: 0);
    }

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
            Hidden?.Reset(Blobs);
            order = Order();
        }
    }

    private void FitMiddleLevel()
    {
        var cloud = _plan.Species.Crown == CrownType.Cloud;
        if (cloud)
        {
            Prune(_plan, Blobs, _hosts, CloudTrianglesL0 - _baseTriangles, minDetail: 0,
                () => Hidden?.Cost(Blobs) ?? CostOf(Blobs));
            Hidden?.Reset(Blobs);
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
