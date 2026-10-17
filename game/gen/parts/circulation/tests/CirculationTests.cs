using System.Numerics;
using System.Reflection;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using GdUnit4;
using S = Campus.Gen.Parts.Circulation.CirculationStyle;

namespace Campus.Gen.Parts.Circulation.Tests;

[TestSuite]
public sealed class CirculationTests
{
    [TestCase]
    public void EveryWp11PartIsInTheCatalogWithItsParameterType()
    {
        var parts = Registered();
        Check(parts.Count >= 12, "Missing WP-11 part registrations.");
        foreach (var (type, attribute) in parts)
        {
            var entry = PartCatalog.Find(new(attribute.Id)) ?? throw new InvalidOperationException($"{attribute.Id} is not in PartCatalog.");
            Check(entry.Owner.Value == "WP-11" && attribute.Owner == "WP-11", $"{attribute.Id} has the wrong owner.");
            Check(ParameterType(type) == entry.ParameterType, $"{attribute.Id} implements the wrong parameter type.");
            Check(type.GetConstructor(Type.EmptyTypes) is not null, $"{attribute.Id} needs a public parameterless constructor.");
        }
    }

    [TestCase]
    public void CatalogDefaultsBuildClosedGeometryWithinBudgetAtEveryLevel()
    {
        foreach (var (type, attribute) in Registered())
        {
            var defaults = PartCatalog.Find(new(attribute.Id))!.Defaults;
            var (bounds, levels, colliders, budget) = Invoke(type, defaults);
            for (var level = 0; level < levels.Length; level++)
            {
                ClosedClockwise(levels[level], bounds, attribute.Id);
                var triangles = levels[level].Meshes.Sum(m => m.Index.Length / 3);
                if (level <= 1) Check(triangles <= (level == 0 ? budget.L0 : budget.L1), $"{attribute.Id} L{level} has {triangles} triangles over its budget.");
            }
            Check(levels[0].Meshes.Sum(m => m.Index.Length) >= levels[^1].Meshes.Sum(m => m.Index.Length), $"{attribute.Id} gets heavier when far.");
            Check(colliders.All(c => c.Foot != FootMat.Auto), $"{attribute.Id} leaves a collider's Foot at Auto.");
        }
    }

    [TestCase]
    public void EveryVariationOfEveryGallerySampleIsClosed()
    {
        foreach (var sample in CirculationGallery.Samples())
            foreach (var level in Enum.GetValues<LodLevel>())
                ClosedClockwise(sample.Build(level), sample.Bounds, sample.Id);
        var parts = new (object Part, object[] Params)[]
        {
            (new RailingPart(), new[] { 0, 1, 2 }.Select(v => (object)new RailingParams(2.5f, 1.05f, 0.12f, 2f) { Variation = v }).ToArray()),
            (new StepsPart(), new[] { 0, 1, 2 }.Select(v => (object)new StairParams(2.4f, 1.5f, 3.5f, 10) { Variation = v }).ToArray()),
            (new RampPart(), new[] { 0, 1, 2 }.Select(v => (object)new RampParams(2f, 0.6f, 7.2f) { Variation = v }).ToArray()),
            (new StairStraightPart(), new[] { 0, 1, 2 }.Select(v => (object)new StairParams(1.3f, 1.65f, 11 * 0.28f, 11) { Variation = v }).ToArray()),
            (new StairDoublePart(), new[] { 0, 1 }.Select(v => (object)new StairParams(3.1f, 3.3f, 11 * 0.28f + 1.5f, 11) { Variation = v }).ToArray()),
            (new BalconyPart(), new[] { 0, 1, 2 }.Select(v => (object)new BalconyParams(3.6f, 1.2f, 1.2f) { Variation = v }).ToArray()),
            (new PlanterPart(), new[] { 0, 1, 2 }.Select(v => (object)new FurnitureParams(2.4f, 0.45f, 0.8f) { Variation = v }).ToArray()),
            (new IronGatePart(), new[] { 1, 2 }.Select(l => (object)new DoorParams(1.2f * l, 2.2f, 0.1f, Leaves: l)).ToArray()),
        };
        foreach (var (part, all) in parts)
            foreach (var p in all)
            {
                var (bounds, levels, _, _) = Invoke(part.GetType(), p);
                foreach (var geometry in levels) ClosedClockwise(geometry, bounds, part.GetType().Name);
            }
    }

