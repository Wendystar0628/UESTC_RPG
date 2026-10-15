<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RDPipelineShader

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Wraps a shader resource and allows specialization constants to be applied at pipeline creation time.

Used by [`RenderingDevice.RaytracingPipelineCreate`](RenderingDevice.md) for ray generation, miss, and hit shaders. The pipeline selects the required shader stage automatically.

## 构造函数

### RDPipelineShader

```csharp
public RDPipelineShader()
```

## 属性

### Shader

```csharp
public Rid Shader { get; set; }
```

引擎名：`shader`

Shader resource. The required stage is selected by the pipeline.

### SpecializationConstants

```csharp
public Array<RDPipelineSpecializationConstant> SpecializationConstants { get; set; }
```

引擎名：`specialization_constants`

Specialization constants applied to the selected shader stage at pipeline creation time.

## 方法

### GetShader

```csharp
public Rid GetShader()
```

引擎名：`get_shader`

### GetSpecializationConstants

```csharp
public Array<RDPipelineSpecializationConstant> GetSpecializationConstants()
```

引擎名：`get_specialization_constants`

### SetShader

```csharp
public void SetShader(Rid pMember)
```

引擎名：`set_shader`

### SetSpecializationConstants

```csharp
public void SetSpecializationConstants(Array<RDPipelineSpecializationConstant> specializationConstants)
```

引擎名：`set_specialization_constants`
