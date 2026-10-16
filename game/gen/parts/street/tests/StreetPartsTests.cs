using System.Numerics;
using System.Reflection;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using GdUnit4;

namespace Campus.Gen.Parts.Street.Tests;

[TestSuite]
public sealed class StreetPartsTests
{
    private static readonly Assembly Street = typeof(StreetGallery).Assembly;
    private static readonly string[] Parts =
    [
        PartCatalog.StreetLamp, PartCatalog.Bench, PartCatalog.BinFour, PartCatalog.Bollard, PartCatalog.StoneSphere, PartCatalog.BusStop,
        PartCatalog.AtlasKiosk, PartCatalog.AtlasPost, PartCatalog.EntranceMat, PartCatalog.OpenSign, PartCatalog.EntranceLamp,
        PartCatalog.ParkedCar, PartCatalog.ParkedBicycle, PartCatalog.BicycleShelter, PartCatalog.VendingMachine, PartCatalog.DeliveryLocker,
        PartCatalog.DrinkingFountain, PartCatalog.Signpost, PartCatalog.CurbRamp,
    ];

    private static PartCatalogEntry Entry(string id) =>
        PartCatalog.Find(new(id)) ?? throw new InvalidOperationException($"{id} is not catalogued.");

    [TestCase]
    public void RegisteredPartsMatchTheirCatalogueEntries()
    {
        var parts = Street.GetTypes().Where(t => t.GetCustomAttribute<PartAttribute>() is not null).ToArray();
        Check(parts.Select(t => t.GetCustomAttribute<PartAttribute>()!.Id).Order(StringComparer.Ordinal).SequenceEqual(Parts.Order(StringComparer.Ordinal)),
            $"13a and 13c2b register exactly their parts; found {string.Join(", ", parts.Select(t => t.GetCustomAttribute<PartAttribute>()!.Id))}.");
        foreach (var type in parts)
        {
            var attribute = type.GetCustomAttribute<PartAttribute>()!;
            var entry = Entry(attribute.Id);
            Check(attribute.Owner == "WP-13" && entry.Owner == new WpId("WP-13"), $"{attribute.Id} has the wrong owner.");
            Check(Contract(type).GetGenericArguments()[0] == entry.ParameterType, $"{attribute.Id} takes the wrong parameter record.");
        }
    }

    [TestCase]
    public void CatalogueDefaultsBuildClosedAtEveryLevelWithinBudget()
    {
        foreach (var type in Street.GetTypes().Where(t => t.GetCustomAttribute<PartAttribute>() is not null))
        {
            var entry = Entry(type.GetCustomAttribute<PartAttribute>()!.Id);
            var part = Activator.CreateInstance(type)!;
            var lods = (System.Collections.IDictionary)type.GetProperty("Lods")!.GetValue(part)!;
            var bounds = (Box3)type.GetMethod("Bounds")!.Invoke(part, [entry.Defaults])!;
            var budget = (PartBudget)type.GetProperty("Budget")!.GetValue(part)!;
            Check(lods.Contains(LodLevel.L0) && lods.Contains(LodLevel.L1), $"{entry.Id.Value} lacks near levels.");
            foreach (System.Collections.DictionaryEntry level in lods)
            {
                var geometry = (PartGeometry)((Delegate)level.Value!).DynamicInvoke(entry.Defaults)!;
                ClosedClockwise(geometry, $"{entry.Id.Value} {level.Key}");
                Check(geometry.Bounds == bounds, $"{entry.Id.Value} {level.Key} moved the bounds.");
                var triangles = geometry.Meshes.Sum(m => m.Index.Length / 3);
                Check(triangles <= ((LodLevel)level.Key == LodLevel.L0 ? budget.L0 : budget.L1), $"{entry.Id.Value} {level.Key} has {triangles} triangles, over budget.");
            }
        }
    }

