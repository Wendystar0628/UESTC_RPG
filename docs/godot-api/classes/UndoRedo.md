<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# UndoRedo

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

UndoRedo works by registering methods and property changes inside "actions". You can create an action, then provide ways to do and undo this action using function calls and property changes, then commit the action.

When an action is committed, all of the `do_*` methods will run. If the `Undo()` method is used, the `undo_*` methods will run. If the `Redo()` method is used, once again, all of the `do_*` methods will run.

Here's an example on how to add an action:

```csharp
private UndoRedo _undoRedo;

public override void _Ready()
{
_undoRedo = new UndoRedo();
}

public void DoSomething()
{
// Put your code here.
}

public void UndoSomething()
{
// Put here the code that reverts what's done by "DoSomething()".
}

private void OnMyButtonPressed()
{
var node = GetNode<Node2D>("MyNode2D");
_undoRedo.CreateAction("Move the node");
_undoRedo.AddDoMethod(new Callable(this, MethodName.DoSomething));
_undoRedo.AddUndoMethod(new Callable(this, MethodName.UndoSomething));
_undoRedo.AddDoProperty(node, "position", new Vector2(100, 100));
_undoRedo.AddUndoProperty(node, "position", node.Position);
_undoRedo.CommitAction();
}

```

Before calling any of the `add_(un)do_*` methods, you need to first call `CreateAction()`. Afterwards you need to call `CommitAction()`.

If you don't need to register a method, you can leave `AddDoMethod()` and `AddUndoMethod()` out; the same goes for properties. You can also register more than one method/property.

If you are making an `EditorPlugin` and want to integrate into the editor's undo history, use `EditorUndoRedoManager` instead.

If you are registering multiple properties/method which depend on one another, be aware that by default undo operation are called in the same order they have been added. Therefore instead of grouping do operation with their undo operations it is better to group do on one side and undo on the other as shown below.

```csharp
_undo_redo.CreateAction("Add object");

// DO
_undo_redo.AddDoMethod(new Callable(this, MethodName.CreateObject));
_undo_redo.AddDoMethod(new Callable(this, MethodName.AddObjectToSingleton));

// UNDO
_undo_redo.AddUndoMethod(new Callable(this, MethodName.RemoveObjectFromSingleton));
_undo_redo.AddUndoMethod(new Callable(this, MethodName.DestroyThatObject));

_undo_redo.CommitAction();

```

## 构造函数

### UndoRedo

```csharp
public UndoRedo()
```

## 属性

### MaxSteps

```csharp
public int MaxSteps { get; set; }
```

引擎名：`max_steps`

The maximum number of steps that can be stored in the undo/redo history. If the number of stored steps exceeds this limit, older steps are removed from history and can no longer be reached by calling `Undo()`. A value of `0` or lower means no limit.

## 方法

### AddDoMethod

```csharp
public void AddDoMethod(Callable callable)
```

引擎名：`add_do_method`

Register a [`Callable`](Callable.md) that will be called when the action is committed.

### AddDoProperty

```csharp
public void AddDoProperty(GodotObject object, StringName property, Variant value)
```

引擎名：`add_do_property`

Register a `property` that would change its value to `value` when the action is committed.

### AddDoReference

```csharp
public void AddDoReference(GodotObject object)
```

引擎名：`add_do_reference`

Register a reference to an object that will be erased if the "do" history is deleted. This is useful for objects added by the "do" action and removed by the "undo" action.

When the "do" history is deleted, if the object is a [`RefCounted`](RefCounted.md), it will be unreferenced. Otherwise, it will be freed. Do not use for resources.

```csharp
var node = Node2D.new()
undo_redo.create_action("Add node")
undo_redo.add_do_method(add_child.bind(node))
undo_redo.add_do_reference(node)
undo_redo.add_undo_method(remove_child.bind(node))
undo_redo.commit_action()

```

### AddUndoMethod

```csharp
public void AddUndoMethod(Callable callable)
```

引擎名：`add_undo_method`

Register a [`Callable`](Callable.md) that will be called when the action is undone.

### AddUndoProperty

```csharp
public void AddUndoProperty(GodotObject object, StringName property, Variant value)
```

引擎名：`add_undo_property`

Register a `property` that would change its value to `value` when the action is undone.

### AddUndoReference

```csharp
public void AddUndoReference(GodotObject object)
```

引擎名：`add_undo_reference`

Register a reference to an object that will be erased if the "undo" history is deleted. This is useful for objects added by the "undo" action and removed by the "do" action.

When the "undo" history is deleted, if the object is a [`RefCounted`](RefCounted.md), it will be unreferenced. Otherwise, it will be freed. Do not use for resources.

```csharp
var node = $Node2D
undo_redo.create_action("Remove node")
undo_redo.add_do_method(remove_child.bind(node))
undo_redo.add_undo_method(add_child.bind(node))
undo_redo.add_undo_reference(node)
undo_redo.commit_action()

```

