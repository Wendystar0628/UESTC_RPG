<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorInterface

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

`EditorInterface` gives you control over Godot editor's window. It allows customizing the window, saving and (re-)loading scenes, rendering mesh previews, inspecting and editing resources and objects, and provides access to [`EditorSettings`](EditorSettings.md), [`EditorFileSystem`](EditorFileSystem.md), [`EditorResourcePreview`](EditorResourcePreview.md), [`ScriptEditor`](ScriptEditor.md), the editor viewport, and information about scenes.

**Note:** This class shouldn't be instantiated directly. Instead, access the singleton directly by its name.

```csharp
// In C# you can access it via the static Singleton property.
EditorSettings settings = EditorInterface.Singleton.GetEditorSettings();

```

## 属性

### DistractionFreeMode

```csharp
public bool DistractionFreeMode { get; set; }
```

引擎名：`distraction_free_mode`

If `true`, enables distraction-free mode which hides side docks to increase the space available for the main view.

### MovieMakerEnabled

```csharp
public bool MovieMakerEnabled { get; set; }
```

引擎名：`movie_maker_enabled`

If `true`, the Movie Maker mode is enabled in the editor. See [`MovieWriter`](MovieWriter.md) for more information.

### Singleton

```csharp
public static EditorInterface Singleton { get; }
```

## 方法

### AddRootNode

```csharp
public void AddRootNode(Node node)
```

引擎名：`add_root_node`

Makes `node` root of the currently opened scene. Only works if the scene is empty. If the `node` is a scene instance, an inheriting scene will be created.

### CloseScene

```csharp
public Error CloseScene()
```

引擎名：`close_scene`

Closes the currently active scene, discarding any pending changes in the process. Returns [`Error.Ok`](Error.md) on success or [`Error.DoesNotExist`](Error.md) if there is no scene to close.

### EditNode

```csharp
public void EditNode(Node node)
```

引擎名：`edit_node`

Edits the given [`Node`](Node.md). The node will be also selected if it's inside the scene tree.

### EditResource

```csharp
public void EditResource(Resource resource)
```

引擎名：`edit_resource`

Edits the given [`Resource`](Resource.md). If the resource is a [`Script`](Script.md) you can also edit it with `EditScript()` to specify the line and column position.

### EditScript

```csharp
public void EditScript(Script script, int line = -1, int column = 0, bool grabFocus = true)
```

引擎名：`edit_script`

Edits the given [`Script`](Script.md). The line and column on which to open the script can also be specified. The script will be open with the user-configured editor for the script's language which may be an external editor.

### GetBaseControl

```csharp
public Control GetBaseControl()
```

引擎名：`get_base_control`

Returns the main container of Godot editor's window. For example, you can use it to retrieve the size of the container and place your controls accordingly.

**Warning:** Removing and freeing this node will render the editor useless and may cause a crash.

### GetCommandPalette

```csharp
public EditorCommandPalette GetCommandPalette()
```

引擎名：`get_command_palette`

Returns the editor's [`EditorCommandPalette`](EditorCommandPalette.md) instance.

**Warning:** Removing and freeing this node will render a part of the editor useless and may cause a crash.

### GetCurrentDirectory

```csharp
public string GetCurrentDirectory()
```

引擎名：`get_current_directory`

Returns the current directory being viewed in the [`FileSystemDock`](FileSystemDock.md). If a file is selected, its base directory will be returned using `String.get_base_dir` instead.

### GetCurrentFeatureProfile

```csharp
public string GetCurrentFeatureProfile()
```

引擎名：`get_current_feature_profile`

Returns the name of the currently activated feature profile. If the default profile is currently active, an empty string is returned instead.

In order to get a reference to the [`EditorFeatureProfile`](EditorFeatureProfile.md), you must load the feature profile using [`EditorFeatureProfile.LoadFromFile`](EditorFeatureProfile.md).