    [TestCase]
    public void ReviewSamplesAreClosedBoundedAndWithinBudget()
    {
        var budgets = Street.GetTypes().Where(t => t.GetCustomAttribute<PartAttribute>() is not null)
            .ToDictionary(t => t.GetCustomAttribute<PartAttribute>()!.Id, t => (PartBudget)t.GetProperty("Budget")!.GetValue(Activator.CreateInstance(t))!);
        var samples = StreetGallery.CreateSamples();
        Check(samples.Select(s => s.Part.Value).Distinct().Count() == Parts.Length, "The gallery misses a part.");
        foreach (var sample in samples)
            foreach (var level in Enum.GetValues<LodLevel>())
            {
                if (sample.Build(level) is not { } geometry) continue;
                ClosedClockwise(geometry, $"{sample.Id} {level}");
                Check(geometry.Bounds == sample.Bounds, $"{sample.Id} {level} moved the bounds.");
                var triangles = geometry.Meshes.Sum(m => m.Index.Length / 3);
                var budget = budgets[sample.Part.Value];
                Check(triangles <= (level == LodLevel.L0 ? budget.L0 : budget.L1), $"{sample.Id} {level} has {triangles} triangles, over budget.");
                // The contract's AO is visibility; street parts have no reveals, so nothing is darkened.
                Check(geometry.Meshes.All(m => m.Ao is null), $"{sample.Id} {level} writes occlusion it does not have.");
            }
    }

    [TestCase]
    public void GenerationIsBitwiseRepeatable()
    {
        var first = StreetGallery.CreateExamples(); var second = StreetGallery.CreateExamples();
        Check(first.Count == second.Count && first.Count >= Parts.Length, "Missing gallery examples.");
        for (var i = 0; i < first.Count; i++)
        {
            Check(first[i].Geometry.Meshes.Count == second[i].Geometry.Meshes.Count, "Surface count changed.");
            for (var m = 0; m < first[i].Geometry.Meshes.Count; m++)
            {
                var a = first[i].Geometry.Meshes[m]; var b = second[i].Geometry.Meshes[m];
                Check(a.Material == b.Material, "Material order changed.");
                Check(a.Position.Select(BitConverter.SingleToInt32Bits).SequenceEqual(b.Position.Select(BitConverter.SingleToInt32Bits)), "Vertex bytes changed.");
                Check(a.Normal.Select(BitConverter.SingleToInt32Bits).SequenceEqual(b.Normal.Select(BitConverter.SingleToInt32Bits)), "Normal bytes changed.");
                Check(a.Index.SequenceEqual(b.Index), "Index bytes changed.");
            }
        }
    }

    [TestCase]
    public void CollidersFillFootStayCloseAndFollowTheWalkTable()
    {
        foreach (var sample in StreetGallery.CreateSamples())
        {
            var type = Street.GetTypes().Single(t => t.GetCustomAttribute<PartAttribute>()?.Id == sample.Part.Value);
            var colliders = (IReadOnlyList<Collider>)Contract(type).GetMethod("Collision")!.Invoke(Activator.CreateInstance(type), [sample.Params])!;
            // Parked bicycles never collide (WLD-LIFE §5: they come and go, a collider would leave an invisible wall).
            var visualOnly = sample.Id is "entrance-mat" or "entrance-lamp" or "open-sign-wall" or "bicycle";
            Check(visualOnly ? colliders.Count == 0 : colliders.Count > 0, $"{sample.Id} has the wrong collision class (MAP-QSH-ADDITIONS §7).");
            foreach (var collider in colliders)
            {
                Check(collider.Foot != FootMat.Auto, $"{sample.Id} leaves a collider on Auto (G21).");
                // The kerb ramp is walked on: ground, not a prop.
                Check(collider.Tag == (sample.Id == "kerb-ramp" ? SurfaceTag.Ground : SurfaceTag.Prop), $"{sample.Id} collider has the wrong surface tag.");
                var (min, max) = Extent(collider);
                var b = sample.Bounds;
                // SYS-WALK §7: no more than 0.3 m beyond the visible object; posts may reach below grade.
                Check(min.X >= b.Min.X - 0.3f && min.Z >= b.Min.Z - 0.3f && max.X <= b.Max.X + 0.3f && max.Z <= b.Max.Z + 0.3f && max.Y <= b.Max.Y + 0.3f,
                    $"{sample.Id} collider reaches more than 0.3 m past the part.");
            }
        }
        IPartDef<FurnitureParams> sphere = new StoneSpherePart();
        Check(sphere.Collision(new()).Single().Foot == FootMat.StonePaving, "Stone spheres step as stone paving (WLD-LIFE §5).");
        IPartDef<FurnitureParams> bench = new BenchPart();
        Check(bench.Collision(new()).All(c => c.Foot == FootMat.WoodFloor), "Bench colliders step as wood (WLD-LIFE §5).");
        Check(bench.Collision(new() { Foot = FootMat.Metal }).All(c => c.Foot == FootMat.Metal), "A caller's Foot overrides the default.");
    }

