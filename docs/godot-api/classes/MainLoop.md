<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# MainLoop

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

`MainLoop` is the abstract base class for a Godot project's game loop. It is inherited by [`SceneTree`](SceneTree.md), which is the default game loop implementation used in Godot projects, though it is also possible to write and use one's own `MainLoop` subclass instead of the scene tree.

Upon the application start, a `MainLoop` implementation must be provided to the OS; otherwise, the application will exit. This happens automatically (and a [`SceneTree`](SceneTree.md) is created) unless a `MainLoop` [`Script`](Script.md) is provided from the command line (with e.g. `godot -s my_loop.gd`) or the `ProjectSettings.application/run/main_loop_type` project setting is overwritten.

Here is an example script implementing a simple `MainLoop`:

```csharp
using Godot;

[GlobalClass]
public partial class CustomMainLoop : MainLoop
{
private double _timeElapsed = 0;

public override void _Initialize()
{
GD.Print("Initialized:");
GD.Print($"  Starting Time: {_timeElapsed}");
}

public override bool _Process(double delta)
{
_timeElapsed += delta;
// Return true to end the main loop.
return Input.GetMouseButtonMask() != 0 || Input.IsKeyPressed(Key.Escape);
}

private void _Finalize()
{
GD.Print("Finalized:");
GD.Print($"  End Time: {_timeElapsed}");
}
}

```

## 构造函数

### MainLoop

```csharp
public MainLoop()
```

## 方法

### _Finalize

```csharp
public virtual void _Finalize()
```

引擎名：`_finalize`

Called before the program exits.

### _Initialize

```csharp
public virtual void _Initialize()
```

引擎名：`_initialize`

Called once during initialization.

### _PhysicsProcess

```csharp
public virtual bool _PhysicsProcess(double delta)
```

引擎名：`_physics_process`

Called each physics tick. `delta` is the logical time between physics ticks in seconds and is equal to [`Engine.TimeScale`](Engine.md) / [`Engine.PhysicsTicksPerSecond`](Engine.md). Equivalent to [`Node._PhysicsProcess()`](Node.md).

If implemented, the method must return a boolean value. `true` ends the main loop, while `false` lets it proceed to the next step.

**Note:** `_PhysicsProcess()` may be called up to [`Engine.MaxPhysicsStepsPerFrame`](Engine.md) times per (idle) frame. This step limit may be reached when the engine is suffering performance issues.

**Note:** Accumulated `delta` may diverge from real world seconds.

### _Process

```csharp
public virtual bool _Process(double delta)
```

引擎名：`_process`

Called on each idle frame, prior to rendering, and after physics ticks have been processed. `delta` is the time between frames in seconds. Equivalent to [`Node._Process()`](Node.md).

If implemented, the method must return a boolean value. `true` ends the main loop, while `false` lets it proceed to the next frame.

**Note:** When the engine is struggling and the frame rate is lowered, `delta` will increase. When `delta` is increased, it's capped at a maximum of [`Engine.TimeScale`](Engine.md) * [`Engine.MaxPhysicsStepsPerFrame`](Engine.md) / [`Engine.PhysicsTicksPerSecond`](Engine.md). As a result, accumulated `delta` may not represent real world time.

**Note:** When `--fixed-fps` is enabled or the engine is running in Movie Maker mode (see [`MovieWriter`](MovieWriter.md)), process `delta` will always be the same for every frame, regardless of how much time the frame took to render.

**Note:** Frame delta may be post-processed by [`OS.DeltaSmoothing`](OS.md) if this is enabled for the project.

## 事件（信号）

### OnRequestPermissionsResult

```csharp
public event MainLoop.OnRequestPermissionsResultEventHandler OnRequestPermissionsResult
```

引擎名：`on_request_permissions_result`

Emitted when a user responds to a permission request.

## 常量

### NotificationApplicationFocusIn

```csharp
public const long NotificationApplicationFocusIn = 2016
```

Notification received from the OS when the application is focused, i.e. when changing the focus from the OS desktop or a thirdparty application to any open window of the Godot instance.

Implemented on desktop and mobile platforms.

### NotificationApplicationFocusOut

```csharp
public const long NotificationApplicationFocusOut = 2017
```

Notification received from the OS when the application is defocused, i.e. when changing the focus from any open window of the Godot instance to the OS desktop or a thirdparty application.

Implemented on desktop and mobile platforms.

### NotificationApplicationPaused

```csharp
public const long NotificationApplicationPaused = 2015
```

Notification received from the OS when the application is paused.

Specific to the Android and iOS platforms.

**Note:** On iOS, you only have approximately 5 seconds to finish a task started by this signal. If you go over this allotment, iOS will kill the app instead of pausing it.

### NotificationApplicationPipModeEntered

```csharp
public const long NotificationApplicationPipModeEntered = 2019
```

Notification received when the application enters picture-in-picture mode.

### NotificationApplicationPipModeExited

```csharp
public const long NotificationApplicationPipModeExited = 2020
```

Notification received when the application exits picture-in-picture mode.

### NotificationApplicationResumed

```csharp
public const long NotificationApplicationResumed = 2014
```

Notification received from the OS when the application is resumed.

Specific to the Android and iOS platforms.

### NotificationCrash

```csharp
public const long NotificationCrash = 2012
```

Notification received from Godot's crash handler when the engine is about to crash.

Implemented on desktop platforms if the crash handler is enabled.

### NotificationOsImeUpdate

```csharp
public const long NotificationOsImeUpdate = 2013
```

Notification received from the OS when an update of the Input Method Engine occurs (e.g. change of IME cursor position or composition string).

Implemented on desktop and web platforms.

### NotificationOsMemoryWarning

```csharp
public const long NotificationOsMemoryWarning = 2009
```

Notification received from the OS when the application is exceeding its allocated memory.

Specific to the iOS platform.

### NotificationTextServerChanged

```csharp
public const long NotificationTextServerChanged = 2018
```

Notification received when text server is changed.

### NotificationTranslationChanged

```csharp
public const long NotificationTranslationChanged = 2010
```

Notification received when translations may have changed. Can be triggered by the user changing the locale. Can be used to respond to language changes, for example to change the UI strings on the fly. Useful when working with the built-in translation support, like [`GodotObject.Tr`](GodotObject.md).

### NotificationWMAbout

```csharp
public const long NotificationWMAbout = 2011
```

Notification received from the OS when a request for "About" information is sent.

Specific to the macOS platform.

## OnRequestPermissionsResultEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`MainLoop.OnRequestPermissionsResult`](MainLoop.md) event of a [`MainLoop`](MainLoop.md) class.

### 签名

#### Invoke

```csharp
delegate void OnRequestPermissionsResultEventHandler(string permission, bool granted)
```
