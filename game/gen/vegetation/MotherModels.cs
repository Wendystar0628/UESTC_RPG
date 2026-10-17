using System.Globalization;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Vegetation;

/// <summary>Near-pool levels (ENG-PERF §9): L0+ 0–15 m, L0 15–40 m, L1 40–150 m on a 12 m tree.</summary>
public enum VegLevel : byte { L0Plus = 0, L0 = 1, L1 = 2 }

/// <summary>
/// One mother model at one level: the wood surface (trunk and branches, veg_trunk) and the crown surface (leaf cards
/// first, then the crown masses, all two-sided, veg_card; max-011 review). The crown is one material, so the outline
/// pass only draws its outer contour; every crown vertex carries the normal of one smooth shell (<see cref="CrownShell"/>),
/// so the crown shades as one lit side and one shadow band. Lower levels' cards are a prefix of higher levels' (CUSTOM2.w
/// is the importance rank). TrunkRadius at breast height and BranchHeight feed the trunk collider (WLD-VEGETATION §10).
/// </summary>
public sealed record MotherModel(
    string Species,
    int Variant,
    VegLevel Level,
    GeometryBundle Wood,
    GeometryBundle Crown,
    float Height,
    float TrunkRadius,
    float BranchHeight,
    float CrownRadius,
    Box3 Bounds)
{
    public int Triangles => (Wood.Index.Length + Crown.Index.Length) / 3;

    /// <summary>Leaf cards at the front of <see cref="Crown"/> (4 vertices and 2 triangles each); the masses follow.</summary>
    public int Cards { get; init; }

    /// <summary>The last of <see cref="Cards"/>: the crown-underside layer (<see cref="MotherModels.UnderClearance"/>).</summary>
    public int UnderCards { get; init; }

    /// <summary>Horizontal diameter of the crown shell, the yardstick of the 5% card rule.</summary>
    public float CrownDiameter { get; init; }

    /// <summary>Branch ends the winter twig strokes hang from (WLD-VEGETATION §1.8); also the song-post candidates (§12).</summary>
    public IReadOnlyList<Vector3> BranchTips { get; init; } = [];
}

/// <summary>
/// Mother-model generator (WLD-VEGETATION §1, §11.2; ENG-PERF §9). Pure and deterministic: the same species,
/// variant and level always give the same bytes. Vertex channels:
/// COLOR = (lit colour ID, AO, flags, spare) where spare is the crown-gradient t0 on crown vertices (§1.3) and the
/// whitewash colour ID on trunk vertices; CUSTOM0 = (card centre | mass centre, h2 | mass hash; −1 marks wood);
/// CUSTOM1 = (branch-tip anchor, h3 bloom hash); CUSTOM2 = (mass or ball centre, card importance rank in [0, 1) or −1 on
/// mass vertices, which never shrink with the level bands; underside cards carry 1 + rank when drawn through L0 and
/// 2 + rank when drawn at L0+ only). NORMAL on the crown is the shell normal; on cards it is the shell normal at the card
/// centre (palm fronds: the frond's own normal).
/// </summary>
public static class MotherModels
{
    /// <summary>Trunks start this far below the local ground (WLD-TERRAIN §4.4: HeightAt minus burial).</summary>
    public const float Burial = 0.3f;

    /// <summary>Bark: warm grey-brown, low chroma (max-011 review; colour IDs 252–253, ext/WP-23.json).</summary>
    public static readonly PaletteToken Bark = new("tree.bark.lit");
    public static readonly PaletteToken BarkShade = new("tree.bark.shade");

    /// <summary>Winter twig strokes keep the bare-branch name (WLD-VEGETATION §1.8); packed into the species table.</summary>
    public static readonly PaletteToken Twig = new("tree.bare.lit");

    /// <summary>Lime-washed lower trunk (WLD-VEGETATION §1.7), switched per instance by the variant bit in veg_trunk.</summary>
    public static readonly PaletteToken Whitewash = new("wall.greyWhite.lit");

    /// <summary>
    /// Card budget per crown type at L0+, L0 and L1 (ENG-PERF §9); lower levels take the top-ranked prefix. Every type keeps
    /// the same fractions (L0 ≈ 40%, L1 ≈ 5.6% of L0+), because veg_card shrinks the extra cards by those two fractions
    /// (VEG_KEEP_L0, VEG_KEEP_L1) before the level swaps. Card crowns take the low end of the 1500–2000 band (23d), so their
    /// near masses can be finer icospheres within the L0+ ceiling.
    /// </summary>
    public static int CardCount(CrownType crown, VegLevel level) => (crown, level) switch
    {
        (CrownType.Card, VegLevel.L0Plus) => 1500,
        (CrownType.Card, VegLevel.L0) => 600,
        (CrownType.Card, VegLevel.L1) => 84,
        (CrownType.Mixed, VegLevel.L0Plus) => 900,
        (CrownType.Mixed, VegLevel.L0) => 360,
        (CrownType.Mixed, VegLevel.L1) => 50,
        (CrownType.Cloud, VegLevel.L0Plus) => 380,
        (CrownType.Cloud, VegLevel.L0) => 150,
        (CrownType.Cloud, VegLevel.L1) => 21,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown vegetation level.")
    };

