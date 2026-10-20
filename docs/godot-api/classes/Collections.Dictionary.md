<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Dictionary

命名空间 `Godot.Collections`；class

Wrapper around Godot's Dictionary class, a dictionary of Variant
typed elements allocated in the engine in C++. Useful when
interfacing with the engine.

## 构造函数

### Dictionary

```csharp
public Dictionary()
```

Constructs a new empty `Collections.Dictionary`.

## 属性

### Count

```csharp
public int Count { get; }
```

Returns the number of elements in this `Collections.Dictionary`.
This is also known as the size or length of the dictionary.

返回：The number of elements.

### IsReadOnly

```csharp
public bool IsReadOnly { get; }
```

引擎名：`is_read_only`

Returns `true` if the dictionary is read-only.
See `MakeReadOnly()`.

### Item

```csharp
public Variant this[Variant key] { get; set; }
```

Returns the value at the given `key`.

### Keys

```csharp
public ICollection<Variant> Keys { get; }
```

引擎名：`keys`

Gets the collection of keys in this `Collections.Dictionary`.

### Values

```csharp
public ICollection<Variant> Values { get; }
```

引擎名：`values`

Gets the collection of elements in this `Collections.Dictionary`.

## 方法

### Add

```csharp
public void Add(Variant key, Variant value)
```

Adds an value `value` at key `key`
to this `Collections.Dictionary`.

参数：
- `key`：The key at which to add the value.
- `value`：The value to add.

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Clears the dictionary, removing all entries from it.

### ContainsKey

```csharp
public bool ContainsKey(Variant key)
```

Checks if this `Collections.Dictionary` contains the given key.

参数：
- `key`：The key to look for.

返回：Whether or not this dictionary contains the given key.

### Dispose

```csharp
public void Dispose()
```

Disposes of this `Collections.Dictionary`.

### Dispose

```csharp
public void Dispose(bool disposing)
```

### Duplicate

```csharp
public Dictionary Duplicate(bool deep = false)
```

引擎名：`duplicate`

Returns a copy of the `Collections.Dictionary`.
If `deep` is `true`, a deep copy is performed:
all nested arrays and dictionaries are duplicated and will not be shared with
the original dictionary. If `false`, a shallow copy is made and
references to the original nested arrays and dictionaries are kept, so that
modifying a sub-array or dictionary in the copy will also impact those
referenced in the source dictionary. Note that any [`GodotObject`](GodotObject.md) derived
elements will be shallow copied regardless of the `deep`
setting.

参数：
- `deep`：If `true`, performs a deep copy.

返回：A new Godot Dictionary.

### GetEnumerator

```csharp
public IEnumerator<KeyValuePair<Variant, Variant>> GetEnumerator()
```

Gets an enumerator for this `Collections.Dictionary`.

返回：An enumerator.

### MakeReadOnly

```csharp
public void MakeReadOnly()
```

引擎名：`make_read_only`

Makes the `Collections.Dictionary` read-only, i.e. disabled modying of the
dictionary's elements. Does not apply to nested content, e.g. content of
nested dictionaries.

### Merge

```csharp
public void Merge(Dictionary dictionary, bool overwrite = false)
```

引擎名：`merge`

Adds entries from `dictionary` to this dictionary.
By default, duplicate keys are not copied over, unless `overwrite`
is `true`.

参数：
- `dictionary`：Dictionary to copy entries from.
- `overwrite`：If duplicate keys should be copied over as well.

### RecursiveEqual

```csharp
public bool RecursiveEqual(Dictionary other)
```

引擎名：`recursive_equal`

Compares this `Collections.Dictionary` against the `other`
`Collections.Dictionary` recursively. Returns `true` if the
two dictionaries contain the same keys and values. The order of the entries
does not matter.
otherwise.

参数：
- `other`：The other dictionary to compare against.

返回：`true` if the dictionaries contain the same keys and values,
`false` otherwise.

### Remove

```csharp
public bool Remove(Variant key)
```

Removes an element from this `Collections.Dictionary` by key.

参数：
- `key`：The key of the element to remove.

### ToString

```csharp
public virtual string ToString()
```

Converts this `Collections.Dictionary` to a string.

返回：A string representation of this dictionary.

### TryGetValue

```csharp
public bool TryGetValue(Variant key, out Variant value)
```

Gets the value for the given `key` in the dictionary.
Returns `true` if an entry for the given key exists in
the dictionary; otherwise, returns `false`.

参数：
- `key`：The key of the element to get.
- `value`：The value at the given `key`.

返回：If an entry was found for the given `key`.
