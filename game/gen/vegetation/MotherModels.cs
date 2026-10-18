using Campus.Contracts;

namespace Campus.Gen.Vegetation;

/// <summary>
/// Mother-model generator (WLD-VEGETATION; ENG-PERF). Pure and deterministic: the same species,
/// variant and level always give the same bytes. Vertex channels:
/// COLOR = (lit colour ID, AO, flags, spare) where spare is the crown-gradient t0 on crown vertices (§1.3), 0 on wood; CUSTOM0 = (card centre | mass centre, h2 | mass hash; −1 marks wood);
/// CUSTOM1 = (branch-tip anchor, h3 bloom hash); CUSTOM2 = (mass or ball centre, card importance rank in [0, 1) or −1 on
/// mass vertices, which never shrink with the level bands; underside cards carry 1 + rank when drawn through L0 and
/// 2 + rank when drawn at L0+ only). NORMAL on the crown is the shell normal; on cards it is the shell normal at the card
/// centre (palm fronds: the frond's own normal).
/// </summary>
public static partial class MotherModels
{
    /// <summary>Trunks start this far below the local ground (WLD-TERRAIN: HeightAt minus burial).</summary>
    public const float Burial = 0.3f;

    /// <summary>Bark: warm grey-brown, low chroma (max-011 review; colour IDs 252–253, ext/WP-23.json).</summary>
    public static readonly PaletteToken Bark = new("tree.bark.lit");
    public static readonly PaletteToken BarkShade = new("tree.bark.shade");

    /// <summary>Winter twig strokes keep the bare-branch name (WLD-VEGETATION); packed into the species table.</summary>
    public static readonly PaletteToken Twig = new("tree.bare.lit");

    /// <summary>Lime-washed lower trunk (WLD-VEGETATION), switched per instance by the variant bit in veg_trunk.</summary>
    public static readonly PaletteToken Whitewash = new("wall.greyWhite.lit");

    /// <summary>
    /// Card budget per crown type at L0+, L0 and L1 (ENG-PERF); lower levels take the top-ranked prefix. Every type keeps
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

    /// <summary>Hard ceiling per mother model at L0+ (ENG-PERF: cloud crowns ≤ 10 000 triangles).</summary>
    public const int MaxTrianglesL0Plus = 10000;

    /// <summary>Cloud crowns at L0 (15–40 m): 2.5k (max-011 review); may rise to 3.5k only if the outline still shows facets.</summary>
    public const int CloudTrianglesL0 = 2500;

    /// <summary>
    /// The 3.5k allowance (max-011 review), taken because the 2.5k L0 outline still showed facets at 15–40 m: the most
    /// exposed balls (the ones that make the silhouette) go back to icosphere level 2 within it.
    /// </summary>
    public const int CloudTrianglesL0Outline = 3500;

    /// <summary>
    /// Card and mixed crowns at L0 (15–40 m; ENG-PERF: 3–4k): every mass is at least icosphere level 1 (23d: a bare
    /// icosahedron shows straight edges and corners at 15 m), and the most exposed step up to level 2 within this.
    /// </summary>
    public const int CardTrianglesL0 = 4000;

    /// <summary>
    /// A mass face is left out when all its corners lie this far inside one other mass (a fraction of that mass's radius,
    /// on top of the faceting of a level-1 icosphere), so wind sway and the per-mass leaf-amount lead never open it (23d).
    /// </summary>
    public const float HiddenMargin = 0.06f;

    /// <summary>L1 target (ENG-PERF: 0.6–0.9k); L1 merges its masses down to this many.</summary>
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
    /// The ShadowsOnly proxy of a mother model (WLD-VEGETATION, ENG-PERF): L1 wood plus the L1 masses scaled by
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

    /// <summary>Sides of the far stand-in's trunk (ENG-PERF: L2 is 150–300 triangles or fewer).</summary>
    public const int FarTrunkSides = 5;

    /// <summary>
    /// Vertex tag of the far stand-in in CUSTOM2.w (veg_card, veg_trunk: veg_pool_code): its wood carries 1 (near wood 0),
    /// its masses −2 (near masses −1). The shaders cross-fade the far stand-in against the near pool's L1 by it.
    /// </summary>
    public const float FarWoodTag = 1f, FarMassTag = -2f;

    /// <summary>
    /// The far-pool stand-in of a mother model (WLD-VEGETATION candidate A, mainline cut; max-011 23c2c): a
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
                cap: false, ids, FarWoodTag);
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
        new(palette.Resolve(species.LeafLit).Value, palette.Resolve(Bark).Value);

    private readonly record struct Ids(ushort Leaf, ushort Bark);


}
