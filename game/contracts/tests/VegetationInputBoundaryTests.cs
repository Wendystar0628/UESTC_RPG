using System;
using System.Collections.Generic;
using System.Numerics;
using Campus.Contracts.Maps;
using GdUnit4;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class VegetationInputBoundaryTests
{
    [TestCase]
    public void SnapshotFreezesTheRequestedByteSliceAndLoaderMapping()
    {
        byte[] loaderBuffer = [11, 22, 33, 44, 55];
        var inputs = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal)
        {
            ["vegetation/species.json"] = loaderBuffer.AsMemory(1, 3)
        };
        var snapshot = BuildInputSnapshot.Freeze(inputs);
        Array.Fill(loaderBuffer, (byte)0);
        inputs["vegetation/species.json"] = new byte[] { 99 };
        inputs["vegetation/planting.json"] = new byte[] { 88 };
        inputs.Clear();

        Require(snapshot.Entries.Count == 1 && snapshot.Require("vegetation/species.json").Span.SequenceEqual(new byte[] { 22, 33, 44 }),
            "Snapshot bytes must match the requested slice after the loader reuses its array and mapping.");
        Expect<KeyNotFoundException>(() => snapshot.Require("vegetation/planting.json"));
    }

    [TestCase]
    public void InputPathsRejectTraversalAndDoNotNormalizeMissingKeys()
    {
        var snapshot = BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal)
        {
            ["vegetation/species.json"] = new byte[] { 1 }
        });
        foreach (var key in new[]
        {
            "", " ", " vegetation/species.json", "vegetation/species.json ",
            "/vegetation/species.json", "vegetation\\species.json", "res://data/vegetation/species.json",
            "C:/data/species.json", "vegetation/../species.json", "./vegetation/species.json",
            "vegetation//species.json", "vegetation/species.json/"
        })
        {
            var invalid = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal) { [key] = new byte[] { 1 } };
            Expect<ArgumentException>(() => BuildInputSnapshot.Freeze(invalid));
            Expect<ArgumentException>(() => snapshot.Require(key));
        }
        Expect<KeyNotFoundException>(() => snapshot.Require("Vegetation/species.json"));
        Expect<KeyNotFoundException>(() => snapshot.Require("vegetation/missing.json"));
    }

    [TestCase]
    public void MaskGridRejectsInvalidExtentLengthAndOverflowBeforePainting()
    {
        var grid = new GroundMaskGrid(new MapRect(new(-64, -32), new(64, 32)), 64, 32, 2);
        grid.Validate(8192, 8192);
        foreach (var (mask1, mask2) in new[] { (8188, 8192), (8192, 8196), (-1, 8192), (8192, -1) })
            Expect<ArgumentException>(() => grid.Validate(mask1, mask2));
        foreach (var invalid in new[]
        {
            grid with { Width = 0 }, grid with { Height = -1 }, grid with { Width = 63 },
            grid with { MetersPerPixel = 0 }, grid with { MetersPerPixel = -2 },
            grid with { MetersPerPixel = float.NaN }, grid with { MetersPerPixel = float.PositiveInfinity },
            grid with { Rect = new MapRect(new(64, -32), new(-64, 32)) },
            grid with { Rect = new MapRect(new(-64, -32), new(64, -32)) },
            grid with { Rect = new MapRect(new(float.NaN, -32), new(64, 32)) },
            grid with { Rect = new MapRect(new(-64, -32), new(64, float.PositiveInfinity)) }
        }) Expect<ArgumentException>(() => invalid.Validate(8192, 8192));

        // Existing WP-15 and WP-23 round the extent/mpp; non-integral extents are not a new rejection rule.
        var rounded = new GroundMaskGrid(new MapRect(new(-2, -3), new(.49f, -.51f)), 2, 2, 1);
        rounded.Validate(16, 16);
        // Without checked arithmetic, 32768*32768*4 wraps to zero and empty buffers could be accepted.
        var oversized = new GroundMaskGrid(new MapRect(Vector2.Zero, new(32768, 32768)), 32768, 32768, 1);
        Expect<OverflowException>(() => oversized.Validate(0, 0));
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Expect<TException>(Action action) where TException : Exception
    {
        try { action(); }
        catch (TException) { return; }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }
}
