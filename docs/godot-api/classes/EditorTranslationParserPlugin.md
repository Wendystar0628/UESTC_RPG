<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorTranslationParserPlugin

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

`EditorTranslationParserPlugin` is invoked when a file is being parsed to extract strings that require translation. To define the parsing and string extraction logic, override the `_ParseFile()` method in script.

The return value should be an [`Collections.Array`](Collections.Array.md) of `System.String`[]s, one for each extracted translatable string. Each entry should contain `[msgid, msgctxt, msgid_plural, comment, source_line]`, where all except `msgid` are optional. Empty strings will be ignored.

The extracted strings will be written into a translation template file selected by user under "Template Generation" in "Localization" tab in "Project Settings" menu.

Below shows an example of a custom parser that extracts strings from a CSV file to write into a template.

`System.String`[]

```csharp
using Godot;

[Tool]
public partial class CustomParser : EditorTranslationParserPlugin
{
public override Godot.Collections.Array<string[]> _ParseFile(string path)
{
Godot.Collections.Array<string[]> ret;
using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
string text = file.GetAsText();
string[] splitStrs = text.Split(",", allowEmpty: false);
foreach (string s in splitStrs)
{
ret.Add([s]);
//GD.Print($"Extracted string: {s}");
}
return ret;
}

public override string[] _GetRecognizedExtensions()
{
return ["csv"];
}
}

```

```csharp
// This will add a message with msgid "Test 1", msgctxt "context", msgid_plural "test 1 plurals", comment "test 1 comment", and source line "7".
ret.Add(["Test 1", "context", "test 1 plurals", "test 1 comment", "7"]);
// This will add a message with msgid "A test without context" and msgid_plural "plurals".
ret.Add(["A test without context", "", "plurals"]);
// This will add a message with msgid "Only with context" and msgctxt "a friendly context".
ret.Add(["Only with context", "a friendly context"]);

```

**Note:** If you override parsing logic for standard script types (GDScript, C#, etc.), it would be better to load the `path` argument using [`ResourceLoader.Load`](ResourceLoader.md). This is because built-in scripts are loaded as [`Resource`](Resource.md) type, not [`FileAccess`](FileAccess.md) type. For example:

```csharp
public override Godot.Collections.Array<string[]> _ParseFile(string path)
{
var res = ResourceLoader.Load<Script>(path, "Script");
string text = res.SourceCode;
// Parsing logic.
}

public override string[] _GetRecognizedExtensions()
{
return ["gd"];
}

```

Alternatively, the plugin can directly modify the final list of strings, by implementing `_CustomizeStrings()`.
To use `EditorTranslationParserPlugin`, register it using the [`EditorPlugin.AddTranslationParserPlugin`](EditorPlugin.md) method first.

## 构造函数

### EditorTranslationParserPlugin

```csharp
public EditorTranslationParserPlugin()
```

## 方法

### _CustomizeStrings

```csharp
public virtual Array<string[]> _CustomizeStrings(Array<string[]> strings)
```

引擎名：`_customize_strings`

Called after parsing all files. You can modify the `strings` array to add or remove entries from the final list of strings, then return it after modifications. Each entry is a `System.String`[] like explained in the `EditorTranslationParserPlugin`'s description.

```csharp
@tool
extends EditorTranslationParserPlugin

func _customize_strings(strings):
# Add new string.
strings.append(["Test 1", "context", "test 1 plurals", "test 1 comment"])

# Remove all strings that begin with $.
strings = strings.filter(func(s): return not s[0].begins_with("$"))

return strings

```

### _GetRecognizedExtensions

```csharp
public virtual string[] _GetRecognizedExtensions()
```

引擎名：`_get_recognized_extensions`

Gets the list of file extensions to associate with this parser, e.g. `["csv"]`.

### _ParseFile

```csharp
public virtual Array<string[]> _ParseFile(string path)
```

引擎名：`_parse_file`

Override this method to define a custom parsing logic to extract the translatable strings.
