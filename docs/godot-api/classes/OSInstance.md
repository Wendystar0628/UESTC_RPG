<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OSInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

The [`OS`](OS.md) class wraps the most common functionalities for communicating with the host operating system, such as the video driver, delays, environment variables, execution of binaries, command line, etc.

**Note:** In Godot 4, [`OS`](OS.md) functions related to window management, clipboard, and TTS were moved to the [`DisplayServer`](DisplayServer.md) singleton (and the [`Window`](Window.md) class). Functions related to time were removed and are only available in the [`Time`](Time.md) class.

## 属性

### DeltaSmoothing

```csharp
public bool DeltaSmoothing { get; set; }
```

If `true`, the engine filters the time delta measured between each frame, and attempts to compensate for random variation. This only works on systems where V-Sync is active.

**Note:** On start-up, this is the same as `ProjectSettings.application/run/delta_smoothing`.

### LowProcessorUsageMode

```csharp
public bool LowProcessorUsageMode { get; set; }
```

If `true`, the engine optimizes for low processor usage by only refreshing the screen if needed. Can improve battery consumption on mobile.

**Note:** On start-up, this is the same as `ProjectSettings.application/run/low_processor_mode`.

### LowProcessorUsageModeSleepUsec

```csharp
public int LowProcessorUsageModeSleepUsec { get; set; }
```

The amount of sleeping between frames when the low-processor usage mode is enabled, in microseconds. Higher values will result in lower CPU usage. See also `LowProcessorUsageMode`.

**Note:** On start-up, this is the same as `ProjectSettings.application/run/low_processor_mode_sleep_usec`.

## 方法

### AddLogger

```csharp
public void AddLogger(Logger logger)
```

Add a custom logger to intercept the internal message stream.

### Alert

```csharp
public void Alert(string text, string title = "Alert!")
```

Displays a modal dialog box using the host platform's implementation. The engine execution is blocked until the dialog is closed.

### CloseMidiInputs

```csharp
public void CloseMidiInputs()
```

Shuts down the system MIDI driver. Godot will no longer receive [`InputEventMidi`](InputEventMidi.md). See also `OpenMidiInputs()` and `GetConnectedMidiInputs()`.

**Note:** This method is implemented on Linux, macOS, Windows, and Web.

### Crash

```csharp
public void Crash(string message)
```

Crashes the engine (or the editor if called within a `@tool` script). See also `Kill()`.

**Note:** This method should *only* be used for testing the system's crash handler, not for any other purpose. For general error reporting, use (in order of preference) `@GDScript.assert`, `GD.PushError`, or `Alert()`.

### CreateInstance

```csharp
public int CreateInstance(ReadOnlySpan<string> arguments)
```

Creates a new instance of Godot that runs independently. The `arguments` are used in the given order and separated by a space.

If the process is successfully created, this method returns the new process' ID, which you can use to monitor the process (and potentially terminate it with `Kill()`). If the process cannot be created, this method returns `-1`.

See `CreateProcess()` if you wish to run a different process.

**Note:** This method is implemented on Android, Linux, macOS and Windows.

### CreateInstance

```csharp
public int CreateInstance(string[] arguments)
```

Creates a new instance of Godot that runs independently. The `arguments` are used in the given order and separated by a space.

If the process is successfully created, this method returns the new process' ID, which you can use to monitor the process (and potentially terminate it with `Kill()`). If the process cannot be created, this method returns `-1`.

See `CreateProcess()` if you wish to run a different process.

**Note:** This method is implemented on Android, Linux, macOS and Windows.

### CreateProcess

```csharp
public int CreateProcess(string path, ReadOnlySpan<string> arguments, bool openConsole)
```

Creates a new process that runs independently of Godot. It will not terminate when Godot terminates. The path specified in `path` must exist and be an executable file or macOS `.app` bundle. The path is resolved based on the current platform. The `arguments` are used in the given order and separated by a space.

On Windows, if `openConsole` is `true` and the process is a console app, a new terminal window will be opened.

If the process is successfully created, this method returns its process ID, which you can use to monitor the process (and potentially terminate it with `Kill()`). Otherwise, this method returns `-1`.

**Example:** Run another instance of the project:

```csharp
var pid = OS.CreateProcess(OS.GetExecutablePath(), []);

```

See `Execute()` if you wish to run an external command and retrieve the results.

**Note:** This method is implemented on Android, Linux, macOS, and Windows.

**Note:** On macOS, sandboxed applications are limited to run only embedded helper executables, specified during export or system .app bundle, system .app bundles will ignore arguments.

### CreateProcess

```csharp
public int CreateProcess(string path, string[] arguments, bool openConsole = false)
```

Creates a new process that runs independently of Godot. It will not terminate when Godot terminates. The path specified in `path` must exist and be an executable file or macOS `.app` bundle. The path is resolved based on the current platform. The `arguments` are used in the given order and separated by a space.

On Windows, if `openConsole` is `true` and the process is a console app, a new terminal window will be opened.

If the process is successfully created, this method returns its process ID, which you can use to monitor the process (and potentially terminate it with `Kill()`). Otherwise, this method returns `-1`.

**Example:** Run another instance of the project:

```csharp
var pid = OS.CreateProcess(OS.GetExecutablePath(), []);

```

See `Execute()` if you wish to run an external command and retrieve the results.

**Note:** This method is implemented on Android, Linux, macOS, and Windows.

**Note:** On macOS, sandboxed applications are limited to run only embedded helper executables, specified during export or system .app bundle, system .app bundles will ignore arguments.

### DelayMsec

```csharp
public void DelayMsec(int msec)
```

Delays execution of the current thread by `msec` milliseconds. `msec` must be greater than or equal to `0`. Otherwise, `DelayMsec()` does nothing and prints an error message.

**Note:** `DelayMsec()` is a *blocking* way to delay code execution. To delay code execution in a non-blocking way, you may use [`SceneTree.CreateTimer`](SceneTree.md). Awaiting with [`SceneTreeTimer`](SceneTreeTimer.md) delays the execution of code placed below the `await` without affecting the rest of the project (or editor, for `EditorPlugin`s and `EditorScript`s).

**Note:** When `DelayMsec()` is called on the main thread, it will freeze the project and will prevent it from redrawing and registering input until the delay has passed. When using `DelayMsec()` as part of an `EditorPlugin` or `EditorScript`, it will freeze the editor but won't freeze the project if it is currently running (since the project is an independent child process).

### DelayUsec

```csharp
public void DelayUsec(int usec)
```

Delays execution of the current thread by `usec` microseconds. `usec` must be greater than or equal to `0`. Otherwise, `DelayUsec()` does nothing and prints an error message.

**Note:** `DelayUsec()` is a *blocking* way to delay code execution. To delay code execution in a non-blocking way, you may use [`SceneTree.CreateTimer`](SceneTree.md). Awaiting with a [`SceneTreeTimer`](SceneTreeTimer.md) delays the execution of code placed below the `await` without affecting the rest of the project (or editor, for `EditorPlugin`s and `EditorScript`s).

