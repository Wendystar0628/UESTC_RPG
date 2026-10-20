<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GltfAccessor

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

GLTFAccessor is a data structure representing a glTF `accessor` that would be found in the `"accessors"` array. A buffer is a blob of binary data. A buffer view is a slice of a buffer. An accessor is a typed interpretation of the data in a buffer view.

Most custom data stored in glTF does not need accessors, only buffer views (see [`GltfBufferView`](GltfBufferView.md)). Accessors are for more advanced use cases such as interleaved mesh data encoded for the GPU.

## 构造函数

### GltfAccessor

```csharp
public GltfAccessor()
```

## 属性

### AccessorType

```csharp
public GltfAccessor.GltfAccessorType AccessorType { get; set; }
```

The glTF accessor type, as an enum.

### BufferView

```csharp
public int BufferView { get; set; }
```

The index of the buffer view this accessor is referencing. If `-1`, this accessor is not referencing any buffer view.

### ByteOffset

```csharp
public long ByteOffset { get; set; }
```

The offset relative to the start of the buffer view in bytes.

### ComponentType

```csharp
public GltfAccessor.GltfComponentType ComponentType { get; set; }
```

The glTF component type as an enum. See [`GltfAccessor.GltfComponentType`](GltfAccessor.md) for possible values. Within the core glTF specification, a value of 5125 or "UNSIGNED_INT" must not be used for any accessor that is not referenced by mesh.primitive.indices.

### Count

```csharp
public long Count { get; set; }
```

The number of elements referenced by this accessor.

### Max

```csharp
public double[] Max { get; set; }
```

Maximum value of each component in this accessor.

### Min

```csharp
public double[] Min { get; set; }
```

Minimum value of each component in this accessor.

### Normalized

```csharp
public bool Normalized { get; set; }
```

Specifies whether integer data values are normalized before usage.

### SparseCount

```csharp
public long SparseCount { get; set; }
```

Number of deviating accessor values stored in the sparse array.

### SparseIndicesBufferView

```csharp
public int SparseIndicesBufferView { get; set; }
```

The index of the buffer view with sparse indices. The referenced buffer view MUST NOT have its target or byteStride properties defined. The buffer view and the optional byteOffset MUST be aligned to the componentType byte length.

### SparseIndicesByteOffset

```csharp
public long SparseIndicesByteOffset { get; set; }
```

The offset relative to the start of the buffer view in bytes.

### SparseIndicesComponentType

```csharp
public GltfAccessor.GltfComponentType SparseIndicesComponentType { get; set; }
```

The indices component data type as an enum. Possible values are 5121 for "UNSIGNED_BYTE", 5123 for "UNSIGNED_SHORT", and 5125 for "UNSIGNED_INT".

### SparseValuesBufferView

```csharp
public int SparseValuesBufferView { get; set; }
```

The index of the bufferView with sparse values. The referenced buffer view MUST NOT have its target or byteStride properties defined.

### SparseValuesByteOffset

```csharp
public long SparseValuesByteOffset { get; set; }
```

The offset relative to the start of the bufferView in bytes.

### Type

```csharp
public int Type { get; set; }
```

The glTF accessor type, as an `System.Int32`. Possible values are `0` for "SCALAR", `1` for "VEC2", `2` for "VEC3", `3` for "VEC4", `4` for "MAT2", `5` for "MAT3", and `6` for "MAT4".

## 方法

### FromDictionary

```csharp
public static GltfAccessor FromDictionary(Dictionary dictionary)
```

Creates a new GLTFAccessor instance by parsing the given [`Collections.Dictionary`](Collections.Dictionary.md).

### GetAccessorType

```csharp
public GltfAccessor.GltfAccessorType GetAccessorType()
```

### GetBufferView

```csharp
public int GetBufferView()
```

### GetByteOffset

```csharp
public long GetByteOffset()
```

### GetComponentType

```csharp
public GltfAccessor.GltfComponentType GetComponentType()
```

### GetCount

```csharp
public long GetCount()
```

### GetMax

```csharp
public double[] GetMax()
```

### GetMin

```csharp
public double[] GetMin()
```

