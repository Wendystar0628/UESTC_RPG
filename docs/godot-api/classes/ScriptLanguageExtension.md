<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ScriptLanguageExtension

命名空间 `Godot`；class；继承 [`ScriptLanguage`](ScriptLanguage.md) → [`GodotObject`](GodotObject.md)

## 构造函数

### ScriptLanguageExtension

```csharp
public ScriptLanguageExtension()
```

## 方法

### _AddGlobalConstant

```csharp
public virtual void _AddGlobalConstant(StringName name, Variant value)
```

引擎名：`_add_global_constant`

### _AddNamedGlobalConstant

```csharp
public virtual void _AddNamedGlobalConstant(StringName name, Variant value)
```

引擎名：`_add_named_global_constant`

### _AutoIndentCode

```csharp
public virtual string _AutoIndentCode(string code, int fromLine, int toLine)
```

引擎名：`_auto_indent_code`

### _CanInheritFromFile

```csharp
public virtual bool _CanInheritFromFile()
```

引擎名：`_can_inherit_from_file`

### _CanMakeFunction

```csharp
public virtual bool _CanMakeFunction()
```

引擎名：`_can_make_function`

### _CompleteCode

```csharp
public virtual Dictionary _CompleteCode(string code, string path, GodotObject owner)
```

引擎名：`_complete_code`

### _CreateScript

```csharp
public virtual GodotObject _CreateScript()
```

引擎名：`_create_script`

### _DebugGetCurrentStackInfo

```csharp
public virtual Array<Dictionary> _DebugGetCurrentStackInfo()
```

引擎名：`_debug_get_current_stack_info`

### _DebugGetError

```csharp
public virtual string _DebugGetError()
```

引擎名：`_debug_get_error`

### _DebugGetGlobals

```csharp
public virtual Dictionary _DebugGetGlobals(int maxSubitems, int maxDepth)
```

引擎名：`_debug_get_globals`

### _DebugGetStackLevelCount

```csharp
public virtual int _DebugGetStackLevelCount()
```

引擎名：`_debug_get_stack_level_count`

### _DebugGetStackLevelFunction

```csharp
public virtual string _DebugGetStackLevelFunction(int level)
```

引擎名：`_debug_get_stack_level_function`

### _DebugGetStackLevelLine

```csharp
public virtual int _DebugGetStackLevelLine(int level)
```

引擎名：`_debug_get_stack_level_line`

### _DebugGetStackLevelLocals

```csharp
public virtual Dictionary _DebugGetStackLevelLocals(int level, int maxSubitems, int maxDepth)
```

引擎名：`_debug_get_stack_level_locals`

### _DebugGetStackLevelMembers

```csharp
public virtual Dictionary _DebugGetStackLevelMembers(int level, int maxSubitems, int maxDepth)
```

引擎名：`_debug_get_stack_level_members`

### _DebugGetStackLevelSource

```csharp
public virtual string _DebugGetStackLevelSource(int level)
```

引擎名：`_debug_get_stack_level_source`

Returns the source associated with a given debug stack position.

### _DebugParseStackLevelExpression

```csharp
public virtual string _DebugParseStackLevelExpression(int level, string expression, int maxSubitems, int maxDepth)
```

引擎名：`_debug_parse_stack_level_expression`

### _FindFunction

```csharp
public virtual int _FindFunction(string function, string code)
```

引擎名：`_find_function`

Returns the line where the function is defined in the code, or `-1` if the function is not present.

### _Finish

```csharp
public virtual void _Finish()
```

引擎名：`_finish`

### _Frame

```csharp
public virtual void _Frame()
```

引擎名：`_frame`

### _GetBuiltInTemplates

```csharp
public virtual Array<Dictionary> _GetBuiltInTemplates(StringName object)
```

引擎名：`_get_built_in_templates`

### _GetCommentDelimiters

```csharp
public virtual string[] _GetCommentDelimiters()
```

引擎名：`_get_comment_delimiters`

### _GetDocCommentDelimiters

```csharp
public virtual string[] _GetDocCommentDelimiters()
```

引擎名：`_get_doc_comment_delimiters`

### _GetExtension

```csharp
public virtual string _GetExtension()
```

引擎名：`_get_extension`

### _GetGlobalClassName

```csharp
public virtual Dictionary _GetGlobalClassName(string path)
```

引擎名：`_get_global_class_name`

### _GetName

```csharp
public virtual string _GetName()
```

引擎名：`_get_name`

### _GetPublicAnnotations

```csharp
public virtual Array<Dictionary> _GetPublicAnnotations()
```

引擎名：`_get_public_annotations`

### _GetPublicConstants

```csharp
public virtual Dictionary _GetPublicConstants()
```

引擎名：`_get_public_constants`

### _GetPublicFunctions

```csharp
public virtual Array<Dictionary> _GetPublicFunctions()
```

引擎名：`_get_public_functions`

### _GetRecognizedExtensions

```csharp
public virtual string[] _GetRecognizedExtensions()
```

引擎名：`_get_recognized_extensions`

### _GetReservedWords

```csharp
public virtual string[] _GetReservedWords()
```

引擎名：`_get_reserved_words`

