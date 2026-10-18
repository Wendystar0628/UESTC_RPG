using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Buildings.Mesh;
using Campus.Gen.Buildings.Model;
using Campus.Gen.Buildings.Plan;
using Campus.Gen.Buildings.Rules;
using Campus.Gen.World;

namespace Campus.Gen.Buildings.Build;

/// <summary>
/// Generates one building at one detail level (ART-MODELING minimum components):
/// chamfered corners with custom normals, plinth, string courses, openings (reveal with AO and sill from the
/// wall; WP-10 frames, doors and curtains with OpeningGlass glass, see FacadeOpenings), cornice or parapet with
/// the WP-12 coping and lightning strip, roof with stair-core penthouses and a fenced WP-12 plant enclosure (see
/// RoofParts), entrances (surround, canopy, the WP-11 landing with its slope or steps, see EntranceKit), pilasters,
/// downpipes and AC grilles (WP-12 parts, see FacadeFittings). Surfaces that persist across
/// levels keep their normals, colours and material classes.
/// </summary>
public sealed partial class BuildingMesher
{
    private const float ParapetInner = -0.24f; // parapet inner face, relative to the wall plane

    private readonly BuildingPlan _p;
    private readonly LodLevel _lod;
    private readonly MeshSink _sink;
    private readonly FacadeProfile _prof;
    private readonly FacadeOpenings _openings;
    private readonly RoofParts _roofParts;
    private readonly FacadeFittings _fittings;
    private readonly bool _parapetOk;
    private readonly Face _wall, _trim, _plinth, _roof, _metal, _accent, _stone;

    public BuildingMesher(BuildingPlan plan, WindowBlock block, LodLevel lod, MeshSink sink)
    {
        _p = plan;
        _lod = lod;
        _sink = sink;
        _prof = plan.Profile;
        _openings = new FacadeOpenings(plan, block, lod, sink);
        _roofParts = new RoofParts(plan, lod, sink);
        _fittings = new FacadeFittings(plan, lod, sink);
        _wall = new Face(MaterialKind.Wall, _prof.Wall);
        _accent = new Face(MaterialKind.Wall, _prof.Accent ?? _prof.Wall);
        _trim = new Face(MaterialKind.Trim, _prof.Trim);
        _plinth = new Face(MaterialKind.Trim, _prof.Plinth);
        _stone = new Face(MaterialKind.Trim, Families.StoneBeige);
        _roof = new Face(MaterialKind.Roof, _prof.Roof);
        _metal = new Face(MaterialKind.Metal, Families.FrameAlu, VertexFlags.NoShadow);
        _parapetOk = plan.SkinRings.All(r => r.Offset(ParapetInner - 0.06f) is not null);
    }

    private bool Full => _lod == LodLevel.L0;
    private float Chamfer => Full || _lod == LodLevel.L1 ? BuildingPlan.WallChamfer : 0f;

    public void Build()
    {
        switch (_lod)
        {
            case LodLevel.L0:
            case LodLevel.L1:
                Walls();
                Openings();
                Plinth();
                StringCourses();
                RoofEdge();
                Roof();
                Penthouses();
                Entrances();
                Pilasters();
                if (Full) Downpipes();
                if (_prof.AcGrilles) Grilles();
                break;
            case LodLevel.L2:
                MassWalls(withEdge: true);
                FlatWindows();
                Penthouses();
                break;
            case LodLevel.L3:
                MassWalls(withEdge: false);
                FlatWindows();
                Penthouses();
                break;
            case LodLevel.L4:
                MassWalls(withEdge: false);
                Penthouses();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_lod), _lod, "Unknown LOD.");
        }
    }

}
