using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Campus.Contracts;

namespace Campus.Kernel.Inspect;

internal sealed class PerfFieldDictionary
{
    public static JsonSerializerOptions JsonOptions { get; } = new(ContractJson.Options)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };
    public JsonObject Snapshot { get; }
    public int Schema { get; }
    public double AnchorIntervalMilliseconds { get; }
    public double SampleIntervalMilliseconds { get; }
    public IReadOnlyDictionary<string, string[]> Columns { get; }

    public PerfFieldDictionary(string text)
    {
        Snapshot = JsonNode.Parse(text) as JsonObject ?? throw new InvalidDataException("Performance dictionary root is missing.");
        Schema = Snapshot["schema"]!.GetValue<int>();
        if (Schema != 2 || Snapshot["draft"]?.GetValue<bool>() != false)
            throw new InvalidDataException("Unsupported performance dictionary schema or draft.");
        AnchorIntervalMilliseconds = Snapshot["clock"]!["anchorIntervalSeconds"]!.GetValue<double>() * 1000;
        if (!double.IsFinite(AnchorIntervalMilliseconds) || AnchorIntervalMilliseconds <= 0 || Snapshot["nonSteadyRules"] is not JsonObject)
            throw new InvalidDataException("Performance clock or context rules are invalid.");
        var streams = Snapshot["streams"] as JsonObject ?? throw new InvalidDataException("Performance streams are missing.");
        SampleIntervalMilliseconds = 1000 / streams["samples"]!["rateHz"]!.GetValue<double>();
        var columns = new Dictionary<string, string[]>();
        foreach (string stream in new[] { "frames", "samples", "hw", "hwfast", "events" })
        {
            var spec = streams[stream] ?? throw new InvalidDataException($"Missing stream: {stream}");
            string file = spec["file"]!.GetValue<string>();
            if (file != stream + (stream == "events" ? ".jsonl" : ".csv"))
                throw new InvalidDataException($"Unexpected stream filename: {file}");
            var fields = spec["columns"]!.AsArray();
            var names = new List<string>();
            foreach (var field in fields)
            {
                var entry = field ?? throw new InvalidDataException($"Null field in {stream}");
                string name = entry["name"]!.GetValue<string>();
                string type = entry["type"]!.GetValue<string>();
                if (string.IsNullOrWhiteSpace(name) || name.Contains(',') || names.Contains(name) ||
                    type is not ("integer" or "number" or "boolean" or "string" or "object" or "array"))
                    throw new InvalidDataException($"Invalid field in {stream}: {name}");
                names.Add(name);
            }
            if (names.Count == 0) throw new InvalidDataException($"Empty columns in {stream}");
            columns.Add(stream, names.ToArray());
        }
        if (streams.Count != columns.Count) throw new InvalidDataException("Unexpected performance stream.");
        PerfFrameWriter.ValidateColumns(columns["frames"]);
        Columns = columns;
    }

    public static string Csv(object? value)
    {
        string text = value switch
        {
            null => "null",
            double number when !double.IsFinite(number) => "null",
            float number when !float.IsFinite(number) => "null",
            double number => number.ToString("R", CultureInfo.InvariantCulture),
            float number => number.ToString("R", CultureInfo.InvariantCulture),
            bool boolean => boolean ? "true" : "false",
            IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()!
        };
        return text.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? "\"" + text.Replace("\"", "\"\"") + "\"" : text;
    }

    public static StreamWriter OpenText(string path) => new(
        new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read), new UTF8Encoding(false)) { NewLine = "\n" };

    public static void PublishJson(string path, JsonObject value)
    {
        string temporary = path + ".tmp";
        using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, value, JsonOptions);
            stream.Flush(true);
        }
        if (File.Exists(path)) File.Move(temporary, path, true);
        else File.Move(temporary, path);
    }
}
