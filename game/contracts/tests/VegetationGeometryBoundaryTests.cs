using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class VegetationGeometryBoundaryTests
{
    [TestCase]
    public void AbsentVertexChannelsRemainAbsentAfterFreezing()
    {
        var source = Surface([]) with
        {
            Ao = null,
            Uv = null,
            Uv2 = null,
            ColorIndices = null,
            DecalBaseColorIndices = null,
            Flags = null,
            Reserved = null,
            Custom0 = null,
            Custom1 = null,
            Custom2 = null
        };
        var frozen = GeometryBuffer.Freeze(source);
        AssertThat(frozen.Ao is null && frozen.Uv is null && frozen.Uv2 is null &&
            frozen.ColorIndices is null && frozen.DecalBaseColorIndices is null &&
            frozen.Flags is null && frozen.Reserved is null && frozen.Custom0 is null &&
            frozen.Custom1 is null && frozen.Custom2 is null).OverrideFailureMessage("Absent arrays must not become present empty ReadOnlyMemory through implicit array conversion.").IsTrue();
    }

    [TestCase]
    public void FreezeSurvivesReusedVertexArraysAndReplacedLodList()
    {
        int[] lodIndices = [2, 1, 0];
        var lods = new List<MeshLod> { new(.5f, lodIndices) };
        var source = Surface(lods);
        var frozen = GeometryBuffer.Freeze(source);
        var surfaces = new List<GeometryBuffer> { frozen };
        var prototype = new GeometryPrototype("fixture", surfaces, LocalBounds);

        Array.Fill(source.Position, 99f);
        Array.Fill(source.Normal, 99f);
        Array.Fill(source.Index, 99);
        Array.Fill(source.Ao!, 1f);
        Array.Fill(source.Uv!, 99f);
        Array.Fill(source.Uv2!, 99f);
        Array.Fill(source.ColorIndices!, (ushort)99);
        Array.Fill(source.Flags!, (byte)8);
        Array.Fill(source.Reserved!, (byte)99);
        Array.Fill(source.Custom0!, 99f);
        Array.Fill(source.Custom1!, 99f);
        Array.Fill(source.Custom2!, 99f);
        Array.Fill(lodIndices, 99);
        lods[0] = new MeshLod(7f, [0, 0, 0]);
        lods.Clear();
        surfaces.Clear();

        AssertThat(frozen.Position.Span.SequenceEqual(new float[] { 0, 0, 0, 1, 0, 0, 0, 1, 0 }) &&
            frozen.Normal.Span[2] == 1 && frozen.Index.Span.SequenceEqual(new[] { 0, 1, 2 })).OverrideFailureMessage("Reusing generation arrays changed the frozen surface.").IsTrue();
        AssertThat(frozen.Ao!.Value.Span[0] == .25f && frozen.Uv!.Value.Span[2] == 1 &&
            frozen.Uv2!.Value.Span[0] == 65537 && frozen.ColorIndices!.Value.Span[0] == 2 &&
            frozen.Flags!.Value.Span[0] == 0 && frozen.Reserved!.Value.Span[0] == 3 &&
            frozen.Custom0!.Value.Span[0] == 10 && frozen.Custom1!.Value.Span[0] == 20 &&
            frozen.Custom2!.Value.Span[0] == 30).OverrideFailureMessage("A frozen vertex channel still aliases mutable source storage.").IsTrue();
        AssertThat(frozen.Lods.Count == 1 && frozen.Lods[0].Error == .5f &&
            frozen.Lods[0].Index.Span.SequenceEqual(new[] { 2, 1, 0 })).OverrideFailureMessage("The LOD list or index storage was retained by reference.").IsTrue();
        AssertThat(prototype.Surfaces.Count == 1 && ReferenceEquals(prototype.Surfaces[0], frozen)).OverrideFailureMessage("The prototype must retain the immutable surface while isolating the mutable surface list.").IsTrue();
    }

    [TestCase]
    public void FreezeRejectsLodIndicesOutsideTheOwningSurface()
    {
        // Three vertices: 2 is valid; 3 is already outside the source surface.
        GeometryBuffer.Freeze(Surface([new MeshLod(.5f, [0, 1, 2])]));
        foreach (int[] indices in new int[][] { [0, 1, 3], [0, -1, 2], [0, 1] })
            AssertThrown(() => GeometryBuffer.Freeze(Surface([new MeshLod(.5f, indices)]))).IsInstanceOf<ArgumentException>();
        foreach (float error in new[] { -1f, float.NaN, float.PositiveInfinity })
            AssertThrown(() => GeometryBuffer.Freeze(Surface([new MeshLod(error, [0, 1, 2])]))).IsInstanceOf<ArgumentException>();
    }

    [TestCase]
    public void InstanceRecordPreservesNegativeWorldTransformCustomAndScratchIsolation()
    {
        // Hand-specified 90-degree turn with unequal scales. No trigonometric constructor is involved.
        var at = new Matrix4x4(0, 2, 0, 0, -3, 0, 0, 0, 0, 0, 4, 0, -130, -2.5f, -65, 1);
        var scratch = new float[16];
        InstanceStreamLayout.Write(scratch, at, new(.125f, 7, -2, .75f));
        float[] expected = [0, -3, 0, -130, 2, 0, 0, -2.5f, 0, 0, 4, -65, .125f, 7, -2, .75f];
        AssertThat(scratch.SequenceEqual(expected)).OverrideFailureMessage("Transform rows or the four custom values changed order.").IsTrue();

        // An independent consumer applies the packed rows to local (1,2,-1).
        var transformed = new Vector3(scratch[0] + 2 * scratch[1] - scratch[2] + scratch[3],
            scratch[4] + 2 * scratch[5] - scratch[6] + scratch[7],
            scratch[8] + 2 * scratch[9] - scratch[10] + scratch[11]);
        AssertThat(transformed == new Vector3(-136, -.5f, -69)).OverrideFailureMessage("Negative world translation must be applied exactly once.").IsTrue();

        var prototype = Prototype();
        var stream = new GeometryInstanceStream("fixture-stream", prototype, scratch, WorldBounds);
        Array.Fill(scratch, 99f);
        AssertThat(stream.Count == 1 && stream.Data.Span.SequenceEqual(expected) && ReferenceEquals(stream.Prototype, prototype)).OverrideFailureMessage("A static stream must freeze upload scratch while sharing its immutable prototype.").IsTrue();
    }

    [TestCase]
    public void InstanceWriterRejectsInvalidInputBeforeChangingDestination()
    {
        var target = Enumerable.Repeat(123f, 16).ToArray();
        var projective = Matrix4x4.Identity;
        projective.M14 = .25f;
        var invalidBasis = Matrix4x4.Identity;
        invalidBasis.M22 = float.NaN;
        var invalidTranslation = Matrix4x4.Identity;
        invalidTranslation.M41 = float.PositiveInfinity;
        foreach (var invalid in new[] { projective, invalidBasis, invalidTranslation })
        {
            AssertThrown(() => InstanceStreamLayout.Write(target, invalid, Vector4.Zero)).IsInstanceOf<ArgumentException>();
            AssertThat(target.All(value => value == 123f)).OverrideFailureMessage("Rejected matrix input partially changed the destination.").IsTrue();
        }
        AssertThrown(() => InstanceStreamLayout.Write(target, Matrix4x4.Identity, new(0, 0, 0, float.NegativeInfinity))).IsInstanceOf<ArgumentException>();
        AssertThat(target.All(value => value == 123f)).OverrideFailureMessage("Rejected custom data partially changed the destination.").IsTrue();
        foreach (int length in new[] { 15, 17 })
        {
            var incomplete = Enumerable.Repeat(123f, length).ToArray();
            AssertThrown(() => InstanceStreamLayout.Write(incomplete, Matrix4x4.Identity, Vector4.Zero)).IsInstanceOf<ArgumentException>();
            AssertThat(incomplete.All(value => value == 123f)).OverrideFailureMessage("A wrong-sized destination was partially written.").IsTrue();
        }
    }

    [TestCase]
    public void InstanceStreamRejectsIncompleteAndNonFiniteRecords()
    {
        var prototype = Prototype();
        var valid = new float[32];
        InstanceStreamLayout.Write(valid.AsSpan(0, 16), Matrix4x4.Identity, Vector4.Zero);
        InstanceStreamLayout.Write(valid.AsSpan(16, 16), Matrix4x4.Identity, Vector4.One);
        AssertThat(new GeometryInstanceStream("two", prototype, valid, LocalBounds).Count == 2).OverrideFailureMessage("Two complete records must be accepted.").IsTrue();
        foreach (int length in new[] { 1, 15, 17, 31 })
            AssertThrown(() => new GeometryInstanceStream("partial", prototype, new float[length], LocalBounds)).IsInstanceOf<ArgumentException>();
        foreach (var (offset, value) in new[] { (0, float.NaN), (19, float.PositiveInfinity), (31, float.NegativeInfinity) })
        {
            var invalid = valid.ToArray();
            invalid[offset] = value;
            AssertThrown(() => new GeometryInstanceStream("nonfinite", prototype, invalid, LocalBounds)).IsInstanceOf<ArgumentException>();
        }
    }

    private static readonly Box3 LocalBounds = new(Vector3.Zero, Vector3.One);
    private static readonly Box3 WorldBounds = new(new(-136, -3, -69), new(-130, 0, -65));
    private static GeometryPrototype Prototype() => new("fixture", [GeometryBuffer.Freeze(Surface())], LocalBounds);
    private static GeometryBundle Surface(IReadOnlyList<MeshLod>? lods = null) => new(
        new ToonRef(new ToonSpec(new("fixture.lit"), new("fixture.shade"))),
        [0, 0, 0, 1, 0, 0, 0, 1, 0], [0, 0, 1, 0, 0, 1, 0, 0, 1], [0, 1, 2],
        Ao: [.25f, .5f, .75f], Uv: [0, 0, 1, 0, 0, 1], Uv2: [65537, 0, 65537, 1, 65537, 2], Lods: lods)
    {
        ColorIndices = [2, 3, 4],
        Flags = [0, 0, 0],
        Reserved = [3, 4, 5],
        Custom0 = [10, 1, 2, 3, 10, 1, 2, 3, 10, 1, 2, 3],
        Custom1 = [20, 1, 2, 3, 20, 1, 2, 3, 20, 1, 2, 3],
        Custom2 = [30, 1, 2, 3, 30, 1, 2, 3, 30, 1, 2, 3]
    };
}