**Note:** When `DelayUsec()` is called on the main thread, it will freeze the project and will prevent it from redrawing and registering input until the delay has passed. When using `DelayUsec()` as part of an `EditorPlugin` or `EditorScript`, it will freeze the editor but won't freeze the project if it is currently running (since the project is an independent child process).

### Execute

```csharp
public int Execute(string path, ReadOnlySpan<string> arguments, Array output, bool readStderr, bool openConsole)
```

Executes the given process in a *blocking* way. The file specified in `path` must exist and be executable. The system path resolution will be used. The `arguments` are used in the given order, separated by spaces, and wrapped in quotes.

If an `output` array is provided, the complete shell output of the process is appended to `output` as a single `System.String` element. If `readStderr` is `true`, the output to the standard error stream is also appended to the array.

On Windows, if `openConsole` is `true` and the process is a console app, a new terminal window is opened.

This method returns the exit code of the command, or `-1` if the process fails to execute.

**Note:** The main thread will be blocked until the executed command terminates. Use [`GodotThread`](GodotThread.md) to create a separate thread that will not block the main thread, or use `CreateProcess()` to create a completely independent process.

For example, to retrieve a list of the working directory's contents:

```csharp
Godot.Collections.Array output = [];
int exitCode = OS.Execute("ls", ["-l", "/tmp"], output);

```

If you wish to access a shell built-in or execute a composite command, a platform-specific shell can be invoked. For example:

```csharp
Godot.Collections.Array output = [];
OS.Execute("CMD.exe", ["/C", "cd %TEMP% && dir"], output);

```

**Note:** This method is implemented on Android, Linux, macOS, and Windows.

**Note:** To execute a Windows command interpreter built-in command, specify `cmd.exe` in `path`, `/c` as the first argument, and the desired command as the second argument.

**Note:** To execute a PowerShell built-in command, specify `powershell.exe` in `path`, `-Command` as the first argument, and the desired command as the second argument.

**Note:** To execute a Unix shell built-in command, specify shell executable name in `path`, `-c` as the first argument, and the desired command as the second argument.

**Note:** On macOS, sandboxed applications are limited to run only embedded helper executables, specified during export.

**Note:** On Android, system commands such as `dumpsys` can only be run on a rooted device.

### Execute

```csharp
public int Execute(string path, string[] arguments, Array output = null, bool readStderr = false, bool openConsole = false)
```

Executes the given process in a *blocking* way. The file specified in `path` must exist and be executable. The system path resolution will be used. The `arguments` are used in the given order, separated by spaces, and wrapped in quotes.

If an `output` array is provided, the complete shell output of the process is appended to `output` as a single `System.String` element. If `readStderr` is `true`, the output to the standard error stream is also appended to the array.

On Windows, if `openConsole` is `true` and the process is a console app, a new terminal window is opened.

This method returns the exit code of the command, or `-1` if the process fails to execute.

**Note:** The main thread will be blocked until the executed command terminates. Use [`GodotThread`](GodotThread.md) to create a separate thread that will not block the main thread, or use `CreateProcess()` to create a completely independent process.

For example, to retrieve a list of the working directory's contents:

```csharp
Godot.Collections.Array output = [];
int exitCode = OS.Execute("ls", ["-l", "/tmp"], output);

```

If you wish to access a shell built-in or execute a composite command, a platform-specific shell can be invoked. For example:

```csharp
Godot.Collections.Array output = [];
OS.Execute("CMD.exe", ["/C", "cd %TEMP% && dir"], output);

```

**Note:** This method is implemented on Android, Linux, macOS, and Windows.

**Note:** To execute a Windows command interpreter built-in command, specify `cmd.exe` in `path`, `/c` as the first argument, and the desired command as the second argument.

**Note:** To execute a PowerShell built-in command, specify `powershell.exe` in `path`, `-Command` as the first argument, and the desired command as the second argument.

**Note:** To execute a Unix shell built-in command, specify shell executable name in `path`, `-c` as the first argument, and the desired command as the second argument.

**Note:** On macOS, sandboxed applications are limited to run only embedded helper executables, specified during export.

**Note:** On Android, system commands such as `dumpsys` can only be run on a rooted device.

### ExecuteWithPipe

```csharp
public Dictionary ExecuteWithPipe(string path, ReadOnlySpan<string> arguments)
```

Creates a new process that runs independently of Godot with redirected IO. It will not terminate when Godot terminates. The path specified in `path` must exist and be an executable file or macOS `.app` bundle. The path is resolved based on the current platform. The `arguments` are used in the given order and separated by a space.

If `blocking` is `false`, created pipes work in non-blocking mode, i.e. read and write operations will return immediately. Use [`FileAccess.GetError`](FileAccess.md) to check if the last read/write operation was successful.

If the process cannot be created, this method returns an empty [`Collections.Dictionary`](Collections.Dictionary.md). Otherwise, this method returns a [`Collections.Dictionary`](Collections.Dictionary.md) with the following keys:

- `"stdio"` - [`FileAccess`](FileAccess.md) to access the process stdin and stdout pipes (read/write).

- `"stderr"` - [`FileAccess`](FileAccess.md) to access the process stderr pipe (read only).

- `"pid"` - Process ID as an `System.Int32`, which you can use to monitor the process (and potentially terminate it with `Kill()`).

**Note:** This method is implemented on Android, Linux, macOS, and Windows.

**Note:** To execute a Windows command interpreter built-in command, specify `cmd.exe` in `path`, `/c` as the first argument, and the desired command as the second argument.

**Note:** To execute a PowerShell built-in command, specify `powershell.exe` in `path`, `-Command` as the first argument, and the desired command as the second argument.

**Note:** To execute a Unix shell built-in command, specify shell executable name in `path`, `-c` as the first argument, and the desired command as the second argument.

**Note:** On macOS, sandboxed applications are limited to run only embedded helper executables, specified during export or system .app bundle, system .app bundles will ignore arguments.

### ExecuteWithPipe

```csharp
public Dictionary ExecuteWithPipe(string path, ReadOnlySpan<string> arguments, bool blocking)
```

Creates a new process that runs independently of Godot with redirected IO. It will not terminate when Godot terminates. The path specified in `path` must exist and be an executable file or macOS `.app` bundle. The path is resolved based on the current platform. The `arguments` are used in the given order and separated by a space.

If `blocking` is `false`, created pipes work in non-blocking mode, i.e. read and write operations will return immediately. Use [`FileAccess.GetError`](FileAccess.md) to check if the last read/write operation was successful.

If the process cannot be created, this method returns an empty [`Collections.Dictionary`](Collections.Dictionary.md). Otherwise, this method returns a [`Collections.Dictionary`](Collections.Dictionary.md) with the following keys:

