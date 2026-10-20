<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeInput

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Gives access to input variables (built-ins) available for the shader. See the shading reference for the list of available built-ins for each shader type (check `Tutorials` section for link).

## 构造函数

### VisualShaderNodeInput

```csharp
public VisualShaderNodeInput()
```

## 属性

### InputName

```csharp
public string InputName { get; set; }
```

引擎名：`input_name`

One of the several input constants in lower-case style like: "vertex" (`VERTEX`) or "point_size" (`POINT_SIZE`).

## 方法

### GetInputName

```csharp
public string GetInputName()
```

引擎名：`get_input_name`

### GetInputRealName

```csharp
public string GetInputRealName()
```

引擎名：`get_input_real_name`

Returns a translated name of the current constant in the Godot Shader Language. E.g. `"ALBEDO"` if the `InputName` equal to `"albedo"`.

### SetInputName

```csharp
public void SetInputName(string name)
```

引擎名：`set_input_name`

## 事件（信号）

### InputTypeChanged

```csharp
public event Action InputTypeChanged
```

引擎名：`input_type_changed`

Emitted when input is changed via `InputName`.
