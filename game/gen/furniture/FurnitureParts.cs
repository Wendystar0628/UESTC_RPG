using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Street;

namespace Campus.Gen.Furniture;

/// <summary>Catalogue definitions shared by placement and L0 capacity measurement.</summary>
internal static class FurnitureParts
{
    internal static readonly StreetLampPart Lamp = new();
    internal static readonly BenchPart BenchDef = new();
    internal static readonly BinFourPart BinDef = new();
    internal static readonly BusStopPart StopDef = new();
    internal static readonly AtlasKioskPart KioskDef = new();
    internal static readonly AtlasPostPart PostDef = new();
    internal static readonly OpenSignPart SignDef = new();
    internal static readonly ParkedCarPart CarDef = new();
    internal static readonly KerbRampPart RampDef = new();
    internal static readonly SignpostPart SignpostDef = new();
    internal static readonly DrinkingFountainPart FountainDef = new();
    internal static readonly VendingMachinePart VendingDef = new();
    internal static readonly DeliveryLockerPart LockerDef = new();
    internal static readonly BicycleBayPart BayDef = new();
    internal static readonly EntranceMatPart MatDef = new();
    internal static readonly EntranceLampPart DoorLampDef = new();
    internal static readonly ParkedBicyclePart BikeDef = new();
    internal static readonly StoneSpherePart SphereDef = new();

    internal const string Self = FurnitureModule.ModuleId;
    internal const float PoleRadius = 0.25f;
    internal const float StopHalfDepth = 0.15f;
}
