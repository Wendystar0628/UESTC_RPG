<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorDebuggerPlugin

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

`EditorDebuggerPlugin` provides functions related to the editor side of the debugger.

To interact with the debugger, an instance of this class must be added to the editor via [`EditorPlugin.AddDebuggerPlugin`](EditorPlugin.md).

Once added, the `_SetupSession()` callback will be called for every [`EditorDebuggerSession`](EditorDebuggerSession.md) available to the plugin, and when new ones are created (the sessions may be inactive during this stage).

You can retrieve the available [`EditorDebuggerSession`](EditorDebuggerSession.md)s via `GetSessions()` or get a specific one via `GetSession()`.

To connect on the running game side, use the [`EngineDebugger`](EngineDebugger.md) singleton:

**Note:** While the game is running, `GD.Print` and similar functions *called in the editor* do not print anything, the Output Log prints only game messages.

## 构造函数

### EditorDebuggerPlugin

```csharp
public EditorDebuggerPlugin()
```

## 方法

### GetSession

```csharp
public EditorDebuggerSession GetSession(int id)
```

引擎名：`get_session`

Returns the [`EditorDebuggerSession`](EditorDebuggerSession.md) with the given `id`.

### GetSessions

```csharp
public Array GetSessions()
```

引擎名：`get_sessions`

Returns an array of [`EditorDebuggerSession`](EditorDebuggerSession.md) currently available to this debugger plugin.

**Note:** Sessions in the array may be inactive, check their state via [`EditorDebuggerSession.IsActive`](EditorDebuggerSession.md).

### _BreakpointSetInTree

```csharp
public virtual void _BreakpointSetInTree(Script script, int line, bool enabled)
```

引擎名：`_breakpoint_set_in_tree`

Override this method to be notified when a breakpoint is set in the editor.

### _BreakpointsClearedInTree

```csharp
public virtual void _BreakpointsClearedInTree()
```

引擎名：`_breakpoints_cleared_in_tree`

Override this method to be notified when all breakpoints are cleared in the editor.

### _Capture

```csharp
public virtual bool _Capture(string message, Array data, int sessionId)
```

引擎名：`_capture`

Override this method to process incoming messages. The `sessionId` is the ID of the [`EditorDebuggerSession`](EditorDebuggerSession.md) that received the `message`. Use `GetSession()` to retrieve the session. This method should return `true` if the message is recognized.

### _GotoScriptLine

```csharp
public virtual void _GotoScriptLine(Script script, int line)
```

引擎名：`_goto_script_line`

Override this method to be notified when a breakpoint line has been clicked in the debugger breakpoint panel.

### _HasCapture

```csharp
public virtual bool _HasCapture(string capture)
```

引擎名：`_has_capture`

Override this method to enable receiving messages from the debugger. If `capture` is "my_message" then messages starting with "my_message:" will be passed to the `_Capture()` method.

### _SetupSession

```csharp
public virtual void _SetupSession(int sessionId)
```

引擎名：`_setup_session`

Override this method to be notified whenever a new [`EditorDebuggerSession`](EditorDebuggerSession.md) is created. Note that the session may be inactive during this stage.
