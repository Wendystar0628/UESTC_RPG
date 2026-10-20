<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RegExMatch

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Contains the results of a single [`RegEx`](RegEx.md) match returned by [`RegEx.Search`](RegEx.md) and [`RegEx.SearchAll`](RegEx.md). It can be used to find the position and range of the match and its capturing groups, and it can extract its substring for you.

## 构造函数

### RegExMatch

```csharp
public RegExMatch()
```

## 属性

### Names

```csharp
public Dictionary Names { get; }
```

引擎名：`names`

A dictionary of named groups and its corresponding group number. Only groups that were matched are included. If multiple groups have the same name, that name would refer to the first matching one.

### Strings

```csharp
public string[] Strings { get; }
```

引擎名：`strings`

An [`Collections.Array`](Collections.Array.md) of the match and its capturing groups.

### Subject

```csharp
public string Subject { get; }
```

引擎名：`subject`

The source string used with the search pattern to find this matching result.

## 方法

### GetEnd

```csharp
public int GetEnd(Variant? name = null)
```

引擎名：`get_end`

Returns the end position of the match within the source string. The end position of capturing groups can be retrieved by providing its group number as an integer or its string name (if it's a named group). The default value of 0 refers to the whole pattern.

Returns -1 if the group did not match or doesn't exist.

参数：
- `name`：If the parameter is null, then the default value is `(Variant)(0)`.

### GetGroupCount

```csharp
public int GetGroupCount()
```

引擎名：`get_group_count`

Returns the number of capturing groups.

### GetNames

```csharp
public Dictionary GetNames()
```

引擎名：`get_names`

### GetStart

```csharp
public int GetStart(Variant? name = null)
```

引擎名：`get_start`

Returns the starting position of the match within the source string. The starting position of capturing groups can be retrieved by providing its group number as an integer or its string name (if it's a named group). The default value of 0 refers to the whole pattern.

Returns -1 if the group did not match or doesn't exist.

参数：
- `name`：If the parameter is null, then the default value is `(Variant)(0)`.

### GetString

```csharp
public string GetString(Variant? name = null)
```

引擎名：`get_string`

Returns the substring of the match from the source string. Capturing groups can be retrieved by providing its group number as an integer or its string name (if it's a named group). The default value of 0 refers to the whole pattern.

Returns an empty string if the group did not match or doesn't exist.

参数：
- `name`：If the parameter is null, then the default value is `(Variant)(0)`.

### GetStrings

```csharp
public string[] GetStrings()
```

引擎名：`get_strings`

### GetSubject

```csharp
public string GetSubject()
```

引擎名：`get_subject`
