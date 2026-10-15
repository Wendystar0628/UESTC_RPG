<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RDPipelineColorBlendStateAttachment

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Controls how blending between source and destination fragments is performed when using [`RenderingDevice`](RenderingDevice.md).

For reference, this is how common user-facing blend modes are implemented in Godot's 2D renderer:

**Mix:**

```csharp
var attachment = RDPipelineColorBlendStateAttachment.new()
attachment.enable_blend = true
attachment.color_blend_op = RenderingDevice.BLEND_OP_ADD
attachment.src_color_blend_factor = RenderingDevice.BLEND_FACTOR_SRC_ALPHA
attachment.dst_color_blend_factor = RenderingDevice.BLEND_FACTOR_ONE_MINUS_SRC_ALPHA
attachment.alpha_blend_op = RenderingDevice.BLEND_OP_ADD
attachment.src_alpha_blend_factor = RenderingDevice.BLEND_FACTOR_ONE
attachment.dst_alpha_blend_factor = RenderingDevice.BLEND_FACTOR_ONE_MINUS_SRC_ALPHA

```

**Add:**

```csharp
var attachment = RDPipelineColorBlendStateAttachment.new()
attachment.enable_blend = true
attachment.alpha_blend_op = RenderingDevice.BLEND_OP_ADD
attachment.color_blend_op = RenderingDevice.BLEND_OP_ADD
attachment.src_color_blend_factor = RenderingDevice.BLEND_FACTOR_SRC_ALPHA
attachment.dst_color_blend_factor = RenderingDevice.BLEND_FACTOR_ONE
attachment.src_alpha_blend_factor = RenderingDevice.BLEND_FACTOR_SRC_ALPHA
attachment.dst_alpha_blend_factor = RenderingDevice.BLEND_FACTOR_ONE

```

**Subtract:**

```csharp
var attachment = RDPipelineColorBlendStateAttachment.new()
attachment.enable_blend = true
attachment.alpha_blend_op = RenderingDevice.BLEND_OP_REVERSE_SUBTRACT
attachment.color_blend_op = RenderingDevice.BLEND_OP_REVERSE_SUBTRACT
attachment.src_color_blend_factor = RenderingDevice.BLEND_FACTOR_SRC_ALPHA
attachment.dst_color_blend_factor = RenderingDevice.BLEND_FACTOR_ONE
attachment.src_alpha_blend_factor = RenderingDevice.BLEND_FACTOR_SRC_ALPHA
attachment.dst_alpha_blend_factor = RenderingDevice.BLEND_FACTOR_ONE

```

**Multiply:**

```csharp
var attachment = RDPipelineColorBlendStateAttachment.new()
attachment.enable_blend = true
attachment.alpha_blend_op = RenderingDevice.BLEND_OP_ADD
attachment.color_blend_op = RenderingDevice.BLEND_OP_ADD
attachment.src_color_blend_factor = RenderingDevice.BLEND_FACTOR_DST_COLOR
attachment.dst_color_blend_factor = RenderingDevice.BLEND_FACTOR_ZERO
attachment.src_alpha_blend_factor = RenderingDevice.BLEND_FACTOR_DST_ALPHA
attachment.dst_alpha_blend_factor = RenderingDevice.BLEND_FACTOR_ZERO

```

**Pre-multiplied alpha:**

```csharp
var attachment = RDPipelineColorBlendStateAttachment.new()
attachment.enable_blend = true
attachment.alpha_blend_op = RenderingDevice.BLEND_OP_ADD
attachment.color_blend_op = RenderingDevice.BLEND_OP_ADD
attachment.src_color_blend_factor = RenderingDevice.BLEND_FACTOR_ONE
attachment.dst_color_blend_factor = RenderingDevice.BLEND_FACTOR_ONE_MINUS_SRC_ALPHA
attachment.src_alpha_blend_factor = RenderingDevice.BLEND_FACTOR_ONE
attachment.dst_alpha_blend_factor = RenderingDevice.BLEND_FACTOR_ONE_MINUS_SRC_ALPHA

```

## 构造函数

### RDPipelineColorBlendStateAttachment

```csharp
public RDPipelineColorBlendStateAttachment()
```

## 属性

### AlphaBlendOp

```csharp
public RenderingDevice.BlendOperation AlphaBlendOp { get; set; }
```

引擎名：`alpha_blend_op`

The blend mode to use for the alpha channel.

### ColorBlendOp

```csharp
public RenderingDevice.BlendOperation ColorBlendOp { get; set; }
```

引擎名：`color_blend_op`

The blend mode to use for the red/green/blue color channels.

### DstAlphaBlendFactor

```csharp
public RenderingDevice.BlendFactor DstAlphaBlendFactor { get; set; }
```

引擎名：`dst_alpha_blend_factor`

Controls how the blend factor for the alpha channel is determined based on the destination's fragments.

### DstColorBlendFactor

```csharp
public RenderingDevice.BlendFactor DstColorBlendFactor { get; set; }
```

引擎名：`dst_color_blend_factor`

Controls how the blend factor for the color channels is determined based on the destination's fragments.

### EnableBlend

```csharp
public bool EnableBlend { get; set; }
```

引擎名：`enable_blend`

