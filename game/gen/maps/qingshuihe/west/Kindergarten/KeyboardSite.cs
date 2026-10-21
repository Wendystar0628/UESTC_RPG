using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.Sites;
using static Campus.Gen.Sites.ZoneLooks;

namespace Campus.Gen.Maps.Qingshuihe.West.Kindergarten;

/// <summary>The three unlettered keyboard rows on the authored northeast face. The ordinary building remains game/gen/buildings/'s.</summary>
public sealed class KeyboardSite : ZoneSite
{
    public override string Id => "19c.keyboard";

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        [SiteRanges.Bounds(world.Feature(WestIds.KeyboardFace), 0.5f)];

    public override void Build(SiteBuilder b)
    {
        var face = b.Context.World.Feature(WestIds.KeyboardFace);
        var (a, z) = face.PointPair();
        // The authored direction runs southeast; its right side faces northeast.
        var f = PlanFrame.Toward(a, z);
        var ground = b.Context.World.Terrain.PadY(new(face.Text("parent")));
        var height = face.Number("height");
        var pitch = face.Number("pitch");
        var gap = face.Number("gap");
        var total = face.Number("units") * pitch;
        var margin = (Vector2.Distance(a, z) - total) * .5f;
        var outward = -1f;
        GridFacade.Box(b, f, 0, Vector2.Distance(a, z), ground + .15f, ground + height,
            -.22f, -.02f, S("wall.greyWhite", ToonFx.BevelLift), .025f);
        var rows = face.Props!.Value.GetProperty("rows");
        var floor = height / rows.GetArrayLength();
        var rowIndex = 0;
        foreach (var row in rows.EnumerateArray())
        {
            var x = margin;
            foreach (var key in row.EnumerateArray())
            {
                var width = key.GetProperty("units").GetSingle() * pitch;
                var ink = S(key.GetProperty("color").GetString()!, ToonFx.GlassStreak | ToonFx.Glare, LineClass.Fine, 0) with { Kind = ToonKind.Glass };
                var bottom = ground + floor * rowIndex + .7f;
                var top = ground + floor * (rowIndex + 1) - .45f;
                GridFacade.Box(b, f, x, x + width, bottom - .12f, top + .12f, -.38f, -.2f,
                    S("frame.alu", ToonFx.BevelLift, LineClass.Fine), .025f);
                GridFacade.Box(b, f, x + gap * .5f, x + width - gap * .5f, bottom, top,
                    outward * .42f, outward * .38f, ink, .025f);
                x += width;
            }
            rowIndex++;
        }
    }
}
