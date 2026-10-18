using System.Globalization;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Vegetation;

/// <summary>A tapered polyline: the trunk (level 0), a main branch (1) or a secondary branch (2).</summary>
internal sealed class Limb(int level)
{
    public int Level { get; } = level;
    public List<Vector3> Points { get; } = [];
    public List<float> Radii { get; } = [];
    public Vector3 Tip => Points[^1];
}

/// <summary>
/// One crown mass. Card and mixed crowns use lumpy blobs (3–7 mains, 2–3 subs each); cloud crowns use the
/// small balls of WLD-VEGETATION (core 0.8r, satellites, bridges). Anchor is the branch tip the cards of this
/// blob retreat to in winter (WLD-VEGETATION). Group is the L1 merge group: −1 for blobs dropped at L1.
/// </summary>
internal sealed record Blob(Vector3 Center, float Radius, float Lump, int Bumps, float Hash, float BloomHash,
    Vector3 Anchor, int Group, bool Core);

/// <summary>
/// Branch skeleton and crown masses of one mother model, decided once from the species and variant and then meshed
/// per level. Everything is drawn from one seeded stream, so repeated builds on the same machine produce identical mother models.
/// </summary>
internal sealed class TreePlan
{
    private const float Tau = 6.2831853f;
    private Rng _rng;

    public SpeciesInfo Species { get; }
    public TreeForm Form { get; }
    public int Variant { get; }
    public float Height { get; }
    public float CrownRx { get; }
    public float CrownRy { get; }
    public Vector3 CrownCenter { get; }
    public float BranchY { get; }
    public float TrunkRadius { get; }
    public List<Limb> Limbs { get; } = [];
    public List<Blob> Blobs { get; } = [];

    /// <summary>
    /// L1 masses (ENG-PERF "6–10 merged blobs"): one sphere per branch group covering its core and sub-blobs,
    /// so the far crown keeps the near crown's volume; ungrouped masses (bridges, satellites, fillers) fold into the
    /// nearest. Groups beyond <paramref name="max"/> merge pairwise, closest first, to hold the L1 triangle target.
    /// </summary>
    public List<Blob> MergedBlobs(int max)
    {
        var groups = Blobs.Where(b => b.Core).ToList();
        var sets = groups.Select(core => Blobs.Where(b => b == core || (!b.Core && Nearest(groups, b.Center) == core)).ToList()).ToList();
        while (sets.Count > Math.Max(1, max))
        {
            var (bi, bj, best) = (0, 1, float.MaxValue);
            for (var i = 0; i < sets.Count; i++)
                for (var j = i + 1; j < sets.Count; j++)
                {
                    var d = Vector3.Distance(Merge(sets[i]).Center, Merge(sets[j]).Center);
                    if (d < best) (bi, bj, best) = (i, j, d);
                }
            sets[bi].AddRange(sets[bj]);
            sets.RemoveAt(bj);
        }
        return sets.Select(Merge).ToList();
    }

    private static Blob Merge(List<Blob> members)
    {
        var core = members.FirstOrDefault(b => b.Core) ?? members[0];
        var weight = members.Sum(b => b.Radius * b.Radius * b.Radius);
        var center = members.Aggregate(Vector3.Zero, (acc, b) => acc + b.Center * (b.Radius * b.Radius * b.Radius)) / weight;
        var radius = members.Max(b => Vector3.Distance(b.Center, center) + b.Radius) * 0.86f;
        return core with { Center = center, Radius = MathF.Max(radius, core.Radius), Lump = MathF.Max(core.Lump, 0.15f), Bumps = 12 };
    }

    /// <summary>Directions sampled when looking for holes in the crown (WLD-VEGETATION: no see-through gaps).</summary>
    public const int FillSamples = 96;

    /// <summary>Most filler masses one crown may receive (cloud crowns fewer: their gaps between clouds are part of the look).</summary>
    public const int MaxFill = 24;
    public const int MaxFillCloud = 6;

