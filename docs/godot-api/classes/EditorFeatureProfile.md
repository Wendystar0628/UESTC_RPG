<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorFeatureProfile

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

An editor feature profile can be used to disable specific features of the Godot editor. When disabled, the features won't appear in the editor, which makes the editor less cluttered. This is useful in education settings to reduce confusion or when working in a team. For example, artists and level designers could use a feature profile that disables the script editor to avoid accidentally making changes to files they aren't supposed to edit.

To manage editor feature profiles visually, use **Editor > Manage Feature Profiles...** at the top of the editor window.

## 构造函数

### EditorFeatureProfile

```csharp
public EditorFeatureProfile()
```

## 方法

### GetFeatureName

```csharp
public string GetFeatureName(EditorFeatureProfile.Feature feature)
```

引擎名：`get_feature_name`

Returns the specified `feature`'s human-readable name.

### IsClassDisabled

```csharp
public bool IsClassDisabled(StringName className)
```

引擎名：`is_class_disabled`

Returns `true` if the class specified by `className` is disabled. When disabled, the class won't appear in the Create New Node dialog.

### IsClassEditorDisabled

```csharp
public bool IsClassEditorDisabled(StringName className)
```

引擎名：`is_class_editor_disabled`

Returns `true` if editing for the class specified by `className` is disabled. When disabled, the class will still appear in the Create New Node dialog but the Inspector will be read-only when selecting a node that extends the class.

### IsClassPropertyDisabled

```csharp
public bool IsClassPropertyDisabled(StringName className, StringName property)
```

引擎名：`is_class_property_disabled`

Returns `true` if `property` is disabled in the class specified by `className`. When a property is disabled, it won't appear in the Inspector when selecting a node that extends the class specified by `className`.

### IsFeatureDisabled

```csharp
public bool IsFeatureDisabled(EditorFeatureProfile.Feature feature)
```

引擎名：`is_feature_disabled`

Returns `true` if the `feature` is disabled. When a feature is disabled, it will disappear from the editor entirely.

### LoadFromFile

```csharp
public Error LoadFromFile(string path)
```

引擎名：`load_from_file`

Loads an editor feature profile from a file. The file must follow the JSON format obtained by using the feature profile manager's **Export** button or the `SaveToFile()` method.

**Note:** Feature profiles created via the user interface are loaded from the `feature_profiles` directory, as a file with the `.profile` extension. The editor configuration folder can be found by using [`EditorPaths.GetConfigDir`](EditorPaths.md).

### SaveToFile

```csharp
public Error SaveToFile(string path)
```

引擎名：`save_to_file`

Saves the editor feature profile to a file in JSON format. It can then be imported using the feature profile manager's **Import** button or the `LoadFromFile()` method.

**Note:** Feature profiles created via the user interface are saved in the `feature_profiles` directory, as a file with the `.profile` extension. The editor configuration folder can be found by using [`EditorPaths.GetConfigDir`](EditorPaths.md).

### SetDisableClass

```csharp
public void SetDisableClass(StringName className, bool disable)
```

引擎名：`set_disable_class`

If `disable` is `true`, disables the class specified by `className`. When disabled, the class won't appear in the Create New Node dialog.

### SetDisableClassEditor

```csharp
public void SetDisableClassEditor(StringName className, bool disable)
```

引擎名：`set_disable_class_editor`

If `disable` is `true`, disables editing for the class specified by `className`. When disabled, the class will still appear in the Create New Node dialog but the Inspector will be read-only when selecting a node that extends the class.

### SetDisableClassProperty

```csharp
public void SetDisableClassProperty(StringName className, StringName property, bool disable)
```

引擎名：`set_disable_class_property`

If `disable` is `true`, disables editing for `property` in the class specified by `className`. When a property is disabled, it won't appear in the Inspector when selecting a node that extends the class specified by `className`.

### SetDisableFeature

```csharp
public void SetDisableFeature(EditorFeatureProfile.Feature feature, bool disable)
```

引擎名：`set_disable_feature`

If `disable` is `true`, disables the editor feature specified in `feature`. When a feature is disabled, it will disappear from the editor entirely.

## Feature

命名空间 `Godot`；enum；仅编辑器（GodotSharpEditor）

### 枚举值

- `Feature3D = 0`：The 3D editor.
- `Script = 1`：The Script tab, which contains the script editor and class reference browser.
- `AssetLib = 2`：The Asset Store tab.
- `SceneTree = 3`：Scene tree editing.
- `NodeDock = 4`：The Node dock.
- `FilesystemDock = 5`：The FileSystem dock.
- `ImportDock = 6`：The Import dock.
- `HistoryDock = 7`：The History dock.
- `Game = 8`：The Game tab, which allows embedding the game window and selecting nodes by clicking inside of it.
- `SignalsDock = 9`：The Signals dock.
- `GroupsDock = 10`：The Groups dock.
- `Max = 11`：Represents the size of the `EditorFeatureProfile.Feature` enum.
