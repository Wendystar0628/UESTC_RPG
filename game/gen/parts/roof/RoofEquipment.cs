using Campus.Contracts.Parts;

namespace Campus.Gen.Parts.Roof;

/// <summary>Shared roof and facade equipment dimensions used by building fittings.</summary>
public static class RoofEquipment
{
    public static readonly EquipmentParams DormUnit = new(0.64f, 0.48f, 0.26f);
    public static readonly PanelParams DormCage = new(0.8f, 0.64f, 0.36f);
}
