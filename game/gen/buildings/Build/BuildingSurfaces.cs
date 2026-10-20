using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.Buildings.Mesh;
using Campus.Gen.Buildings.Rules;

namespace Campus.Gen.Buildings.Build;

/// <summary>The material faces shared by every mesh responsibility and detail level of one building.</summary>
internal sealed class BuildingSurfaces
{
    public Face Wall { get; }
    public Face Accent { get; }
    public Face Trim { get; }
    public Face Plinth { get; }
    public Face Stone { get; }
    public Face Roof { get; }
    public Face Metal { get; }

    public BuildingSurfaces(FacadeProfile profile)
    {
        // Every wall-class face of a building carries the same pattern, so walls, reveals and far massing stay one material;
        // only the main wall panels turn it on (UV2 gate in Walls).
        var pattern = WallPatterns.Of(profile.Wall);
        Wall = new Face(MaterialKind.Wall, profile.Wall, Pattern: pattern);
        Accent = new Face(MaterialKind.Wall, profile.Accent ?? profile.Wall, Pattern: pattern);
        Trim = new Face(MaterialKind.Trim, profile.Trim);
        Plinth = new Face(MaterialKind.Trim, profile.Plinth);
        Stone = new Face(MaterialKind.Trim, Families.StoneBeige);
        Roof = new Face(MaterialKind.Roof, profile.Roof);
        Metal = new Face(MaterialKind.Metal, Families.FrameAlu, VertexFlag.NoShadow);
    }
}