**Note:** Feature profiles created via the user interface are loaded from the `feature_profiles` directory, as a file with the `.profile` extension. The editor configuration folder can be found by using [`EditorPaths.GetConfigDir`](EditorPaths.md).

### GetCurrentPath

```csharp
public string GetCurrentPath()
```

引擎名：`get_current_path`

Returns the current path being viewed in the [`FileSystemDock`](FileSystemDock.md).

### GetEditedSceneRoot

```csharp
public Node GetEditedSceneRoot()
```

引擎名：`get_edited_scene_root`

Returns the edited (current) scene's root [`Node`](Node.md).

### GetEditorLanguage

```csharp
public string GetEditorLanguage()
```

引擎名：`get_editor_language`

Returns the language currently used for the editor interface.

### GetEditorMainScreen

```csharp
public VBoxContainer GetEditorMainScreen()
```

引擎名：`get_editor_main_screen`

Returns the editor control responsible for main screen plugins and tools. Use it with plugins that implement [`EditorPlugin._HasMainScreen()`](EditorPlugin.md).

**Note:** This node is a [`VBoxContainer`](VBoxContainer.md), which means that if you add a [`Control`](Control.md) child to it, you need to set the child's [`Control.SizeFlagsVertical`](Control.md) to [`Control.SizeFlags.ExpandFill`](Control.md) to make it use the full available space.

**Warning:** Removing and freeing this node will render a part of the editor useless and may cause a crash.

### GetEditorPaths

```csharp
public EditorPaths GetEditorPaths()
```

引擎名：`get_editor_paths`

Returns the [`EditorPaths`](EditorPaths.md) singleton.

### GetEditorScale

```csharp
public float GetEditorScale()
```

引擎名：`get_editor_scale`

Returns the actual scale of the editor UI (`1.0` being 100% scale). This can be used to adjust position and dimensions of the UI added by plugins.

**Note:** This value is set via the `EditorSettings.interface/editor/appearance/display_scale` and `EditorSettings.interface/editor/appearance/custom_display_scale` settings. The editor must be restarted for changes to be properly applied.

### GetEditorSettings

```csharp
public EditorSettings GetEditorSettings()
```

引擎名：`get_editor_settings`

Returns the editor's [`EditorSettings`](EditorSettings.md) instance.

### GetEditorTheme

```csharp
public Theme GetEditorTheme()
```

引擎名：`get_editor_theme`

Returns the editor's [`Theme`](Theme.md).

**Note:** When creating custom editor UI, prefer accessing theme items directly from your GUI nodes using the `get_theme_*` methods.

### GetEditorToaster

```csharp
public EditorToaster GetEditorToaster()
```

引擎名：`get_editor_toaster`

Returns the editor's [`EditorToaster`](EditorToaster.md).

### GetEditorUndoRedo

```csharp
public EditorUndoRedoManager GetEditorUndoRedo()
```

引擎名：`get_editor_undo_redo`

Returns the editor's [`EditorUndoRedoManager`](EditorUndoRedoManager.md).

### GetEditorViewport2D

```csharp
public SubViewport GetEditorViewport2D()
```

Returns the 2D editor [`SubViewport`](SubViewport.md). It does not have a camera. Instead, the view transforms are done directly and can be accessed with [`Viewport.GlobalCanvasTransform`](Viewport.md).

### GetEditorViewport3D

```csharp
public SubViewport GetEditorViewport3D(int idx = 0)
```

Returns the specified 3D editor [`SubViewport`](SubViewport.md), from `0` to `3`. The viewport can be used to access the active editor cameras with [`Viewport.GetCamera3D`](Viewport.md).

### GetFileSystemDock

```csharp
public FileSystemDock GetFileSystemDock()
```

引擎名：`get_file_system_dock`

Returns the editor's [`FileSystemDock`](FileSystemDock.md) instance.

**Warning:** Removing and freeing this node will render a part of the editor useless and may cause a crash.

