<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Script

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A class stored as a resource. A script extends the functionality of all objects that instantiate it.

This is the base class for all scripts and should not be used directly. Trying to create a new script with this class will result in an error.

The `new` method of a script subclass creates a new instance. [`GodotObject.SetScript`](GodotObject.md) extends an existing object, if that object's class matches one of the script's base classes.

## 属性

### SourceCode

```csharp
public string SourceCode { get; set; }
```

引擎名：`source_code`

The script source code or an empty string if source code is not available. When set, does not reload the class implementation automatically.

## 方法

### CanInstantiate

```csharp
public bool CanInstantiate()
```

引擎名：`can_instantiate`

Returns `true` if the script can be instantiated.

### GetBaseScript

```csharp
public Script GetBaseScript()
```

引擎名：`get_base_script`

Returns the script directly inherited by this script.

### GetGlobalName

```csharp
public StringName GetGlobalName()
```

引擎名：`get_global_name`

Returns the class name associated with the script, if there is one. Returns an empty string otherwise.

To give the script a global name, you can use the `class_name` keyword in GDScript and the `[GlobalClass]` attribute in C#.

```csharp
using Godot;

[GlobalClass]
public partial class MyNode : Node
{
}

```

### GetInstanceBaseType

```csharp
public StringName GetInstanceBaseType()
```

引擎名：`get_instance_base_type`

Returns the script's base type.

### GetPropertyDefaultValue

```csharp
public Variant GetPropertyDefaultValue(StringName property)
```

引擎名：`get_property_default_value`

Returns the default value of the specified property.

### GetRpcConfig

```csharp
public Variant GetRpcConfig()
```

引擎名：`get_rpc_config`

Returns a [`Collections.Dictionary`](Collections.Dictionary.md) mapping method names to their RPC configuration defined by this script.

### GetScriptConstantMap

```csharp
public Dictionary GetScriptConstantMap()
```

引擎名：`get_script_constant_map`

Returns a dictionary containing constant names and their values.

### GetScriptMethodList

```csharp
public Array<Dictionary> GetScriptMethodList()
```

引擎名：`get_script_method_list`

Returns the list of methods in this `Script`.

**Note:** The dictionaries returned by this method are formatted identically to those returned by [`GodotObject.GetMethodList`](GodotObject.md).

### GetScriptPropertyList

```csharp
public Array<Dictionary> GetScriptPropertyList()
```

引擎名：`get_script_property_list`

Returns the list of properties in this `Script`.

**Note:** The dictionaries returned by this method are formatted identically to those returned by [`GodotObject.GetPropertyList`](GodotObject.md).

### GetScriptSignalList

```csharp
public Array<Dictionary> GetScriptSignalList()
```

引擎名：`get_script_signal_list`

Returns the list of signals defined in this `Script`.

**Note:** The dictionaries returned by this method are formatted identically to those returned by [`GodotObject.GetSignalList`](GodotObject.md).

### GetSourceCode

```csharp
public string GetSourceCode()
```

引擎名：`get_source_code`

### HasScriptMethod

```csharp
public bool HasScriptMethod(StringName methodName)
```

引擎名：`has_script_method`

Returns `true` if the script, or a base class, defines a method with the given name.

### HasScriptSignal

```csharp
public bool HasScriptSignal(StringName signalName)
```

引擎名：`has_script_signal`

Returns `true` if the script, or a base class, defines a signal with the given name.

### HasSourceCode

```csharp
public bool HasSourceCode()
```

引擎名：`has_source_code`

Returns `true` if the script contains non-empty source code.

**Note:** If a script does not have source code, this does not mean that it is invalid or unusable. For example, a [`GDScript`](GDScript.md) that was exported with binary tokenization has no source code, but still behaves as expected and could be instantiated. This can be checked with `CanInstantiate()`.

### InstanceHas

```csharp
public bool InstanceHas(GodotObject baseObject)
```

引擎名：`instance_has`

Returns `true` if `baseObject` is an instance of this script.

### IsAbstract

```csharp
public bool IsAbstract()
```

引擎名：`is_abstract`

Returns `true` if the script is an abstract script. An abstract script does not have a constructor and cannot be instantiated.

### IsTool

```csharp
public bool IsTool()
```

引擎名：`is_tool`

Returns `true` if the script is a tool script. A tool script can run in the editor.

### Reload

```csharp
public Error Reload(bool keepState = false)
```

引擎名：`reload`

Reloads the script's class implementation. Returns an error code.

### SetSourceCode

```csharp
public void SetSourceCode(string source)
```

引擎名：`set_source_code`