- `"stdio"` - [`FileAccess`](FileAccess.md) to access the process stdin and stdout pipes (read/write).

- `"stderr"` - [`FileAccess`](FileAccess.md) to access the process stderr pipe (read only).

- `"pid"` - Process ID as an `System.Int32`, which you can use to monitor the process (and potentially terminate it with `Kill()`).

**Note:** This method is implemented on Android, Linux, macOS, and Windows.

**Note:** To execute a Windows command interpreter built-in command, specify `cmd.exe` in `path`, `/c` as the first argument, and the desired command as the second argument.

**Note:** To execute a PowerShell built-in command, specify `powershell.exe` in `path`, `-Command` as the first argument, and the desired command as the second argument.

**Note:** To execute a Unix shell built-in command, specify shell executable name in `path`, `-c` as the first argument, and the desired command as the second argument.

**Note:** On macOS, sandboxed applications are limited to run only embedded helper executables, specified during export or system .app bundle, system .app bundles will ignore arguments.

### ExecuteWithPipe

```csharp
public Dictionary ExecuteWithPipe(string path, string[] arguments)
```

Creates a new process that runs independently of Godot with redirected IO. It will not terminate when Godot terminates. The path specified in `path` must exist and be an executable file or macOS `.app` bundle. The path is resolved based on the current platform. The `arguments` are used in the given order and separated by a space.

If `blocking` is `false`, created pipes work in non-blocking mode, i.e. read and write operations will return immediately. Use [`FileAccess.GetError`](FileAccess.md) to check if the last read/write operation was successful.

If the process cannot be created, this method returns an empty [`Collections.Dictionary`](Collections.Dictionary.md). Otherwise, this method returns a [`Collections.Dictionary`](Collections.Dictionary.md) with the following keys:

- `"stdio"` - [`FileAccess`](FileAccess.md) to access the process stdin and stdout pipes (read/write).

- `"stderr"` - [`FileAccess`](FileAccess.md) to access the process stderr pipe (read only).

- `"pid"` - Process ID as an `System.Int32`, which you can use to monitor the process (and potentially terminate it with `Kill()`).

**Note:** This method is implemented on Android, Linux, macOS, and Windows.

**Note:** To execute a Windows command interpreter built-in command, specify `cmd.exe` in `path`, `/c` as the first argument, and the desired command as the second argument.

**Note:** To execute a PowerShell built-in command, specify `powershell.exe` in `path`, `-Command` as the first argument, and the desired command as the second argument.

**Note:** To execute a Unix shell built-in command, specify shell executable name in `path`, `-c` as the first argument, and the desired command as the second argument.

**Note:** On macOS, sandboxed applications are limited to run only embedded helper executables, specified during export or system .app bundle, system .app bundles will ignore arguments.

### ExecuteWithPipe

```csharp
public Dictionary ExecuteWithPipe(string path, string[] arguments, bool blocking = true)
```

Creates a new process that runs independently of Godot with redirected IO. It will not terminate when Godot terminates. The path specified in `path` must exist and be an executable file or macOS `.app` bundle. The path is resolved based on the current platform. The `arguments` are used in the given order and separated by a space.

If `blocking` is `false`, created pipes work in non-blocking mode, i.e. read and write operations will return immediately. Use [`FileAccess.GetError`](FileAccess.md) to check if the last read/write operation was successful.

If the process cannot be created, this method returns an empty [`Collections.Dictionary`](Collections.Dictionary.md). Otherwise, this method returns a [`Collections.Dictionary`](Collections.Dictionary.md) with the following keys:

- `"stdio"` - [`FileAccess`](FileAccess.md) to access the process stdin and stdout pipes (read/write).

- `"stderr"` - [`FileAccess`](FileAccess.md) to access the process stderr pipe (read only).

- `"pid"` - Process ID as an `System.Int32`, which you can use to monitor the process (and potentially terminate it with `Kill()`).

**Note:** This method is implemented on Android, Linux, macOS, and Windows.

**Note:** To execute a Windows command interpreter built-in command, specify `cmd.exe` in `path`, `/c` as the first argument, and the desired command as the second argument.

**Note:** To execute a PowerShell built-in command, specify `powershell.exe` in `path`, `-Command` as the first argument, and the desired command as the second argument.

**Note:** To execute a Unix shell built-in command, specify shell executable name in `path`, `-c` as the first argument, and the desired command as the second argument.

**Note:** On macOS, sandboxed applications are limited to run only embedded helper executables, specified during export or system .app bundle, system .app bundles will ignore arguments.

### FindKeycodeFromString

```csharp
public Key FindKeycodeFromString(string string)
```

Finds the keycode for the given string. The returned values are equivalent to the [`Key`](Key.md) constants.

```csharp
GD.Print(OS.FindKeycodeFromString("C"));         // Prints C (Key.C)
GD.Print(OS.FindKeycodeFromString("Escape"));    // Prints Escape (Key.Escape)
GD.Print(OS.FindKeycodeFromString("Shift+Tab")); // Prints 37748738 (KeyModifierMask.MaskShift | Key.Tab)
GD.Print(OS.FindKeycodeFromString("Unknown"));   // Prints None (Key.None)

```

See also `GetKeycodeString()`.

### GetCacheDir

```csharp
public string GetCacheDir()
```

Returns the *global* cache data directory according to the operating system's standards.

On the Linux/BSD platform, this path can be overridden by setting the `XDG_CACHE_HOME` environment variable before starting the project. See [File paths in Godot projects]($DOCS_URL/tutorials/io/data_paths.html) in the documentation for more information. See also `GetConfigDir()` and `GetDataDir()`.

Not to be confused with `GetUserDataDir()`, which returns the *project-specific* user data path.

### GetCmdlineArgs

```csharp
public string[] GetCmdlineArgs()
```

Returns the command-line arguments passed to the engine, excluding arguments processed by the engine, such as `--headless` and `--fullscreen`.

```csharp
# Godot has been executed with the following command:
# godot --headless --verbose --scene my_scene.tscn --custom
OS.get_cmdline_args() # Returns ["--scene", "my_scene.tscn", "--custom"]

```

Command-line arguments can be written in any form, including both `--key value` and `--key=value` forms so they can be properly parsed, as long as custom command-line arguments do not conflict with engine arguments.

You can also incorporate environment variables using the `GetEnvironment()` method.

You can set `ProjectSettings.editor/run/main_run_args` to define command-line arguments to be passed by the editor when running the project.

**Example:** Parse command-line arguments into a [`Collections.Dictionary`](Collections.Dictionary.md) using the `--key=value` form for arguments:

```csharp
var arguments = new Dictionary<string, string>();
foreach (var argument in OS.GetCmdlineArgs())
{
if (argument.Contains('='))
{
string[] keyValue = argument.Split("=");
arguments[keyValue[0].TrimPrefix("--")] = keyValue[1];
}
else
{
// Options without an argument will be present in the dictionary,
// with the value set to an empty string.
arguments[argument.TrimPrefix("--")] = "";
}
}

```

