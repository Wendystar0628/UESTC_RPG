<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Gradient

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This resource describes a color transition by defining a set of colored points and how to interpolate between them.

See also [`Curve`](Curve.md) which supports more complex easing methods, but does not support colors.

## 构造函数

### Gradient

```csharp
public Gradient()
```

## 属性

### Colors

```csharp
public Color[] Colors { get; set; }
```

引擎名：`colors`

Gradient's colors as a [`Color`](Color.md)[].

**Note:** Setting this property updates all colors at once. To update any color individually use `SetColor()`.

### InterpolationColorSpace

```csharp
public Gradient.ColorSpace InterpolationColorSpace { get; set; }
```

引擎名：`interpolation_color_space`

The color space used to interpolate between points of the gradient. It does not affect the returned colors, which will always use nonlinear sRGB encoding.

**Note:** This setting has no effect when `InterpolationMode` is set to [`Gradient.InterpolationModeEnum.Constant`](Gradient.md).

### InterpolationMode

```csharp
public Gradient.InterpolationModeEnum InterpolationMode { get; set; }
```

引擎名：`interpolation_mode`

The algorithm used to interpolate between points of the gradient.

### Offsets

```csharp
public float[] Offsets { get; set; }
```

引擎名：`offsets`

Gradient's offsets as a `System.Single`[].

**Note:** Setting this property updates all offsets at once. To update any offset individually use `SetOffset()`.

## 方法

### AddPoint

```csharp
public void AddPoint(float offset, Color color)
```

引擎名：`add_point`

Adds the specified color to the gradient, with the specified offset.

### GetColor

```csharp
public Color GetColor(int point)
```

引擎名：`get_color`

Returns the color of the gradient color at index `point`.

### GetColors

```csharp
public Color[] GetColors()
```

引擎名：`get_colors`

### GetInterpolationColorSpace

```csharp
public Gradient.ColorSpace GetInterpolationColorSpace()
```

引擎名：`get_interpolation_color_space`

### GetInterpolationMode

```csharp
public Gradient.InterpolationModeEnum GetInterpolationMode()
```

引擎名：`get_interpolation_mode`

### GetOffset

```csharp
public float GetOffset(int point)
```

引擎名：`get_offset`

Returns the offset of the gradient color at index `point`.

### GetOffsets

```csharp
public float[] GetOffsets()
```

引擎名：`get_offsets`

### GetPointCount

```csharp
public int GetPointCount()
```

引擎名：`get_point_count`

Returns the number of colors in the gradient.

### RemovePoint

```csharp
public void RemovePoint(int point)
```

引擎名：`remove_point`

Removes the color at index `point`.

### Reverse

```csharp
public void Reverse()
```

引擎名：`reverse`

Reverses/mirrors the gradient.

**Note:** This method mirrors all points around the middle of the gradient, which may produce unexpected results when `InterpolationMode` is set to [`Gradient.InterpolationModeEnum.Constant`](Gradient.md).

### Sample

```csharp
public Color Sample(float offset)
```

引擎名：`sample`

Returns the interpolated color specified by `offset`. `offset` should be between `0.0` and `1.0` (inclusive). Using a value lower than `0.0` will return the same color as `0.0`, and using a value higher than `1.0` will return the same color as `1.0`. If your input value is not within this range, consider using `Mathf.Remap` on the input value with output values set to `0.0` and `1.0`.

### SetColor

```csharp
public void SetColor(int point, Color color)
```

引擎名：`set_color`

Sets the color of the gradient color at index `point`.

### SetColors

```csharp
public void SetColors(Color[] colors)
```

引擎名：`set_colors`

### SetColors

```csharp
public void SetColors(ReadOnlySpan<Color> colors)
```

引擎名：`set_colors`

### SetInterpolationColorSpace

```csharp
public void SetInterpolationColorSpace(Gradient.ColorSpace interpolationColorSpace)
```

引擎名：`set_interpolation_color_space`

### SetInterpolationMode

```csharp
public void SetInterpolationMode(Gradient.InterpolationModeEnum interpolationMode)
```

引擎名：`set_interpolation_mode`

### SetOffset

```csharp
public void SetOffset(int point, float offset)
```

引擎名：`set_offset`

Sets the offset for the gradient color at index `point`.

### SetOffsets

```csharp
public void SetOffsets(ReadOnlySpan<float> offsets)
```

引擎名：`set_offsets`

### SetOffsets

```csharp
public void SetOffsets(float[] offsets)
```

引擎名：`set_offsets`

## ColorSpace

命名空间 `Godot`；enum

### 枚举值

- `Srgb = 0`：sRGB color space.
- `LinearSrgb = 1`：Linear sRGB color space.
- `Oklab = 2`：[Oklab](https://bottosson.github.io/posts/oklab/) color space.

## InterpolationModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Linear = 0`：Linear interpolation.
- `Constant = 1`：Constant interpolation, color changes abruptly at each point and stays uniform between.
- `Cubic = 2`：Cubic interpolation.
