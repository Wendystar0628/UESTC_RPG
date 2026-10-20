<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CanvasTexture

命名空间 `Godot`；class；继承 [`Texture2D`](Texture2D.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`CanvasTexture` is an alternative to [`ImageTexture`](ImageTexture.md) for 2D rendering. It allows using normal maps and specular maps in any node that inherits from [`CanvasItem`](CanvasItem.md). `CanvasTexture` also allows overriding the texture's filter and repeat mode independently of the node's properties (or the project settings).

**Note:** `CanvasTexture` cannot be used in 3D. It will not display correctly when applied to any [`VisualInstance3D`](VisualInstance3D.md), such as [`Sprite3D`](Sprite3D.md) or [`Decal`](Decal.md). For physically-based materials in 3D, use [`BaseMaterial3D`](BaseMaterial3D.md) instead.

## 构造函数

### CanvasTexture

```csharp
public CanvasTexture()
```

## 属性

### DiffuseTexture

```csharp
public Texture2D DiffuseTexture { get; set; }
```

引擎名：`diffuse_texture`

The diffuse (color) texture to use. This is the main texture you want to set in most cases.

### NormalTexture

```csharp
public Texture2D NormalTexture { get; set; }
```

引擎名：`normal_texture`

The normal map texture to use. Only has a visible effect if [`Light2D`](Light2D.md)s are affecting this `CanvasTexture`.

**Note:** Godot expects the normal map to use X+, Y+, and Z+ coordinates. See [this page](http://wiki.polycount.com/wiki/Normal_Map_Technical_Details#Common_Swizzle_Coordinates) for a comparison of normal map coordinates expected by popular engines.

### SpecularColor

```csharp
public Color SpecularColor { get; set; }
```

引擎名：`specular_color`

The multiplier for specular reflection colors. The [`Light2D`](Light2D.md)'s color is also taken into account when determining the reflection color. Only has a visible effect if [`Light2D`](Light2D.md)s are affecting this `CanvasTexture`.

### SpecularShininess

```csharp
public float SpecularShininess { get; set; }
```

引擎名：`specular_shininess`

The specular exponent for [`Light2D`](Light2D.md) specular reflections. Higher values result in a more glossy/"wet" look, with reflections becoming more localized and less visible overall. The default value of `1.0` disables specular reflections entirely. Only has a visible effect if [`Light2D`](Light2D.md)s are affecting this `CanvasTexture`.

### SpecularTexture

```csharp
public Texture2D SpecularTexture { get; set; }
```

引擎名：`specular_texture`

The specular map to use for [`Light2D`](Light2D.md) specular reflections. This should be a grayscale or colored texture, with brighter areas resulting in a higher `SpecularShininess` value. Using a colored `SpecularTexture` allows controlling specular shininess on a per-channel basis. Only has a visible effect if [`Light2D`](Light2D.md)s are affecting this `CanvasTexture`.

### TextureFilter

```csharp
public CanvasItem.TextureFilterEnum TextureFilter { get; set; }
```

引擎名：`texture_filter`

The texture filtering mode to use when drawing this `CanvasTexture`.

### TextureRepeat

```csharp
public CanvasItem.TextureRepeatEnum TextureRepeat { get; set; }
```

引擎名：`texture_repeat`

The texture repeat mode to use when drawing this `CanvasTexture`.

## 方法

### GetDiffuseTexture

```csharp
public Texture2D GetDiffuseTexture()
```

引擎名：`get_diffuse_texture`

### GetNormalTexture

```csharp
public Texture2D GetNormalTexture()
```

引擎名：`get_normal_texture`

### GetSpecularColor

```csharp
public Color GetSpecularColor()
```

引擎名：`get_specular_color`

### GetSpecularShininess

```csharp
public float GetSpecularShininess()
```

引擎名：`get_specular_shininess`

### GetSpecularTexture

```csharp
public Texture2D GetSpecularTexture()
```

引擎名：`get_specular_texture`

### GetTextureFilter

```csharp
public CanvasItem.TextureFilterEnum GetTextureFilter()
```

引擎名：`get_texture_filter`

### GetTextureRepeat

```csharp
public CanvasItem.TextureRepeatEnum GetTextureRepeat()
```

引擎名：`get_texture_repeat`

### SetDiffuseTexture

```csharp
public void SetDiffuseTexture(Texture2D texture)
```

引擎名：`set_diffuse_texture`

### SetNormalTexture

```csharp
public void SetNormalTexture(Texture2D texture)
```

引擎名：`set_normal_texture`

### SetSpecularColor

```csharp
public void SetSpecularColor(Color color)
```

引擎名：`set_specular_color`

### SetSpecularShininess

```csharp
public void SetSpecularShininess(float shininess)
```

引擎名：`set_specular_shininess`

### SetSpecularTexture

```csharp
public void SetSpecularTexture(Texture2D texture)
```

引擎名：`set_specular_texture`

### SetTextureFilter

```csharp
public void SetTextureFilter(CanvasItem.TextureFilterEnum filter)
```

引擎名：`set_texture_filter`

### SetTextureRepeat

```csharp
public void SetTextureRepeat(CanvasItem.TextureRepeatEnum repeat)
```

引擎名：`set_texture_repeat`
