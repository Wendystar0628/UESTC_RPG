<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EngineInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

The [`Engine`](Engine.md) singleton allows you to query and modify the project's run-time parameters, such as frames per second, time scale, and others. It also stores information about the current build of Godot, such as the current version.

## 属性

### MaxFps

```csharp
public int MaxFps { get; set; }
```

The maximum number of frames that can be rendered every second (FPS). A value of `0` means the framerate is uncapped.

Limiting the FPS can be useful to reduce the host machine's power consumption, which reduces heat, noise emissions, and improves battery life.

If `ProjectSettings.display/window/vsync/vsync_mode` is **Enabled** or **Adaptive**, the setting takes precedence and the max FPS number cannot exceed the monitor's refresh rate. See also [`DisplayServer.ScreenGetRefreshRate`](DisplayServer.md).

If `ProjectSettings.display/window/vsync/vsync_mode` is **Enabled**, on monitors with variable refresh rate enabled (G-Sync/FreeSync), using an FPS limit a few frames lower than the monitor's refresh rate will [reduce input lag while avoiding tearing](https://blurbusters.com/howto-low-lag-vsync-on/). At higher refresh rates, the difference between the FPS limit and the monitor refresh rate should be increased to ensure frames to account for timing inaccuracies. The optimal formula for the FPS limit value in this scenario is `r - (r * r) / 3600.0`, where `r` is the monitor's refresh rate.

**Note:** The actual number of frames per second may still be below this value if the CPU or GPU cannot keep up with the project's logic and rendering.

**Note:** If `ProjectSettings.display/window/vsync/vsync_mode` is **Disabled**, limiting the FPS to a high value that can be consistently reached on the system can reduce input lag compared to an uncapped framerate. Since this works by ensuring the GPU load is lower than 100%, this latency reduction is only effective in GPU-bottlenecked scenarios, not CPU-bottlenecked scenarios.

### MaxPhysicsStepsPerFrame

```csharp
public int MaxPhysicsStepsPerFrame { get; set; }
```

The maximum number of physics steps that can be simulated each rendered frame.

**Note:** The default value is tuned to prevent expensive physics simulations from triggering even more expensive simulations indefinitely. However, the game will appear to slow down if the rendering FPS is less than `1 / max_physics_steps_per_frame` of `PhysicsTicksPerSecond`. This occurs even if `delta` is consistently used in physics calculations. To avoid this, increase `MaxPhysicsStepsPerFrame` if you have increased `PhysicsTicksPerSecond` significantly above its default value.

### PhysicsJitterFix

```csharp
public double PhysicsJitterFix { get; set; }
```

How much physics ticks are synchronized with real time. If `0` or less, the ticks are fully synchronized. Higher values cause the in-game clock to deviate more from the real clock, but they smooth out framerate jitters.

**Note:** The default value of `0.5` should be good enough for most cases; values above `2` could cause the game to react to dropped frames with a noticeable delay and are not recommended.

**Note:** When using a custom physics interpolation solution, or within a network game, it's recommended to disable the physics jitter fix by setting this property to `0`.

### PhysicsTicksPerSecond

```csharp
public int PhysicsTicksPerSecond { get; set; }
```

The number of fixed iterations per second. This controls how often physics simulation and the [`Node._PhysicsProcess()`](Node.md) method are run.

CPU usage scales approximately with the physics tick rate. However, at very low tick rates (usually below 30), physics behavior can break down. Input can also become less responsive at low tick rates as there can be a gap between input being registered, and the response on the next physics tick. High tick rates give more accurate physics simulation, particularly for fast moving objects. For example, racing games may benefit from increasing the tick rate above the default 60.

See also `MaxFps` and `ProjectSettings.physics/common/physics_ticks_per_second`.

**Note:** Only `MaxPhysicsStepsPerFrame` physics ticks may be simulated per rendered frame at most. If more physics ticks have to be simulated per rendered frame to keep up with rendering, the project will appear to slow down (even if `delta` is used consistently in physics calculations). Therefore, it is recommended to also increase `MaxPhysicsStepsPerFrame` if increasing `PhysicsTicksPerSecond` significantly above its default value.

