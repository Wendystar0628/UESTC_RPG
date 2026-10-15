<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Json

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

The `Json` class enables all data types to be converted to and from a JSON string. This is useful for serializing data, e.g. to save to a file or send over the network.

`Stringify()` is used to convert any data type into a JSON string.

`Parse()` is used to convert any existing JSON data into a [`Variant`](Variant.md) that can be used within Godot. If successfully parsed, use `Data` to retrieve the [`Variant`](Variant.md), and use `@GlobalScope.typeof` to check if the Variant's type is what you expect. JSON Objects are converted into a [`Collections.Dictionary`](Collections.Dictionary.md), but JSON data can be used to store [`Collections.Array`](Collections.Array.md)s, numbers, `System.String`s and even just a boolean.

```csharp
var data_to_send = ["a", "b", "c"]
var json_string = JSON.stringify(data_to_send)
# Save data
# ...
# Retrieve data
var json = JSON.new()
var error = json.parse(json_string)
if error == OK:
var data_received = json.data
if typeof(data_received) == TYPE_ARRAY:
print(data_received) # Prints the array.
else:
print("Unexpected data")
else:
print("JSON Parse Error: ", json.get_error_message(), " in ", json_string, " at line ", json.get_error_line())

```

Alternatively, you can parse strings using the static `ParseString()` method, but it doesn't handle errors.

```csharp
var data = JSON.parse_string(json_string) # Returns null if parsing failed.

```

**Note:** Both parse methods do not fully comply with the JSON specification:

- Trailing commas in arrays or objects are ignored, instead of causing a parser error.

- New line and tab characters are accepted in string literals, and are treated like their corresponding escape sequences `\n` and `\t`.

- Numbers are parsed using `String.to_float` which is generally more lax than the JSON specification.

- Certain errors, such as invalid Unicode sequences, do not cause a parser error. Instead, the string is cleaned up and an error is logged to the console.

## 构造函数

### Json

```csharp
public Json()
```

## 属性

### Data

```csharp
public Variant Data { get; set; }
```

Contains the parsed JSON data in [`Variant`](Variant.md) form.

## 方法

### FromNative

```csharp
public static Variant FromNative(Variant variant, bool fullObjects = false)
```

Converts a native engine type to a JSON-compliant value.

By default, objects are ignored for security reasons, unless `fullObjects` is `true`.

You can convert a native value to a JSON string like this:

```csharp
func encode_data(value, full_objects = false):
return JSON.stringify(JSON.from_native(value, full_objects))

```

### GetData

```csharp
public Variant GetData()
```

### GetErrorLine

```csharp
public int GetErrorLine()
```

Returns `0` if the last call to `Parse()` was successful, or the line number where the parse failed.

### GetErrorMessage

```csharp
public string GetErrorMessage()
```

Returns an empty string if the last call to `Parse()` was successful, or the error message if it failed.

### GetParsedText

```csharp
public string GetParsedText()
```

Return the text parsed by `Parse()` (requires passing `keep_text` to `Parse()`).

### Parse

```csharp
public Error Parse(string jsonText, bool keepText = false)
```

Attempts to parse the `jsonText` provided.

Returns an [`Error`](Error.md). If the parse was successful, it returns [`Error.Ok`](Error.md) and the result can be retrieved using `Data`. If unsuccessful, use `GetErrorLine()` and `GetErrorMessage()` to identify the source of the failure.

Non-static variant of `ParseString()`, if you want custom error handling.

The optional `keepText` argument instructs the parser to keep a copy of the original text. This text can be obtained later by using the `GetParsedText()` function and is used when saving the resource (instead of generating new text from `Data`).

### ParseString

```csharp
public static Variant ParseString(string jsonString)
```

Attempts to parse the `jsonString` provided and returns the parsed data. Returns `null` if parse failed.

### SetData

```csharp
public void SetData(Variant data)
```

### Stringify

```csharp
public static string Stringify(Variant data, string indent = "", bool sortKeys = true, bool fullPrecision = false)
```

Converts a [`Variant`](Variant.md) var to JSON text and returns the result. Useful for serializing data to store or send over the network.

**Note:** The JSON specification does not define integer or float types, but only a *number* type. Therefore, converting a Variant to JSON text will convert all numerical values to `System.Single` types.

**Note:** If `fullPrecision` is `true`, when stringifying floats, the unreliable digits are stringified in addition to the reliable digits to guarantee exact decoding.

The `indent` parameter controls if and how something is indented; its contents will be used where there should be an indent in the output. Even spaces like `"   "` will work. `\t` and `\n` can also be used for a tab indent, or to make a newline for each indent respectively.

**Warning:** Non-finite numbers are not supported in JSON. Any occurrences of `@GDScript.INF` will be replaced with `1e99999`, and negative `@GDScript.INF` will be replaced with `-1e99999`, but they will be interpreted correctly as infinity by most JSON parsers. `@GDScript.NAN` will be replaced with `null`, and it will not be interpreted as NaN in JSON parsers. If you expect non-finite numbers, consider passing your data through `FromNative()` first.

**Example output:**

```csharp
## JSON.stringify(my_dictionary)
{"name":"my_dictionary","version":"1.0.0","entities":[{"name":"entity_0","value":"value_0"},{"name":"entity_1","value":"value_1"}]}

## JSON.stringify(my_dictionary, "\t")
{
"name": "my_dictionary",
"version": "1.0.0",
"entities": [
{
"name": "entity_0",
"value": "value_0"
},
{
"name": "entity_1",
"value": "value_1"
}
]
}

## JSON.stringify(my_dictionary, "...")
{
..."name": "my_dictionary",
..."version": "1.0.0",
..."entities": [
......{
........."name": "entity_0",
........."value": "value_0"
......},
......{
........."name": "entity_1",
........."value": "value_1"
......}
...]
}

```

### ToNative

```csharp
public static Variant ToNative(Variant json, bool allowObjects = false)
```

Converts a JSON-compliant value that was created with `FromNative()` back to native engine types.

By default, objects are ignored for security reasons, unless `allowObjects` is `true`.

You can convert a JSON string back to a native value like this:

```csharp
func decode_data(string, allow_objects = false):
return JSON.to_native(JSON.parse_string(string), allow_objects)

```
