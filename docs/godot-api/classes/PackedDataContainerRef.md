<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PackedDataContainerRef

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

When packing nested containers using [`PackedDataContainer`](PackedDataContainer.md), they are recursively packed into `PackedDataContainerRef` (only applies to [`Collections.Array`](Collections.Array.md) and [`Collections.Dictionary`](Collections.Dictionary.md)). Their data can be retrieved the same way as from [`PackedDataContainer`](PackedDataContainer.md).

```csharp
var packed = PackedDataContainer.new()
packed.pack([1, 2, 3, ["nested1", "nested2"], 4, 5, 6])

for element in packed:
if element is PackedDataContainerRef:
for subelement in element:
print("::", subelement)
else:
print(element)

```

Prints:

```csharp
1
2
3
::nested1
::nested2
4
5
6

```

## 方法

### Size

```csharp
public int Size()
```

引擎名：`size`

Returns the size of the packed container (see `Array.size` and `Dictionary.size`).
