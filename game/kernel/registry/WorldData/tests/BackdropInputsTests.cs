using System.IO.Hashing;
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
    public void AuthoredRelativeKeyAndRawBytesAreFrozenAtLoad()
    {
        var map = Descriptor("environment/far.json");
        var raw = Encoding.UTF8.GetBytes("{ \"farClip\":12000 }");
        var files = new Dictionary<string, ReadOnlyMemory<byte>> { ["campus.json"] = new byte[] { 1 } };
        var reads = new List<string>();
        var snapshot = BackdropInputs.Freeze(map, "res://fixture/", files,
            path => path == "res://fixture/environment/far.json", path => { reads.Add(path); return raw; });
        raw[0] = 0;
        files.Clear();
        Check(snapshot.Entries.Count == 2 && reads.SequenceEqual(["res://fixture/environment/far.json"]), "Wrong optional input path.");
        Check(snapshot.Require("environment/far.json").Span[0] == (byte)'{' &&
            !snapshot.Entries.ContainsKey("backdrop.json"), "Backdrop bytes were aliased or renamed.");
    }

    [TestCase]
    public void NonJsonAuthoredPathIsReadAndExistingCaptureIsNeverReread()
    {
        var map = Descriptor("environment/backdrop.data");
        var bytes = Encoding.UTF8.GetBytes("{\"farClip\":12000}");
        var files = new Dictionary<string, ReadOnlyMemory<byte>>();
        var reads = 0;
        BackdropInputs.AddOptional(map, "res://fixture/", files, _ => true, path =>
        {
            Check(path == "res://fixture/environment/backdrop.data", "Extension-free capture used the wrong key.");
            ++reads;
            return bytes;
        });
        BackdropInputs.AddOptional(map, "res://fixture/", files,
            _ => throw new InvalidOperationException("Captured file was probed twice."),
            _ => throw new InvalidOperationException("Captured file was read twice."));
        Check(reads == 1 && files[map.Env.Backdrop].Equals((ReadOnlyMemory<byte>)bytes), "Capture copied or reread the source.");
        var input = new MapLoadInput(new(map, "res://fixture/", "res://bake/fixture/", []), files, ReadOnlyMemory<byte>.Empty);
        bytes[0] = 0;
        Check(input.SourceInputs.Require(map.Env.Backdrop).Span[0] == (byte)'{', "Formal load input did not freeze the captured bytes.");
    }

    [TestCase]
    public void MissingIsOptionalButEmptyOrInvalidPathsFailBeforePublication()
    {
        var map = Descriptor("nested/backdrop.json");
        var missing = BackdropInputs.Freeze(map, "res://fixture/", new Dictionary<string, ReadOnlyMemory<byte>>(),
            _ => false, _ => throw new InvalidOperationException("An absent input must not be read."));
        Check(missing.Entries.Count == 0, "An absent backdrop was synthesized.");
        Fails(() => BackdropInputs.Freeze(map, "res://fixture/", missing.Entries, _ => true, _ => ReadOnlyMemory<byte>.Empty));
        foreach (var key in new[] { "../backdrop.json", "/backdrop.json", "a\\b.json", "res://escape.json", "a//b.json" })
            Fails(() => BackdropInputs.Freeze(Descriptor(key), "res://fixture/", missing.Entries,
                _ => throw new InvalidOperationException("Invalid keys reached I/O."), _ => ReadOnlyMemory<byte>.Empty));
    }

    [TestCase]
    public void ScopedHashIncludesAuthoredKeyAndEveryOriginalByte()
    {
        var original = Snapshot("backdrop.json", "{\"farClip\":12000}");
        var whitespace = Snapshot("backdrop.json", "{ \"farClip\":12000}");
        var nested = Snapshot("environment/far.json", "{\"farClip\":12000}");
        var nestedChanged = Snapshot("environment/far.json", "{\"farClip\":13000}");
        Check(Hash(original) != Hash(whitespace) && Hash(original) != Hash(nested) && Hash(nested) != Hash(nestedChanged),
            "Scope framing lost a key or raw-byte change.");
        Check(Hash(original) != Hash(BuildInputSnapshot.Empty), "An absent backdrop shares the present-input hash.");
        // This proves the existing hash-append primitive only; D0 has no production DataHash/cache consumer.
    }

    [TestCase]
    public void ConsecutiveMapsReadTheirOwnFarAndMissingPreservesTheCurrentCamera()
    {
        var far = BackdropInputs.CameraFar(Descriptor(), Snapshot("backdrop.json", "{\"farClip\":12000}"), 8000, .05f);
        far = BackdropInputs.CameraFar(Descriptor("nested/far.json"), Snapshot("nested/far.json", "{\"farClip\":19000}"), far, .05f);
        Check(far == 19000, "The second map retained the first map's far clip.");
        far = BackdropInputs.CameraFar(Descriptor(), BuildInputSnapshot.Empty, far, .05f);
        Check(far == 19000, "The missing file reset the camera far clip.");
        foreach (var bad in new[] { "", "{", "[]", "null", "{}", "{\"farClip\":null}", "{\"farClip\":\"12000\"}",
            "{\"farClip\":0}", "{\"farClip\":-1}", "{\"farClip\":0.05}", "{\"farClip\":1e99}" })
            Fails(() => BackdropInputs.CameraFar(Descriptor(), Snapshot("backdrop.json", bad), far, .05f));
        Check(far == 19000, "Invalid data mutated the current value.");
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
    private static string Hash(BuildInputSnapshot inputs)
    {
        var hash = new XxHash128();
        BuildInputAssembly.AppendScopedInputs(bytes => hash.Append(bytes.Span), BuildInputSnapshot.Empty, inputs.Entries);
        return Convert.ToHexString(hash.GetCurrentHash());
    }
    private static void Fails(Action action)
    {
        try { action(); }
        catch (InvalidDataException) { return; }
        throw new InvalidOperationException("Expected invalid backdrop input to fail.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