    /// <summary>
    /// Plugs see-through holes (max-011: metasequoia and ginkgo crowns): probes the fitted crown ellipsoid at 78% of its
    /// radius over the sides and top, and wherever no mass covers the probe adds a filler mass on that ray, as large as
    /// fits inside the ellipsoid. The underside is left open: looking up into a crown shows its branches.
    /// </summary>
    private void FillCrown()
    {
        if (Blobs.Count == 0) return;
        var shell = new CrownShell(this);
        var tips = BranchTips().ToList();
        var added = 0;
        var max = Species.Crown == CrownType.Cloud ? MaxFillCloud : MaxFill;
        for (var i = 0; i < FillSamples && added < max; i++)
        {
            var z = 1f - 2f * (i + 0.5f) / FillSamples;
            if (z < -0.3f) continue;
            var s = MathF.Sqrt(MathF.Max(0f, 1f - z * z));
            var phi = i * 2.3999632f;
            var e = shell.EllipsoidPoint(new Vector3(s * MathF.Cos(phi), z, s * MathF.Sin(phi)));
            var probe = shell.Center + (e - shell.Center) * 0.78f;
            if (Blobs.Any(b => Vector3.Distance(probe, b.Center) < b.Radius * 0.92f)) continue;
            // Slide the filler along the ray until it is big enough to swallow the probe (narrow cone tops need it outward).
            var center = probe;
            var radius = 0f;
            foreach (var f in new[] { 0.45f, 0.55f, 0.65f, 0.72f })
            {
                center = shell.Center + (e - shell.Center) * f;
                radius = MathF.Max(0.3f, 0.95f * shell.EllipsoidDepth(center));
                if (radius >= Vector3.Distance(center, probe) * 1.05f) break;
            }
            var anchor = tips.Count > 0 ? tips.MinBy(t => Vector3.DistanceSquared(t, center)) : center;
            if (Species.Crown == CrownType.Cloud) AddBall(center, radius, anchor, -1, false);
            else AddBlob(center, radius, anchor, -1, false);
            added++;
        }
    }

    private static Blob Nearest(List<Blob> cores, Vector3 p)
    {
        var best = cores[0];
        foreach (var c in cores)
            if (Vector3.DistanceSquared(c.Center, p) < Vector3.DistanceSquared(best.Center, p)) best = c;
        return best;
    }
    private readonly List<(Vector3 Center, float Radius, Vector3 Tip)> _clusters = [];

    /// <summary>Group of bridge balls between clouds (kept at L1 as merged masses).</summary>
    public const int BridgeGroup = -2;

    /// <summary>Branch tips cards retreat to in winter: every branch end, plus the trunk top of leaders and stemless forms.</summary>
    public IEnumerable<Vector3> BranchTips()
    {
        var branches = Limbs.Where(l => l.Level > 0).Select(l => l.Tip).ToList();
        if (Form.Leader || branches.Count == 0) branches.AddRange(Limbs.Where(l => l.Level == 0).Select(l => l.Tip));
        return branches;
    }

    public TreePlan(SpeciesInfo species, int variant)
    {
        Species = species;
        Variant = variant;
        Form = TreeForms.Get(species.Form);
        _rng = new Rng(StableHash.Hash32(0x23A0u, "mother", species.Id, variant.ToString(CultureInfo.InvariantCulture)));
        Height = TreeForms.HeightOf(species, variant);
        var cs = TreeForms.CrownScale(variant);
        CrownRx = Form.CrownRadius * Height * cs;
        var bottom = Form.CrownBottom * Height;
        CrownRy = (Height - bottom) * 0.5f;
        var lean = Azimuth(Next() * Tau) * (TreeForms.Asymmetry(variant) * CrownRx);
        CrownCenter = new Vector3(lean.X, bottom + CrownRy, lean.Z);
        TrunkRadius = MathF.Max(0.035f, Form.TrunkRadius * Height * TreeForms.TrunkScale(variant));
        BranchY = Form.ClearTrunk * Height * (variant == 1 ? 1.05f : 1f);

        if (Form.Id == "palm") BuildPalm();
        else
        {
            if (TreeForms.IsBall(Form.Id)) BuildShrub();
            else BuildTree();
            FillCrown();
        }
    }

    /// <summary>Crown envelope radius at relative crown height t ∈ [−1, 1], blending an ellipsoid towards a cone.</summary>
    public float Envelope(float t)
    {
        t = Math.Clamp(t, -1f, 1f);
        var ellipse = MathF.Sqrt(MathF.Max(0f, 1f - t * t));
        var cone = (1f - t) * 0.5f * (0.6f + 0.4f * ScalarMath.Smoothstep((t + 1f) / 0.3f));
        return CrownRx * (ScalarMath.Lerp(ellipse, cone, Form.Taper));
    }

