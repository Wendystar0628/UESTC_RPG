using Campus.Contracts;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using Godot;

namespace Campus.Hud.Map;

/// <summary>
/// 把 IAtlasBaseSource 发布的四张 SDF 绑到 map_base 材质（SYS-ATLAS-UI）：每帧只比 Rev，变了才释放旧图、建新图；
/// 就绪只指四张 SDF，标签、字形、区域各自的能力不在这里等。纹理与它们的 Rect 都取自快照，本类拥有创建出的纹理并负责释放。
/// </summary>
internal sealed class MapBaseBinding(IAtlasBaseSource source, ShaderMaterial material) : IDisposable
{
    private static readonly StringName SdfA = new("map_sdf_a");
    private static readonly StringName SdfB = new("map_sdf_b");
    private static readonly StringName SdfOut = new("map_sdf_out");
    private static readonly StringName SdfRing = new("map_sdf_ring");
    private static readonly StringName RectA = new("rect_a");
    private static readonly StringName RectOut = new("rect_out");
    private static readonly StringName RectRing = new("rect_ring");
    private static readonly StringName DataReady = new("data_ready");

    private readonly List<ImageTexture> _owned = [];
    private long _rev = long.MinValue;

    /// <summary>加载或切图时调：清 data_ready 与旧图绑定，下一次 <see cref="Poll"/> 重新判断。</summary>
    public void Clear()
    {
        _rev = long.MinValue;
        Release();
    }

    /// <summary>每帧调。Rev 没变什么都不做；变了按快照重绑（可能就绪，也可能刚被清空）。<paramref name="map"/> 为 null 表示没有当前图。</summary>
    public void Poll(MapId? map)
    {
        if (map is null || source.Rev == _rev) return;
        _rev = source.Rev;
        Release();
        var snapshot = source.Ready ? source.Snapshot : null;
        if (snapshot is null || snapshot.Map != map.Value) return;
        material.SetShaderParameter(SdfA, Own(snapshot.SdfA));
        material.SetShaderParameter(SdfB, Own(snapshot.SdfB));
        material.SetShaderParameter(SdfOut, Own(snapshot.SdfOut));
        material.SetShaderParameter(SdfRing, Own(snapshot.SdfRing));
        material.SetShaderParameter(RectA, RectUniform(snapshot.SdfA.Rect));
        material.SetShaderParameter(RectOut, RectUniform(snapshot.SdfOut.Rect));
        material.SetShaderParameter(RectRing, RectUniform(snapshot.SdfRing.Rect));
        material.SetShaderParameter(DataReady, 1f);
    }

    public void Dispose() => Clear();

    private void Release()
    {
        if (GodotObject.IsInstanceValid(material))
        {
            material.SetShaderParameter(DataReady, 0f);
            foreach (var name in new[] { SdfA, SdfB, SdfOut, SdfRing }) material.SetShaderParameter(name, new Variant());
        }
        foreach (var texture in _owned) texture.Dispose();
        _owned.Clear();
    }

    private ImageTexture Own(AtlasBaseTexture t)
    {
        using var image = Image.CreateFromData(t.Width, t.Height, false, Image.Format.Rgba8, t.Rgba8.Span);
        image.GenerateMipmaps();
        var texture = ImageTexture.CreateFromImage(image);
        _owned.Add(texture);
        return texture;
    }

    private static Vector4 RectUniform(MapRect r) => new(r.Min.X, r.Min.Y, r.Max.X - r.Min.X, r.Max.Y - r.Min.Y);
}