    [TestCase]
    public void GenerationIsBitwiseRepeatable()
    {
        var first = CirculationGallery.CreateExamples(); var second = CirculationGallery.CreateExamples();
        Check(first.Count == second.Count && first.Count >= 15, "Gallery is incomplete.");
        for (var i = 0; i < first.Count; i++)
        {
            Check(first[i].Geometry.Meshes.Count == second[i].Geometry.Meshes.Count, "Surface count changed.");
            for (var m = 0; m < first[i].Geometry.Meshes.Count; m++)
            {
                var a = first[i].Geometry.Meshes[m]; var b = second[i].Geometry.Meshes[m];
                Check(a.Position.Select(BitConverter.SingleToInt32Bits).SequenceEqual(b.Position.Select(BitConverter.SingleToInt32Bits)), "Vertex bytes changed.");
                Check(a.Normal.Select(BitConverter.SingleToInt32Bits).SequenceEqual(b.Normal.Select(BitConverter.SingleToInt32Bits)), "Normal bytes changed.");
                Check(a.Index.SequenceEqual(b.Index) && a.Material == b.Material, "Index or material changed.");
            }
        }
    }

    [TestCase]
    public void RailingsCannotBeJumped()
    {
        foreach (var height in new[] { 0.9f, 1.05f, 1.2f })
        {
            var wall = (WallCollider)new RailingPart().Collision(new RailingParams(4, height, 0.1f, 2)).Single();
            Check(wall.Tag == SurfaceTag.Barrier && wall.Y1 >= S.BarrierTop && wall.Y0 <= -S.BarrierBelow, "Railing collision is jumpable.");
            Check(wall.HalfT <= 0.3f && wall.Foot == FootMat.Metal, "Railing collision spreads too far or has no foot.");
        }
        var balcony = new BalconyPart().Collision(new BalconyParams(3.6f, 1.2f, 1.2f));
        Check(balcony.OfType<WallCollider>().Count() == 3 && balcony.OfType<WallCollider>().All(w => w.Y1 >= S.BalconySlab + S.BarrierTop), "Balcony guard is jumpable.");
        var gate = (WallCollider)new IronGatePart().Collision(new DoorParams(1.8f, 1.2f, 0.1f)).Single();
        Check(gate.Tag == SurfaceTag.DoorClosed && gate.Y1 >= S.BarrierTop, "Gate seal is too low.");
    }

    [TestCase]
    public void StepsRampThroughEveryNosingFlushAtTopBuriedAtFoot()
    {
        var p = new StairParams(3f, 0.9f, 6 * 0.35f, 6);
        var colliders = new StepsPart().Collision(p);
        var ramp = colliders.OfType<RampCollider>().Single();
        float r = 0.15f, t = 0.35f;
        Near(-S.RampBury, ramp.P0.Y, 1e-5); Near(p.Height, ramp.P2.Y, 1e-5); Near(p.Height, ramp.P3.Y, 1e-5);
        // Nosing k sits at z = −D/2 + (k − 1)·T, y = k·R; the plane through P0..P3 must pass every one of them.
        for (var k = 1; k <= p.Steps; k++)
        {
            var z = -p.Depth * 0.5f + (k - 1) * t;
            var y = ramp.P0.Y + (z - ramp.P0.Z) * (ramp.P3.Y - ramp.P0.Y) / (ramp.P3.Z - ramp.P0.Z);
            Near(k * r, y, 1e-4);
        }
        var angle = Math.Atan2(ramp.P3.Y - ramp.P0.Y, ramp.P3.Z - ramp.P0.Z) * 180 / Math.PI;
        Check(angle is > 19 and < 27, $"Outdoor step ramp pitch {angle}° is outside 19–27°.");
        Check(ramp.Tag == SurfaceTag.Stair && ramp.Stair is { Count: 6 } spec && Math.Abs(spec.RiserM - r) < 1e-5 && Math.Abs(spec.TreadM - t) < 1e-5
            && ramp.Foot == FootMat.StonePaving, "Ramp metadata is incomplete.");
        Check(colliders.OfType<BoxCollider>().Any(b => Math.Abs(b.At.M42 + b.Half.Y - p.Height) < 1e-5), "Top tread has no level support.");
        var stoop = new StepsPart().Collision(new StairParams(2f, 0.3f, 0.7f, 2));
        Check(!stoop.OfType<RampCollider>().Any() && stoop.OfType<BoxCollider>().Count(b => b.Tag == SurfaceTag.Stair) == 2, "Two steps should rely on auto-step boxes.");
    }

