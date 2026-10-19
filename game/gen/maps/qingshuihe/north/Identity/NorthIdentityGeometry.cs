using System.Numerics;
using System.Text;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Street;
using Campus.Gen.Sites;

namespace Campus.Gen.Maps.Qingshuihe.North.Identity;

/// <summary>North identity construction from authored records and the existing part kernels.</summary>
internal static class NorthIdentityGeometry
{
    internal static void Box(SiteBuilder b, PlanFrame f, float x, float y, float z,
        float width, float height, float depth, ToonSpec material)
    {
        var bevel = b.Detail == SiteDetail.Mass ? 0 : MathF.Min(PartStyleDefaults.WallCornerBevel,
            MathF.Min(width, MathF.Min(height, depth)) * .25f);
        b.Add(PartGeometryKernel.BeveledBox(width, height, depth, bevel, material), f.At(x, y, z));
    }

    internal static void Sign(SiteBuilder b, WorldFeature record)
    {
        var p = record.Point();
        var f = PlanFrame.Along(p, record.Pair("direction"));
        var y = b.Ground(p) + record.Number("base");
        var width = record.Number("width"); var height = record.Number("height");
        var depth = record.Number("depth");
        var face = ZoneLooks.S(record.Text("board"), ToonFx.BevelLift);
        if (record.OptionalText("channel") is { } channel) face = face with { Emissive = new(channel) };
        Box(b, f, 0, y, 0, width, height, depth, face);
        if (b.Detail != SiteDetail.Full) return;
        var atlas = new SceneTextAtlas(b.Context.GlobalInputs.Require(SceneTextInputs.AtlasKey));
        var material = ZoneLooks.S(record.Text("ink"), line: LineClass.None);
        var background = b.Context.Catalog.Resolve(face.Lit);
        var builder = new PartBuilder();
        var text = record.Text("text");
        var vertical = record.Number("vertical") > 0;
        var icon = record.OptionalText("icon");
        if (vertical)
        {
            var glyphs = text.EnumerateRunes().ToArray();
            var reserve = icon is null ? 1 : 2;
            var pitch = height / (glyphs.Length + reserve);
            for (var i = 0; i < glyphs.Length; i++)
                Lettering.Write(builder, atlas.Layout(glyphs[i].ToString()), background, 0,
                    height - pitch * (i + reserve), depth * .5f, true, MathF.Min(width, pitch) * .8f, material);
            if (icon == "pulse")
            {
                // An original pulse stroke, with no protected cross, crest or brand silhouette.
                Vector2[] pulse = [new(-.4f, 0), new(-.18f, 0), new(-.05f, .2f), new(.1f, -.2f), new(.2f, 0), new(.4f, 0)];
                for (var i = 0; i + 1 < pulse.Length; i++)
                    builder.AddPipe(new(pulse[i].X * width, height - pitch + pulse[i].Y * width, depth),
                        new(pulse[i + 1].X * width, height - pitch + pulse[i + 1].Y * width, depth),
                        PartStyleDefaults.TrimEdgeBevel, 8, material);
            }
        }
        else
        {
            var run = atlas.Layout(text);
            var letter = MathF.Min(height * .7f, width * .9f / run.Width);
            Lettering.Write(builder, run, background, 0, height * .5f, depth * .5f, true, letter, material);
        }
        var bounds = builder.Measure();
        b.Add(builder.Finish(new(bounds.Min, bounds.Max)), f.At(0, y, 0));
    }
}
