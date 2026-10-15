<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GodotInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

GodotInstance represents a running Godot instance that is controlled from an outside codebase, without a perpetual main loop. It is created by the C API `libgodot_create_godot_instance`. Only one may be created per process.

## 方法

### FocusIn

```csharp
public void FocusIn()
```

引擎名：`focus_in`

Notifies the instance that it is now in focus.

### FocusOut

```csharp
public void FocusOut()
```

引擎名：`focus_out`

Notifies the instance that it is now not in focus.

### IsStarted

```csharp
public bool IsStarted()
```

引擎名：`is_started`

Returns `true` if this instance has been fully started.

### Iteration

```csharp
public bool Iteration()
```

引擎名：`iteration`

Runs a single iteration of the main loop. Returns `true` if the engine is attempting to quit.

### Pause

```csharp
public void Pause()
```

引擎名：`pause`

Notifies the instance that it is going to be paused.

### Resume

```csharp
public void Resume()
```

引擎名：`resume`

Notifies the instance that it is being resumed.

### Start

```csharp
public bool Start()
```

引擎名：`start`

Finishes this instance's startup sequence. Returns `true` on success.
