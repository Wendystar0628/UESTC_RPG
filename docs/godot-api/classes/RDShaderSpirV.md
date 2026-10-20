<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RDShaderSpirV

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`RDShaderSpirV` represents an [`RDShaderFile`](RDShaderFile.md)'s [SPIR-V](https://www.khronos.org/spir/) code for various shader stages, as well as possible compilation error messages. SPIR-V is a low-level intermediate shader representation. This intermediate representation is not used directly by GPUs for rendering, but it can be compiled into binary shaders that GPUs can understand. Unlike compiled shaders, SPIR-V is portable across GPU models and driver versions.

This object is used by [`RenderingDevice`](RenderingDevice.md).

## 构造函数

### RDShaderSpirV

```csharp
public RDShaderSpirV()
```

## 属性

### BytecodeAnyHit

```csharp
public byte[] BytecodeAnyHit { get; set; }
```

The SPIR-V bytecode for the any hit shader stage.

### BytecodeClosestHit

```csharp
public byte[] BytecodeClosestHit { get; set; }
```

The SPIR-V bytecode for the closest hit shader stage.

### BytecodeCompute

```csharp
public byte[] BytecodeCompute { get; set; }
```

The SPIR-V bytecode for the compute shader stage.

### BytecodeFragment

```csharp
public byte[] BytecodeFragment { get; set; }
```

The SPIR-V bytecode for the fragment shader stage.

### BytecodeIntersection

```csharp
public byte[] BytecodeIntersection { get; set; }
```

The SPIR-V bytecode for the intersection shader stage.

### BytecodeMiss

```csharp
public byte[] BytecodeMiss { get; set; }
```

The SPIR-V bytecode for the miss shader stage.

### BytecodeRaygen

```csharp
public byte[] BytecodeRaygen { get; set; }
```

The SPIR-V bytecode for the ray generation shader stage.

### BytecodeTesselationControl

```csharp
public byte[] BytecodeTesselationControl { get; set; }
```

The SPIR-V bytecode for the tessellation control shader stage.

### BytecodeTesselationEvaluation

```csharp
public byte[] BytecodeTesselationEvaluation { get; set; }
```

The SPIR-V bytecode for the tessellation evaluation shader stage.

### BytecodeVertex

```csharp
public byte[] BytecodeVertex { get; set; }
```

The SPIR-V bytecode for the vertex shader stage.

### CompileErrorAnyHit

```csharp
public string CompileErrorAnyHit { get; set; }
```

The compilation error message for the any hit shader stage (set by the SPIR-V compiler and Godot). If empty, shader compilation was successful.

### CompileErrorClosestHit

```csharp
public string CompileErrorClosestHit { get; set; }
```

The compilation error message for the closest hit shader stage (set by the SPIR-V compiler and Godot). If empty, shader compilation was successful.

### CompileErrorCompute

```csharp
public string CompileErrorCompute { get; set; }
```

The compilation error message for the compute shader stage (set by the SPIR-V compiler and Godot). If empty, shader compilation was successful.

### CompileErrorFragment

```csharp
public string CompileErrorFragment { get; set; }
```

The compilation error message for the fragment shader stage (set by the SPIR-V compiler and Godot). If empty, shader compilation was successful.

### CompileErrorIntersection

```csharp
public string CompileErrorIntersection { get; set; }
```

The compilation error message for the intersection shader stage (set by the SPIR-V compiler and Godot). If empty, shader compilation was successful.

### CompileErrorMiss

```csharp
public string CompileErrorMiss { get; set; }
```

The compilation error message for the miss shader stage (set by the SPIR-V compiler and Godot). If empty, shader compilation was successful.

### CompileErrorRaygen

```csharp
public string CompileErrorRaygen { get; set; }
```

The compilation error message for the ray generation shader stage (set by the SPIR-V compiler and Godot). If empty, shader compilation was successful.

### CompileErrorTesselationControl

```csharp
public string CompileErrorTesselationControl { get; set; }
```

The compilation error message for the tessellation control shader stage (set by the SPIR-V compiler and Godot). If empty, shader compilation was successful.

### CompileErrorTesselationEvaluation

```csharp
public string CompileErrorTesselationEvaluation { get; set; }
```

The compilation error message for the tessellation evaluation shader stage (set by the SPIR-V compiler and Godot). If empty, shader compilation was successful.

### CompileErrorVertex

```csharp
public string CompileErrorVertex { get; set; }
```

The compilation error message for the vertex shader stage (set by the SPIR-V compiler and Godot). If empty, shader compilation was successful.

## 方法

### GetStageBytecode

```csharp
public byte[] GetStageBytecode(RenderingDevice.ShaderStage stage)
```

Equivalent to getting one of `BytecodeCompute`, `BytecodeFragment`, `BytecodeTesselationControl`, `BytecodeTesselationEvaluation`, `BytecodeVertex`.

### GetStageCompileError

```csharp
public string GetStageCompileError(RenderingDevice.ShaderStage stage)
```

Returns the compilation error message for the given shader `stage`. Equivalent to getting one of `CompileErrorCompute`, `CompileErrorFragment`, `CompileErrorTesselationControl`, `CompileErrorTesselationEvaluation`, `CompileErrorVertex`.

### SetStageBytecode

```csharp
public void SetStageBytecode(RenderingDevice.ShaderStage stage, ReadOnlySpan<byte> bytecode)
```

Sets the SPIR-V `bytecode` for the given shader `stage`. Equivalent to setting one of `BytecodeCompute`, `BytecodeFragment`, `BytecodeTesselationControl`, `BytecodeTesselationEvaluation`, `BytecodeVertex`.

### SetStageBytecode

```csharp
public void SetStageBytecode(RenderingDevice.ShaderStage stage, byte[] bytecode)
```

Sets the SPIR-V `bytecode` for the given shader `stage`. Equivalent to setting one of `BytecodeCompute`, `BytecodeFragment`, `BytecodeTesselationControl`, `BytecodeTesselationEvaluation`, `BytecodeVertex`.

### SetStageCompileError

```csharp
public void SetStageCompileError(RenderingDevice.ShaderStage stage, string compileError)
```

Sets the compilation error message for the given shader `stage` to `compileError`. Equivalent to setting one of `CompileErrorCompute`, `CompileErrorFragment`, `CompileErrorTesselationControl`, `CompileErrorTesselationEvaluation`, `CompileErrorVertex`.
