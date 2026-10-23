using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Campus.Contracts;
using Campus.Contracts.Save;

namespace Campus.Kernel.Save.Format;

public sealed class SaveFormatException(string message, Exception? innerException = null) : Exception(message, innerException)
{
    public SaveMeta? Metadata { get; internal set; }
    internal bool IsNewer { get; init; }
}

public sealed record RawSaveSection([property: DataVersion] int Version, byte[] Envelope);
public sealed record SaveDocument(SaveMeta Meta, IReadOnlyDictionary<string, RawSaveSection> Sections, byte[]? Thumbnail);

/// <summary>One format number (header, meta and body agree). No engine types, reflection serialization, paths, or executable content.</summary>
public static class SaveContainer
{
    public const int HeaderSize = 64;
    public const int MaxFile = 20 * 1024 * 1024;
    public const int MaxMeta = 16 * 1024;
    public const int MaxCompressed = 16 * 1024 * 1024;
    public const int MaxThumbnail = 256 * 1024;
    [DataVersion]
    public const int CurrentFormat = 2;
    private static ReadOnlySpan<byte> Magic => "UESTCSAV"u8;

    public static byte[] Encode(SaveDocument document)
    {
        ValidateMeta(document.Meta);
        var thumb = ValidateThumbnail(document.Thumbnail) ? document.Thumbnail : null;
        var meta = JsonSerializer.SerializeToUtf8Bytes(document.Meta with { HasThumb = thumb is not null }, SaveFormatJsonContext.Strict.SaveMeta);
        if (meta.Length > MaxMeta) throw new SaveFormatException("Save metadata exceeds 16 KiB.");
        using var raw = new BoundedMemoryStream(MaxFile);
        using (var writer = new Utf8JsonWriter(raw))
        {
            writer.WriteStartObject(); writer.WriteNumber("format", CurrentFormat); writer.WriteStartObject("sections");
            foreach (var (key, section) in document.Sections.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                ValidateSectionKey(key);
                writer.WritePropertyName(key);
                if (writer.BytesCommitted + writer.BytesPending + section.Envelope.Length > MaxFile)
                    throw new SaveFormatException("Expanded save exceeds the container limit.");
                using var envelope = ParseJson(section.Envelope);
                ValidateEnvelope(envelope.RootElement, out var version);
                if (version != section.Version) throw new SaveFormatException("Section version does not match its envelope.");
                writer.WriteRawValue(section.Envelope, skipInputValidation: false);
            }
            writer.WriteEndObject(); writer.WriteEndObject();
        }
        using var compressed = new BoundedMemoryStream(MaxCompressed);
        using (var brotli = new BrotliStream(compressed, CompressionLevel.Fastest, leaveOpen: true)) raw.WriteTo(brotli);
        var bytes = new byte[checked(HeaderSize + meta.Length + (int)compressed.Length + (thumb?.Length ?? 0))];
        Magic.CopyTo(bytes);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(8), (ushort)CurrentFormat);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(10), (ushort)(thumb is null ? 1 : 3));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), (uint)meta.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16), (uint)compressed.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20), (uint)(thumb?.Length ?? 0));
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(24), document.Meta.Gen);
        meta.CopyTo(bytes, HeaderSize);
        compressed.GetBuffer().AsSpan(0, (int)compressed.Length).CopyTo(bytes.AsSpan(HeaderSize + meta.Length));
        thumb?.CopyTo(bytes, HeaderSize + meta.Length + (int)compressed.Length);
        Hash(bytes).CopyTo(bytes, 32);
        return bytes;
    }

    public static SaveDocument Decode(ReadOnlyMemory<byte> bytes, ulong? expectedGeneration = null, SlotId? expectedSlot = null)
    {
        SaveMeta? metadata = null;
        try { return DecodeChecked(bytes, expectedGeneration, expectedSlot, out metadata); }
        catch (SaveFormatException error) { error.Metadata ??= metadata; throw; }
        catch (Exception error) when (error is JsonException or InvalidDataException or FormatException or OverflowException or ArgumentException or InvalidOperationException)
        { throw new SaveFormatException("Invalid save: " + error.Message, error) { Metadata = metadata }; }
    }

    private static SaveDocument DecodeChecked(ReadOnlyMemory<byte> memory, ulong? expectedGeneration, SlotId? expectedSlot, out SaveMeta? metadata)
    {
        metadata = null;
        var bytes = memory.Span;
        if (bytes.Length < HeaderSize || bytes.Length > MaxFile || !bytes[..8].SequenceEqual(Magic))
            throw new SaveFormatException("Invalid save header or file size.");
        var headerFormat = BinaryPrimitives.ReadUInt16LittleEndian(bytes[8..]);
        var flags = BinaryPrimitives.ReadUInt16LittleEndian(bytes[10..]);
        var ml = BinaryPrimitives.ReadUInt32LittleEndian(bytes[12..]);
        var bl = BinaryPrimitives.ReadUInt32LittleEndian(bytes[16..]);
        var tl = BinaryPrimitives.ReadUInt32LittleEndian(bytes[20..]);
        var gen = BinaryPrimitives.ReadUInt64LittleEndian(bytes[24..]);
        if (ml == 0 || ml > MaxMeta || (ulong)HeaderSize + ml > (ulong)bytes.Length ||
            gen == 0 || expectedGeneration is { } eg && gen != eg)
            throw new SaveFormatException("Invalid metadata length or generation.");
        if (bl == 0 || bl > MaxCompressed || tl > MaxThumbnail ||
            (ulong)HeaderSize + ml + bl + tl != (ulong)bytes.Length)
            throw new SaveFormatException("Invalid body or thumbnail lengths.");
        if (!CryptographicOperations.FixedTimeEquals(Hash(bytes), bytes.Slice(32, 32)))
            throw new SaveFormatException("SHA-256 mismatch.");
        // SYS-SAVE-FORMAT: a future container is read-only even when its metadata schema is unknown.
        if (headerFormat > CurrentFormat)
            throw new SaveFormatException("Save container comes from a newer version.") { IsNewer = true };
        // Only metadata covered by a verified container hash can identify a world line for recovery.
        using var metaJson = ParseJson(memory.Slice(HeaderSize, (int)ml));
        if (metaJson.RootElement.TryGetProperty("format", out var metadataFormat) && metadataFormat.GetInt32() > CurrentFormat)
            throw new SaveFormatException("Save metadata comes from a newer version.") { IsNewer = true };
        var meta = JsonSerializer.Deserialize(metaJson.RootElement, SaveFormatJsonContext.Strict.SaveMeta)
            ?? throw new SaveFormatException("Missing metadata.");
        ValidateMeta(meta);
        if (meta.Gen != gen || expectedSlot is { } es && meta.Slot != es)
            throw new SaveFormatException("Metadata identity disagrees with the container.");
        metadata = meta;
        if (headerFormat != CurrentFormat || (flags & ~3) != 0 || (flags & 1) == 0 ||
            ((flags & 2) != 0) != (tl != 0) || meta.HasThumb != (tl != 0))
            throw new SaveFormatException("Unsupported container flags.");
        using var body = new BoundedMemoryStream(MaxFile);
        try
        {
            using var brotli = new BrotliStream(new MemoryStream(memory.Slice(HeaderSize + (int)ml, (int)bl).ToArray()), CompressionMode.Decompress);
            brotli.CopyTo(body);
        }
        catch (InvalidDataException error) { throw new SaveFormatException("Invalid Brotli stream.", error); }
        using var json = ParseJson(body.ToArray());
        var root = json.RootElement;
        RequireMembers(root, ["format", "sections"]);
        var format = root.GetProperty("format").GetInt32();
        if (format > CurrentFormat)
            throw new SaveFormatException("Save body comes from a newer version.") { IsNewer = true };
        if (format != meta.Format || format != CurrentFormat) throw new SaveFormatException("Body format mismatch.");
        var sectionsElement = root.GetProperty("sections");
        if (sectionsElement.ValueKind != JsonValueKind.Object) throw new SaveFormatException("sections must be an object.");
        var sections = new SortedDictionary<string, RawSaveSection>(StringComparer.Ordinal);
        foreach (var section in sectionsElement.EnumerateObject())
        {
            ValidateSectionKey(section.Name);
            ValidateEnvelope(section.Value, out var sectionVersion);
            sections.Add(section.Name, new(sectionVersion, Encoding.UTF8.GetBytes(section.Value.GetRawText())));
        }
        var thumbnail = tl == 0 ? null : memory.Slice(HeaderSize + (int)ml + (int)bl, (int)tl).ToArray();
        if (!ValidateThumbnail(thumbnail)) thumbnail = null;
        return new(meta, sections, thumbnail);
    }

    // The existing container bounds also cap expanded JSON and encoded buffers, before a write can grow them.
    private sealed class BoundedMemoryStream(int maximum) : MemoryStream
    {
        private void CheckWrite(int count)
        {
            if (Position > maximum - (long)count) throw new SaveFormatException("Save buffer exceeds its container limit.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        { CheckWrite(count); base.Write(buffer, offset, count); }

        public override void Write(ReadOnlySpan<byte> buffer)
        { CheckWrite(buffer.Length); base.Write(buffer); }

        public override void WriteByte(byte value)
        { CheckWrite(1); base.WriteByte(value); }
    }

    public static JsonDocument ParseJson(ReadOnlyMemory<byte> utf8)
    {
        var document = JsonDocument.Parse(utf8);
        try { CheckDuplicates(document.RootElement); return document; }
        catch { document.Dispose(); throw; }
    }

    private static void CheckDuplicates(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new SaveFormatException("Duplicate JSON property: " + property.Name);
                CheckDuplicates(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) CheckDuplicates(item);
    }

    private static void ValidateEnvelope(JsonElement element, [DataVersion] out int version)
    {
        RequireMembers(element, ["v", "data"]);
        version = element.GetProperty("v").GetInt32();
        if (version < 1 || element.GetProperty("data").ValueKind != JsonValueKind.Object)
            throw new SaveFormatException("Invalid save section envelope.");
    }

    private static void RequireMembers(JsonElement element, string[] required)
    {
        if (element.ValueKind != JsonValueKind.Object) throw new SaveFormatException("Expected JSON object.");
        var actual = element.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal).ToArray();
        if (!actual.SequenceEqual(required.Order(StringComparer.Ordinal))) throw new SaveFormatException("Invalid JSON members.");
    }

    private static byte[] Hash(ReadOnlySpan<byte> bytes)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(bytes[..32]); hash.AppendData(bytes[HeaderSize..]);
        return hash.GetHashAndReset();
    }

    public static bool ValidateThumbnail(byte[]? bytes) => bytes is { Length: >= 12 and <= MaxThumbnail } &&
        bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8) &&
        BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4)) + 8UL == (ulong)bytes.Length;

    public static void ValidateSectionKey(string key)
    {
        var id = key;
        if (key.StartsWith("map:", StringComparison.Ordinal))
        {
            var slash = key.IndexOf('/');
            if (slash < 5 || !IsId(key[4..slash])) throw new SaveFormatException("Invalid per-map key.");
            id = key[(slash + 1)..];
        }
        if (!IsId(id)) throw new SaveFormatException("Invalid section key.");
    }

    public static bool IsId(string? id) => !string.IsNullOrEmpty(id) && id.Length <= 128 &&
        id.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');

    private static void ValidateMeta(SaveMeta meta)
    {
        if (meta.Format != CurrentFormat || meta.Gen == 0 || meta.Line == 0 || meta.Line > meta.Gen ||
            !ValidSlot(meta.Slot) || !IsId(meta.Map.Value) || !IsId(meta.Ruleset) ||
            !double.IsFinite(meta.PlaySeconds) || meta.PlaySeconds < 0 || !double.IsFinite(meta.WorldHours) ||
            !double.IsFinite(meta.Clock) || meta.Clock < 0 || meta.Clock >= 24 ||
            !float.IsFinite(meta.Pose.Position.X) || !float.IsFinite(meta.Pose.Position.Y) || !float.IsFinite(meta.Pose.Position.Z) ||
            !float.IsFinite(meta.Pose.YawDeg) || !float.IsFinite(meta.Pose.PitchDeg) ||
            !Enum.IsDefined(meta.Kind) || !Enum.IsDefined(meta.Mode) || !Enum.IsDefined(meta.Season) || !Enum.IsDefined(meta.Weather) ||
            !Enum.IsDefined(meta.Reason)) throw new SaveFormatException("Invalid metadata fields.");
    }

    public static bool ValidSlot(SlotId slot) => SaveSlots.Auto.Contains(slot);
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(SaveMeta))]
internal partial class SaveFormatJsonContext : JsonSerializerContext
{
    internal static SaveFormatJsonContext Strict { get; } = new(new JsonSerializerOptions(ContractJson.Options)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        PropertyNameCaseInsensitive = false
    });
}