**Note:** Consider enabling [physics interpolation]($DOCS_URL/tutorials/physics/interpolation/index.html) if you change `PhysicsTicksPerSecond` to a value that is not a multiple of `60`. Using physics interpolation will avoid jittering when the monitor refresh rate and physics update rate don't exactly match.

### PrintErrorMessages

```csharp
public bool PrintErrorMessages { get; set; }
```

If `false`, stops printing error and warning messages to the console and editor Output log. This can be used to hide error and warning messages during unit test suite runs. This property is equivalent to the `ProjectSettings.application/run/disable_stderr` project setting.

**Note:** This property does not impact the editor's Errors tab when running a project from the editor.

**Warning:** If set to `false` anywhere in the project, important error messages may be hidden even if they are emitted from other scripts. In a `@tool` script, this will also impact the editor itself. Do *not* report bugs before ensuring error messages are enabled (as they are by default).

### PrintToStdOut

```csharp
public bool PrintToStdOut { get; set; }
```

If `false`, stops printing messages (for example using `GD.Print`) to the console, log files, and editor Output log. This property is equivalent to the `ProjectSettings.application/run/disable_stdout` project setting.

**Note:** This does not stop printing errors or warnings produced by scripts to the console or log files, for more details see `PrintErrorMessages`.

### TimeScale

```csharp
public double TimeScale { get; set; }
```

The speed multiplier at which the in-game clock updates, compared to real time. For example, if set to `2.0` the game runs twice as fast, and if set to `0.5` the game runs half as fast.

This value affects [`Timer`](Timer.md), [`SceneTreeTimer`](SceneTreeTimer.md), and all other simulations that make use of `delta` time (such as [`Node._Process()`](Node.md) and [`Node._PhysicsProcess()`](Node.md)).

**Note:** It's recommended to keep this property above `0.0`, as the game may behave unexpectedly otherwise.

**Note:** This does not affect audio playback speed. Use [`AudioServer.PlaybackSpeedScale`](AudioServer.md) to adjust audio playback speed independently of [`Engine.TimeScale`](Engine.md).

**Note:** This does not automatically adjust `PhysicsTicksPerSecond`. With values above `1.0` physics simulation may become less precise, as each physics tick will stretch over a larger period of engine time. If you're modifying [`Engine.TimeScale`](Engine.md) to speed up simulation by a large factor, consider also increasing `PhysicsTicksPerSecond` to make the simulation more reliable.

## 方法

### CaptureScriptBacktraces

```csharp
public Array<ScriptBacktrace> CaptureScriptBacktraces(bool includeVariables = false)
```

Captures and returns backtraces from all registered script languages.

By default, the returned [`ScriptBacktrace`](ScriptBacktrace.md) will only contain stack frames in editor builds and debug builds. To enable them for release builds as well, you need to enable `ProjectSettings.debug/settings/gdscript/always_track_call_stacks`.

If `includeVariables` is `true`, the backtrace will also include the names and values of any global variables (e.g. autoload singletons) at the point of the capture, as well as local variables and class member variables at each stack frame. This will however will only be respected when running the game with a debugger attached, like when running the game from the editor. To enable it for export builds as well, you need to enable `ProjectSettings.debug/settings/gdscript/always_track_local_variables`.

**Warning:** When `includeVariables` is `true`, any captured variables can potentially (e.g. with GDScript backtraces) be their actual values, including any object references. This means that storing such a [`ScriptBacktrace`](ScriptBacktrace.md) will prevent those objects from being deallocated, so it's generally recommended not to do so.

### GetArchitectureName

```csharp
public string GetArchitectureName()
```

Returns the name of the CPU architecture the Godot binary was built for. Possible return values include `"x86_64"`, `"x86_32"`, `"arm64"`, `"arm32"`, `"rv64"`, `"ppc64"`, `"loongarch64"`, `"wasm64"`, and `"wasm32"`.