    [TestCase]
    public void IndoorStairsMatchTheWp51aColliderRules()
    {
        // WP-51a default plan: 1.45 m flights, 0.2 m well, 1.5 m landing, 0.28 m treads, 3.3 m storey in 2 × 11 risers.
        var p = new StairParams(3.1f, 3.3f, 11 * 0.28f + 1.5f, 11);
        var colliders = new StairDoublePart().Collision(p);
        var ramps = colliders.OfType<RampCollider>().ToArray();
        Check(ramps.Length == 2, "A dog-leg stair has two flight ramps.");
        foreach (var ramp in ramps)
        {
            var run = Math.Abs(ramp.P3.Z - ramp.P0.Z); var rise = ramp.P3.Y - ramp.P0.Y;
            var angle = Math.Atan2(rise, run) * 180 / Math.PI;
            Check(angle is >= 26 and <= 34, $"Flight pitch {angle}° is outside 26–34°.");
            Check(ramp.Stair is { Count: 11 } spec && Math.Abs(spec.RiserM - 0.15f) < 1e-5 && Math.Abs(spec.TreadM - 0.28f) < 1e-5 && ramp.Tag == SurfaceTag.Stair,
                "Flight StairSpec is wrong.");
        }
        Near(-S.RampBury, ramps[0].P0.Y, 1e-5); Near(p.Height * 0.5f, ramps[0].P3.Y, 1e-5);
        Near(p.Height * 0.5f - S.RampBury, ramps[1].P0.Y, 1e-5); Near(p.Height, ramps[1].P3.Y, 1e-5);
        var landing = colliders.OfType<SlabCollider>().Single();
        Near(p.Height * 0.5f, landing.Y, 1e-5);
        // The top edge of flight 1 is the landing's front edge.
        Near(landing.Ring.Min(v => v.Y), ramps[0].P3.Z, 1e-5);
        var well = colliders.OfType<WallCollider>().Single();
        Check(well.Tag == SurfaceTag.Barrier && Math.Abs(well.HalfT - S.WellWidth * 0.5f) < 1e-6 && Math.Abs(well.Y1 - (p.Height + 1.5f)) < 1e-5 && well.Y0 == 0,
            "The well needs a full wall to 1.5 m over the upper floor.");
        var open = new StairOpenPart().Collision(new StairParams(1.6f, 1.8f, 11 * 0.27f, 11));
        var sides = open.OfType<SlopedWallCollider>().ToArray();
        Check(sides.Length == 2 && sides.All(s => s.Y1a >= s.Y0a + 1.5f && s.Y1b >= 1.8f + 1.5f - 1e-4), "Open flights need sloped barriers 1.5 m over the nosings.");
        Check(open.All(c => c.Foot != FootMat.Auto), "Indoor stair colliders need a foot material.");
    }

