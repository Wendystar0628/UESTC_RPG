<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeParameter

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A parameter represents a variable in the shader which is set externally, i.e. from the [`ShaderMaterial`](ShaderMaterial.md). Parameters are exposed as properties in the [`ShaderMaterial`](ShaderMaterial.md) and can be assigned from the Inspector or from a script.

## 属性

### InstanceIndex

```csharp
public int InstanceIndex { get; set; }
```

引擎名：`instance_index`

The index within 0-15 range, which is used to avoid clashes when shader used on multiple materials.

### ParameterName

```csharp
public string ParameterName { get; set; }
```

引擎名：`parameter_name`

Name of the parameter, by which it can be accessed through the [`ShaderMaterial`](ShaderMaterial.md) properties.

### Qualifier

```csharp
public VisualShaderNodeParameter.QualifierEnum Qualifier { get; set; }
```

引擎名：`qualifier`

Defines the scope of the parameter.

## 方法

### GetInstanceIndex

```csharp
public int GetInstanceIndex()
```

引擎名：`get_instance_index`

### GetParameterName

```csharp
public string GetParameterName()
```

引擎名：`get_parameter_name`

### GetQualifier

```csharp
public VisualShaderNodeParameter.QualifierEnum GetQualifier()
```

引擎名：`get_qualifier`

### SetInstanceIndex

```csharp
public void SetInstanceIndex(int instanceIndex)
```

引擎名：`set_instance_index`

### SetParameterName

```csharp
public void SetParameterName(string name)
```

引擎名：`set_parameter_name`

### SetQualifier

```csharp
public void SetQualifier(VisualShaderNodeParameter.QualifierEnum qualifier)
```

引擎名：`set_qualifier`

## QualifierEnum

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：The parameter will be tied to the [`ShaderMaterial`](ShaderMaterial.md) using this shader.
- `Global = 1`：The parameter will use a global value, defined in Project Settings.
- `Instance = 2`：The parameter will be tied to the node with attached [`ShaderMaterial`](ShaderMaterial.md) using this shader.
- `InstanceIndex = 3`：The parameter will be tied to the node with attached [`ShaderMaterial`](ShaderMaterial.md) using this shader.
- `Max = 4`：Represents the size of the `VisualShaderNodeParameter.QualifierEnum` enum.
