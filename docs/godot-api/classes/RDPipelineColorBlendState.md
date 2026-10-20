<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RDPipelineColorBlendState

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This object is used by [`RenderingDevice`](RenderingDevice.md).

## 构造函数

### RDPipelineColorBlendState

```csharp
public RDPipelineColorBlendState()
```

## 属性

### Attachments

```csharp
public Array<RDPipelineColorBlendStateAttachment> Attachments { get; set; }
```

引擎名：`attachments`

The attachments that are blended together.

### BlendConstant

```csharp
public Color BlendConstant { get; set; }
```

引擎名：`blend_constant`

The constant color to blend with. See also [`RenderingDevice.DrawListSetBlendConstants`](RenderingDevice.md).

### EnableLogicOp

```csharp
public bool EnableLogicOp { get; set; }
```

引擎名：`enable_logic_op`

If `true`, performs the logic operation defined in `LogicOp`.

### LogicOp

```csharp
public RenderingDevice.LogicOperation LogicOp { get; set; }
```

引擎名：`logic_op`

The logic operation to perform for blending. Only effective if `EnableLogicOp` is `true`.

## 方法

### GetAttachments

```csharp
public Array<RDPipelineColorBlendStateAttachment> GetAttachments()
```

引擎名：`get_attachments`

### GetBlendConstant

```csharp
public Color GetBlendConstant()
```

引擎名：`get_blend_constant`

### GetEnableLogicOp

```csharp
public bool GetEnableLogicOp()
```

引擎名：`get_enable_logic_op`

### GetLogicOp

```csharp
public RenderingDevice.LogicOperation GetLogicOp()
```

引擎名：`get_logic_op`

### SetAttachments

```csharp
public void SetAttachments(Array<RDPipelineColorBlendStateAttachment> attachments)
```

引擎名：`set_attachments`

### SetBlendConstant

```csharp
public void SetBlendConstant(Color pMember)
```

引擎名：`set_blend_constant`

### SetEnableLogicOp

```csharp
public void SetEnableLogicOp(bool pMember)
```

引擎名：`set_enable_logic_op`

### SetLogicOp

```csharp
public void SetLogicOp(RenderingDevice.LogicOperation pMember)
```

引擎名：`set_logic_op`
