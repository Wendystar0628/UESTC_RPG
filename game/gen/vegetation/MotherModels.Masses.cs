using System.Globalization;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Vegetation;

public static partial class MotherModels
{
    /// <summary>
    /// Icosphere level of one mass: 2 at L0+ and 1 at L0 (ENG-PERF; 23d: card and mixed masses as well, with their
    /// hidden faces left out, so the near outline is round instead of faceted); L0 then steps the most exposed up to 2
    /// within its allowance. L1 uses merged masses at level 1.
    /// </summary>
    private static int BlobDetail(VegLevel level) => level switch
    {
        VegLevel.L0Plus => 2,
        VegLevel.L0 or VegLevel.L1 => 1,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown vegetation level.")
    };

    /// <summary>Masses drawn at a level, with their plan index, radius scale and detail (L1 keeps the enlarged core masses).</summary>
    private static List<(Blob Blob, int Index, float Scale, int Detail)> BlobsFor(TreePlan plan, VegLevel level)
    {
        var list = new List<(Blob, int, float, int)>();
        var crown = plan.Species.Crown;
        if (level == VegLevel.L1)
        {
            var merged = plan.MergedBlobs(MaxMassesL1);
            // Merged masses take indices after the plan's own, so their bumps differ from any near-level blob.
            for (var i = 0; i < merged.Count; i++) list.Add((merged[i], plan.Blobs.Count + i, 1f, 1));
            return list;
        }
        for (var i = 0; i < plan.Blobs.Count; i++)
        {
            var blob = plan.Blobs[i];
            list.Add((blob, i, 1f, BlobDetail(level)));
        }
        return list;
    }

    private static int CostOf(List<(Blob Blob, int Index, float Scale, int Detail)> blobs) =>
        blobs.Sum(b => Icosphere.Get(b.Detail).Index.Length / 3);

    /// <summary>
    /// The faces of each near mass that no other mass hides (23d). A face is hidden when all three corners lie inside one
    /// other mass of the same set, at least <see cref="HiddenMargin"/> of its radius plus a level-1 facet below its lumpy
    /// surface. Kept faces are cached per mass and detail; Reset after masses were pruned (they hide nothing any more).
    /// </summary>
    private sealed class HiddenFaces
    {
        private readonly TreePlan _plan;
        private readonly Dictionary<int, Vector4[]> _bumps = [];
        private readonly Dictionary<(int Index, int Detail), int[]> _kept = [];
        private List<(Blob Blob, int Index)> _set = [];

        public HiddenFaces(TreePlan plan, List<(Blob Blob, int Index, float Scale, int Detail)> blobs)
        {
            _plan = plan;
            Reset(blobs);
        }

        public void Reset(List<(Blob Blob, int Index, float Scale, int Detail)> blobs)
        {
            _set = blobs.Select(b => (b.Blob, b.Index)).ToList();
            foreach (var (blob, index) in _set)
                if (!_bumps.ContainsKey(index)) _bumps[index] = Bumps(_plan, blob, index);
            _kept.Clear();
        }

        public int Cost(List<(Blob Blob, int Index, float Scale, int Detail)> blobs) => blobs.Sum(b => Kept(b.Index, b.Detail).Length);

        /// <summary>Kept faces of a mass, as offsets into its icosphere's index list (multiples of 3).</summary>
        public int[] Kept(int index, int detail)
        {
            if (_kept.TryGetValue((index, detail), out var cached)) return cached;
            var blob = _set.First(b => b.Index == index).Blob;
            var sphere = Icosphere.Get(detail);
            var bumps = _bumps[index];
            var corners = sphere.Vertices.Select(d => Surface(blob.Center, blob.Radius, blob.Lump, bumps, d)).ToArray();
            var inside = new int[corners.Length];
            for (var v = 0; v < corners.Length; v++)
            {
                inside[v] = -1;
                for (var o = 0; o < _set.Count; o++)
                {
                    if (_set[o].Index == index || !Swallows(_set[o].Blob, _bumps[_set[o].Index], corners[v])) continue;
                    inside[v] = o;
                    break;
                }
            }
            var kept = new List<int>(sphere.Index.Length / 3);
            for (var t = 0; t < sphere.Index.Length; t += 3)
            {
                // Corners may sit in different masses: the face is hidden only if the first corner's mass holds all three.
                var o = inside[sphere.Index[t]];
                var hidden = o >= 0;
                for (var k = 1; hidden && k < 3; k++)
                {
                    var v = sphere.Index[t + k];
                    hidden = inside[v] == o || (inside[v] >= 0 && Swallows(_set[o].Blob, _bumps[_set[o].Index], corners[v]));
                }
                if (!hidden) kept.Add(t);
            }
            var result = kept.ToArray();
            _kept[(index, detail)] = result;
            return result;
        }

        /// <summary>Whether p lies inside the drawn surface of mass o by the margin (the inverse of <see cref="Surface"/>).</summary>
        private static bool Swallows(Blob o, Vector4[] bumps, Vector3 p)
        {
            var q = p - o.Center;
            // The lumpy surface never reaches past (1 + 1.05·lump) radii (and the squashed bottom less): far points are outside.
            if (q.LengthSquared() > o.Radius * o.Radius * (1f + 1.05f * o.Lump) * (1f + 1.05f * o.Lump)) return false;
            if (q.Y < 0f) q.Y /= 0.7f;
            var length = q.Length();
            if (length < 1e-4f) return true;
            var d = q / length;
            var sum = 0f;
            foreach (var b in bumps)
            {
                var k = Math.Clamp((Vector3.Dot(d, new Vector3(b.X, b.Y, b.Z)) - 0.78f) / 0.22f, 0f, 1f);
                sum += ScalarMath.Smoothstep(k) * b.W;
            }
            var r = o.Radius * (1f + o.Lump * (Math.Min(sum, 1.4f) - 0.35f));
            return length < r * (1f - FacetSag) - HiddenMargin * o.Radius;
        }
    }