    /// <summary>Hard ceiling per mother model at L0+ (ENG-PERF §9: cloud crowns ≤ 10 000 triangles).</summary>
    public const int MaxTrianglesL0Plus = 10000;

    /// <summary>Cloud crowns at L0 (15–40 m): 2.5k (max-011 review); may rise to 3.5k only if the outline still shows facets.</summary>
    public const int CloudTrianglesL0 = 2500;

    /// <summary>
    /// The 3.5k allowance (max-011 review), taken because the 2.5k L0 outline still showed facets at 15–40 m: the most
    /// exposed balls (the ones that make the silhouette) go back to icosphere level 2 within it.
    /// </summary>
    public const int CloudTrianglesL0Outline = 3500;

    /// <summary>
    /// Card and mixed crowns at L0 (15–40 m; ENG-PERF §9: 3–4k): every mass is at least icosphere level 1 (23d: a bare
    /// icosahedron shows straight edges and corners at 15 m), and the most exposed step up to level 2 within this.
    /// </summary>
    public const int CardTrianglesL0 = 4000;

    /// <summary>
    /// A mass face is left out when all its corners lie this far inside one other mass (a fraction of that mass's radius,
    /// on top of the faceting of a level-1 icosphere), so wind sway and the per-mass leaf-amount lead never open it (23d).
    /// </summary>
    public const float HiddenMargin = 0.06f;

    /// <summary>L1 target (ENG-PERF §9: 0.6–0.9k); L1 merges its masses down to this many.</summary>
    public const int MaxMassesL1 = 6;

    /// <summary>Cards stay within this fraction of the crown diameter outside the crown masses (max-011 review: 5%).</summary>
    public const float CardReach = 0.05f;

    /// <summary>Card half-diagonal cap as a fraction of the crown diameter, so small crowns keep their cards inside the 5% band.</summary>
    public const float CardSizeOfDiameter = 0.035f;

    /// <summary>Cards per cluster (max-011 review ④: 3–5, no lone card).</summary>
    public const int ClusterMin = 3;
    public const int ClusterMax = 5;

    /// <summary>
    /// How deep a cluster's centre sits inside the outer crown mass, as a fraction of the card radius (half diagonal): at
    /// least 30% (max-011 review ④), so each cluster grows out of the outline instead of hovering on it.
    /// </summary>
    public const float ClusterPressMin = 0.3f;
    public const float ClusterPressMax = 0.55f;

    /// <summary>Spread of the cards around their cluster centre, in card edges (tangential), so they overlap into one clump.</summary>
    public const float ClusterSpread = 0.45f;

    /// <summary>
    /// Crown-underside layer (max-011 23c3): crowns one can stand under — crown envelope bottom at least this high — get
    /// card clusters on the underside of their lowest masses, so looking up from under the tree (the ginkgo avenue) the
    /// crown bottom reads as soft leaf clumps, not as the flat undersides and faceted outline of the masses. The clusters
    /// are pressed in like the outline clusters; veg_card keeps them at full size when seen from below and draws them a
    /// little towards the camera along the view ray, so they cover the underside they sit in without moving on screen.
    /// </summary>
    public const float UnderClearance = 2.4f;

    /// <summary>Share of the crown footprint the underside clusters cover at L0+ (counted as cluster area, overlaps aside).</summary>
    public const float UnderCover = 0.75f;

    /// <summary>Most underside clusters per mother model (the L0+ triangle ceiling still rules).</summary>
    public const int UnderClustersMax = 56;

    /// <summary>Underside = where the crown shell normal points down more than this (the sides belong to the outline cards).</summary>
    public const float UnderFacing = -0.3f;

    /// <summary>
    /// L0 keeps this leading share of the underside clusters (veg_card VEG_UNDER_KEEP_L0); L1 draws none. Cloud crowns keep
    /// their L0 at the 3.5k outline allowance, so their underside is drawn at L0+ only.
    /// </summary>
    public const float UnderKeepL0 = 0.5f;

    /// <summary>
    /// Underside cards are this much larger than outline cards, and veg_card draws their leaves this much smaller
    /// (VEG_UNDER_FIT): seen face-on from below, the leaves then stay inside the card instead of being cut by its edges.
    /// </summary>
    public const float UnderCardScale = 1.3f;

    /// <summary>Depth of a level-1 icosphere's faces below its smooth lumpy surface, as a fraction of the mass radius.</summary>
    public const float FacetSag = 0.05f;

    /// <summary>Crown-mass AO by depth below the shell: 1 on the shell, 1 − this deep in the crown (pushes the terminator only).</summary>
    public const float MassAoDepth = 0.15f;

    /// <summary>The shadow proxy's masses sit this much inside the visible crown, so they never cast onto its lit side.</summary>
    public const float ProxyScale = 0.92f;

    public static ToonSpec WoodSpec() => new(Bark, BarkShade)
    {
        Kind = ToonKind.VegTrunk,
        Steps = ShadeSteps.Three,
        Band = 0.06f,
        Normal = ToonNormal.Smooth,
        Line = LineClass.Tree,
        Translucency = 0f,
        Wet = 0f
    };

