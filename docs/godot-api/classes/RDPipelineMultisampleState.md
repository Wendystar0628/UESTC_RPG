<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RDPipelineMultisampleState

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`RDPipelineMultisampleState` is used to control how multisample or supersample antialiasing is being performed when rendering using [`RenderingDevice`](RenderingDevice.md).

## 构造函数

### RDPipelineMultisampleState

```csharp
public RDPipelineMultisampleState()
```

## 属性

### EnableAlphaToCoverage

```csharp
public bool EnableAlphaToCoverage { get; set; }
```

引擎名：`enable_alpha_to_coverage`

If `true`, alpha to coverage is enabled. This generates a temporary coverage value based on the alpha component of the fragment's first color output. This allows alpha transparency to make use of multisample antialiasing.

### EnableAlphaToOne

```csharp
public bool EnableAlphaToOne { get; set; }
```

引擎名：`enable_alpha_to_one`

If `true`, alpha is forced to either `0.0` or `1.0`. This allows hardening the edges of antialiased alpha transparencies. Only relevant if `EnableAlphaToCoverage` is `true`.

### EnableSampleShading

```csharp
public bool EnableSampleShading { get; set; }
```

引擎名：`enable_sample_shading`

If `true`, enables per-sample shading which replaces MSAA by SSAA. This provides higher quality antialiasing that works with transparent (alpha scissor) edges. This has a very high performance cost. See also `MinSampleShading`. See the [per-sample shading Vulkan documentation](https://registry.khronos.org/vulkan/specs/1.3-extensions/html/vkspec.html#primsrast-sampleshading) for more details.

### MinSampleShading

```csharp
public float MinSampleShading { get; set; }
```

引擎名：`min_sample_shading`

The multiplier of `SampleCount` that determines how many samples are performed for each fragment. Must be between `0.0` and `1.0` (inclusive). Only effective if `EnableSampleShading` is `true`. If `MinSampleShading` is `1.0`, fragment invocation must only read from the coverage index sample. Tile image access must not be used if `EnableSampleShading` is *not* `1.0`.

### SampleCount

```csharp
public RenderingDevice.TextureSamples SampleCount { get; set; }
```

引擎名：`sample_count`

The number of MSAA samples (or SSAA samples if `EnableSampleShading` is `true`) to perform. Higher values result in better antialiasing, at the cost of performance.

### SampleMasks

```csharp
public Array<int> SampleMasks { get; set; }
```

引擎名：`sample_masks`

The sample mask array. See the [sample mask Vulkan documentation](https://registry.khronos.org/vulkan/specs/1.3-extensions/html/vkspec.html#fragops-samplemask) for more details.

## 方法

### GetEnableAlphaToCoverage

```csharp
public bool GetEnableAlphaToCoverage()
```

引擎名：`get_enable_alpha_to_coverage`

### GetEnableAlphaToOne

```csharp
public bool GetEnableAlphaToOne()
```

引擎名：`get_enable_alpha_to_one`

### GetEnableSampleShading

```csharp
public bool GetEnableSampleShading()
```

引擎名：`get_enable_sample_shading`

### GetMinSampleShading

```csharp
public float GetMinSampleShading()
```

引擎名：`get_min_sample_shading`

### GetSampleCount

```csharp
public RenderingDevice.TextureSamples GetSampleCount()
```

引擎名：`get_sample_count`

### GetSampleMasks

```csharp
public Array<int> GetSampleMasks()
```

引擎名：`get_sample_masks`

### SetEnableAlphaToCoverage

```csharp
public void SetEnableAlphaToCoverage(bool pMember)
```

引擎名：`set_enable_alpha_to_coverage`

### SetEnableAlphaToOne

```csharp
public void SetEnableAlphaToOne(bool pMember)
```

引擎名：`set_enable_alpha_to_one`

### SetEnableSampleShading

```csharp
public void SetEnableSampleShading(bool pMember)
```

引擎名：`set_enable_sample_shading`

### SetMinSampleShading

```csharp
public void SetMinSampleShading(float pMember)
```

引擎名：`set_min_sample_shading`

### SetSampleCount

```csharp
public void SetSampleCount(RenderingDevice.TextureSamples pMember)
```

引擎名：`set_sample_count`

### SetSampleMasks

```csharp
public void SetSampleMasks(Array<int> masks)
```

引擎名：`set_sample_masks`
