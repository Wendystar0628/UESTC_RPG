<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Logger

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Custom logger to receive messages from the internal error/warning stream. Loggers are registered via [`OS.AddLogger`](OS.md).

## 构造函数

### Logger

```csharp
public Logger()
```

## 方法

### _LogError

```csharp
public virtual void _LogError(string function, string file, int line, string code, string rationale, bool editorNotify, int errorType, Array<ScriptBacktrace> scriptBacktraces)
```

引擎名：`_log_error`

Called when an error is logged. The error provides the `function`, `file`, and `line` that it originated from, as well as either the `code` that generated the error or a `rationale`.

The type of error provided by `errorType` is described in the [`Logger.ErrorType`](Logger.md) enumeration.

Additionally, `scriptBacktraces` provides backtraces for each of the script languages. These will only contain stack frames in editor builds and debug builds by default. To enable them for release builds as well, you need to enable `ProjectSettings.debug/settings/gdscript/always_track_call_stacks`.

**Warning:** This method will be called from threads other than the main thread, possibly at the same time, so you will need to have some kind of thread-safety in your implementation of it, like a [`Mutex`](Mutex.md).

**Note:** `scriptBacktraces` will not contain any captured variables, due to its prohibitively high cost. To get those, you will need to capture the backtraces yourself, from within the `Logger` virtual methods, using [`Engine.CaptureScriptBacktraces`](Engine.md).

**Note:** Logging errors from this method using functions like `GD.PushError` or `GD.PushWarning` is not supported, as it could cause infinite recursion. These errors will only show up in the console output.

### _LogMessage

```csharp
public virtual void _LogMessage(string message, bool error)
```

引擎名：`_log_message`

Called when a message is logged. If `error` is `true`, then this message was meant to be sent to `stderr`.

**Warning:** This method will be called from threads other than the main thread, possibly at the same time, so you will need to have some kind of thread-safety in your implementation of it, like a [`Mutex`](Mutex.md).

**Note:** Logging another message from this method using functions like `GD.Print` is not supported, as it could cause infinite recursion. These messages will only show up in the console output.

## ErrorType

命名空间 `Godot`；enum

### 枚举值

- `Error = 0`：The message received is an error.
- `Warning = 1`：The message received is a warning.
- `Script = 2`：The message received is a script error.
- `Shader = 3`：The message received is a shader error.