    public static ToonSpec CrownSpec(SpeciesInfo species)
    {
        ArgumentNullException.ThrowIfNull(species);
        return new(species.LeafLit, species.LeafShade)
        {
            Kind = ToonKind.VegCard,
            Steps = ShadeSteps.Three,
            Band = 0.06f,
            Normal = ToonNormal.Smooth,
            Line = LineClass.Tree,
            Translucency = 0.8f,
            Wet = 0f
        };
    }

    public static MotherModel Build(SpeciesInfo species, int variant, VegLevel level, PaletteCatalog palette)
    {
        ArgumentNullException.ThrowIfNull(species);
        ArgumentNullException.ThrowIfNull(palette);
        if (variant is < 0 or >= TreeForms.Variants)
            throw new ArgumentOutOfRangeException(nameof(variant), variant, "Mother-model variants are 0–3.");
        var plan = new TreePlan(species, variant);
        var shell = new CrownShell(plan);
        var ids = IdsOf(species, palette);
        var wood = new MeshWriter(withUv: false);
        var crown = new MeshWriter(withUv: true);

        WriteWood(wood, plan, level, ids);
        var palm = species.Form == "palm";
        var cardTarget = palm ? (level switch { VegLevel.L0Plus => 20, VegLevel.L0 => 16, _ => 10 }) : CardCount(species.Crown, level);
        var allCards = palm ? PalmFronds(plan, 20) : PlaceCards(plan, shell, CardCount(species.Crown, VegLevel.L0Plus));
        // Lower levels keep whole clusters: the prefix ends on the cluster that crosses the card target.
        var take = Math.Min(cardTarget, allCards.Count);
        while (take < allCards.Count && take > 0 && allCards[take].Cluster == allCards[take - 1].Cluster) take++;
        var drawn = allCards.Take(take).ToList();
        foreach (var card in drawn) WriteCard(crown, plan, shell, card, ids, Importance(plan, card));
        // The underside layer follows the outline cards: all of it at L0+, a leading share of whole clusters at L0 (card and
        // mixed crowns), none at L1. Its rank is its own (tier 1 or 2 plus the cluster's place in the layer).
        var under = palm ? new List<Card>() : PlaceUnderCards(plan, shell);
        var underTier = species.Crown == CrownType.Cloud ? 2f : 1f;
        var underTake = (level, species.Crown) switch
        {
            (VegLevel.L0Plus, _) => under.Count,
            (VegLevel.L0, not CrownType.Cloud) => (int)MathF.Ceiling(under.Count * UnderKeepL0),
            _ => 0
        };
        while (underTake < under.Count && underTake > 0 && under[underTake].Cluster == under[underTake - 1].Cluster) underTake++;
        foreach (var card in under.Take(underTake)) WriteCard(crown, plan, shell, card, ids, underTier + card.Cluster / (float)under.Count);
        drawn.AddRange(under.Take(underTake));
        var cards = crown.Cards;
        // Masses that carry a drawn card stay, so no card is left floating where its mass was dropped.
        var hosts = drawn.Select(c => c.Owner).ToHashSet();

        var blobs = BlobsFor(plan, level);
        // Near levels draw only the faces of each mass that no other mass swallows (23d), so the triangles go to the
        // outline: every mass can be a finer icosphere within the same budget.
        var hidden = level == VegLevel.L1 ? null : new HiddenFaces(plan, blobs);
        int Total() => wood.Triangles + crown.Triangles + (hidden?.Cost(blobs) ?? CostOf(blobs));
        // Fits the masses under a triangle limit: the least exposed step down one icosphere level (not below minDetail; cores
        // last), and only when none can step down does the least exposed free non-core mass go (which may unhide faces of others).
        void Fit(int limit, int minDetail)
        {
            List<int> Order() => ByExposure(plan, blobs).Concat(Enumerable.Range(0, blobs.Count).Where(i => blobs[i].Blob.Core)).ToList();
            var order = Order();
            while (Total() > limit)
            {
                var step = order.FindIndex(i => blobs[i].Detail > minDetail);
                if (step >= 0)
                {
                    var (blob, index, scale, detail) = blobs[order[step]];
                    blobs[order[step]] = (blob, index, scale, detail - 1);
                    continue;
                }
                var free = order.FindIndex(i => !blobs[i].Blob.Core && !hosts.Contains(blobs[i].Index));
                if (free < 0) break;
                blobs.RemoveAt(order[free]);
                hidden?.Reset(blobs);
                order = Order();
            }
        }
        if (level == VegLevel.L0Plus)
        {
            // Keep the hard ceiling: the least exposed masses step down to icosphere level 1 (they barely show), and only
            // if that is not enough do free non-core masses go, least exposed first.
            Fit(MaxTrianglesL0Plus - 100, minDetail: 1);
        }
        else if (level == VegLevel.L0)
        {
            var cloud = species.Crown == CrownType.Cloud;
            if (cloud)
            {
                Prune(plan, blobs, hosts, CloudTrianglesL0 - wood.Triangles - crown.Triangles, minDetail: 0, () => hidden?.Cost(blobs) ?? CostOf(blobs));
                hidden?.Reset(blobs);
            }
            // Cores first (the big masses), then the others from the most exposed down, step up to icosphere level 2 while
            // the outline allowance lasts (cloud crowns 3.5k; card and mixed crowns the top of the 3–4k L0 band), after the
            // least exposed card and mixed masses stepped down to the icosahedron if level 1 alone does not fit.
            var limit = cloud ? CloudTrianglesL0Outline : CardTrianglesL0;
            if (!cloud) Fit(limit, minDetail: 0);
            var order = ByExposure(plan, blobs);
            order.Reverse();
            order.InsertRange(0, Enumerable.Range(0, blobs.Count).Where(i => blobs[i].Blob.Core));
            foreach (var i in order)
            {
                var (blob, index, scale, detail) = blobs[i];
                if (detail >= 2) continue;
                blobs[i] = (blob, index, scale, 2);
                if (Total() > limit)
                {
                    blobs[i] = (blob, index, scale, detail);
                    break;
                }
            }
        }
        foreach (var (blob, index, scale, detail) in blobs)
            WriteBlob(crown, plan, shell, blob, index, scale, detail, ids, faces: hidden?.Kept(index, detail));

        var woodBundle = wood.Bundle(WoodSpec(), flags: 0);
        var crownBundle = crown.Bundle(CrownSpec(species), flags: 8);
        var bounds = Union(wood.Bounds(), crown.Bounds());
        return new MotherModel(species.Id, variant, level, woodBundle, crownBundle, plan.Height,
            plan.TrunkRadius, plan.BranchY, plan.CrownRx, bounds)
        { Cards = cards, UnderCards = underTake, CrownDiameter = shell.Diameter, BranchTips = plan.BranchTips().ToArray() };
    }