    [TestCase]
    public void BenchSeatsComeFromTheSeatCollider()
    {
        // Read through the contract interface only: a shadow seat type would leave IPartDef on its default empty list (contract-33).
        IPartDef<FurnitureParams> def = new BenchPart();
        var backed = new FurnitureParams(1.8f, 0.8f, 0.62f);
        var seat = (BoxCollider)def.Collision(backed)[0];
        var seats = def.Seats(backed);
        Check(seats.Count == 1 && seats[0].Back && seats[0].Places == 2, "A 1.8 m backed bench declares one front edge with two places.");
        var top = seat.At.M42 + seat.Half.Y;
        Check(Math.Abs(top - StreetStyle.SeatHeight) < 1e-5f && Math.Abs(seats[0].From.Y - top) < 1e-5f && Math.Abs(seats[0].To.Y - top) < 1e-5f,
            "The seat edge lies on the seat collider top at 0.45 m.");
        Check(Math.Abs(seats[0].From.Z - (seat.At.M43 - seat.Half.Z)) < 1e-5f && seats[0].Facing == -Vector3.UnitZ, "The seat edge is the collider's front edge, facing −Z.");
        Check(seats[0].From.X < seats[0].To.X, "From is on the sitter's left.");
        Check(Math.Abs(seats[0].Depth - 2 * seat.Half.Z) < 1e-5f, "Seat depth is the collider depth.");
        var backless = def.Seats(new FurnitureParams(1.6f, 0.45f, 0.5f) { Variation = 1 });
        Check(backless.Count == 2 && backless.All(s => !s.Back) && backless[0].Facing == -backless[1].Facing, "A backless bench seats both sides.");
        foreach (var s in backless)
            Check(Seating.Positions(s).All(p => Math.Abs(p.Y - StreetStyle.SeatHeight) < 1e-5f), "Sitting positions lie on the seat.");
        // Turned boxes (a placed tier, a rotated bench) keep the edge on the box's own front.
        var turned = new BoxCollider(Matrix4x4.CreateTranslation(0, 0.2f, 0) * DetMath.RotationY(float.Pi * 0.5f) * Matrix4x4.CreateTranslation(3, 0, 1), new(1f, 0.2f, 0.25f), SurfaceTag.Prop);
        var edge = Seating.FromBox(turned, SeatSide.Front).Single();
        Check(Math.Abs(edge.Facing.X - -1f) < 1e-5f && Math.Abs((edge.From.Y + edge.To.Y) * 0.5f - 0.4f) < 1e-5f, "A box turned a quarter faces −X.");
        Check(Seating.FromBox(new BoxCollider(Matrix4x4.CreateTranslation(0, 0.6f, 0), new(1, 0.6f, 0.3f), SurfaceTag.Prop), SeatSide.Front).Count == 0, "A 1.2 m top is not a seat.");
        // Every street part reaches its seats through the contract: only the bench declares any, and nothing returns another seat type.
        foreach (var sample in StreetGallery.CreateSamples())
        {
            var type = Street.GetTypes().Single(t => t.GetCustomAttribute<PartAttribute>()?.Id == sample.Part.Value);
            var declared = (IReadOnlyList<SeatDecl>)Contract(type).GetMethod("Seats")!.Invoke(Activator.CreateInstance(type), [sample.Params])!;
            Check(sample.Part.Value == PartCatalog.Bench ? declared.Count > 0 : declared.Count == 0, $"{sample.Id} declares {declared.Count} seats through IPartDef.");
        }
        Check(!Street.GetTypes().Any(t => t.Name == nameof(SeatDecl)), "The street assembly still defines its own seat record.");
    }