To detect whether the current build is 64-bit, or the type of architecture, don't use the architecture name. Instead, use [`OS.HasFeature`](OS.md) to check for the `"64"` feature tag, or tags such as `"x86"` or `"arm"`. See the [Feature Tags]($DOCS_URL/tutorials/export/feature_tags.html) documentation for more details.

**Note:** This method does *not* return the name of the system's CPU architecture (like [`OS.GetProcessorName`](OS.md)). For example, when running an `x86_32` Godot binary on an `x86_64` system, the returned value will still be `"x86_32"`.

### GetAuthorInfo

```csharp
public Dictionary GetAuthorInfo()
```

Returns the engine author information as a [`Collections.Dictionary`](Collections.Dictionary.md), where each entry is an [`Collections.Array`](Collections.Array.md) of strings with the names of notable contributors to the Godot Engine: `lead_developers`, `founders`, `project_managers`, and `developers`.

### GetCopyrightInfo

```csharp
public Array<Dictionary> GetCopyrightInfo()
```

Returns an [`Collections.Array`](Collections.Array.md) of dictionaries with copyright information for every component of Godot's source code.

Every [`Collections.Dictionary`](Collections.Dictionary.md) contains a `name` identifier, and a `parts` array of dictionaries. It describes the component in detail with the following entries:

- `files` - [`Collections.Array`](Collections.Array.md) of file paths from the source code affected by this component;

- `copyright` - [`Collections.Array`](Collections.Array.md) of owners of this component;

- `license` - The license applied to this component (such as "[Expat](https://en.wikipedia.org/wiki/MIT_License#Ambiguity_and_variants)" or "[CC-BY-4.0](https://creativecommons.org/licenses/by/4.0/)").

### GetDonorInfo

```csharp
public Dictionary GetDonorInfo()
```

Returns a [`Collections.Dictionary`](Collections.Dictionary.md) of categorized donor names. Each entry is an [`Collections.Array`](Collections.Array.md) of strings:

{`platinum_sponsors`, `gold_sponsors`, `silver_sponsors`, `bronze_sponsors`, `mini_sponsors`, `gold_donors`, `silver_donors`, `bronze_donors`}

### GetFramesDrawn

```csharp
public int GetFramesDrawn()
```

Returns the total number of frames drawn since the engine started.

**Note:** On headless platforms, or if rendering is disabled with `--disable-render-loop` via command line, this method always returns `0`. See also `GetProcessFrames()`.

### GetFramesPerSecond

```csharp
public double GetFramesPerSecond()
```

Returns the average frames rendered every second (FPS), also known as the framerate.

### GetLicenseInfo

```csharp
public Dictionary GetLicenseInfo()
```

Returns a [`Collections.Dictionary`](Collections.Dictionary.md) of licenses used by Godot and included third party components. Each entry is a license name (such as "[Expat](https://en.wikipedia.org/wiki/MIT_License#Ambiguity_and_variants)") and its associated text.

### GetLicenseText

```csharp
public string GetLicenseText()
```

Returns the full Godot license text.

### GetMainLoop

```csharp
public MainLoop GetMainLoop()
```

Returns the instance of the [`MainLoop`](MainLoop.md). This is usually the main [`SceneTree`](SceneTree.md) and is the same as [`Node.GetTree`](Node.md).

**Note:** The type instantiated as the main loop can changed with `ProjectSettings.application/run/main_loop_type`.

### GetMaxFps

```csharp
public int GetMaxFps()
```

### GetMaxPhysicsStepsPerFrame

```csharp
public int GetMaxPhysicsStepsPerFrame()
```

### GetPhysicsFrames

```csharp
public ulong GetPhysicsFrames()
```

Returns the total number of frames passed since the engine started. This number is increased every **physics frame**. See also `GetProcessFrames()`.

This method can be used to run expensive logic less often without relying on a [`Timer`](Timer.md):

```csharp
public override void _PhysicsProcess(double delta)
{
base._PhysicsProcess(delta);

if (Engine.GetPhysicsFrames() % 2 == 0)
{
// Run expensive logic only once every 2 physics frames here.
}
}

```

