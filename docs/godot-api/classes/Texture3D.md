<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Texture3D

命名空间 `Godot`；class；继承 [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Base class for [`ImageTexture3D`](ImageTexture3D.md) and [`CompressedTexture3D`](CompressedTexture3D.md). Cannot be used directly, but contains all the functions necessary for accessing the derived resource types. `Texture3D` is the base class for all 3-dimensional texture types. See also [`TextureLayered`](TextureLayered.md).

All images need to have the same width, height and number of mipmap levels.

To create such a texture file yourself, reimport your image files using the Godot Editor import presets.

## 构造函数

### Texture3D

```csharp
public Texture3D()
```

## 方法

### CreatePlaceholder

```csharp
public Resource CreatePlaceholder()
```

引擎名：`create_placeholder`

Creates a placeholder version of this resource ([`PlaceholderTexture3D`](PlaceholderTexture3D.md)).

### GetData

```csharp
public Array<Image> GetData()
```

引擎名：`get_data`

Returns the `Texture3D`'s data as an array of [`Image`](Image.md)s. Each [`Image`](Image.md) represents a *slice* of the `Texture3D`, with different slices mapping to different depth (Z axis) levels.

### GetDepth

```csharp
public int GetDepth()
```

引擎名：`get_depth`

Returns the `Texture3D`'s depth in pixels. Depth is typically represented by the Z axis (a dimension not present in [`Texture2D`](Texture2D.md)).

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

Returns the `Texture3D`'s height in pixels. Width is typically represented by the Y axis.

### GetWidth

```csharp
public int GetWidth()
```

引擎名：`get_width`

Returns the `Texture3D`'s width in pixels. Width is typically represented by the X axis.

### HasMipmaps

```csharp
public bool HasMipmaps()
```

引擎名：`has_mipmaps`

Returns `true` if the `Texture3D` has generated mipmaps.

### _GetData

```csharp
public virtual Array<Image> _GetData()
```

引擎名：`_get_data`

Called when the `Texture3D`'s data is queried.

### _GetDepth

```csharp
public virtual int _GetDepth()
```

引擎名：`_get_depth`

Called when the `Texture3D`'s depth is queried.

### _GetFormat

```csharp
public virtual Image.Format _GetFormat()
```

引擎名：`_get_format`

Called when the `Texture3D`'s format is queried.

### _GetHeight

```csharp
public virtual int _GetHeight()
```

引擎名：`_get_height`

Called when the `Texture3D`'s height is queried.

### _GetWidth

```csharp
public virtual int _GetWidth()
```

引擎名：`_get_width`

Called when the `Texture3D`'s width is queried.

### _HasMipmaps

```csharp
public virtual bool _HasMipmaps()
```

引擎名：`_has_mipmaps`

Called when the presence of mipmaps in the `Texture3D` is queried.