### ClearHistory

```csharp
public void ClearHistory(bool increaseVersion = true)
```

引擎名：`clear_history`

Clear the undo/redo history and associated references.

Passing `false` to `increaseVersion` will prevent the version number from increasing when the history is cleared.

### CommitAction

```csharp
public void CommitAction(bool execute = true)
```

引擎名：`commit_action`

Commit the action. If `execute` is `true` (which it is by default), all "do" methods/properties are called/set when this function is called.

### CreateAction

```csharp
public void CreateAction(string name, UndoRedo.MergeMode mergeMode = UndoRedo.MergeMode.Disable, bool backwardUndoOps = false)
```

引擎名：`create_action`

Create a new action. After this is called, do all your calls to `AddDoMethod()`, `AddUndoMethod()`, `AddDoProperty()`, and `AddUndoProperty()`, then commit the action with `CommitAction()`.

The way actions are merged is dictated by `mergeMode`.

The way undo operation are ordered in actions is dictated by `backwardUndoOps`. When `backwardUndoOps` is `false` undo option are ordered in the same order they were added. Which means the first operation to be added will be the first to be undone.

### CreateAction

```csharp
public void CreateAction(string name, UndoRedo.MergeMode mergeMode)
```

引擎名：`create_action`

Create a new action. After this is called, do all your calls to `AddDoMethod()`, `AddUndoMethod()`, `AddDoProperty()`, and `AddUndoProperty()`, then commit the action with `CommitAction()`.

The way actions are merged is dictated by `mergeMode`.

The way undo operation are ordered in actions is dictated by `backwardUndoOps`. When `backwardUndoOps` is `false` undo option are ordered in the same order they were added. Which means the first operation to be added will be the first to be undone.

### EndForceKeepInMergeEnds

```csharp
public void EndForceKeepInMergeEnds()
```

引擎名：`end_force_keep_in_merge_ends`

Stops marking operations as to be processed even if the action gets merged with another in the [`UndoRedo.MergeMode.Ends`](UndoRedo.md) mode. See `StartForceKeepInMergeEnds()`.

### GetActionName

```csharp
public string GetActionName(int id)
```

引擎名：`get_action_name`

Gets the action name from its index.

### GetCurrentAction

```csharp
public int GetCurrentAction()
```

引擎名：`get_current_action`

Gets the index of the current action.

### GetCurrentActionName

```csharp
public string GetCurrentActionName()
```

引擎名：`get_current_action_name`

Gets the name of the current action, equivalent to `get_action_name(get_current_action())`.

### GetHistoryCount

```csharp
public int GetHistoryCount()
```

引擎名：`get_history_count`

Returns how many elements are in the history.

### GetMaxSteps

```csharp
public int GetMaxSteps()
```

引擎名：`get_max_steps`

### GetVersion

```csharp
public ulong GetVersion()
```

引擎名：`get_version`

Gets the version. Every time a new action is committed, the `UndoRedo`'s version number is increased automatically.

This is useful mostly to check if something changed from a saved version.

### HasRedo

```csharp
public bool HasRedo()
```

引擎名：`has_redo`

Returns `true` if a "redo" action is available.

### HasUndo

```csharp
public bool HasUndo()
```

引擎名：`has_undo`

Returns `true` if an "undo" action is available.

### IsCommittingAction

```csharp
public bool IsCommittingAction()
```

引擎名：`is_committing_action`

Returns `true` if the `UndoRedo` is currently committing the action, i.e. running its "do" method or property change (see `CommitAction()`).

### Redo

```csharp
public bool Redo()
```

引擎名：`redo`

Redo the last action. Returns `false` if there was no action to redo.

### SetMaxSteps

```csharp
public void SetMaxSteps(int maxSteps)
```

引擎名：`set_max_steps`

### StartForceKeepInMergeEnds

```csharp
public void StartForceKeepInMergeEnds()
```

引擎名：`start_force_keep_in_merge_ends`

Marks the next "do" and "undo" operations to be processed even if the action gets merged with another in the [`UndoRedo.MergeMode.Ends`](UndoRedo.md) mode. Return to normal operation using `EndForceKeepInMergeEnds()`.

### Undo

```csharp
public bool Undo()
```

引擎名：`undo`

Undo the last action. Returns `false` if there was no action to undo.

## 事件（信号）

### VersionChanged

```csharp
public event Action VersionChanged
```

引擎名：`version_changed`

Called when `Undo()` or `Redo()` was called.

## MergeMode

命名空间 `Godot`；enum

### 枚举值

- `Disable = 0`：Makes "do"/"undo" operations stay in separate actions.
- `Ends = 1`：Merges this action with the previous one if they have the same name.
- `All = 2`：Merges this action with the previous one if they have the same name.
