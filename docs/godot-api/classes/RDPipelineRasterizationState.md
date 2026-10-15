<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RDPipelineRasterizationState

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This object is used by [`RenderingDevice`](RenderingDevice.md).

## 构造函数

### RDPipelineRasterizationState

```csharp
public RDPipelineRasterizationState()
```

## 属性

### CullMode

```csharp
public RenderingDevice.PolygonCullMode CullMode { get; set; }
```

引擎名：`cull_mode`

The cull mode to use when drawing polygons, which determines whether front faces or backfaces are hidden.

### DepthBiasClamp

```csharp
public float DepthBiasClamp { get; set; }
```

引擎名：`depth_bias_clamp`

A limit for how much each depth value can be offset. If negative, it serves as a minimum value, but if positive, it serves as a maximum value.

### DepthBiasConstantFactor

```csharp
public float DepthBiasConstantFactor { get; set; }
```

引擎名：`depth_bias_constant_factor`

A constant offset added to each depth value. Applied after `DepthBiasSlopeFactor`.

### DepthBiasEnabled

```csharp
public bool DepthBiasEnabled { get; set; }
```

引擎名：`depth_bias_enabled`

If `true`, each generated depth value will by offset by some amount. The specific amount is generated per polygon based on the values of `DepthBiasSlopeFactor` and `DepthBiasConstantFactor`.

### DepthBiasSlopeFactor

```csharp
public float DepthBiasSlopeFactor { get; set; }
```

引擎名：`depth_bias_slope_factor`

A constant scale applied to the slope of each polygons' depth. Applied before `DepthBiasConstantFactor`.

### DiscardPrimitives

```csharp
public bool DiscardPrimitives { get; set; }
```

引擎名：`discard_primitives`

If `true`, primitives are discarded immediately before the rasterization stage.

### EnableDepthClamp

```csharp
public bool EnableDepthClamp { get; set; }
```

引擎名：`enable_depth_clamp`

If `true`, clamps depth values according to the minimum and maximum depth of the associated viewport.

### FrontFace

```csharp
public RenderingDevice.PolygonFrontFace FrontFace { get; set; }
```

引擎名：`front_face`

The winding order to use to determine which face of a triangle is considered its front face.

### LineWidth

```csharp
public float LineWidth { get; set; }
```

引擎名：`line_width`

The line width to use when drawing lines (in pixels). Thick lines may not be supported on all hardware.

### PatchControlPoints

```csharp
public uint PatchControlPoints { get; set; }
```

引擎名：`patch_control_points`

The number of control points to use when drawing a patch with tessellation enabled. Higher values result in higher quality at the cost of performance.

### Wireframe

```csharp
public bool Wireframe { get; set; }
```

引擎名：`wireframe`

If `true`, performs wireframe rendering for triangles instead of flat or textured rendering.

## 方法

### GetCullMode

```csharp
public RenderingDevice.PolygonCullMode GetCullMode()
```

引擎名：`get_cull_mode`

### GetDepthBiasClamp

```csharp
public float GetDepthBiasClamp()
```

引擎名：`get_depth_bias_clamp`

### GetDepthBiasConstantFactor

```csharp
public float GetDepthBiasConstantFactor()
```

引擎名：`get_depth_bias_constant_factor`

### GetDepthBiasEnabled

```csharp
public bool GetDepthBiasEnabled()
```

引擎名：`get_depth_bias_enabled`

### GetDepthBiasSlopeFactor

```csharp
public float GetDepthBiasSlopeFactor()
```

引擎名：`get_depth_bias_slope_factor`

### GetDiscardPrimitives

```csharp
public bool GetDiscardPrimitives()
```

引擎名：`get_discard_primitives`

### GetEnableDepthClamp

```csharp
public bool GetEnableDepthClamp()
```

引擎名：`get_enable_depth_clamp`

### GetFrontFace

```csharp
public RenderingDevice.PolygonFrontFace GetFrontFace()
```

引擎名：`get_front_face`

### GetLineWidth

```csharp
public float GetLineWidth()
```

引擎名：`get_line_width`

### GetPatchControlPoints

```csharp
public uint GetPatchControlPoints()
```

引擎名：`get_patch_control_points`

### GetWireframe

```csharp
public bool GetWireframe()
```

引擎名：`get_wireframe`

### SetCullMode

```csharp
public void SetCullMode(RenderingDevice.PolygonCullMode pMember)
```

引擎名：`set_cull_mode`

### SetDepthBiasClamp

```csharp
public void SetDepthBiasClamp(float pMember)
```

引擎名：`set_depth_bias_clamp`

### SetDepthBiasConstantFactor

```csharp
public void SetDepthBiasConstantFactor(float pMember)
```

引擎名：`set_depth_bias_constant_factor`

### SetDepthBiasEnabled

```csharp
public void SetDepthBiasEnabled(bool pMember)
```

引擎名：`set_depth_bias_enabled`

### SetDepthBiasSlopeFactor

```csharp
public void SetDepthBiasSlopeFactor(float pMember)
```

引擎名：`set_depth_bias_slope_factor`

### SetDiscardPrimitives

```csharp
public void SetDiscardPrimitives(bool pMember)
```

引擎名：`set_discard_primitives`

### SetEnableDepthClamp

```csharp
public void SetEnableDepthClamp(bool pMember)
```

引擎名：`set_enable_depth_clamp`

### SetFrontFace

```csharp
public void SetFrontFace(RenderingDevice.PolygonFrontFace pMember)
```

引擎名：`set_front_face`

### SetLineWidth

```csharp
public void SetLineWidth(float pMember)
```

引擎名：`set_line_width`

### SetPatchControlPoints

```csharp
public void SetPatchControlPoints(uint pMember)
```

引擎名：`set_patch_control_points`

### SetWireframe

```csharp
public void SetWireframe(bool pMember)
```

引擎名：`set_wireframe`
