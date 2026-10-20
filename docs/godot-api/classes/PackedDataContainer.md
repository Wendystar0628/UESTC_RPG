<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PackedDataContainer

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`PackedDataContainer` can be used to efficiently store data from untyped containers. The data is packed into raw bytes and can be saved to file. Only [`Collections.Array`](Collections.Array.md) and [`Collections.Dictionary`](Collections.Dictionary.md) can be stored this way.

You can retrieve the data by iterating on the container, which will work as if iterating on the packed data itself. If the packed container is a [`Collections.Dictionary`](Collections.Dictionary.md), the data can be retrieved by key names (`System.String`/[`StringName`](StringName.md) only).

```csharp
var data = { "key": "value", "another_key": 123, "lock": Vector2() }
var packed = PackedDataContainer.new()
packed.pack(data)
ResourceSaver.save(packed, "packed_data.res")

```

```csharp
var container = load("packed_data.res")
for key in container:
prints(key, container[key])

```

Prints:

```csharp
key value
lock (0, 0)
another_key 123

```

Nested containers will be packed recursively. While iterating, they will be returned as [`PackedDataContainerRef`](PackedDataContainerRef.md).

## 构造函数

### PackedDataContainer

```csharp
public PackedDataContainer()
```

## 属性

### __Data___

```csharp
public byte[] __Data___ { get; set; }
```

## 方法

### Pack

```csharp
public Error Pack(Variant value)
```

引擎名：`pack`

Packs the given container into a binary representation. The `value` must be either [`Collections.Array`](Collections.Array.md) or [`Collections.Dictionary`](Collections.Dictionary.md), any other type will result in invalid data error.

**Note:** Subsequent calls to this method will overwrite the existing data.

### Size

```csharp
public int Size()
```

引擎名：`size`

Returns the size of the packed container (see `Array.size` and `Dictionary.size`).
