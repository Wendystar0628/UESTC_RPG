using N = System.Numerics;
using G = Godot;

namespace Campus.Kernel.Renderer;

public static class NumericsBridge
{
    public static G.Vector2 ToGodot(this N.Vector2 value) => new(value.X, value.Y);
    public static N.Vector2 ToNumerics(this G.Vector2 value) => new(value.X, value.Y);
    public static G.Vector3 ToGodot(this N.Vector3 value) => new(value.X, value.Y, value.Z);
    public static N.Vector3 ToNumerics(this G.Vector3 value) => new(value.X, value.Y, value.Z);
    public static G.Quaternion ToGodot(this N.Quaternion value) => new(value.X, value.Y, value.Z, value.W);
    public static N.Quaternion ToNumerics(this G.Quaternion value) => new(value.X, value.Y, value.Z, value.W);
    public static G.Transform3D ToGodot(this N.Matrix4x4 value) => new(
        new G.Basis(new(value.M11, value.M12, value.M13), new(value.M21, value.M22, value.M23), new(value.M31, value.M32, value.M33)),
        new(value.M41, value.M42, value.M43));
    public static N.Matrix4x4 ToNumerics(this G.Transform3D value) => new(
        value.Basis.X.X, value.Basis.X.Y, value.Basis.X.Z, 0,
        value.Basis.Y.X, value.Basis.Y.Y, value.Basis.Y.Z, 0,
        value.Basis.Z.X, value.Basis.Z.Y, value.Basis.Z.Z, 0,
        value.Origin.X, value.Origin.Y, value.Origin.Z, 1);
}
