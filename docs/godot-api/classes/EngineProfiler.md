<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EngineProfiler

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class can be used to implement custom profilers that are able to interact with the engine and editor debugger.

See [`EngineDebugger`](EngineDebugger.md) and `EditorDebuggerPlugin` for more information.

## 构造函数

### EngineProfiler

```csharp
public EngineProfiler()
```

## 方法

### _AddFrame

```csharp
public virtual void _AddFrame(Array data)
```

引擎名：`_add_frame`

Called when data is added to profiler using [`EngineDebugger.ProfilerAddFrameData`](EngineDebugger.md).

### _Tick

```csharp
public virtual void _Tick(double frameTime, double processTime, double physicsTime, double physicsFrameTime)
```

引擎名：`_tick`

Called once every engine iteration when the profiler is active with information about the current frame. All time values are in seconds. Lower values represent faster processing times and are therefore considered better.

### _Toggle

```csharp
public virtual void _Toggle(bool enable, Array options)
```

引擎名：`_toggle`

Called when the profiler is enabled/disabled, along with a set of `options`.