    /// <summary>
    /// The ShadowsOnly proxy of a mother model (WLD-VEGETATION §11.3, ENG-PERF §9): L1 wood plus the L1 masses scaled by
    /// <see cref="ProxyScale"/>, drawn with veg_trunk and its shadow_proxy holes. One per mother model, shared by all levels.
    /// </summary>
    public static GeometryBundle ShadowProxy(SpeciesInfo species, int variant, PaletteCatalog palette)
    {
        ArgumentNullException.ThrowIfNull(species);
        ArgumentNullException.ThrowIfNull(palette);
        var plan = new TreePlan(species, variant);
        var shell = new CrownShell(plan);
        var ids = IdsOf(species, palette);
        var w = new MeshWriter(withUv: false);
        WriteWood(w, plan, VegLevel.L1, ids);
        foreach (var (blob, index, _, detail) in BlobsFor(plan, VegLevel.L1))
            WriteBlob(w, plan, shell, blob, index, ProxyScale, detail, ids);
        return w.Bundle(WoodSpec(), flags: 0);
    }

    /// <summary>Sides of the far stand-in's trunk (ENG-PERF §9: L2 is 150–300 triangles or fewer).</summary>
    public const int FarTrunkSides = 5;

    /// <summary>
    /// Vertex tag of the far stand-in in CUSTOM2.w (veg_card, veg_trunk: veg_pool_code): its wood carries 1 (near wood 0),
    /// its masses −2 (near masses −1). The shaders cross-fade the far stand-in against the near pool's L1 by it.
    /// </summary>
    public const float FarWoodTag = 1f, FarMassTag = -2f;

    /// <summary>
    /// The far-pool stand-in of a mother model (WLD-VEGETATION §11.4–§11.5 candidate A, mainline cut; max-011 23c2c): a
    /// five-sided trunk from the buried foot to the crown base, and the L1 merged masses as level-0 lumpy icospheres sampled
    /// on the same surface as the L1 masses (so the two meet in the cross-fade band with the same bulk, normals, colour and
    /// crown gradient), no cards and no branches; about 130 triangles. Stemless balls (shrubs) have no wood. Wood is drawn
    /// with veg_trunk; the crown with veg_card in the same vertex layout as the near crown. Both carry the far tags
    /// (<see cref="FarWoodTag"/>, <see cref="FarMassTag"/>). The crown casts no shadow (the far pool has none).
    /// </summary>
    public static (GeometryBundle Wood, GeometryBundle Crown) FarStandIn(SpeciesInfo species, int variant, PaletteCatalog palette)
    {
        ArgumentNullException.ThrowIfNull(species);
        ArgumentNullException.ThrowIfNull(palette);
        if (variant is < 0 or >= TreeForms.Variants)
            throw new ArgumentOutOfRangeException(nameof(variant), variant, "Mother-model variants are 0–3.");
        var plan = new TreePlan(species, variant);
        var shell = new CrownShell(plan);
        var ids = IdsOf(species, palette);
        var wood = new MeshWriter(withUv: false);
        var crown = new MeshWriter(withUv: true);
        var limb = TreeForms.IsBall(plan.Form.Id) ? null : plan.Limbs.FirstOrDefault(l => l.Level == 0);
        if (limb is { Points.Count: >= 2 })
        {
            var top = limb.Points.FindIndex(p => p.Y >= plan.BranchY);
            if (top <= 0) top = limb.Points.Count - 1;
            WriteTube(wood, [limb.Points[0], limb.Points[top]], [limb.Radii[0], limb.Radii[top]], FarTrunkSides, 0f, 0f,
                cap: false, trunk: true, ids, FarWoodTag);
        }
        // A palm's crown is its fronds (cards): the stand-in swells the head mass to about the frond reach instead.
        var palm = plan.Form.Id == "palm";
        foreach (var (blob, index, scale, _) in BlobsFor(plan, VegLevel.L1))
        {
            var s = palm ? MathF.Max(scale, (blob.Radius * 0.6f + plan.Form.CardSize) * 0.8f / MathF.Max(blob.Radius, 0.1f)) : scale;
            WriteBlob(crown, plan, shell, blob, index, s, detail: 0, ids, FarMassTag);
        }
        return (wood.Bundle(WoodSpec(), flags: 0), crown.Bundle(CrownSpec(species), flags: 8));
    }

