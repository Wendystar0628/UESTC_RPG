<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# NoiseTexture3D

命名空间 `Godot`；class；继承 [`Texture3D`](Texture3D.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Uses the [`FastNoiseLite`](FastNoiseLite.md) library or other noise generators to fill the texture data of your desired size.

The class uses [`GodotThread`](GodotThread.md)s to generate the texture data internally, so [`Texture3D.GetData`](Texture3D.md) may return `null` if the generation process has not completed yet. In that case, you need to wait for the texture to be generated before accessing the image:

```csharp
var texture = NoiseTexture3D.new()
texture.noise = FastNoiseLite.new()
await texture.changed
var data = texture.get_data()

```

## 构造函数

### NoiseTexture3D

```csharp
public NoiseTexture3D()
```

## 属性

### ColorRamp

```csharp
public Gradient ColorRamp { get; set; }
```

引擎名：`color_ramp`

A [`Gradient`](Gradient.md) which is used to map the luminance of each pixel to a color value.

### Depth

```csharp
public int Depth { get; set; }
```

引擎名：`depth`

Depth of the generated texture (in pixels).

### Height

```csharp
public int Height { get; set; }
```

引擎名：`height`

Height of the generated texture (in pixels).

### Invert

```csharp
public bool Invert { get; set; }
```

引擎名：`invert`

If `true`, inverts the noise texture. White becomes black, black becomes white.

### Noise

```csharp
public Noise Noise { get; set; }
```

引擎名：`noise`

The instance of the [`Noise`](Noise.md) object.

### Normalize

```csharp
public bool Normalize { get; set; }
```

引擎名：`normalize`

If `true`, the noise image coming from the noise generator is normalized to the range `0.0` to `1.0`.

Turning normalization off can affect the contrast and allows you to generate non repeating tileable noise textures.

### Seamless

```csharp
public bool Seamless { get; set; }
```

引擎名：`seamless`

If `true`, a seamless texture is requested from the [`Noise`](Noise.md) resource.

**Note:** Seamless noise textures may take longer to generate and/or can have a lower contrast compared to non-seamless noise depending on the used [`Noise`](Noise.md) resource. This is because some implementations use higher dimensions for generating seamless noise.

**Note:** The default [`FastNoiseLite`](FastNoiseLite.md) implementation uses the fallback path for seamless generation. If using a `Width`, `Height` or `Depth` lower than the default, you may need to increase `SeamlessBlendSkirt` to make seamless blending more effective.

### SeamlessBlendSkirt

```csharp
public float SeamlessBlendSkirt { get; set; }
```

引擎名：`seamless_blend_skirt`

Used for the default/fallback implementation of the seamless texture generation. It determines the distance over which the seams are blended. High values may result in less details and contrast. See [`Noise`](Noise.md) for further details.

**Note:** If using a `Width`, `Height` or `Depth` lower than the default, you may need to increase `SeamlessBlendSkirt` to make seamless blending more effective.

### Width

```csharp
public int Width { get; set; }
```

引擎名：`width`

Width of the generated texture (in pixels).

## 方法

### GetColorRamp

```csharp
public Gradient GetColorRamp()
```

引擎名：`get_color_ramp`

### GetInvert

```csharp
public bool GetInvert()
```

引擎名：`get_invert`

### GetNoise

```csharp
public Noise GetNoise()
```

引擎名：`get_noise`

### GetSeamless

```csharp
public bool GetSeamless()
```

引擎名：`get_seamless`

### GetSeamlessBlendSkirt

```csharp
public float GetSeamlessBlendSkirt()
```

引擎名：`get_seamless_blend_skirt`

### IsNormalized

```csharp
public bool IsNormalized()
```

引擎名：`is_normalized`

### SetColorRamp

```csharp
public void SetColorRamp(Gradient gradient)
```

引擎名：`set_color_ramp`

### SetDepth

```csharp
public void SetDepth(int depth)
```

引擎名：`set_depth`

### SetHeight

```csharp
public void SetHeight(int height)
```

引擎名：`set_height`

### SetInvert

```csharp
public void SetInvert(bool invert)
```

引擎名：`set_invert`

### SetNoise

```csharp
public void SetNoise(Noise noise)
```

引擎名：`set_noise`

### SetNormalize

```csharp
public void SetNormalize(bool normalize)
```

引擎名：`set_normalize`

### SetSeamless

```csharp
public void SetSeamless(bool seamless)
```

引擎名：`set_seamless`

### SetSeamlessBlendSkirt

```csharp
public void SetSeamlessBlendSkirt(float seamlessBlendSkirt)
```

引擎名：`set_seamless_blend_skirt`

### SetWidth

```csharp
public void SetWidth(int width)
```

引擎名：`set_width`
