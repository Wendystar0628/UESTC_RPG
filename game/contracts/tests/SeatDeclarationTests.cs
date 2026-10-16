using System.Numerics;
using System.Text.Json;
using GdUnit4;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class SeatDeclarationTests
{
    [TestCase]
    public void ExistingPartImplementationNeedsNoSeatsOverride()
    {
        IPartDef<int> legacy = new LegacyPart();
        if (legacy.Seats(0).Count != 0) throw new InvalidOperationException("Legacy parts must default to no declared seats.");
    }

    [TestCase]
    public void ExistingContentOutputConstructorAndDeconstructionRemainCompatible()
    {
        var legacy = new ContentOutput([], []);
        var (meshes, parts, emitters, occupancy) = legacy;
        if (legacy.Seats.Count != 0 || ContentOutput.Empty.Seats.Count != 0 ||
            meshes.Count != 0 || parts.Count != 0 || emitters is not null || occupancy is not null)
            throw new InvalidOperationException("Legacy ContentOutput semantics changed.");
        var seat = Seat();
        var declared = legacy with { Seats = [seat] };
        if (declared.Seats.Count != 1 || declared.Seats[0] != seat || legacy.Seats.Count != 0)
            throw new InvalidOperationException("Init-only seat declarations were not retained.");
    }

    [TestCase]
    public void SeatJsonRoundTripContainsExactlyTheSixDeclaredFields()
    {
        var seat = Seat();
        var json = JsonSerializer.Serialize(seat, ContractJson.Options);
        using var document = JsonDocument.Parse(json);
        var names = document.RootElement.EnumerateObject().Select(field => field.Name).Order(StringComparer.Ordinal).ToArray();
        var expected = new[] { "back", "depth", "facing", "from", "places", "to" };
        if (!names.SequenceEqual(expected) || JsonSerializer.Deserialize<SeatDecl>(json, ContractJson.Options) != seat)
            throw new InvalidOperationException("Seat declarations did not preserve the six-field wire shape.");
        var (from, to, facing, depth, back, places) = seat;
        if (from != seat.From || to != seat.To || facing != seat.Facing || depth != seat.Depth || back != seat.Back || places != seat.Places)
            throw new InvalidOperationException("Seat positional deconstruction changed.");
    }

    [TestCase]
    public void ExistingVectorJsonBoundaryRejectsNonFiniteSeatCoordinates()
    {
        const string json = """
            {"from":[0,0.45,0],"to":[1.5,0.45,0],"facing":[0,0,-1],"depth":0.5,"back":true,"places":2}
            """;
        foreach (var invalid in new[]
        {
            json.Replace("[0,0.45,0]", "[1e1000,0.45,0]", StringComparison.Ordinal),
            json.Replace("[1.5,0.45,0]", "[1.5,1e1000,0]", StringComparison.Ordinal),
            json.Replace("[0,0,-1]", "[0,0,1e1000]", StringComparison.Ordinal)
        }) Expect<JsonException>(() => JsonSerializer.Deserialize<SeatDecl>(invalid, ContractJson.Options));
    }

    private static SeatDecl Seat() => new(new(0, .45f, 0), new(1.5f, .45f, 0), -Vector3.UnitZ, .5f, true, 2);

    private static void Expect<TException>(Action action) where TException : Exception
    {
        try { action(); }
        catch (TException) { return; }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }

    // Deliberately implements only the pre-seat abstract surface.
    private sealed class LegacyPart : IPartDef<int>
    {
        public IReadOnlyDictionary<LodLevel, Func<int, PartGeometry>> Lods { get; } =
            new Dictionary<LodLevel, Func<int, PartGeometry>>();
        public Box3 Bounds(int p) => new(Vector3.Zero, Vector3.One);
        public PartBudget Budget { get; } = new(0, 0);
    }
}
