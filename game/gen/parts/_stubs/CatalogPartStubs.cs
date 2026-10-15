using Campus.Contracts;
using Campus.Contracts.Parts;

namespace Campus.Gen.Parts.Stubs;

[Part(PartCatalog.Box, Owner = "WP-00")]
public sealed class BoxStub : BoxPartStub<BoxParams> { }

[Part(PartCatalog.Column, Owner = "WP-00")]
public sealed class ColumnStub : BoxPartStub<ColumnParams> { }

[Part(PartCatalog.WallPanel, Owner = "WP-00")]
public sealed class WallPanelStub : BoxPartStub<PanelParams> { }

[Part(PartCatalog.Trim, Owner = "WP-00")]
public sealed class TrimStub : BoxPartStub<TrimParams> { }

[Part(PartCatalog.Slab, Owner = "WP-00")]
public sealed class SlabStub : BoxPartStub<PanelParams> { }

[Part(PartCatalog.WindowCasement, Owner = "WP-00")]
public sealed class WindowCasementStub : BoxPartStub<WindowParams> { }

[Part(PartCatalog.WindowRibbon, Owner = "WP-00")]
public sealed class WindowRibbonStub : BoxPartStub<WindowParams> { }

[Part(PartCatalog.WindowSlit, Owner = "WP-00")]
public sealed class WindowSlitStub : BoxPartStub<WindowParams> { }

[Part(PartCatalog.WindowHigh, Owner = "WP-00")]
public sealed class WindowHighStub : BoxPartStub<WindowParams> { }

[Part(PartCatalog.WindowObservation, Owner = "WP-00")]
public sealed class WindowObservationStub : BoxPartStub<WindowParams> { }

[Part(PartCatalog.WindowService, Owner = "WP-00")]
public sealed class WindowServiceStub : BoxPartStub<WindowParams> { }

[Part(PartCatalog.CurtainWall, Owner = "WP-00")]
public sealed class CurtainWallStub : BoxPartStub<WindowParams> { }

[Part(PartCatalog.Door, Owner = "WP-00")]
public sealed class DoorStub : BoxPartStub<DoorParams> { }

[Part(PartCatalog.DoorWood, Owner = "WP-00")]
public sealed class DoorWoodStub : BoxPartStub<DoorParams> { }

[Part(PartCatalog.DoorFire, Owner = "WP-00")]
public sealed class DoorFireStub : BoxPartStub<DoorParams> { }

[Part(PartCatalog.DoorGlass, Owner = "WP-00")]
public sealed class DoorGlassStub : BoxPartStub<DoorParams> { }

[Part(PartCatalog.DoorRoller, Owner = "WP-00")]
public sealed class DoorRollerStub : BoxPartStub<DoorParams> { }

[Part(PartCatalog.DoorScreen, Owner = "WP-00")]
public sealed class DoorScreenStub : BoxPartStub<DoorParams> { }

[Part(PartCatalog.DoorStall, Owner = "WP-00")]
public sealed class DoorStallStub : BoxPartStub<DoorParams> { }

[Part(PartCatalog.AutomaticDoorFrame, Owner = "WP-00")]
public sealed class AutomaticDoorFrameStub : BoxPartStub<DoorParams> { }

[Part(PartCatalog.AutomaticDoorLeaf, Owner = "WP-00")]
public sealed class AutomaticDoorLeafStub : BoxPartStub<DoorParams> { }

[Part(PartCatalog.DoorCloser, Owner = "WP-00")]
public sealed class DoorCloserStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.DoorStop, Owner = "WP-00")]
public sealed class DoorStopStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.DoorVisionPanel, Owner = "WP-00")]
public sealed class DoorVisionPanelStub : BoxPartStub<WindowParams> { }

[Part(PartCatalog.Louvre, Owner = "WP-00")]
public sealed class LouvreStub : BoxPartStub<PanelParams> { }

[Part(PartCatalog.Curtain, Owner = "WP-00")]
public sealed class CurtainStub : BoxPartStub<PanelParams> { }

[Part(PartCatalog.PvcCurtain, Owner = "WP-00")]
public sealed class PvcCurtainStub : BoxPartStub<PanelParams> { }

[Part(PartCatalog.InteriorCard, Owner = "WP-00")]
public sealed class InteriorCardStub : BoxPartStub<PanelParams> { }

[Part(PartCatalog.Railing, Owner = "WP-00")]
public sealed class RailingStub : BoxPartStub<RailingParams> { }

