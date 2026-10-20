<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Texture2D

命名空间 `Godot`；class；继承 [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A texture works by registering an image in the video hardware, which then can be used in 3D models or 2D [`Sprite2D`](Sprite2D.md) or GUI [`Control`](Control.md).

Textures are often created by loading them from a file. See `@GDScript.load`.

`Texture2D` is a base for other resources. It cannot be used directly.

**Note:** The maximum texture size is 16384×16384 pixels due to graphics hardware limitations. Larger textures may fail to import.

## 构造函数

### Texture2D

```csharp
public Texture2D()
```

## 方法

### CreatePlaceholder

```csharp
public Resource CreatePlaceholder()
```

引擎名：`create_placeholder`

Creates a placeholder version of this resource ([`PlaceholderTexture2D`](PlaceholderTexture2D.md)).

### Draw

```csharp
public void Draw(Rid canvasItem, Vector2 position, Color? modulate = null, bool transpose = false)
```

引擎名：`draw`

Draws the texture using a [`CanvasItem`](CanvasItem.md) with the [`RenderingServer`](RenderingServer.md) API at the specified `position`.

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawRect

```csharp
public void DrawRect(Rid canvasItem, Rect2 rect, bool tile, Color? modulate = null, bool transpose = false)
```

引擎名：`draw_rect`

Draws the texture using a [`CanvasItem`](CanvasItem.md) with the [`RenderingServer`](RenderingServer.md) API.

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### DrawRectRegion

```csharp
public void DrawRectRegion(Rid canvasItem, Rect2 rect, Rect2 srcRect, Color? modulate = null, bool transpose = false, bool clipUV = true)
```

引擎名：`draw_rect_region`

Draws a part of the texture using a [`CanvasItem`](CanvasItem.md) with the [`RenderingServer`](RenderingServer.md) API.

参数：
- `modulate`：If the parameter is null, then the default value is `new Color(1.0f, 1.0f, 1.0f, 1.0f)`.

### GetFormat

```csharp
public Image.Format GetFormat()
```

引擎名：`get_format`

Returns the image format of the texture.

### GetHeight

```csharp
public int GetHeight()
```

引擎名：`get_height`

Returns the texture height in pixels.

### GetImage

```csharp
public Image GetImage()
```

引擎名：`get_image`

Returns an [`Image`](Image.md) that is a copy of data from this `Texture2D` (a new [`Image`](Image.md) is created each time). [`Image`](Image.md)s can be accessed and manipulated directly.

**Note:** This will return `null` if this `Texture2D` is invalid.

**Note:** This will fetch the texture data from the GPU, which might cause performance problems when overused. Avoid calling `GetImage()` every frame, especially on large textures.

### GetMipmapCount

```csharp
public int GetMipmapCount()
```

引擎名：`get_mipmap_count`

Returns the number of mipmaps of the texture.

### GetSize

```csharp
public Vector2 GetSize()
```

引擎名：`get_size`

Returns the texture size in pixels.

### GetWidth

```csharp
public int GetWidth()
```

引擎名：`get_width`

Returns the texture width in pixels.

### HasAlpha

```csharp
public bool HasAlpha()
```

引擎名：`has_alpha`

Returns `true` if this `Texture2D` has an alpha channel.

### HasMipmaps

```csharp
public bool HasMipmaps()
```

引擎名：`has_mipmaps`

Returns `true` if the texture has mipmaps.

### _Draw

```csharp
public virtual void _Draw(Rid toCanvasItem, Vector2 pos, Color modulate, bool transpose)
```

引擎名：`_draw`

Called when the entire `Texture2D` is requested to be drawn over a [`CanvasItem`](CanvasItem.md), with the top-left offset specified in `pos`. `modulate` specifies a multiplier for the colors being drawn, while `transpose` specifies whether drawing should be performed in column-major order instead of row-major order (resulting in 90-degree clockwise rotation).

**Note:** This is only used in 2D rendering, not 3D.

### _DrawRect

```csharp
public virtual void _DrawRect(Rid toCanvasItem, Rect2 rect, bool tile, Color modulate, bool transpose)
```

引擎名：`_draw_rect`

Called when the `Texture2D` is requested to be drawn onto [`CanvasItem`](CanvasItem.md)'s specified `rect`. `modulate` specifies a multiplier for the colors being drawn, while `transpose` specifies whether drawing should be performed in column-major order instead of row-major order (resulting in 90-degree clockwise rotation).

**Note:** This is only used in 2D rendering, not 3D.

### _DrawRectRegion

```csharp
public virtual void _DrawRectRegion(Rid toCanvasItem, Rect2 rect, Rect2 srcRect, Color modulate, bool transpose, bool clipUV)
```

引擎名：`_draw_rect_region`

Called when a part of the `Texture2D` specified by `srcRect`'s coordinates is requested to be drawn onto [`CanvasItem`](CanvasItem.md)'s specified `rect`. `modulate` specifies a multiplier for the colors being drawn, while `transpose` specifies whether drawing should be performed in column-major order instead of row-major order (resulting in 90-degree clockwise rotation).

**Note:** This is only used in 2D rendering, not 3D.

### _GetFormat

```csharp
public virtual Image.Format _GetFormat()
```

引擎名：`_get_format`

Called when `GetFormat()` is called.

### _GetHeight

```csharp
public virtual int _GetHeight()
```

引擎名：`_get_height`

Called when the `Texture2D`'s height is queried.

### _GetImage

```csharp
public virtual Image _GetImage()
```

引擎名：`_get_image`

Called when `GetImage()` is called.

### _GetMipmapCount

```csharp
public virtual int _GetMipmapCount()
```

引擎名：`_get_mipmap_count`

Called when `GetMipmapCount()` is called.

### _GetWidth

```csharp
public virtual int _GetWidth()
```

引擎名：`_get_width`

Called when the `Texture2D`'s width is queried.

### _HasAlpha

```csharp
public virtual bool _HasAlpha()
```

引擎名：`_has_alpha`

Called when the presence of an alpha channel in the `Texture2D` is queried.

### _HasMipmaps

```csharp
public virtual bool _HasMipmaps()
```

引擎名：`_has_mipmaps`

Called when `HasMipmaps()` is called.

### _IsPixelOpaque

```csharp
public virtual bool _IsPixelOpaque(int x, int y)
```

引擎名：`_is_pixel_opaque`

Called when a pixel's opaque state in the `Texture2D` is queried at the specified `(x, y)` position.
