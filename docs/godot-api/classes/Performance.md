<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Performance

命名空间 `Godot`；static class

This class provides access to a number of different monitors related to performance, such as memory usage, draw calls, and FPS. These are the same as the values displayed in the **Monitor** tab in the editor's **Debugger** panel. By using the `GetMonitor()` method of this class, you can access this data from your code.

You can add custom monitors using the `AddCustomMonitor()` method. Custom monitors are available in **Monitor** tab in the editor's **Debugger** panel together with built-in monitors.

**Note:** Some of the built-in monitors are only available in debug mode and will always return `0` when used in a project exported in release mode.

**Note:** Some of the built-in monitors are not updated in real-time for performance reasons, so there may be a delay of up to 1 second between changes.

**Note:** Custom monitors do not support negative values. Negative values are clamped to 0.

## 属性

### Singleton

```csharp
public static PerformanceInstance Singleton { get; }
```

## 方法

### AddCustomMonitor

```csharp
public static void AddCustomMonitor(StringName id, Callable callable, Array arguments = null, Performance.MonitorType type = Performance.MonitorType.Quantity)
```

引擎名：`add_custom_monitor`

Adds a custom monitor with the name `id`. You can specify the category of the monitor using slash delimiters in `id` (for example: `"Game/NumberOfNPCs"`). If there is more than one slash delimiter, then the default category is used. The default category is `"Custom"`. Prints an error if given `id` is already present.

```csharp
public override void _Ready()
{
var monitorValue = new Callable(this, MethodName.GetMonitorValue);

// Adds monitor with name "MyName" to category "MyCategory".
Performance.AddCustomMonitor("MyCategory/MyMonitor", monitorValue);
// Adds monitor with name "MyName" to category "Custom".
// Note: "MyCategory/MyMonitor" and "MyMonitor" have same name but different ids so the code is valid.
Performance.AddCustomMonitor("MyMonitor", monitorValue);

// Adds monitor with name "MyName" to category "Custom".
// Note: "MyMonitor" and "Custom/MyMonitor" have same name and same category but different ids so the code is valid.
Performance.AddCustomMonitor("Custom/MyMonitor", monitorValue);

// Adds monitor with name "MyCategoryOne/MyCategoryTwo/MyMonitor" to category "Custom".
Performance.AddCustomMonitor("MyCategoryOne/MyCategoryTwo/MyMonitor", monitorValue);
}

public int GetMonitorValue()
{
return GD.Randi() % 25;
}

```

The debugger calls the callable to get the value of custom monitor. The callable must return a zero or positive integer or floating-point number.

Callables are called with arguments supplied in argument array.

### AddCustomMonitor

```csharp
public static void AddCustomMonitor(StringName id, Callable callable, Array arguments)
```

引擎名：`add_custom_monitor`

Adds a custom monitor with the name `id`. You can specify the category of the monitor using slash delimiters in `id` (for example: `"Game/NumberOfNPCs"`). If there is more than one slash delimiter, then the default category is used. The default category is `"Custom"`. Prints an error if given `id` is already present.

```csharp
public override void _Ready()
{
var monitorValue = new Callable(this, MethodName.GetMonitorValue);

// Adds monitor with name "MyName" to category "MyCategory".
Performance.AddCustomMonitor("MyCategory/MyMonitor", monitorValue);
// Adds monitor with name "MyName" to category "Custom".
// Note: "MyCategory/MyMonitor" and "MyMonitor" have same name but different ids so the code is valid.
Performance.AddCustomMonitor("MyMonitor", monitorValue);

// Adds monitor with name "MyName" to category "Custom".
// Note: "MyMonitor" and "Custom/MyMonitor" have same name and same category but different ids so the code is valid.
Performance.AddCustomMonitor("Custom/MyMonitor", monitorValue);

// Adds monitor with name "MyCategoryOne/MyCategoryTwo/MyMonitor" to category "Custom".
Performance.AddCustomMonitor("MyCategoryOne/MyCategoryTwo/MyMonitor", monitorValue);
}

public int GetMonitorValue()
{
return GD.Randi() % 25;
}

```

The debugger calls the callable to get the value of custom monitor. The callable must return a zero or positive integer or floating-point number.

Callables are called with arguments supplied in argument array.

### GetCustomMonitor

```csharp
public static Variant GetCustomMonitor(StringName id)
```

引擎名：`get_custom_monitor`

Returns the value of custom monitor with given `id`. The callable is called to get the value of custom monitor. See also `HasCustomMonitor()`. Prints an error if the given `id` is absent.

### GetCustomMonitorNames

```csharp
public static Array<StringName> GetCustomMonitorNames()
```

引擎名：`get_custom_monitor_names`

Returns the names of active custom monitors in an [`Collections.Array`](Collections.Array.md).

### GetCustomMonitorTypes

```csharp
public static int[] GetCustomMonitorTypes()
```

