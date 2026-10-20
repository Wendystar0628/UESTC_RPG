<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RDTextureFormat

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This object is used by [`RenderingDevice`](RenderingDevice.md).

## 构造函数

### RDTextureFormat

```csharp
public RDTextureFormat()
```

## 属性

### ArrayLayers

```csharp
public uint ArrayLayers { get; set; }
```

引擎名：`array_layers`

The number of layers in the texture. Only relevant for 2D texture arrays.

### Depth

```csharp
public uint Depth { get; set; }
```

引擎名：`depth`

The texture's depth (in pixels). This is always `1` for 2D textures.

### Format

```csharp
public RenderingDevice.DataFormat Format { get; set; }
```

引擎名：`format`

The texture's pixel data format.

### Height

```csharp
public uint Height { get; set; }
```

引擎名：`height`

The texture's height (in pixels).

### IsDiscardable

```csharp
public bool IsDiscardable { get; set; }
```

引擎名：`is_discardable`

If a texture is discardable, its contents do not need to be preserved between frames. This flag is only relevant when the texture is used as target in a draw list.

This information is used by [`RenderingDevice`](RenderingDevice.md) to figure out if a texture's contents can be discarded, eliminating unnecessary writes to memory and boosting performance.

### IsResolveBuffer

```csharp
public bool IsResolveBuffer { get; set; }
```

引擎名：`is_resolve_buffer`

The texture will be used as the destination of a resolve operation.

### Mipmaps

```csharp
public uint Mipmaps { get; set; }
```

引擎名：`mipmaps`

The number of mipmaps available in the texture.

### Samples

```csharp
public RenderingDevice.TextureSamples Samples { get; set; }
```

引擎名：`samples`

The number of samples used when sampling the texture.

### TextureType

```csharp
public RenderingDevice.TextureType TextureType { get; set; }
```

引擎名：`texture_type`

The texture type.

### UsageBits

```csharp
public RenderingDevice.TextureUsageBits UsageBits { get; set; }
```

引擎名：`usage_bits`

The texture's usage bits, which determine what can be done using the texture.

### Width

```csharp
public uint Width { get; set; }
```

引擎名：`width`

The texture's width (in pixels).

## 方法

### AddShareableFormat

```csharp
public void AddShareableFormat(RenderingDevice.DataFormat format)
```

引擎名：`add_shareable_format`

Adds `format` as a valid format for the corresponding [`RDTextureView`](RDTextureView.md)'s [`RDTextureView.FormatOverride`](RDTextureView.md) property. If any format is added as shareable, then the main `Format` must also be added.

### GetArrayLayers

```csharp
public uint GetArrayLayers()
```

引擎名：`get_array_layers`

### GetDepth

```csharp
public uint GetDepth()
```

引擎名：`get_depth`

### GetFormat

```csharp
public RenderingDevice.DataFormat GetFormat()
```

引擎名：`get_format`

### GetHeight

```csharp
public uint GetHeight()
```

引擎名：`get_height`

### GetIsDiscardable

```csharp
public bool GetIsDiscardable()
```

引擎名：`get_is_discardable`

### GetIsResolveBuffer

```csharp
public bool GetIsResolveBuffer()
```

引擎名：`get_is_resolve_buffer`

### GetMipmaps

```csharp
public uint GetMipmaps()
```

引擎名：`get_mipmaps`

### GetSamples

```csharp
public RenderingDevice.TextureSamples GetSamples()
```

引擎名：`get_samples`

### GetTextureType

```csharp
public RenderingDevice.TextureType GetTextureType()
```

引擎名：`get_texture_type`

### GetUsageBits

```csharp
public RenderingDevice.TextureUsageBits GetUsageBits()
```

引擎名：`get_usage_bits`

### GetWidth

```csharp
public uint GetWidth()
```

引擎名：`get_width`

### RemoveShareableFormat

```csharp
public void RemoveShareableFormat(RenderingDevice.DataFormat format)
```

引擎名：`remove_shareable_format`

Removes `format` from the list of valid formats that the corresponding [`RDTextureView`](RDTextureView.md)'s [`RDTextureView.FormatOverride`](RDTextureView.md) property can be set to.

### SetArrayLayers

```csharp
public void SetArrayLayers(uint pMember)
```

引擎名：`set_array_layers`

### SetDepth

```csharp
public void SetDepth(uint pMember)
```

引擎名：`set_depth`

### SetFormat

```csharp
public void SetFormat(RenderingDevice.DataFormat pMember)
```

引擎名：`set_format`

### SetHeight

```csharp
public void SetHeight(uint pMember)
```

引擎名：`set_height`

### SetIsDiscardable

```csharp
public void SetIsDiscardable(bool pMember)
```

引擎名：`set_is_discardable`

### SetIsResolveBuffer

```csharp
public void SetIsResolveBuffer(bool pMember)
```

引擎名：`set_is_resolve_buffer`

### SetMipmaps

```csharp
public void SetMipmaps(uint pMember)
```

引擎名：`set_mipmaps`

### SetSamples

```csharp
public void SetSamples(RenderingDevice.TextureSamples pMember)
```

引擎名：`set_samples`

### SetTextureType

```csharp
public void SetTextureType(RenderingDevice.TextureType pMember)
```

引擎名：`set_texture_type`

### SetUsageBits

```csharp
public void SetUsageBits(RenderingDevice.TextureUsageBits pMember)
```

引擎名：`set_usage_bits`

### SetWidth

```csharp
public void SetWidth(uint pMember)
```

引擎名：`set_width`
