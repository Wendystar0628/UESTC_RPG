<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeParameterRef

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Creating a reference to a [`VisualShaderNodeParameter`](VisualShaderNodeParameter.md) allows you to reuse this parameter in different shaders or shader stages easily.

## 构造函数

### VisualShaderNodeParameterRef

```csharp
public VisualShaderNodeParameterRef()
```

## 属性

### ParamType

```csharp
public int ParamType { get; set; }
```

引擎名：`param_type`

### ParameterName

```csharp
public string ParameterName { get; set; }
```

引擎名：`parameter_name`

The name of the parameter which this reference points to.

## 方法

### GetParameterName

```csharp
public string GetParameterName()
```

引擎名：`get_parameter_name`

### SetParameterName

```csharp
public void SetParameterName(string name)
```

引擎名：`set_parameter_name`