### GetInspector

```csharp
public EditorInspector GetInspector()
```

引擎名：`get_inspector`

Returns the editor's [`EditorInspector`](EditorInspector.md) instance.

**Warning:** Removing and freeing this node will render a part of the editor useless and may cause a crash.

### GetNode3DRotateSnap

```csharp
public float GetNode3DRotateSnap()
```

Returns the amount of degrees the 3D editor's rotational snapping is set to.

### GetNode3DScaleSnap

```csharp
public float GetNode3DScaleSnap()
```

Returns the amount of units the 3D editor's scale snapping is set to.

### GetNode3DTranslateSnap

```csharp
public float GetNode3DTranslateSnap()
```

Returns the amount of units the 3D editor's translation snapping is set to.

### GetOpenSceneRoots

```csharp
public Array<Node> GetOpenSceneRoots()
```

引擎名：`get_open_scene_roots`

Returns an array with references to the root nodes of the currently opened scenes.

### GetOpenScenes

```csharp
public string[] GetOpenScenes()
```

引擎名：`get_open_scenes`

Returns an array with the file paths of the currently opened scenes.

### GetPlayingScene

```csharp
public string GetPlayingScene()
```

引擎名：`get_playing_scene`

Returns the name of the scene that is being played. If no scene is currently being played, returns an empty string.

### GetResourceFilesystem

```csharp
public EditorFileSystem GetResourceFilesystem()
```

引擎名：`get_resource_filesystem`

Returns the editor's [`EditorFileSystem`](EditorFileSystem.md) instance.

### GetResourcePreviewer

```csharp
public EditorResourcePreview GetResourcePreviewer()
```

引擎名：`get_resource_previewer`

Returns the editor's [`EditorResourcePreview`](EditorResourcePreview.md) instance.

### GetScriptEditor

```csharp
public ScriptEditor GetScriptEditor()
```

引擎名：`get_script_editor`

Returns the editor's [`ScriptEditor`](ScriptEditor.md) instance.

**Warning:** Removing and freeing this node will render a part of the editor useless and may cause a crash.

### GetSelectedPaths

```csharp
public string[] GetSelectedPaths()
```

引擎名：`get_selected_paths`

Returns an array containing the paths of the currently selected files (and directories) in the [`FileSystemDock`](FileSystemDock.md).

### GetSelection

```csharp
public EditorSelection GetSelection()
```

引擎名：`get_selection`

Returns the editor's [`EditorSelection`](EditorSelection.md) instance.

### GetUnsavedScenes

```csharp
public string[] GetUnsavedScenes()
```

引擎名：`get_unsaved_scenes`

Returns an array of file paths of currently unsaved scenes.

### InspectObject

```csharp
public void InspectObject(GodotObject object, string forProperty = "", bool inspectorOnly = false)
```

引擎名：`inspect_object`

Shows the given property on the given `object` in the editor's Inspector dock. If `inspectorOnly` is `true`, plugins will not attempt to edit `object`.

### IsDistractionFreeModeEnabled

```csharp
public bool IsDistractionFreeModeEnabled()
```

引擎名：`is_distraction_free_mode_enabled`

### IsMovieMakerEnabled

```csharp
public bool IsMovieMakerEnabled()
```

引擎名：`is_movie_maker_enabled`

### IsMultiWindowEnabled

```csharp
public bool IsMultiWindowEnabled()
```

引擎名：`is_multi_window_enabled`

Returns `true` if multiple window support is enabled in the editor. Multiple window support is enabled if *all* of these statements are true:

- `EditorSettings.interface/multi_window/enable` is `true`.

- `EditorSettings.interface/editor/display/single_window_mode` is `false`.

- [`Viewport.GuiEmbedSubwindows`](Viewport.md) is `false`. This is forced to `true` on platforms that don't support multiple windows such as Web, or when the `--single-window` [command line argument]($DOCS_URL/tutorials/editor/command_line_tutorial.html) is used.

