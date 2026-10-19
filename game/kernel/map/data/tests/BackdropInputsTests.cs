using static GdUnit4.Assertions;
using Campus.Pipeline.Map;
using System.Numerics;
using System.Text;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Kernel.Maps;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class BackdropInputsTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void AuthoredRelativeKeyAndRawBytesAreFrozenAtLoad()
    {
        var map = Descriptor("environment/far.json");
        var raw = Encoding.UTF8.GetBytes("{ \"farClip\":12000 }");
        var files = new Dictionary<string, ReadOnlyMemory<byte>> { ["world/geo.json"] = new byte[] { 1 } };
        var reads = new List<string>();
        var snapshot = BackdropInputs.Freeze(map, "res://fixture/", files,
            path => path == "res://fixture/environment/far.json", path => { reads.Add(path); return raw; });
        raw[0] = 0;
        files.Clear();
        AssertThat(snapshot.Entries.Count == 2 && reads.SequenceEqual(["res://fixture/environment/far.json"])).OverrideFailureMessage("Wrong optional input path.").IsTrue();
        AssertThat(snapshot.Require("environment/far.json").Span[0] == (byte)'{' &&
            !snapshot.Entries.ContainsKey("backdrop.json")).OverrideFailureMessage("Backdrop bytes were aliased or renamed.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void NonJsonAuthoredPathIsReadAndExistingCaptureIsNeverReread()
    {
        var map = Descriptor("environment/backdrop.data");
        var bytes = Encoding.UTF8.GetBytes("{\"farClip\":12000}");
        var files = new Dictionary<string, ReadOnlyMemory<byte>>();
        var reads = 0;
        BackdropInputs.AddOptional(map, "res://fixture/", files, _ => true, path =>
        {
            AssertThat(path == "res://fixture/environment/backdrop.data").OverrideFailureMessage("Extension-free capture used the wrong key.").IsTrue();
            ++reads;
            return bytes;
        });
        BackdropInputs.AddOptional(map, "res://fixture/", files,
            _ => throw new InvalidOperationException("Captured file was probed twice."),
            _ => throw new InvalidOperationException("Captured file was read twice."));
        AssertThat(reads == 1 && files[map.Env.Backdrop].Equals((ReadOnlyMemory<byte>)bytes)).OverrideFailureMessage("Capture copied or reread the source.").IsTrue();
        var input = new MapLoadInput(new(map, "res://fixture/", []), files);
        bytes[0] = 0;
        AssertThat(input.SourceInputs.Require(map.Env.Backdrop).Span[0] == (byte)'{').OverrideFailureMessage("Formal load input did not freeze the captured bytes.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void MissingIsOptionalButEmptyOrInvalidPathsFailBeforePublication()
    {
        var map = Descriptor("nested/backdrop.json");
        var missing = BackdropInputs.Freeze(map, "res://fixture/", new Dictionary<string, ReadOnlyMemory<byte>>(),
            _ => false, _ => throw new InvalidOperationException("An absent input must not be read."));
        AssertThat(missing.Entries.Count == 0).OverrideFailureMessage("An absent backdrop was synthesized.").IsTrue();
        {
            var rejected = false;
            try { BackdropInputs.Freeze(map, "res://fixture/", missing.Entries, _ => true, _ => ReadOnlyMemory<byte>.Empty); }
            catch (InvalidDataException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Expected invalid backdrop input to fail.").IsTrue();
        }
        foreach (var key in new[] { "../backdrop.json", "/backdrop.json", "a\\b.json", "res://escape.json", "a//b.json" })
            {
                var rejected = false;
                try { BackdropInputs.Freeze(Descriptor(key), "res://fixture/", missing.Entries,
                _ => throw new InvalidOperationException("Invalid keys reached I/O."), _ => ReadOnlyMemory<byte>.Empty); }
                catch (InvalidDataException) { rejected = true; }
                AssertThat(rejected).OverrideFailureMessage("Expected invalid backdrop input to fail.").IsTrue();
            }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ConsecutiveMapsReadTheirOwnFarAndMissingPreservesTheCurrentCamera()
    {
        var far = BackdropInputs.CameraFar(Descriptor(), Snapshot("backdrop.json", "{\"farClip\":12000}"), 8000, .05f);
        far = BackdropInputs.CameraFar(Descriptor("nested/far.json"), Snapshot("nested/far.json", "{\"farClip\":19000}"), far, .05f);
        AssertThat(far == 19000).OverrideFailureMessage("The second map retained the first map's far clip.").IsTrue();
        far = BackdropInputs.CameraFar(Descriptor(), BuildInputSnapshot.Empty, far, .05f);
        AssertThat(far == 19000).OverrideFailureMessage("The missing file reset the camera far clip.").IsTrue();
        foreach (var bad in new[] { "", "{", "[]", "null", "{}", "{\"farClip\":null}", "{\"farClip\":\"12000\"}",
            "{\"farClip\":0}", "{\"farClip\":-1}", "{\"farClip\":0.05}", "{\"farClip\":1e99}" })
            {
                var rejected = false;
                try { BackdropInputs.CameraFar(Descriptor(), Snapshot("backdrop.json", bad), far, .05f); }
                catch (InvalidDataException) { rejected = true; }
                AssertThat(rejected).OverrideFailureMessage("Expected invalid backdrop input to fail.").IsTrue();
            }
        AssertThat(far == 19000).OverrideFailureMessage("Invalid data mutated the current value.").IsTrue();
    }

    internal static MapDescriptor Descriptor(string backdrop = "backdrop.json") => new(new("fixture-map"),
        new("测试地图", "Fixture"), MapKind.Outdoor, 1, 1, new(0, 0, 32648, new(5000, -7000), new(5200, -6800)),
        new(5100, -6900), new(new(5000, -28, -7000), new(5200, 2000, -6800)), new(new(5000, -7000), new(5200, -6800)),
        new(.9f, 30, 3000, -28, 2000, 30, 40, 150), new(78, -110), new(81, -107), "spawn",
        [new("spawn", new(new(5100, 20, -6900), 0, -15), null)],
        new(MapTimeRule.Shared, new(17.5, Season.Autumn), new("默认", "Default"), false, "fixture"),
        new("sky.json", backdrop, []), new Dictionary<string, string>(), []);

    internal static BuildInputSnapshot Snapshot(string key, string json) =>
        BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>> { [key] = Encoding.UTF8.GetBytes(json) });
}
