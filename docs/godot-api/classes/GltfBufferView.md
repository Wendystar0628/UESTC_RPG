<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GltfBufferView

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

GLTFBufferView is a data structure representing a glTF `bufferView` that would be found in the `"bufferViews"` array. A buffer is a blob of binary data. A buffer view is a slice of a buffer that can be used to identify and extract data from the buffer.

Most custom uses of buffers only need to use the `Buffer`, `ByteLength`, and `ByteOffset`. The `ByteStride` and `Indices` properties are for more advanced use cases such as interleaved mesh data encoded for the GPU.

## 构造函数

### GltfBufferView

```csharp
public GltfBufferView()
```

## 属性

### Buffer

```csharp
public int Buffer { get; set; }
```

The index of the buffer this buffer view is referencing. If `-1`, this buffer view is not referencing any buffer.

### ByteLength

```csharp
public long ByteLength { get; set; }
```

The length, in bytes, of this buffer view. If `0`, this buffer view is empty.

### ByteOffset

```csharp
public long ByteOffset { get; set; }
```

The offset, in bytes, from the start of the buffer to the start of this buffer view.

### ByteStride

```csharp
public long ByteStride { get; set; }
```

The stride, in bytes, between interleaved data. If `-1`, this buffer view is not interleaved.

### Indices

```csharp
public bool Indices { get; set; }
```

`true` if the GLTFBufferView's OpenGL GPU buffer type is an `ELEMENT_ARRAY_BUFFER` used for vertex indices (integer constant `34963`). `false` if the buffer type is any other value. See [Buffers, BufferViews, and Accessors](https://github.com/KhronosGroup/glTF-Tutorials/blob/master/gltfTutorial/gltfTutorial_005_BuffersBufferViewsAccessors.md) for possible values. This property is set on import and used on export.

### VertexAttributes

```csharp
public bool VertexAttributes { get; set; }
```

`true` if the GLTFBufferView's OpenGL GPU buffer type is an `ARRAY_BUFFER` used for vertex attributes (integer constant `34962`). `false` if the buffer type is any other value. See [Buffers, BufferViews, and Accessors](https://github.com/KhronosGroup/glTF-Tutorials/blob/master/gltfTutorial/gltfTutorial_005_BuffersBufferViewsAccessors.md) for possible values. This property is set on import and used on export.

## 方法

### FromDictionary

```csharp
public static GltfBufferView FromDictionary(Dictionary dictionary)
```

Creates a new GLTFBufferView instance by parsing the given [`Collections.Dictionary`](Collections.Dictionary.md).

### GetBuffer

```csharp
public int GetBuffer()
```

### GetByteLength

```csharp
public long GetByteLength()
```

### GetByteOffset

```csharp
public long GetByteOffset()
```

### GetByteStride

```csharp
public long GetByteStride()
```

### GetIndices

```csharp
public bool GetIndices()
```

### GetVertexAttributes

```csharp
public bool GetVertexAttributes()
```

### LoadBufferViewData

```csharp
public byte[] LoadBufferViewData(GltfState state)
```

Loads the buffer view data from the buffer referenced by this buffer view in the given [`GltfState`](GltfState.md). Interleaved data with a byte stride is not yet supported by this method. The data is returned as a `System.Byte`[].

### SetBuffer

```csharp
public void SetBuffer(int buffer)
```

### SetByteLength

```csharp
public void SetByteLength(long byteLength)
```

### SetByteOffset

```csharp
public void SetByteOffset(long byteOffset)
```

### SetByteStride

```csharp
public void SetByteStride(long byteStride)
```

### SetIndices

```csharp
public void SetIndices(bool indices)
```

### SetVertexAttributes

```csharp
public void SetVertexAttributes(bool isAttributes)
```

### ToDictionary

```csharp
public Dictionary ToDictionary()
```

Serializes this GLTFBufferView instance into a [`Collections.Dictionary`](Collections.Dictionary.md).