    private static Ids IdsOf(SpeciesInfo species, PaletteCatalog palette) =>
        new(palette.Resolve(species.LeafLit).Value, palette.Resolve(Bark).Value, palette.Resolve(Whitewash).Value);

    private readonly record struct Ids(ushort Leaf, ushort Bark, ushort Whitewash);

    /// <summary>
    /// One leaf card; Owner is the plan mass it sits on (that mass is never pruned while the card is drawn). Cards of one
    /// cluster share Cluster, the first card's index (their importance rank), H2 (they shed and fold away together) and
    /// the winter anchor; H3 stays per card (bloom and the card's own leaf shape).
    /// </summary>
    private sealed record Card(Vector3 Center, Vector3 Normal, Vector3 U, Vector3 V, float Width, float Height,
        float H2, float H3, Vector3 Anchor, Vector3 BlobCenter, int Owner, int Cluster);

    /// <summary>
    /// Icosphere level of one mass: 2 at L0+ and 1 at L0 (ENG-PERF §9; 23d: card and mixed masses as well, with their
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
                sum += k * k * (3f - 2f * k) * b.W;
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
    // Wood: tapered tubes with a lobed root flare; trunks 32 / 16 / 8 sides (ENG-PERF §9), branch tips capped.
    // ---------------------------------------------------------------------------------------------------------

    private static void WriteWood(MeshWriter w, TreePlan plan, VegLevel level, Ids ids)
    {
        var flarePhase = plan.Next() * 6.2831853f;
        foreach (var limb in plan.Limbs)
        {
            var sides = (limb.Level, level) switch
            {
                (0, VegLevel.L0Plus) => 32,
                (0, VegLevel.L0) => 16,
                (0, _) => 6,
                (1, VegLevel.L0Plus) => 12,
                (1, VegLevel.L0) => 6,
                (1, _) => 4,
                (_, VegLevel.L1) => 0,
                (_, VegLevel.L0) => 4,
                _ => 6
            };
            if (sides == 0) continue;
            var points = new List<Vector3>(limb.Points);
            var radii = new List<float>(limb.Radii);
            if (points.Count > 4)
            {
                // L0 and L1 keep every other ring: limbs are gentle curves, and the silhouette is what reads past 15 m.
                // L0+ thins only the trunk above the crown base, where the crown hides it (a 19 m leader is 20 rings).
                var above = level == VegLevel.L0Plus ? (limb.Level == 0 ? plan.BranchY + 1f : float.MaxValue) : float.MinValue;
                for (var i = points.Count - 2; i > 0; i -= 2)
                    if (points[i].Y > above)
                    {
                        points.RemoveAt(i);
                        radii.RemoveAt(i);
                    }
            }
            if (limb.Level == 0 && points.Count > 2 && level != VegLevel.L1)
            {
                // Extra rings above the ground carry the root flare.
                foreach (var f in new[] { 0.45f, 0.2f })
                {
                    points.Insert(2, Vector3.Lerp(points[1], points[2], f));
                    radii.Insert(2, radii[1] + (radii[2] - radii[1]) * f);
                }
            }
            var flare = limb.Level == 0 && !TreeForms.IsBall(plan.Form.Id) ? level switch { VegLevel.L0Plus => 0.55f, VegLevel.L0 => 0.4f, _ => 0.2f } : 0f;
            var cap = limb.Level > 0 || plan.Form.Leader;
            WriteTube(w, points, radii, sides, flare, flarePhase, cap, limb.Level == 0, ids);
        }
    }