### _GetStringDelimiters

```csharp
public virtual string[] _GetStringDelimiters()
```

引擎名：`_get_string_delimiters`

### _GetType

```csharp
public virtual string _GetType()
```

引擎名：`_get_type`

### _HandlesGlobalClassType

```csharp
public virtual bool _HandlesGlobalClassType(string type)
```

引擎名：`_handles_global_class_type`

### _HasNamedClasses

```csharp
public virtual bool _HasNamedClasses()
```

引擎名：`_has_named_classes`

### _Init

```csharp
public virtual void _Init()
```

引擎名：`_init`

### _IsControlFlowKeyword

```csharp
public virtual bool _IsControlFlowKeyword(string keyword)
```

引擎名：`_is_control_flow_keyword`

### _IsUsingTemplates

```csharp
public virtual bool _IsUsingTemplates()
```

引擎名：`_is_using_templates`

### _LookupCode

```csharp
public virtual Dictionary _LookupCode(string code, string symbol, string path, GodotObject owner)
```

引擎名：`_lookup_code`

### _MakeFunction

```csharp
public virtual string _MakeFunction(string className, string functionName, string[] functionArgs)
```

引擎名：`_make_function`

### _MakeTemplate

```csharp
public virtual Script _MakeTemplate(string template, string className, string baseClassName)
```

引擎名：`_make_template`

### _OpenInExternalEditor

```csharp
public virtual Error _OpenInExternalEditor(Script script, int line, int column)
```

引擎名：`_open_in_external_editor`

### _OverridesExternalEditor

```csharp
public virtual bool _OverridesExternalEditor()
```

引擎名：`_overrides_external_editor`

### _PreferredFileNameCasing

```csharp
public virtual ScriptLanguage.ScriptNameCasing _PreferredFileNameCasing()
```

引擎名：`_preferred_file_name_casing`

### _ProfilingSetSaveNativeCalls

```csharp
public virtual void _ProfilingSetSaveNativeCalls(bool enable)
```

引擎名：`_profiling_set_save_native_calls`

### _ProfilingStart

```csharp
public virtual void _ProfilingStart()
```

引擎名：`_profiling_start`

### _ProfilingStop

```csharp
public virtual void _ProfilingStop()
```

引擎名：`_profiling_stop`

### _ReloadAllScripts

```csharp
public virtual void _ReloadAllScripts()
```

引擎名：`_reload_all_scripts`

### _ReloadScripts

```csharp
public virtual void _ReloadScripts(Array scripts, bool softReload)
```

引擎名：`_reload_scripts`

Reloads all `scripts` from disk and the specifics of how that happens is `ScriptLanguageExtension` specific.

### _ReloadToolScript

```csharp
public virtual void _ReloadToolScript(Script script, bool softReload)
```

引擎名：`_reload_tool_script`

Reloads the given `script` from disk and the specifics of how that happens is `ScriptLanguageExtension` specific.

### _RemoveNamedGlobalConstant

```csharp
public virtual void _RemoveNamedGlobalConstant(StringName name)
```

引擎名：`_remove_named_global_constant`

### _SupportsBuiltinMode

```csharp
public virtual bool _SupportsBuiltinMode()
```

引擎名：`_supports_builtin_mode`

### _SupportsDocumentation

```csharp
public virtual bool _SupportsDocumentation()
```

引擎名：`_supports_documentation`

### _ThreadEnter

```csharp
public virtual void _ThreadEnter()
```

引擎名：`_thread_enter`

### _ThreadExit

```csharp
public virtual void _ThreadExit()
```

引擎名：`_thread_exit`

### _Validate

```csharp
public virtual Dictionary _Validate(string script, string path, bool validateFunctions, bool validateErrors, bool validateWarnings, bool validateSafeLines)
```

引擎名：`_validate`

### _ValidatePath

```csharp
public virtual string _ValidatePath(string path)
```

引擎名：`_validate_path`

## CodeCompletionKind

命名空间 `Godot`；enum

### 枚举值

- `Class = 0`
- `Function = 1`
- `Signal = 2`
- `Variable = 3`
- `Member = 4`
- `Enum = 5`
- `Constant = 6`
- `NodePath = 7`
- `FilePath = 8`
- `PlainText = 9`
- `Keyword = 10`
- `Max = 11`

## CodeCompletionLocation

命名空间 `Godot`；enum

### 枚举值

- `Local = 0`：The option is local to the location of the code completion query - e.g.
- `ParentMask = 256`：The option is from the containing class or a parent class, relative to the location of the code completion query.
- `OtherUserCode = 512`：The option is from user code which is not local and not in a derived class (e.g.
- `Other = 1024`：The option is from other engine code, not covered by the other enum constants - e.g.

## LookupResultType

命名空间 `Godot`；enum

### 枚举值

- `ScriptLocation = 0`
- `Class = 1`
- `ClassConstant = 2`
- `ClassProperty = 3`
- `ClassMethod = 4`
- `ClassSignal = 5`
- `ClassEnum = 6`
- `ClassTbdGlobalscope = 7`
- `ClassAnnotation = 8`
- `LocalConstant = 9`
- `LocalVariable = 10`
- `Max = 11`