    [TestCase]
    public void LampMarkersUseScheduledChannelsAndSitUnderTheLens()
    {
        foreach (var sample in StreetGallery.CreateSamples())
        {
            var at = Matrix4x4.CreateTranslation(10, 1, -4);
            foreach (var emitter in sample.Emitters(at))
            {
                Check(LightChannelCatalog.Slot(emitter.Channel) is >= 12 and <= 27, $"{sample.Id} marker is not on a lamp channel.");
                Check(emitter.Color.Value == "lamp.warm", $"{sample.Id} marker is not warm (MAP-QSH-ADDITIONS §8.2).");
                Check(emitter.Energy is 1f or 0.75f or 0.5f && emitter.Range > 0, $"{sample.Id} marker is off the three grades.");
                var local = emitter.Position - new Vector3(10, 1, -4);
                // Markers sit just under their lens: inside the part's height, or a few centimetres under a wall lantern.
                Check(local.Y > sample.Bounds.Min.Y - 0.1f && local.Y < sample.Bounds.Max.Y, $"{sample.Id} marker is outside the lamp's height.");
            }
            // Every self-lit lens rides a lamp or signal channel.
            foreach (var mesh in sample.Build(LodLevel.L0)!.Meshes)
                if (mesh.Material.Spec.Emissive is { } channel)
                    Check(LightChannelCatalog.Slot(channel) is >= 12 and <= 27, $"{sample.Id} lens has no lamp channel.");
        }
        var disc = new LampParams(0.7f, 6f, 0.7f);
        Check(StreetLampPart.Emitter(disc, Matrix4x4.Identity).Channel.Value == "lamp.first", "The disc lamp is in the first-lamp group by default.");
        Check(StreetLampPart.Emitter(disc with { Variation = 2 }, Matrix4x4.Identity).Channel.Value == "lamp.street", "Variation 2 moves the disc lamp to the street group.");
        Check(StreetLampPart.Emitter(disc, Matrix4x4.Identity, LampGrade.Three).Energy == 0.5f, "Grade three halves the energy.");
        var arm = new LampParams(1.7f, 7f, 0.5f) { Variation = 1 };
        var marker = StreetLampPart.Emitter(arm, Matrix4x4.Identity);
        Check(marker.Position.Z < -1f && marker.Channel.Value == "lamp.street", "The single-arm marker hangs under the head, out over the road.");
        var door = EntranceLampPart.Emitter(new LampParams(0.25f, 0.25f, 0.15f), Matrix4x4.Identity);
        Check(door.Channel.Value == "lamp.first" && door.Energy == StreetStyle.GradeTwoEnergy && door.Position.Z < -0.075f,
            "The door lamp marker is a first-lamp-group fixture at grade-two energy and sits in front.");
    }