### IsNode3DSnapEnabled

```csharp
public bool IsNode3DSnapEnabled()
```

Returns `true` if the 3D editor currently has snapping mode enabled, and `false` otherwise.

### IsObjectEdited

```csharp
public bool IsObjectEdited(GodotObject object)
```

引擎名：`is_object_edited`

Returns `true` if the object has been marked as edited through `SetObjectEdited()`.

### IsPlayingScene

```csharp
public bool IsPlayingScene()
```

引擎名：`is_playing_scene`

Returns `true` if a scene is currently being played, `false` otherwise. Paused scenes are considered as being played.

### IsPluginEnabled

```csharp
public bool IsPluginEnabled(string plugin)
```

引擎名：`is_plugin_enabled`

Returns `true` if the specified `plugin` is enabled. The plugin name is the same as its directory name.

### MakeMeshPreviews

```csharp
public Array<Texture2D> MakeMeshPreviews(Array<Mesh> meshes, int previewSize)
```

引擎名：`make_mesh_previews`

Returns mesh previews rendered at the given size as an [`Collections.Array`](Collections.Array.md) of [`Texture2D`](Texture2D.md)s.

### MarkSceneAsUnsaved

```csharp
public void MarkSceneAsUnsaved()
```

引擎名：`mark_scene_as_unsaved`

Marks the current scene tab as unsaved.

### OpenSceneFromPath

```csharp
public void OpenSceneFromPath(string sceneFilepath, bool setInherited = false)
```

引擎名：`open_scene_from_path`

Opens the scene at the given path. If `setInherited` is `true`, creates a new inherited scene.

### OpenSceneFromPath

```csharp
public void OpenSceneFromPath(string scenePath)
```

引擎名：`open_scene_from_path`

Opens the scene at the given path. If `setInherited` is `true`, creates a new inherited scene.

### PlayCurrentScene

```csharp
public void PlayCurrentScene()
```

引擎名：`play_current_scene`

Plays the currently active scene.

### PlayCustomScene

```csharp
public void PlayCustomScene(string sceneFilepath)
```

引擎名：`play_custom_scene`

Plays the scene specified by its filepath.

### PlayMainScene

```csharp
public void PlayMainScene()
```

引擎名：`play_main_scene`

Plays the main scene.

### PopupCreateDialog

```csharp
public void PopupCreateDialog(Callable callback, StringName baseType = null, string currentType = "", string dialogTitle = "", Array<StringName> typeBlocklist = null)
```

引擎名：`popup_create_dialog`

Pops up an editor dialog for creating an object.

The `callback` must take a single argument of type `System.String`, which will contain the type name of the selected object (or the script path of the type, if the type is created from a script), or be an empty string if no item is selected.

The `baseType` specifies the base type of objects to display. For example, if you set this to "Resource", all types derived from [`Resource`](Resource.md) will display in the create dialog.

The `currentType` will be passed in the search box of the create dialog, and the specified type can be immediately selected when the dialog pops up. If the `currentType` is not derived from `baseType`, there will be no result of the type in the dialog.

The `dialogTitle` allows you to define a custom title for the dialog. This is useful if you want to accurately hint the usage of the dialog. If the `dialogTitle` is an empty string, the dialog will use "Create New 'Base Type'" as the default title.

The `typeBlocklist` contains a list of type names, and the types in the blocklist will be hidden from the create dialog.

**Note:** Trying to list the base type in the `typeBlocklist` will hide all types derived from the base type from the create dialog.

### PopupDialog

```csharp
public void PopupDialog(Window dialog, Rect2I? rect = null)
```

引擎名：`popup_dialog`

Pops up the `dialog` in the editor UI with [`Window.PopupExclusive`](Window.md). The dialog must have no current parent, otherwise the method fails.

See also [`Window.SetUnparentWhenInvisible`](Window.md).

