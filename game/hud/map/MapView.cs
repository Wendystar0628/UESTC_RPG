using Campus.Contracts.Maps;
using Godot;

namespace Campus.Hud.Map;

/// <summary>
/// 一张地图视口：中心 (x, z)（m）、每米逻辑像素数、旋转（弧度）。北朝上时屏幕 +x 是东（+x），屏幕 +y 是南（+z），
/// 与世界坐标"x 向东、z 向南"一致。与 map_base 的 instance uniform view 同一套定义。
/// </summary>
public readonly record struct MapView(Vector2 Center, float Ppm, float Rotation)
{
    public Vector4 Uniform => new(Center.X, Center.Y, Ppm, Rotation);

    public Vector2 ToLocal(Vector2 world, Vector2 size)
    {
        var d = (world - Center) * Ppm;
        var c = MathF.Cos(Rotation); var s = MathF.Sin(Rotation);
        return size * 0.5f + new Vector2(c * d.X + s * d.Y, -s * d.X + c * d.Y);
    }

    public Vector2 ToWorld(Vector2 local, Vector2 size)
    {
        var q = local - size * 0.5f;
        var c = MathF.Cos(Rotation); var s = MathF.Sin(Rotation);
        return Center + new Vector2(c * q.X - s * q.Y, s * q.X + c * q.Y) / Ppm;
    }

    public static Vector2 Flat(System.Numerics.Vector3 p) => new(p.X, p.Z);
    public static Vector2 Flat(System.Numerics.Vector2 p) => new(p.X, p.Y);
}

/// <summary>
/// 光标读数用的经纬度：本图坐标是 UTM 格网米（GeoFrame），按原点换算，并扣掉格网收敛角（清水河约 0.55°），
/// 在 1.5 km 内误差小于 1 m。只用于界面读数。
/// </summary>
public sealed class MapGeo(GeoFrame geo)
{
    private readonly double _lat0 = geo.OriginLat;
    private readonly double _lon0 = geo.OriginLon;
    private readonly double _theta = Convergence(geo);

    private static double Convergence(GeoFrame g)
    {
        var zone = g.Epsg % 100;
        if (g.Epsg / 100 is not (326 or 327) || zone is < 1 or > 60) return 0;
        var cm = -183.0 + 6.0 * zone;
        return Math.Atan(Math.Tan((g.OriginLon - cm) * Math.PI / 180) * Math.Sin(g.OriginLat * Math.PI / 180));
    }

    public (double Lat, double Lon) LatLon(Vector2 world)
    {
        double east = world.X, north = -world.Y;
        var nTrue = -east * Math.Sin(_theta) + north * Math.Cos(_theta);
        var eTrue = east * Math.Cos(_theta) + north * Math.Sin(_theta);
        var phi = _lat0 * Math.PI / 180;
        var mLat = 111132.92 - 559.82 * Math.Cos(2 * phi) + 1.175 * Math.Cos(4 * phi);
        var mLon = 111412.84 * Math.Cos(phi) - 93.5 * Math.Cos(3 * phi);
        return (_lat0 + nTrue / mLat, _lon0 + eTrue / mLon);
    }

    public string Format(Vector2 world)
    {
        var (lat, lon) = LatLon(world);
        return FormattableString.Invariant($"{Math.Abs(lat):0.0000}°{(lat >= 0 ? 'N' : 'S')} {Math.Abs(lon):0.0000}°{(lon >= 0 ? 'E' : 'W')}");
    }
}