    public bool InsideEnvelope(Vector3 p, float margin)
    {
        var t = (p.Y - CrownCenter.Y) / CrownRy;
        if (t < -1.05f || t > 1.02f) return false;
        var d = new Vector2(p.X - CrownCenter.X, p.Z - CrownCenter.Z).Length();
        return d <= Envelope(t) * margin;
    }

    public float Next() => _rng.NextFloat();
    public float Range(float a, float b) => ScalarMath.Lerp(a, b, _rng.NextFloat());
    public int Count(int min, int max) => max <= min ? min : min + _rng.NextInt(max - min + 1);

    public Vector3 RandomDirection()
    {
        var z = Range(-1f, 1f);
        var phi = Range(0f, Tau);
        var s = MathF.Sqrt(MathF.Max(0f, 1f - z * z));
        return new Vector3(s * MathF.Cos(phi), z, s * MathF.Sin(phi));
    }

    public static Vector3 Azimuth(float angle) => new(MathF.Cos(angle), 0f, MathF.Sin(angle));

    private void BuildTree()
    {
        var stems = Math.Max(1, Form.Stems);
        var trunkTop = Form.Leader ? Height * 0.94f : ScalarMath.Lerp(BranchY, Height, 0.16f);
        var stemTops = new List<Limb>();
        var stemAngle = Next() * Tau;
        for (var s = 0; s < stems; s++)
        {
            // Multi-stem forms splay outwards from a shared root; single trunks wander a little.
            var splay = stems > 1 ? Azimuth(stemAngle + s * Tau / stems) * (Height * Range(0.06f, 0.1f)) : Vector3.Zero;
            var wander = Azimuth(Next() * Tau) * (Height * 0.012f);
            var trunk = new Limb(0);
            AddTrunkPoint(trunk, -MotherModels.Burial, Vector3.Zero, 1f);
            var steps = Math.Max(4, (int)MathF.Ceiling(trunkTop / 1.1f));
            for (var i = 0; i <= steps; i++)
            {
                var y = trunkTop * i / steps;
                var f = y / trunkTop;
                var offset = splay * (f * f * 0.8f + f * 0.2f) + wander * MathF.Sin(f * 3.1f) + (CrownCenter with { Y = 0f }) * (f * f * 0.5f);
                var radius = Form.Leader
                    ? (y < BranchY ? 1f - 0.35f * y / BranchY : 0.65f - 0.53f * (y - BranchY) / MathF.Max(0.1f, trunkTop - BranchY))
                    : 1f - 0.4f * f;
                AddTrunkPoint(trunk, y, offset, radius / MathF.Sqrt(stems));
            }
            if (Form.Leader) trunk.Radii[^1] = TrunkRadius * 0.1f;
            Limbs.Add(trunk);
            stemTops.Add(trunk);
        }

        var mains = Count(Form.MainMin, Form.MainMax);
        var phase = Next() * Tau;
        var asym = TreeForms.Asymmetry(Variant);
        var leanAngle = MathF.Abs(CrownCenter.X) + MathF.Abs(CrownCenter.Z) > 1e-4f ? MathF.Atan2(CrownCenter.Z, CrownCenter.X) : 0f;
        for (var i = 0; i < mains; i++)
        {
            var angle = phase + (i + Range(-0.3f, 0.3f)) * Tau / mains;
            if (asym > 0f) angle = BendTowards(angle, leanAngle, asym * 1.6f);
            var trunk = stemTops[i % stems];
            Vector3 origin;
            Vector3 tip;
            float baseRadius;
            if (Form.Leader)
            {
                var span = Height * 0.88f - BranchY;
                var y = BranchY + span * (i + 0.5f + Range(-0.25f, 0.25f)) / mains;
                origin = PointAt(trunk, y);
                var t = (y - CrownCenter.Y) / CrownRy;
                var reach = Envelope(t + 0.12f) * Range(0.78f, 0.92f);
                var pitch = (Form.MainPitchDeg + Range(-8f, 8f)) * (MathF.PI / 180f);
                var dir = Azimuth(angle) * MathF.Cos(pitch) + Vector3.UnitY * MathF.Sin(pitch);
                tip = origin + dir * (reach / MathF.Max(0.35f, MathF.Cos(pitch)));
                baseRadius = RadiusAtHeight(trunk, y) * Range(0.45f, 0.6f);
            }
            else
            {
                var top = trunk.Tip;
                var y = ScalarMath.Lerp(BranchY, top.Y, Range(0.1f, 0.95f));
                origin = PointAt(trunk, y);
                var t = Range(-0.2f, 0.45f);
                tip = CrownCenter + Azimuth(angle) * (Envelope(t) * Range(0.62f, 0.76f)) + Vector3.UnitY * (t * CrownRy);
                baseRadius = RadiusAtHeight(trunk, y) * Range(0.62f, 0.78f);
            }
            var main = Curve(1, origin, tip, Form.MainPitchDeg, baseRadius, 0.3f, 6);
            Limbs.Add(main);
            AddMainCrown(main, i);
        }

        AddBridges();
        if (Form.Leader)
        {
            // The leader's own top carries a blob (ginkgo, metasequoia).
            var top = stemTops[0].Tip;
            var r = MathF.Max(0.45f, Envelope(0.75f) * 0.75f);
            AddBlob(top - Vector3.UnitY * (r * 0.35f), r, top, 0, true);
            // Leader crowns get a body of masses along the leader (max-011: no sky through a metasequoia or ginkgo); the
            // masses at the branch tips then only shape the tiered outline around it.
            var tips = BranchTips().ToList();
            for (var y = CrownCenter.Y - CrownRy * 0.8f; y < CrownCenter.Y + CrownRy * 0.7f;)
            {
                var rc = MathF.Max(0.35f, Envelope((y - CrownCenter.Y) / CrownRy) * LeaderBody);
                var c = PointAt(stemTops[0], y + rc * 0.5f);
                AddBlob(c, rc, tips.MinBy(t => Vector3.DistanceSquared(t, c)), -1, false);
                y += rc * 1.2f;
            }
        }
        // Neighbouring masses step front and back along the crown radius, so their seams show a depth step (max-110 110c-4).
        CrownStagger.Apply(this);
    }

