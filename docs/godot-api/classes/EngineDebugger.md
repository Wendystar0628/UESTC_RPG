<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EngineDebugger

命名空间 `Godot`；static class

`EngineDebugger` handles the communication between the editor and the running game. It is active in the running game. Messages can be sent/received through it. It also manages the profilers.

## 属性

### Singleton

```csharp
public static EngineDebuggerInstance Singleton { get; }
```

## 方法

### ClearBreakpoints

```csharp
public static void ClearBreakpoints()
```

引擎名：`clear_breakpoints`

Clears all breakpoints.

### Debug

```csharp
public static void Debug(bool canContinue = true, bool isErrorBreakpoint = false)
```

引擎名：`debug`

Starts a debug break in script execution, optionally specifying whether the program can continue based on `canContinue` and whether the break was due to a breakpoint.

### GetDepth

```csharp
public static int GetDepth()
```

引擎名：`get_depth`

Returns the current debug depth.

### GetLinesLeft

```csharp
public static int GetLinesLeft()
```

引擎名：`get_lines_left`

Returns the number of lines that remain.

### HasCapture

```csharp
public static bool HasCapture(StringName name)
```

引擎名：`has_capture`

Returns `true` if a capture with the given name is present otherwise `false`.

### HasProfiler

```csharp
public static bool HasProfiler(StringName name)
```

引擎名：`has_profiler`

Returns `true` if a profiler with the given name is present otherwise `false`.

### InsertBreakpoint

```csharp
public static void InsertBreakpoint(int line, StringName source)
```

引擎名：`insert_breakpoint`

Inserts a new breakpoint with the given `source` and `line`.

### IsActive

```csharp
public static bool IsActive()
```

引擎名：`is_active`

Returns `true` if the debugger is active otherwise `false`.

### IsBreakpoint

```csharp
public static bool IsBreakpoint(int line, StringName source)
```

引擎名：`is_breakpoint`

Returns `true` if the given `source` and `line` represent an existing breakpoint.

### IsProfiling

```csharp
public static bool IsProfiling(StringName name)
```

引擎名：`is_profiling`

Returns `true` if a profiler with the given name is present and active otherwise `false`.

### IsSkippingBreakpoints

```csharp
public static bool IsSkippingBreakpoints()
```

引擎名：`is_skipping_breakpoints`

Returns `true` if the debugger is skipping breakpoints otherwise `false`.

### LinePoll

```csharp
public static void LinePoll()
```

引擎名：`line_poll`

Forces a processing loop of debugger events. The purpose of this method is just processing events every now and then when the script might get too busy, so that bugs like infinite loops can be caught.

### ProfilerAddFrameData

```csharp
public static void ProfilerAddFrameData(StringName name, Array data)
```

引擎名：`profiler_add_frame_data`

Calls the `add` callable of the profiler with given `name` and `data`.

### ProfilerEnable

```csharp
public static void ProfilerEnable(StringName name, bool enable, Array arguments = null)
```

引擎名：`profiler_enable`

Calls the `toggle` callable of the profiler with given `name` and `arguments`. Enables/Disables the same profiler depending on `enable` argument.

### RegisterMessageCapture

```csharp
public static void RegisterMessageCapture(StringName name, Callable callable)
```

引擎名：`register_message_capture`

Registers a message capture with given `name`. If `name` is "my_message" then messages starting with "my_message:" will be called with the given callable.

The callable must accept a message string and a data array as argument. The callable should return `true` if the message is recognized.

**Note:** The callable will receive the message with the prefix stripped, unlike `EditorDebuggerPlugin._capture`. See the `EditorDebuggerPlugin` description for an example.

### RegisterProfiler

```csharp
public static void RegisterProfiler(StringName name, EngineProfiler profiler)
```

引擎名：`register_profiler`

Registers a profiler with the given `name`. See [`EngineProfiler`](EngineProfiler.md) for more information.

### RemoveBreakpoint

```csharp
public static void RemoveBreakpoint(int line, StringName source)
```

引擎名：`remove_breakpoint`

Removes a breakpoint with the given `source` and `line`.

### ScriptDebug

```csharp
public static void ScriptDebug(ScriptLanguage language, bool canContinue = true, bool isErrorBreakpoint = false)
```

引擎名：`script_debug`

Starts a debug break in script execution, optionally specifying whether the program can continue based on `canContinue` and whether the break was due to a breakpoint.

### SendMessage

```csharp
public static void SendMessage(string message, Array data)
```

引擎名：`send_message`

Sends a message with given `message` and `data` array.

### SetDepth

```csharp
public static void SetDepth(int depth)
```

引擎名：`set_depth`

Sets the current debugging depth.

### SetLinesLeft

```csharp
public static void SetLinesLeft(int lines)
```

引擎名：`set_lines_left`

Sets the current debugging lines that remain.

### UnregisterMessageCapture

```csharp
public static void UnregisterMessageCapture(StringName name)
```

引擎名：`unregister_message_capture`

Unregisters the message capture with given `name`.

### UnregisterProfiler

```csharp
public static void UnregisterProfiler(StringName name)
```

引擎名：`unregister_profiler`

Unregisters a profiler with given `name`.
