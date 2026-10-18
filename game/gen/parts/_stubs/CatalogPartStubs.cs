using Campus.Contracts;
using Campus.Contracts.Parts;

namespace Campus.Gen.Parts.Stubs;

[Part(PartCatalog.FoldingGate, Owner = "WP-00")]
public sealed class FoldingGateStub : BoxPartStub<DoorParams> { }

[Part(PartCatalog.RaisedFloor, Owner = "WP-00")]
public sealed class RaisedFloorStub : BoxPartStub<PanelParams> { }

[Part(PartCatalog.TieredFloor, Owner = "WP-00")]
public sealed class TieredFloorStub : BoxPartStub<StairParams> { }

[Part(PartCatalog.ElevatorDoor, Owner = "WP-00")]
public sealed class ElevatorDoorStub : BoxPartStub<DoorParams> { }

[Part(PartCatalog.TestTower, Owner = "WP-00")]
public sealed class TestTowerStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.SolarPanel, Owner = "WP-00")]
public sealed class SolarPanelStub : BoxPartStub<PanelParams> { }

[Part(PartCatalog.WallFan, Owner = "WP-00")]
public sealed class WallFanStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.SecurityCamera, Owner = "WP-00")]
public sealed class SecurityCameraStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.IndoorSpeaker, Owner = "WP-00")]
public sealed class IndoorSpeakerStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.CableTray, Owner = "WP-00")]
public sealed class CableTrayStub : BoxPartStub<TrimParams> { }

[Part(PartCatalog.Turnstile, Owner = "WP-00")]
public sealed class TurnstileStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.Switchgear, Owner = "WP-00")]
public sealed class SwitchgearStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.DirectorySign, Owner = "WP-00")]
public sealed class DirectorySignStub : BoxPartStub<SignParams> { }

[Part(PartCatalog.Pennant, Owner = "WP-00")]
public sealed class PennantStub : BoxPartStub<SignParams> { }

[Part(PartCatalog.Banner, Owner = "WP-00")]
public sealed class BannerStub : BoxPartStub<SignParams> { }

[Part(PartCatalog.RoadNameSign, Owner = "WP-00")]
public sealed class RoadNameSignStub : BoxPartStub<SignParams> { }

[Part(PartCatalog.StreetSpeaker, Owner = "WP-00")]
public sealed class StreetSpeakerStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.ConvexMirror, Owner = "WP-00")]
public sealed class ConvexMirrorStub : BoxPartStub<SignParams> { }

[Part(PartCatalog.UtilityPole, Owner = "WP-00")]
public sealed class UtilityPoleStub : BoxPartStub<ColumnParams> { }

[Part(PartCatalog.StreetFence, Owner = "WP-00")]
public sealed class StreetFenceStub : BoxPartStub<RailingParams> { }

[Part(PartCatalog.Drain, Owner = "WP-00")]
public sealed class DrainStub : BoxPartStub<TrimParams> { }

[Part(PartCatalog.WallClock, Owner = "WP-00")]
public sealed class WallClockStub : BoxPartStub<SignParams> { }

[Part(PartCatalog.NoticeBoard, Owner = "WP-00")]
public sealed class NoticeBoardStub : BoxPartStub<SignParams> { }