    [TestCase]
    public void TerrainDropFollowsTheConnectionTable()
    {
        Check(TerrainDrop.Plan(0.03f, 5, true).Method == DropMethod.Absorb, "≤ 4 cm is absorbed.");
        Check(TerrainDrop.Plan(0.1f, 5, true).Method == DropMethod.SlopedCurb, "≤ 15 cm is a sloped curb.");
        Check(TerrainDrop.Plan(5f, 100, true).Method == DropMethod.Terraces, "> 4 m goes to terraces.");
        Check(TerrainDrop.Plan(0.3f, 4, true).Method == DropMethod.Ramp && TerrainDrop.Plan(0.3f, 2, true).Method == DropMethod.Steps, "12 × ΔH decides ramp or steps.");
        for (var rise = 0.16f; rise <= 4f; rise += 0.07f)
        {
            foreach (var main in new[] { true, false })
            {
                var plan = TerrainDrop.Plan(rise, 0.5f, main);
                Check(plan.Method == DropMethod.Steps, $"{rise} m with no room must be steps.");
                Check(plan.ClearWidth >= (main ? S.MainNetworkWidth : S.OtherWidth), "Clear width below the network minimum.");
                Check(plan.Risers >= 2 && Math.Abs(plan.Risers * plan.Riser - rise) < 1e-4, "Risers do not add up.");
                if (rise > 0.35f && rise >= 0.36f) Check(plan.Riser >= S.RiserMin - 1e-4 && plan.Riser <= S.RiserMax + 1e-4, $"Riser {plan.Riser} out of 0.12–0.16.");
                if (rise <= 0.35f) Check(plan.Riser <= S.RiserTarget + 1e-5, "Low steps exceed 0.15 m.");
                Near(Math.Clamp(0.65 - 2 * plan.Riser, 0.30, 0.40), plan.Tread, 1e-5);
                Check(plan.ParallelRampNeeded == rise > S.FlightMaxRise, "Steps over 1.5 m need a second route.");
                float y = 0;
                foreach (var part in plan.Parts)
                {
                    var p = (StairParams)part.Params;
                    Check(p.Height <= S.FlightMaxRise + 1e-4, "A flight rises more than 1.5 m.");
                    Near(y, part.At.M42, 1e-4); y += p.Height;
                    var geometry = new StepsPart().Lods[LodLevel.L0](p);
                    ClosedClockwise(geometry, new StepsPart().Bounds(p), "planned steps");
                }
                Near(rise, y, 1e-4);
                foreach (var landing in plan.Landings)
                    Check(landing.End - landing.Start + plan.Tread >= Math.Max(S.LandingMin, plan.ClearWidth) - 1e-4, "Landing shorter than the rule.");
            }
        }
        for (var rise = 0.16f; rise <= 1.5f; rise += 0.11f)
        {
            var plan = TerrainDrop.Plan(rise, 40, true);
            Check(plan.Method == DropMethod.Ramp && plan.Grade <= S.RampMaxGrade + 1e-6 && plan.Fits, "Ramp plan breaks 1:12 or does not fit.");
            foreach (var run in plan.RampRuns) Check(run.Y1 - run.Y0 <= S.RampMaxSegmentRise + 1e-4, "Ramp run rises more than 0.75 m.");
            foreach (var pad in plan.Landings) Check(pad.End - pad.Start >= S.RampLandingMin - 1e-4, "Ramp pad shorter than 1.5 m.");
            foreach (var part in plan.Parts)
            {
                var p = (RampParams)part.Params;
                ClosedClockwise(new RampPart().Lods[LodLevel.L0](p), new RampPart().Bounds(p), "planned ramp");
                Check(new RampPart().Collision(p).Count == 0, "Terrain ramps leave walking to the height field.");
            }
        }
    }

