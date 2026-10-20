<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ConfigFile

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This helper class can be used to store [`Variant`](Variant.md) values on the filesystem using INI-style formatting. The stored values are identified by a section and a key:

```csharp
[section]
some_key=42
string_example="Hello World3D!"
a_vector=Vector3(1, 0, 2)

```

The stored data can be saved to or parsed from a file, though ConfigFile objects can also be used directly without accessing the filesystem.

The following example shows how to create a simple `ConfigFile` and save it on disc:

```csharp
// Create new ConfigFile object.
var config = new ConfigFile();

// Store some values.
config.SetValue("Player1", "player_name", "Steve");
config.SetValue("Player1", "best_score", 10);
config.SetValue("Player2", "player_name", "V3geta");
config.SetValue("Player2", "best_score", 9001);

// Save it to a file (overwrite if already exists).
config.Save("user://scores.cfg");

```

This example shows how the above file could be loaded:

```csharp
var score_data = new Godot.Collections.Dictionary();
var config = new ConfigFile();

// Load data from a file.
Error err = config.Load("user://scores.cfg");

// If the file didn't load, ignore it.
if (err != Error.Ok)
{
return;
}

// Iterate over all sections.
foreach (String player in config.GetSections())
{
// Fetch the data for each section.
var player_name = (String)config.GetValue(player, "player_name");
var player_score = (int)config.GetValue(player, "best_score");
score_data[player_name] = player_score;
}

```

Any operation that mutates the ConfigFile such as `SetValue()`, `Clear()`, or `EraseSection()`, only changes what is loaded in memory. If you want to write the change to a file, you have to save the changes with `Save()`, `SaveEncrypted()`, or `SaveEncryptedPass()`.

Keep in mind that section and property names can't contain spaces. Anything after a space will be ignored on save and on load.

ConfigFiles can also contain manually written comment lines starting with a semicolon (`;`). Those lines will be ignored when parsing the file. Note that comments will be lost when saving the ConfigFile. This can still be useful for dedicated server configuration files, which are typically never overwritten without explicit user action.

**Note:** The file extension given to a ConfigFile does not have any impact on its formatting or behavior. By convention, the `.cfg` extension is used here, but any other extension such as `.ini` is also valid. Since neither `.cfg` nor `.ini` are standardized, Godot's ConfigFile formatting may differ from files written by other programs.

## 构造函数

### ConfigFile

```csharp
public ConfigFile()
```

## 方法

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Removes the entire contents of the config.

### EncodeToText

```csharp
public string EncodeToText()
```

引擎名：`encode_to_text`

Obtain the text version of this config file (the same text that would be written to a file).

### EraseSection

```csharp
public void EraseSection(string section)
```

引擎名：`erase_section`

Deletes the specified section along with all the key-value pairs inside. Raises an error if the section does not exist.

### EraseSectionKey

```csharp
public void EraseSectionKey(string section, string key)
```

引擎名：`erase_section_key`

Deletes the specified key in a section. Raises an error if either the section or the key do not exist.

### GetSectionKeys

```csharp
public string[] GetSectionKeys(string section)
```

引擎名：`get_section_keys`

Returns an array of all defined key identifiers in the specified section. Raises an error and returns an empty array if the section does not exist.

### GetSections

```csharp
public string[] GetSections()
```

引擎名：`get_sections`

Returns an array of all defined section identifiers.

### GetValue

```csharp
public Variant GetValue(string section, string key, Variant default = default)
```

引擎名：`get_value`

Returns the current value for the specified section and key. If either the section or the key do not exist, the method returns the fallback `default` value. If `default` is not specified or set to `null`, an error is also raised.

### HasSection

```csharp
public bool HasSection(string section)
```

引擎名：`has_section`

Returns `true` if the specified section exists.

### HasSectionKey

```csharp
public bool HasSectionKey(string section, string key)
```

引擎名：`has_section_key`

Returns `true` if the specified section-key pair exists.

### Load

```csharp
public Error Load(string path)
```

引擎名：`load`

Loads the config file specified as a parameter. The file's contents are parsed and loaded in the `ConfigFile` object which the method was called on.

Returns [`Error.Ok`](Error.md) on success, or one of the other [`Error`](Error.md) values if the operation failed.

### LoadEncrypted

```csharp
public Error LoadEncrypted(string path, ReadOnlySpan<byte> key)
```

引擎名：`load_encrypted`

Loads the encrypted config file specified as a parameter, using the provided `key` to decrypt it. The file's contents are parsed and loaded in the `ConfigFile` object which the method was called on.

Returns [`Error.Ok`](Error.md) on success, or one of the other [`Error`](Error.md) values if the operation failed.

### LoadEncrypted

```csharp
public Error LoadEncrypted(string path, byte[] key)
```

引擎名：`load_encrypted`

Loads the encrypted config file specified as a parameter, using the provided `key` to decrypt it. The file's contents are parsed and loaded in the `ConfigFile` object which the method was called on.

Returns [`Error.Ok`](Error.md) on success, or one of the other [`Error`](Error.md) values if the operation failed.

### LoadEncryptedPass

```csharp
public Error LoadEncryptedPass(string path, string password)
```

引擎名：`load_encrypted_pass`

Loads the encrypted config file specified as a parameter, using the provided `password` to decrypt it. The file's contents are parsed and loaded in the `ConfigFile` object which the method was called on.

Returns [`Error.Ok`](Error.md) on success, or one of the other [`Error`](Error.md) values if the operation failed.

### Parse

```csharp
public Error Parse(string data)
```

引擎名：`parse`

Parses the passed string as the contents of a config file. The string is parsed and loaded in the ConfigFile object which the method was called on.

Returns [`Error.Ok`](Error.md) on success, or one of the other [`Error`](Error.md) values if the operation failed.

### Save

```csharp
public Error Save(string path)
```

引擎名：`save`

Saves the contents of the `ConfigFile` object to the file specified as a parameter. The output file uses an INI-style structure.

Returns [`Error.Ok`](Error.md) on success, or one of the other [`Error`](Error.md) values if the operation failed.

### SaveEncrypted

```csharp
public Error SaveEncrypted(string path, ReadOnlySpan<byte> key)
```

引擎名：`save_encrypted`

Saves the contents of the `ConfigFile` object to the AES-256 encrypted file specified as a parameter, using the provided `key` to encrypt it. The output file uses an INI-style structure.

Returns [`Error.Ok`](Error.md) on success, or one of the other [`Error`](Error.md) values if the operation failed.

### SaveEncrypted

```csharp
public Error SaveEncrypted(string path, byte[] key)
```

引擎名：`save_encrypted`

Saves the contents of the `ConfigFile` object to the AES-256 encrypted file specified as a parameter, using the provided `key` to encrypt it. The output file uses an INI-style structure.

Returns [`Error.Ok`](Error.md) on success, or one of the other [`Error`](Error.md) values if the operation failed.

### SaveEncryptedPass

```csharp
public Error SaveEncryptedPass(string path, string password)
```

引擎名：`save_encrypted_pass`

Saves the contents of the `ConfigFile` object to the AES-256 encrypted file specified as a parameter, using the provided `password` to encrypt it. The output file uses an INI-style structure.

Returns [`Error.Ok`](Error.md) on success, or one of the other [`Error`](Error.md) values if the operation failed.

### SetValue

```csharp
public void SetValue(string section, string key, Variant value)
```

引擎名：`set_value`

Assigns a value to the specified key of the specified section. If either the section or the key do not exist, they are created. Passing a `null` value deletes the specified key if it exists, and deletes the section if it ends up empty once the key has been removed.
