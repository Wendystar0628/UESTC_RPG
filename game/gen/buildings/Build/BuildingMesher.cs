using Campus.Gen.Geometry;
using Campus.Contracts;
using Campus.Gen.Buildings.Model;

namespace Campus.Gen.Buildings.Build;

/// <summary>Builds one building at one LOD, preserving the emission order across its independent mesh responsibilities.</summary>
public sealed class BuildingMesher
{
    private readonly LodLevel _lod;
    private readonly BuildingFacadeMesher _facade;
    private readonly BuildingRoofMesher _roof;
    private readonly BuildingDetailMesher _details;
    private readonly BuildingMassingMesher _massing;

    public BuildingMesher(BuildingPlan plan, WindowBlock block, LodLevel lod, GeometrySink sink)
    {
        _lod = lod;
        var openings = new FacadeOpenings(plan, block, lod, sink);
        var roofParts = new RoofParts(plan, lod, sink);
        var fittings = new FacadeFittings(plan, lod, sink);
        var surfaces = new BuildingSurfaces(plan.Profile);
        var skin = new BuildingSkin(plan, lod, sink);
        _facade = new BuildingFacadeMesher(plan, sink, surfaces, skin, openings);
        _roof = new BuildingRoofMesher(plan, lod, sink, surfaces, skin, roofParts);
        _details = new BuildingDetailMesher(plan, lod, sink, surfaces, skin, fittings);
        _massing = new BuildingMassingMesher(plan, sink, surfaces, skin, openings, _roof);
    }

    public void Build()
    {
        switch (_lod)
        {
            case LodLevel.L0:
            case LodLevel.L1:
                _facade.Build();
                _roof.BuildEdge();
                _roof.BuildSlab();
                _roof.BuildRelief();
                _details.Build();
                break;
            case LodLevel.L2:
                _massing.Build(withEdge: true, withWindows: true);
                _roof.BuildRelief();
                break;
            case LodLevel.L3:
                _massing.Build(withEdge: false, withWindows: true);
                _roof.BuildRelief();
                break;
            case LodLevel.L4:
                _massing.Build(withEdge: false, withWindows: false);
                _roof.BuildRelief();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_lod), _lod, "Unknown LOD.");
        }
    }
}
