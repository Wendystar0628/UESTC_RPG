using Campus.Tests;
using System.Numerics;
using System.Text.Json;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class SeatDeclarationTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ExistingPartImplementationNeedsNoSeatsOverride()
    {
        IPartDef<int> part = new DefaultSeatsPart();
        if (part.Seats(0).Count != 0) throw new InvalidOperationException("Parts without a Seats override must expose no declared seats.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void EmptyContentOutputConstructionAndDeconstructionPreserveDeclaredDefaults()
    {
        var emptyOutput = new ContentOutput([], []);
        var (meshes, parts, emitters, occupancy) = emptyOutput;
        if (emptyOutput.Seats.Count != 0 || ContentOutput.Empty.Seats.Count != 0 ||
            meshes.Count != 0 || parts.Count != 0 || emitters is not null || occupancy is not null)
            throw new InvalidOperationException("Empty ContentOutput defaults changed.");
        var seat = Seat();
        var declared = emptyOutput with { Seats = [seat] };
        if (declared.Seats.Count != 1 || declared.Seats[0] != seat || emptyOutput.Seats.Count != 0)
            throw new InvalidOperationException("Init-only seat declarations were not retained.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
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
    [TestCategory(TestReason.Contract)]
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
        })
        {
            bool rejected = false;
            try { JsonSerializer.Deserialize<SeatDecl>(invalid, ContractJson.Options); }
            catch (JsonException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Expected JsonException.").IsTrue();
        }
    }

    private static SeatDecl Seat() => new(new(0, .45f, 0), new(1.5f, .45f, 0), -Vector3.UnitZ, .5f, true, 2);

    // Implements the required part members and uses the default empty seat declaration.
    private sealed class DefaultSeatsPart : IPartDef<int>
    {
        public IReadOnlyDictionary<LodLevel, Func<int, PartGeometry>> Lods { get; } =
            new Dictionary<LodLevel, Func<int, PartGeometry>>();
        public Box3 Bounds(int p) => new(Vector3.Zero, Vector3.One);
        public PartBudget Budget { get; } = new(0, 0);
    }
}
