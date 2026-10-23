using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Campus.Contracts;
using Campus.Contracts.Save;

namespace Campus.Kernel.Save.Format.Tests;

internal static class SaveFormatFixture
{
    internal static SaveMeta Meta(ulong gen = 1, ulong line = 1, SaveReason reason = SaveReason.Test) => new(SaveContainer.CurrentFormat, new("s1"), SlotKind.Auto,
        gen, line, 20, new("test-map"), 17.5, 17.5, Season.Autumn, WeatherKind.Clear, ViewMode.Fly,
        new(new Vector3(1, 2, 3), 4, 5), null, null, null, 1792082400, reason);
    internal static SaveDocument Document(ulong gen = 1, ulong line = 1, SaveReason reason = SaveReason.Test) => new(Meta(gen, line, reason),
        new SortedDictionary<string, RawSaveSection>(StringComparer.Ordinal)
        { ["world"] = new(1, "{\"v\":1,\"data\":{\"hours\":17.5}}"u8.ToArray()) }, null);
    internal static SaveSections Sections(TestSection? section = null, int version = 1, RestorePhase phase = RestorePhase.World) =>
        new([new(new("world") { Version = version, Phase = phase, Order = 10 }, section ?? new())]);
}

public sealed record TestState(double Hours);
public sealed class TestSection : ISaveSection<TestState>
{
    public TestState Current { get; set; } = new(17.5);
    public System.Text.Json.Serialization.Metadata.JsonTypeInfo<TestState> TypeInfo => TestSaveJsonContext.Strict.TestState;
    public TestState Capture(in CaptureContext context) => Current;
    public void Restore(TestState state, RestoreContext context) => Current = state;
    public TestState CreateDefault(NewGameContext context) => new(context.Start.Clock);
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(TestState))]
internal partial class TestSaveJsonContext : JsonSerializerContext
{
    internal static TestSaveJsonContext Strict { get; } = new(new JsonSerializerOptions(ContractJson.Options)
    { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, RespectRequiredConstructorParameters = true });
}