    private static void WriteTube(MeshWriter w, List<Vector3> points, List<float> radii, int sides, float flare,
        float flarePhase, bool cap, bool trunk, Ids ids, float tag = 0f)
    {
        var n = points.Count;
        var tangent = Vector3.Normalize(points[1] - points[0]);
        var normal = Vector3.Normalize(Vector3.Cross(tangent, MathF.Abs(tangent.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitX));
        var first = w.VertexCount;
        for (var i = 0; i < n; i++)
        {
            var t = i == 0 ? points[1] - points[0] : i == n - 1 ? points[i] - points[i - 1] : points[i + 1] - points[i - 1];
            t = Vector3.Normalize(t);
            // Parallel transport keeps the ring seams from twisting.
            normal = Vector3.Normalize(normal - t * Vector3.Dot(normal, t));
            var binormal = Vector3.Cross(t, normal);
            var y = points[i].Y;
            for (var s = 0; s < sides; s++)
            {
                var a = s * 6.2831853f / sides;
                var c = DetMath.Cos(a);
                var sn = DetMath.Sin(a);
                var radial = normal * c + binormal * sn;
                var r = radii[i];
                if (flare > 0f)
                {
                    var k = 1f - Math.Clamp(y / 0.9f, 0f, 1f);
                    r *= 1f + flare * k * k * (1f + 0.35f * DetMath.Cos(5f * a + flarePhase));
                }
                w.Add(points[i] + radial * r, radial, ao: 1f, ids.Bark, trunk ? checked((byte)ids.Whitewash) : (byte)0,
                    new Vector4(0f, 0f, 0f, -1f), Vector4.Zero, new Vector4(0f, 0f, 0f, tag), Vector2.Zero);
            }
        }
        for (var i = 0; i < n - 1; i++)
            for (var s = 0; s < sides; s++)
            {
                var a = first + i * sides + s;
                var b = first + i * sides + (s + 1) % sides;
                var c = a + sides;
                var d = b + sides;
                w.Tri(a, c, b);
                w.Tri(b, c, d);
            }
        if (!cap) return;
        var lastT = Vector3.Normalize(points[n - 1] - points[n - 2]);
        var tip = w.Add(points[n - 1] + lastT * (radii[n - 1] * 1.6f), lastT, 1f, ids.Bark, 0,
            new Vector4(0f, 0f, 0f, -1f), Vector4.Zero, Vector4.Zero, Vector2.Zero);
        var ring = first + (n - 1) * sides;
        for (var s = 0; s < sides; s++) w.Tri(ring + s, tip, ring + (s + 1) % sides);
    }

    // ---------------------------------------------------------------------------------------------------------
    // Crown masses: lumpy icospheres that only shape the silhouette. Their normals come from the crown shell, so
    // neighbouring masses never start their own light and shade (max-011 review).
    // ---------------------------------------------------------------------------------------------------------

    private static void WriteBlob(MeshWriter w, TreePlan plan, CrownShell shell, Blob blob, int index, float scale, int detail, Ids ids,
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
            var ao = 1f - MassAoDepth * depth * depth * (3f - 2f * depth);
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
            bumps[i] = new Vector4(s * DetMath.Cos(phi), z, s * DetMath.Sin(phi), 0.6f + 0.8f * rng.NextFloat());
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
            sum += k * k * (3f - 2f * k) * b.W;
        }
        var r = radius * (1f + lump * (Math.Min(sum, 1.4f) - 0.35f));
        var p = d * r;
        if (p.Y < 0f) p.Y *= 0.7f;
        return center + p;
    }

    // ---------------------------------------------------------------------------------------------------------
    // Cards (WLD-VEGETATION §11.2, max-011 reviews): cards come in clusters of 3–5. A cluster's centre sits on the
    // outermost crown mass along a ray aimed at the crown shell, pressed into it by at least 30% of the card radius,
    // and its cards overlap around that centre facing the shell normal, so each clump grows out of the outline and none
    // hovers alone. veg_card keeps them at full size only on the silhouette ring. Cluster directions follow a Halton
    // sequence, so every prefix of whole clusters (the lower levels) still covers the whole crown evenly.
    // ---------------------------------------------------------------------------------------------------------

    private static List<Card> PlaceCards(TreePlan plan, CrownShell shell, int total)
    {
        var cards = new List<Card>(total + ClusterMax);
        var bumps = plan.Blobs.Select((b, i) => Bumps(plan, b, i)).ToArray();
        for (var i = 1; cards.Count < total && i <= total * 4; i++)
        {
            var z = 1f - 2f * Halton(i, 2);
            var phi = 6.2831853f * Halton(i, 3);
            var s = MathF.Sqrt(MathF.Max(0f, 1f - z * z));
            var d = new Vector3(s * DetMath.Cos(phi), z, s * DetMath.Sin(phi));
            // The underside carries half the density: it is seen less and holds the hanging fringe.
            if (d.Y < -0.5f && (i & 1) == 0) continue;
            var target = shell.EllipsoidPoint(d);
            var origin = shell.Center + (target - shell.Center) * 2f;
            if (!OuterHit(plan, bumps, origin, shell.Center - origin, out var hit, out var owner)) continue;
            AddCluster(cards, plan, shell, hit, owner);
        }
        return cards;
    }

    /// <summary>
    /// The crown-underside layer (<see cref="UnderClearance"/>): rays straight up from below the crown over a Halton
    /// spread of its footprint meet the lowest mass; where the shell faces down there, a cluster is pressed in as on the
    /// outline. Cluster indices count within the layer, so every prefix of whole clusters covers the underside evenly.
    /// </summary>
    private static List<Card> PlaceUnderCards(TreePlan plan, CrownShell shell)
    {
        var cards = new List<Card>();
        if (plan.CrownCenter.Y - plan.CrownRy < UnderClearance || plan.Blobs.Count == 0) return cards;
        var bumps = plan.Blobs.Select((b, i) => Bumps(plan, b, i)).ToArray();
        var (aspect, spread) = CardAspect(plan);
        var low = plan.Blobs.Min(b => b.Center.Y - b.Radius) - 1f;
        var reach = plan.Blobs.Max(b => Vector2.Distance(new(b.Center.X, b.Center.Z), new(shell.Center.X, shell.Center.Z)) + b.Radius);
        var size = MathF.Min(plan.Form.CardSize, CardSizeOfDiameter * shell.Diameter / spread);
        var clusterArea = size * size * aspect * (ClusterMin + ClusterMax) * 0.5f * 0.5f;
        var target = Math.Min(UnderClustersMax, (int)MathF.Round(UnderCover * MathF.PI * reach * reach / clusterArea));
        var clusters = 0;
        for (var i = 1; clusters < target && i <= target * 6; i++)
        {
            var r = reach * MathF.Sqrt(Halton(i, 2));
            var phi = 6.2831853f * Halton(i, 3);
            var origin = new Vector3(shell.Center.X + r * DetMath.Cos(phi), low, shell.Center.Z + r * DetMath.Sin(phi));
            if (!OuterHit(plan, bumps, origin, Vector3.UnitY, out var hit, out var owner)) continue;
            if (shell.Normal(hit).Y > UnderFacing) continue;
            var start = cards.Count;
            AddCluster(cards, plan, shell, hit, owner, UnderCardScale);
            // Renumber the cluster within the layer (the rank veg_card folds the layer by).
            for (var k = start; k < cards.Count; k++) cards[k] = cards[k] with { Cluster = start };
            clusters++;
        }
        return cards;
    }

