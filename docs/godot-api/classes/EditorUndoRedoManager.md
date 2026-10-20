<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorUndoRedoManager

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

`EditorUndoRedoManager` is a manager for [`UndoRedo`](UndoRedo.md) objects associated with edited scenes. Each scene has its own undo history and `EditorUndoRedoManager` ensures that each action performed in the editor gets associated with a proper scene. For actions not related to scenes ([`ProjectSettings`](ProjectSettings.md) edits, external resources, etc.), a separate global history is used.

The usage is mostly the same as [`UndoRedo`](UndoRedo.md). You create and commit actions and the manager automatically decides under-the-hood what scenes it belongs to. The scene is deduced based on the first operation in an action, using the object from the operation. The rules are as follows:

- If the object is a [`Node`](Node.md), use the currently edited scene;

- If the object is a built-in resource, use the scene from its path;

- If the object is external resource or anything else, use global history.

This guessing can sometimes yield false results, so you can provide a custom context object when creating an action.

`EditorUndoRedoManager` is intended to be used by Godot editor plugins. You can obtain it using [`EditorPlugin.GetUndoRedo`](EditorPlugin.md). For non-editor uses or plugins that don't need to integrate with the editor's undo history, use [`UndoRedo`](UndoRedo.md) instead.

The manager's API is mostly the same as in [`UndoRedo`](UndoRedo.md), so you can refer to its documentation for more examples. The main difference is that `EditorUndoRedoManager` uses object + method name for actions, instead of [`Callable`](Callable.md).

## 方法

### AddDoMethod

```csharp
public void AddDoMethod(GodotObject object, StringName method, ReadOnlySpan<Variant> args)
```

引擎名：`add_do_method`

Register a method that will be called when the action is committed (i.e. the "do" action).

If this is the first operation, the `object` will be used to deduce target undo history.

### AddDoMethod

```csharp
public void AddDoMethod(GodotObject object, StringName method, params Variant[] args)
```

引擎名：`add_do_method`

Register a method that will be called when the action is committed (i.e. the "do" action).

If this is the first operation, the `object` will be used to deduce target undo history.

### AddDoProperty

```csharp
public void AddDoProperty(GodotObject object, StringName property, Variant value)
```

引擎名：`add_do_property`

Register a property value change for "do".

If this is the first operation, the `object` will be used to deduce target undo history.

### AddDoReference

```csharp
public void AddDoReference(GodotObject object)
```

引擎名：`add_do_reference`

Register a reference for "do" that will be erased if the "do" history is lost. This is useful mostly for new nodes created for the "do" call. Do not use for resources.

### AddUndoMethod

```csharp
public void AddUndoMethod(GodotObject object, StringName method, ReadOnlySpan<Variant> args)
```

引擎名：`add_undo_method`

Register a method that will be called when the action is undone (i.e. the "undo" action).

If this is the first operation, the `object` will be used to deduce target undo history.

### AddUndoMethod

```csharp
public void AddUndoMethod(GodotObject object, StringName method, params Variant[] args)
```

引擎名：`add_undo_method`

Register a method that will be called when the action is undone (i.e. the "undo" action).

If this is the first operation, the `object` will be used to deduce target undo history.

### AddUndoProperty

```csharp
public void AddUndoProperty(GodotObject object, StringName property, Variant value)
```

引擎名：`add_undo_property`

Register a property value change for "undo".

If this is the first operation, the `object` will be used to deduce target undo history.

### AddUndoReference

```csharp
public void AddUndoReference(GodotObject object)
```

引擎名：`add_undo_reference`

Register a reference for "undo" that will be erased if the "undo" history is lost. This is useful mostly for nodes removed with the "do" call (not the "undo" call!).

### ClearHistory

```csharp
public void ClearHistory(int id = -99, bool increaseVersion = true)
```

引擎名：`clear_history`

Clears the given undo history. You can clear history for a specific scene, global history, or for all histories at once (except [`EditorUndoRedoManager.SpecialHistory.RemoteHistory`](EditorUndoRedoManager.md)) if `id` is [`EditorUndoRedoManager.SpecialHistory.InvalidHistory`](EditorUndoRedoManager.md).

If `increaseVersion` is `true`, the undo history version will be increased, marking it as unsaved. Useful for operations that modify the scene, but don't support undo.

```csharp
var scene_root = EditorInterface.get_edited_scene_root()
var undo_redo = EditorInterface.get_editor_undo_redo()
undo_redo.clear_history(undo_redo.get_object_history_id(scene_root))

```

**Note:** If you want to mark an edited scene as unsaved without clearing its history, use [`EditorInterface.MarkSceneAsUnsaved`](EditorInterface.md) instead.

### CommitAction

```csharp
public void CommitAction(bool execute = true)
```

引擎名：`commit_action`

Commits the action. If `execute` is `true` (default), all "do" methods/properties are called/set when this function is called.

### CreateAction

```csharp
public void CreateAction(string name, UndoRedo.MergeMode mergeMode = UndoRedo.MergeMode.Disable, GodotObject customContext = null, bool backwardUndoOps = false, bool markUnsaved = true)
```