    [TestCase]
    public void EmbankmentStepsAndRiserCountsFollowTheOutdoorStepRule()
    {
        // 堤上路 (MAP-QSH-TERRAIN) by WLD-TERRAIN: the +1.0 m bank is one flight of 7 × 1/7 m on 0.65 − 2/7 m treads.
        foreach (var plan in new[] { TerrainDrop.PlanSteps(1.0f, mainNetwork: true), TerrainDrop.Plan(1.0f, 3, mainNetwork: true) })
        {
            Check(plan is { Method: DropMethod.Steps, Risers: 7, Fits: true, ParallelRampNeeded: false } && plan.Parts.Count == 1 && plan.Landings.Count == 0,
                "Embankment steps are not a single 7-riser flight.");
            Near(1.0 / 7, plan.Riser, 1e-6); Near(0.65 - 2.0 / 7, plan.Tread, 1e-5);
            var p = (StairParams)plan.Parts[0].Params;
            Check(p.Steps == 7 && Math.Abs(p.Height - 1f) < 1e-5 && Math.Abs(p.Depth - 7 * plan.Tread) < 1e-4, "Embankment flight parameters are wrong.");
        }
        // The paired route (§5.1 两条路): a 1:12 ramp in two runs of ≤ 0.75 m when the bank has the room.
        Check(TerrainDrop.Plan(1.0f, 40, mainNetwork: true) is { Method: DropMethod.Ramp, Fits: true }
            && TerrainDrop.PlanRamp(1.0f, 40, mainNetwork: true).RampRuns.Count == 2, "The embankment's ramp route is wrong.");
        // 4 × 0.25 m risers are outside the stone step range and are refused.
        Throws(() => new StepsPart().Lods[LodLevel.L0](new StairParams(2.4f + 2 * S.CheekWidth, 1f, 4 * 0.3f, 4)));
        Throws(() => StepsAlong.Build([new(0, 0), new(6, 0)], false, 0, 4, 0.25f, 0.3f, StepsFinish.Stone, LodLevel.L0));
        // The riser count never drops as the drop grows; from 0.36 m the riser lies in 0.12–0.16 and is the closest to 0.15.
        var previous = 0;
        for (var i = 16; i <= 400; i++)
        {
            var rise = i / 100f; var n = TerrainDrop.RiserCount(rise);
            Check(n >= previous, $"{rise} m takes {n} risers, fewer than a lower drop's {previous}.");
            previous = n;
            if (rise < 0.36f) continue;
            var r = rise / n;
            Check(n >= 3 && r >= S.RiserMin - 1e-5 && r <= S.RiserMax + 1e-5, $"{rise} m: riser {r} out of 0.12–0.16.");
            for (var m = 3; m <= 40; m++)
                if (rise / m >= S.RiserMin - 1e-5 && rise / m <= S.RiserMax + 1e-5)
                    Check(Math.Abs(rise / m - S.RiserTarget) >= Math.Abs(r - S.RiserTarget) - 1e-5, $"{rise} m: {m} risers come closer to 0.15 than {n}.");
        }
    }

    [TestCase]
    public void TerraceEdgeBandsAreClosedWalkableAndWalledOnTheLowSide()
    {
        Vector2[] line = [new(0, 0), new(8, 0), new(12, 4)];
        foreach (var lod in Enum.GetValues<LodLevel>())
        {
            var geometry = TerraceEdge.Build(line, 0, 0.7f, lod);
            ClosedClockwise(geometry, geometry.Bounds, "terrace edge");
        }
        var colliders = TerraceEdge.Collide(line, 0, 0.7f);
        var walls = colliders.OfType<WallCollider>().ToArray();
        Check(walls.Length == 2 && walls.All(w => w.Y0 <= -TerraceEdge.Below + 1e-5 && Math.Abs(w.Y1 - 0.7f) < 1e-5), "Band front walls are wrong.");
        // A line running +x keeps its high ground toward −z, so the first wall stands on the +z (low) side.
        Check(walls[0].A.Y > 0.9f && walls[0].B.Y > 0.9f, "The wall is not on the low side.");
        Check(colliders.OfType<BoxCollider>().All(b => Math.Abs(b.At.M42 + b.Half.Y - 0.7f) < 1e-4), "Band top is not walkable at the upper level.");
        Throws(() => TerraceEdge.Build(line, 0, 1.6f, LodLevel.L0));
    }

