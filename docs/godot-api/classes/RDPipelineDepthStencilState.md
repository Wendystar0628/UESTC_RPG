<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RDPipelineDepthStencilState

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`RDPipelineDepthStencilState` controls the way depth and stencil comparisons are performed when sampling those values using [`RenderingDevice`](RenderingDevice.md).

## 构造函数

### RDPipelineDepthStencilState

```csharp
public RDPipelineDepthStencilState()
```

## 属性

### BackOpCompare

```csharp
public RenderingDevice.CompareOperator BackOpCompare { get; set; }
```

引擎名：`back_op_compare`

The method used for comparing the previous back stencil value and `BackOpReference`.

### BackOpCompareMask

```csharp
public uint BackOpCompareMask { get; set; }
```

引擎名：`back_op_compare_mask`

Selects which bits from the back stencil value will be compared.

### BackOpDepthFail

```csharp
public RenderingDevice.StencilOperation BackOpDepthFail { get; set; }
```

引擎名：`back_op_depth_fail`

The operation to perform on the stencil buffer for back pixels that pass the stencil test but fail the depth test.

### BackOpFail

```csharp
public RenderingDevice.StencilOperation BackOpFail { get; set; }
```

引擎名：`back_op_fail`

The operation to perform on the stencil buffer for back pixels that fail the stencil test.

### BackOpPass

```csharp
public RenderingDevice.StencilOperation BackOpPass { get; set; }
```

引擎名：`back_op_pass`

The operation to perform on the stencil buffer for back pixels that pass the stencil test.

### BackOpReference

```csharp
public uint BackOpReference { get; set; }
```

引擎名：`back_op_reference`

The value the previous back stencil value will be compared to.

### BackOpWriteMask

```csharp
public uint BackOpWriteMask { get; set; }
```

引擎名：`back_op_write_mask`

Selects which bits from the back stencil value will be changed.

### DepthCompareOperator

```csharp
public RenderingDevice.CompareOperator DepthCompareOperator { get; set; }
```

引擎名：`depth_compare_operator`

The method used for comparing the previous and current depth values.

### DepthRangeMax

```csharp
public float DepthRangeMax { get; set; }
```

引擎名：`depth_range_max`

The maximum depth that returns `true` for `EnableDepthRange`.

### DepthRangeMin

```csharp
public float DepthRangeMin { get; set; }
```

引擎名：`depth_range_min`

The minimum depth that returns `true` for `EnableDepthRange`.

### EnableDepthRange

```csharp
public bool EnableDepthRange { get; set; }
```

引擎名：`enable_depth_range`

If `true`, each depth value will be tested to see if it is between `DepthRangeMin` and `DepthRangeMax`. If it is outside of these values, it is discarded.

### EnableDepthTest

```csharp
public bool EnableDepthTest { get; set; }
```

引擎名：`enable_depth_test`