引擎名：`get_custom_monitor_types`

Returns the [`Performance.MonitorType`](Performance.md) values of active custom monitors in an [`Collections.Array`](Collections.Array.md).

### GetMonitor

```csharp
public static double GetMonitor(Performance.Monitor monitor)
```

引擎名：`get_monitor`

Returns the value of one of the available built-in monitors. You should provide one of the [`Performance.Monitor`](Performance.md) constants as the argument, like this:

```csharp
GD.Print(Performance.GetMonitor(Performance.Monitor.TimeFps)); // Prints the FPS to the console.

```

See `GetCustomMonitor()` to query custom performance monitors' values.

### GetMonitorModificationTime

```csharp
public static ulong GetMonitorModificationTime()
```

引擎名：`get_monitor_modification_time`

Returns the last tick in which custom monitor was added/removed (in microseconds since the engine started). This is set to [`Time.GetTicksUsec`](Time.md) when the monitor is updated.

### HasCustomMonitor

```csharp
public static bool HasCustomMonitor(StringName id)
```

引擎名：`has_custom_monitor`

Returns `true` if custom monitor with the given `id` is present, `false` otherwise.

### RemoveCustomMonitor

```csharp
public static void RemoveCustomMonitor(StringName id)
```

引擎名：`remove_custom_monitor`

Removes the custom monitor with given `id`. Prints an error if the given `id` is already absent.

## Monitor

命名空间 `Godot`；enum

### 枚举值