**Note:** Passing custom user arguments directly is not recommended, as the engine may discard or modify them. Instead, pass the standard UNIX double dash (`--`) and then the custom arguments, which the engine will ignore by design. These can be read via `GetCmdlineUserArgs()`.

### GetCmdlineUserArgs

```csharp
public string[] GetCmdlineUserArgs()
```

Returns the command-line user arguments passed to the engine. User arguments are ignored by the engine and reserved for the user. They are passed after the double dash `--` argument. `++` may be used when `--` is intercepted by another program (such as `startx`).

```csharp
# Godot has been executed with the following command:
# godot --fullscreen --custom -- --level=2 --hardcore

OS.get_cmdline_args()      # Returns ["--custom"]
OS.get_cmdline_user_args() # Returns ["--level=2", "--hardcore"]

```

To get arguments passed before `--` or `++`, use `GetCmdlineArgs()`.

### GetConfigDir

```csharp
public string GetConfigDir()
```

Returns the *global* user configuration directory according to the operating system's standards.

On the Linux/BSD platform, this path can be overridden by setting the `XDG_CONFIG_HOME` environment variable before starting the project. See [File paths in Godot projects]($DOCS_URL/tutorials/io/data_paths.html) in the documentation for more information. See also `GetCacheDir()` and `GetDataDir()`.

Not to be confused with `GetUserDataDir()`, which returns the *project-specific* user data path.

### GetConnectedMidiInputs

```csharp
public string[] GetConnectedMidiInputs()
```

Returns an array of connected MIDI device names, if they exist. Returns an empty array if the system MIDI driver has not previously been initialized with `OpenMidiInputs()`. See also `CloseMidiInputs()`.

**Note:** This method is implemented on Linux, macOS, Windows, and Web.