If `true`, performs blending between the source and destination according to the factors defined in `SrcColorBlendFactor`, `DstColorBlendFactor`, `SrcAlphaBlendFactor` and `DstAlphaBlendFactor`. The blend modes `ColorBlendOp` and `AlphaBlendOp` are also taken into account, with `WriteR`, `WriteG`, `WriteB` and `WriteA` controlling the output.

### SrcAlphaBlendFactor

```csharp
public RenderingDevice.BlendFactor SrcAlphaBlendFactor { get; set; }
```

引擎名：`src_alpha_blend_factor`

Controls how the blend factor for the alpha channel is determined based on the source's fragments.

### SrcColorBlendFactor

```csharp
public RenderingDevice.BlendFactor SrcColorBlendFactor { get; set; }
```

引擎名：`src_color_blend_factor`

Controls how the blend factor for the color channels is determined based on the source's fragments.

### WriteA

```csharp
public bool WriteA { get; set; }
```

引擎名：`write_a`

If `true`, writes the new alpha channel to the final result.

### WriteB

```csharp
public bool WriteB { get; set; }
```

引擎名：`write_b`

If `true`, writes the new blue color channel to the final result.

### WriteG

```csharp
public bool WriteG { get; set; }
```

引擎名：`write_g`

If `true`, writes the new green color channel to the final result.

### WriteR

```csharp
public bool WriteR { get; set; }
```

引擎名：`write_r`

If `true`, writes the new red color channel to the final result.

## 方法

### GetAlphaBlendOp

```csharp
public RenderingDevice.BlendOperation GetAlphaBlendOp()
```

引擎名：`get_alpha_blend_op`

### GetColorBlendOp

```csharp
public RenderingDevice.BlendOperation GetColorBlendOp()
```

引擎名：`get_color_blend_op`

### GetDstAlphaBlendFactor

```csharp
public RenderingDevice.BlendFactor GetDstAlphaBlendFactor()
```

引擎名：`get_dst_alpha_blend_factor`

### GetDstColorBlendFactor

```csharp
public RenderingDevice.BlendFactor GetDstColorBlendFactor()
```

引擎名：`get_dst_color_blend_factor`

### GetEnableBlend

```csharp
public bool GetEnableBlend()
```

引擎名：`get_enable_blend`

### GetSrcAlphaBlendFactor

```csharp
public RenderingDevice.BlendFactor GetSrcAlphaBlendFactor()
```

引擎名：`get_src_alpha_blend_factor`

### GetSrcColorBlendFactor

```csharp
public RenderingDevice.BlendFactor GetSrcColorBlendFactor()
```

引擎名：`get_src_color_blend_factor`

### GetWriteA

```csharp
public bool GetWriteA()
```

引擎名：`get_write_a`

### GetWriteB

```csharp
public bool GetWriteB()
```

引擎名：`get_write_b`

### GetWriteG

```csharp
public bool GetWriteG()
```

引擎名：`get_write_g`

### GetWriteR

```csharp
public bool GetWriteR()
```

引擎名：`get_write_r`

### SetAlphaBlendOp

```csharp
public void SetAlphaBlendOp(RenderingDevice.BlendOperation pMember)
```

引擎名：`set_alpha_blend_op`

### SetAsMix

```csharp
public void SetAsMix()
```

引擎名：`set_as_mix`

Convenience method to perform standard mix blending with straight (non-premultiplied) alpha. This sets `EnableBlend` to `true`, `SrcColorBlendFactor` to [`RenderingDevice.BlendFactor.SrcAlpha`](RenderingDevice.md), `DstColorBlendFactor` to [`RenderingDevice.BlendFactor.OneMinusSrcAlpha`](RenderingDevice.md), `SrcAlphaBlendFactor` to [`RenderingDevice.BlendFactor.SrcAlpha`](RenderingDevice.md) and `DstAlphaBlendFactor` to [`RenderingDevice.BlendFactor.OneMinusSrcAlpha`](RenderingDevice.md).

### SetColorBlendOp

```csharp
public void SetColorBlendOp(RenderingDevice.BlendOperation pMember)
```

引擎名：`set_color_blend_op`

### SetDstAlphaBlendFactor

```csharp
public void SetDstAlphaBlendFactor(RenderingDevice.BlendFactor pMember)
```

引擎名：`set_dst_alpha_blend_factor`

### SetDstColorBlendFactor

```csharp
public void SetDstColorBlendFactor(RenderingDevice.BlendFactor pMember)
```

引擎名：`set_dst_color_blend_factor`

### SetEnableBlend

```csharp
public void SetEnableBlend(bool pMember)
```

引擎名：`set_enable_blend`

### SetSrcAlphaBlendFactor

```csharp
public void SetSrcAlphaBlendFactor(RenderingDevice.BlendFactor pMember)
```

引擎名：`set_src_alpha_blend_factor`

### SetSrcColorBlendFactor

```csharp
public void SetSrcColorBlendFactor(RenderingDevice.BlendFactor pMember)
```

引擎名：`set_src_color_blend_factor`

### SetWriteA

```csharp
public void SetWriteA(bool pMember)
```

引擎名：`set_write_a`

### SetWriteB

```csharp
public void SetWriteB(bool pMember)
```

引擎名：`set_write_b`

### SetWriteG

```csharp
public void SetWriteG(bool pMember)
```

引擎名：`set_write_g`

### SetWriteR

```csharp
public void SetWriteR(bool pMember)
```

引擎名：`set_write_r`
