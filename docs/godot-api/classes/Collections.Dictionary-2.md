<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Dictionary<TKey, TValue>

命名空间 `Godot.Collections`；class

Typed wrapper around Godot's Dictionary class, a dictionary of `TKey`
and `TValue` annotated, Variant typed elements allocated in the engine in C++.
Useful when interfacing with the engine. Otherwise prefer .NET collections
such as `System.Collections.Generic.Dictionary`.

## 构造函数

### Dictionary

```csharp
public Dictionary()
```

Constructs a new empty [`Collections.Dictionary`](Collections.Dictionary.md).

返回：A new Godot Dictionary.

### Dictionary

```csharp
public Dictionary(Dictionary dictionary)
```

Constructs a new [`Collections.Dictionary`](Collections.Dictionary.md) from the given dictionary's elements.

参数：
- `dictionary`：The dictionary to construct from.

返回：A new Godot Dictionary.

### Dictionary

```csharp
public Dictionary(IDictionary<TKey, TValue> dictionary)
```

Constructs a new [`Collections.Dictionary`](Collections.Dictionary.md) from the given dictionary's elements.

参数：
- `dictionary`：The dictionary to construct from.

返回：A new Godot Dictionary.

## 属性

### Count

```csharp
public int Count { get; }
```

Returns the number of elements in this [`Collections.Dictionary`](Collections.Dictionary.md).
This is also known as the size or length of the dictionary.

返回：The number of elements.

### IsReadOnly

```csharp
public bool IsReadOnly { get; }
```

引擎名：`is_read_only`

Returns `true` if the dictionary is read-only.
See [`Collections.Dictionary.MakeReadOnly()`](Collections.Dictionary.md).

### Item

```csharp
public TValue this[TKey key] { get; set; }
```

Returns the value at the given `key`.

### Keys

```csharp
public ICollection<TKey> Keys { get; }
```

引擎名：`keys`

Gets the collection of keys in this [`Collections.Dictionary`](Collections.Dictionary.md).

### Values

```csharp
public ICollection<TValue> Values { get; }
```

引擎名：`values`

Gets the collection of elements in this [`Collections.Dictionary`](Collections.Dictionary.md).

## 方法

### Add

```csharp
public void Add(TKey key, TValue value)
```

Adds an object `value` at key `key`
to this [`Collections.Dictionary`](Collections.Dictionary.md).

参数：
- `key`：The key at which to add the object.
- `value`：The object to add.

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Clears the dictionary, removing all entries from it.

### ContainsKey

```csharp
public bool ContainsKey(TKey key)
```

Checks if this [`Collections.Dictionary`](Collections.Dictionary.md) contains the given key.

参数：
- `key`：The key to look for.

返回：Whether or not this dictionary contains the given key.

### Duplicate

```csharp
public Dictionary<TKey, TValue> Duplicate(bool deep = false)
```

引擎名：`duplicate`

Returns a copy of the [`Collections.Dictionary`](Collections.Dictionary.md).
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
public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
```

Gets an enumerator for this [`Collections.Dictionary`](Collections.Dictionary.md).

返回：An enumerator.

### MakeReadOnly

```csharp
public void MakeReadOnly()
```

引擎名：`make_read_only`

Makes the [`Collections.Dictionary`](Collections.Dictionary.md) read-only, i.e. disabled
modying of the dictionary's elements. Does not apply to nested content,
e.g. content of nested dictionaries.

### Merge

```csharp
public void Merge(Dictionary<TKey, TValue> dictionary, bool overwrite = false)
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
public bool RecursiveEqual(Dictionary<TKey, TValue> other)
```

引擎名：`recursive_equal`

Compares this [`Collections.Dictionary`](Collections.Dictionary.md) against the `other`
[`Collections.Dictionary`](Collections.Dictionary.md) recursively. Returns `true` if the
two dictionaries contain the same keys and values. The order of the entries does not matter.
otherwise.

参数：
- `other`：The other dictionary to compare against.

返回：`true` if the dictionaries contain the same keys and values,
`false` otherwise.

### Remove

```csharp
public bool Remove(TKey key)
```

Removes an element from this [`Collections.Dictionary`](Collections.Dictionary.md) by key.

参数：
- `key`：The key of the element to remove.

### ToString

```csharp
public virtual string ToString()
```

Converts this [`Collections.Dictionary`](Collections.Dictionary.md) to a string.

返回：A string representation of this dictionary.

### TryGetValue

```csharp
public bool TryGetValue(TKey key, out TValue value)
```

Gets the value for the given `key` in the dictionary.
Returns `true` if an entry for the given key exists in
the dictionary; otherwise, returns `false`.

参数：
- `key`：The key of the element to get.
- `value`：The value at the given `key`.

返回：If an entry was found for the given `key`.

## 运算符

### op_Explicit

```csharp
public static Dictionary op_Explicit(Dictionary<TKey, TValue> from)
```

Converts this typed [`Collections.Dictionary`](Collections.Dictionary.md) to an untyped [`Collections.Dictionary`](Collections.Dictionary.md).

参数：
- `from`：The typed dictionary to convert.

返回：A new Godot Dictionary, or `null` if  was null.

### op_Explicit

```csharp
public static Dictionary<TKey, TValue> op_Explicit(Variant from)
```

### op_Implicit

```csharp
public static Variant op_Implicit(Dictionary<TKey, TValue> from)
```
