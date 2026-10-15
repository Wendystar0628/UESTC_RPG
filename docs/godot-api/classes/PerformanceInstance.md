<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PerformanceInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

This class provides access to a number of different monitors related to performance, such as memory usage, draw calls, and FPS. These are the same as the values displayed in the **Monitor** tab in the editor's **Debugger** panel. By using the `GetMonitor()` method of this class, you can access this data from your code.

You can add custom monitors using the `AddCustomMonitor()` method. Custom monitors are available in **Monitor** tab in the editor's **Debugger** panel together with built-in monitors.

**Note:** Some of the built-in monitors are only available in debug mode and will always return `0` when used in a project exported in release mode.

**Note:** Some of the built-in monitors are not updated in real-time for performance reasons, so there may be a delay of up to 1 second between changes.

**Note:** Custom monitors do not support negative values. Negative values are clamped to 0.

## 方法

### AddCustomMonitor

```csharp
public void AddCustomMonitor(StringName id, Callable callable, Array arguments = null, Performance.MonitorType type = Performance.MonitorType.Quantity)
```

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
public void AddCustomMonitor(StringName id, Callable callable, Array arguments)
```

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
public Variant GetCustomMonitor(StringName id)
```

Returns the value of custom monitor with given `id`. The callable is called to get the value of custom monitor. See also `HasCustomMonitor()`. Prints an error if the given `id` is absent.

### GetCustomMonitorNames

```csharp
public Array<StringName> GetCustomMonitorNames()
```

Returns the names of active custom monitors in an [`Collections.Array`](Collections.Array.md).

### GetCustomMonitorTypes

```csharp
public int[] GetCustomMonitorTypes()
```

Returns the [`Performance.MonitorType`](Performance.md) values of active custom monitors in an [`Collections.Array`](Collections.Array.md).

### GetMonitor

```csharp
public double GetMonitor(Performance.Monitor monitor)
```

Returns the value of one of the available built-in monitors. You should provide one of the [`Performance.Monitor`](Performance.md) constants as the argument, like this:

```csharp
GD.Print(Performance.GetMonitor(Performance.Monitor.TimeFps)); // Prints the FPS to the console.

```

See `GetCustomMonitor()` to query custom performance monitors' values.

### GetMonitorModificationTime

```csharp
public ulong GetMonitorModificationTime()
```

Returns the last tick in which custom monitor was added/removed (in microseconds since the engine started). This is set to [`Time.GetTicksUsec`](Time.md) when the monitor is updated.

### HasCustomMonitor

```csharp
public bool HasCustomMonitor(StringName id)
```

Returns `true` if custom monitor with the given `id` is present, `false` otherwise.

### RemoveCustomMonitor

```csharp
public void RemoveCustomMonitor(StringName id)
```

Removes the custom monitor with given `id`. Prints an error if the given `id` is already absent.
