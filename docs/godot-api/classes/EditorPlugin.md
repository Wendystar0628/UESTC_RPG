<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorPlugin

命名空间 `Godot`；class；继承 [`Node`](Node.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

Plugins are used by the editor to extend functionality. The most common types of plugins are those which edit a given node or resource type, import plugins and export plugins. See also [`EditorScript`](EditorScript.md) to add functions to the editor.

**Note:** Some names in this class contain "left" or "right" (e.g. [`EditorPlugin.DockSlot.LeftUl`](EditorPlugin.md)). These APIs assume left-to-right layout, and would be backwards when using right-to-left layout. These names are kept for compatibility reasons.

## 构造函数

### EditorPlugin

```csharp
public EditorPlugin()
```

## 方法

### AddAutoloadSingleton

```csharp
public void AddAutoloadSingleton(string name, string path)
```

引擎名：`add_autoload_singleton`

Adds a script at `path` to the Autoload list as `name`.

### AddContextMenuPlugin

```csharp
public void AddContextMenuPlugin(EditorContextMenuPlugin.ContextMenuSlot slot, EditorContextMenuPlugin plugin)
```

引擎名：`add_context_menu_plugin`

Adds a plugin to the context menu. `slot` is the context menu where the plugin will be added.

**Note:** A plugin instance can belong only to a single context menu slot.

### AddControlToBottomPanel

```csharp
public Button AddControlToBottomPanel(Control control, string title)
```

引擎名：`add_control_to_bottom_panel`

Adds a control to the bottom panel (together with Output, Debug, Animation, etc.). Returns a reference to a button that is outside the scene tree. It's up to you to hide/show the button when needed. When your plugin is deactivated, make sure to remove your custom control with `RemoveControlFromBottomPanel()` and free it with [`Node.QueueFree`](Node.md).

`shortcut` is a shortcut that, when activated, will toggle the bottom panel's visibility. The shortcut object is only set when this control is added to the bottom panel.

**Note** See the default editor bottom panel shortcuts in the Editor Settings for inspiration. By convention, they all use Alt modifier.

### AddControlToBottomPanel

```csharp
public Button AddControlToBottomPanel(Control control, string title, Shortcut shortcut = null)
```

引擎名：`add_control_to_bottom_panel`

Adds a control to the bottom panel (together with Output, Debug, Animation, etc.). Returns a reference to a button that is outside the scene tree. It's up to you to hide/show the button when needed. When your plugin is deactivated, make sure to remove your custom control with `RemoveControlFromBottomPanel()` and free it with [`Node.QueueFree`](Node.md).

`shortcut` is a shortcut that, when activated, will toggle the bottom panel's visibility. The shortcut object is only set when this control is added to the bottom panel.

**Note** See the default editor bottom panel shortcuts in the Editor Settings for inspiration. By convention, they all use Alt modifier.

### AddControlToContainer

```csharp
public void AddControlToContainer(EditorPlugin.CustomControlContainer container, Control control)
```

引擎名：`add_control_to_container`

Adds a custom control to a container in the editor UI.

Please remember that you have to manage the visibility of your custom controls yourself (and likely hide it after adding it).

When your plugin is deactivated, make sure to remove your custom control with `RemoveControlFromContainer()` and free it with [`Node.QueueFree`](Node.md).

### AddControlToDock

```csharp
public void AddControlToDock(EditorPlugin.DockSlot slot, Control control)
```

引擎名：`add_control_to_dock`

Adds the control to a specific dock slot.

If the dock is repositioned and as long as the plugin is active, the editor will save the dock position on further sessions.

When your plugin is deactivated, make sure to remove your custom control with `RemoveControlFromDocks()` and free it with [`Node.QueueFree`](Node.md).

Optionally, you can specify a shortcut parameter. When pressed, this shortcut will open and focus the dock.

### AddControlToDock

```csharp
public void AddControlToDock(EditorPlugin.DockSlot slot, Control control, Shortcut shortcut = null)
```

引擎名：`add_control_to_dock`

Adds the control to a specific dock slot.

If the dock is repositioned and as long as the plugin is active, the editor will save the dock position on further sessions.

When your plugin is deactivated, make sure to remove your custom control with `RemoveControlFromDocks()` and free it with [`Node.QueueFree`](Node.md).

Optionally, you can specify a shortcut parameter. When pressed, this shortcut will open and focus the dock.

### AddCustomType

```csharp
public void AddCustomType(string type, string base, Script script, Texture2D icon)
```

引擎名：`add_custom_type`

Adds a custom type, which will appear in the list of nodes or resources.

When a given node or resource is selected, the base type will be instantiated (e.g. "Node3D", "Control", "Resource"), then the script will be loaded and set to this object.

**Note:** The base type is the base engine class which this type's class hierarchy inherits, not any custom type parent classes.

You can use the virtual method `_Handles()` to check if your custom object is being edited by checking the script or using the `is` keyword.

During run-time, this will be a simple object with a script so this function does not need to be called then.

**Note:** Custom types added this way are not true classes. They are just a helper to create a node with specific script.

### AddDebuggerPlugin

```csharp
public void AddDebuggerPlugin(EditorDebuggerPlugin script)
```

引擎名：`add_debugger_plugin`

Adds a [`Script`](Script.md) as debugger plugin to the Debugger. The script must extend [`EditorDebuggerPlugin`](EditorDebuggerPlugin.md).

### AddDock

```csharp
public void AddDock(EditorDock dock)
```

引擎名：`add_dock`

Adds a new dock.

When your plugin is deactivated, make sure to remove your custom dock with `RemoveDock()` and free it with [`Node.QueueFree`](Node.md).

### AddExportPlatform

```csharp
public void AddExportPlatform(EditorExportPlatform platform)
```

引擎名：`add_export_platform`

Registers a new [`EditorExportPlatform`](EditorExportPlatform.md). Export platforms provides functionality of exporting to the specific platform.

### AddExportPlugin

```csharp
public void AddExportPlugin(EditorExportPlugin plugin)
```

引擎名：`add_export_plugin`

Registers a new [`EditorExportPlugin`](EditorExportPlugin.md). Export plugins are used to perform tasks when the project is being exported.

See `AddInspectorPlugin()` for an example of how to register a plugin.

### AddImportPlugin

```csharp
public void AddImportPlugin(EditorImportPlugin importer, bool firstPriority = false)
```

引擎名：`add_import_plugin`

Registers a new [`EditorImportPlugin`](EditorImportPlugin.md). Import plugins are used to import custom and unsupported assets as a custom [`Resource`](Resource.md) type.

If `firstPriority` is `true`, the new import plugin is inserted first in the list and takes precedence over pre-existing plugins.

**Note:** If you want to import custom 3D asset formats use `AddSceneFormatImporterPlugin()` instead.

See `AddInspectorPlugin()` for an example of how to register a plugin.

### AddInspectorPlugin

```csharp
public void AddInspectorPlugin(EditorInspectorPlugin plugin)
```

引擎名：`add_inspector_plugin`

Registers a new [`EditorInspectorPlugin`](EditorInspectorPlugin.md). Inspector plugins are used to extend [`EditorInspector`](EditorInspector.md) and provide custom configuration tools for your object's properties.

**Note:** Always use `RemoveInspectorPlugin()` to remove the registered [`EditorInspectorPlugin`](EditorInspectorPlugin.md) when your `EditorPlugin` is disabled to prevent leaks and an unexpected behavior.

### AddNode3DGizmoPlugin

```csharp
public void AddNode3DGizmoPlugin(EditorNode3DGizmoPlugin plugin)
```

Registers a new [`EditorNode3DGizmoPlugin`](EditorNode3DGizmoPlugin.md). Gizmo plugins are used to add custom gizmos to the 3D preview viewport for a [`Node3D`](Node3D.md).

See `AddInspectorPlugin()` for an example of how to register a plugin.

### AddResourceConversionPlugin

```csharp
public void AddResourceConversionPlugin(EditorResourceConversionPlugin plugin)
```

引擎名：`add_resource_conversion_plugin`

Registers a new [`EditorResourceConversionPlugin`](EditorResourceConversionPlugin.md). Resource conversion plugins are used to add custom resource converters to the editor inspector.

See [`EditorResourceConversionPlugin`](EditorResourceConversionPlugin.md) for an example of how to create a resource conversion plugin.

### AddSceneFormatImporterPlugin

```csharp
public void AddSceneFormatImporterPlugin(EditorSceneFormatImporter sceneFormatImporter, bool firstPriority = false)
```

引擎名：`add_scene_format_importer_plugin`

Registers a new [`EditorSceneFormatImporter`](EditorSceneFormatImporter.md). Scene importers are used to import custom 3D asset formats as scenes.

If `firstPriority` is `true`, the new import plugin is inserted first in the list and takes precedence over pre-existing plugins.

### AddScenePostImportPlugin

```csharp
public void AddScenePostImportPlugin(EditorScenePostImportPlugin sceneImportPlugin, bool firstPriority = false)
```

引擎名：`add_scene_post_import_plugin`

Add an [`EditorScenePostImportPlugin`](EditorScenePostImportPlugin.md). These plugins allow customizing the import process of 3D assets by adding new options to the import dialogs.

If `firstPriority` is `true`, the new import plugin is inserted first in the list and takes precedence over pre-existing plugins.

### AddToolMenuItem

```csharp
public void AddToolMenuItem(string name, Callable callable)
```

引擎名：`add_tool_menu_item`

Adds a custom menu item to **Project > Tools** named `name`. When clicked, the provided `callable` will be called.

### AddToolSubmenuItem

```csharp
public void AddToolSubmenuItem(string name, PopupMenu submenu)
```

引擎名：`add_tool_submenu_item`

Adds a custom [`PopupMenu`](PopupMenu.md) submenu under **Project > Tools >** `name`. Use `RemoveToolMenuItem()` on plugin clean up to remove the menu.

### AddTranslationParserPlugin

```csharp
public void AddTranslationParserPlugin(EditorTranslationParserPlugin parser)
```

引擎名：`add_translation_parser_plugin`

Registers a custom translation parser plugin for extracting translatable strings from custom files.

### AddUndoRedoInspectorHookCallback

```csharp
public void AddUndoRedoInspectorHookCallback(Callable callable)
```

引擎名：`add_undo_redo_inspector_hook_callback`

Hooks a callback into the undo/redo action creation when a property is modified in the inspector. This allows, for example, to save other properties that may be lost when a given property is modified.

The callback should have 4 arguments: [`GodotObject`](GodotObject.md) `undo_redo`, [`GodotObject`](GodotObject.md) `modified_object`, `System.String` `property` and [`Variant`](Variant.md) `new_value`. They are, respectively, the [`UndoRedo`](UndoRedo.md) object used by the inspector, the currently modified object, the name of the modified property and the new value the property is about to take.

### GetEditorInterface

```csharp
public EditorInterface GetEditorInterface()
```

引擎名：`get_editor_interface`

Returns the [`EditorInterface`](EditorInterface.md) singleton instance.

### GetExportAsMenu

```csharp
public PopupMenu GetExportAsMenu()
```

引擎名：`get_export_as_menu`

Returns the [`PopupMenu`](PopupMenu.md) under **Scene > Export As...**.

### GetPluginVersion

```csharp
public string GetPluginVersion()
```

引擎名：`get_plugin_version`

Provide the version of the plugin declared in the `plugin.cfg` config file.

### GetScriptCreateDialog

```csharp
public ScriptCreateDialog GetScriptCreateDialog()
```

引擎名：`get_script_create_dialog`

Gets the Editor's dialog used for making scripts.

**Note:** Users can configure it before use.

**Warning:** Removing and freeing this node will render a part of the editor useless and may cause a crash.

### GetUndoRedo

```csharp
public EditorUndoRedoManager GetUndoRedo()
```

引擎名：`get_undo_redo`

Gets the undo/redo object. Most actions in the editor can be undoable, so use this object to make sure this happens when it's worth it.

### HideBottomPanel

```csharp
public void HideBottomPanel()
```

引擎名：`hide_bottom_panel`

Minimizes the bottom panel.

### MakeBottomPanelItemVisible

```csharp
public void MakeBottomPanelItemVisible(Control item)
```

引擎名：`make_bottom_panel_item_visible`

Makes a specific item in the bottom panel visible.

### QueueSaveLayout

```csharp
public void QueueSaveLayout()
```

引擎名：`queue_save_layout`

Queue save the project's editor layout.

### RemoveAutoloadSingleton

```csharp
public void RemoveAutoloadSingleton(string name)
```

引擎名：`remove_autoload_singleton`

Removes an Autoload `name` from the list.

### RemoveContextMenuPlugin

```csharp
public void RemoveContextMenuPlugin(EditorContextMenuPlugin plugin)
```

引擎名：`remove_context_menu_plugin`

Removes the specified context menu plugin.

### RemoveControlFromBottomPanel

```csharp
public void RemoveControlFromBottomPanel(Control control)
```

引擎名：`remove_control_from_bottom_panel`

Removes the control from the bottom panel. You have to manually [`Node.QueueFree`](Node.md) the control.

### RemoveControlFromContainer

```csharp
public void RemoveControlFromContainer(EditorPlugin.CustomControlContainer container, Control control)
```

引擎名：`remove_control_from_container`

Removes the control from the specified container. You have to manually [`Node.QueueFree`](Node.md) the control.

### RemoveControlFromDocks

```csharp
public void RemoveControlFromDocks(Control control)
```

引擎名：`remove_control_from_docks`

Removes the control from the dock. You have to manually [`Node.QueueFree`](Node.md) the control.

### RemoveCustomType

```csharp
public void RemoveCustomType(string type)
```

引擎名：`remove_custom_type`

Removes a custom type added by `AddCustomType()`.

### RemoveDebuggerPlugin

```csharp
public void RemoveDebuggerPlugin(EditorDebuggerPlugin script)
```

引擎名：`remove_debugger_plugin`

Removes the debugger plugin with given script from the Debugger.

### RemoveDock

```csharp
public void RemoveDock(EditorDock dock)
```

引擎名：`remove_dock`

Removes `dock` from the available docks. You should manually call [`Node.QueueFree`](Node.md) to free it.

### RemoveExportPlatform

```csharp
public void RemoveExportPlatform(EditorExportPlatform platform)
```

引擎名：`remove_export_platform`

Removes an export platform registered by `AddExportPlatform()`.

### RemoveExportPlugin

```csharp
public void RemoveExportPlugin(EditorExportPlugin plugin)
```

引擎名：`remove_export_plugin`

Removes an export plugin registered by `AddExportPlugin()`.

### RemoveImportPlugin

```csharp
public void RemoveImportPlugin(EditorImportPlugin importer)
```

引擎名：`remove_import_plugin`

Removes an import plugin registered by `AddImportPlugin()`.

### RemoveInspectorPlugin

```csharp
public void RemoveInspectorPlugin(EditorInspectorPlugin plugin)
```

引擎名：`remove_inspector_plugin`

Removes an inspector plugin registered by `AddInspectorPlugin()`.

### RemoveNode3DGizmoPlugin

```csharp
public void RemoveNode3DGizmoPlugin(EditorNode3DGizmoPlugin plugin)
```

Removes a gizmo plugin registered by `AddNode3DGizmoPlugin()`.

### RemoveResourceConversionPlugin

```csharp
public void RemoveResourceConversionPlugin(EditorResourceConversionPlugin plugin)
```

引擎名：`remove_resource_conversion_plugin`

Removes a resource conversion plugin registered by `AddResourceConversionPlugin()`.

### RemoveSceneFormatImporterPlugin

```csharp
public void RemoveSceneFormatImporterPlugin(EditorSceneFormatImporter sceneFormatImporter)
```

引擎名：`remove_scene_format_importer_plugin`

Removes a scene format importer registered by `AddSceneFormatImporterPlugin()`.

### RemoveScenePostImportPlugin

```csharp
public void RemoveScenePostImportPlugin(EditorScenePostImportPlugin sceneImportPlugin)
```

引擎名：`remove_scene_post_import_plugin`

Remove the [`EditorScenePostImportPlugin`](EditorScenePostImportPlugin.md), added with `AddScenePostImportPlugin()`.

### RemoveToolMenuItem

```csharp
public void RemoveToolMenuItem(string name)
```

引擎名：`remove_tool_menu_item`

Removes a menu `name` from **Project > Tools**.

### RemoveTranslationParserPlugin

```csharp
public void RemoveTranslationParserPlugin(EditorTranslationParserPlugin parser)
```

引擎名：`remove_translation_parser_plugin`

Removes a custom translation parser plugin registered by `AddTranslationParserPlugin()`.

### RemoveUndoRedoInspectorHookCallback

```csharp
public void RemoveUndoRedoInspectorHookCallback(Callable callable)
```

引擎名：`remove_undo_redo_inspector_hook_callback`

Removes a callback previously added by `AddUndoRedoInspectorHookCallback()`.

### SetDockTabIcon

```csharp
public void SetDockTabIcon(Control control, Texture2D icon)
```

引擎名：`set_dock_tab_icon`

Sets the tab icon for the given control in a dock slot. Setting to `null` removes the icon.

### SetForceDrawOverForwardingEnabled

```csharp
public void SetForceDrawOverForwardingEnabled()
```

引擎名：`set_force_draw_over_forwarding_enabled`

Enables calling of `_ForwardCanvasForceDrawOverViewport()` for the 2D editor and `_Forward3DForceDrawOverViewport()` for the 3D editor when their viewports are updated. You need to call this method only once and it will work permanently for this plugin.

### SetInputEventForwardingAlwaysEnabled

```csharp
public void SetInputEventForwardingAlwaysEnabled()
```

引擎名：`set_input_event_forwarding_always_enabled`

Use this method if you always want to receive inputs from 3D view screen inside `_Forward3DGuiInput()`. It might be especially usable if your plugin will want to use raycast in the scene.

### UpdateOverlays

```csharp
public int UpdateOverlays()
```

引擎名：`update_overlays`

Updates the overlays of the 2D and 3D editor viewport. Causes methods `_ForwardCanvasDrawOverViewport()`, `_ForwardCanvasForceDrawOverViewport()`, `_Forward3DDrawOverViewport()` and `_Forward3DForceDrawOverViewport()` to be called.

### _ApplyChanges

```csharp
public virtual void _ApplyChanges()
```

引擎名：`_apply_changes`

This method is called when the editor is about to save the project, switch to another tab, etc. It asks the plugin to apply any pending state changes to ensure consistency.

This is used, for example, in shader editors to let the plugin know that it must apply the shader code being written by the user to the object.

### _Build

```csharp
public virtual bool _Build()
```

引擎名：`_build`

This method is called when the editor is about to run the project. The plugin can then perform required operations before the project runs.

This method must return a boolean. If this method returns `false`, the project will not run. The run is aborted immediately, so this also prevents all other plugins' `_Build()` methods from running.

### _Clear

```csharp
public virtual void _Clear()
```

引擎名：`_clear`

Clear all the state and reset the object being edited to zero. This ensures your plugin does not keep editing a currently existing node, or a node from the wrong scene.

### _DisablePlugin

```csharp
public virtual void _DisablePlugin()
```

引擎名：`_disable_plugin`

Called by the engine when the user disables the `EditorPlugin` in the Plugin tab of the project settings window.

### _Edit

```csharp
public virtual void _Edit(GodotObject object)
```

引擎名：`_edit`

This function is used for plugins that edit specific object types (nodes or resources). It requests the editor to edit the given object.

`object` can be `null` if the plugin was editing an object, but there is no longer any selected object handled by this plugin. It can be used to cleanup editing state.

### _EnablePlugin

```csharp
public virtual void _EnablePlugin()
```

引擎名：`_enable_plugin`

Called by the engine when the user enables the `EditorPlugin` in the Plugin tab of the project settings window.

### _Forward3DDrawOverViewport

```csharp
public virtual void _Forward3DDrawOverViewport(Control viewportControl)
```

Called by the engine when the 3D editor's viewport is updated. `viewportControl` is an overlay on top of the viewport and it can be used for drawing. You can update the viewport manually by calling `UpdateOverlays()`.

```csharp
public override void _Forward3DDrawOverViewport(Control viewportControl)
{
// Draw a circle at the cursor's position.
viewportControl.DrawCircle(viewportControl.GetLocalMousePosition(), 64, Colors.White);
}

public override EditorPlugin.AfterGuiInput _Forward3DGuiInput(Camera3D viewportCamera, InputEvent @event)
{
if (@event is InputEventMouseMotion)
{
// Redraw the viewport when the cursor is moved.
UpdateOverlays();
return EditorPlugin.AfterGuiInput.Stop;
}
return EditorPlugin.AfterGuiInput.Pass;
}

```

### _Forward3DForceDrawOverViewport

```csharp
public virtual void _Forward3DForceDrawOverViewport(Control viewportControl)
```

This method is the same as `_Forward3DDrawOverViewport()`, except it draws on top of everything. Useful when you need an extra layer that shows over anything else.

You need to enable calling of this method by using `SetForceDrawOverForwardingEnabled()`.

### _Forward3DGuiInput

```csharp
public virtual int _Forward3DGuiInput(Camera3D viewportCamera, InputEvent event)
```

Called when there is a root node in the current edited scene, `_Handles()` is implemented, and an [`InputEvent`](InputEvent.md) happens in the 3D viewport. The return value decides whether the [`InputEvent`](InputEvent.md) is consumed or forwarded to other `EditorPlugin`s. See [`EditorPlugin.AfterGuiInput`](EditorPlugin.md) for options.

```csharp
// Prevents the InputEvent from reaching other Editor classes.
public override EditorPlugin.AfterGuiInput _Forward3DGuiInput(Camera3D camera, InputEvent @event)
{
return EditorPlugin.AfterGuiInput.Stop;
}

```

This method must return [`EditorPlugin.AfterGuiInput.Pass`](EditorPlugin.md) in order to forward the [`InputEvent`](InputEvent.md) to other Editor classes.

```csharp
// Consumes InputEventMouseMotion and forwards other InputEvent types.
public override EditorPlugin.AfterGuiInput _Forward3DGuiInput(Camera3D camera, InputEvent @event)
{
return @event is InputEventMouseMotion ? EditorPlugin.AfterGuiInput.Stop : EditorPlugin.AfterGuiInput.Pass;
}

```

### _ForwardCanvasDrawOverViewport

```csharp
public virtual void _ForwardCanvasDrawOverViewport(Control viewportControl)
```

引擎名：`_forward_canvas_draw_over_viewport`

Called by the engine when the 2D editor's viewport is updated. `viewportControl` is an overlay on top of the viewport and it can be used for drawing. You can update the viewport manually by calling `UpdateOverlays()`.

```csharp
public override void _ForwardCanvasDrawOverViewport(Control viewportControl)
{
// Draw a circle at the cursor's position.
viewportControl.DrawCircle(viewportControl.GetLocalMousePosition(), 64, Colors.White);
}

public override bool _ForwardCanvasGuiInput(InputEvent @event)
{
if (@event is InputEventMouseMotion)
{
// Redraw the viewport when the cursor is moved.
UpdateOverlays();
return true;
}
return false;
}

```

### _ForwardCanvasForceDrawOverViewport

```csharp
public virtual void _ForwardCanvasForceDrawOverViewport(Control viewportControl)
```

引擎名：`_forward_canvas_force_draw_over_viewport`

This method is the same as `_ForwardCanvasDrawOverViewport()`, except it draws on top of everything. Useful when you need an extra layer that shows over anything else.

You need to enable calling of this method by using `SetForceDrawOverForwardingEnabled()`.

### _ForwardCanvasGuiInput

```csharp
public virtual bool _ForwardCanvasGuiInput(InputEvent event)
```

引擎名：`_forward_canvas_gui_input`

Called when there is a root node in the current edited scene, `_Handles()` is implemented, and an [`InputEvent`](InputEvent.md) happens in the 2D viewport. If this method returns `true`, `event` is intercepted by this `EditorPlugin`, otherwise `event` is forwarded to other Editor classes.

```csharp
// Prevents the InputEvent from reaching other Editor classes.
public override bool ForwardCanvasGuiInput(InputEvent @event)
{
return true;
}

```

This method must return `false` in order to forward the [`InputEvent`](InputEvent.md) to other Editor classes.

```csharp
// Consumes InputEventMouseMotion and forwards other InputEvent types.
public override bool _ForwardCanvasGuiInput(InputEvent @event)
{
if (@event is InputEventMouseMotion)
{
return true;
}
return false;
}

```

### _GetBreakpoints

```csharp
public virtual string[] _GetBreakpoints()
```

引擎名：`_get_breakpoints`

This is for editors that edit script-based objects. You can return a list of breakpoints in the format (`script:line`), for example: `res://path_to_script.gd:25`.

### _GetPluginIcon

```csharp
public virtual Texture2D _GetPluginIcon()
```

引擎名：`_get_plugin_icon`

Override this method in your plugin to return a [`Texture2D`](Texture2D.md) in order to give it an icon.

For main screen plugins, this appears at the top of the screen, to the right of the "2D", "3D", "Script", "Game", and "Asset Store" buttons.

Ideally, the plugin icon should be white with a transparent background and 16×16 pixels in size.

```csharp
public override Texture2D _GetPluginIcon()
{
// You can use a custom icon:
return ResourceLoader.Load<Texture2D>("res://addons/my_plugin/my_plugin_icon.svg");
// Or use a built-in icon:
return EditorInterface.Singleton.GetEditorTheme().GetIcon("Node", "EditorIcons");
}

```

### _GetPluginName

```csharp
public virtual string _GetPluginName()
```

引擎名：`_get_plugin_name`

Override this method in your plugin to provide the name of the plugin when displayed in the Godot editor.

For main screen plugins, this appears at the top of the screen, to the right of the "2D", "3D", "Script", "Game", and "Asset Store" buttons.

### _GetState

```csharp
public virtual Dictionary _GetState()
```

引擎名：`_get_state`

Override this method to provide a state data you want to be saved, like view position, grid settings, folding, etc. This is used when saving the scene (so state is kept when opening it again) and for switching tabs (so state can be restored when the tab returns). This data is automatically saved for each scene in an `editstate` file in the editor metadata folder. If you want to store global (scene-independent) editor data for your plugin, you can use `_GetWindowLayout()` instead.

Use `_SetState()` to restore your saved state.

**Note:** This method should not be used to save important settings that should persist with the project.

**Note:** You must implement `_GetPluginName()` for the state to be stored and restored correctly.

```csharp
func _get_state():
var state = { "zoom": zoom, "preferred_color": my_color }
return state

```

### _GetUnsavedStatus

```csharp
public virtual string _GetUnsavedStatus(string forScene)
```

引擎名：`_get_unsaved_status`

Override this method to provide a custom message that lists unsaved changes. The editor will call this method when exiting or when closing a scene, and display the returned string in a confirmation dialog. Return empty string if the plugin has no unsaved changes.

When closing a scene, `forScene` is the path to the scene being closed. You can use it to handle built-in resources in that scene.

If the user confirms saving, `_SaveExternalData()` will be called, before closing the editor.

```csharp
func _get_unsaved_status(for_scene):
if not unsaved:
return ""

if for_scene.is_empty():
return "Save changes in MyCustomPlugin before closing?"
else:
return "Scene %s has changes from MyCustomPlugin. Save before closing?" % for_scene.get_file()

func _save_external_data():
unsaved = false

```

If the plugin has no scene-specific changes, you can ignore the calls when closing scenes:

```csharp
func _get_unsaved_status(for_scene):
if not for_scene.is_empty():
return ""

```

### _GetWindowLayout

```csharp
public virtual void _GetWindowLayout(ConfigFile configuration)
```

引擎名：`_get_window_layout`

Override this method to provide the GUI layout of the plugin or any other data you want to be stored. This is used to save the project's editor layout when `QueueSaveLayout()` is called or the editor layout was changed (for example changing the position of a dock). The data is stored in the `editor_layout.cfg` file in the editor metadata directory.

Use `_SetWindowLayout()` to restore your saved layout.

```csharp
func _get_window_layout(configuration):
configuration.set_value("MyPlugin", "window_position", $Window.position)
configuration.set_value("MyPlugin", "icon_color", $Icon.modulate)

```

### _Handles

```csharp
public virtual bool _Handles(GodotObject object)
```

引擎名：`_handles`

Implement this function if your plugin edits a specific type of object (Resource or Node). If you return `true`, then you will get the functions `_Edit()` and `_MakeVisible()` called when the editor requests them. If you have declared the methods `_ForwardCanvasGuiInput()` and `_Forward3DGuiInput()` these will be called too.

**Note:** Each plugin should handle only one type of objects at a time. If a plugin handles more types of objects and they are edited at the same time, it will result in errors.

### _HasMainScreen

```csharp
public virtual bool _HasMainScreen()
```

引擎名：`_has_main_screen`

Returns `true` if this is a main screen editor plugin (it goes in the workspace selector together with **2D**, **3D**, **Script**, **Game**, and **Asset Store**).

When the plugin's workspace is selected, other main screen plugins will be hidden, but your plugin will not appear automatically. It needs to be added as a child of [`EditorInterface.GetEditorMainScreen`](EditorInterface.md) and made visible inside `_MakeVisible()`.

Use `_GetPluginName()` and `_GetPluginIcon()` to customize the plugin button's appearance.

```csharp
var plugin_control

func _enter_tree():
plugin_control = preload("my_plugin_control.tscn").instantiate()
EditorInterface.get_editor_main_screen().add_child(plugin_control)
plugin_control.hide()

func _has_main_screen():
return true

func _make_visible(visible):
plugin_control.visible = visible

func _get_plugin_name():
return "My Super Cool Plugin 3000"

func _get_plugin_icon():
return EditorInterface.get_editor_theme().get_icon("Node", "EditorIcons")

```

### _MakeVisible

```csharp
public virtual void _MakeVisible(bool visible)
```

引擎名：`_make_visible`

This function will be called when the editor is requested to become visible. It is used for plugins that edit a specific object type.

Remember that you have to manage the visibility of all your editor controls manually.

### _RunScene

```csharp
public virtual string[] _RunScene(string scene, string[] args)
```

引擎名：`_run_scene`

This function is called when an individual scene is about to be played in the editor. `args` is a list of command line arguments that will be passed to the new Godot instance, which will be replaced by the list returned by this function.

```csharp
func _run_scene(scene, args):
args.append("--an-extra-argument")
return args

```

**Note:** Text that is printed in this method will not be visible in the editor's Output panel unless `EditorSettings.run/output/always_clear_output_on_play` is `false`.

### _SaveExternalData

```csharp
public virtual void _SaveExternalData()
```

引擎名：`_save_external_data`

This method is called after the editor saves the project or when it's closed. It asks the plugin to save edited external scenes/resources.

### _SetState

```csharp
public virtual void _SetState(Dictionary state)
```

引擎名：`_set_state`

Restore the state saved by `_GetState()`. This method is called when the current scene tab is changed in the editor.

**Note:** Your plugin must implement `_GetPluginName()`, otherwise it will not be recognized and this method will not be called.

```csharp
func _set_state(data):
zoom = data.get("zoom", 1.0)
preferred_color = data.get("my_color", Color.WHITE)

```

### _SetWindowLayout

```csharp
public virtual void _SetWindowLayout(ConfigFile configuration)
```

引擎名：`_set_window_layout`

Restore the plugin GUI layout and data saved by `_GetWindowLayout()`. This method is called for every plugin on editor startup. Use the provided `configuration` file to read your saved data.

```csharp
func _set_window_layout(configuration):
$Window.position = configuration.get_value("MyPlugin", "window_position", Vector2())
$Icon.modulate = configuration.get_value("MyPlugin", "icon_color", Color.WHITE)

```

## 事件（信号）

### MainScreenChanged

```csharp
public event EditorPlugin.MainScreenChangedEventHandler MainScreenChanged
```

引擎名：`main_screen_changed`

Emitted when user changes the workspace (**2D**, **3D**, **Script**, **Game**, **Asset Store**). Also works with custom screens defined by plugins.

### ProjectSettingsChanged

```csharp
public event Action ProjectSettingsChanged
```

引擎名：`project_settings_changed`

Emitted when any project setting has changed.

### ResourceSaved

```csharp
public event EditorPlugin.ResourceSavedEventHandler ResourceSaved
```

引擎名：`resource_saved`

Emitted when the given `resource` was saved on disc. See also `SceneSaved`.

### SceneChanged

```csharp
public event EditorPlugin.SceneChangedEventHandler SceneChanged
```

引擎名：`scene_changed`

Emitted when the scene is changed in the editor. The argument will return the root node of the scene that has just become active. If this scene is new and empty, the argument will be `null`.

### SceneClosed

```csharp
public event EditorPlugin.SceneClosedEventHandler SceneClosed
```

引擎名：`scene_closed`

Emitted when user closes a scene. The argument is a file path to the closed scene.

### SceneSaved

```csharp
public event EditorPlugin.SceneSavedEventHandler SceneSaved
```

引擎名：`scene_saved`

Emitted when a scene was saved on disc. The argument is a file path to the saved scene. See also `ResourceSaved`.

## AfterGuiInput

命名空间 `Godot`；enum；仅编辑器（GodotSharpEditor）

### 枚举值

- `Pass = 0`：Forwards the [`InputEvent`](InputEvent.md) to other EditorPlugins.
- `Stop = 1`：Prevents the [`InputEvent`](InputEvent.md) from reaching other Editor classes.
- `Custom = 2`：Pass the [`InputEvent`](InputEvent.md) to other editor plugins except the main [`Node3D`](Node3D.md) one.

## CustomControlContainer

命名空间 `Godot`；enum；仅编辑器（GodotSharpEditor）

### 枚举值

- `Toolbar = 0`：Main editor toolbar, next to play buttons.
- `SpatialEditorMenu = 1`：The toolbar that appears when 3D editor is active.
- `SpatialEditorSideLeft = 2`：Left sidebar of the 3D editor.
- `SpatialEditorSideRight = 3`：Right sidebar of the 3D editor.
- `SpatialEditorBottom = 4`：Bottom panel of the 3D editor.
- `CanvasEditorMenu = 5`：The toolbar that appears when 2D editor is active.
- `CanvasEditorSideLeft = 6`：Left sidebar of the 2D editor.
- `CanvasEditorSideRight = 7`：Right sidebar of the 2D editor.
- `CanvasEditorBottom = 8`：Bottom panel of the 2D editor.
- `InspectorBottom = 9`：Bottom section of the inspector.
- `ProjectSettingTabLeft = 10`：Tab of Project Settings dialog, to the left of other tabs.
- `ProjectSettingTabRight = 11`：Tab of Project Settings dialog, to the right of other tabs.

## DockSlot

命名空间 `Godot`；enum；仅编辑器（GodotSharpEditor）

### 枚举值

- `None = -1`：The dock is closed.
- `LeftUl = 0`：Dock slot, left side, upper-left (empty in default layout).
- `LeftBl = 1`：Dock slot, left side, bottom-left (empty in default layout).
- `LeftUr = 2`：Dock slot, left side, upper-right (in default layout includes Scene and Import docks).
- `LeftBr = 3`：Dock slot, left side, bottom-right (in default layout includes FileSystem dock).
- `RightUl = 4`：Dock slot, right side, upper-left (in default layout includes Inspector, Node, and History docks).
- `RightBl = 5`：Dock slot, right side, bottom-left (empty in default layout).
- `RightUr = 6`：Dock slot, right side, upper-right (empty in default layout).
- `RightBr = 7`：Dock slot, right side, bottom-right (empty in default layout).
- `Bottom = 8`：Bottom panel.
- `Max = 9`：Represents the size of the `EditorPlugin.DockSlot` enum.

## MainScreenChangedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorPlugin.MainScreenChanged`](EditorPlugin.md) event of a [`EditorPlugin`](EditorPlugin.md) class.

### 签名

#### Invoke

```csharp
delegate void MainScreenChangedEventHandler(string screenName)
```

## ResourceSavedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorPlugin.ResourceSaved`](EditorPlugin.md) event of a [`EditorPlugin`](EditorPlugin.md) class.

### 签名

#### Invoke

```csharp
delegate void ResourceSavedEventHandler(Resource resource)
```

## SceneChangedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorPlugin.SceneChanged`](EditorPlugin.md) event of a [`EditorPlugin`](EditorPlugin.md) class.

### 签名

#### Invoke

```csharp
delegate void SceneChangedEventHandler(Node sceneRoot)
```

## SceneClosedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorPlugin.SceneClosed`](EditorPlugin.md) event of a [`EditorPlugin`](EditorPlugin.md) class.

### 签名

#### Invoke

```csharp
delegate void SceneClosedEventHandler(string filepath)
```

## SceneSavedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`EditorPlugin.SceneSaved`](EditorPlugin.md) event of a [`EditorPlugin`](EditorPlugin.md) class.

### 签名

#### Invoke

```csharp
delegate void SceneSavedEventHandler(string filepath)
```
