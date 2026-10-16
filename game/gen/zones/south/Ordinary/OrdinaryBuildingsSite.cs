using Campus.Contracts;
using Campus.Gen.Zones.South.Framework;

namespace Campus.Gen.Zones.South.Ordinary;

/// <summary>
/// The south zone's ordinary buildings stay with WP-14's facade rules (MAP-QSH-SOUTH §8, §9): the innovation centre, the Kechuang
/// canteen, the orphan blocks 6152, 6194 and 6195, buildings 1–8 with the public toilet, the stand and the sports platform.
/// Their storeys, heights and families are set in south.overlay.json; this site claims nothing and builds nothing, it only
/// registers the zone's inspection probes on them (MAP-QSH-SOUTH Q5, Q6, Q8 (a)), from their OSM footprints.
/// </summary>
public sealed class OrdinaryBuildingsSite : SouthSite
{
    public const string InnovationA = "qsh:relation:10807928", InnovationC = "qsh:way:779496165", Building4 = "qsh:way:691679134";

    public override string Id => "18c.ordinary";

    public override IReadOnlyList<InspectionProbe> Probes =>
    [
        // Q5 · K3: 20 m in front of A block's road front (osm:way:779496157, facing 37°), eye height.
        Probe("Q5", InnovationA, new(-608.4f, 1.62f, 351.7f), new(-0.598f, 0.801f), 3f, 20f),
        // Q6 · K4 (a): 45° down onto the roofs and back lanes of buildings 1–8 from about 40 m, over building 4.
        Probe("Q6", Building4, new(215.9f, 36.1f, 675.5f), new(0.707f, -0.707f), -45f, 40f),
        // Q8 (a), the first of its three rotating places: 2 m above C block's parapet on its north-east front, across the roof.
        Probe("Q8", InnovationC, new(-675.7f, 33.2f, 537.3f), new(-0.653f, 0.757f), -25f, 2f),
    ];

    public override void Build(SiteBuilder b) { }
}
