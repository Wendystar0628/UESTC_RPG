<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RDPipelineSpecializationConstant

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A *specialization constant* is a way to create additional variants of shaders without actually increasing the number of shader versions that are compiled. This allows improving performance by reducing the number of shader versions and reducing `if` branching, while still allowing shaders to be flexible for different use cases.

This object is used by [`RenderingDevice`](RenderingDevice.md).

## 构造函数

### RDPipelineSpecializationConstant

```csharp
public RDPipelineSpecializationConstant()
```

## 属性

### ConstantId

```csharp
public uint ConstantId { get; set; }
```

引擎名：`constant_id`

The identifier of the specialization constant. This is a value starting from `0` and that increments for every different specialization constant for a given shader.

### Value

```csharp
public Variant Value { get; set; }
```

引擎名：`value`

The specialization constant's value. Only `System.Boolean`, `System.Int32` and `System.Single` types are valid for specialization constants.

## 方法

### GetConstantId

```csharp
public uint GetConstantId()
```

引擎名：`get_constant_id`

### GetValue

```csharp
public Variant GetValue()
```

引擎名：`get_value`

### SetConstantId

```csharp
public void SetConstantId(uint constantId)
```

引擎名：`set_constant_id`

### SetValue

```csharp
public void SetValue(Variant value)
```

引擎名：`set_value`
