<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EngineDebuggerInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

[`EngineDebugger`](EngineDebugger.md) handles the communication between the editor and the running game. It is active in the running game. Messages can be sent/received through it. It also manages the profilers.

## 方法

### ClearBreakpoints

```csharp
public void ClearBreakpoints()
```

Clears all breakpoints.

### Debug

```csharp
public void Debug(bool canContinue = true, bool isErrorBreakpoint = false)
```

Starts a debug break in script execution, optionally specifying whether the program can continue based on `canContinue` and whether the break was due to a breakpoint.

### GetDepth

```csharp
public int GetDepth()
```

Returns the current debug depth.

### GetLinesLeft

```csharp
public int GetLinesLeft()
```

Returns the number of lines that remain.

### HasCapture

```csharp
public bool HasCapture(StringName name)
```

Returns `true` if a capture with the given name is present otherwise `false`.

### HasProfiler

```csharp
public bool HasProfiler(StringName name)
```

Returns `true` if a profiler with the given name is present otherwise `false`.

### InsertBreakpoint

```csharp
public void InsertBreakpoint(int line, StringName source)
```

Inserts a new breakpoint with the given `source` and `line`.

### IsActive

```csharp
public bool IsActive()
```

Returns `true` if the debugger is active otherwise `false`.

### IsBreakpoint

```csharp
public bool IsBreakpoint(int line, StringName source)
```

Returns `true` if the given `source` and `line` represent an existing breakpoint.

### IsProfiling

```csharp
public bool IsProfiling(StringName name)
```

Returns `true` if a profiler with the given name is present and active otherwise `false`.

### IsSkippingBreakpoints

```csharp
public bool IsSkippingBreakpoints()
```

Returns `true` if the debugger is skipping breakpoints otherwise `false`.

### LinePoll

```csharp
public void LinePoll()
```

Forces a processing loop of debugger events. The purpose of this method is just processing events every now and then when the script might get too busy, so that bugs like infinite loops can be caught.

### ProfilerAddFrameData

```csharp
public void ProfilerAddFrameData(StringName name, Array data)
```

Calls the `add` callable of the profiler with given `name` and `data`.

### ProfilerEnable

```csharp
public void ProfilerEnable(StringName name, bool enable, Array arguments = null)
```

Calls the `toggle` callable of the profiler with given `name` and `arguments`. Enables/Disables the same profiler depending on `enable` argument.

### RegisterMessageCapture

```csharp
public void RegisterMessageCapture(StringName name, Callable callable)
```

Registers a message capture with given `name`. If `name` is "my_message" then messages starting with "my_message:" will be called with the given callable.

The callable must accept a message string and a data array as argument. The callable should return `true` if the message is recognized.

**Note:** The callable will receive the message with the prefix stripped, unlike `EditorDebuggerPlugin._capture`. See the `EditorDebuggerPlugin` description for an example.

### RegisterProfiler

```csharp
public void RegisterProfiler(StringName name, EngineProfiler profiler)
```

Registers a profiler with the given `name`. See [`EngineProfiler`](EngineProfiler.md) for more information.

### RemoveBreakpoint

```csharp
public void RemoveBreakpoint(int line, StringName source)
```

Removes a breakpoint with the given `source` and `line`.

### ScriptDebug

```csharp
public void ScriptDebug(ScriptLanguage language, bool canContinue = true, bool isErrorBreakpoint = false)
```

Starts a debug break in script execution, optionally specifying whether the program can continue based on `canContinue` and whether the break was due to a breakpoint.

### SendMessage

```csharp
public void SendMessage(string message, Array data)
```

Sends a message with given `message` and `data` array.

### SetDepth

```csharp
public void SetDepth(int depth)
```

Sets the current debugging depth.

### SetLinesLeft

```csharp
public void SetLinesLeft(int lines)
```

Sets the current debugging lines that remain.

### UnregisterMessageCapture

```csharp
public void UnregisterMessageCapture(StringName name)
```

Unregisters the message capture with given `name`.

### UnregisterProfiler

```csharp
public void UnregisterProfiler(StringName name)
```

Unregisters a profiler with given `name`.