    [TestCase]
    public void SizesAndColoursFollowTheStyleRules()
    {
        // Every surface colour is a registered Lit/Shade pair name, never a literal.
        var palette = Path.Combine(RepositoryRoot(), "game", "style", "palette");
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in new[] { Path.Combine(palette, "core.json") }.Concat(Directory.GetFiles(Path.Combine(palette, "ext"), "*.json")))
        {
            using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllBytes(file));
            if (!document.RootElement.TryGetProperty("entries", out var entries)) continue;
            foreach (var entry in entries.EnumerateArray())
                if (entry.TryGetProperty("token", out var token)) tokens.Add(token.GetString()!);
        }
        Check(tokens.Contains(StreetStyle.LampWarm.Value), "The lamp colour is registered.");
        foreach (var sample in StreetGallery.CreateSamples())
            foreach (var level in Enum.GetValues<LodLevel>())
            {
                if (sample.Build(level) is not { } geometry) continue;
                foreach (var mesh in geometry.Meshes)
                {
                    var lit = mesh.Material.Spec.Lit.Value; var shade = mesh.Material.Spec.Shade.Value;
                    Check(lit.EndsWith(".lit", StringComparison.Ordinal) && shade == lit[..^4] + ".shade", $"{sample.Id} uses an unpaired colour {lit}.");
                    Check(tokens.Contains(lit) && tokens.Contains(shade), $"{sample.Id} uses unregistered colour {lit}.");
                }
            }
        // Proportions of the atlas entities (SYS-ATLAS §6); the seat anchor is checked against the collider above.
        var post = new AtlasPostPart().Bounds(new SignParams(0.3f, 1.05f, 0.3f));
        Check(Math.Abs(post.Max.Y - 1.05f) < 0.01f, "The atlas post is 1.05 m tall.");
        var kiosk = new AtlasKioskPart().Bounds(new SignParams(2f, 2.6f, 0.8f));
        Check(Math.Abs(kiosk.Size.X - 2f) < 0.01f && Math.Abs(kiosk.Max.Y - 2.6f) < 0.01f, "The atlas kiosk is 2.0 m wide and 2.6 m tall.");
        // A caller's material colours stone spheres and bollards (the south gate passes its granite and marking paint).
        var granite = new ToonSpec(new("stone.beige.lit"), new("stone.beige.shade"));
        var sphere = new StoneSpherePart().Lods[LodLevel.L0](new FurnitureParams(0.6f, 0.6f, 0.6f) { Material = granite });
        Check(sphere.Meshes.All(m => m.Material.Spec.Lit.Value == "stone.beige.lit"), "A caller's stone colours the sphere.");
        Check(Lettering.Covers(StreetStyle.StopTitle) && Lettering.Covers(StreetStyle.OpenTitle), "Fixed labels have stroke glyphs.");
        Check(StreetStyle.BinDoors.Length == 4 && StreetStyle.BinDoors.Distinct().Count() == 4, "Four distinct sorting colours.");
        Check(StreetStyle.BinDoors[2] == "equip.fireRed" && StreetStyle.BinDoors[1] == StreetStyle.SignBlue, "Bin red is the campus red; bin and stop share the street blue (Opus review 10:07).");
        // Review caps (Opus 10:07): round bollards ≤ 120 triangles near, the shuttle stop ≤ 400.
        Check(new BollardPart().Budget.L0 <= 120 && new BusStopPart().Budget.L0 <= 400, "Bollard or stop budget above the review cap.");
    }

    [TestCase]
    public void TheParkedCarStandsOnBigWheelsUnderArchesWithNoSkirt()
    {
        // Opus review 13c2b 21:05: 0.65 m wheels showing under arches, the body's underside 0.15 m up with no dark skirt below it,
        // about 1200 triangles near. Judged from the geometry of both body shapes.
        var part = new ParkedCarPart();
        foreach (var (p, shape) in new[] { (new VehicleParams(1.8f, 1.47f, 4.5f), StreetStyle.Saloon), (new VehicleParams(1.72f, 1.52f, 4f) { Variation = 1 }, StreetStyle.Hatch) })
        {
            var near = part.Lods[LodLevel.L0](p);
            var counts = part.Lods.ToDictionary(l => l.Key, l => l.Value(p).Meshes.Sum(m => m.Index.Length / 3));
            Console.WriteLine($"car variation {p.Variation}: L0 {counts[LodLevel.L0]}, L1 {counts[LodLevel.L1]}, L2 {counts[LodLevel.L2]} triangles");
            Check(counts[LodLevel.L0] is > 900 and <= 1200 && counts[LodLevel.L1] <= 360 && counts[LodLevel.L2] < counts[LodLevel.L1], "car triangles off the review's 1200 and their proportion");
            var body = near.Meshes.Where(m => m.Material.Spec.Lit.Value == StreetStyle.CarBodies[0] + ".lit" && m.Material.Spec.Fx.HasFlag(ToonFx.BevelLift)).ToList();
            var tyres = near.Meshes.Where(m => m.Material.Spec.Lit.Value == StreetStyle.Tyre + ".lit" && m.Material.Spec.Normal == ToonNormal.Smooth).ToList();
            Check(body.Count == 1 && tyres.Count == 1, "the car's body or tyres are not one surface each");
            var bodyPoints = Enumerable.Range(0, body[0].Position.Length / 3).Select(i => P(body[0], i)).ToList();
            var tyrePoints = Enumerable.Range(0, tyres[0].Position.Length / 3).Select(i => P(tyres[0], i)).ToList();
            Check(bodyPoints.Min(q => q.Y) >= StreetStyle.CarClearance - 1e-4f, $"the body hangs to {bodyPoints.Min(q => q.Y):F3} m");
            var diameter = tyrePoints.Max(q => q.Y) - tyrePoints.Min(q => q.Y);
            Check(MathF.Abs(diameter - 2f * StreetStyle.WheelRadius) < 0.01f && tyrePoints.Min(q => q.Y) > -1e-3f, $"wheels {diameter:F3} m across");
            // Over each axle the body rises in an arch, so the wheel shows whole from the side.
            foreach (var axle in new[] { -p.Depth * 0.5f + shape.FrontAxle * p.Depth, p.Depth * 0.5f - shape.RearAxle * p.Depth })
            {
                var over = bodyPoints.Where(q => MathF.Abs(q.Z - axle) < 0.25f).Min(q => q.Y);
                Check(over >= StreetStyle.WheelRadius + 0.2f, $"the body comes down to {over:F2} m over the axle at {axle:F2}");
            }
        }
    }

    [TestCase]
    public void StreetLifePartsDeclareTheirSoundsSocketsAndSizes()
    {
        // WLD-LIFE §4.12: of the street parts only the vending machine hums, within 4 m; every other part is silent.
        foreach (var sample in StreetGallery.CreateSamples())
        {
            var type = Street.GetTypes().Single(t => t.GetCustomAttribute<PartAttribute>()?.Id == sample.Part.Value);
            var sounds = (IReadOnlyList<SoundSocketDecl>)Contract(type).GetMethod("Sounds")!.Invoke(Activator.CreateInstance(type), [sample.Params])!;
            if (sample.Part.Value != PartCatalog.VendingMachine) { Check(sounds.Count == 0, $"{sample.Id} makes a sound."); continue; }
            Check(sounds.Count == 1 && sounds[0].AudibleRadius <= 4f && sounds[0].Set.Value.Length > 0, $"{sample.Id}: the compressor socket is missing or carries too far.");
            Check(sample.Bounds.Contains(new(sounds[0].Local.M41, sounds[0].Local.M42, sounds[0].Local.M43)), $"{sample.Id}: the compressor is outside the machine.");
        }
        // The shared bicycle's contact points (WP-54b's riding clips): seat above the cranks, grips left (−X) and right, axles on the wheels.
        IPartDef<VehicleParams> bike = new ParkedBicyclePart();
        var sockets = bike.Sockets;
        Check(new[] { "seat", "grip.left", "grip.right", "pedal.left", "pedal.right", "axle.front", "axle.rear" }.All(sockets.ContainsKey), "The bicycle lacks a riding socket.");
        Check(sockets["grip.left"].M41 < 0 && sockets["grip.right"].M41 > 0 && sockets["axle.front"].M43 < sockets["axle.rear"].M43, "The bicycle's sockets face the wrong way (front is −Z, a rider's left is −X).");
        Check(sockets["seat"].M42 > sockets["pedal.left"].M42 + 0.4f, "The saddle is not above the pedals.");
        // Sizes from MAP-QSH-ADDITIONS §2.13: the fountain's top at 1.1 m on 0.6 × 0.45, the signpost 2.6 m with 3–5 boards.
        var fountain = new DrinkingFountainPart().Bounds(new EquipmentParams(0.6f, 1.1f, 0.45f));
        Check(Math.Abs(fountain.Max.Y - 1.1f) < 0.01f, "The fountain's top is not at 1.1 m.");
        Check(Lettering.Covers(StreetStyle.FountainTitle), "The fountain's plate has no glyphs.");
        foreach (var boards in new[] { 0, 1, 2 })
        {
            var sign = new SignpostPart();
            var geometry = sign.Lods[LodLevel.L0](new SignParams(1.2f, 2.6f, 0.12f) { Variation = boards });
            ClosedClockwise(geometry, $"signpost {3 + boards} boards");
            Check(geometry.Meshes.Sum(m => m.Index.Length / 3) <= sign.Budget.L0, $"signpost {3 + boards} boards is over its budget.");
        }
        // Body colours stay low-saturation and registered; a caller's Material picks one.
        var car = new ParkedCarPart().Lods[LodLevel.L0](new VehicleParams(1.8f, 1.47f, 4.5f) { Material = new(new("umbrella.beige.lit"), new("umbrella.beige.shade")) });
        Check(car.Meshes.Any(m => m.Material.Spec.Lit.Value == "umbrella.beige.lit"), "A caller's colour does not paint the car body.");
        Check(StreetStyle.CarBodies.All(c => !c.Contains("fireRed", StringComparison.Ordinal)), "A car body uses a saturated colour.");
    }

    private static Type Contract(Type type) => type.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPartDef<>));

    private static (Vector3 Min, Vector3 Max) Extent(Collider collider) => collider switch
    {
        PostCollider p => (new(p.C.X - p.R, p.Y0, p.C.Y - p.R), new(p.C.X + p.R, p.Y1, p.C.Y + p.R)),
        BoxCollider b => (new(b.At.M41 - b.Half.X, b.At.M42 - b.Half.Y, b.At.M43 - b.Half.Z), new(b.At.M41 + b.Half.X, b.At.M42 + b.Half.Y, b.At.M43 + b.Half.Z)),
        RampCollider r => (Vector3.Min(Vector3.Min(r.P0, r.P1), Vector3.Min(r.P2, r.P3)), Vector3.Max(Vector3.Max(r.P0, r.P1), Vector3.Max(r.P2, r.P3))),
        _ => throw new InvalidOperationException($"Unexpected collider {collider.GetType().Name}."),
    };

    private static string RepositoryRoot()
    {
        if (Environment.GetEnvironmentVariable("UESTC_REPOSITORY") is { Length: > 0 } root) return root;
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "game", "style", "palette", "core.json"))) return directory.FullName;
        throw new InvalidOperationException("Repository root not found.");
    }

    private static Vector3 P(GeometryBundle m, int i) => new(m.Position[3 * i], m.Position[3 * i + 1], m.Position[3 * i + 2]);
    private static Vector3 N(GeometryBundle m, int i) => new(m.Normal[3 * i], m.Normal[3 * i + 1], m.Normal[3 * i + 2]);
    private static void ClosedClockwise(PartGeometry geometry, string what)
    {
        Check(geometry.Meshes.Count > 0, $"{what} is empty.");
        foreach (var m in geometry.Meshes)
        {
            m.ValidateVertexData();
            var edges = new Dictionary<(Vector3, Vector3), int>();
            for (var i = 0; i < m.Index.Length; i += 3)
            {
                var a = m.Index[i]; var b = m.Index[i + 1]; var c = m.Index[i + 2];
                var cross = Vector3.Cross(P(m, b) - P(m, a), P(m, c) - P(m, a));
                Check(Vector3.Dot(cross, N(m, a) + N(m, b) + N(m, c)) < 0, $"{what}: face winding disagrees with outward normals.");
                foreach (var (from, to) in new[] { (P(m, a), P(m, b)), (P(m, b), P(m, c)), (P(m, c), P(m, a)) })
                { edges.TryGetValue((from, to), out var count); edges[(from, to)] = count + 1; }
            }
            // Printed face geometry (lettering, pictograms; line class None) is flat quads by design (ART-MODELING §7.5); every
            // other surface is a set of closed solids.
            if (m.Material.Spec.Line != LineClass.None)
                foreach (var (edge, count) in edges)
                    Check(count == 1 && edges.TryGetValue((edge.Item2, edge.Item1), out var reverse) && reverse == 1, $"{what}: a solid has an open or non-manifold edge.");
            else Check(m.Flags is not null && m.Flags.All(f => (f & VertexFlag.NoShadow) != 0), $"{what}: printed face geometry casts shadows.");
            for (var i = 0; i < m.Position.Length / 3; i++)
            {
                Check(geometry.Bounds.Contains(P(m, i)), $"{what}: bounds miss vertex {P(m, i)} ({geometry.Bounds.Min} – {geometry.Bounds.Max}).");
                Check(Math.Abs(1 - N(m, i).Length()) <= 1e-5, $"{what}: a normal is not unit length.");
            }
        }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
