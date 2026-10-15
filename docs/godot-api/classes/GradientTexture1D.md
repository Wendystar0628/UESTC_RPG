<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GradientTexture1D

命名空间 `Godot`；class；继承 [`Texture2D`](Texture2D.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A 1D texture that obtains colors from a [`Gradient`](Gradient.md) to fill the texture data. The texture is filled by sampling the gradient for each pixel. Therefore, the texture does not necessarily represent an exact copy of the gradient, as it may miss some colors if there are not enough pixels. See also [`GradientTexture2D`](GradientTexture2D.md), [`CurveTexture`](CurveTexture.md) and [`CurveXyzTexture`](CurveXyzTexture.md).

## 构造函数

### GradientTexture1D

```csharp
public GradientTexture1D()
```

## 属性

### Gradient

```csharp
public Gradient Gradient { get; set; }
```

引擎名：`gradient`

The [`Gradient`](Gradient.md) used to fill the texture.

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

The number of color samples that will be obtained from the [`Gradient`](Gradient.md).

## 方法

### GetGradient

```csharp
public Gradient GetGradient()
```

引擎名：`get_gradient`

### IsUsingHdr

```csharp
public bool IsUsingHdr()
```

引擎名：`is_using_hdr`

### SetGradient

```csharp
public void SetGradient(Gradient gradient)
```

引擎名：`set_gradient`

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