引擎名：`create_action`

Create a new action. After this is called, do all your calls to `AddDoMethod()`, `AddUndoMethod()`, `AddDoProperty()`, and `AddUndoProperty()`, then commit the action with `CommitAction()`.

The way actions are merged is dictated by the `mergeMode` argument.

If `customContext` object is provided, it will be used for deducing target history (instead of using the first operation).

The way undo operation are ordered in actions is dictated by `backwardUndoOps`. When `backwardUndoOps` is `false` undo option are ordered in the same order they were added. Which means the first operation to be added will be the first to be undone.

If `markUnsaved` is `false`, the action will not mark the history as unsaved. This is useful for example for actions that change a selection, or a setting that will be saved automatically. Otherwise, this should be left to `true` if the action requires saving by the user or if it can cause data loss when left unsaved.

### CreateAction

```csharp
public void CreateAction(string name, UndoRedo.MergeMode mergeMode, GodotObject customContext)
```

引擎名：`create_action`

Create a new action. After this is called, do all your calls to `AddDoMethod()`, `AddUndoMethod()`, `AddDoProperty()`, and `AddUndoProperty()`, then commit the action with `CommitAction()`.

The way actions are merged is dictated by the `mergeMode` argument.

If `customContext` object is provided, it will be used for deducing target history (instead of using the first operation).

The way undo operation are ordered in actions is dictated by `backwardUndoOps`. When `backwardUndoOps` is `false` undo option are ordered in the same order they were added. Which means the first operation to be added will be the first to be undone.

If `markUnsaved` is `false`, the action will not mark the history as unsaved. This is useful for example for actions that change a selection, or a setting that will be saved automatically. Otherwise, this should be left to `true` if the action requires saving by the user or if it can cause data loss when left unsaved.

### CreateAction

```csharp
public void CreateAction(string name, UndoRedo.MergeMode mergeMode, GodotObject customContext, bool backwardUndoOps)
```

引擎名：`create_action`

Create a new action. After this is called, do all your calls to `AddDoMethod()`, `AddUndoMethod()`, `AddDoProperty()`, and `AddUndoProperty()`, then commit the action with `CommitAction()`.

The way actions are merged is dictated by the `mergeMode` argument.

If `customContext` object is provided, it will be used for deducing target history (instead of using the first operation).

The way undo operation are ordered in actions is dictated by `backwardUndoOps`. When `backwardUndoOps` is `false` undo option are ordered in the same order they were added. Which means the first operation to be added will be the first to be undone.

If `markUnsaved` is `false`, the action will not mark the history as unsaved. This is useful for example for actions that change a selection, or a setting that will be saved automatically. Otherwise, this should be left to `true` if the action requires saving by the user or if it can cause data loss when left unsaved.

### ForceFixedHistory

```csharp
public void ForceFixedHistory()
```

引擎名：`force_fixed_history`

Forces the next operation (e.g. `AddDoMethod()`) to use the action's history rather than guessing it from the object. This is sometimes needed when a history can't be correctly determined, like for a nested resource that doesn't have a path yet.

This method should only be used when absolutely necessary, otherwise it might cause invalid history state. For most of complex cases, the `custom_context` parameter of `CreateAction()` is sufficient.

### GetHistoryUndoRedo

```csharp
public UndoRedo GetHistoryUndoRedo(int id)
```

引擎名：`get_history_undo_redo`

Returns the [`UndoRedo`](UndoRedo.md) object associated with the given history `id`.

`id` above `0` are mapped to the opened scene tabs (but it doesn't match their order). `id` of `0` or lower have special meaning (see [`EditorUndoRedoManager.SpecialHistory`](EditorUndoRedoManager.md)).

Best used with `GetObjectHistoryId()`. This method is only provided in case you need some more advanced methods of [`UndoRedo`](UndoRedo.md) (but keep in mind that directly operating on the [`UndoRedo`](UndoRedo.md) object might affect editor's stability).

### GetObjectHistoryId

```csharp
public int GetObjectHistoryId(GodotObject object)
```

引擎名：`get_object_history_id`

Returns the history ID deduced from the given `object`. It can be used with `GetHistoryUndoRedo()`.

### IsCommittingAction

```csharp
public bool IsCommittingAction()
```

引擎名：`is_committing_action`

Returns `true` if the `EditorUndoRedoManager` is currently committing the action, i.e. running its "do" method or property change (see `CommitAction()`).

## 事件（信号）

### HistoryChanged

```csharp
public event Action HistoryChanged
```

引擎名：`history_changed`

Emitted when the list of actions in any history has changed, either when an action is committed or a history is cleared.

### VersionChanged

```csharp
public event Action VersionChanged
```

引擎名：`version_changed`

Emitted when the version of any history has changed as a result of undo or redo call.

## SpecialHistory

命名空间 `Godot`；enum；仅编辑器（GodotSharpEditor）

### 枚举值

- `GlobalHistory = 0`：Global history not associated with any scene, but with external resources etc.
- `RemoteHistory = -9`：History associated with remote inspector.
- `InvalidHistory = -99`：Invalid "null" history.