- `TimeFps = 0`：The number of frames rendered in the last second.
- `TimeProcess = 1`：Time it took to complete one frame, in seconds.
- `TimePhysicsProcess = 2`：Time it took to complete one physics frame, in seconds.
- `TimeNavigationProcess = 3`：Time it took to complete one navigation step, in seconds.
- `MemoryStatic = 4`：Static memory currently used, in bytes.
- `MemoryStaticMax = 5`：Available static memory.
- `MemoryMessageBufferMax = 6`：Largest amount of memory the message queue buffer has used, in bytes.
- `ObjectCount = 7`：Number of objects currently instantiated (including nodes).
- `ObjectResourceCount = 8`：Number of resources currently used.
- `ObjectNodeCount = 9`：Number of nodes currently instantiated in the scene tree.
- `ObjectOrphanNodeCount = 10`：Number of orphan nodes, i.e.
- `RenderTotalObjectsInFrame = 11`：The total number of objects in the last rendered frame.
- `RenderTotalPrimitivesInFrame = 12`：The total number of vertices or indices rendered in the last rendered frame.
- `RenderTotalDrawCallsInFrame = 13`：The total number of draw calls performed in the last rendered frame.
- `RenderVideoMemUsed = 14`：The amount of video memory used (texture and vertex memory combined, in bytes).
- `RenderTextureMemUsed = 15`：The amount of texture memory used (in bytes).
- `RenderBufferMemUsed = 16`：The amount of render buffer memory used (in bytes).
- `Physics2DActiveObjects = 17`：Number of active [`RigidBody2D`](RigidBody2D.md) nodes in the game.
- `Physics2DCollisionPairs = 18`：Number of collision pairs in the 2D physics engine.
- `Physics2DIslandCount = 19`：Number of islands in the 2D physics engine.
- `Physics3DActiveObjects = 20`：Number of active [`RigidBody3D`](RigidBody3D.md) and [`VehicleBody3D`](VehicleBody3D.md) nodes in the game.
- `Physics3DCollisionPairs = 21`：Number of collision pairs in the 3D physics engine.
- `Physics3DIslandCount = 22`：Number of islands in the 3D physics engine.
- `AudioOutputLatency = 23`：Output latency of the [`AudioServer`](AudioServer.md).
- `NavigationActiveMaps = 24`：Number of active navigation maps in [`NavigationServer2D`](NavigationServer2D.md) and [`NavigationServer3D`](NavigationServer3D.md).
- `NavigationRegionCount = 25`：Number of active navigation regions in [`NavigationServer2D`](NavigationServer2D.md) and [`NavigationServer3D`](NavigationServer3D.md).
- `NavigationAgentCount = 26`：Number of active navigation agents processing avoidance in [`NavigationServer2D`](NavigationServer2D.md) and [`NavigationServer3D`](NavigationServer3D.md).
- `NavigationLinkCount = 27`：Number of active navigation links in [`NavigationServer2D`](NavigationServer2D.md) and [`NavigationServer3D`](NavigationServer3D.md).
- `NavigationPolygonCount = 28`：Number of navigation mesh polygons in [`NavigationServer2D`](NavigationServer2D.md) and [`NavigationServer3D`](NavigationServer3D.md).
- `NavigationEdgeCount = 29`：Number of navigation mesh polygon edges in [`NavigationServer2D`](NavigationServer2D.md) and [`NavigationServer3D`](NavigationServer3D.md).
- `NavigationEdgeMergeCount = 30`：Number of navigation mesh polygon edges that were merged due to edge key overlap in [`NavigationServer2D`](NavigationServer2D.md) and [`NavigationServer3D`](NavigationServer3D.md).
- `NavigationEdgeConnectionCount = 31`：Number of polygon edges that are considered connected by edge proximity [`NavigationServer2D`](NavigationServer2D.md) and [`NavigationServer3D`](NavigationServer3D.md).
- `NavigationEdgeFreeCount = 32`：Number of navigation mesh polygon edges that could not be merged in [`NavigationServer2D`](NavigationServer2D.md) and [`NavigationServer3D`](NavigationServer3D.md).
- `NavigationObstacleCount = 33`：Number of active navigation obstacles in the [`NavigationServer2D`](NavigationServer2D.md) and [`NavigationServer3D`](NavigationServer3D.md).
- `PipelineCompilationsCanvas = 34`：Number of pipeline compilations that were triggered by the 2D canvas renderer.
- `PipelineCompilationsMesh = 35`：Number of pipeline compilations that were triggered by loading meshes.
- `PipelineCompilationsSurface = 36`：Number of pipeline compilations that were triggered by building the surface cache before rendering the scene.
- `PipelineCompilationsDraw = 37`：Number of pipeline compilations that were triggered while drawing the scene.
- `PipelineCompilationsSpecialization = 38`：Number of pipeline compilations that were triggered to optimize the current scene.
- `Navigation2DActiveMaps = 39`：Number of active navigation maps in the [`NavigationServer2D`](NavigationServer2D.md).
- `Navigation2DRegionCount = 40`：Number of active navigation regions in the [`NavigationServer2D`](NavigationServer2D.md).
- `Navigation2DAgentCount = 41`：Number of active navigation agents processing avoidance in the [`NavigationServer2D`](NavigationServer2D.md).
- `Navigation2DLinkCount = 42`：Number of active navigation links in the [`NavigationServer2D`](NavigationServer2D.md).
- `Navigation2DPolygonCount = 43`：Number of navigation mesh polygons in the [`NavigationServer2D`](NavigationServer2D.md).
- `Navigation2DEdgeCount = 44`：Number of navigation mesh polygon edges in the [`NavigationServer2D`](NavigationServer2D.md).
- `Navigation2DEdgeMergeCount = 45`：Number of navigation mesh polygon edges that were merged due to edge key overlap in the [`NavigationServer2D`](NavigationServer2D.md).
- `Navigation2DEdgeConnectionCount = 46`：Number of polygon edges that are considered connected by edge proximity [`NavigationServer2D`](NavigationServer2D.md).
- `Navigation2DEdgeFreeCount = 47`：Number of navigation mesh polygon edges that could not be merged in the [`NavigationServer2D`](NavigationServer2D.md).
- `Navigation2DObstacleCount = 48`：Number of active navigation obstacles in the [`NavigationServer2D`](NavigationServer2D.md).
- `Navigation3DActiveMaps = 49`：Number of active navigation maps in the [`NavigationServer3D`](NavigationServer3D.md).
- `Navigation3DRegionCount = 50`：Number of active navigation regions in the [`NavigationServer3D`](NavigationServer3D.md).
- `Navigation3DAgentCount = 51`：Number of active navigation agents processing avoidance in the [`NavigationServer3D`](NavigationServer3D.md).
- `Navigation3DLinkCount = 52`：Number of active navigation links in the [`NavigationServer3D`](NavigationServer3D.md).
- `Navigation3DPolygonCount = 53`：Number of navigation mesh polygons in the [`NavigationServer3D`](NavigationServer3D.md).
- `Navigation3DEdgeCount = 54`：Number of navigation mesh polygon edges in the [`NavigationServer3D`](NavigationServer3D.md).
- `Navigation3DEdgeMergeCount = 55`：Number of navigation mesh polygon edges that were merged due to edge key overlap in the [`NavigationServer3D`](NavigationServer3D.md).
- `Navigation3DEdgeConnectionCount = 56`：Number of polygon edges that are considered connected by edge proximity [`NavigationServer3D`](NavigationServer3D.md).
- `Navigation3DEdgeFreeCount = 57`：Number of navigation mesh polygon edges that could not be merged in the [`NavigationServer3D`](NavigationServer3D.md).
- `Navigation3DObstacleCount = 58`：Number of active navigation obstacles in the [`NavigationServer3D`](NavigationServer3D.md).
- `MonitorMax = 59`：Represents the size of the `Performance.Monitor` enum.

## MonitorType

命名空间 `Godot`；enum

### 枚举值

- `Quantity = 0`：Monitor output is formatted as an integer value.
- `Memory = 1`：Monitor output is formatted as computer memory.
- `Time = 2`：Monitor output is formatted as time in milliseconds.
- `Percentage = 3`：Monitor output is formatted as a percentage.
