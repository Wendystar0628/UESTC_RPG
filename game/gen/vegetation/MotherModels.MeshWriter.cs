using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;

namespace Campus.Gen.Vegetation;

public static partial class MotherModels
{
    /// <summary>Growable vertex arrays for one GeometryBundle.</summary>
    private sealed class MeshWriter(bool withUv)
    {
        private readonly List<float> _position = [];
        private readonly List<float> _normal = [];
        private readonly List<float> _ao = [];
        private readonly List<ushort> _color = [];
        private readonly List<byte> _spare = [];
        private readonly List<float> _c0 = [];
        private readonly List<float> _c1 = [];
        private readonly List<float> _c2 = [];
        private readonly List<float> _uv = [];
        private readonly List<int> _index = [];

        public int VertexCount => _ao.Count;
        public int Triangles => _index.Count / 3;
        public int Cards { get; set; }

        public int Add(Vector3 p, Vector3 n, float ao, ushort color, byte spare, Vector4 c0, Vector4 c1, Vector4 c2, Vector2 uv)
        {
            _position.AddRange([p.X, p.Y, p.Z]);
            _normal.AddRange([n.X, n.Y, n.Z]);
            _ao.Add(Math.Clamp(ao, 0f, 1f));
            _color.Add(color);
            _spare.Add(spare);
            _c0.AddRange([c0.X, c0.Y, c0.Z, c0.W]);
            _c1.AddRange([c1.X, c1.Y, c1.Z, c1.W]);
            _c2.AddRange([c2.X, c2.Y, c2.Z, c2.W]);
            if (withUv) _uv.AddRange([uv.X, uv.Y]);
            return _ao.Count - 1;
        }

        public void Tri(int a, int b, int c) => _index.AddRange([a, b, c]);

        public Box3 Bounds()
        {
            if (_position.Count == 0) return new Box3(Vector3.Zero, Vector3.Zero);
            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);
            for (var i = 0; i < _position.Count; i += 3)
            {
                var p = new Vector3(_position[i], _position[i + 1], _position[i + 2]);
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
            return new Box3(min, max);
        }

        public GeometryBundle Bundle(ToonSpec spec, byte flags)
        {
            GeometryVertexLimits.Require(VertexCount);
            var bundle = new GeometryBundle(new ToonRef(spec), [.. _position], [.. _normal], [.. _index], [.. _ao], withUv ? [.. _uv] : null)
            {
                ColorIndices = [.. _color],
                Reserved = [.. _spare],
                Flags = flags == 0 ? null : Enumerable.Repeat(flags, _ao.Count).ToArray(),
                Custom0 = [.. _c0],
                Custom1 = [.. _c1],
                Custom2 = [.. _c2]
            };
            bundle.ValidateVertexData();
            return bundle;
        }
    }
}