If `true`, enables depth testing which allows objects to be automatically occluded by other objects based on their depth. This also allows objects to be partially occluded by other objects. If `false`, objects will appear in the order they were drawn (like in Godot's 2D renderer).

### EnableDepthWrite

```csharp
public bool EnableDepthWrite { get; set; }
```

引擎名：`enable_depth_write`

If `true`, writes to the depth buffer whenever the depth test returns `true`. Only works when enable_depth_test is also `true`.

### EnableStencil

```csharp
public bool EnableStencil { get; set; }
```

引擎名：`enable_stencil`

If `true`, enables stencil testing. There are separate stencil buffers for front-facing triangles and back-facing triangles. See properties that begin with "front_op" and properties with "back_op" for each.

### FrontOpCompare

```csharp
public RenderingDevice.CompareOperator FrontOpCompare { get; set; }
```

引擎名：`front_op_compare`

The method used for comparing the previous front stencil value and `FrontOpReference`.

### FrontOpCompareMask

```csharp
public uint FrontOpCompareMask { get; set; }
```

引擎名：`front_op_compare_mask`

Selects which bits from the front stencil value will be compared.

### FrontOpDepthFail

```csharp
public RenderingDevice.StencilOperation FrontOpDepthFail { get; set; }
```

引擎名：`front_op_depth_fail`

The operation to perform on the stencil buffer for front pixels that pass the stencil test but fail the depth test.

### FrontOpFail

```csharp
public RenderingDevice.StencilOperation FrontOpFail { get; set; }
```

引擎名：`front_op_fail`

The operation to perform on the stencil buffer for front pixels that fail the stencil test.

### FrontOpPass

```csharp
public RenderingDevice.StencilOperation FrontOpPass { get; set; }
```

引擎名：`front_op_pass`

The operation to perform on the stencil buffer for front pixels that pass the stencil test.

### FrontOpReference

```csharp
public uint FrontOpReference { get; set; }
```

引擎名：`front_op_reference`

The value the previous front stencil value will be compared to.

### FrontOpWriteMask

```csharp
public uint FrontOpWriteMask { get; set; }
```

引擎名：`front_op_write_mask`

Selects which bits from the front stencil value will be changed.

## 方法

### GetBackOpCompare

```csharp
public RenderingDevice.CompareOperator GetBackOpCompare()
```

引擎名：`get_back_op_compare`

### GetBackOpCompareMask

```csharp
public uint GetBackOpCompareMask()
```

引擎名：`get_back_op_compare_mask`

### GetBackOpDepthFail

```csharp
public RenderingDevice.StencilOperation GetBackOpDepthFail()
```

引擎名：`get_back_op_depth_fail`

### GetBackOpFail

```csharp
public RenderingDevice.StencilOperation GetBackOpFail()
```

引擎名：`get_back_op_fail`

### GetBackOpPass

```csharp
public RenderingDevice.StencilOperation GetBackOpPass()
```

引擎名：`get_back_op_pass`

### GetBackOpReference

```csharp
public uint GetBackOpReference()
```

引擎名：`get_back_op_reference`

### GetBackOpWriteMask

```csharp
public uint GetBackOpWriteMask()
```

引擎名：`get_back_op_write_mask`

### GetDepthCompareOperator

```csharp
public RenderingDevice.CompareOperator GetDepthCompareOperator()
```

引擎名：`get_depth_compare_operator`

### GetDepthRangeMax

```csharp
public float GetDepthRangeMax()
```

引擎名：`get_depth_range_max`

### GetDepthRangeMin

```csharp
public float GetDepthRangeMin()
```

引擎名：`get_depth_range_min`

### GetEnableDepthRange

```csharp
public bool GetEnableDepthRange()
```

引擎名：`get_enable_depth_range`

### GetEnableDepthTest

```csharp
public bool GetEnableDepthTest()
```

引擎名：`get_enable_depth_test`

### GetEnableDepthWrite

```csharp
public bool GetEnableDepthWrite()
```

引擎名：`get_enable_depth_write`

### GetEnableStencil

```csharp
public bool GetEnableStencil()
```

引擎名：`get_enable_stencil`

### GetFrontOpCompare

```csharp
public RenderingDevice.CompareOperator GetFrontOpCompare()
```

引擎名：`get_front_op_compare`

### GetFrontOpCompareMask

```csharp
public uint GetFrontOpCompareMask()
```

引擎名：`get_front_op_compare_mask`

### GetFrontOpDepthFail

```csharp
public RenderingDevice.StencilOperation GetFrontOpDepthFail()
```

引擎名：`get_front_op_depth_fail`

### GetFrontOpFail

```csharp
public RenderingDevice.StencilOperation GetFrontOpFail()
```

引擎名：`get_front_op_fail`

### GetFrontOpPass

```csharp
public RenderingDevice.StencilOperation GetFrontOpPass()
```

引擎名：`get_front_op_pass`

### GetFrontOpReference

```csharp
public uint GetFrontOpReference()
```

引擎名：`get_front_op_reference`

### GetFrontOpWriteMask

```csharp
public uint GetFrontOpWriteMask()
```

引擎名：`get_front_op_write_mask`

### SetBackOpCompare

```csharp
public void SetBackOpCompare(RenderingDevice.CompareOperator pMember)
```

引擎名：`set_back_op_compare`

### SetBackOpCompareMask

```csharp
public void SetBackOpCompareMask(uint pMember)
```

引擎名：`set_back_op_compare_mask`

### SetBackOpDepthFail

```csharp
public void SetBackOpDepthFail(RenderingDevice.StencilOperation pMember)
```

引擎名：`set_back_op_depth_fail`

### SetBackOpFail

```csharp
public void SetBackOpFail(RenderingDevice.StencilOperation pMember)
```

引擎名：`set_back_op_fail`

### SetBackOpPass

```csharp
public void SetBackOpPass(RenderingDevice.StencilOperation pMember)
```

引擎名：`set_back_op_pass`

### SetBackOpReference

```csharp
public void SetBackOpReference(uint pMember)
```

引擎名：`set_back_op_reference`

### SetBackOpWriteMask

```csharp
public void SetBackOpWriteMask(uint pMember)
```

引擎名：`set_back_op_write_mask`

### SetDepthCompareOperator

```csharp
public void SetDepthCompareOperator(RenderingDevice.CompareOperator pMember)
```

引擎名：`set_depth_compare_operator`

### SetDepthRangeMax

```csharp
public void SetDepthRangeMax(float pMember)
```

引擎名：`set_depth_range_max`

### SetDepthRangeMin

```csharp
public void SetDepthRangeMin(float pMember)
```

引擎名：`set_depth_range_min`

### SetEnableDepthRange

```csharp
public void SetEnableDepthRange(bool pMember)
```

引擎名：`set_enable_depth_range`

### SetEnableDepthTest

```csharp
public void SetEnableDepthTest(bool pMember)
```

引擎名：`set_enable_depth_test`

### SetEnableDepthWrite

```csharp
public void SetEnableDepthWrite(bool pMember)
```

引擎名：`set_enable_depth_write`

### SetEnableStencil

```csharp
public void SetEnableStencil(bool pMember)
```

引擎名：`set_enable_stencil`

### SetFrontOpCompare

```csharp
public void SetFrontOpCompare(RenderingDevice.CompareOperator pMember)
```

引擎名：`set_front_op_compare`

### SetFrontOpCompareMask

```csharp
public void SetFrontOpCompareMask(uint pMember)
```

引擎名：`set_front_op_compare_mask`

### SetFrontOpDepthFail

```csharp
public void SetFrontOpDepthFail(RenderingDevice.StencilOperation pMember)
```

引擎名：`set_front_op_depth_fail`

### SetFrontOpFail

```csharp
public void SetFrontOpFail(RenderingDevice.StencilOperation pMember)
```

引擎名：`set_front_op_fail`

### SetFrontOpPass

```csharp
public void SetFrontOpPass(RenderingDevice.StencilOperation pMember)
```

引擎名：`set_front_op_pass`

### SetFrontOpReference

```csharp
public void SetFrontOpReference(uint pMember)
```

引擎名：`set_front_op_reference`

### SetFrontOpWriteMask

```csharp
public void SetFrontOpWriteMask(uint pMember)
```

引擎名：`set_front_op_write_mask`
