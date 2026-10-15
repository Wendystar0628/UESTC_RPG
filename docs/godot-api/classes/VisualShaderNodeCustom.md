<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeCustom

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

By inheriting this class you can create a custom [`VisualShader`](VisualShader.md) script addon which will be automatically added to the Visual Shader Editor. The [`VisualShaderNode`](VisualShaderNode.md)'s behavior is defined by overriding the provided virtual methods.

In order for the node to be registered as an editor addon, you must use the `@tool` annotation and provide a `class_name` for your custom script. For example:

```csharp
@tool
extends VisualShaderNodeCustom
class_name VisualShaderNodeNoise

```

## 构造函数

### VisualShaderNodeCustom

```csharp
public VisualShaderNodeCustom()
```

## 属性

### Initialized

```csharp
public bool Initialized { get; set; }
```

引擎名：`initialized`

### Properties

```csharp
public string Properties { get; set; }
```

引擎名：`properties`

## 方法

### GetOptionIndex

```csharp
public int GetOptionIndex(int option)
```

引擎名：`get_option_index`

Returns the selected index of the drop-down list option within a graph. You may use this function to define the specific behavior in the `_GetCode()` or `_GetGlobalCode()`.

### _GetCategory

```csharp
public virtual string _GetCategory()
```

引擎名：`_get_category`

Override this method to define the path to the associated custom node in the Visual Shader Editor's members dialog. The path may look like `"MyGame/MyFunctions/Noise"`.

Defining this method is **optional**. If not overridden, the node will be filed under the "Addons" category.

### _GetCode

```csharp
public virtual string _GetCode(Array<string> inputVars, Array<string> outputVars, Shader.Mode mode, VisualShader.Type type)
```

引擎名：`_get_code`

Override this method to define the actual shader code of the associated custom node. The shader code should be returned as a string, which can have multiple lines (the `"""` multiline string construct can be used for convenience).

The `inputVars` and `outputVars` arrays contain the string names of the various input and output variables, as defined by `_get_input_*` and `_get_output_*` virtual methods in this class.

The output ports can be assigned values in the shader code. For example, `return output_vars[0] + " = " + input_vars[0] + ";"`.

You can customize the generated code based on the shader `mode` and/or `type`.

Defining this method is **required**.

### _GetDefaultInputPort

```csharp
public virtual int _GetDefaultInputPort(VisualShaderNode.PortType type)
```

引擎名：`_get_default_input_port`

Override this method to define the input port which should be connected by default when this node is created as a result of dragging a connection from an existing node to the empty space on the graph.

Defining this method is **optional**. If not overridden, the connection will be created to the first valid port.

### _GetDescription

```csharp
public virtual string _GetDescription()
```

引擎名：`_get_description`

Override this method to define the description of the associated custom node in the Visual Shader Editor's members dialog.

Defining this method is **optional**.

### _GetFuncCode

```csharp
public virtual string _GetFuncCode(Shader.Mode mode, VisualShader.Type type)
```

引擎名：`_get_func_code`

Override this method to add a shader code to the beginning of each shader function (once). The shader code should be returned as a string, which can have multiple lines (the `"""` multiline string construct can be used for convenience).

If there are multiple custom nodes of different types which use this feature the order of each insertion is undefined.

You can customize the generated code based on the shader `mode` and/or `type`.

Defining this method is **optional**.

### _GetGlobalCode

```csharp
public virtual string _GetGlobalCode(Shader.Mode mode)
```

引擎名：`_get_global_code`

Override this method to add shader code on top of the global shader, to define your own standard library of reusable methods, varyings, constants, uniforms, etc. The shader code should be returned as a string, which can have multiple lines (the `"""` multiline string construct can be used for convenience).

Be careful with this functionality as it can cause name conflicts with other custom nodes, so be sure to give the defined entities unique names.

You can customize the generated code based on the shader `mode`.

Defining this method is **optional**.

### _GetInputPortCount

```csharp
public virtual int _GetInputPortCount()
```

引擎名：`_get_input_port_count`

Override this method to define the number of input ports of the associated custom node.

Defining this method is **required**. If not overridden, the node has no input ports.

### _GetInputPortDefaultValue

```csharp
public virtual Variant _GetInputPortDefaultValue(int port)
```