### GetPhysicsInterpolationFraction

```csharp
public double GetPhysicsInterpolationFraction()
```

Returns the fraction through the current physics tick we are at the time of rendering the frame. This can be used to implement fixed timestep interpolation.

### GetPhysicsJitterFix

```csharp
public double GetPhysicsJitterFix()
```

### GetPhysicsTicksPerSecond

```csharp
public int GetPhysicsTicksPerSecond()
```

### GetProcessFrames

```csharp
public ulong GetProcessFrames()
```

Returns the total number of frames passed since the engine started. This number is increased every **process frame**, regardless of whether the render loop is enabled. See also `GetFramesDrawn()` and `GetPhysicsFrames()`.

This method can be used to run expensive logic less often without relying on a [`Timer`](Timer.md):

```csharp
public override void _Process(double delta)
{
base._Process(delta);

if (Engine.GetProcessFrames() % 5 == 0)
{
// Run expensive logic only once every 5 process (render) frames here.
}
}

```

### GetScriptLanguage

```csharp
public ScriptLanguage GetScriptLanguage(int index)
```

Returns an instance of a [`ScriptLanguage`](ScriptLanguage.md) with the given `index`.

### GetScriptLanguageCount

```csharp
public int GetScriptLanguageCount()
```

Returns the number of available script languages. Use with `GetScriptLanguage()`.

### GetSingleton

```csharp
public GodotObject GetSingleton(StringName name)
```

Returns the global singleton with the given `name`, or `null` if it does not exist. Often used for plugins. See also `HasSingleton()` and `GetSingletonList()`.

**Note:** Global singletons are not the same as autoloaded nodes, which are configurable in the project settings.

### GetSingletonList

```csharp
public string[] GetSingletonList()
```

Returns a list of names of all available global singletons. See also `GetSingleton()`.

### GetTimeScale

```csharp
public double GetTimeScale()
```

### GetVersionInfo

```csharp
public Dictionary GetVersionInfo()
```

Returns the current engine version information as a [`Collections.Dictionary`](Collections.Dictionary.md) containing the following entries:

- `major` - Major version number as an int;

- `minor` - Minor version number as an int;

- `patch` - Patch version number as an int;

- `hex` - Full version encoded as a hexadecimal int with one byte (2 hex digits) per number (see example below);

- `status` - Status (such as "beta", "rc1", "rc2", "stable", etc.) as a String;

- `build` - Build name (e.g. "custom_build") as a String;

- `hash` - Full Git commit hash as a String;

- `timestamp` - Holds the Git commit date UNIX timestamp in seconds as an int, or `0` if unavailable;

- `string` - `major`, `minor`, `patch`, `status`, and `build` in a single String.

The `hex` value is encoded as follows, from left to right: one byte for the major, one byte for the minor, one byte for the patch version. For example, "3.1.12" would be `0x03010C`.

**Note:** The `hex` value is still an `System.Int32` internally, and printing it will give you its decimal representation, which is not particularly meaningful. Use hexadecimal literals for quick version comparisons from code:

```csharp
if ((int)Engine.GetVersionInfo()["hex"] >= 0x040100)
{
// Do things specific to version 4.1 or later.
}
else
{
// Do things specific to versions before 4.1.
}

```

### GetWriteMoviePath

```csharp
public string GetWriteMoviePath()
```

Returns the path to the [`MovieWriter`](MovieWriter.md)'s output file, or an empty string if the engine wasn't started in Movie Maker mode. The default path can be changed in `ProjectSettings.editor/movie_writer/movie_file`.

### HasSingleton

```csharp
public bool HasSingleton(StringName name)
```

Returns `true` if a singleton with the given `name` exists in the global scope. See also `GetSingleton()`.

```csharp
GD.Print(Engine.HasSingleton("OS"));          // Prints True
GD.Print(Engine.HasSingleton("Engine"));      // Prints True
GD.Print(Engine.HasSingleton("AudioServer")); // Prints True
GD.Print(Engine.HasSingleton("Unknown"));     // Prints False

```