参数：
- `rect`：If the parameter is null, then the default value is `new Rect2I(new Vector2I(0, 0), new Vector2I(0, 0))`.

### PopupDialogCentered

```csharp
public void PopupDialogCentered(Window dialog, Vector2I? minsize = null)
```

引擎名：`popup_dialog_centered`

Pops up the `dialog` in the editor UI with [`Window.PopupExclusiveCentered`](Window.md). The dialog must have no current parent, otherwise the method fails.

See also [`Window.SetUnparentWhenInvisible`](Window.md).

参数：
- `minsize`：If the parameter is null, then the default value is `new Vector2I(0, 0)`.

### PopupDialogCenteredClamped

```csharp
public void PopupDialogCenteredClamped(Window dialog, Vector2I? minsize = null, float fallbackRatio = 0.75f)
```

引擎名：`popup_dialog_centered_clamped`

Pops up the `dialog` in the editor UI with [`Window.PopupExclusiveCenteredClamped`](Window.md). The dialog must have no current parent, otherwise the method fails.

See also [`Window.SetUnparentWhenInvisible`](Window.md).

参数：
- `minsize`：If the parameter is null, then the default value is `new Vector2I(0, 0)`.

### PopupDialogCenteredRatio

```csharp
public void PopupDialogCenteredRatio(Window dialog, float ratio = 0.8f)
```

引擎名：`popup_dialog_centered_ratio`

Pops up the `dialog` in the editor UI with [`Window.PopupExclusiveCenteredRatio`](Window.md). The dialog must have no current parent, otherwise the method fails.

See also [`Window.SetUnparentWhenInvisible`](Window.md).

### PopupMethodSelector

```csharp
public void PopupMethodSelector(GodotObject object, Callable callback, string currentValue = "")
```

引擎名：`popup_method_selector`

Pops up an editor dialog for selecting a method from `object`. The `callback` must take a single argument of type `System.String` which will contain the name of the selected method or be empty if the dialog is canceled. If `currentValue` is provided, the method will be selected automatically in the method list, if it exists.

### PopupNodeSelector

```csharp
public void PopupNodeSelector(Callable callback, Array<StringName> validTypes = null, Node currentValue = null)
```

引擎名：`popup_node_selector`

Pops up an editor dialog for selecting a [`Node`](Node.md) from the edited scene. The `callback` must take a single argument of type [`NodePath`](NodePath.md). It is called on the selected [`NodePath`](NodePath.md) or the empty path `^""` if the dialog is canceled. If `validTypes` is provided, the dialog will only show Nodes that match one of the listed Node types. If `currentValue` is provided, the Node will be automatically selected in the tree, if it exists.

**Example:** Display the node selection dialog as soon as this node is added to the tree for the first time:

```csharp
func _ready():
if Engine.is_editor_hint():
EditorInterface.popup_node_selector(_on_node_selected, ["Button"])

func _on_node_selected(node_path):
if node_path.is_empty():
print("node selection canceled")
else:
print("selected ", node_path)

```

### PopupNodeSelector

```csharp
public void PopupNodeSelector(Callable callback, Array<StringName> validTypes)
```

引擎名：`popup_node_selector`

Pops up an editor dialog for selecting a [`Node`](Node.md) from the edited scene. The `callback` must take a single argument of type [`NodePath`](NodePath.md). It is called on the selected [`NodePath`](NodePath.md) or the empty path `^""` if the dialog is canceled. If `validTypes` is provided, the dialog will only show Nodes that match one of the listed Node types. If `currentValue` is provided, the Node will be automatically selected in the tree, if it exists.

**Example:** Display the node selection dialog as soon as this node is added to the tree for the first time:

```csharp
func _ready():
if Engine.is_editor_hint():
EditorInterface.popup_node_selector(_on_node_selected, ["Button"])

func _on_node_selected(node_path):
if node_path.is_empty():
print("node selection canceled")
else:
print("selected ", node_path)

```

