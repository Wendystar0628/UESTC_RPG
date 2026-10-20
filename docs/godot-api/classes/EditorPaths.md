<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorPaths

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

This editor-only singleton returns OS-specific paths to various data folders and files. It can be used in editor plugins to ensure files are saved in the correct location on each operating system.

**Note:** This singleton is not accessible in exported projects. Attempting to access it in an exported project will result in a script error as the singleton won't be declared. To prevent script errors in exported projects, use [`Engine.HasSingleton`](Engine.md) to check whether the singleton is available before using it.

**Note:** On the Linux/BSD platform, Godot complies with the [XDG Base Directory Specification](https://specifications.freedesktop.org/basedir-spec/basedir-spec-latest.html). You can override environment variables following the specification to change the editor and project data paths.

## 构造函数

### EditorPaths

```csharp
public EditorPaths()
```

## 方法

### GetCacheDir

```csharp
public string GetCacheDir()
```

引擎名：`get_cache_dir`

Returns the absolute path to the user's cache folder. This folder should be used for temporary data that can be removed safely whenever the editor is closed (such as generated resource thumbnails).

**Default paths per platform:**

```csharp
- Windows: %LOCALAPPDATA%\Godot\
- macOS: ~/Library/Caches/Godot/
- Linux: ~/.cache/godot/

```

### GetConfigDir

```csharp
public string GetConfigDir()
```

引擎名：`get_config_dir`

Returns the absolute path to the user's configuration folder. This folder should be used for *persistent* user configuration files.

**Default paths per platform:**

```csharp
- Windows: %APPDATA%\Godot\                    (same as `get_data_dir()`)
- macOS: ~/Library/Application Support/Godot/  (same as `get_data_dir()`)
- Linux: ~/.config/godot/

```

### GetDataDir

```csharp
public string GetDataDir()
```

引擎名：`get_data_dir`

Returns the absolute path to the user's data folder. This folder should be used for *persistent* user data files such as installed export templates.

**Default paths per platform:**

```csharp
- Windows: %APPDATA%\Godot\                    (same as `get_config_dir()`)
- macOS: ~/Library/Application Support/Godot/  (same as `get_config_dir()`)
- Linux: ~/.local/share/godot/

```

### GetProjectSettingsDir

```csharp
public string GetProjectSettingsDir()
```

引擎名：`get_project_settings_dir`

Returns the relative path to the editor settings for this project. This is usually `"res://.godot/editor"`. Projects all have a unique subdirectory inside the settings path where project-specific editor settings are saved.

### GetSelfContainedFile

```csharp
public string GetSelfContainedFile()
```

引擎名：`get_self_contained_file`

Returns the absolute path to the self-contained file that makes the current Godot editor instance be considered as self-contained. Returns an empty string if the current Godot editor instance isn't self-contained. See also `IsSelfContained()`.

### IsSelfContained

```csharp
public bool IsSelfContained()
```

引擎名：`is_self_contained`

Returns `true` if the editor is marked as self-contained, `false` otherwise. When self-contained mode is enabled, user configuration, data and cache files are saved in an `editor_data/` folder next to the editor binary. This makes portable usage easier and ensures the Godot editor minimizes file writes outside its own folder. Self-contained mode is not available for exported projects.

Self-contained mode can be enabled by creating a file named `._sc_` or `_sc_` in the same folder as the editor binary or macOS .app bundle while the editor is not running. See also `GetSelfContainedFile()`.

**Note:** On macOS, quarantine flag should be manually removed before using self-contained mode, see [Running on macOS](https://docs.godotengine.org/en/stable/tutorials/export/running_on_macos.html).

**Note:** On macOS, placing `_sc_` or any other file inside .app bundle will break digital signature and make it non-portable, consider placing it in the same folder as the .app bundle instead.

**Note:** The Steam release of Godot uses self-contained mode by default.