### GetNormalized

```csharp
public bool GetNormalized()
```

### GetSparseCount

```csharp
public long GetSparseCount()
```

### GetSparseIndicesBufferView

```csharp
public int GetSparseIndicesBufferView()
```

### GetSparseIndicesByteOffset

```csharp
public long GetSparseIndicesByteOffset()
```

### GetSparseIndicesComponentType

```csharp
public GltfAccessor.GltfComponentType GetSparseIndicesComponentType()
```

### GetSparseValuesBufferView

```csharp
public int GetSparseValuesBufferView()
```

### GetSparseValuesByteOffset

```csharp
public long GetSparseValuesByteOffset()
```

### GetType

```csharp
public int GetType()
```

### SetAccessorType

```csharp
public void SetAccessorType(GltfAccessor.GltfAccessorType accessorType)
```

### SetBufferView

```csharp
public void SetBufferView(int bufferView)
```

### SetByteOffset

```csharp
public void SetByteOffset(long byteOffset)
```

### SetComponentType

```csharp
public void SetComponentType(GltfAccessor.GltfComponentType componentType)
```

### SetComponentType

```csharp
public void SetComponentType(int componentType)
```

### SetCount

```csharp
public void SetCount(long count)
```

### SetMax

```csharp
public void SetMax(ReadOnlySpan<double> max)
```

### SetMax

```csharp
public void SetMax(double[] max)
```

### SetMin

```csharp
public void SetMin(ReadOnlySpan<double> min)
```

### SetMin

```csharp
public void SetMin(double[] min)
```

### SetNormalized

```csharp
public void SetNormalized(bool normalized)
```

### SetSparseCount

```csharp
public void SetSparseCount(long sparseCount)
```

### SetSparseIndicesBufferView

```csharp
public void SetSparseIndicesBufferView(int sparseIndicesBufferView)
```

### SetSparseIndicesByteOffset

```csharp
public void SetSparseIndicesByteOffset(long sparseIndicesByteOffset)
```

### SetSparseIndicesComponentType

```csharp
public void SetSparseIndicesComponentType(GltfAccessor.GltfComponentType sparseIndicesComponentType)
```

### SetSparseIndicesComponentType

```csharp
public void SetSparseIndicesComponentType(int sparseIndicesComponentType)
```

### SetSparseValuesBufferView

```csharp
public void SetSparseValuesBufferView(int sparseValuesBufferView)
```

### SetSparseValuesByteOffset

```csharp
public void SetSparseValuesByteOffset(long sparseValuesByteOffset)
```

### SetType

```csharp
public void SetType(int type)
```

### ToDictionary

```csharp
public Dictionary ToDictionary()
```

Serializes this GLTFAccessor instance into a [`Collections.Dictionary`](Collections.Dictionary.md).

## GltfAccessorType

命名空间 `Godot`；enum

### 枚举值

- `Scalar = 0`：Accessor type "SCALAR".
- `Vec2 = 1`：Accessor type "VEC2".
- `Vec3 = 2`：Accessor type "VEC3".
- `Vec4 = 3`：Accessor type "VEC4".
- `Mat2 = 4`：Accessor type "MAT2".
- `Mat3 = 5`：Accessor type "MAT3".
- `Mat4 = 6`：Accessor type "MAT4".

## GltfComponentType

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：Component type "NONE".
- `SignedByte = 5120`：Component type "BYTE".
- `UnsignedByte = 5121`：Component type "UNSIGNED_BYTE".
- `SignedShort = 5122`：Component type "SHORT".
- `UnsignedShort = 5123`：Component type "UNSIGNED_SHORT".
- `SignedInt = 5124`：Component type "INT".
- `UnsignedInt = 5125`：Component type "UNSIGNED_INT".
- `SingleFloat = 5126`：Component type "FLOAT".
- `DoubleFloat = 5130`：Component type "DOUBLE".
- `HalfFloat = 5131`：Component type "HALF_FLOAT".
- `SignedLong = 5134`：Component type "LONG".
- `UnsignedLong = 5135`：Component type "UNSIGNED_LONG".
