using Campus.Contracts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Street;

namespace Campus.Gen.Parts.Sport;

/// <summary>Approved sports proportions; ordinary metal sections reuse the existing parts vocabulary.</summary>
public static class SportsStyle
{
    public const float CourtFence = 4, TrackFence = 2.5f, PoolFence = 2.2f, PostPitch = 3;
    public const float CourtGateWidth = 1.2f, GateHeight = 2.2f, PoolGateWidth = 2.4f;
    public const float CourtLight = 12, TrackLight = 20, PracticeWall = 3;
    public const float Spring = 12, Rise = 13, SkylightWidth = 6;
    public const float SeatRise = 0.4f, SeatDepth = 0.8f, SeatLength = 20;
    public const int SeatRows = 3;
    public const float CanopyBack = 2.6f, CanopyFront = 3;
    public const float Rail = StreetStyle.FenceRail, Post = StreetStyle.FencePostRadius;
    public const float Wire = StreetStyle.FenceUpright;
    public const float WirePitch = StreetStyle.FenceUprightPitch * 2;
    public const float Bevel = PartStyleDefaults.TrimEdgeBevel;
    public const float PoleRadius = StreetStyle.LampPoleBaseRadius;
    public const float Slab = PartStyleDefaults.CorniceSmall;
    public const float Cover = PartStyleDefaults.CorniceMedium;
    // Regulation equipment sizes provide the human scale anchors.
    public const float HoopHeight = 3.05f, HoopRadius = 0.225f, BoardWidth = 1.8f, BoardHeight = 1.05f;
    public const float TennisNet = 0.914f, TennisPost = 1.07f, Marking = 0.05f;
    public const float BasketballLength = 28, BasketballWidth = 15, TennisLength = 23.77f, TennisWidth = 10.97f;
    public const float FenceGroundBand = 3;
    public static readonly ToonSpec Steel = PartMaterials.Metal("frame.alu");
    public static readonly ToonSpec Silver = PartMaterials.Casing("roof.silverMetal");
    public static readonly ToonSpec Green = PartMaterials.Metal("ground.grassDark");
    public static readonly ToonSpec Glass = PartMaterials.Glazing();
    public static readonly ToonSpec White = new(new("ground.marking.lit"), new("ground.marking.shade")) { Line = LineClass.None };
    public static readonly ToonSpec Court = new(new("ground.court.lit"), new("ground.court.shade")) { Line = LineClass.None };
    public static readonly ToonSpec PlayBlue = new(new("umbrella.grayblue.lit"), new("umbrella.grayblue.shade")) { Line = LineClass.None };
    public static readonly ToonSpec PlayRed = new(new("ground.track.lit"), new("ground.track.shade")) { Line = LineClass.None };
}
