<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RDSamplerState

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This object is used by [`RenderingDevice`](RenderingDevice.md).

## 构造函数

### RDSamplerState

```csharp
public RDSamplerState()
```

## 属性

### AnisotropyMax

```csharp
public float AnisotropyMax { get; set; }
```

引擎名：`anisotropy_max`

Maximum anisotropy that can be used when sampling. Only effective if `UseAnisotropy` is `true`. Higher values result in a sharper sampler at oblique angles, at the cost of performance (due to memory bandwidth). This value may be limited by the graphics hardware in use. Most graphics hardware only supports values up to `16.0`.

If `AnisotropyMax` is `1.0`, forcibly disables anisotropy even if `UseAnisotropy` is `true`.

### BorderColor

```csharp
public RenderingDevice.SamplerBorderColor BorderColor { get; set; }
```

引擎名：`border_color`

The border color that will be returned when sampling outside the sampler's bounds and the `RepeatU`, `RepeatV` or `RepeatW` modes have repeating disabled.

### CompareOp

```csharp
public RenderingDevice.CompareOperator CompareOp { get; set; }
```

引擎名：`compare_op`

The compare operation to use. Only effective if `EnableCompare` is `true`.

### EnableCompare

```csharp
public bool EnableCompare { get; set; }
```

引擎名：`enable_compare`

If `true`, returned values will be based on the comparison operation defined in `CompareOp`. This is a hardware-based approach and is therefore faster than performing this manually in a shader. For example, compare operations are used for shadow map rendering by comparing depth values from a shadow sampler.

### LodBias

```csharp
public float LodBias { get; set; }
```

引擎名：`lod_bias`

The mipmap LOD bias to use. Positive values will make the sampler blurrier at a given distance, while negative values will make the sampler sharper at a given distance (at the risk of looking grainy). Recommended values are between `-0.5` and `0.0`. Only effective if the sampler has mipmaps available.

### MagFilter

```csharp
public RenderingDevice.SamplerFilter MagFilter { get; set; }
```

引擎名：`mag_filter`

The sampler's magnification filter. It is the filtering method used when sampling texels that appear bigger than on-screen pixels.

### MaxLod

```csharp
public float MaxLod { get; set; }
```

引擎名：`max_lod`

The maximum mipmap LOD bias to display (lowest resolution). Only effective if the sampler has mipmaps available.

### MinFilter

```csharp
public RenderingDevice.SamplerFilter MinFilter { get; set; }
```

引擎名：`min_filter`

The sampler's minification filter. It is the filtering method used when sampling texels that appear smaller than on-screen pixels.

### MinLod

```csharp
public float MinLod { get; set; }
```

引擎名：`min_lod`

The minimum mipmap LOD bias to display (highest resolution). Only effective if the sampler has mipmaps available.

### MipFilter

```csharp
public RenderingDevice.SamplerFilter MipFilter { get; set; }
```

引擎名：`mip_filter`

The filtering method to use for mipmaps.

### RepeatU

```csharp
public RenderingDevice.SamplerRepeatMode RepeatU { get; set; }
```

引擎名：`repeat_u`

The repeat mode to use along the U axis of UV coordinates. This affects the returned values if sampling outside the UV bounds.

### RepeatV

```csharp
public RenderingDevice.SamplerRepeatMode RepeatV { get; set; }
```

引擎名：`repeat_v`

The repeat mode to use along the V axis of UV coordinates. This affects the returned values if sampling outside the UV bounds.

### RepeatW

```csharp
public RenderingDevice.SamplerRepeatMode RepeatW { get; set; }
```

引擎名：`repeat_w`

The repeat mode to use along the W axis of UV coordinates. This affects the returned values if sampling outside the UV bounds. Only effective for 3D samplers.

### UnnormalizedUvw

```csharp
public bool UnnormalizedUvw { get; set; }
```

引擎名：`unnormalized_uvw`

If `true`, the texture will be sampled with coordinates ranging from 0 to the texture's resolution. Otherwise, the coordinates will be normalized and range from 0 to 1.

### UseAnisotropy

```csharp
public bool UseAnisotropy { get; set; }
```

引擎名：`use_anisotropy`