[Part(PartCatalog.Handrail, Owner = "WP-00")]
public sealed class HandrailStub : BoxPartStub<RailingParams> { }

[Part(PartCatalog.StairStraight, Owner = "WP-00")]
public sealed class StairStraightStub : BoxPartStub<StairParams> { }

[Part(PartCatalog.StairDouble, Owner = "WP-00")]
public sealed class StairDoubleStub : BoxPartStub<StairParams> { }

[Part(PartCatalog.StairOpen, Owner = "WP-00")]
public sealed class StairOpenStub : BoxPartStub<StairParams> { }

[Part(PartCatalog.Steps, Owner = "WP-00")]
public sealed class StepsStub : BoxPartStub<StairParams> { }

[Part(PartCatalog.Ramp, Owner = "WP-00")]
public sealed class RampStub : BoxPartStub<RampParams> { }

[Part(PartCatalog.Balcony, Owner = "WP-00")]
public sealed class BalconyStub : BoxPartStub<BalconyParams> { }

[Part(PartCatalog.DryingRail, Owner = "WP-00")]
public sealed class DryingRailStub : BoxPartStub<RailingParams> { }

[Part(PartCatalog.Planter, Owner = "WP-00")]
public sealed class PlanterStub : BoxPartStub<FurnitureParams> { }

[Part(PartCatalog.IronGate, Owner = "WP-00")]
public sealed class IronGateStub : BoxPartStub<DoorParams> { }

[Part(PartCatalog.FoldingGate, Owner = "WP-00")]
public sealed class FoldingGateStub : BoxPartStub<DoorParams> { }

[Part(PartCatalog.RaisedFloor, Owner = "WP-00")]
public sealed class RaisedFloorStub : BoxPartStub<PanelParams> { }

[Part(PartCatalog.TieredFloor, Owner = "WP-00")]
public sealed class TieredFloorStub : BoxPartStub<StairParams> { }

[Part(PartCatalog.Platform, Owner = "WP-00")]
public sealed class PlatformStub : BoxPartStub<PanelParams> { }

[Part(PartCatalog.ElevatorDoor, Owner = "WP-00")]
public sealed class ElevatorDoorStub : BoxPartStub<DoorParams> { }

[Part(PartCatalog.ParapetCap, Owner = "WP-00")]
public sealed class ParapetCapStub : BoxPartStub<TrimParams> { }

[Part(PartCatalog.LightningStrip, Owner = "WP-00")]
public sealed class LightningStripStub : BoxPartStub<TrimParams> { }

[Part(PartCatalog.Skylight, Owner = "WP-00")]
public sealed class SkylightStub : BoxPartStub<RoofParams> { }

[Part(PartCatalog.WaterTank, Owner = "WP-00")]
public sealed class WaterTankStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.HeatPump, Owner = "WP-00")]
public sealed class HeatPumpStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.AirConditioner, Owner = "WP-00")]
public sealed class AirConditionerStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.EquipmentGrille, Owner = "WP-00")]
public sealed class EquipmentGrilleStub : BoxPartStub<PanelParams> { }

[Part(PartCatalog.Canopy, Owner = "WP-00")]
public sealed class CanopyStub : BoxPartStub<RoofParams> { }

[Part(PartCatalog.Downpipe, Owner = "WP-00")]
public sealed class DownpipeStub : BoxPartStub<ColumnParams> { }

[Part(PartCatalog.Antenna, Owner = "WP-00")]
public sealed class AntennaStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.TestTower, Owner = "WP-00")]
public sealed class TestTowerStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.SolarPanel, Owner = "WP-00")]
public sealed class SolarPanelStub : BoxPartStub<PanelParams> { }

[Part(PartCatalog.EquipmentFence, Owner = "WP-00")]
public sealed class EquipmentFenceStub : BoxPartStub<RailingParams> { }

[Part(PartCatalog.Hydrant, Owner = "WP-00")]
public sealed class HydrantStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.Extinguisher, Owner = "WP-00")]
public sealed class ExtinguisherStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.IndoorAirConditioner, Owner = "WP-00")]
public sealed class IndoorAirConditionerStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.CeilingFan, Owner = "WP-00")]
public sealed class CeilingFanStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.WallFan, Owner = "WP-00")]
public sealed class WallFanStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.SecurityCamera, Owner = "WP-00")]
public sealed class SecurityCameraStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.IndoorSpeaker, Owner = "WP-00")]
public sealed class IndoorSpeakerStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.SwitchPanel, Owner = "WP-00")]
public sealed class SwitchPanelStub : BoxPartStub<PanelParams> { }

