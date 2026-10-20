<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GradientTexture2D

命名空间 `Godot`；class；继承 [`Texture2D`](Texture2D.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A 2D texture that obtains colors from a [`Gradient`](Gradient.md) to fill the texture data. This texture is able to transform a color transition into different patterns such as a linear or a radial gradient. The texture is filled by interpolating colors starting from `FillFrom` to `FillTo` offsets by default, but the gradient fill can be repeated to cover the entire texture.

The gradient is sampled individually for each pixel so it does not necessarily represent an exact copy of the gradient (see `Width` and `Height`). See also [`GradientTexture1D`](GradientTexture1D.md), [`CurveTexture`](CurveTexture.md) and [`CurveXyzTexture`](CurveXyzTexture.md).

## 构造函数

### GradientTexture2D

```csharp
public GradientTexture2D()
```

## 属性

### Fill

```csharp
public GradientTexture2D.FillEnum Fill { get; set; }
```

引擎名：`fill`

The gradient's fill type.

### FillFrom

```csharp
public Vector2 FillFrom { get; set; }
```

引擎名：`fill_from`

The initial offset used to fill the texture specified in UV coordinates.

### FillTo

```csharp
public Vector2 FillTo { get; set; }
```

引擎名：`fill_to`

The final offset used to fill the texture specified in UV coordinates.

### Gradient

```csharp
public Gradient Gradient { get; set; }
```

引擎名：`gradient`

The [`Gradient`](Gradient.md) used to fill the texture.

### Height

```csharp
public int Height { get; set; }
```

引擎名：`height`

The number of vertical color samples that will be obtained from the [`Gradient`](Gradient.md), which also represents the texture's height.

### Repeat

```csharp
public GradientTexture2D.RepeatEnum Repeat { get; set; }
```

引擎名：`repeat`

The gradient's repeat type.

### UseHdr

```csharp
public bool UseHdr { get; set; }
```

引擎名：`use_hdr`

If `true`, the generated texture will support high dynamic range ([`Image.Format.Rgbaf`](Image.md) format). This allows for glow effects to work if [`Environment.GlowEnabled`](Environment.md) is `true`. If `false`, the generated texture will use low dynamic range; overbright colors will be clamped ([`Image.Format.Rgba8`](Image.md) format).

### Width

```csharp
public int Width { get; set; }
```

引擎名：`width`

The number of horizontal color samples that will be obtained from the [`Gradient`](Gradient.md), which also represents the texture's width.

## 方法

### GetFill

```csharp
public GradientTexture2D.FillEnum GetFill()
```

引擎名：`get_fill`

### GetFillFrom

```csharp
public Vector2 GetFillFrom()
```

引擎名：`get_fill_from`

### GetFillTo

```csharp
public Vector2 GetFillTo()
```

引擎名：`get_fill_to`

### GetGradient

```csharp
public Gradient GetGradient()
```

引擎名：`get_gradient`

### GetRepeat

```csharp
public GradientTexture2D.RepeatEnum GetRepeat()
```

引擎名：`get_repeat`

### IsUsingHdr

```csharp
public bool IsUsingHdr()
```

引擎名：`is_using_hdr`

### SetFill

```csharp
public void SetFill(GradientTexture2D.FillEnum fill)
```

引擎名：`set_fill`

### SetFillFrom

```csharp
public void SetFillFrom(Vector2 fillFrom)
```

引擎名：`set_fill_from`

### SetFillTo

```csharp
public void SetFillTo(Vector2 fillTo)
```

引擎名：`set_fill_to`

### SetGradient

```csharp
public void SetGradient(Gradient gradient)
```

引擎名：`set_gradient`

### SetHeight

```csharp
public void SetHeight(int height)
```

引擎名：`set_height`

### SetRepeat

```csharp
public void SetRepeat(GradientTexture2D.RepeatEnum repeat)
```

引擎名：`set_repeat`

### SetUseHdr

```csharp
public void SetUseHdr(bool enabled)
```

引擎名：`set_use_hdr`

### SetWidth

```csharp
public void SetWidth(int width)
```

引擎名：`set_width`

## FillEnum

命名空间 `Godot`；enum

### 枚举值

- `Linear = 0`：The colors are linearly interpolated in a straight line.
- `Radial = 1`：The colors are linearly interpolated in a circular pattern.
- `Square = 2`：The colors are linearly interpolated in a square pattern.
- `Conic = 3`：The colors are linearly interpolated in a cone pattern.

## RepeatEnum

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：The gradient fill is restricted to the range defined by [`GradientTexture2D.FillFrom`](GradientTexture2D.md) to [`GradientTexture2D.FillTo`](GradientTexture2D.md) offsets.
- `Repeat = 1`：The texture is filled starting from [`GradientTexture2D.FillFrom`](GradientTexture2D.md) to [`GradientTexture2D.FillTo`](GradientTexture2D.md) offsets, repeating the same pattern in both directions.
- `Mirror = 2`：The texture is filled starting from [`GradientTexture2D.FillFrom`](GradientTexture2D.md) to [`GradientTexture2D.FillTo`](GradientTexture2D.md) offsets, mirroring the pattern in both directions.
