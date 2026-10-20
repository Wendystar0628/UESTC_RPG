using Campus.Contracts;
using Campus.Gen.Geometry;
using System.Numerics;

namespace Campus.Gen.Maps.Qingshuihe.Core.Library;

/// <summary>The podium decks shared by shell collision and the north terrace's outdoor access.</summary>
internal sealed class LibraryTerrace
{
    private readonly LibraryPlan _plan;

    public LibraryTerrace(LibraryPlan plan) => _plan = plan;

    public IEnumerable<Block> Decks => _plan.Blocks.Where(block => block.Kind == BlockKind.Podium);

    public IReadOnlyList<OutdoorAccessArea> DescribeAccess()
    {
        var areas = new List<StoreyInfo>();
        var covers = _plan.Blocks.Where(block => block.Kind != BlockKind.Podium && block.Y1 > _plan.SecondFloorY)
            .Select(block => new PolygonRings(block.Ring)).ToArray();
        foreach (var podium in Decks)
        {
            var minimum = podium.Ring.Aggregate(Vector2.Min);
            var maximum = podium.Ring.Aggregate(Vector2.Max);
            var north = new PolygonRings([minimum, new(maximum.X, minimum.Y),
                new(maximum.X, _plan.Centre.Y), new(minimum.X, _plan.Centre.Y)]);
            foreach (var ring in PlanPolygons.IntersectionTriangles(new(podium.Ring), north, covers))
                areas.Add(new(2, _plan.SecondFloorY, _plan.StoreyY(2) - _plan.SecondFloorY, ring));
        }
        return [new("library.terrace-f", CoreIds.Library, areas.AsReadOnly())];
    }
}
