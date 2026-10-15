<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ScriptExtension

命名空间 `Godot`；class；继承 [`Script`](Script.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

## 构造函数

### ScriptExtension

```csharp
public ScriptExtension()
```

## 方法

### _CanInstantiate

```csharp
public virtual bool _CanInstantiate()
```

引擎名：`_can_instantiate`

### _EditorCanReloadFromFile

```csharp
public virtual bool _EditorCanReloadFromFile()
```

引擎名：`_editor_can_reload_from_file`

### _GetBaseScript

```csharp
public virtual Script _GetBaseScript()
```

引擎名：`_get_base_script`

### _GetClassIconPath

```csharp
public virtual string _GetClassIconPath()
```

引擎名：`_get_class_icon_path`

### _GetConstants

```csharp
public virtual Dictionary _GetConstants()
```

引擎名：`_get_constants`

### _GetDocClassName

```csharp
public virtual StringName _GetDocClassName()
```

引擎名：`_get_doc_class_name`

### _GetDocumentation

```csharp
public virtual Array<Dictionary> _GetDocumentation()
```

引擎名：`_get_documentation`

### _GetGlobalName

```csharp
public virtual StringName _GetGlobalName()
```

引擎名：`_get_global_name`

### _GetInstanceBaseType

```csharp
public virtual StringName _GetInstanceBaseType()
```

引擎名：`_get_instance_base_type`

### _GetLanguage

```csharp
public virtual ScriptLanguage _GetLanguage()
```

引擎名：`_get_language`

### _GetMemberLine

```csharp
public virtual int _GetMemberLine(StringName member)
```

引擎名：`_get_member_line`

### _GetMembers

```csharp
public virtual Array<StringName> _GetMembers()
```

引擎名：`_get_members`

### _GetMethodInfo

```csharp
public virtual Dictionary _GetMethodInfo(StringName method)
```

引擎名：`_get_method_info`

### _GetPropertyDefaultValue

```csharp
public virtual Variant _GetPropertyDefaultValue(StringName property)
```

引擎名：`_get_property_default_value`

### _GetRpcConfig

```csharp
public virtual Variant _GetRpcConfig()
```

引擎名：`_get_rpc_config`

### _GetScriptMethodArgumentCount

```csharp
public virtual Variant _GetScriptMethodArgumentCount(StringName method)
```

引擎名：`_get_script_method_argument_count`

Return the expected argument count for the given `method`, or `null` if it can't be determined (which will then fall back to the default behavior).

### _GetScriptMethodList

```csharp
public virtual Array<Dictionary> _GetScriptMethodList()
```

引擎名：`_get_script_method_list`

### _GetScriptPropertyList

```csharp
public virtual Array<Dictionary> _GetScriptPropertyList()
```

引擎名：`_get_script_property_list`

### _GetScriptSignalList

```csharp
public virtual Array<Dictionary> _GetScriptSignalList()
```

引擎名：`_get_script_signal_list`

### _GetSourceCode

```csharp
public virtual string _GetSourceCode()
```

引擎名：`_get_source_code`

### _HasMethod

```csharp
public virtual bool _HasMethod(StringName method)
```

引擎名：`_has_method`

### _HasPropertyDefaultValue

```csharp
public virtual bool _HasPropertyDefaultValue(StringName property)
```

引擎名：`_has_property_default_value`

### _HasScriptSignal

```csharp
public virtual bool _HasScriptSignal(StringName signal)
```

引擎名：`_has_script_signal`

### _HasSourceCode

```csharp
public virtual bool _HasSourceCode()
```

引擎名：`_has_source_code`

### _HasStaticMethod

```csharp
public virtual bool _HasStaticMethod(StringName method)
```

引擎名：`_has_static_method`

### _InheritsScript

```csharp
public virtual bool _InheritsScript(Script script)
```

引擎名：`_inherits_script`

### _InstanceHas

```csharp
public virtual bool _InstanceHas(GodotObject object)
```

引擎名：`_instance_has`

### _IsAbstract

```csharp
public virtual bool _IsAbstract()
```

引擎名：`_is_abstract`

Returns `true` if the script is an abstract script. Abstract scripts cannot be instantiated directly, instead other scripts should inherit them. Abstract scripts will be either unselectable or hidden in the Create New Node dialog (unselectable if there are non-abstract classes inheriting it, otherwise hidden).

### _IsPlaceholderFallbackEnabled

```csharp
public virtual bool _IsPlaceholderFallbackEnabled()
```

引擎名：`_is_placeholder_fallback_enabled`

### _IsTool

```csharp
public virtual bool _IsTool()
```

引擎名：`_is_tool`

### _IsValid

```csharp
public virtual bool _IsValid()
```

引擎名：`_is_valid`

### _Reload

```csharp
public virtual Error _Reload(bool keepState)
```

引擎名：`_reload`

### _SetSourceCode

```csharp
public virtual void _SetSourceCode(string code)
```

引擎名：`_set_source_code`

### _UpdateExports

```csharp
public virtual void _UpdateExports()
```

引擎名：`_update_exports`