[Part(PartCatalog.CableTray, Owner = "WP-00")]
public sealed class CableTrayStub : BoxPartStub<TrimParams> { }

[Part(PartCatalog.Boiler, Owner = "WP-00")]
public sealed class BoilerStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.WaterDispenser, Owner = "WP-00")]
public sealed class WaterDispenserStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.CardReader, Owner = "WP-00")]
public sealed class CardReaderStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.Turnstile, Owner = "WP-00")]
public sealed class TurnstileStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.Switchgear, Owner = "WP-00")]
public sealed class SwitchgearStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.ExitSign, Owner = "WP-00")]
public sealed class ExitSignStub : BoxPartStub<SignParams> { }

[Part(PartCatalog.RoomSign, Owner = "WP-00")]
public sealed class RoomSignStub : BoxPartStub<SignParams> { }

[Part(PartCatalog.DirectorySign, Owner = "WP-00")]
public sealed class DirectorySignStub : BoxPartStub<SignParams> { }

[Part(PartCatalog.Pennant, Owner = "WP-00")]
public sealed class PennantStub : BoxPartStub<SignParams> { }

[Part(PartCatalog.Banner, Owner = "WP-00")]
public sealed class BannerStub : BoxPartStub<SignParams> { }

[Part(PartCatalog.StreetLamp, Owner = "WP-00")]
public sealed class StreetLampStub : BoxPartStub<LampParams> { }

[Part(PartCatalog.Bench, Owner = "WP-00")]
public sealed class BenchStub : BoxPartStub<FurnitureParams> { }

[Part(PartCatalog.BinFour, Owner = "WP-00")]
public sealed class BinFourStub : BoxPartStub<FurnitureParams> { }

[Part(PartCatalog.Bollard, Owner = "WP-00")]
public sealed class BollardStub : BoxPartStub<ColumnParams> { }

[Part(PartCatalog.StoneSphere, Owner = "WP-00")]
public sealed class StoneSphereStub : BoxPartStub<FurnitureParams> { }

[Part(PartCatalog.BusStop, Owner = "WP-00")]
public sealed class BusStopStub : BoxPartStub<SignParams> { }

[Part(PartCatalog.ParkedCar, Owner = "WP-00")]
public sealed class ParkedCarStub : BoxPartStub<VehicleParams> { }

[Part(PartCatalog.ParkedBicycle, Owner = "WP-00")]
public sealed class ParkedBicycleStub : BoxPartStub<VehicleParams> { }

[Part(PartCatalog.DeliveryLocker, Owner = "WP-00")]
public sealed class DeliveryLockerStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.VendingMachine, Owner = "WP-00")]
public sealed class VendingMachineStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.Signpost, Owner = "WP-00")]
public sealed class SignpostStub : BoxPartStub<SignParams> { }

[Part(PartCatalog.DrinkingFountain, Owner = "WP-00")]
public sealed class DrinkingFountainStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.AtlasKiosk, Owner = "WP-00")]
public sealed class AtlasKioskStub : BoxPartStub<SignParams> { }

[Part(PartCatalog.AtlasPost, Owner = "WP-00")]
public sealed class AtlasPostStub : BoxPartStub<SignParams> { }

[Part(PartCatalog.EntranceMat, Owner = "WP-00")]
public sealed class EntranceMatStub : BoxPartStub<PanelParams> { }

[Part(PartCatalog.OpenSign, Owner = "WP-00")]
public sealed class OpenSignStub : BoxPartStub<SignParams> { }

[Part(PartCatalog.EntranceLamp, Owner = "WP-00")]
public sealed class EntranceLampStub : BoxPartStub<LampParams> { }

[Part(PartCatalog.RoadNameSign, Owner = "WP-00")]
public sealed class RoadNameSignStub : BoxPartStub<SignParams> { }

[Part(PartCatalog.StreetSpeaker, Owner = "WP-00")]
public sealed class StreetSpeakerStub : BoxPartStub<EquipmentParams> { }

[Part(PartCatalog.ConvexMirror, Owner = "WP-00")]
public sealed class ConvexMirrorStub : BoxPartStub<SignParams> { }

[Part(PartCatalog.BicycleShelter, Owner = "WP-00")]
public sealed class BicycleShelterStub : BoxPartStub<RoofParams> { }

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