引擎名：`_get_input_port_default_value`

Override this method to define the default value for the specified input port. Prefer use this over [`VisualShaderNode.SetInputPortDefaultValue`](VisualShaderNode.md).

Defining this method is **required**. If not overridden, the node has no default values for their input ports.

### _GetInputPortName

```csharp
public virtual string _GetInputPortName(int port)
```

引擎名：`_get_input_port_name`

Override this method to define the names of input ports of the associated custom node. The names are used both for the input slots in the editor and as identifiers in the shader code, and are passed in the `input_vars` array in `_GetCode()`.

Defining this method is **optional**, but recommended. If not overridden, input ports are named as `"in" + str(port)`.

### _GetInputPortType

```csharp
public virtual VisualShaderNode.PortType _GetInputPortType(int port)
```

引擎名：`_get_input_port_type`

Override this method to define the returned type of each input port of the associated custom node.

Defining this method is **optional**, but recommended. If not overridden, input ports will return the [`VisualShaderNode.PortType.Scalar`](VisualShaderNode.md) type.

### _GetName

```csharp
public virtual string _GetName()
```

引擎名：`_get_name`

Override this method to define the name of the associated custom node in the Visual Shader Editor's members dialog and graph.

Defining this method is **optional**, but recommended. If not overridden, the node will be named as "Unnamed".

### _GetOutputPortCount

```csharp
public virtual int _GetOutputPortCount()
```

引擎名：`_get_output_port_count`

Override this method to define the number of output ports of the associated custom node.

Defining this method is **required**. If not overridden, the node has no output ports.

### _GetOutputPortName

```csharp
public virtual string _GetOutputPortName(int port)
```

引擎名：`_get_output_port_name`

Override this method to define the names of output ports of the associated custom node. The names are used both for the output slots in the editor and as identifiers in the shader code, and are passed in the `output_vars` array in `_GetCode()`.

Defining this method is **optional**, but recommended. If not overridden, output ports are named as `"out" + str(port)`.

### _GetOutputPortType

```csharp
public virtual VisualShaderNode.PortType _GetOutputPortType(int port)
```

引擎名：`_get_output_port_type`

Override this method to define the returned type of each output port of the associated custom node.

Defining this method is **optional**, but recommended. If not overridden, output ports will return the [`VisualShaderNode.PortType.Scalar`](VisualShaderNode.md) type.

### _GetPropertyCount

```csharp
public virtual int _GetPropertyCount()
```

引擎名：`_get_property_count`

Override this method to define the number of the properties.

Defining this method is **optional**.

### _GetPropertyDefaultIndex

```csharp
public virtual int _GetPropertyDefaultIndex(int index)
```

引擎名：`_get_property_default_index`

Override this method to define the default index of the property of the associated custom node.

Defining this method is **optional**.

### _GetPropertyName

```csharp
public virtual string _GetPropertyName(int index)
```

引擎名：`_get_property_name`

Override this method to define the names of the property of the associated custom node.

Defining this method is **optional**.

### _GetPropertyOptions

```csharp
public virtual string[] _GetPropertyOptions(int index)
```

引擎名：`_get_property_options`

Override this method to define the options inside the drop-down list property of the associated custom node.

Defining this method is **optional**.

### _GetReturnIconType

```csharp
public virtual VisualShaderNode.PortType _GetReturnIconType()
```

引擎名：`_get_return_icon_type`

Override this method to define the return icon of the associated custom node in the Visual Shader Editor's members dialog.

Defining this method is **optional**. If not overridden, no return icon is shown.

### _IsAvailable

```csharp
public virtual bool _IsAvailable(Shader.Mode mode, VisualShader.Type type)
```

引擎名：`_is_available`

Override this method to prevent the node to be visible in the member dialog for the certain `mode` and/or `type`.

Defining this method is **optional**. If not overridden, it's `true`.

### _IsHighend

```csharp
public virtual bool _IsHighend()
```

引擎名：`_is_highend`

Override this method to enable the high-end mark in the Visual Shader Editor's members dialog. This should return `true` for nodes that only work when using the Forward+ and Mobile renderers.

Defining this method is **optional**. If not overridden, it's `false`, which indicates this node works with all renderers (including Compatibility).