    [TestCase]
    public void PathStepsSeatsAndRampEdgesAreClosedAndClimbTheRightWay()
    {
        // Time-plaza ring (MAP-QSH-TERRAIN): 6 × 0.15 / 0.35 from the −0.9 floor, a closed loop climbing outward.
        var ring = Loop(24.9f, 24.9f, 64);
        foreach (var lod in Enum.GetValues<LodLevel>())
        {
            var g = StepsAlong.Build(ring, true, -0.9f, 6, 0.15f, 0.35f, StepsFinish.Stone, lod);
            ClosedClockwise(g, g.Bounds, "plaza ring");
        }
        var ramps = StepsAlong.Collide(ring, true, -0.9f, 6, 0.15f, 0.35f, StepsFinish.Stone).OfType<RampCollider>().ToArray();
        Check(ramps.Length == 64 && ramps.All(r => r.Foot == FootMat.StonePaving && r.Stair is { Count: 6 }), "Ring needs a stair ramp per segment.");
        static float Radius(Vector3 p) => MathF.Sqrt(p.X * p.X + p.Z * p.Z);
        Check(ramps.All(r => Radius(r.P3) > Radius(r.P0) + 1.5f && Math.Abs(r.P3.Y - 0) < 1e-4 && Math.Abs(r.P0.Y + 1.1f) < 1e-4), "Ring steps must climb outward to 0 m.");
        // Open arc of stone steps, and grass seat tiers straight and round (看台 4 × 0.4 / 1.5, 下沉草坪 3 × 0.3 / 1.3).
        var arc = Loop(12f, 12f, 48).Take(13).ToArray();
        var seatsLine = new[] { new Vector2(0, 0), new Vector2(0, 20), new Vector2(3, 38) };
        foreach (var lod in Enum.GetValues<LodLevel>())
        {
            var a = StepsAlong.Build(arc, false, 0, 4, 0.15f, 0.35f, StepsFinish.Stone, lod);
            ClosedClockwise(a, a.Bounds, "arc steps");
            var s = StepsAlong.Build(seatsLine, false, 0, 4, 0.4f, 1.5f, StepsFinish.GrassSeats, lod);
            ClosedClockwise(s, s.Bounds, "seat tiers");
            var e = StepsAlong.Build(Loop(20f, 30f, 72), true, -0.9f, 3, 0.3f, 1.3f, StepsFinish.GrassSeats, lod);
            ClosedClockwise(e, e.Bounds, "sunken lawn");
        }
        var seats = StepsAlong.Collide(seatsLine, false, 0, 4, 0.4f, 1.5f, StepsFinish.GrassSeats);
        Check(seats.Count == 8 && seats.OfType<BoxCollider>().Max(b => b.At.M42 + b.Half.Y) is var top && Math.Abs(top - 1.6f) < 1e-4, "Seat tiers top out at +1.6.");
        Throws(() => StepsAlong.Build(seatsLine, false, 0, 4, 0.4f, 1.5f, StepsFinish.Stone, LodLevel.L0));
        // Rim ramp of the plaza: 1:12 along the circle with a level pad, deck in the height field.
        var deck = new List<Vector3>();
        for (var i = 0; i <= 20; i++)
        {
            var angle = 0.45 * i / 20; var s = 12.5f * i / 20;
            var y = -Math.Clamp(s < 5.5f ? s / 12 : s < 7 ? 5.5f / 12 : (s - 1.5f) / 12, 0, 0.9f);
            deck.Add(new((float)(25.9 * DetMath.Cos(angle)), y, (float)(25.9 * DetMath.Sin(angle))));
        }
        foreach (var lod in Enum.GetValues<LodLevel>())
        {
            var g = RampEdges.Build(deck, 2.3f, lod);
            ClosedClockwise(g, g.Bounds, "ramp edges");
        }
        var planted = TerraceEdge.Build([new(0, 0), new(10, 0), new(14, 3)], 0, 1.0f, LodLevel.L0, planted: true);
        ClosedClockwise(planted, planted.Bounds, "planted terrace");
        Check(planted.Meshes.Any(m => m.Material.Spec == S.Soil), "A planted band needs a soil bed.");
        var steps = TerrainDrop.PlanSteps(1.5f, mainNetwork: true);
        var ramp = TerrainDrop.PlanRamp(1.5f, 40, mainNetwork: true);
        Check(steps.Method == DropMethod.Steps && steps.Risers == 10 && ramp.Method == DropMethod.Ramp && ramp.RampRuns.Count == 2, "Paired steps and ramp plans are wrong.");
    }

