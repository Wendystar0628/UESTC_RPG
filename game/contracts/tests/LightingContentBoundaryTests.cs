using Campus.Tests;
using System.Numerics;
using System.Text.Json;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class LightingContentBoundaryTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ChannelNumbersAreStableAndRejectUnregisteredInputs()
    {
        // These slots are persisted in glass UV2 and consumed by independently authored shaders.
        var fixedSlots = new (int Slot, string Id)[]
        {
            (0, "window.general"), (7, "window.allnight"), (11, "interior.public"),
            (12, "lamp.first"), (23, "reserved.23"), (28, "reserved.28"), (30, "reserved.30"), (31, "sys.poolGain")
        };
        foreach (var (slot, id) in fixedSlots)
            if (LightChannelCatalog.Id(slot).Value != id || LightChannelCatalog.Slot(new ChannelId(id)) != slot)
                throw new InvalidOperationException($"Lighting ABI slot {slot} changed.");
        var names = Enumerable.Range(0, LightChannelCatalog.Count).Select(LightChannelCatalog.Id).ToArray();
        if (names.Distinct().Count() != 32 || names.Where((id, slot) => LightChannelCatalog.Slot(id) != slot).Any())
            throw new InvalidOperationException("Lighting slots must form a complete bijection.");
        if (LightChannelCatalog.Slot(new ChannelId("WINDOW.GENERAL")) != -1 || LightChannelCatalog.Slot(default) != -1)
            throw new InvalidOperationException("Unregistered names must not silently select a shader slot.");
        AssertThrown(() => LightChannelCatalog.Id(-1)).IsInstanceOf<ArgumentException>();
        AssertThrown(() => LightChannelCatalog.Id(32)).IsInstanceOf<ArgumentException>();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ContentOutputRoundTripPreservesWindowNumberingAndShaderPayloadBoundaries()
    {
        const int baseId = 65500, columns = 20, localColumn = 5, row = 1, columnStart = 10;
        var channel = LightChannelCatalog.Id(3);
        var packedChannel = LightChannelCatalog.Slot(channel) + 32 * columns;
        var windowId = baseId + row * columns + columnStart + localColumn;
        var facade = new WindowFacadeDecl(baseId, columns, channel, columnStart, 6, 2,
            Vector3.Zero, Vector3.UnitX, Vector3.UnitY, 3, 2, 2, [1, 5])
        { Skip = [11] };
        var loose = new WindowDecl(windowId, baseId, columns, channel,
            new Vector3(16, 6, 0), Vector3.UnitX, Vector3.UnitY, 2, 2);
        float[] positions = [0, 0, 0, 1, 0, 0, 0, 1, 0];
        float[] normals = [0, 0, 1, 0, 0, 1, 0, 0, 1];
        var glass = new GeometryBundle(new ToonRef(new ToonSpec(new("glass.lit"), new("glass.shade")) { Kind = ToonKind.Glass }),
            positions, normals, [0, 1, 2], Uv: [15, 1, 16, 1, 15, 2],
            Uv2: [baseId, packedChannel, baseId, packedChannel, baseId, packedChannel])
        {
            Reserved = [15, 15, 15],
            Reflect = true
        };
        foreach (var type in new byte[] { 0, WindowCardEncoding.PhysicalCurtainFlag, WindowCardEncoding.Lobby + 1 })
            AssertThrown(() => (glass with { Reserved = [type, type, type] }).ValidateVertexData()).IsInstanceOf<ArgumentException>();
        AssertThrown(() => (glass with { Reserved = null }).ValidateVertexData()).IsInstanceOf<ArgumentException>();
        (glass with { Reserved = [WindowCardEncoding.Lobby, WindowCardEncoding.Lobby, WindowCardEncoding.Lobby] }).ValidateVertexData();
        var curtainedType = (byte)(WindowCardEncoding.PhysicalCurtainFlag | 1);
        (glass with { Reserved = [curtainedType, curtainedType, curtainedType] }).ValidateVertexData();
        (glass with { Reserved = null, Uv2 = null }).ValidateVertexData();
        (glass with { Reserved = null, Uv2 = [0, 0, 0, 0, 0, 0] }).ValidateVertexData();
        (glass with { Reserved = null, Material = new(glass.Material.Spec with { Kind = ToonKind.GlassNear }) }).ValidateVertexData();
        var water = new GeometryBundle(new ToonRef(new ToonSpec(new("water.far.lit"), new("water.far.shade"))),
            positions, normals, [0, 1, 2], Uv: [.2f, 0, .2f, 0, .2f, 0], Uv2: [12, -1, 12, -1, 12, -1])
        {
            Shader = ShaderId.Water,
            Flags = [8, 8, 8]
        };
        var output = new ContentOutput([glass, water], [])
        {
            Windows = [facade],
            LooseWindows = [loose],
            Entrances = [new EntranceDecl("fixture-door", new(0, 0, 2.5f), new(0, 0, -3), 1.5f, false)]
        };
        var json = JsonSerializer.Serialize(output, ContractJson.Options);
        var restored = JsonSerializer.Deserialize<ContentOutput>(json, ContractJson.Options)
            ?? throw new JsonException("Null content output.");
        foreach (var mesh in restored.Meshes) mesh.ValidateVertexData();
        var restoredGlass = restored.Meshes[0];
        var uv = restoredGlass.Uv ?? throw new InvalidOperationException("Missing window UV.");
        var uv2 = restoredGlass.Uv2 ?? throw new InvalidOperationException("Missing window UV2.");
        var recoveredColumns = (int)uv2[1] / LightChannelCatalog.Count;
        var recoveredSlot = (int)uv2[1] % LightChannelCatalog.Count;
        var recoveredId = (int)uv2[0] + (int)uv[1] * recoveredColumns + (int)uv[0];
        if (BitConverter.SingleToInt32Bits(uv2[0]) != BitConverter.SingleToInt32Bits(baseId) ||
            recoveredId != 65535 || recoveredId != restored.LooseWindows[0].Id || recoveredSlot != 3)
            throw new InvalidOperationException("Window IDs or channel packing lost fp32 precision.");
        if (!restored.Windows[0].Skip.Contains(row * restored.Windows[0].Columns + localColumn) ||
            restoredGlass.Reserved is not [15, 15, 15] || !restoredGlass.Reflect || restored.Entrances[0].Open)
            throw new InvalidOperationException("Irregular-window handoff or closed-entry metadata was lost.");
        var restoredWater = restored.Meshes[1];
        if (restoredWater.Shader != ShaderId.Water || restoredWater.Uv2 is not [12, -1, 12, -1, 12, -1] ||
            restoredWater.Flags is not [8, 8, 8] || restoredWater.Reflect)
            throw new InvalidOperationException("Non-toon water payload was mistaken for glass window numbering.");
    }


}