**Note:** On the Web platform, Web MIDI needs to be supported by the browser. [For the time being](https://caniuse.com/midi), it is currently supported by all major browsers, except Safari.

**Note:** On the Web platform, using MIDI input requires a browser permission to be granted first. This permission request is performed when calling `OpenMidiInputs()`. The browser will refrain from processing MIDI input until the user accepts the permission request.

### GetDataDir

```csharp
public string GetDataDir()
```

Returns the *global* user data directory according to the operating system's standards.

On the Linux/BSD platform, this path can be overridden by setting the `XDG_DATA_HOME` environment variable before starting the project. See [File paths in Godot projects]($DOCS_URL/tutorials/io/data_paths.html) in the documentation for more information. See also `GetCacheDir()` and `GetConfigDir()`.

Not to be confused with `GetUserDataDir()`, which returns the *project-specific* user data path.

### GetDistributionName

```csharp
public string GetDistributionName()
```

Returns the name of the distribution for Linux and BSD platforms (e.g. "Ubuntu", "Manjaro", "OpenBSD", etc.).

Returns the same value as `GetName()` for stock Android ROMs, but attempts to return the custom ROM name for popular Android derivatives such as "LineageOS".

Returns the same value as `GetName()` for other platforms.

**Note:** This method is not supported on the Web platform. It returns an empty string.

### GetEntropy

```csharp
public byte[] GetEntropy(int size)
```

Generates a `System.Byte`[] of cryptographically secure random bytes with given `size`.

**Note:** Generating large quantities of bytes using this method can result in locking and entropy of lower quality on most platforms. Using [`Crypto.GenerateRandomBytes`](Crypto.md) is preferred in most cases.

### GetEnvironment

```csharp
public string GetEnvironment(string variable)
```

Returns the value of the given environment variable, or an empty string if `variable` doesn't exist.

**Note:** Double-check the casing of `variable`. Environment variable names are case-sensitive on all platforms except Windows.

**Note:** On macOS, applications do not have access to shell environment variables.

### GetExecutablePath

```csharp
public string GetExecutablePath()
```

Returns the file path to the current engine executable.

**Note:** On macOS, if you want to launch another instance of Godot, always use `CreateInstance()` instead of relying on the executable path.

### GetGrantedPermissions

```csharp
public string[] GetGrantedPermissions()
```

On Android devices: Returns the list of dangerous permissions that have been granted.

On macOS: Returns the list of granted permissions and user selected folders accessible to the application (sandboxed applications only). Use the native file dialog to request folder access permission.

On iOS, visionOS: Returns the list of granted permissions.

### GetKeycodeString

```csharp
public string GetKeycodeString(Key code)
```

Returns the given keycode as a `System.String`.

```csharp
GD.Print(OS.GetKeycodeString(Key.C));                                    // Prints "C"
GD.Print(OS.GetKeycodeString(Key.Escape));                               // Prints "Escape"
GD.Print(OS.GetKeycodeString((Key)KeyModifierMask.MaskShift | Key.Tab)); // Prints "Shift+Tab"

```

See also `FindKeycodeFromString()`, [`InputEventKey.Keycode`](InputEventKey.md), and [`InputEventKey.GetKeycodeWithModifiers`](InputEventKey.md).

### GetLocale

```csharp
public string GetLocale()
```

Returns the host OS locale as a `System.String` of the form `language_Script_COUNTRY_VARIANT@extra`. Every substring after `language` is optional and may not exist.

- `language` - 2 or 3-letter [language code](https://en.wikipedia.org/wiki/List_of_ISO_639-1_codes), in lower case.

- `Script` - 4-letter [script code](https://en.wikipedia.org/wiki/ISO_15924), in title case.

- `COUNTRY` - 2 or 3-letter [country code](https://en.wikipedia.org/wiki/ISO_3166-1), in upper case.

- `VARIANT` - language variant, region and sort order. The variant can have any number of underscored keywords.

- `extra` - semicolon separated list of additional key words. This may include currency, calendar, sort order and numbering system information.

If you want only the language code and not the fully specified locale from the OS, you can use `GetLocaleLanguage()`.

### GetLocaleLanguage

```csharp
public string GetLocaleLanguage()
```

Returns the host OS locale's 2 or 3-letter [language code](https://en.wikipedia.org/wiki/List_of_ISO_639-1_codes) as a string which should be consistent on all platforms. This is equivalent to extracting the `language` part of the `GetLocale()` string.

This can be used to narrow down fully specified locale strings to only the "common" language code, when you don't need the additional information about country code or variants. For example, for a French Canadian user with `fr_CA` locale, this would return `fr`.

### GetLowProcessorUsageModeSleepUsec

```csharp
public int GetLowProcessorUsageModeSleepUsec()
```

### GetMainThreadId

```csharp
public ulong GetMainThreadId()
```

Returns the ID of the main thread. See `GetThreadCallerId()`.

**Note:** Thread IDs are not deterministic and may be reused across application restarts.

### GetMemoryInfo

```csharp
public Dictionary GetMemoryInfo()
```

Returns a [`Collections.Dictionary`](Collections.Dictionary.md) containing information about the current memory with the following entries:

- `"physical"` - total amount of usable physical memory in bytes. This value can be slightly less than the actual physical memory amount, since it does not include memory reserved by the kernel and devices.

- `"free"` - amount of physical memory, that can be immediately allocated without disk access or other costly operations, in bytes. The process might be able to allocate more physical memory, but this action will require moving inactive pages to disk, which can be expensive.

- `"available"` - amount of memory that can be allocated without extending the swap file(s), in bytes. This value includes both physical memory and swap.

- `"stack"` - size of the current thread stack in bytes.

**Note:** Each entry's value may be `-1` if it is unknown.

### GetModelName

```csharp
public string GetModelName()
```

Returns the model name of the current device.

**Note:** This method is implemented on Android, iOS, macOS, and Windows. Returns `"GenericDevice"` on unsupported platforms.

### GetName

```csharp
public string GetName()
```

Returns the name of the host platform.

- On Windows, this is `"Windows"`.

- On macOS, this is `"macOS"`.

- On Linux-based operating systems, this is `"Linux"`.

- On BSD-based operating systems, this is `"FreeBSD"`, `"NetBSD"`, `"OpenBSD"`, or `"BSD"` as a fallback.

- On Android, this is `"Android"`.

- On iOS, this is `"iOS"`.

- On Web, this is `"Web"`.

**Note:** Custom builds of the engine may support additional platforms, such as consoles, possibly returning other names.

```csharp
switch (OS.GetName())
{
case "Windows":
GD.Print("Welcome to Windows");
break;
case "macOS":
GD.Print("Welcome to macOS!");
break;
case "Linux":
case "FreeBSD":
case "NetBSD":
case "OpenBSD":
case "BSD":
GD.Print("Welcome to Linux/BSD!");
break;
case "Android":
GD.Print("Welcome to Android!");
break;
case "iOS":
GD.Print("Welcome to iOS!");
break;
case "Web":
GD.Print("Welcome to the Web!");
break;
}

```

**Note:** On Web platforms, it is still possible to determine the host platform's OS with feature tags. See `HasFeature()`.

### GetProcessExitCode

```csharp
public int GetProcessExitCode(int pid)
```

Returns the exit code of a spawned process once it has finished running (see `IsProcessRunning()`).

Returns `-1` if the `pid` is not a PID of a spawned child process, the process is still running, or the method is not implemented for the current platform.

**Note:** Returns `-1` if the `pid` is a macOS bundled app process.

**Note:** This method is implemented on Android, Linux, macOS and Windows.

### GetProcessId

```csharp
public int GetProcessId()
```

Returns the number used by the host machine to uniquely identify this application.

**Note:** On Web, this method always returns `0`.

### GetProcessorCount

```csharp
public int GetProcessorCount()
```

Returns the number of *logical* CPU cores available on the host machine. On CPUs with HyperThreading enabled, this number will be greater than the number of *physical* CPU cores.

### GetProcessorName

```csharp
public string GetProcessorName()
```

Returns the full name of the CPU model on the host machine (e.g. `"Intel(R) Core(TM) i7-6700K CPU @ 4.00GHz"`).

**Note:** This method is only implemented on Windows, macOS, Linux and iOS. On Android and Web, `GetProcessorName()` returns an empty string.

### GetRestartOnExitArguments

```csharp
public string[] GetRestartOnExitArguments()
```

Returns the list of command line arguments that will be used when the project automatically restarts using `SetRestartOnExit()`. See also `IsRestartOnExitSet()`.

### GetStaticMemoryPeakUsage

```csharp
public ulong GetStaticMemoryPeakUsage()
```

Returns the maximum amount of static memory used. Only works in debug builds.

### GetStaticMemoryUsage

```csharp
public ulong GetStaticMemoryUsage()
```

Returns the amount of static memory being used by the program in bytes. Only works in debug builds.

### GetStdInType

```csharp
public OS.StdHandleType GetStdInType()
```

Returns the type of the standard input device.

**Note:** This method is implemented on Linux, macOS, and Windows.

**Note:** On exported Windows builds, run the console wrapper executable to access the standard input. If you need a single executable with full console support, use a custom build compiled with the `windows_subsystem=console` flag.

### GetStdOutType

```csharp
public OS.StdHandleType GetStdOutType()
```

Returns the type of the standard output device.

**Note:** This method is implemented on Linux, macOS, and Windows.

### GetStderrType

```csharp
public OS.StdHandleType GetStderrType()
```

Returns the type of the standard error device.

**Note:** This method is implemented on Linux, macOS, and Windows.

### GetSystemCaCertificates

```csharp
public string GetSystemCaCertificates()
```

Returns the list of certification authorities trusted by the operating system as a string of concatenated certificates in PEM format.

### GetSystemDir

```csharp
public string GetSystemDir(OS.SystemDir dir, bool sharedStorage = true)
```

Returns the path to commonly used folders across different platforms, as defined by `dir`. See the [`OS.SystemDir`](OS.md) constants for available locations.

**Note:** This method is implemented on Android, Linux, macOS and Windows.

**Note:** Shared storage is implemented on Android and allows to differentiate between app specific and shared directories, if `sharedStorage` is `true`. Shared directories have additional restrictions on Android.

### GetSystemFontPath

```csharp
public string GetSystemFontPath(string fontName, int weight = 400, int stretch = 100, bool italic = false)
```

Returns the path to the system font file with `fontName` and style. Returns an empty string if no matching fonts found.

The following aliases can be used to request default fonts: "sans-serif", "serif", "monospace", "cursive", and "fantasy".

**Note:** Returned font might have different style if the requested style is not available.

**Note:** This method is implemented on Android, iOS, Linux, macOS and Windows.

### GetSystemFontPathForText

```csharp
public string[] GetSystemFontPathForText(string fontName, string text, string locale = "", string script = "", int weight = 400, int stretch = 100, bool italic = false)
```

Returns an array of the system substitute font file paths, which are similar to the font with `fontName` and style for the specified text, locale, and script. Returns an empty array if no matching fonts found.

The following aliases can be used to request default fonts: "sans-serif", "serif", "monospace", "cursive", and "fantasy".

**Note:** Depending on OS, it's not guaranteed that any of the returned fonts will be suitable for rendering specified text. Fonts should be loaded and checked in the order they are returned, and the first suitable one used.

**Note:** Returned fonts might have different style if the requested style is not available or belong to a different font family.

**Note:** This method is implemented on Android, iOS, Linux, macOS and Windows.

### GetSystemFonts

```csharp
public string[] GetSystemFonts()
```

Returns the list of font family names available.

**Note:** This method is implemented on Android, iOS, Linux, macOS and Windows.

### GetTempDir

```csharp
public string GetTempDir()
```

Returns the *global* temporary data directory according to the operating system's standards.

### GetThreadCallerId

```csharp
public ulong GetThreadCallerId()
```

Returns the ID of the current thread. This can be used in logs to ease debugging of multi-threaded applications.

**Note:** Thread IDs are not deterministic and may be reused across application restarts.

### GetUniqueId

```csharp
public string GetUniqueId()
```

Returns a string that is unique to the device.

**Note:** This string may change without notice if the user reinstalls their operating system, upgrades it, or modifies their hardware. This means it should generally not be used to encrypt persistent data, as the data saved before an unexpected ID change would become inaccessible. The returned string may also be falsified using external programs, so do not rely on the string returned by this method for security purposes.

**Note:** On Web, returns an empty string and generates an error, as this method cannot be implemented for security reasons.

### GetUserDataDir

```csharp
public string GetUserDataDir()
```

Returns the absolute directory path where user data is written (the `user://` directory in Godot). The path depends on the project name and `ProjectSettings.application/config/use_custom_user_dir`.

- On Windows, this is `%AppData%\Godot\app_userdata\[project_name]`, or `%AppData%\[custom_name]` if `use_custom_user_dir` is set. `%AppData%` expands to `%UserProfile%\AppData\Roaming`.

- On macOS, this is `~/Library/Application Support/Godot/app_userdata/[project_name]`, or `~/Library/Application Support/[custom_name]` if `use_custom_user_dir` is set.

- On Linux and BSD, this is `~/.local/share/godot/app_userdata/[project_name]`, or `~/.local/share/[custom_name]` if `use_custom_user_dir` is set.

- On Android and iOS, this is a sandboxed directory in either internal or external storage, depending on the user's configuration.

- On Web, this is a virtual directory managed by the browser.

If the project name is empty, `[project_name]` falls back to `[unnamed project]`.

Not to be confused with `GetDataDir()`, which returns the *global* (non-project-specific) user home directory.

### GetVersion

```csharp
public string GetVersion()
```

Returns the exact production and build version of the operating system. This is different from the branded version used in marketing. This helps to distinguish between different releases of operating systems, including minor versions, and insider and custom builds.

- For Windows, the major and minor version are returned, as well as the build number. For example, the returned string may look like `10.0.9926` for a build of Windows 10.

- For rolling distributions, such as Arch Linux, an empty string is returned.

- For macOS and iOS, the major and minor version are returned, as well as the patch number.

- For Android, the SDK version and the incremental build number are returned. If it's a custom ROM, it attempts to return its version instead.

**Note:** This method is not supported on the Web platform. It returns an empty string.

### GetVersionAlias

```csharp
public string GetVersionAlias()
```

Returns the branded version used in marketing, followed by the build number (on Windows), the version number (on macOS), or the SDK version and incremental build number (on Android). Examples include `11 (build 22000)`, `Sequoia (15.0.0)`, and `15 (SDK 35 build abc528-11988f)`.

This value can then be appended to `GetName()` to get a full, human-readable operating system name and version combination for the operating system. Windows feature updates such as 24H2 are not contained in the resulting string, but Windows Server is recognized as such (e.g. `2025 (build 26100)` for Windows Server 2025).

**Note:** This method is only supported on Windows, macOS, and Android. On other operating systems, it returns the same value as `GetVersion()`.

### GetVideoAdapterDriverInfo

```csharp
public string[] GetVideoAdapterDriverInfo()
```

Returns the video adapter driver name and version for the user's currently active graphics card, as a `System.String`[]. See also [`RenderingServer.GetVideoAdapterApiVersion`](RenderingServer.md).

The first element holds the driver name, such as `nvidia`, `amdgpu`, etc.

The second element holds the driver version. For example, on the `nvidia` driver on a Linux/BSD platform, the version is in the format `510.85.02`. For Windows, the driver's format is `31.0.15.1659`.

**Note:** This method is only supported on Linux/BSD and Windows when not running in headless mode. On other platforms, it returns an empty array.

**Note:** This method will run slowly the first time it is called in a session; it can take several seconds depending on the operating system and hardware. It is blocking if called on the main thread, so it's recommended to call it on a separate thread using [`GodotThread`](GodotThread.md). This allows the engine to keep running while the information is being retrieved. However, `GetVideoAdapterDriverInfo()` is *not* thread-safe, so it should not be called from multiple threads at the same time.

### HasEnvironment

```csharp
public bool HasEnvironment(string variable)
```

Returns `true` if the environment variable with the name `variable` exists.

**Note:** Double-check the casing of `variable`. Environment variable names are case-sensitive on all platforms except Windows.

### HasFeature

```csharp
public bool HasFeature(string tagName)
```

Returns `true` if the feature for the given feature tag is supported in the currently running instance, depending on the platform, build, etc. Can be used to check whether you're currently running a debug build, on a certain platform or arch, etc. Refer to the [Feature Tags]($DOCS_URL/tutorials/export/feature_tags.html) documentation for more details.

**Note:** Tag names are case-sensitive.

**Note:** On the Web platform, one of the following additional tags is defined to indicate the host platform: `web_android`, `web_ios`, `web_linuxbsd`, `web_macos`, or `web_windows`.

### IsDebugBuild

```csharp
public bool IsDebugBuild()
```

Returns `true` if the Godot binary used to run the project is a *debug* export template, or when running in the editor.

Returns `false` if the Godot binary used to run the project is a *release* export template.

**Note:** To check whether the Godot binary used to run the project is an export template (debug or release), use `OS.has_feature("template")` instead.

### IsDeltaSmoothingEnabled

```csharp
public bool IsDeltaSmoothingEnabled()
```

### IsInLowProcessorUsageMode

```csharp
public bool IsInLowProcessorUsageMode()
```

### IsKeycodeUnicode

```csharp
public bool IsKeycodeUnicode(long code)
```

Returns `true` if the input keycode corresponds to a Unicode character. For a list of codes, see the [`Key`](Key.md) constants.

```csharp
GD.Print(OS.IsKeycodeUnicode((long)Key.G));      // Prints True
GD.Print(OS.IsKeycodeUnicode((long)Key.Kp4));    // Prints True
GD.Print(OS.IsKeycodeUnicode((long)Key.Tab));    // Prints False
GD.Print(OS.IsKeycodeUnicode((long)Key.Escape)); // Prints False

```

### IsProcessRunning

```csharp
public bool IsProcessRunning(int pid)
```

Returns `true` if the child process ID (`pid`) is still running or `false` if it has terminated. `pid` must be a valid ID generated from `CreateProcess()`.

**Note:** This method is implemented on Android, iOS, Linux, macOS, and Windows.

### IsRestartOnExitSet

```csharp
public bool IsRestartOnExitSet()
```

Returns `true` if the project will automatically restart when it exits for any reason, `false` otherwise. See also `SetRestartOnExit()` and `GetRestartOnExitArguments()`.

### IsSandboxed

```csharp
public bool IsSandboxed()
```

Returns `true` if the application is running in the sandbox.

**Note:** This method is only implemented on macOS and Linux.

### IsStdOutVerbose

```csharp
public bool IsStdOutVerbose()
```

Returns `true` if the engine was executed with the `--verbose` or `-v` command line argument, or if `ProjectSettings.debug/settings/stdout/verbose_stdout` is `true`. See also `@GlobalScope.print_verbose`.

### IsUserfsPersistent

```csharp
public bool IsUserfsPersistent()
```

Returns `true` if the `user://` file system is persistent, that is, its state is the same after a player quits and starts the game again. Relevant to the Web platform, where this persistence may be unavailable.

### Kill

```csharp
public Error Kill(int pid)
```

Kill (terminate) the process identified by the given process ID (`pid`), such as the ID returned by `Execute()` in non-blocking mode. See also `Crash()`.

**Note:** This method can also be used to kill processes that were not spawned by the engine.

**Note:** This method is implemented on Android, iOS, Linux, macOS and Windows.

### MoveToTrash

```csharp
public Error MoveToTrash(string path)
```

Moves the file or directory at the given `path` to the system's recycle bin. See also [`DirAccess.Remove`](DirAccess.md).

The method takes only global paths, so you may need to use [`ProjectSettings.GlobalizePath`](ProjectSettings.md). Do not use it for files in `res://` as it will not work in exported projects.

Returns [`Error.Failed`](Error.md) if the file or directory cannot be found, or the system does not support this method.

```csharp
var fileToRemove = "user://slot1.save";
OS.MoveToTrash(ProjectSettings.GlobalizePath(fileToRemove));

```

**Note:** This method is implemented on Android, Linux, macOS and Windows.

**Note:** If the user has disabled the recycle bin on their system, the file will be permanently deleted instead.

### OpenMidiInputs

```csharp
public void OpenMidiInputs()
```

Initializes the singleton for the system MIDI driver, allowing Godot to receive [`InputEventMidi`](InputEventMidi.md). See also `GetConnectedMidiInputs()` and `CloseMidiInputs()`.

**Note:** This method is implemented on Linux, macOS, Windows, and Web.

**Note:** On the Web platform, Web MIDI needs to be supported by the browser. [For the time being](https://caniuse.com/midi), it is currently supported by all major browsers, except Safari.

**Note:** On the Web platform, using MIDI input requires a browser permission to be granted first. This permission request is performed when calling `OpenMidiInputs()`. The browser will refrain from processing MIDI input until the user accepts the permission request.

### OpenWithProgram

```csharp
public Error OpenWithProgram(string programPath, ReadOnlySpan<string> paths)
```

Opens one or more files/directories with the specified application. The `programPath` specifies the path to the application to use for opening the files, and `paths` contains an array of file/directory paths to open.

**Note:** This method is mostly only relevant for macOS, where opening files using `CreateProcess()` might fail. On other platforms, this falls back to using `CreateProcess()`.

**Note:** On macOS, `programPath` should ideally be the path to a `.app` bundle.

### OpenWithProgram

```csharp
public Error OpenWithProgram(string programPath, string[] paths)
```

Opens one or more files/directories with the specified application. The `programPath` specifies the path to the application to use for opening the files, and `paths` contains an array of file/directory paths to open.

**Note:** This method is mostly only relevant for macOS, where opening files using `CreateProcess()` might fail. On other platforms, this falls back to using `CreateProcess()`.

**Note:** On macOS, `programPath` should ideally be the path to a `.app` bundle.

### ReadBufferFromStdIn

```csharp
public byte[] ReadBufferFromStdIn(long bufferSize = 1024)
```

Reads a user input as raw data from the standard input. This operation can be *blocking*, which causes the window to freeze if `ReadBufferFromStdIn()` is called on the main thread.

- If standard input is console, this method will block until the program receives a line break in standard input (usually by the user pressing Enter).

- If standard input is pipe, this method will block until a specific amount of data is read or pipe is closed.

- If standard input is a file, this method will read a specific amount of data (or less if end-of-file is reached) and return immediately.

**Note:** This method is implemented on Linux, macOS, and Windows.

**Note:** On exported Windows builds, run the console wrapper executable to access the terminal. If standard input is console, calling this method without console wrapped will freeze permanently. If standard input is pipe or file, it can be used without console wrapper. If you need a single executable with full console support, use a custom build compiled with the `windows_subsystem=console` flag.

### ReadStringFromStdIn

```csharp
public string ReadStringFromStdIn()
```

Reads a user input as a UTF-8 encoded string from the standard input. This operation can be *blocking*, which causes the window to freeze if `ReadStringFromStdIn()` is called on the main thread.

- If standard input is console, this method will block until the program receives a line break in standard input (usually by the user pressing Enter).

- If standard input is pipe, this method will block until a specific amount of data is read or pipe is closed.

- If standard input is a file, this method will read a specific amount of data (or less if end-of-file is reached) and return immediately.

**Note:** This method automatically replaces `\r\n` line breaks with `\n` and removes them from the end of the string. Use `ReadBufferFromStdIn()` to read the unprocessed data.

**Note:** This method is implemented on Linux, macOS, and Windows.

**Note:** On exported Windows builds, run the console wrapper executable to access the terminal. If standard input is console, calling this method without console wrapped will freeze permanently. If standard input is pipe or file, it can be used without console wrapper. If you need a single executable with full console support, use a custom build compiled with the `windows_subsystem=console` flag.

### ReadStringFromStdIn

```csharp
public string ReadStringFromStdIn(long bufferSize = 1024)
```

Reads a user input as a UTF-8 encoded string from the standard input. This operation can be *blocking*, which causes the window to freeze if `ReadStringFromStdIn()` is called on the main thread.

- If standard input is console, this method will block until the program receives a line break in standard input (usually by the user pressing Enter).

- If standard input is pipe, this method will block until a specific amount of data is read or pipe is closed.

- If standard input is a file, this method will read a specific amount of data (or less if end-of-file is reached) and return immediately.

**Note:** This method automatically replaces `\r\n` line breaks with `\n` and removes them from the end of the string. Use `ReadBufferFromStdIn()` to read the unprocessed data.

**Note:** This method is implemented on Linux, macOS, and Windows.

**Note:** On exported Windows builds, run the console wrapper executable to access the terminal. If standard input is console, calling this method without console wrapped will freeze permanently. If standard input is pipe or file, it can be used without console wrapper. If you need a single executable with full console support, use a custom build compiled with the `windows_subsystem=console` flag.

### RemoveLogger

```csharp
public void RemoveLogger(Logger logger)
```

Remove a custom logger added by `AddLogger()`.

### RequestPermission

```csharp
public bool RequestPermission(string name)
```

Requests permission from the OS for the given `name`. Returns `true` if the permission has already been granted. See also [`MainLoop.OnRequestPermissionsResult`](MainLoop.md).

The `name` must be the full permission name. For example:

- `OS.request_permission("android.permission.READ_EXTERNAL_STORAGE")`

- `OS.request_permission("android.permission.POST_NOTIFICATIONS")`

- `OS.request_permission("macos.permission.RECORD_SCREEN")`

- `OS.request_permission("appleembedded.permission.AUDIO_RECORD")`

**Note:** On Android, permission must be checked during export.

**Note:** This method is implemented on Android, macOS, and visionOS platforms.

### RequestPermissions

```csharp
public bool RequestPermissions()
```

Requests *dangerous* permissions from the OS. Returns `true` if permissions have already been granted. See also [`MainLoop.OnRequestPermissionsResult`](MainLoop.md).

**Note:** Permissions must be checked during export.

**Note:** This method is only implemented on Android. Normal permissions are automatically granted at install time in Android applications.

### RevokeGrantedPermissions

```csharp
public void RevokeGrantedPermissions()
```

On macOS (sandboxed applications only), this function clears list of user selected folders accessible to the application.

### SetDeltaSmoothing

```csharp
public void SetDeltaSmoothing(bool deltaSmoothingEnabled)
```

### SetEnvironment

```csharp
public void SetEnvironment(string variable, string value)
```

Sets the value of the environment variable `variable` to `value`. The environment variable will be set for the Godot process and any process executed with `Execute()` after running `SetEnvironment()`. The environment variable will *not* persist to processes run after the Godot process was terminated.

**Note:** Environment variable names are case-sensitive on all platforms except Windows. The `variable` name cannot be empty or include the `=` character. On Windows, there is a 32767 characters limit for the combined length of `variable`, `value`, and the `=` and null terminator characters that will be registered in the environment block.

### SetLowProcessorUsageMode

```csharp
public void SetLowProcessorUsageMode(bool enable)
```

### SetLowProcessorUsageModeSleepUsec

```csharp
public void SetLowProcessorUsageModeSleepUsec(int usec)
```

### SetRestartOnExit

```csharp
public void SetRestartOnExit(bool restart, ReadOnlySpan<string> arguments)
```

If `restart` is `true`, restarts the project automatically when it is exited with [`SceneTree.Quit`](SceneTree.md) or [`Node.NotificationWMCloseRequest`](Node.md). Command-line `arguments` can be supplied. To restart the project with the same command line arguments as originally used to run the project, pass `GetCmdlineArgs()` as the value for `arguments`.

This method can be used to apply setting changes that require a restart. See also `IsRestartOnExitSet()` and `GetRestartOnExitArguments()`.

**Note:** This method is only effective on desktop platforms, and only when the project isn't started from the editor. It will have no effect on mobile and Web platforms, or when the project is started from the editor.

**Note:** If the project process crashes or is *killed* by the user (by sending `SIGKILL` instead of the usual `SIGTERM`), the project won't restart automatically.

### SetRestartOnExit

```csharp
public void SetRestartOnExit(bool restart, string[] arguments = null)
```

If `restart` is `true`, restarts the project automatically when it is exited with [`SceneTree.Quit`](SceneTree.md) or [`Node.NotificationWMCloseRequest`](Node.md). Command-line `arguments` can be supplied. To restart the project with the same command line arguments as originally used to run the project, pass `GetCmdlineArgs()` as the value for `arguments`.

This method can be used to apply setting changes that require a restart. See also `IsRestartOnExitSet()` and `GetRestartOnExitArguments()`.

**Note:** This method is only effective on desktop platforms, and only when the project isn't started from the editor. It will have no effect on mobile and Web platforms, or when the project is started from the editor.

**Note:** If the project process crashes or is *killed* by the user (by sending `SIGKILL` instead of the usual `SIGTERM`), the project won't restart automatically.

参数：
- `arguments`：If the parameter is null, then the default value is `Array.Empty<string>()`.

### SetThreadName

```csharp
public Error SetThreadName(string name)
```

Assigns the given name to the current thread. Returns [`Error.Unavailable`](Error.md) if unavailable on the current platform.

### SetUseFileAccessSaveAndSwap

```csharp
public void SetUseFileAccessSaveAndSwap(bool enabled)
```

If `enabled` is `true`, when opening a file for writing, a temporary file is used in its place. When closed, it is automatically applied to the target file.

This can useful when files may be opened by other applications, such as antiviruses, text editors, or even the Godot editor itself.

### ShellOpen

```csharp
public Error ShellOpen(string uri)
```

Requests the OS to open a resource identified by `uri` with the most appropriate program. For example:

- `OS.shell_open("C:\\Users\\name\\Downloads")` on Windows opens the file explorer at the user's Downloads folder.

- `OS.shell_open("C:/Users/name/Downloads")` also works on Windows and opens the file explorer at the user's Downloads folder.

- `OS.shell_open("https://godotengine.org")` opens the default web browser on the official Godot website.

- `OS.shell_open("mailto:example@example.com")` opens the default email client with the "To" field set to `example@example.com`. See [RFC 2368 - The mailto URL scheme](https://datatracker.ietf.org/doc/html/rfc2368) for a list of fields that can be added.

Use [`ProjectSettings.GlobalizePath`](ProjectSettings.md) to convert a `res://` or `user://` project path into a system path for use with this method.

**Note:** Use `String.uri_encode` to encode characters within URLs in a URL-safe, portable way. This is especially required for line breaks. Otherwise, `ShellOpen()` may not work correctly in a project exported to the Web platform.

**Note:** This method is implemented on Android, iOS, Web, Linux, macOS and Windows.

### ShellShowInFileManager

```csharp
public Error ShellShowInFileManager(string fileOrDirPath, bool openFolder = true)
```

Requests the OS to open the file manager, navigate to the given `fileOrDirPath` and select the target file or folder.

If `openFolder` is `true` and `fileOrDirPath` is a valid directory path, the OS will open the file manager and navigate to the target folder without selecting anything.

Use [`ProjectSettings.GlobalizePath`](ProjectSettings.md) to convert a `res://` or `user://` project path into a system path to use with this method.

**Note:** This method is currently only implemented on Windows and macOS. On other platforms, it will fallback to `ShellOpen()` with a directory path of `fileOrDirPath` prefixed with `file://`.

### UnsetEnvironment

```csharp
public void UnsetEnvironment(string variable)
```

Removes the given environment variable from the current environment, if it exists. The `variable` name cannot be empty or include the `=` character. The environment variable will be removed for the Godot process and any process executed with `Execute()` after running `UnsetEnvironment()`. The removal of the environment variable will *not* persist to processes run after the Godot process was terminated.

**Note:** Environment variable names are case-sensitive on all platforms except Windows.