    /// <summary>Radius of the leader-body masses as a fraction of the crown envelope at their height.</summary>
    public const float LeaderBody = 0.72f;

    private void AddMainCrown(Limb main, int index)
    {
        var length = Length(main);
        var mainTip = main.Tip;
        var outward = Horizontal(mainTip - CrownCenter);
        var secondaries = Count(Form.SecondaryMin, Form.SecondaryMax);
        var tips = new List<Vector3>();
        for (var j = 0; j < secondaries; j++)
        {
            var f = 0.45f + 0.45f * (j + 0.5f) / secondaries + Range(-0.06f, 0.06f);
            var origin = Along(main, f);
            var tangent = Vector3.Normalize(Along(main, MathF.Min(1f, f + 0.05f)) - Along(main, MathF.Max(0f, f - 0.05f)));
            var side = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, tangent) + new Vector3(1e-4f, 0f, 0f));
            var sign = j % 2 == 0 ? 1f : -1f;
            var dir = Vector3.Normalize(tangent + side * (sign * Range(0.45f, 0.85f)) + Vector3.UnitY * Range(0.2f, 0.55f) * (1f - Form.Droop));
            var len = length * Range(0.34f, 0.5f) * (1f - 0.3f * f);
            var tip = origin + dir * len;
            if (Form.Droop > 0f) tip.Y -= len * Form.Droop * Range(0.35f, 0.6f);
            for (var k = 0; k < 6 && !InsideEnvelope(tip, 0.98f); k++) tip = origin + (tip - origin) * 0.82f;
            var secondary = Curve(2, origin, tip, Form.Droop > 0f ? 25f : 35f, RadiusAt(main, f) * 0.62f, 0.35f, 4);
            Limbs.Add(secondary);
            tips.Add(secondary.Tip);
        }

        switch (Species.Crown)
        {
            case CrownType.Card:
            case CrownType.Mixed:
                {
                    var mains = Math.Max(1, Form.MainMax);
                    var rm = MathF.Max(0.4f, (Form.Leader ? Envelope((mainTip.Y - CrownCenter.Y) / CrownRy) * 0.78f : CrownRx * 0.92f / MathF.Cbrt(mains)))
                        * (Species.Crown == CrownType.Mixed ? 1.12f : 1f);
                    var center = mainTip + outward * (rm * 0.15f) + Vector3.UnitY * (rm * 0.1f);
                    AddBlob(center, rm, mainTip, index, true);
                    var subs = Math.Min(3, tips.Count);
                    for (var j = 0; j < subs; j++)
                    {
                        var tip = tips[j];
                        var rs = rm * Range(0.7f, 0.86f);
                        AddBlob(tip + Vector3.UnitY * (rs * 0.15f), rs, tip, index, false);
                    }
                    if (Species.Crown == CrownType.Mixed)
                    {
                        // Mixed crowns are thick: two satellite masses hug each main blob (WLD-VEGETATION).
                        for (var k = 0; k < 2; k++)
                        {
                            var d = Vector3.Normalize(RandomDirection() + outward * 0.8f + Vector3.UnitY * 0.5f);
                            AddBlob(center + d * (rm * 0.62f), rm * Range(0.5f, 0.62f), mainTip, -1, false);
                        }
                    }
                    break;
                }
            case CrownType.Cloud:
                AddCloudCluster(mainTip, outward, index, tips);
                break;
            default:
                throw new InvalidOperationException($"Unknown crown {Species.Crown}.");
        }
    }

    private void AddCloudCluster(Vector3 mainTip, Vector3 outward, int index, List<Vector3> tips)
    {
        // One big cloud: a core ball (0.8r) and 6–8 satellites (0.40–0.58r at 0.55–0.78r, leaning out and up),
        // all with 10–14 bumps of 0.13 and bottoms squashed to 0.7 (WLD-VEGETATION, ART-MODELING).
        var mains = Math.Max(1, Form.MainMax);
        var r = CrownRx * 0.8f / MathF.Cbrt(mains);
        var center = Vector3.Lerp(mainTip, CrownCenter, 0.1f) + Vector3.UnitY * (r * 0.1f);
        AddBall(center, r * 0.8f, mainTip, index, true);
        _clusters.Add((center, r, mainTip));
        var satellites = Form.Id == "shrub" ? 5 : Math.Clamp((26 - mains * 2) / mains, 4, 8);
        for (var k = 0; k < satellites; k++)
        {
            var d = Vector3.Normalize(RandomDirection() + outward * 0.55f + Vector3.UnitY * 0.45f);
            var anchor = tips.Count > 0 ? tips[k % tips.Count] : mainTip;
            AddBall(center + d * (r * Range(0.55f, 0.78f)), r * Range(0.40f, 0.58f), anchor, -1, false);
        }
    }

    /// <summary>Bridge balls between neighbouring clouds, so the clusters read as one crown.</summary>
    private void AddBridges()
    {
        if (_clusters.Count < 2) return;
        for (var i = 0; i < _clusters.Count; i++)
        {
            var a = _clusters[i];
            var b = _clusters[(i + 1) % _clusters.Count];
            if (_clusters.Count == 2 && i == 1) break;
            var mid = (a.Center + b.Center) * 0.5f + Vector3.UnitY * (a.Radius * 0.1f);
            AddBall(mid, (a.Radius + b.Radius) * 0.25f, a.Tip, BridgeGroup, false);
        }
    }

    private void BuildPalm()
    {
        var trunk = new Limb(0);
        var top = Height * Form.ClearTrunk;
        var wander = Azimuth(Next() * Tau) * (Height * 0.02f);
        AddTrunkPoint(trunk, -MotherModels.Burial, Vector3.Zero, 1.15f);
        for (var i = 0; i <= 6; i++)
        {
            var f = i / 6f;
            AddTrunkPoint(trunk, top * f, wander * (f * f), 1f - 0.15f * f);
        }
        Limbs.Add(trunk);
        // The fronds hang from a small head of leaf-coloured mass at the top of the stem.
        AddBall(trunk.Tip + Vector3.UnitY * 0.15f, TrunkRadius * 2.2f, trunk.Tip, 0, true);
    }

    private void BuildShrub()
    {
        // A clipped ball: three clusters around a low centre, no visible stem (WLD-VEGETATION shrub balls).
        var trunk = new Limb(0);
        AddTrunkPoint(trunk, -MotherModels.Burial, Vector3.Zero, 1f);
        AddTrunkPoint(trunk, Height * 0.3f, Vector3.Zero, 0.7f);
        Limbs.Add(trunk);
        var phase = Next() * Tau;
        var clusters = Form.MainMax;
        for (var i = 0; i < clusters; i++)
        {
            var dir = Azimuth(phase + i * Tau / clusters);
            var tip = new Vector3(CrownCenter.X, Height * 0.42f, CrownCenter.Z) + dir * (CrownRx * 0.36f);
            AddCloudCluster(tip, dir, i, [trunk.Tip]);
        }
        AddBridges();
    }

    private void AddBlob(Vector3 center, float radius, Vector3 anchor, int group, bool core) =>
        Blobs.Add(new Blob(center, radius, Form.Lumps, 6 + _rng.NextInt(3), Next(), Next(), anchor, group, core));

    private void AddBall(Vector3 center, float radius, Vector3 anchor, int group, bool core) =>
        Blobs.Add(new Blob(center, radius, 0.13f, 10 + _rng.NextInt(5), Next(), Next(), anchor, group, core));

    private void AddTrunkPoint(Limb trunk, float y, Vector3 offset, float radiusScale)
    {
        trunk.Points.Add(new Vector3(offset.X, y, offset.Z));
        trunk.Radii.Add(TrunkRadius * radiusScale);
    }

    /// <summary>A quadratic curve from origin to tip leaving at the given pitch, radius tapering to tipRatio.</summary>
    private static Limb Curve(int level, Vector3 origin, Vector3 tip, float pitchDeg, float radius, float tipRatio, int points)
    {
        var limb = new Limb(level);
        var span = tip - origin;
        var flat = Horizontal(span);
        var pitch = pitchDeg * (MathF.PI / 180f);
        var leave = Vector3.Normalize(flat * MathF.Cos(pitch) + Vector3.UnitY * MathF.Sin(pitch));
        var control = origin + leave * (span.Length() * 0.45f);
        for (var i = 0; i < points; i++)
        {
            var t = i / (points - 1f);
            var a = Vector3.Lerp(origin, control, t);
            var b = Vector3.Lerp(control, tip, t);
            limb.Points.Add(Vector3.Lerp(a, b, t));
            limb.Radii.Add(radius * (ScalarMath.Lerp(1f, tipRatio, t)));
        }
        return limb;
    }

    private static Vector3 Horizontal(Vector3 v)
    {
        var h = new Vector3(v.X, 0f, v.Z);
        return h.LengthSquared() > 1e-8f ? Vector3.Normalize(h) : Vector3.UnitX;
    }

    private static float BendTowards(float angle, float target, float amount)
    {
        var delta = target - angle;
        while (delta > MathF.PI) delta -= Tau;
        while (delta < -MathF.PI) delta += Tau;
        return angle + delta * Math.Clamp(amount, 0f, 0.9f);
    }

    private static float Length(Limb limb)
    {
        var sum = 0f;
        for (var i = 1; i < limb.Points.Count; i++) sum += Vector3.Distance(limb.Points[i - 1], limb.Points[i]);
        return sum;
    }

    /// <summary>Point at curve fraction f ∈ [0, 1] by point index.</summary>
    private static Vector3 Along(Limb limb, float f)
    {
        var x = Math.Clamp(f, 0f, 1f) * (limb.Points.Count - 1);
        var i = Math.Min((int)x, limb.Points.Count - 2);
        return Vector3.Lerp(limb.Points[i], limb.Points[i + 1], x - i);
    }

    private static float RadiusAt(Limb limb, float f)
    {
        var x = Math.Clamp(f, 0f, 1f) * (limb.Points.Count - 1);
        var i = Math.Min((int)x, limb.Points.Count - 2);
        return ScalarMath.Lerp(limb.Radii[i], limb.Radii[i + 1], x - i);
    }

    private static Vector3 PointAt(Limb trunk, float y) => Along(trunk, FractionAt(trunk, y));

    private static float RadiusAtHeight(Limb trunk, float y) => RadiusAt(trunk, FractionAt(trunk, y));

    private static float FractionAt(Limb trunk, float y)
    {
        for (var i = 1; i < trunk.Points.Count; i++)
            if (y <= trunk.Points[i].Y)
            {
                var a = trunk.Points[i - 1].Y;
                var b = trunk.Points[i].Y;
                var t = b > a ? (y - a) / (b - a) : 0f;
                return (i - 1 + t) / (trunk.Points.Count - 1);
            }
        return 1f;
    }
}
