<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RegEx

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A regular expression (or regex) is a compact language that can be used to recognize strings that follow a specific pattern, such as URLs, email addresses, complete sentences, etc. For example, a regex of `ab[0-9]` would find any string that is `ab` followed by any number from `0` to `9`. For a more in-depth look, you can easily find various tutorials and detailed explanations on the Internet.

To begin, the RegEx object needs to be compiled with the search pattern using `Compile()` before it can be used. Alternatively, the static method `CreateFromString()` can be used to create and compile a RegEx object in a single method call.

```csharp
var regex = RegEx.new()
regex.compile("\\w-(\\d+)")
# Shorthand to create and compile a regex (used in the examples below):
var regex2 = RegEx.create_from_string("\\w-(\\d+)")

```

The search pattern must be escaped first for GDScript before it is escaped for the expression. For example, `compile("\\d+")` would be read by RegEx as `\d+`. Similarly, `compile("\"(?:\\\\.|[^\"])*\"")` would be read as `"(?:\\.|[^"])*"`. In GDScript, you can also use raw string literals (r-strings). For example, `compile(r'"(?:\\.|[^"])*"')` would be read the same.

Using `Search()`, you can find the pattern within the given text. If a pattern is found, [`RegExMatch`](RegExMatch.md) is returned and you can retrieve details of the results using methods such as [`RegExMatch.GetString`](RegExMatch.md) and [`RegExMatch.GetStart`](RegExMatch.md).

```csharp
var regex = RegEx.create_from_string("\\w-(\\d+)")
var result = regex.search("abc n-0123")
if result:
print(result.get_string()) # Prints "n-0123"

```

The results of capturing groups `()` can be retrieved by passing the group number to the various methods in [`RegExMatch`](RegExMatch.md). Group 0 is the default and will always refer to the entire pattern. In the above example, calling `result.get_string(1)` would give you `0123`.

This version of RegEx also supports named capturing groups, and the names can be used to retrieve the results. If two or more groups have the same name, the name would only refer to the first one with a match.

```csharp
var regex = RegEx.create_from_string("d(?<digit>[0-9]+)|x(?<digit>[0-9a-f]+)")
var result = regex.search("the number is x2f")
if result:
print(result.get_string("digit")) # Prints "2f"

```

If you need to process multiple results, `SearchAll()` generates a list of all non-overlapping results. This can be combined with a `for` loop for convenience.

```csharp
# Prints "01 03 0 3f 42"
for result in regex.search_all("d01, d03, d0c, x3f and x42"):
print(result.get_string("digit"))

```

**Example:** Split a string using a RegEx:

```csharp
var regex = RegEx.create_from_string("\\S+") # Negated whitespace character class.
var results = []
for result in regex.search_all("One  Two \n\tThree"):
results.push_back(result.get_string())
print(results) # Prints ["One", "Two", "Three"]

```

**Note:** Godot's regex implementation is based on the [PCRE2](https://www.pcre.org/) library. You can view the full pattern reference [here](https://www.pcre.org/current/doc/html/pcre2pattern.html).

**Tip:** You can use [Regexr](https://regexr.com/) to test regular expressions online.

## 构造函数

### RegEx

```csharp
public RegEx()
```

## 方法

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

This method resets the state of the object, as if it was freshly created. Namely, it unassigns the regular expression of this object.

### Compile

```csharp
public Error Compile(string pattern)
```

引擎名：`compile`

Compiles and assign the search pattern to use. Returns [`Error.Ok`](Error.md) if the compilation is successful. If compilation fails, returns [`Error.Failed`](Error.md) and when `showError` is `true`, details are printed to standard output.

### Compile

```csharp
public Error Compile(string pattern, bool showError = true)
```

引擎名：`compile`

Compiles and assign the search pattern to use. Returns [`Error.Ok`](Error.md) if the compilation is successful. If compilation fails, returns [`Error.Failed`](Error.md) and when `showError` is `true`, details are printed to standard output.

### CreateFromString

```csharp
public static RegEx CreateFromString(string pattern)
```

引擎名：`create_from_string`

Creates and compiles a new `RegEx` object. See also `Compile()`.

### CreateFromString

```csharp
public static RegEx CreateFromString(string pattern, bool showError = true)
```

引擎名：`create_from_string`

Creates and compiles a new `RegEx` object. See also `Compile()`.

### GetGroupCount

```csharp
public int GetGroupCount()
```

引擎名：`get_group_count`

Returns the number of capturing groups in compiled pattern.

### GetNames

```csharp
public string[] GetNames()
```

引擎名：`get_names`

Returns an array of names of named capturing groups in the compiled pattern. They are ordered by appearance.

### GetPattern

```csharp
public string GetPattern()
```

引擎名：`get_pattern`

Returns the original search pattern that was compiled.

### IsValid

```csharp
public bool IsValid()
```

引擎名：`is_valid`

Returns whether this object has a valid search pattern assigned.

### Search

```csharp
public RegExMatch Search(string subject, int offset = 0, int end = -1)
```

引擎名：`search`

Searches the text for the compiled pattern. Returns a [`RegExMatch`](RegExMatch.md) container of the first matching result if found, otherwise `null`.

The region to search within can be specified with `offset` and `end`. This is useful when searching for another match in the same `subject` by calling this method again after a previous success. Note that setting these parameters differs from passing over a shortened string. For example, the start anchor `^` is not affected by `offset`, and the character before `offset` will be checked for the word boundary `\b`.

### SearchAll

```csharp
public Array<RegExMatch> SearchAll(string subject, int offset = 0, int end = -1)
```

引擎名：`search_all`

Searches the text for the compiled pattern. Returns an array of [`RegExMatch`](RegExMatch.md) containers for each non-overlapping result. If no results were found, an empty array is returned instead.

The region to search within can be specified with `offset` and `end`. This is useful when searching for another match in the same `subject` by calling this method again after a previous success. Note that setting these parameters differs from passing over a shortened string. For example, the start anchor `^` is not affected by `offset`, and the character before `offset` will be checked for the word boundary `\b`.

### Sub

```csharp
public string Sub(string subject, string replacement, bool all = false, int offset = 0, int end = -1)
```

引擎名：`sub`

Searches the text for the compiled pattern and replaces it with the specified string. Escapes and backreferences such as `$1` and `$name` are expanded and resolved. By default, only the first instance is replaced, but it can be changed for all instances (global replacement).

The region to search within can be specified with `offset` and `end`. This is useful when searching for another match in the same `subject` by calling this method again after a previous success. Note that setting these parameters differs from passing over a shortened string. For example, the start anchor `^` is not affected by `offset`, and the character before `offset` will be checked for the word boundary `\b`.
