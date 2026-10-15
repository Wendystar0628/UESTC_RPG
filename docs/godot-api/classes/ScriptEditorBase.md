<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ScriptEditorBase

命名空间 `Godot`；class；继承 [`VBoxContainer`](VBoxContainer.md) → [`BoxContainer`](BoxContainer.md) → [`Container`](Container.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

Base editor for editing scripts in the [`ScriptEditor`](ScriptEditor.md). This does not include documentation items.

## 方法

### AddSyntaxHighlighter

```csharp
public void AddSyntaxHighlighter(EditorSyntaxHighlighter highlighter)
```

引擎名：`add_syntax_highlighter`

Adds an [`EditorSyntaxHighlighter`](EditorSyntaxHighlighter.md) to the open script.

### GetBaseEditor

```csharp
public Control GetBaseEditor()
```

引擎名：`get_base_editor`

Returns the underlying [`Control`](Control.md) used for editing scripts. For text scripts, this is a [`CodeEdit`](CodeEdit.md).

## 事件（信号）

### EditedScriptChanged

```csharp
public event Action EditedScriptChanged
```

引擎名：`edited_script_changed`

Emitted after script validation.

### GoToHelp

```csharp
public event ScriptEditorBase.GoToHelpEventHandler GoToHelp
```

引擎名：`go_to_help`

Emitted when the user requests a specific documentation page.

### GoToMethod

```csharp
public event ScriptEditorBase.GoToMethodEventHandler GoToMethod
```

引擎名：`go_to_method`

Emitted when the user requests to view a specific method of a script, similar to `RequestOpenScriptAtLine`.

### NameChanged

```csharp
public event Action NameChanged
```

引擎名：`name_changed`

Emitted after script validation or when the edited resource has changed.

### ReplaceInFilesRequested

```csharp
public event ScriptEditorBase.ReplaceInFilesRequestedEventHandler ReplaceInFilesRequested
```

引擎名：`replace_in_files_requested`

Emitted when the user request to find and replace text in the file system.

### RequestHelp

```csharp
public event ScriptEditorBase.RequestHelpEventHandler RequestHelp
```

引擎名：`request_help`

Emitted when the user requests contextual help.

### RequestOpenScriptAtLine

```csharp
public event ScriptEditorBase.RequestOpenScriptAtLineEventHandler RequestOpenScriptAtLine
```

引擎名：`request_open_script_at_line`

Emitted when the user requests to view a specific line of a script, similar to `GoToMethod`.

### RequestSaveHistory

```csharp
public event Action RequestSaveHistory
```

引擎名：`request_save_history`

Emitted when the user contextual goto and the item is in the same script.

### RequestSavePreviousState

```csharp
public event ScriptEditorBase.RequestSavePreviousStateEventHandler RequestSavePreviousState
```

引擎名：`request_save_previous_state`

Emitted when the user changes current script or moves caret by 10 or more columns within the same script.

### SearchInFilesRequested

```csharp
public event ScriptEditorBase.SearchInFilesRequestedEventHandler SearchInFilesRequested
```

引擎名：`search_in_files_requested`

Emitted when the user request to search text in the file system.

## GoToHelpEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`ScriptEditorBase.GoToHelp`](ScriptEditorBase.md) event of a [`ScriptEditorBase`](ScriptEditorBase.md) class.

### 签名

#### Invoke

```csharp
delegate void GoToHelpEventHandler(string what)
```

## GoToMethodEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`ScriptEditorBase.GoToMethod`](ScriptEditorBase.md) event of a [`ScriptEditorBase`](ScriptEditorBase.md) class.

### 签名

#### Invoke

```csharp
delegate void GoToMethodEventHandler(GodotObject script, string method)
```

## ReplaceInFilesRequestedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`ScriptEditorBase.ReplaceInFilesRequested`](ScriptEditorBase.md) event of a [`ScriptEditorBase`](ScriptEditorBase.md) class.

### 签名

#### Invoke

```csharp
delegate void ReplaceInFilesRequestedEventHandler(string text)
```

## RequestHelpEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`ScriptEditorBase.RequestHelp`](ScriptEditorBase.md) event of a [`ScriptEditorBase`](ScriptEditorBase.md) class.

### 签名

#### Invoke

```csharp
delegate void RequestHelpEventHandler(string topic)
```

## RequestOpenScriptAtLineEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`ScriptEditorBase.RequestOpenScriptAtLine`](ScriptEditorBase.md) event of a [`ScriptEditorBase`](ScriptEditorBase.md) class.

### 签名

#### Invoke

```csharp
delegate void RequestOpenScriptAtLineEventHandler(GodotObject script, long line)
```

## RequestSavePreviousStateEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`ScriptEditorBase.RequestSavePreviousState`](ScriptEditorBase.md) event of a [`ScriptEditorBase`](ScriptEditorBase.md) class.

### 签名

#### Invoke

```csharp
delegate void RequestSavePreviousStateEventHandler(Dictionary state)
```

## SearchInFilesRequestedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`ScriptEditorBase.SearchInFilesRequested`](ScriptEditorBase.md) event of a [`ScriptEditorBase`](ScriptEditorBase.md) class.

### 签名

#### Invoke

```csharp
delegate void SearchInFilesRequestedEventHandler(string text)
```
