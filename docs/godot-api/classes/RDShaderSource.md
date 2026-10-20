<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RDShaderSource

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Shader source code in text form.

See also [`RDShaderFile`](RDShaderFile.md). `RDShaderSource` is only meant to be used with the [`RenderingDevice`](RenderingDevice.md) API. It should not be confused with Godot's own [`Shader`](Shader.md) resource, which is what Godot's various nodes use for high-level shader programming.

## 构造函数

### RDShaderSource

```csharp
public RDShaderSource()
```

## 属性

### Language

```csharp
public RenderingDevice.ShaderLanguage Language { get; set; }
```

引擎名：`language`

The language the shader is written in.

### SourceAnyHit

```csharp
public string SourceAnyHit { get; set; }
```

引擎名：`source_any_hit`

Source code for the shader's any hit stage.

### SourceClosestHit

```csharp
public string SourceClosestHit { get; set; }
```

引擎名：`source_closest_hit`

Source code for the shader's closest hit stage.

### SourceCompute

```csharp
public string SourceCompute { get; set; }
```

引擎名：`source_compute`

Source code for the shader's compute stage.

### SourceFragment

```csharp
public string SourceFragment { get; set; }
```

引擎名：`source_fragment`

Source code for the shader's fragment stage.

### SourceIntersection

```csharp
public string SourceIntersection { get; set; }
```

引擎名：`source_intersection`

Source code for the shader's intersection stage.

### SourceMiss

```csharp
public string SourceMiss { get; set; }
```

引擎名：`source_miss`

Source code for the shader's miss stage.

### SourceRaygen

```csharp
public string SourceRaygen { get; set; }
```

引擎名：`source_raygen`

Source code for the shader's ray generation stage.

### SourceTesselationControl

```csharp
public string SourceTesselationControl { get; set; }
```

引擎名：`source_tesselation_control`

Source code for the shader's tessellation control stage.

### SourceTesselationEvaluation

```csharp
public string SourceTesselationEvaluation { get; set; }
```

引擎名：`source_tesselation_evaluation`

Source code for the shader's tessellation evaluation stage.

### SourceVertex

```csharp
public string SourceVertex { get; set; }
```

引擎名：`source_vertex`

Source code for the shader's vertex stage.

## 方法

### GetLanguage

```csharp
public RenderingDevice.ShaderLanguage GetLanguage()
```

引擎名：`get_language`

### GetStageSource

```csharp
public string GetStageSource(RenderingDevice.ShaderStage stage)
```

引擎名：`get_stage_source`

Returns source code for the specified shader `stage`. Equivalent to getting one of `SourceCompute`, `SourceFragment`, `SourceTesselationControl`, `SourceTesselationEvaluation` or `SourceVertex`.

### SetLanguage

```csharp
public void SetLanguage(RenderingDevice.ShaderLanguage language)
```

引擎名：`set_language`

### SetStageSource

```csharp
public void SetStageSource(RenderingDevice.ShaderStage stage, string source)
```

引擎名：`set_stage_source`

Sets `source` code for the specified shader `stage`. Equivalent to setting one of `SourceCompute`, `SourceFragment`, `SourceTesselationControl`, `SourceTesselationEvaluation` or `SourceVertex`.

**Note:** If you set the compute shader source code using this method directly, remember to remove the Godot-specific hint `#[compute]`.
