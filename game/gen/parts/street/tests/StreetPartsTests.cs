using System.Numerics;
using System.Reflection;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using GdUnit4;
using Campus.Tests;
using static GdUnit4.Assertions;

namespace Campus.Gen.Parts.Street.Tests;

[TestSuite]
public sealed class StreetPartsTests
{
    private static IEnumerable<Type> StreetTypes => typeof(StreetGallery).Assembly.GetTypes()
        .Where(type => type.Namespace == typeof(StreetGallery).Namespace ||
            type.Namespace?.StartsWith(typeof(StreetGallery).Namespace + ".", StringComparison.Ordinal) == true);
    private static readonly string[] Parts =
    [
        PartCatalog.StreetLamp, PartCatalog.Bench, PartCatalog.BinFour, PartCatalog.Bollard, PartCatalog.StoneSphere, PartCatalog.BusStop,
        PartCatalog.AtlasKiosk, PartCatalog.AtlasPost, PartCatalog.EntranceMat, PartCatalog.OpenSign, PartCatalog.EntranceLamp,
        PartCatalog.ParkedCar, PartCatalog.ParkedBicycle, PartCatalog.BicycleShelter, PartCatalog.VendingMachine, PartCatalog.DeliveryLocker,
        PartCatalog.DrinkingFountain, PartCatalog.Signpost, PartCatalog.CurbRamp,
    ];

    private static PartCatalogEntry Entry(string id) =>
        PartCatalog.Find(new(id)) ?? throw new InvalidOperationException($"{id} is not catalogued.");

