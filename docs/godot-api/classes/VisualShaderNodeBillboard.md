<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeBillboard

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

The output port of this node needs to be connected to `Model View Matrix` port of [`VisualShaderNodeOutput`](VisualShaderNodeOutput.md).

## 构造函数

### VisualShaderNodeBillboard

```csharp
public VisualShaderNodeBillboard()
```

## 属性

### BillboardType

```csharp
public VisualShaderNodeBillboard.BillboardTypeEnum BillboardType { get; set; }
```

引擎名：`billboard_type`

Controls how the object faces the camera.

### KeepScale

```csharp
public bool KeepScale { get; set; }
```

引擎名：`keep_scale`

If `true`, the shader will keep the scale set for the mesh. Otherwise, the scale is lost when billboarding.

## 方法

### GetBillboardType

```csharp
public VisualShaderNodeBillboard.BillboardTypeEnum GetBillboardType()
```

引擎名：`get_billboard_type`

### IsKeepScaleEnabled

```csharp
public bool IsKeepScaleEnabled()
```

引擎名：`is_keep_scale_enabled`

### SetBillboardType

```csharp
public void SetBillboardType(VisualShaderNodeBillboard.BillboardTypeEnum billboardType)
```

引擎名：`set_billboard_type`

### SetKeepScaleEnabled

```csharp
public void SetKeepScaleEnabled(bool enabled)
```

引擎名：`set_keep_scale_enabled`

## BillboardTypeEnum

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Billboarding is disabled and the node does nothing.
- `Enabled = 1`：A standard billboarding algorithm is enabled.
- `FixedY = 2`：A billboarding algorithm to rotate around Y-axis is enabled.
- `Particles = 3`：A billboarding algorithm designed to use on particles is enabled.
- `Max = 4`：Represents the size of the `VisualShaderNodeBillboard.BillboardTypeEnum` enum.