    private static Vector2[] Loop(float rx, float rz, int count) =>
        Enumerable.Range(0, count).Select(i =>
        {
            var a = 2 * Math.PI * i / count;
            return new Vector2((float)(rx * DetMath.Cos(a)), (float)(rz * DetMath.Sin(a)));
        }).ToArray();

    private static void Throws(Action action)
    {
        try { action(); } catch (ArgumentException) { return; }
        throw new InvalidOperationException("Invalid input was accepted.");
    }

    private static List<(Type Type, PartAttribute Attribute)> Registered() =>
        typeof(RailingPart).Assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract)
            .Select(t => (Type: t, Attribute: t.GetCustomAttribute<PartAttribute>()))
            .Where(x => x.Attribute is not null).Select(x => (x.Type, x.Attribute!)).OrderBy(x => x.Item2.Id, StringComparer.Ordinal).ToList();

    private static Type ParameterType(Type part) =>
        part.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPartDef<>)).GetGenericArguments()[0];

    private static (Box3 Bounds, PartGeometry[] Levels, IReadOnlyList<Collider> Colliders, PartBudget Budget) Invoke(Type type, object p)
    {
        var method = typeof(CirculationTests).GetMethod(nameof(InvokeTyped), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(ParameterType(type));
        return ((Box3, PartGeometry[], IReadOnlyList<Collider>, PartBudget))method.Invoke(null, [Activator.CreateInstance(type)!, p])!;
    }

    private static (Box3, PartGeometry[], IReadOnlyList<Collider>, PartBudget) InvokeTyped<T>(IPartDef<T> part, T p) where T : notnull =>
        (part.Bounds(p), Enum.GetValues<LodLevel>().Select(l => part.Lods[l](p)).ToArray(), part.Collision(p), part.Budget);

    private static Vector3 P(GeometryBundle m, int i) => new(m.Position[3 * i], m.Position[3 * i + 1], m.Position[3 * i + 2]);
    private static Vector3 N(GeometryBundle m, int i) => new(m.Normal[3 * i], m.Normal[3 * i + 1], m.Normal[3 * i + 2]);

    private static void ClosedClockwise(PartGeometry geometry, Box3 bounds, string what)
    {
        Check(geometry.Bounds == bounds && geometry.Meshes.Count > 0, $"{what}: geometry bounds differ from the declared bounds.");
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
            foreach (var (edge, count) in edges)
                Check(count == 1 && edges.TryGetValue((edge.Item2, edge.Item1), out var reverse) && reverse == 1, $"{what}: a solid has an open or non-manifold edge at {edge.Item1}–{edge.Item2}.");
            for (var i = 0; i < m.Position.Length / 3; i++)
            { Check(bounds.Contains(P(m, i)), $"{what}: a vertex leaves the bounds."); Near(1, N(m, i).Length(), 1e-5); }
            // Ao is visibility (1 = unoccluded); WP-11 adds no occlusion of its own, so only kernel reveals (0.8) may dip below 1.
            Check(m.Ao is not null && m.Ao.All(v => v >= 0.45f && v <= 1f), $"{what}: Ao must be visibility in 0.45–1 (a double flip would read 0).");
        }
    }

    private static void Near(double expected, double actual, double tolerance) => Check(Math.Abs(expected - actual) <= tolerance, $"Expected {expected}; got {actual}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
