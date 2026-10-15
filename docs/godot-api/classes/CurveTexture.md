<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CurveTexture

命名空间 `Godot`；class；继承 [`Texture2D`](Texture2D.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A 1D texture where pixel brightness corresponds to points on a unit [`Curve`](Curve.md) resource, either in grayscale or in red. This visual representation simplifies the task of saving curves as image files.

If you need to store up to 3 curves within a single texture, use [`CurveXyzTexture`](CurveXyzTexture.md) instead. See also [`GradientTexture1D`](GradientTexture1D.md) and [`GradientTexture2D`](GradientTexture2D.md).

## 构造函数

### CurveTexture

```csharp
public CurveTexture()
```

## 属性

### Curve

```csharp
public Curve Curve { get; set; }
```

引擎名：`curve`

The [`Curve`](Curve.md) that is rendered onto the texture. Should be a unit [`Curve`](Curve.md).

### TextureMode

```csharp
public CurveTexture.TextureModeEnum TextureMode { get; set; }
```

引擎名：`texture_mode`

The format the texture should be generated with. When passing a CurveTexture as an input to a [`Shader`](Shader.md), this may need to be adjusted.

### Width

```csharp
public int Width { get; set; }
```

引擎名：`width`

The width of the texture (in pixels). Higher values make it possible to represent high-frequency data better (such as sudden direction changes), at the cost of increased generation time and memory usage.

## 方法

### GetCurve

```csharp
public Curve GetCurve()
```

引擎名：`get_curve`

### GetTextureMode

```csharp
public CurveTexture.TextureModeEnum GetTextureMode()
```

引擎名：`get_texture_mode`

### SetCurve

```csharp
public void SetCurve(Curve curve)
```

引擎名：`set_curve`

### SetTextureMode

```csharp
public void SetTextureMode(CurveTexture.TextureModeEnum textureMode)
```

引擎名：`set_texture_mode`

### SetWidth

```csharp
public void SetWidth(int width)
```

引擎名：`set_width`

## TextureModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Rgb = 0`：Store the curve equally across the red, green and blue channels.
- `Red = 1`：Store the curve only in the red channel.