    /// <summary>Non-core masses, least exposed first: exposure is the share of a mass's surface outside every other mass.</summary>
    private static List<int> ByExposure(TreePlan plan, List<(Blob Blob, int Index, float Scale, int Detail)> blobs)
    {
        var probe = Icosphere.Get(1).Vertices;
        var order = new List<(int I, float E)>();
        for (var i = 0; i < blobs.Count; i++)
        {
            var (blob, index, _, _) = blobs[i];
            if (blob.Core) continue;
            var bumps = Bumps(plan, blob, index);
            var open = 0;
            foreach (var d in probe)
            {
                var p = Surface(blob.Center, blob.Radius, blob.Lump, bumps, d);
                if (!blobs.Any(o => o.Blob != blob && Vector3.Distance(p, o.Blob.Center) < o.Blob.Radius * 0.95f)) open++;
            }
            order.Add((i, open / (float)probe.Length));
        }
        return order.OrderBy(o => o.E).ThenBy(o => o.I).Select(o => o.I).ToList();
    }

    /// <summary>
    /// Fits the masses into a budget: drops the cardless non-core masses that show least (with the shell normals a hidden
    /// mass adds nothing but triangles), then steps the least exposed of the rest down one icosphere level, not below
    /// minDetail (L0+ keeps level 1: a bare icosahedron would leave its cards hovering over the flat faces up close).
    /// </summary>
    private static void Prune(TreePlan plan, List<(Blob Blob, int Index, float Scale, int Detail)> blobs, HashSet<int> hosts, int budget, int minDetail,
        Func<int> cost)
    {
        while (cost() > budget)
        {
            var free = ByExposure(plan, blobs).Where(i => !hosts.Contains(blobs[i].Index)).ToList();
            if (free.Count == 0) break;
            blobs.RemoveAt(free[0]);
        }
        foreach (var i in ByExposure(plan, blobs))
        {
            if (cost() <= budget) break;
            var (blob, index, scale, detail) = blobs[i];
            if (detail > minDetail) blobs[i] = (blob, index, scale, detail - 1);
        }
    }

    // ---------------------------------------------------------------------------------------------------------
    // Crown masses: lumpy icospheres that only shape the silhouette. Their normals come from the crown shell, so
    // neighbouring masses never start their own light and shade (max-011 review).
    // ---------------------------------------------------------------------------------------------------------

    private static void WriteBlob(VegetationPrototypeWriter w, TreePlan plan, CrownShell shell, Blob blob, int index, float scale, int detail, Ids ids,
        float tag = -1f, int[]? faces = null)
    {
        var sphere = Icosphere.Get(detail);
        var bumps = Bumps(plan, blob, index);
        var radius = blob.Radius * scale;
        var first = w.VertexCount;
        foreach (var d in sphere.Vertices)
        {
            var p = Surface(blob.Center, radius, blob.Lump, bumps, d);
            var n = shell.Normal(p);
            var depth = Math.Clamp(shell.Depth(p) / MathF.Max(0.1f, shell.Diameter * 0.15f), 0f, 1f);
            var ao = 1f - MassAoDepth * ScalarMath.Smoothstep(depth);
            w.Add(p, n, ao, ids.Leaf, Gradient(plan, p, n), new Vector4(blob.Center, blob.Hash),
                new Vector4(blob.Anchor, blob.BloomHash), new Vector4(blob.Center, tag), Vector2.Zero);
        }
        // Icosphere winding faces outward with a counter-clockwise front; D0 meshes take the opposite order.
        if (faces is null)
        {
            for (var t = 0; t < sphere.Index.Length; t += 3)
                w.Tri(first + sphere.Index[t], first + sphere.Index[t + 2], first + sphere.Index[t + 1]);
            return;
        }
        foreach (var t in faces) w.Tri(first + sphere.Index[t], first + sphere.Index[t + 2], first + sphere.Index[t + 1]);
    }

    private static Vector4[] Bumps(TreePlan plan, Blob blob, int index)
    {
        var rng = new Rng(StableHash.Hash32(0x23A1u, plan.Species.Id, plan.Variant.ToString(CultureInfo.InvariantCulture),
            index.ToString(CultureInfo.InvariantCulture)));
        var bumps = new Vector4[blob.Bumps];
        for (var i = 0; i < bumps.Length; i++)
        {
            var z = rng.NextFloat() * 2f - 1f;
            var phi = rng.NextFloat() * 6.2831853f;
            var s = MathF.Sqrt(MathF.Max(0f, 1f - z * z));
            bumps[i] = new Vector4(s * MathF.Cos(phi), z, s * MathF.Sin(phi), 0.6f + 0.8f * rng.NextFloat());
        }
        return bumps;
    }

    /// <summary>Lumpy surface point along unit direction d: soft bumps of the given amplitude, bottom squashed to 0.7.</summary>
    private static Vector3 Surface(Vector3 center, float radius, float lump, Vector4[] bumps, Vector3 d)
    {
        var sum = 0f;
        foreach (var b in bumps)
        {
            var k = Math.Clamp((Vector3.Dot(d, new Vector3(b.X, b.Y, b.Z)) - 0.78f) / 0.22f, 0f, 1f);
            sum += ScalarMath.Smoothstep(k) * b.W;
        }
        var r = radius * (1f + lump * (Math.Min(sum, 1.4f) - 0.35f));
        var p = d * r;
        if (p.Y < 0f) p.Y *= 0.7f;
        return center + p;
    }
}
