using System.Numerics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Campus.Contracts;

public static class ContractJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            MaxDepth = 64,
            AllowOutOfOrderMetadataProperties = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        options.Converters.Add(new StringIdConverterFactory());
        options.Converters.Add(new Vector2Converter());
        options.Converters.Add(new Vector3Converter());
        options.Converters.Add(new Vector4Converter());
        options.Converters.Add(new QuaternionConverter());
        options.Converters.Add(new MatrixConverter());
        return options;
    }

    private sealed class StringIdConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeToConvert.Assembly == typeof(ContractJson).Assembly && typeToConvert.IsValueType &&
            typeToConvert.GetProperty("Value")?.PropertyType == typeof(string) &&
            typeToConvert.GetConstructor([typeof(string)]) is not null;

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            (JsonConverter)Activator.CreateInstance(typeof(StringIdConverter<>).MakeGenericType(typeToConvert))!;
    }

    private sealed class StringIdConverter<T> : JsonConverter<T> where T : struct
    {
        private static readonly ConstructorInfo Constructor = typeof(T).GetConstructor([typeof(string)])!;
        private static readonly PropertyInfo ValueProperty = typeof(T).GetProperty("Value")!;
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String) throw new JsonException("Expected a string identifier.");
            var value = reader.GetString() ?? throw new JsonException("Identifier cannot be null.");
            return (T)Constructor.Invoke([value]);
        }
        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
            writer.WriteStringValue((string?)ValueProperty.GetValue(value));
        public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            (T)Constructor.Invoke([reader.GetString() ?? throw new JsonException("Null identifier.")]);
        public override void WriteAsPropertyName(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
            writer.WritePropertyName((string?)ValueProperty.GetValue(value) ?? throw new JsonException("Null identifier."));
    }

    private static float[] ReadArray(ref Utf8JsonReader reader, int length)
    {
        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("Expected a numeric vector.");
        var values = new float[length];
        for (var i = 0; i < length; i++)
        {
            if (!reader.Read() || reader.TokenType != JsonTokenType.Number) throw new JsonException("Invalid vector length.");
            values[i] = reader.GetSingle();
            if (!float.IsFinite(values[i])) throw new JsonException("Non-finite vector.");
        }
        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray) throw new JsonException("Invalid vector length.");
        return values;
    }
    private static void WriteArray(Utf8JsonWriter writer, params float[] values)
    {
        writer.WriteStartArray();
        foreach (var value in values) writer.WriteNumberValue(value);
        writer.WriteEndArray();
    }
    private sealed class Vector2Converter : JsonConverter<Vector2>
    {
        public override Vector2 Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) { var a = ReadArray(ref reader, 2); return new(a[0], a[1]); }
        public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options) => WriteArray(writer, value.X, value.Y);
    }
    private sealed class Vector3Converter : JsonConverter<Vector3>
    {
        public override Vector3 Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) { var a = ReadArray(ref reader, 3); return new(a[0], a[1], a[2]); }
        public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options) => WriteArray(writer, value.X, value.Y, value.Z);
    }
    private sealed class Vector4Converter : JsonConverter<Vector4>
    {
        public override Vector4 Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) { var a = ReadArray(ref reader, 4); return new(a[0], a[1], a[2], a[3]); }
        public override void Write(Utf8JsonWriter writer, Vector4 value, JsonSerializerOptions options) => WriteArray(writer, value.X, value.Y, value.Z, value.W);
    }
    private sealed class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override Quaternion Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) { var a = ReadArray(ref reader, 4); return new(a[0], a[1], a[2], a[3]); }
        public override void Write(Utf8JsonWriter writer, Quaternion value, JsonSerializerOptions options) => WriteArray(writer, value.X, value.Y, value.Z, value.W);
    }
    private sealed class MatrixConverter : JsonConverter<Matrix4x4>
    {
        public override Matrix4x4 Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            var a = ReadArray(ref reader, 16);
            return new(a[0], a[1], a[2], a[3], a[4], a[5], a[6], a[7], a[8], a[9], a[10], a[11], a[12], a[13], a[14], a[15]);
        }
        public override void Write(Utf8JsonWriter writer, Matrix4x4 value, JsonSerializerOptions options) =>
            WriteArray(writer, value.M11, value.M12, value.M13, value.M14, value.M21, value.M22, value.M23, value.M24,
                value.M31, value.M32, value.M33, value.M34, value.M41, value.M42, value.M43, value.M44);
    }
}
