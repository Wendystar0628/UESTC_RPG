<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RDAccelerationStructureGeometry

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`RDAccelerationStructureGeometry` describes a set of triangles used as raytracing geometry in the [`RenderingDevice.BlasCreate`](RenderingDevice.md) method.

The geometry is always in triangle list form, either indexed or non-indexed. Triangle strips are not supported.

## 构造函数

### RDAccelerationStructureGeometry

```csharp
public RDAccelerationStructureGeometry()
```

## 属性

### Flags

```csharp
public RenderingDevice.AccelerationStructureGeometryFlagBits Flags { get; set; }
```

引擎名：`flags`

Flags for the geometry.

### IndexBuffer

```csharp
public Rid IndexBuffer { get; set; }
```

引擎名：`index_buffer`

Buffer containing vertex indices. If `null`, triangles are non-indexed.

### IndexCount

```csharp
public uint IndexCount { get; set; }
```

引擎名：`index_count`

Number of indices used by this geometry in `IndexBuffer`.

### IndexOffset

```csharp
public uint IndexOffset { get; set; }
```

引擎名：`index_offset`

Byte offset of the first index in `IndexBuffer`.

### VertexBuffer

```csharp
public Rid VertexBuffer { get; set; }
```

引擎名：`vertex_buffer`

Buffer containing vertices.

### VertexCount

```csharp
public uint VertexCount { get; set; }
```

引擎名：`vertex_count`

Number of vertices used by this geometry in `VertexBuffer`.

### VertexFormat

```csharp
public RenderingDevice.DataFormat VertexFormat { get; set; }
```

引擎名：`vertex_format`

Format of the vertices in `VertexBuffer`.

### VertexOffset

```csharp
public uint VertexOffset { get; set; }
```

引擎名：`vertex_offset`

Byte offset of the first vertex in `VertexBuffer`.

### VertexStride

```csharp
public uint VertexStride { get; set; }
```

引擎名：`vertex_stride`

Number of bytes between each vertex in `VertexBuffer`.

## 方法

### GetFlags

```csharp
public RenderingDevice.AccelerationStructureGeometryFlagBits GetFlags()
```

引擎名：`get_flags`

### GetIndexBuffer

```csharp
public Rid GetIndexBuffer()
```

引擎名：`get_index_buffer`

### GetIndexCount

```csharp
public uint GetIndexCount()
```

引擎名：`get_index_count`

### GetIndexOffset

```csharp
public uint GetIndexOffset()
```

引擎名：`get_index_offset`

### GetVertexBuffer

```csharp
public Rid GetVertexBuffer()
```

引擎名：`get_vertex_buffer`

### GetVertexCount

```csharp
public uint GetVertexCount()
```

引擎名：`get_vertex_count`

### GetVertexFormat

```csharp
public RenderingDevice.DataFormat GetVertexFormat()
```

引擎名：`get_vertex_format`

### GetVertexOffset

```csharp
public uint GetVertexOffset()
```

引擎名：`get_vertex_offset`

### GetVertexStride

```csharp
public uint GetVertexStride()
```

引擎名：`get_vertex_stride`

### SetFlags

```csharp
public void SetFlags(RenderingDevice.AccelerationStructureGeometryFlagBits pMember)
```

引擎名：`set_flags`

### SetIndexBuffer

```csharp
public void SetIndexBuffer(Rid pMember)
```

引擎名：`set_index_buffer`

### SetIndexCount

```csharp
public void SetIndexCount(uint pMember)
```

引擎名：`set_index_count`

### SetIndexOffset

```csharp
public void SetIndexOffset(uint pMember)
```

引擎名：`set_index_offset`

### SetVertexBuffer

```csharp
public void SetVertexBuffer(Rid pMember)
```

引擎名：`set_vertex_buffer`

### SetVertexCount

```csharp
public void SetVertexCount(uint pMember)
```

引擎名：`set_vertex_count`

### SetVertexFormat

```csharp
public void SetVertexFormat(RenderingDevice.DataFormat pMember)
```

引擎名：`set_vertex_format`

### SetVertexOffset

```csharp
public void SetVertexOffset(uint pMember)
```

引擎名：`set_vertex_offset`

### SetVertexStride

```csharp
public void SetVertexStride(uint pMember)
```

引擎名：`set_vertex_stride`