    private static (float Aspect, float Spread) CardAspect(TreePlan plan)
    {
        var aspect = plan.Species.Card == CardShape.Strand ? 1.8f : 1f;
        return (aspect, 0.5f * MathF.Sqrt(1f + aspect * aspect));
    }

    /// <summary>
    /// One cluster of 3–5 cards at a mass hit. The cluster centre is pressed into the owning mass along its radial
    /// direction at the hit (so the clump stays in that mass even where the shell normal grazes it), measured from the
    /// drawn faceted mass, which lies up to FacetSag of the mass radius inside the smooth lump. A card corner then reaches
    /// at most (1 − press) card radii out, well inside the 5% band; the cards themselves face the shell.
    /// </summary>
    private static void AddCluster(List<Card> cards, TreePlan plan, CrownShell shell, Vector3 hit, int owner, float scale = 1f)
    {
        var (aspect, spread) = CardAspect(plan);
        var bottom = plan.CrownCenter.Y - plan.CrownRy;
        var size = scale * MathF.Min(plan.Form.CardSize * plan.Range(0.85f, 1.2f), CardSizeOfDiameter * shell.Diameter / spread);
        var radius = spread * size;
        var blob = plan.Blobs[owner];
        var m = Vector3.Normalize(hit - blob.Center + new Vector3(0f, 1e-6f, 0f));
        var clusterCenter = hit - m * (radius * plan.Range(ClusterPressMin, ClusterPressMax) + blob.Radius * FacetSag);
        var anchor = blob.Anchor + plan.RandomDirection() * plan.Range(0f, 0.3f);
        var h2 = plan.Next();
        var cluster = cards.Count;
        var members = plan.Count(ClusterMin, ClusterMax);
        var tangent = Vector3.Normalize(Vector3.Cross(MathF.Abs(m.Y) < 0.95f ? Vector3.UnitY : Vector3.UnitX, m));
        var bitangent = Vector3.Cross(m, tangent);
        var turn = plan.Next() * 6.2831853f;
        for (var k = 0; k < members; k++)
        {
            // Members ring the centre (the first one on it), never further out than the centre itself.
            var a = turn + k * 6.2831853f / members;
            var r = k == 0 ? 0f : size * ClusterSpread * plan.Range(0.6f, 1f);
            var center = clusterCenter + (tangent * DetMath.Cos(a) + bitangent * DetMath.Sin(a)) * r
                - m * (radius * plan.Range(0f, 0.15f) + r * r / MathF.Max(0.2f, 2f * blob.Radius));
            var cn = shell.Normal(center);
            var droop = plan.Form.Droop;
            if (plan.Species.Crown == CrownType.Cloud && center.Y < bottom + plan.CrownRy * 0.6f) droop = MathF.Max(droop, 0.7f);
            var (u, v) = Frame(cn, plan.Next() * 6.2831853f, droop);
            var card = size * plan.Range(0.88f, 1.08f);
            cards.Add(new Card(center, cn, u, v, card, card * aspect, h2, plan.Next(), anchor, blob.Center, owner, cluster));
        }
    }

    /// <summary>First crown mass met by a ray (the outermost one seen from outside) and the lumpy surface point there.</summary>
    private static bool OuterHit(TreePlan plan, Vector4[][] bumps, Vector3 origin, Vector3 direction, out Vector3 point, out int owner)
    {
        var dir = Vector3.Normalize(direction);
        var best = float.MaxValue;
        owner = -1;
        for (var i = 0; i < plan.Blobs.Count; i++)
        {
            var b = plan.Blobs[i];
            var r = b.Radius * (1f + b.Lump * 0.3f);
            var oc = origin - b.Center;
            var half = Vector3.Dot(oc, dir);
            var disc = half * half - (oc.LengthSquared() - r * r);
            if (disc < 0f) continue;
            var t = -half - MathF.Sqrt(disc);
            if (t > 0f && t < best) (best, owner) = (t, i);
        }
        point = default;
        if (owner < 0) return false;
        var o = plan.Blobs[owner];
        var hit = origin + dir * best;
        var u = Vector3.Normalize(hit - o.Center + new Vector3(0f, 1e-6f, 0f));
        point = Surface(o.Center, o.Radius, o.Lump, bumps[owner], u);
        return true;
    }

