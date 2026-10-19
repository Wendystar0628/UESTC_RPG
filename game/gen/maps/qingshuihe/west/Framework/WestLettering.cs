using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Street;

namespace Campus.Gen.Maps.Qingshuihe.West.Framework;

/// <summary>West landmark lettering through the shared scene-text atlas.</summary>
internal static class WestLettering
{
    internal static void Write(SiteBuilder b, string text, float height, ToonSpec ink, ToonSpec background, Matrix4x4 at)
    {
        if (b.Detail == SiteDetail.Mass) return;
        var atlas = new SceneTextAtlas(b.Context.GlobalInputs.Require(SceneTextInputs.AtlasKey));
        var label = new PartBuilder();
        Lettering.Write(label, atlas.Layout(text), b.Context.Catalog.Resolve(background.Lit), 0, 0, 0, true, height, ink);
        var (min, max) = label.Measure();
        b.Add(label.Finish(new Box3(min, max)), at);
    }
}
