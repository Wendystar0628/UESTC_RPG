<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RDVertexAttribute

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This object is used by [`RenderingDevice`](RenderingDevice.md).

## 构造函数

### RDVertexAttribute

```csharp
public RDVertexAttribute()
```

## 属性

### Binding

```csharp
public uint Binding { get; set; }
```

引擎名：`binding`

The index of the buffer in the vertex buffer array to bind this vertex attribute. When set to `-1`, it defaults to the index of the attribute.

**Note:** You cannot mix binding explicitly assigned attributes with implicitly assigned ones (i.e. `-1`). Either all attributes must have their binding set to `-1`, or all must have explicit bindings.

### Format

```csharp
public RenderingDevice.DataFormat Format { get; set; }
```

引擎名：`format`

The way that this attribute's data is interpreted when sent to a shader.

### Frequency

```csharp
public RenderingDevice.VertexFrequency Frequency { get; set; }
```

引擎名：`frequency`

The rate at which this attribute is pulled from its vertex buffer.

### Location

```csharp
public uint Location { get; set; }
```

引擎名：`location`

The location in the shader that this attribute is bound to.

### Offset

```csharp
public uint Offset { get; set; }
```

引擎名：`offset`

The number of bytes between the start of the vertex buffer and the first instance of this attribute.

### Stride

```csharp
public uint Stride { get; set; }
```

引擎名：`stride`

The number of bytes between the starts of consecutive instances of this attribute.

## 方法

### GetBinding

```csharp
public uint GetBinding()
```

引擎名：`get_binding`

### GetFormat

```csharp
public RenderingDevice.DataFormat GetFormat()
```

引擎名：`get_format`

### GetFrequency

```csharp
public RenderingDevice.VertexFrequency GetFrequency()
```

引擎名：`get_frequency`

### GetLocation

```csharp
public uint GetLocation()
```

引擎名：`get_location`

### GetOffset

```csharp
public uint GetOffset()
```

引擎名：`get_offset`

### GetStride

```csharp
public uint GetStride()
```

引擎名：`get_stride`

### SetBinding

```csharp
public void SetBinding(uint pMember)
```

引擎名：`set_binding`

### SetFormat

```csharp
public void SetFormat(RenderingDevice.DataFormat pMember)
```

引擎名：`set_format`

### SetFrequency

```csharp
public void SetFrequency(RenderingDevice.VertexFrequency pMember)
```

引擎名：`set_frequency`

### SetLocation

```csharp
public void SetLocation(uint pMember)
```

引擎名：`set_location`

### SetOffset

```csharp
public void SetOffset(uint pMember)
```

引擎名：`set_offset`

### SetStride

```csharp
public void SetStride(uint pMember)
```

引擎名：`set_stride`