    private static float Halton(int index, int radix)
    {
        var f = 1f;
        var r = 0f;
        for (var i = index; i > 0; i /= radix)
        {
            f /= radix;
            r += f * (i % radix);
        }
        return r;
    }

    /// <summary>Fan-palm fronds radiating from the crown head, upper ones rising, lower ones hanging.</summary>
    private static List<Card> PalmFronds(TreePlan plan, int count)
    {
        var head = plan.Blobs[0];
        var cards = new List<Card>(count);
        for (var i = 0; i < count; i++)
        {
            var f = (i + 0.5f) / count;
            var azimuth = i * 2.3999632f + plan.Range(-0.2f, 0.2f);
            var pitch = (0.75f - 1.35f * f) + plan.Range(-0.12f, 0.12f);
            var along = Vector3.Normalize(TreePlan.Azimuth(azimuth) * DetMath.Cos(pitch) + Vector3.UnitY * DetMath.Sin(pitch));
            var size = plan.Form.CardSize * plan.Range(0.85f, 1.1f);
            var side = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, along) + new Vector3(0f, 0f, 1e-4f));
            var normal = Vector3.Normalize(Vector3.Cross(side, along));
            var center = head.Center + along * (size * 0.5f + head.Radius * 0.6f);
            // Fronds keep their own plane normal: veg_card turns each frond about it.
            cards.Add(new Card(center, normal, side, along, size, size, plan.Next(), plan.Next(), head.Center, head.Center, 0, i));
        }
        return cards;
    }

    /// <summary>
    /// In-plane axes of a card facing d, rotated by psi; droop turns the base→tip axis V towards hanging.
    /// The base basis (T0 = Y × d, B0 = d × T0) matches veg_card, which re-aims the card at the camera.
    /// </summary>
    private static (Vector3 U, Vector3 V) Frame(Vector3 d, float psi, float droop)
    {
        var t0 = Vector3.Cross(Vector3.UnitY, d);
        t0 = t0.LengthSquared() > 1e-6f ? Vector3.Normalize(t0) : Vector3.UnitX;
        var b0 = Vector3.Cross(d, t0);
        var c = DetMath.Cos(psi);
        var s = DetMath.Sin(psi);
        var u = t0 * c + b0 * s;
        var v = Vector3.Cross(d, u);
        if (droop > 0f)
        {
            var down = -Vector3.UnitY - d * Vector3.Dot(-Vector3.UnitY, d);
            if (down.LengthSquared() > 1e-4f)
            {
                v = Vector3.Normalize(Vector3.Lerp(v, Vector3.Normalize(down), droop));
                u = Vector3.Normalize(Vector3.Cross(v, d));
            }
        }
        return (u, v);
    }

    /// <summary>
    /// Rank of an outline card in the level bands. Palm fronds are few and all needed at every level, so they never shrink.
    /// A cluster shares the rank of its first card, so the level bands fold whole clusters away.
    /// </summary>
    private static float Importance(TreePlan plan, Card card) =>
        plan.Form.Id == "palm" ? 0f : card.Cluster / (float)Math.Max(1, CardCapacity(plan));

    private static void WriteCard(MeshWriter w, TreePlan plan, CrownShell shell, Card card, Ids ids, float importance)
    {
        var first = w.VertexCount;
        var t0 = Gradient(plan, card.Center, plan.Form.Id == "palm" ? card.Normal : shell.Normal(card.Center));
        ReadOnlySpan<Vector2> corners = [new(0f, 0f), new(1f, 0f), new(1f, 1f), new(0f, 1f)];
        foreach (var uv in corners)
        {
            var p = card.Center + card.U * ((uv.X - 0.5f) * card.Width) + card.V * ((uv.Y - 0.5f) * card.Height);
            w.Add(p, card.Normal, 1f, ids.Leaf, t0, new Vector4(card.Center, card.H2), new Vector4(card.Anchor, card.H3),
                new Vector4(card.BlobCenter, importance), uv);
        }
        w.Tri(first, first + 2, first + 1);
        w.Tri(first, first + 3, first + 2);
        w.Cards++;
    }

    private static int CardCapacity(TreePlan plan) => plan.Form.Id == "palm" ? 20 : CardCount(plan.Species.Crown, VegLevel.L0Plus);

    /// <summary>Crown-gradient t0 = 0.36 + 0.26·n.y + 0.30·(height in crown − 0.5) + 0.12·outward (WLD-VEGETATION §1.3).</summary>
    private static byte Gradient(TreePlan plan, Vector3 p, Vector3 n)
    {
        var h = Math.Clamp((p.Y - (plan.CrownCenter.Y - plan.CrownRy)) / (2f * plan.CrownRy), 0f, 1f);
        var outward = Math.Clamp(Vector2.Distance(new(p.X, p.Z), new(plan.CrownCenter.X, plan.CrownCenter.Z)) / MathF.Max(0.1f, plan.CrownRx), 0f, 1f);
        var t0 = Math.Clamp(0.36f + 0.26f * n.Y + 0.30f * (h - 0.5f) + 0.12f * outward, 0f, 1f);
        return (byte)MathF.Round(t0 * 255f);
    }

    private static Box3 Union(Box3 a, Box3 b) => new(Vector3.Min(a.Min, b.Min), Vector3.Max(a.Max, b.Max));

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