### PopupPropertySelector

```csharp
public void PopupPropertySelector(GodotObject object, Callable callback, ReadOnlySpan<int> typeFilter)
```

引擎名：`popup_property_selector`

Pops up an editor dialog for selecting properties from `object`. The `callback` must take a single argument of type [`NodePath`](NodePath.md). It is called on the selected property path (see `NodePath.GetAsPropertyPath`) or the empty path `^""` if the dialog is canceled. If `typeFilter` is provided, the dialog will only show properties that match one of the listed [`Variant.Type`](Variant.md) values. If `currentValue` is provided, the property will be selected automatically in the property list, if it exists.

```csharp
func _ready():
if Engine.is_editor_hint():
EditorInterface.popup_property_selector(this, _on_property_selected, [TYPE_INT])

func _on_property_selected(property_path):
if property_path.is_empty():
print("property selection canceled")
else:
print("selected ", property_path)

```

### PopupPropertySelector

```csharp
public void PopupPropertySelector(GodotObject object, Callable callback, ReadOnlySpan<int> typeFilter, string currentValue)
```

引擎名：`popup_property_selector`

Pops up an editor dialog for selecting properties from `object`. The `callback` must take a single argument of type [`NodePath`](NodePath.md). It is called on the selected property path (see `NodePath.GetAsPropertyPath`) or the empty path `^""` if the dialog is canceled. If `typeFilter` is provided, the dialog will only show properties that match one of the listed [`Variant.Type`](Variant.md) values. If `currentValue` is provided, the property will be selected automatically in the property list, if it exists.

```csharp
func _ready():
if Engine.is_editor_hint():
EditorInterface.popup_property_selector(this, _on_property_selected, [TYPE_INT])

func _on_property_selected(property_path):
if property_path.is_empty():
print("property selection canceled")
else:
print("selected ", property_path)

```

### PopupPropertySelector

```csharp
public void PopupPropertySelector(GodotObject object, Callable callback, int[] typeFilter = null, string currentValue = "")
```

引擎名：`popup_property_selector`

Pops up an editor dialog for selecting properties from `object`. The `callback` must take a single argument of type [`NodePath`](NodePath.md). It is called on the selected property path (see `NodePath.GetAsPropertyPath`) or the empty path `^""` if the dialog is canceled. If `typeFilter` is provided, the dialog will only show properties that match one of the listed [`Variant.Type`](Variant.md) values. If `currentValue` is provided, the property will be selected automatically in the property list, if it exists.

```csharp
func _ready():
if Engine.is_editor_hint():
EditorInterface.popup_property_selector(this, _on_property_selected, [TYPE_INT])

func _on_property_selected(property_path):
if property_path.is_empty():
print("property selection canceled")
else:
print("selected ", property_path)

```

参数：
- `typeFilter`：If the parameter is null, then the default value is `Array.Empty<int>()`.

### PopupPropertySelector

```csharp
public void PopupPropertySelector(GodotObject object, Callable callback, int[] typeFilter)
```

引擎名：`popup_property_selector`

Pops up an editor dialog for selecting properties from `object`. The `callback` must take a single argument of type [`NodePath`](NodePath.md). It is called on the selected property path (see `NodePath.GetAsPropertyPath`) or the empty path `^""` if the dialog is canceled. If `typeFilter` is provided, the dialog will only show properties that match one of the listed [`Variant.Type`](Variant.md) values. If `currentValue` is provided, the property will be selected automatically in the property list, if it exists.

```csharp
func _ready():
if Engine.is_editor_hint():
EditorInterface.popup_property_selector(this, _on_property_selected, [TYPE_INT])

func _on_property_selected(property_path):
if property_path.is_empty():
print("property selection canceled")
else:
print("selected ", property_path)

```

