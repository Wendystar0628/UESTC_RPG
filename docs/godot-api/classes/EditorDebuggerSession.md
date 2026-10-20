<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorDebuggerSession

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

This class cannot be directly instantiated and must be retrieved via an [`EditorDebuggerPlugin`](EditorDebuggerPlugin.md).

You can add tabs to the session UI via `AddSessionTab()`, send messages via `SendMessage()`, and toggle [`EngineProfiler`](EngineProfiler.md)s via `ToggleProfiler()`.

## 方法

### AddSessionTab

```csharp
public void AddSessionTab(Control control)
```

引擎名：`add_session_tab`

Adds the given `control` to the debug session UI in the debugger bottom panel. The `control`'s node name will be used as the tab title.

### IsActive

```csharp
public bool IsActive()
```

引擎名：`is_active`

Returns `true` if the debug session is currently attached to a remote instance.

### IsBreaked

```csharp
public bool IsBreaked()
```

引擎名：`is_breaked`

Returns `true` if the attached remote instance is currently in the debug loop.

### IsDebuggable

```csharp
public bool IsDebuggable()
```

引擎名：`is_debuggable`

Returns `true` if the attached remote instance can be debugged.

### RemoveSessionTab

```csharp
public void RemoveSessionTab(Control control)
```

引擎名：`remove_session_tab`

Removes the given `control` from the debug session UI in the debugger bottom panel.

### SendMessage

```csharp
public void SendMessage(string message, Array data = null)
```

引擎名：`send_message`

Sends the given `message` to the attached remote instance, optionally passing additionally `data`. See [`EngineDebugger`](EngineDebugger.md) for how to retrieve those messages.

### SetBreakpoint

```csharp
public void SetBreakpoint(string path, int line, bool enabled)
```

引擎名：`set_breakpoint`

Enables or disables a specific breakpoint based on `enabled`, updating the Editor Breakpoint Panel accordingly.

### ToggleProfiler

```csharp
public void ToggleProfiler(string profiler, bool enable, Array data = null)
```

引擎名：`toggle_profiler`

Toggle the given `profiler` on the attached remote instance, optionally passing additionally `data`. See [`EngineProfiler`](EngineProfiler.md) for more details.

## 事件（信号）

### Breaked

```csharp
public event EditorDebuggerSession.BreakedEventHandler Breaked
```

引擎名：`breaked`

Emitted when the attached remote instance enters a break state. If `canDebug` is `true`, the remote instance will enter the debug loop.

### Continued

```csharp
public event Action Continued
```

引擎名：`continued`

Emitted when the attached remote instance exits a break state.

### Started

```csharp
public event Action Started
```

引擎名：`started`

Emitted when a remote instance is attached to this session (i.e. the session becomes active).

### Stopped

```csharp
public event Action Stopped
```

引擎名：`stopped`

Emitted when a remote instance is detached from this session (i.e. the session becomes inactive).

## BreakedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorDebuggerSession.Breaked`](EditorDebuggerSession.md) event of a [`EditorDebuggerSession`](EditorDebuggerSession.md) class.

### 签名

#### Invoke

```csharp
delegate void BreakedEventHandler(bool canDebug)
```
