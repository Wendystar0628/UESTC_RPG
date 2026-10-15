<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ScriptEditor

命名空间 `Godot`；class；继承 [`PanelContainer`](PanelContainer.md) → [`Container`](Container.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

Godot editor's script editor.

**Note:** This class shouldn't be instantiated directly. Instead, access the singleton using [`EditorInterface.GetScriptEditor`](EditorInterface.md).

## 方法

### ClearDocsFromScript

```csharp
public void ClearDocsFromScript(Script script)
```

引擎名：`clear_docs_from_script`

Removes the documentation for the given `script`.

**Note:** This should be called whenever the script is changed to keep the open documentation state up to date.

### CloseFile

```csharp
public Error CloseFile(string path)
```

引擎名：`close_file`

Closes the file at the given `path`, discarding any unsaved changes.

Returns [`Error.Ok`](Error.md) on success or [`Error.FileNotFound`](Error.md) if the file is not found.

### GetBreakpoints

```csharp
public string[] GetBreakpoints()
```

引擎名：`get_breakpoints`

Returns array of breakpoints.

### GetCurrentEditor

```csharp
public ScriptEditorBase GetCurrentEditor()
```

引擎名：`get_current_editor`

Returns the [`ScriptEditorBase`](ScriptEditorBase.md) object that the user is currently editing.

### GetCurrentScript

```csharp
public Script GetCurrentScript()
```

引擎名：`get_current_script`

Returns a [`Script`](Script.md) that is currently active in editor.

### GetOpenScriptEditors

```csharp
public Array<ScriptEditorBase> GetOpenScriptEditors()
```

引擎名：`get_open_script_editors`

Returns an array with all [`ScriptEditorBase`](ScriptEditorBase.md) objects which are currently open in editor.

### GetOpenScripts

```csharp
public Array<Script> GetOpenScripts()
```

引擎名：`get_open_scripts`

Returns an array with all [`Script`](Script.md) objects which are currently open in editor.

### GetUnsavedFiles

```csharp
public string[] GetUnsavedFiles()
```

引擎名：`get_unsaved_files`

Returns an array of file paths of scripts with unsaved changes open in the editor.

### GotoHelp

```csharp
public void GotoHelp(string topic)
```

引擎名：`goto_help`

Opens help for the given topic. The `topic` is an encoded string that controls which class, method, constant, signal, annotation, property, or theme item should be focused.

The supported `topic` formats include `class_name:class`, `class_method:class:method`, `class_constant:class:constant`, `class_signal:class:signal`, `class_annotation:class:@annotation`, `class_property:class:property`, and `class_theme_item:class:item`, where `class` is the class name, `method` is the method name, `constant` is the constant name, `signal` is the signal name, `annotation` is the annotation name, `property` is the property name, and `item` is the theme item.

```csharp
# Shows help for the Node class.
class_name:Node
# Shows help for the global min function.
# Global objects are accessible in the `@GlobalScope` namespace, shown here.
class_method:@GlobalScope:min
# Shows help for get_viewport in the Node class.
class_method:Node:get_viewport
# Shows help for the Input constant MOUSE_BUTTON_MIDDLE.
class_constant:Input:MOUSE_BUTTON_MIDDLE
# Shows help for the BaseButton signal pressed.
class_signal:BaseButton:pressed
# Shows help for the CanvasItem property visible.
class_property:CanvasItem:visible
# Shows help for the GDScript annotation export.
# Annotations should be prefixed with the `@` symbol in the descriptor, as shown here.
class_annotation:@GDScript:@export
# Shows help for the GraphNode theme item named panel_selected.
class_theme_item:GraphNode:panel_selected

```

### GotoLine

```csharp
public void GotoLine(int lineNumber)
```

引擎名：`goto_line`

Goes to the specified line in the current script.

### OpenScriptCreateDialog

```csharp
public void OpenScriptCreateDialog(string baseName, string basePath)
```

引擎名：`open_script_create_dialog`

Opens the script create dialog. The script will extend `baseName`. The file extension can be omitted from `basePath`. It will be added based on the selected scripting language.

### RegisterSyntaxHighlighter

```csharp
public void RegisterSyntaxHighlighter(EditorSyntaxHighlighter syntaxHighlighter)
```

引擎名：`register_syntax_highlighter`

Registers the [`EditorSyntaxHighlighter`](EditorSyntaxHighlighter.md) to the editor, the [`EditorSyntaxHighlighter`](EditorSyntaxHighlighter.md) will be available on all open scripts.

**Note:** Does not apply to scripts that are already opened.

### ReloadOpenFiles

```csharp
public void ReloadOpenFiles()
```

引擎名：`reload_open_files`

Reloads all currently opened files. This should be used when opened files are changed outside of the script editor. The user may be prompted to resolve file conflicts, see `EditorSettings.text_editor/behavior/files/auto_reload_scripts_on_external_change`.

### SaveAllScripts

```csharp
public void SaveAllScripts()
```

引擎名：`save_all_scripts`

Saves all open scripts.

### UnregisterSyntaxHighlighter

```csharp
public void UnregisterSyntaxHighlighter(EditorSyntaxHighlighter syntaxHighlighter)
```

引擎名：`unregister_syntax_highlighter`

Unregisters the [`EditorSyntaxHighlighter`](EditorSyntaxHighlighter.md) from the editor.

**Note:** The [`EditorSyntaxHighlighter`](EditorSyntaxHighlighter.md) will still be applied to scripts that are already opened.

### UpdateDocsFromScript

```csharp
public void UpdateDocsFromScript(Script script)
```

引擎名：`update_docs_from_script`

Updates the documentation for the given `script`.

**Note:** This should be called whenever the script is changed to keep the open documentation state up to date.

## 事件（信号）

### EditorScriptChanged

```csharp
public event ScriptEditor.EditorScriptChangedEventHandler EditorScriptChanged
```

引擎名：`editor_script_changed`

Emitted when user changed active script. Argument is a freshly activated [`Script`](Script.md).

### ScriptClose

```csharp
public event ScriptEditor.ScriptCloseEventHandler ScriptClose
```

引擎名：`script_close`

Emitted when editor is about to close the active script. Argument is a [`Script`](Script.md) that is going to be closed.

## EditorScriptChangedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`ScriptEditor.EditorScriptChanged`](ScriptEditor.md) event of a [`ScriptEditor`](ScriptEditor.md) class.

### 签名

#### Invoke

```csharp
delegate void EditorScriptChangedEventHandler(Script script)
```

## ScriptCloseEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`ScriptEditor.ScriptClose`](ScriptEditor.md) event of a [`ScriptEditor`](ScriptEditor.md) class.

### 签名

#### Invoke

```csharp
delegate void ScriptCloseEventHandler(Script script)
```
