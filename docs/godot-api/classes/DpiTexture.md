<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# DpiTexture

命名空间 `Godot`；class；继承 [`Texture2D`](Texture2D.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

An automatically scalable [`Texture2D`](Texture2D.md) based on an SVG image. `DpiTexture`s are used to automatically re-rasterize icons and other texture based UI theme elements to match viewport scale and font oversampling. See also `ProjectSettings.display/window/stretch/mode` ("canvas_items" mode) and [`Viewport.OversamplingOverride`](Viewport.md).

## 构造函数

### DpiTexture

```csharp
public DpiTexture()
```

## 属性

### BaseScale

```csharp
public float BaseScale { get; set; }
```

Texture scale. `1.0` is the original SVG size. Higher values result in a larger image.

### ColorMap

```csharp
public Dictionary ColorMap { get; set; }
```

If set, remaps texture colors according to [`Color`](Color.md)-[`Color`](Color.md) map.

### FixAlphaBorder

```csharp
public bool FixAlphaBorder { get; set; }
```

If `true`, puts pixels of the same surrounding color in transition from transparent to opaque areas. For textures displayed with bilinear filtering, this helps to reduce the outline effect when exporting images from an image editor.

### PremultAlpha

```csharp
public bool PremultAlpha { get; set; }
```

An alternative to fixing darkened borders with `FixAlphaBorder` is to use premultiplied alpha. By enabling this option, the texture will be converted to this format. A premultiplied alpha texture requires specific materials to be displayed correctly:

- In 2D, a [`CanvasItemMaterial`](CanvasItemMaterial.md) will need to be created and configured to use the [`CanvasItemMaterial.BlendModeEnum.PremultAlpha`](CanvasItemMaterial.md) blend mode on [`CanvasItem`](CanvasItem.md)s that use this texture. In custom `canvas_item` shaders, `render_mode blend_premul_alpha;` should be used.

- In 3D, a [`BaseMaterial3D`](BaseMaterial3D.md) will need to be created and configured to use the [`BaseMaterial3D.BlendModeEnum.PremultAlpha`](BaseMaterial3D.md) blend mode on materials that use this texture. In custom `spatial` shaders, `render_mode blend_premul_alpha;` should be used.

### Saturation

```csharp
public float Saturation { get; set; }
```

Overrides texture saturation.

### _Source

```csharp
public string _Source { get; set; }
```

## 方法

### CreateFromString

```csharp
public static DpiTexture CreateFromString(string source, float scale = 1f, float saturation = 1f, Dictionary colorMap = null)
```

Creates a new `DpiTexture` and initializes it by allocating and setting the SVG data to `source`.

### GetBaseScale

```csharp
public float GetBaseScale()
```

### GetColorMap

```csharp
public Dictionary GetColorMap()
```

### GetFixAlphaBorder

```csharp
public bool GetFixAlphaBorder()
```

### GetPremultAlpha

```csharp
public bool GetPremultAlpha()
```

### GetSaturation

```csharp
public float GetSaturation()
```

### GetScaledRid

```csharp
public Rid GetScaledRid()
```

Returns the [`Rid`](Rid.md) of the texture rasterized to match the oversampling of the currently drawn canvas item.

### GetSource

```csharp
public string GetSource()
```

Returns this SVG texture's source code.

### SetBaseScale

```csharp
public void SetBaseScale(float baseScale)
```

### SetColorMap

```csharp
public void SetColorMap(Dictionary colorMap)
```

### SetFixAlphaBorder

```csharp
public void SetFixAlphaBorder(bool fixAlphaBorder)
```

### SetPremultAlpha

```csharp
public void SetPremultAlpha(bool premultAlpha)
```

### SetSaturation

```csharp
public void SetSaturation(float saturation)
```

### SetSizeOverride

```csharp
public void SetSizeOverride(Vector2I size)
```

Resizes the texture to the specified dimensions.

### SetSource

```csharp
public void SetSource(string source)
```

Sets this SVG texture's source code.
