<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TextureLayered

命名空间 `Godot`；class；继承 [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Base class for [`ImageTextureLayered`](ImageTextureLayered.md) and [`CompressedTextureLayered`](CompressedTextureLayered.md). Cannot be used directly, but contains all the functions necessary for accessing the derived resource types. See also [`Texture3D`](Texture3D.md).

Data is set on a per-layer basis. For [`Texture2DArray`](Texture2DArray.md)s, the layer specifies the array layer.

All images need to have the same width, height and number of mipmap levels.

A `TextureLayered` can be loaded with [`ResourceLoader.Load`](ResourceLoader.md).

Internally, Godot maps these files to their respective counterparts in the target rendering driver (Vulkan, OpenGL3).

## 构造函数

### TextureLayered

```csharp
public TextureLayered()
```

## 方法

### GetFormat

```csharp
public Image.Format GetFormat()
```

引擎名：`get_format`

Returns the current format being used by this texture.

### GetHeight

```csharp
public int GetHeight()
```

引擎名：`get_height`

Returns the height of the texture in pixels. Height is typically represented by the Y axis.

### GetLayerData

```csharp
public Image GetLayerData(int layer)
```

引擎名：`get_layer_data`

Returns an [`Image`](Image.md) resource with the data from specified `layer`.

### GetLayeredType

```csharp
public TextureLayered.LayeredType GetLayeredType()
```

引擎名：`get_layered_type`

Returns the `TextureLayered`'s type. The type determines how the data is accessed, with cubemaps having special types.

### GetLayers

```csharp
public int GetLayers()
```

引擎名：`get_layers`

Returns the number of referenced [`Image`](Image.md)s.

### GetWidth

```csharp
public int GetWidth()
```

引擎名：`get_width`

Returns the width of the texture in pixels. Width is typically represented by the X axis.

### HasMipmaps

```csharp
public bool HasMipmaps()
```

引擎名：`has_mipmaps`

Returns `true` if the layers have generated mipmaps.

### _GetFormat

```csharp
public virtual Image.Format _GetFormat()
```

引擎名：`_get_format`

Called when the `TextureLayered`'s format is queried.

### _GetHeight

```csharp
public virtual int _GetHeight()
```

引擎名：`_get_height`

Called when the `TextureLayered`'s height is queried.

### _GetLayerData

```csharp
public virtual Image _GetLayerData(int layerIndex)
```

引擎名：`_get_layer_data`

Called when the data for a layer in the `TextureLayered` is queried.

### _GetLayeredType

```csharp
public virtual uint _GetLayeredType()
```

引擎名：`_get_layered_type`

Called when the layers' type in the `TextureLayered` is queried.

### _GetLayers

```csharp
public virtual int _GetLayers()
```

引擎名：`_get_layers`

Called when the number of layers in the `TextureLayered` is queried.

### _GetWidth

```csharp
public virtual int _GetWidth()
```

引擎名：`_get_width`

Called when the `TextureLayered`'s width queried.

### _HasMipmaps

```csharp
public virtual bool _HasMipmaps()
```

引擎名：`_has_mipmaps`

Called when the presence of mipmaps in the `TextureLayered` is queried.

## LayeredType

命名空间 `Godot`；enum

### 枚举值

- `Type2DArray = 0`：Texture is a generic [`Texture2DArray`](Texture2DArray.md).
- `Cubemap = 1`：Texture is a [`Cubemap`](Cubemap.md), with each side in its own layer (6 in total).
- `CubemapArray = 2`：Texture is a [`CubemapArray`](CubemapArray.md), with each cubemap being made of 6 layers.