参数：
- `typeFilter`：If the parameter is null, then the default value is `Array.Empty<int>()`.

### PopupQuickOpen

```csharp
public void PopupQuickOpen(Callable callback, Array<StringName> baseTypes = null)
```

引擎名：`popup_quick_open`

Pops up an editor dialog for quick selecting a resource file. The `callback` must take a single argument of type `System.String` which will contain the path of the selected resource or be empty if the dialog is canceled. If `baseTypes` is provided, the dialog will only show resources that match these types. Only types deriving from [`Resource`](Resource.md) are supported.

### ReloadSceneFromPath

```csharp
public void ReloadSceneFromPath(string sceneFilepath)
```

引擎名：`reload_scene_from_path`

Reloads the scene at the given path. Fails if the scene is not open.

### RestartEditor

```csharp
public void RestartEditor(bool save = true)
```

引擎名：`restart_editor`

Restarts the editor. This closes the editor and then opens the same project. If `save` is `true`, the project will be saved before restarting.

### SaveAllScenes

```csharp
public void SaveAllScenes()
```

引擎名：`save_all_scenes`

Saves all opened scenes in the editor.

### SaveScene

```csharp
public Error SaveScene()
```

引擎名：`save_scene`

Saves the currently active scene. Returns either [`Error.Ok`](Error.md) or [`Error.CantCreate`](Error.md).

### SaveSceneAs

```csharp
public void SaveSceneAs(string path, bool withPreview = true)
```

引擎名：`save_scene_as`

Saves the currently active scene as a file at `path`.

### SelectFile

```csharp
public void SelectFile(string file)
```

引擎名：`select_file`

Selects the file, with the path provided by `file`, in the FileSystem dock.

### SetCurrentFeatureProfile

```csharp
public void SetCurrentFeatureProfile(string profileName)
```

引擎名：`set_current_feature_profile`

Selects and activates the specified feature profile with the given `profileName`. Set `profileName` to an empty string to reset to the default feature profile.

A feature profile can be created programmatically using the [`EditorFeatureProfile`](EditorFeatureProfile.md) class.

**Note:** The feature profile that gets activated must be located in the `feature_profiles` directory, as a file with the `.profile` extension. If a profile could not be found, an error occurs. The editor configuration folder can be found by using [`EditorPaths.GetConfigDir`](EditorPaths.md).

### SetDistractionFreeMode

```csharp
public void SetDistractionFreeMode(bool enter)
```

引擎名：`set_distraction_free_mode`

### SetMainScreenEditor

```csharp
public void SetMainScreenEditor(string name)
```

引擎名：`set_main_screen_editor`

Sets the editor's current main screen to the one specified in `name`. `name` must match the title of the tab in question exactly (e.g. `2D`, `3D`, `Script`, `Game`, or `Asset Store` for default tabs).

### SetMovieMakerEnabled

```csharp
public void SetMovieMakerEnabled(bool enabled)
```

引擎名：`set_movie_maker_enabled`

### SetObjectEdited

```csharp
public void SetObjectEdited(GodotObject object, bool edited)
```

引擎名：`set_object_edited`

If `edited` is `true`, the object is marked as edited.

**Note:** This is primarily used by the editor for [`Resource`](Resource.md) based objects to track their modified state. For example, any changes to an open scene, a resource in the inspector, or an edited script will cause this method to be called with `true`. Saving the scene, script, or resource resets the edited state by calling this method with `false`.

**Note:** Each call to this method increments the object's edited version. This is used to track changes in the editor and to trigger when thumbnails should be regenerated for resources.

### SetPluginEnabled

```csharp
public void SetPluginEnabled(string plugin, bool enabled)
```

引擎名：`set_plugin_enabled`

Sets the enabled status of a plugin. The plugin name is the same as its directory name.

### StopPlayingScene

```csharp
public void StopPlayingScene()
```

引擎名：`stop_playing_scene`

Stops the scene that is currently playing.