If `true`, perform anisotropic sampling. See `AnisotropyMax`.

## 方法

### GetAnisotropyMax

```csharp
public float GetAnisotropyMax()
```

引擎名：`get_anisotropy_max`

### GetBorderColor

```csharp
public RenderingDevice.SamplerBorderColor GetBorderColor()
```

引擎名：`get_border_color`

### GetCompareOp

```csharp
public RenderingDevice.CompareOperator GetCompareOp()
```

引擎名：`get_compare_op`

### GetEnableCompare

```csharp
public bool GetEnableCompare()
```

引擎名：`get_enable_compare`

### GetLodBias

```csharp
public float GetLodBias()
```

引擎名：`get_lod_bias`

### GetMagFilter

```csharp
public RenderingDevice.SamplerFilter GetMagFilter()
```

引擎名：`get_mag_filter`

### GetMaxLod

```csharp
public float GetMaxLod()
```

引擎名：`get_max_lod`

### GetMinFilter

```csharp
public RenderingDevice.SamplerFilter GetMinFilter()
```

引擎名：`get_min_filter`

### GetMinLod

```csharp
public float GetMinLod()
```

引擎名：`get_min_lod`

### GetMipFilter

```csharp
public RenderingDevice.SamplerFilter GetMipFilter()
```

引擎名：`get_mip_filter`

### GetRepeatU

```csharp
public RenderingDevice.SamplerRepeatMode GetRepeatU()
```

引擎名：`get_repeat_u`

### GetRepeatV

```csharp
public RenderingDevice.SamplerRepeatMode GetRepeatV()
```

引擎名：`get_repeat_v`

### GetRepeatW

```csharp
public RenderingDevice.SamplerRepeatMode GetRepeatW()
```

引擎名：`get_repeat_w`

### GetUnnormalizedUvw

```csharp
public bool GetUnnormalizedUvw()
```

引擎名：`get_unnormalized_uvw`

### GetUseAnisotropy

```csharp
public bool GetUseAnisotropy()
```

引擎名：`get_use_anisotropy`

### SetAnisotropyMax

```csharp
public void SetAnisotropyMax(float pMember)
```

引擎名：`set_anisotropy_max`

### SetBorderColor

```csharp
public void SetBorderColor(RenderingDevice.SamplerBorderColor pMember)
```

引擎名：`set_border_color`

### SetCompareOp

```csharp
public void SetCompareOp(RenderingDevice.CompareOperator pMember)
```

引擎名：`set_compare_op`

### SetEnableCompare

```csharp
public void SetEnableCompare(bool pMember)
```

引擎名：`set_enable_compare`

### SetLodBias

```csharp
public void SetLodBias(float pMember)
```

引擎名：`set_lod_bias`

### SetMagFilter

```csharp
public void SetMagFilter(RenderingDevice.SamplerFilter pMember)
```

引擎名：`set_mag_filter`

### SetMaxLod

```csharp
public void SetMaxLod(float pMember)
```

引擎名：`set_max_lod`

### SetMinFilter

```csharp
public void SetMinFilter(RenderingDevice.SamplerFilter pMember)
```

引擎名：`set_min_filter`

### SetMinLod

```csharp
public void SetMinLod(float pMember)
```

引擎名：`set_min_lod`

### SetMipFilter

```csharp
public void SetMipFilter(RenderingDevice.SamplerFilter pMember)
```

引擎名：`set_mip_filter`

### SetRepeatU

```csharp
public void SetRepeatU(RenderingDevice.SamplerRepeatMode pMember)
```

引擎名：`set_repeat_u`

### SetRepeatV

```csharp
public void SetRepeatV(RenderingDevice.SamplerRepeatMode pMember)
```

引擎名：`set_repeat_v`

### SetRepeatW

```csharp
public void SetRepeatW(RenderingDevice.SamplerRepeatMode pMember)
```

引擎名：`set_repeat_w`

### SetUnnormalizedUvw

```csharp
public void SetUnnormalizedUvw(bool pMember)
```

引擎名：`set_unnormalized_uvw`

### SetUseAnisotropy

```csharp
public void SetUseAnisotropy(bool pMember)
```

引擎名：`set_use_anisotropy`
