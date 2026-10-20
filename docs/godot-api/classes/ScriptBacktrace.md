<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ScriptBacktrace

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`ScriptBacktrace` holds an already captured backtrace of a specific script language, such as GDScript or C#, which are captured using [`Engine.CaptureScriptBacktraces`](Engine.md).

See `ProjectSettings.debug/settings/gdscript/always_track_call_stacks` and `ProjectSettings.debug/settings/gdscript/always_track_local_variables` for ways of controlling the contents of this class.

## 构造函数

### ScriptBacktrace

```csharp
public ScriptBacktrace()
```

## 方法

### Format

```csharp
public string Format(int indentAll = 0, int indentFrames = 4)
```

引擎名：`format`

Converts the backtrace to a `System.String`, where the entire string will be indented by `indentAll` number of spaces, and the individual stack frames will be additionally indented by `indentFrames` number of spaces.

**Note:** Calling `System.Object.ToString()` on a `ScriptBacktrace` will produce the same output as calling `Format()` with all parameters left at their default values.

### GetFrameCount

```csharp
public int GetFrameCount()
```

引擎名：`get_frame_count`

Returns the number of stack frames in the backtrace.

### GetFrameFile

```csharp
public string GetFrameFile(int index)
```

引擎名：`get_frame_file`

Returns the file name of the call site represented by the stack frame at the specified index.

### GetFrameFunction

```csharp
public string GetFrameFunction(int index)
```

引擎名：`get_frame_function`

Returns the name of the function called at the stack frame at the specified index.

### GetFrameLine

```csharp
public int GetFrameLine(int index)
```

引擎名：`get_frame_line`

Returns the line number of the call site represented by the stack frame at the specified index.

### GetGlobalVariableCount

```csharp
public int GetGlobalVariableCount()
```

引擎名：`get_global_variable_count`

Returns the number of global variables (e.g. autoload singletons) in the backtrace.

**Note:** This will be non-zero only if the `include_variables` parameter was `true` when capturing the backtrace with [`Engine.CaptureScriptBacktraces`](Engine.md).

### GetGlobalVariableName

```csharp
public string GetGlobalVariableName(int variableIndex)
```

引擎名：`get_global_variable_name`

Returns the name of the global variable at the specified index.

### GetGlobalVariableValue

```csharp
public Variant GetGlobalVariableValue(int variableIndex)
```

引擎名：`get_global_variable_value`

Returns the value of the global variable at the specified index.

**Warning:** With GDScript backtraces, the returned [`Variant`](Variant.md) will be the variable's actual value, including any object references. This means that storing the returned [`Variant`](Variant.md) will prevent any such object from being deallocated, so it's generally recommended not to do so.

### GetLanguageName

```csharp
public string GetLanguageName()
```

引擎名：`get_language_name`

Returns the name of the script language that this backtrace was captured from.

### GetLocalVariableCount

```csharp
public int GetLocalVariableCount(int frameIndex)
```

引擎名：`get_local_variable_count`

Returns the number of local variables in the stack frame at the specified index.

**Note:** This will be non-zero only if the `include_variables` parameter was `true` when capturing the backtrace with [`Engine.CaptureScriptBacktraces`](Engine.md).

### GetLocalVariableName

```csharp
public string GetLocalVariableName(int frameIndex, int variableIndex)
```

引擎名：`get_local_variable_name`

Returns the name of the local variable at the specified `variableIndex` in the stack frame at the specified `frameIndex`.

### GetLocalVariableValue

```csharp
public Variant GetLocalVariableValue(int frameIndex, int variableIndex)
```

引擎名：`get_local_variable_value`

Returns the value of the local variable at the specified `variableIndex` in the stack frame at the specified `frameIndex`.

**Warning:** With GDScript backtraces, the returned [`Variant`](Variant.md) will be the variable's actual value, including any object references. This means that storing the returned [`Variant`](Variant.md) will prevent any such object from being deallocated, so it's generally recommended not to do so.

### GetMemberVariableCount

```csharp
public int GetMemberVariableCount(int frameIndex)
```

引擎名：`get_member_variable_count`

Returns the number of member variables in the stack frame at the specified index.

**Note:** This will be non-zero only if the `include_variables` parameter was `true` when capturing the backtrace with [`Engine.CaptureScriptBacktraces`](Engine.md).

### GetMemberVariableName

```csharp
public string GetMemberVariableName(int frameIndex, int variableIndex)
```

引擎名：`get_member_variable_name`

Returns the name of the member variable at the specified `variableIndex` in the stack frame at the specified `frameIndex`.

### GetMemberVariableValue

```csharp
public Variant GetMemberVariableValue(int frameIndex, int variableIndex)
```

引擎名：`get_member_variable_value`

Returns the value of the member variable at the specified `variableIndex` in the stack frame at the specified `frameIndex`.

**Warning:** With GDScript backtraces, the returned [`Variant`](Variant.md) will be the variable's actual value, including any object references. This means that storing the returned [`Variant`](Variant.md) will prevent any such object from being deallocated, so it's generally recommended not to do so.

### IsEmpty

```csharp
public bool IsEmpty()
```

引擎名：`is_empty`

Returns `true` if the backtrace has no stack frames.
