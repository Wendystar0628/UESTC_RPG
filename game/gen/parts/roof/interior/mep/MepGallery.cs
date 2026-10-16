using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Roof.Mep;

/// <summary>
/// Review set for the part gallery and the tests: every interior equipment part in all three variations at the sizes the
/// golden-sample dorm (学知苑 1 栋, WLD-INTERIORS §2.8) uses. Door plates carry sample room numbers through
/// <see cref="RoomSignPart.Build"/>.
/// </summary>
public static class MepGallery
{
    public static IReadOnlyList<RoofSample> CreateSamples() => Array.AsReadOnly<RoofSample>(
    [
        Sample("boiler-floor", "开水器（落地两嘴）", PartCatalog.Boiler, new BoilerPart(), new EquipmentParams(0.6f, 1.25f, 0.45f)),
        Sample("boiler-wide", "开水器（三嘴、踢脚）", PartCatalog.Boiler, new BoilerPart(), new EquipmentParams(0.8f, 1.3f, 0.5f) { Variation = 1 }),
        Sample("boiler-wall", "开水器（壁挂）", PartCatalog.Boiler, new BoilerPart(), new EquipmentParams(0.45f, 0.7f, 0.32f) { Variation = 2 }),
        Sample("dispenser-bottle", "饮水机（桶装）", PartCatalog.WaterDispenser, new WaterDispenserPart(), new EquipmentParams(0.32f, 1.32f, 0.34f)),
        Sample("dispenser-cabinet", "饮水机（下置）", PartCatalog.WaterDispenser, new WaterDispenserPart(), new EquipmentParams(0.33f, 1.05f, 0.35f) { Variation = 1 }),
        Sample("dispenser-purifier", "直饮机", PartCatalog.WaterDispenser, new WaterDispenserPart(), new EquipmentParams(0.5f, 1.4f, 0.4f) { Variation = 2 }),
        Sample("hydrant-glass", "消火栓箱（红框玻璃门）", PartCatalog.Hydrant, new HydrantPart(), new EquipmentParams(0.7f, 1f, 0.24f)),
        Sample("hydrant-steel", "消火栓箱（红钢门）", PartCatalog.Hydrant, new HydrantPart(), new EquipmentParams(0.65f, 0.8f, 0.24f) { Variation = 1 }),
        Sample("hydrant-stainless", "消火栓箱（不锈钢框）", PartCatalog.Hydrant, new HydrantPart(), new EquipmentParams(0.7f, 1f, 0.24f) { Variation = 2 }),
        Sample("extinguisher", "灭火器", PartCatalog.Extinguisher, new ExtinguisherPart(), new EquipmentParams(0.16f, 0.52f, 0.16f)),
        Sample("extinguisher-pair", "灭火器（一对）", PartCatalog.Extinguisher, new ExtinguisherPart(), new EquipmentParams(0.34f, 0.52f, 0.17f) { Variation = 1 }),
        Sample("extinguisher-bracket", "灭火器（挂架）", PartCatalog.Extinguisher, new ExtinguisherPart(), new EquipmentParams(0.18f, 0.52f, 0.2f) { Variation = 2 }),
        Sample("exit-door", "安全出口灯（门）", PartCatalog.ExitSign, new ExitSignPart(), new SignParams(0.36f, 0.145f, 0.03f)),
        Sample("exit-left", "安全出口灯（左箭头）", PartCatalog.ExitSign, new ExitSignPart(), new SignParams(0.36f, 0.145f, 0.03f) { Variation = 1 }),
        Sample("exit-right", "安全出口灯（右箭头）", PartCatalog.ExitSign, new ExitSignPart(), new SignParams(0.36f, 0.145f, 0.03f) { Variation = 2 }),
        Plate("room-sign-white", "门牌（白）", new SignParams(0.3f, 0.12f, 0.02f), "1-201"),
        Plate("room-sign-alu", "门牌（铝）", new SignParams(0.24f, 0.09f, 0.015f) { Variation = 1 }, "2-215"),
        Plate("room-sign-wood", "门牌（木）", new SignParams(0.3f, 0.12f, 0.02f) { Variation = 2 }, "1-108"),
        Sample("ac-wall", "壁挂空调（圆弧）", PartCatalog.IndoorAirConditioner, new IndoorAirConditionerPart(), new EquipmentParams(0.85f, 0.29f, 0.21f)),
        Sample("ac-flat", "壁挂空调（平面板）", PartCatalog.IndoorAirConditioner, new IndoorAirConditionerPart(), new EquipmentParams(0.9f, 0.3f, 0.2f) { Variation = 1 }),
        Sample("ac-cabinet", "柜式空调", PartCatalog.IndoorAirConditioner, new IndoorAirConditionerPart(), new EquipmentParams(0.5f, 1.75f, 0.32f) { Variation = 2 }),
        Sample("fan-three", "吊扇（三叶白）", PartCatalog.CeilingFan, new CeilingFanPart(), new EquipmentParams(1.4f, 0.45f, 1.4f)),
        Sample("fan-tapered", "吊扇（三叶灰）", PartCatalog.CeilingFan, new CeilingFanPart(), new EquipmentParams(1.2f, 0.45f, 1.2f) { Variation = 1 }),
        Sample("fan-four", "吊扇（四叶）", PartCatalog.CeilingFan, new CeilingFanPart(), new EquipmentParams(1.3f, 0.5f, 1.3f) { Variation = 2 }),
        Sample("switch-one", "开关面板（单键）", PartCatalog.SwitchPanel, new SwitchPanelPart(), new PanelParams(0.086f, 0.086f, 0.012f)),
        Sample("switch-two", "开关面板（双键）", PartCatalog.SwitchPanel, new SwitchPanelPart(), new PanelParams(0.086f, 0.086f, 0.012f) { Variation = 1 }),
        Sample("switch-three", "开关面板（三键）", PartCatalog.SwitchPanel, new SwitchPanelPart(), new PanelParams(0.086f, 0.086f, 0.012f) { Variation = 2 }),
        Sample("reader-card", "刷卡器", PartCatalog.CardReader, new CardReaderPart(), new EquipmentParams(0.086f, 0.13f, 0.03f)),
        Sample("reader-keypad", "刷卡器（密码键盘）", PartCatalog.CardReader, new CardReaderPart(), new EquipmentParams(0.09f, 0.15f, 0.03f) { Variation = 1 }),
        Sample("reader-face", "人脸平板", PartCatalog.CardReader, new CardReaderPart(), new EquipmentParams(0.13f, 0.21f, 0.025f) { Variation = 2 }),
    ]);

    /// <summary>Near-level geometry of every sample, in the same order (WP-01 gallery examples).</summary>
    public static IReadOnlyList<GalleryExample> CreateExamples() =>
        Array.AsReadOnly(CreateSamples().Select(s => new GalleryExample(s.Id, s.Label, s.Build(LodLevel.L0)!)).ToArray());

    private static RoofSample Sample<TParams>(string id, string label, string part, IPartDef<TParams> def, TParams p) where TParams : BoxPartParams =>
        new(id, label, new(part), p, level => def.Lods.TryGetValue(level, out var build) ? build(p) : null, def.Bounds(p));

    private static RoofSample Plate(string id, string label, SignParams p, string number) =>
        new(id, label, new(PartCatalog.RoomSign), p, level => level <= LodLevel.L1 ? RoomSignPart.Build(p, number, level) : null, new RoomSignPart().Bounds(p));
}