**Note:** Global singletons are not the same as autoloaded nodes, which are configurable in the project settings.

### IsEditorHint

```csharp
public bool IsEditorHint()
```

Returns `true` if the script is currently running inside the editor, otherwise returns `false`. This is useful for `@tool` scripts to conditionally draw editor helpers, or prevent accidentally running "game" code that would affect the scene state while in the editor:

```csharp
if (Engine.IsEditorHint())
DrawGizmos();
else
SimulatePhysics();

```

See [Running code in the editor]($DOCS_URL/tutorials/plugins/running_code_in_the_editor.html) in the documentation for more information.

**Note:** To detect whether the script is running on an editor *build* (such as when pressing F5), use [`OS.HasFeature`](OS.md) with the `"editor"` argument instead. `OS.has_feature("editor")` evaluate to `true` both when the script is running in the editor and when running the project from the editor, but returns `false` when run from an exported project.

### IsEmbeddedInEditor

```csharp
public bool IsEmbeddedInEditor()
```

Returns `true` if the engine is running embedded in the editor. This is useful to prevent attempting to update window mode or window flags that are not supported when running the project embedded in the editor.

### IsInPhysicsFrame

```csharp
public bool IsInPhysicsFrame()
```

Returns `true` if the engine is inside the fixed physics process step of the main loop.

```csharp
func _enter_tree():
# Depending on when the node is added to the tree,
# prints either "true" or "false".
print(Engine.is_in_physics_frame())

func _process(delta):
print(Engine.is_in_physics_frame()) # Prints false

func _physics_process(delta):
print(Engine.is_in_physics_frame()) # Prints true

```

### IsPrintingErrorMessages

```csharp
public bool IsPrintingErrorMessages()
```

### IsPrintingToStdOut

```csharp
public bool IsPrintingToStdOut()
```

### RegisterScriptLanguage

```csharp
public Error RegisterScriptLanguage(ScriptLanguage language)
```

Registers a [`ScriptLanguage`](ScriptLanguage.md) instance to be available with `ScriptServer`.

Returns:

- [`Error.Ok`](Error.md) on success;

- [`Error.Unavailable`](Error.md) if `ScriptServer` has reached the limit and cannot register any new language;

- [`Error.AlreadyExists`](Error.md) if `ScriptServer` already contains a language with similar extension/name/type.

### RegisterSingleton

```csharp
public void RegisterSingleton(StringName name, GodotObject instance)
```

Registers the given [`GodotObject`](GodotObject.md) `instance` as a singleton, available globally under `name`. Useful for plugins.

### SetMaxFps

```csharp
public void SetMaxFps(int maxFps)
```

### SetMaxPhysicsStepsPerFrame

```csharp
public void SetMaxPhysicsStepsPerFrame(int maxPhysicsSteps)
```

### SetPhysicsJitterFix

```csharp
public void SetPhysicsJitterFix(double physicsJitterFix)
```

### SetPhysicsTicksPerSecond

```csharp
public void SetPhysicsTicksPerSecond(int physicsTicksPerSecond)
```

### SetPrintErrorMessages

```csharp
public void SetPrintErrorMessages(bool enabled)
```

### SetPrintToStdOut

```csharp
public void SetPrintToStdOut(bool enabled)
```

### SetTimeScale

```csharp
public void SetTimeScale(double timeScale)
```

### UnregisterScriptLanguage

```csharp
public Error UnregisterScriptLanguage(ScriptLanguage language)
```

Unregisters the [`ScriptLanguage`](ScriptLanguage.md) instance from `ScriptServer`.

Returns:

- [`Error.Ok`](Error.md) on success;

- [`Error.DoesNotExist`](Error.md) if the language is not registered in `ScriptServer`.

### UnregisterSingleton

```csharp
public void UnregisterSingleton(StringName name)
```

Removes the singleton registered under `name`. The singleton object is *not* freed. Only works with user-defined singletons registered with `RegisterSingleton()`.