    [TestCase, TestCategory(TestReason.Contract)]
    public void RegisteredPartsMatchTheirCatalogueEntries()
    {
        var parts = StreetTypes.Where(t => t.GetCustomAttribute<PartAttribute>() is not null).ToArray();
        AssertThat(parts.Select(t => t.GetCustomAttribute<PartAttribute>()!.Id).Order(StringComparer.Ordinal).SequenceEqual(Parts.Order(StringComparer.Ordinal)))
            .OverrideFailureMessage($"13a and 13c2b register exactly their parts; found {string.Join(", ", parts.Select(t => t.GetCustomAttribute<PartAttribute>()!.Id))}.")
            .IsTrue();
        foreach (var type in parts)
        {
            var attribute = type.GetCustomAttribute<PartAttribute>()!;
            var entry = Entry(attribute.Id);
            AssertThat(attribute.Owner == "WP-13" && entry.Owner == new WpId("WP-13")).OverrideFailureMessage($"{attribute.Id} has the wrong owner.").IsTrue();
            AssertThat(Contract(type).GetGenericArguments()[0] == entry.ParameterType).OverrideFailureMessage($"{attribute.Id} takes the wrong parameter record.").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void CatalogueDefaultsBuildClosedAtEveryLevel()
    {
        foreach (var type in StreetTypes.Where(t => t.GetCustomAttribute<PartAttribute>() is not null))
        {
            var entry = Entry(type.GetCustomAttribute<PartAttribute>()!.Id);
            var part = Activator.CreateInstance(type)!;
            var lods = (System.Collections.IDictionary)type.GetProperty("Lods")!.GetValue(part)!;
            var bounds = (Box3)type.GetMethod("Bounds")!.Invoke(part, [entry.Defaults])!;
            AssertThat(lods.Contains(LodLevel.L0) && lods.Contains(LodLevel.L1)).OverrideFailureMessage($"{entry.Id.Value} lacks near levels.").IsTrue();
            foreach (System.Collections.DictionaryEntry level in lods)
            {
                var geometry = (PartGeometry)((Delegate)level.Value!).DynamicInvoke(entry.Defaults)!;
                ClosedClockwise(geometry, $"{entry.Id.Value} {level.Key}");
                AssertThat(geometry.Bounds == bounds).OverrideFailureMessage($"{entry.Id.Value} {level.Key} moved the bounds.").IsTrue();
            }
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void ReviewSamplesAreClosedAndBounded()
    {
        var samples = StreetGallery.CreateSamples();
        AssertThat(samples.Select(s => s.Part.Value).Distinct().Count() == Parts.Length).OverrideFailureMessage("The gallery misses a part.").IsTrue();
        foreach (var sample in samples)
            foreach (var level in Enum.GetValues<LodLevel>())
            {
                if (sample.Build(level) is not { } geometry) continue;
                ClosedClockwise(geometry, $"{sample.Id} {level}");
                AssertThat(geometry.Bounds == sample.Bounds).OverrideFailureMessage($"{sample.Id} {level} moved the bounds.").IsTrue();
                // The contract's AO is visibility; street parts have no reveals, so nothing is darkened.
                AssertThat(geometry.Meshes.All(m => m.Ao is null)).OverrideFailureMessage($"{sample.Id} {level} writes occlusion it does not have.").IsTrue();
            }
    }

    [TestCase, TestCategory(TestTag.Determinism)]
    [TestCategory(TestReason.Contract)]
    public void GenerationIsBitwiseRepeatable()
    {
        var first = StreetGallery.CreateExamples(); var second = StreetGallery.CreateExamples();
        AssertThat(first.Count == second.Count && first.Count >= Parts.Length).OverrideFailureMessage("Missing gallery examples.").IsTrue();
        for (var i = 0; i < first.Count; i++)
        {
            AssertThat(first[i].Geometry.Meshes.Count == second[i].Geometry.Meshes.Count).OverrideFailureMessage("Surface count changed.").IsTrue();
            for (var m = 0; m < first[i].Geometry.Meshes.Count; m++)
            {
                var a = first[i].Geometry.Meshes[m]; var b = second[i].Geometry.Meshes[m];
                AssertThat(a.Material == b.Material).OverrideFailureMessage("Material order changed.").IsTrue();
                AssertThat(a.Position.Select(BitConverter.SingleToInt32Bits).SequenceEqual(b.Position.Select(BitConverter.SingleToInt32Bits))).OverrideFailureMessage("Vertex bytes changed.").IsTrue();
                AssertThat(a.Normal.Select(BitConverter.SingleToInt32Bits).SequenceEqual(b.Normal.Select(BitConverter.SingleToInt32Bits))).OverrideFailureMessage("Normal bytes changed.").IsTrue();
                AssertThat(a.Index.SequenceEqual(b.Index)).OverrideFailureMessage("Index bytes changed.").IsTrue();
            }
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void CollidersFillFootStayCloseAndFollowTheWalkTable()
    {
        foreach (var sample in StreetGallery.CreateSamples())
        {
            var type = StreetTypes.Single(t => t.GetCustomAttribute<PartAttribute>()?.Id == sample.Part.Value);
            var colliders = (IReadOnlyList<Collider>)Contract(type).GetMethod("Collision")!.Invoke(Activator.CreateInstance(type), [sample.Params])!;
            // Parked bicycles never collide (WLD-LIFE: they come and go, a collider would leave an invisible wall).
            var visualOnly = sample.Id is "entrance-mat" or "entrance-lamp" or "open-sign-wall" or "bicycle";
            AssertThat(visualOnly ? colliders.Count == 0 : colliders.Count > 0).OverrideFailureMessage($"{sample.Id} has the wrong collision class (MAP-QSH-ADDITIONS).").IsTrue();
            foreach (var collider in colliders)
            {
                AssertThat(collider.Foot != FootMat.Auto).OverrideFailureMessage($"{sample.Id} leaves a collider on Auto (G21).").IsTrue();
                // The kerb ramp is walked on: ground, not a prop.
                AssertThat(collider.Tag == (sample.Id == "kerb-ramp" ? SurfaceTag.Ground : SurfaceTag.Prop)).OverrideFailureMessage($"{sample.Id} collider has the wrong surface tag.").IsTrue();
                var (min, max) = Extent(collider);
                var b = sample.Bounds;
                // SYS-WALK: no more than 0.3 m beyond the visible object; posts may reach below grade.
                AssertThat(min.X >= b.Min.X - 0.3f && min.Z >= b.Min.Z - 0.3f && max.X <= b.Max.X + 0.3f && max.Z <= b.Max.Z + 0.3f && max.Y <= b.Max.Y + 0.3f).OverrideFailureMessage($"{sample.Id} collider reaches more than 0.3 m past the part.").IsTrue();
            }
        }
        IPartDef<FurnitureParams> sphere = new StoneSpherePart();
        AssertThat(sphere.Collision(new()).Single().Foot == FootMat.StonePaving).OverrideFailureMessage("Stone spheres step as stone paving (WLD-LIFE).").IsTrue();
        IPartDef<FurnitureParams> bench = new BenchPart();
        AssertThat(bench.Collision(new()).All(c => c.Foot == FootMat.WoodFloor)).OverrideFailureMessage("Bench colliders step as wood (WLD-LIFE).").IsTrue();
        AssertThat(bench.Collision(new() { Foot = FootMat.Metal }).All(c => c.Foot == FootMat.Metal)).OverrideFailureMessage("A caller's Foot overrides the default.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void BenchSeatsComeFromTheSeatCollider()
    {
        // Read through the contract interface only: a shadow seat type would leave IPartDef on its default empty list (contract-33).
        IPartDef<FurnitureParams> def = new BenchPart();
        var backed = new FurnitureParams(1.8f, 0.8f, 0.62f);
        var seat = (BoxCollider)def.Collision(backed)[0];
        var seats = def.Seats(backed);
        AssertThat(seats.Count == 1 && seats[0].Back && seats[0].Places == 2).OverrideFailureMessage("A 1.8 m backed bench declares one front edge with two places.").IsTrue();
        var top = seat.At.M42 + seat.Half.Y;
        AssertThat(Math.Abs(top - StreetStyle.SeatHeight) < TestTolerance.Position && Math.Abs(seats[0].From.Y - top) < TestTolerance.Position && Math.Abs(seats[0].To.Y - top) < TestTolerance.Position).OverrideFailureMessage("The seat edge lies on the seat collider top at 0.45 m.").IsTrue();
        AssertThat(Math.Abs(seats[0].From.Z - (seat.At.M43 - seat.Half.Z)) < TestTolerance.Position && seats[0].Facing == -Vector3.UnitZ).OverrideFailureMessage("The seat edge is the collider's front edge, facing −Z.").IsTrue();
        AssertThat(seats[0].From.X < seats[0].To.X).OverrideFailureMessage("From is on the sitter's left.").IsTrue();
        AssertThat(Math.Abs(seats[0].Depth - 2 * seat.Half.Z) < TestTolerance.Position).OverrideFailureMessage("Seat depth is the collider depth.").IsTrue();
        var backless = def.Seats(new FurnitureParams(1.6f, 0.45f, 0.5f) { Variation = 1 });
        AssertThat(backless.Count == 2 && backless.All(s => !s.Back) && backless[0].Facing == -backless[1].Facing).OverrideFailureMessage("A backless bench seats both sides.").IsTrue();
        foreach (var s in backless)
            AssertThat(Seating.Positions(s).All(p => Math.Abs(p.Y - StreetStyle.SeatHeight) < TestTolerance.Position)).OverrideFailureMessage("Sitting positions lie on the seat.").IsTrue();
        // Turned boxes (a placed tier, a rotated bench) keep the edge on the box's own front.
        var turned = new BoxCollider(Matrix4x4.CreateTranslation(0, 0.2f, 0) * Matrix4x4.CreateRotationY(float.Pi * 0.5f) * Matrix4x4.CreateTranslation(3, 0, 1), new(1f, 0.2f, 0.25f), SurfaceTag.Prop);
        var edge = Seating.FromBox(turned, SeatSide.Front).Single();
        AssertThat(Math.Abs(edge.Facing.X - -1f) < TestTolerance.Position && Math.Abs((edge.From.Y + edge.To.Y) * 0.5f - 0.4f) < TestTolerance.Position).OverrideFailureMessage("A box turned a quarter faces −X.").IsTrue();
        AssertThat(Seating.FromBox(new BoxCollider(Matrix4x4.CreateTranslation(0, 0.6f, 0), new(1, 0.6f, 0.3f), SurfaceTag.Prop), SeatSide.Front).Count == 0).OverrideFailureMessage("A 1.2 m top is not a seat.").IsTrue();
        // Every street part reaches its seats through the contract: only the bench declares any, and nothing returns another seat type.
        foreach (var sample in StreetGallery.CreateSamples())
        {
            var type = StreetTypes.Single(t => t.GetCustomAttribute<PartAttribute>()?.Id == sample.Part.Value);
            var declared = (IReadOnlyList<SeatDecl>)Contract(type).GetMethod("Seats")!.Invoke(Activator.CreateInstance(type), [sample.Params])!;
            AssertThat(sample.Part.Value == PartCatalog.Bench ? declared.Count > 0 : declared.Count == 0).OverrideFailureMessage($"{sample.Id} declares {declared.Count} seats through IPartDef.").IsTrue();
        }
        AssertThat(!StreetTypes.Any(t => t.Name == nameof(SeatDecl))).OverrideFailureMessage("The street assembly still defines its own seat record.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void LampMarkersUseScheduledChannelsAndSitUnderTheLens()
    {
        foreach (var sample in StreetGallery.CreateSamples())
        {
            var at = Matrix4x4.CreateTranslation(10, 1, -4);
            foreach (var emitter in sample.Emitters(at))
            {
                AssertThat(LightChannelCatalog.Slot(emitter.Channel) is >= 12 and <= 27).OverrideFailureMessage($"{sample.Id} marker is not on a lamp channel.").IsTrue();
                AssertThat(emitter.Color.Value == "lamp.warm").OverrideFailureMessage($"{sample.Id} marker is not warm (MAP-QSH-ADDITIONS).").IsTrue();
                AssertThat(emitter.Energy is 1f or 0.75f or 0.5f && emitter.Range > 0).OverrideFailureMessage($"{sample.Id} marker is off the three grades.").IsTrue();
                var local = emitter.Position - new Vector3(10, 1, -4);
                // Markers sit just under their lens: inside the part's height, or a few centimetres under a wall lantern.
                AssertThat(local.Y > sample.Bounds.Min.Y - 0.1f && local.Y < sample.Bounds.Max.Y).OverrideFailureMessage($"{sample.Id} marker is outside the lamp's height.").IsTrue();
            }
            // Every self-lit lens rides a lamp or signal channel.
            foreach (var mesh in sample.Build(LodLevel.L0)!.Meshes)
                if (mesh.Material.Spec.Emissive is { } channel)
                    AssertThat(LightChannelCatalog.Slot(channel) is >= 12 and <= 27).OverrideFailureMessage($"{sample.Id} lens has no lamp channel.").IsTrue();
        }
        var disc = new LampParams(0.7f, 6f, 0.7f);
        AssertThat(StreetLampPart.Emitter(disc, Matrix4x4.Identity).Channel.Value == "lamp.first").OverrideFailureMessage("The disc lamp is in the first-lamp group by default.").IsTrue();
        AssertThat(StreetLampPart.Emitter(disc with { Variation = 2 }, Matrix4x4.Identity).Channel.Value == "lamp.street").OverrideFailureMessage("Variation 2 moves the disc lamp to the street group.").IsTrue();
        AssertThat(StreetLampPart.Emitter(disc, Matrix4x4.Identity, LampGrade.Three).Energy == 0.5f).OverrideFailureMessage("Grade three halves the energy.").IsTrue();
        var arm = new LampParams(1.7f, 7f, 0.5f) { Variation = 1 };
        var marker = StreetLampPart.Emitter(arm, Matrix4x4.Identity);
        AssertThat(marker.Position.Z < -1f && marker.Channel.Value == "lamp.street").OverrideFailureMessage("The single-arm marker hangs under the head, out over the road.").IsTrue();
        var door = EntranceLampPart.Emitter(new LampParams(0.25f, 0.25f, 0.15f), Matrix4x4.Identity);
        AssertThat(door.Channel.Value == "lamp.first" && door.Energy == StreetStyle.GradeTwoEnergy && door.Position.Z < -0.075f).OverrideFailureMessage("The door lamp marker is a first-lamp-group fixture at grade-two energy and sits in front.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void SizesAndColoursFollowTheStyleRules()
    {
        // Every surface colour is a registered Lit/Shade pair name, never a literal.
        var palette = Path.Combine(Campus.Tests.RepositoryFixture.Root, "game", "style", "palette");
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in new[] { Path.Combine(palette, "core.json") }.Concat(Directory.GetFiles(Path.Combine(palette, "ext"), "*.json")))
        {
            using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllBytes(file));
            if (!document.RootElement.TryGetProperty("entries", out var entries)) continue;
            foreach (var entry in entries.EnumerateArray())
                if (entry.TryGetProperty("token", out var token)) tokens.Add(token.GetString()!);
        }
        AssertThat(tokens.Contains(StreetStyle.LampWarm.Value)).OverrideFailureMessage("The lamp colour is registered.").IsTrue();
        foreach (var sample in StreetGallery.CreateSamples())
            foreach (var level in Enum.GetValues<LodLevel>())
            {
                if (sample.Build(level) is not { } geometry) continue;
                foreach (var mesh in geometry.Meshes)
                {
                    var lit = mesh.Material.Spec.Lit.Value; var shade = mesh.Material.Spec.Shade.Value;
                    AssertThat(lit.EndsWith(".lit", StringComparison.Ordinal) && shade == lit[..^4] + ".shade").OverrideFailureMessage($"{sample.Id} uses an unpaired colour {lit}.").IsTrue();
                    AssertThat(tokens.Contains(lit) && tokens.Contains(shade)).OverrideFailureMessage($"{sample.Id} uses unregistered colour {lit}.").IsTrue();
                }
            }
        // Proportions of the atlas entities (SYS-ATLAS); the seat anchor is checked against the collider above.
        var post = new AtlasPostPart().Bounds(new SignParams(0.3f, 1.05f, 0.3f));
        AssertThat(Math.Abs(post.Max.Y - 1.05f) < TestTolerance.Position).OverrideFailureMessage("The atlas post is 1.05 m tall.").IsTrue();
        var kiosk = new AtlasKioskPart().Bounds(new SignParams(2f, 2.6f, 0.8f));
        AssertThat(Math.Abs(kiosk.Size.X - 2f) < TestTolerance.Position && Math.Abs(kiosk.Max.Y - 2.6f) < TestTolerance.Position).OverrideFailureMessage("The atlas kiosk is 2.0 m wide and 2.6 m tall.").IsTrue();
        // A caller's material colours stone spheres and bollards (the south gate passes its granite and marking paint).
        var granite = new ToonSpec(new("stone.beige.lit"), new("stone.beige.shade"));
        var sphere = new StoneSpherePart().Lods[LodLevel.L0](new FurnitureParams(0.6f, 0.6f, 0.6f) { Material = granite });
        AssertThat(sphere.Meshes.All(m => m.Material.Spec.Lit.Value == "stone.beige.lit")).OverrideFailureMessage("A caller's stone colours the sphere.").IsTrue();
        AssertThat(StrokeText.Covers(StreetStyle.StopTitle) && StrokeText.Covers(StreetStyle.OpenTitle)).OverrideFailureMessage("Fixed labels have stroke glyphs.").IsTrue();
        AssertThat(StreetStyle.BinDoors.Length == 4 && StreetStyle.BinDoors.Distinct().Count() == 4).OverrideFailureMessage("Four distinct sorting colours.").IsTrue();
        AssertThat(StreetStyle.BinDoors[2] == "equip.fireRed" && StreetStyle.BinDoors[1] == StreetStyle.SignBlue).OverrideFailureMessage("Bin red is the campus red; bin and stop share the street blue (Opus review 10:07).").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void TheParkedCarStandsOnBigWheelsUnderArchesWithNoSkirt()
    {
        // Opus review 13c2b 21:05: 0.65 m wheels showing under arches, the body's underside 0.15 m up with no dark skirt below it.
        // Judged from the geometry of both body shapes.
        var part = new ParkedCarPart();
        foreach (var (p, shape) in new[] { (new VehicleParams(1.8f, 1.47f, 4.5f), StreetStyle.Saloon), (new VehicleParams(1.72f, 1.52f, 4f) { Variation = 1 }, StreetStyle.Hatch) })
        {
            var near = part.Lods[LodLevel.L0](p);
            var body = near.Meshes.Where(m => m.Material.Spec.Lit.Value == StreetStyle.CarBodies[0] + ".lit" && m.Material.Spec.Fx.HasFlag(ToonFx.BevelLift)).ToList();
            var tyres = near.Meshes.Where(m => m.Material.Spec.Lit.Value == StreetStyle.Tyre + ".lit" && m.Material.Spec.Normal == ToonNormal.Smooth).ToList();
            AssertThat(body.Count == 1 && tyres.Count == 1).OverrideFailureMessage("the car's body or tyres are not one surface each").IsTrue();
            var bodyPoints = Enumerable.Range(0, body[0].Position.Length / 3).Select(i => P(body[0], i)).ToList();
            var tyrePoints = Enumerable.Range(0, tyres[0].Position.Length / 3).Select(i => P(tyres[0], i)).ToList();
            AssertThat(bodyPoints.Min(q => q.Y) >= StreetStyle.CarClearance - TestTolerance.Position).OverrideFailureMessage($"the body hangs to {bodyPoints.Min(q => q.Y):F3} m").IsTrue();
            var diameter = tyrePoints.Max(q => q.Y) - tyrePoints.Min(q => q.Y);
            AssertThat(MathF.Abs(diameter - 2f * StreetStyle.WheelRadius) < TestTolerance.Position && tyrePoints.Min(q => q.Y) > -TestTolerance.Position).OverrideFailureMessage($"wheels {diameter:F3} m across").IsTrue();
            // Over each axle the body rises in an arch, so the wheel shows whole from the side.
            foreach (var axle in new[] { -p.Depth * 0.5f + shape.FrontAxle * p.Depth, p.Depth * 0.5f - shape.RearAxle * p.Depth })
            {
                var over = bodyPoints.Where(q => MathF.Abs(q.Z - axle) < 0.25f).Min(q => q.Y);
                AssertThat(over >= StreetStyle.WheelRadius + 0.2f).OverrideFailureMessage($"the body comes down to {over:F2} m over the axle at {axle:F2}").IsTrue();
            }
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void StreetLifePartsDeclareTheirSoundsSocketsAndSizes()
    {
        // WLD-LIFE: of the street parts only the vending machine hums, within 4 m; every other part is silent.
        foreach (var sample in StreetGallery.CreateSamples())
        {
            var type = StreetTypes.Single(t => t.GetCustomAttribute<PartAttribute>()?.Id == sample.Part.Value);
            var sounds = (IReadOnlyList<SoundSocketDecl>)Contract(type).GetMethod("Sounds")!.Invoke(Activator.CreateInstance(type), [sample.Params])!;
            if (sample.Part.Value != PartCatalog.VendingMachine) { AssertThat(sounds.Count == 0).OverrideFailureMessage($"{sample.Id} makes a sound.").IsTrue(); continue; }
            AssertThat(sounds.Count == 1 && sounds[0].AudibleRadius <= 4f && sounds[0].Set.Value.Length > 0).OverrideFailureMessage($"{sample.Id}: the compressor socket is missing or carries too far.").IsTrue();
            AssertThat(sample.Bounds.Contains(new(sounds[0].Local.M41, sounds[0].Local.M42, sounds[0].Local.M43))).OverrideFailureMessage($"{sample.Id}: the compressor is outside the machine.").IsTrue();
        }
        // The shared bicycle's contact points (WP-54b's riding clips): seat above the cranks, grips left (−X) and right, axles on the wheels.
        IPartDef<VehicleParams> bike = new ParkedBicyclePart();
        var sockets = bike.Sockets;
        AssertThat(new[] { "seat", "grip.left", "grip.right", "pedal.left", "pedal.right", "axle.front", "axle.rear" }.All(sockets.ContainsKey)).OverrideFailureMessage("The bicycle lacks a riding socket.").IsTrue();
        AssertThat(sockets["grip.left"].M41 < 0 && sockets["grip.right"].M41 > 0 && sockets["axle.front"].M43 < sockets["axle.rear"].M43).OverrideFailureMessage("The bicycle's sockets face the wrong way (front is −Z, a rider's left is −X).").IsTrue();
        AssertThat(sockets["seat"].M42 > sockets["pedal.left"].M42 + 0.4f).OverrideFailureMessage("The saddle is not above the pedals.").IsTrue();
        // Sizes from MAP-QSH-ADDITIONS: the fountain's top at 1.1 m on 0.6 × 0.45, the signpost 2.6 m with 3–5 boards.
        var fountain = new DrinkingFountainPart().Bounds(new EquipmentParams(0.6f, 1.1f, 0.45f));
        AssertThat(Math.Abs(fountain.Max.Y - 1.1f) < TestTolerance.Position).OverrideFailureMessage("The fountain's top is not at 1.1 m.").IsTrue();
        AssertThat(StrokeText.Covers(StreetStyle.FountainTitle)).OverrideFailureMessage("The fountain's plate has no glyphs.").IsTrue();
        foreach (var boards in new[] { 0, 1, 2 })
        {
            var sign = new SignpostPart();
            var geometry = sign.Lods[LodLevel.L0](new SignParams(1.2f, 2.6f, 0.12f) { Variation = boards });
            ClosedClockwise(geometry, $"signpost {3 + boards} boards");
        }
        // Body colours stay low-saturation and registered; a caller's Material picks one.
        var car = new ParkedCarPart().Lods[LodLevel.L0](new VehicleParams(1.8f, 1.47f, 4.5f) { Material = new(new("umbrella.beige.lit"), new("umbrella.beige.shade")) });
        AssertThat(car.Meshes.Any(m => m.Material.Spec.Lit.Value == "umbrella.beige.lit")).OverrideFailureMessage("A caller's colour does not paint the car body.").IsTrue();
        AssertThat(StreetStyle.CarBodies.All(c => !c.Contains("fireRed", StringComparison.Ordinal))).OverrideFailureMessage("A car body uses a saturated colour.").IsTrue();
    }

    private static Type Contract(Type type) => type.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPartDef<>));

    private static (Vector3 Min, Vector3 Max) Extent(Collider collider) => collider switch
    {
        PostCollider p => (new(p.C.X - p.R, p.Y0, p.C.Y - p.R), new(p.C.X + p.R, p.Y1, p.C.Y + p.R)),
        BoxCollider b => (new(b.At.M41 - b.Half.X, b.At.M42 - b.Half.Y, b.At.M43 - b.Half.Z), new(b.At.M41 + b.Half.X, b.At.M42 + b.Half.Y, b.At.M43 + b.Half.Z)),
        RampCollider r => (Vector3.Min(Vector3.Min(r.P0, r.P1), Vector3.Min(r.P2, r.P3)), Vector3.Max(Vector3.Max(r.P0, r.P1), Vector3.Max(r.P2, r.P3))),
        _ => throw new InvalidOperationException($"Unexpected collider {collider.GetType().Name}."),
    };

    private static Vector3 P(GeometryBundle m, int i) => new(m.Position[3 * i], m.Position[3 * i + 1], m.Position[3 * i + 2]);
    private static Vector3 N(GeometryBundle m, int i) => new(m.Normal[3 * i], m.Normal[3 * i + 1], m.Normal[3 * i + 2]);
    private static void ClosedClockwise(PartGeometry geometry, string what)
    {
        AssertThat(geometry.Meshes.Count > 0).OverrideFailureMessage($"{what} is empty.").IsTrue();
        foreach (var m in geometry.Meshes)
        {
            m.ValidateVertexData();
            var edges = new Dictionary<(Vector3, Vector3), int>();
            for (var i = 0; i < m.Index.Length; i += 3)
            {
                var a = m.Index[i]; var b = m.Index[i + 1]; var c = m.Index[i + 2];
                var cross = Vector3.Cross(P(m, b) - P(m, a), P(m, c) - P(m, a));
                AssertThat(Vector3.Dot(cross, N(m, a) + N(m, b) + N(m, c)) < 0).OverrideFailureMessage($"{what}: face winding disagrees with outward normals.").IsTrue();
                foreach (var (from, to) in new[] { (P(m, a), P(m, b)), (P(m, b), P(m, c)), (P(m, c), P(m, a)) })
                { edges.TryGetValue((from, to), out var count); edges[(from, to)] = count + 1; }
            }
            // Printed face geometry (lettering, pictograms; line class None) is flat quads by design (ART-MODELING); every
            // other surface is a set of closed solids.
            if (m.Material.Spec.Line != LineClass.None)
                foreach (var (edge, count) in edges)
                    AssertThat(count == 1 && edges.TryGetValue((edge.Item2, edge.Item1), out var reverse) && reverse == 1).OverrideFailureMessage($"{what}: a solid has an open or non-manifold edge.").IsTrue();
            else AssertThat(m.Flags is not null && m.Flags.All(f => (f & VertexFlag.NoShadow) != 0)).OverrideFailureMessage($"{what}: printed face geometry casts shadows.").IsTrue();
            for (var i = 0; i < m.Position.Length / 3; i++)
            {
                AssertThat(geometry.Bounds.Contains(P(m, i))).OverrideFailureMessage($"{what}: bounds miss vertex {P(m, i)} ({geometry.Bounds.Min} – {geometry.Bounds.Max}).").IsTrue();
                AssertThat(Math.Abs(1 - N(m, i).Length()) <= TestTolerance.Position).OverrideFailureMessage($"{what}: a normal is not unit length.").IsTrue();
            }
        }
    }
}
