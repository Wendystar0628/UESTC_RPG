<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SceneTree

命名空间 `Godot`；class；继承 [`MainLoop`](MainLoop.md) → [`GodotObject`](GodotObject.md)

As one of the most important classes, the `SceneTree` manages the hierarchy of nodes in a scene, as well as scenes themselves. Nodes can be added, fetched and removed. The whole scene tree (and thus the current scene) can be paused. Scenes can be loaded, switched and reloaded.

You can also use the `SceneTree` to organize your nodes into **groups**: every node can be added to as many groups as you want to create, e.g. an "enemy" group. You can then iterate these groups or even call methods and set properties on all the nodes belonging to any given group.

`SceneTree` is the default [`MainLoop`](MainLoop.md) implementation used by the engine, and is thus in charge of the game loop.

## 构造函数

### SceneTree

```csharp
public SceneTree()
```

## 属性

### AutoAcceptQuit

```csharp
public bool AutoAcceptQuit { get; set; }
```

引擎名：`auto_accept_quit`

If `true`, the application automatically accepts quitting requests.

For mobile platforms, see `QuitOnGoBack`.

### CurrentScene

```csharp
public Node CurrentScene { get; set; }
```

引擎名：`current_scene`

The root node of the currently loaded main scene, usually as a direct child of `Root`. See also `ChangeSceneToFile()`, `ChangeSceneToPacked()`, and `ReloadCurrentScene()`.

**Warning:** Setting this property directly may not work as expected, as it does *not* add or remove any nodes from this tree.

### DebugCollisionsHint

```csharp
public bool DebugCollisionsHint { get; set; }
```

引擎名：`debug_collisions_hint`

If `true`, collision shapes will be visible when running the game from the editor for debugging purposes.

**Note:** This property is not designed to be changed at run-time. Changing the value of `DebugCollisionsHint` while the project is running will not have the desired effect.

### DebugNavigationHint

```csharp
public bool DebugNavigationHint { get; set; }
```

引擎名：`debug_navigation_hint`

If `true`, navigation polygons will be visible when running the game from the editor for debugging purposes.

**Note:** This property is not designed to be changed at run-time. Changing the value of `DebugNavigationHint` while the project is running will not have the desired effect.

### DebugPathsHint

```csharp
public bool DebugPathsHint { get; set; }
```

引擎名：`debug_paths_hint`

If `true`, curves from [`Path2D`](Path2D.md) and [`Path3D`](Path3D.md) nodes will be visible when running the game from the editor for debugging purposes.

**Note:** This property is not designed to be changed at run-time. Changing the value of `DebugPathsHint` while the project is running will not have the desired effect.

### EditedSceneRoot

```csharp
public Node EditedSceneRoot { get; set; }
```

引擎名：`edited_scene_root`

The root of the scene currently being edited in the editor. This is usually a direct child of `Root`.

**Note:** This property does nothing in release builds.

### MultiplayerPoll

```csharp
public bool MultiplayerPoll { get; set; }
```

引擎名：`multiplayer_poll`

If `true` (default value), enables automatic polling of the [`MultiplayerApi`](MultiplayerApi.md) for this SceneTree during `ProcessFrame`.

If `false`, you need to manually call [`MultiplayerApi.Poll`](MultiplayerApi.md) to process network packets and deliver RPCs. This allows running RPCs in a different loop (e.g. physics, thread, specific time step) and for manual [`Mutex`](Mutex.md) protection when accessing the [`MultiplayerApi`](MultiplayerApi.md) from threads.

### Paused

```csharp
public bool Paused { get; set; }
```

引擎名：`paused`

If `true`, the scene tree is considered paused. This causes the following behavior:

- 2D and 3D physics will be stopped, as well as collision detection and related signals.

- Depending on each node's [`Node.ProcessMode`](Node.md), their [`Node._Process()`](Node.md), [`Node._PhysicsProcess()`](Node.md) and [`Node._Input()`](Node.md) callback methods may not called anymore.

### PhysicsInterpolation

```csharp
public bool PhysicsInterpolation { get; set; }
```

引擎名：`physics_interpolation`

If `true`, the renderer will interpolate the transforms of objects (both physics and non-physics) between the last two transforms, so that smooth motion is seen even when physics ticks do not coincide with rendered frames.

The default value of this property is controlled by `ProjectSettings.physics/common/physics_interpolation`.

**Note:** Although this is a global setting, finer control of individual branches of the `SceneTree` is possible using [`Node.PhysicsInterpolationMode`](Node.md).

### QuitOnGoBack

```csharp
public bool QuitOnGoBack { get; set; }
```

引擎名：`quit_on_go_back`

If `true`, the application quits automatically when navigating back (e.g. using the system "Back" button on Android).

To handle 'Go Back' button when this option is disabled, use [`DisplayServer.WindowEvent.GoBackRequest`](DisplayServer.md).

### Root

```csharp
public Window Root { get; }
```

引擎名：`root`

The tree's root [`Window`](Window.md). This is top-most [`Node`](Node.md) of the scene tree, and is always present. An absolute [`NodePath`](NodePath.md) always starts from this node. Children of the root node may include the loaded `CurrentScene`, as well as any [AutoLoad]($DOCS_URL/tutorials/scripting/singletons_autoload.html) configured in the Project Settings.

**Warning:** Do not delete this node. This will result in unstable behavior, followed by a crash.

## 方法

### CallGroup

```csharp
public void CallGroup(StringName group, StringName method, ReadOnlySpan<Variant> args)
```

引擎名：`call_group`

Calls `method` on each node inside this tree added to the given `group`. You can pass arguments to `method` by specifying them at the end of this method call. Nodes that cannot call `method` (either because the method doesn't exist or the arguments do not match) are ignored. See also `SetGroup()` and `NotifyGroup()`.

**Note:** This method acts immediately on all selected nodes at once, which may cause stuttering in some performance-intensive situations.

**Note:** In C#, `method` must be in snake_case when referring to built-in Godot methods. Prefer using the names exposed in the `MethodName` class to avoid allocating a new [`StringName`](StringName.md) on each call.

### CallGroup

```csharp
public void CallGroup(StringName group, StringName method, params Variant[] args)
```

引擎名：`call_group`

Calls `method` on each node inside this tree added to the given `group`. You can pass arguments to `method` by specifying them at the end of this method call. Nodes that cannot call `method` (either because the method doesn't exist or the arguments do not match) are ignored. See also `SetGroup()` and `NotifyGroup()`.

**Note:** This method acts immediately on all selected nodes at once, which may cause stuttering in some performance-intensive situations.

**Note:** In C#, `method` must be in snake_case when referring to built-in Godot methods. Prefer using the names exposed in the `MethodName` class to avoid allocating a new [`StringName`](StringName.md) on each call.

### CallGroupFlags

```csharp
public void CallGroupFlags(long flags, StringName group, StringName method, ReadOnlySpan<Variant> args)
```

引擎名：`call_group_flags`

Calls the given `method` on each node inside this tree added to the given `group`. Use `flags` to customize this method's behavior (see [`SceneTree.GroupCallFlags`](SceneTree.md)). Additional arguments for `method` can be passed at the end of this method. Nodes that cannot call `method` (either because the method doesn't exist or the arguments do not match) are ignored.

```csharp
# Calls "hide" to all nodes of the "enemies" group, at the end of the frame and in reverse tree order.
get_tree().call_group_flags(
SceneTree.GROUP_CALL_DEFERRED | SceneTree.GROUP_CALL_REVERSE,
"enemies", "hide")

```

**Note:** In C#, `method` must be in snake_case when referring to built-in Godot methods. Prefer using the names exposed in the `MethodName` class to avoid allocating a new [`StringName`](StringName.md) on each call.

### CallGroupFlags

```csharp
public void CallGroupFlags(long flags, StringName group, StringName method, params Variant[] args)
```

引擎名：`call_group_flags`

Calls the given `method` on each node inside this tree added to the given `group`. Use `flags` to customize this method's behavior (see [`SceneTree.GroupCallFlags`](SceneTree.md)). Additional arguments for `method` can be passed at the end of this method. Nodes that cannot call `method` (either because the method doesn't exist or the arguments do not match) are ignored.

```csharp
# Calls "hide" to all nodes of the "enemies" group, at the end of the frame and in reverse tree order.
get_tree().call_group_flags(
SceneTree.GROUP_CALL_DEFERRED | SceneTree.GROUP_CALL_REVERSE,
"enemies", "hide")

```

**Note:** In C#, `method` must be in snake_case when referring to built-in Godot methods. Prefer using the names exposed in the `MethodName` class to avoid allocating a new [`StringName`](StringName.md) on each call.

### ChangeSceneToFile

```csharp
public Error ChangeSceneToFile(string path)
```

引擎名：`change_scene_to_file`

Changes the running scene to the one at the given `path`, after loading it into a [`PackedScene`](PackedScene.md) and creating a new instance.

Returns [`Error.Ok`](Error.md) on success, [`Error.CantOpen`](Error.md) if the `path` cannot be loaded into a [`PackedScene`](PackedScene.md), or [`Error.CantCreate`](Error.md) if that scene cannot be instantiated.

**Note:** See `ChangeSceneToNode()` for details on the order of operations.

### ChangeSceneToNode

```csharp
public Error ChangeSceneToNode(Node node)
```

引擎名：`change_scene_to_node`

Changes the running scene to the provided [`Node`](Node.md). Useful when you want to set up the new scene before changing.

Returns [`Error.Ok`](Error.md) on success, [`Error.InvalidParameter`](Error.md) if the `node` is `null`, or [`Error.Unconfigured`](Error.md) if the `node` is already inside the scene tree.

**Note:** Operations happen in the following order when `ChangeSceneToNode()` is called:

1. The current scene node is immediately removed from the tree. From that point, [`Node.GetTree`](Node.md) called on the current (outgoing) scene will return `null`. `CurrentScene` will be `null` too, because the new scene is not available yet.

2. At the end of the frame, the formerly current scene, already removed from the tree, will be deleted (freed from memory) and then the new scene node will be added to the tree. [`Node.GetTree`](Node.md) and `CurrentScene` will be back to working as usual.

This ensures that both scenes aren't running at the same time, while still freeing the previous scene in a safe way similar to [`Node.QueueFree`](Node.md).

If you want to reliably access the new scene, await the `SceneChanged` signal.

**Warning:** After using this method, the `SceneTree` will take ownership of the node and will free it automatically when changing scene again. Any references you had to that node will become invalid.

### ChangeSceneToPacked

```csharp
public Error ChangeSceneToPacked(PackedScene packedScene)
```

引擎名：`change_scene_to_packed`

Changes the running scene to a new instance of the given [`PackedScene`](PackedScene.md) (which must be valid).

Returns [`Error.Ok`](Error.md) on success, [`Error.CantCreate`](Error.md) if the scene cannot be instantiated, or [`Error.InvalidParameter`](Error.md) if the scene is invalid.

**Note:** See `ChangeSceneToNode()` for details on the order of operations.

### CreateTimer

```csharp
public SceneTreeTimer CreateTimer(double timeSec, bool processAlways = true, bool processInPhysics = false, bool ignoreTimeScale = false)
```

引擎名：`create_timer`

Returns a new [`SceneTreeTimer`](SceneTreeTimer.md). After `timeSec` in seconds have passed, the timer will emit [`SceneTreeTimer.Timeout`](SceneTreeTimer.md) and will be automatically freed.

If `processAlways` is `false`, the timer will be paused when setting `Paused` to `true`.

If `processInPhysics` is `true`, the timer will update at the end of the physics frame, instead of the process frame.

If `ignoreTimeScale` is `true`, the timer will ignore [`Engine.TimeScale`](Engine.md) and update with the real, elapsed time.

This method is commonly used to create a one-shot delay timer, as in the following example:

```csharp
public async Task SomeFunction()
{
GD.Print("start");
await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);
GD.Print("end");
}

```

**Note:** The timer is always updated *after* all of the nodes in the tree. A node's [`Node._Process()`](Node.md) method would be called before the timer updates (or [`Node._PhysicsProcess()`](Node.md) if `processInPhysics` is set to `true`).

### CreateTween

```csharp
public Tween CreateTween()
```

引擎名：`create_tween`

Creates and returns a new [`Tween`](Tween.md) processed in this tree. The Tween will start automatically on the next process frame or physics frame (depending on its [`Tween.TweenProcessMode`](Tween.md)).

**Note:** A [`Tween`](Tween.md) created using this method is not bound to any [`Node`](Node.md). It may keep working until there is nothing left to animate. If you want the [`Tween`](Tween.md) to be automatically killed when the [`Node`](Node.md) is freed, use [`Node.CreateTween`](Node.md) or [`Tween.BindNode`](Tween.md).

### GetCurrentScene

```csharp
public Node GetCurrentScene()
```

引擎名：`get_current_scene`

### GetEditedSceneRoot

```csharp
public Node GetEditedSceneRoot()
```

引擎名：`get_edited_scene_root`

### GetFirstNodeInGroup

```csharp
public Node GetFirstNodeInGroup(StringName group)
```

引擎名：`get_first_node_in_group`

Returns the first [`Node`](Node.md) found inside the tree, that has been added to the given `group`, in scene hierarchy order. Returns `null` if no match is found. See also `GetNodesInGroup()`.

### GetFrame

```csharp
public long GetFrame()
```

引擎名：`get_frame`

Returns how many physics process steps have been processed, since the application started. This is *not* a measurement of elapsed time. See also `PhysicsFrame`. For the number of frames rendered, see [`Engine.GetProcessFrames`](Engine.md).

### GetMultiplayer

```csharp
public MultiplayerApi GetMultiplayer(NodePath forPath = null)
```

引擎名：`get_multiplayer`

Searches for the [`MultiplayerApi`](MultiplayerApi.md) configured for the given path, if one does not exist it searches the parent paths until one is found. If the path is empty, or none is found, the default one is returned. See `SetMultiplayer()`.

### GetNodeCount

```csharp
public int GetNodeCount()
```

引擎名：`get_node_count`

Returns the number of nodes inside this tree.

### GetNodeCountInGroup

```csharp
public int GetNodeCountInGroup(StringName group)
```

引擎名：`get_node_count_in_group`

Returns the number of nodes assigned to the given group.

### GetNodesInGroup

```csharp
public Array<Node> GetNodesInGroup(StringName group)
```

引擎名：`get_nodes_in_group`

Returns an [`Collections.Array`](Collections.Array.md) containing all nodes inside this tree, that have been added to the given `group`, in scene hierarchy order.

### GetProcessedTweens

```csharp
public Array<Tween> GetProcessedTweens()
```

引擎名：`get_processed_tweens`

Returns an [`Collections.Array`](Collections.Array.md) of currently existing [`Tween`](Tween.md)s in the tree, including paused tweens.

### GetRoot

```csharp
public Window GetRoot()
```

引擎名：`get_root`

### HasGroup

```csharp
public bool HasGroup(StringName name)
```

引擎名：`has_group`

Returns `true` if a node added to the given group `name` exists in the tree.

### IsAccessibilityEnabled

```csharp
public bool IsAccessibilityEnabled()
```

引擎名：`is_accessibility_enabled`

Returns `true` if accessibility features are enabled, and accessibility information updates are actively processed.

### IsAccessibilitySupported

```csharp
public bool IsAccessibilitySupported()
```

引擎名：`is_accessibility_supported`

Returns `true` if accessibility features are supported by the OS and enabled in project settings.

### IsAutoAcceptQuit

```csharp
public bool IsAutoAcceptQuit()
```

引擎名：`is_auto_accept_quit`

### IsDebuggingCollisionsHint

```csharp
public bool IsDebuggingCollisionsHint()
```

引擎名：`is_debugging_collisions_hint`

### IsDebuggingNavigationHint

```csharp
public bool IsDebuggingNavigationHint()
```

引擎名：`is_debugging_navigation_hint`

### IsDebuggingPathsHint

```csharp
public bool IsDebuggingPathsHint()
```

引擎名：`is_debugging_paths_hint`

### IsMultiplayerPollEnabled

```csharp
public bool IsMultiplayerPollEnabled()
```

引擎名：`is_multiplayer_poll_enabled`

### IsPaused

```csharp
public bool IsPaused()
```

引擎名：`is_paused`

### IsPhysicsInterpolationEnabled

```csharp
public bool IsPhysicsInterpolationEnabled()
```

引擎名：`is_physics_interpolation_enabled`

### IsQuitOnGoBack

```csharp
public bool IsQuitOnGoBack()
```

引擎名：`is_quit_on_go_back`

### NotifyGroup

```csharp
public void NotifyGroup(StringName group, int notification)
```

引擎名：`notify_group`

Calls [`GodotObject.Notification`](GodotObject.md) with the given `notification` to all nodes inside this tree added to the `group`. See also [Godot notifications]($DOCS_URL/tutorials/best_practices/godot_notifications.html) and `CallGroup()` and `SetGroup()`.

**Note:** This method acts immediately on all selected nodes at once, which may cause stuttering in some performance-intensive situations.

### NotifyGroupFlags

```csharp
public void NotifyGroupFlags(uint callFlags, StringName group, int notification)
```

引擎名：`notify_group_flags`

Calls [`GodotObject.Notification`](GodotObject.md) with the given `notification` to all nodes inside this tree added to the `group`. Use `callFlags` to customize this method's behavior (see [`SceneTree.GroupCallFlags`](SceneTree.md)).

### QueueDelete

```csharp
public void QueueDelete(GodotObject obj)
```

引擎名：`queue_delete`

Queues the given `obj` to be deleted, calling its [`GodotObject.Free`](GodotObject.md) at the end of the current frame. This method is similar to [`Node.QueueFree`](Node.md).

### Quit

```csharp
public void Quit(int exitCode = 0)
```

引擎名：`quit`

Quits the application at the end of the current iteration, with the given `exitCode`.

By convention, an exit code of `0` indicates success, whereas any other exit code indicates an error. For portability reasons, it should be between `0` and `125` (inclusive).

**Note:** On iOS this method doesn't work. Instead, as recommended by the [iOS Human Interface Guidelines](https://developer.apple.com/library/archive/qa/qa1561/_index.html), the user is expected to close apps via the Home button.

### ReloadCurrentScene

```csharp
public Error ReloadCurrentScene()
```

引擎名：`reload_current_scene`

Reloads the currently active scene, replacing `CurrentScene` with a new instance of its original [`PackedScene`](PackedScene.md).

Returns [`Error.Ok`](Error.md) on success, [`Error.Unconfigured`](Error.md) if no `CurrentScene` is defined, [`Error.CantOpen`](Error.md) if `CurrentScene` cannot be loaded into a [`PackedScene`](PackedScene.md), or [`Error.CantCreate`](Error.md) if the scene cannot be instantiated.

### SetAutoAcceptQuit

```csharp
public void SetAutoAcceptQuit(bool enabled)
```

引擎名：`set_auto_accept_quit`

### SetCurrentScene

```csharp
public void SetCurrentScene(Node childNode)
```

引擎名：`set_current_scene`

### SetDebugCollisionsHint

```csharp
public void SetDebugCollisionsHint(bool enable)
```

引擎名：`set_debug_collisions_hint`

### SetDebugNavigationHint

```csharp
public void SetDebugNavigationHint(bool enable)
```

引擎名：`set_debug_navigation_hint`

### SetDebugPathsHint

```csharp
public void SetDebugPathsHint(bool enable)
```

引擎名：`set_debug_paths_hint`

### SetEditedSceneRoot

```csharp
public void SetEditedSceneRoot(Node scene)
```

引擎名：`set_edited_scene_root`

### SetGroup

```csharp
public void SetGroup(StringName group, string property, Variant value)
```

引擎名：`set_group`

Sets the given `property` to `value` on all nodes inside this tree added to the given `group`. Nodes that do not have the `property` are ignored. See also `CallGroup()` and `NotifyGroup()`.

**Note:** This method acts immediately on all selected nodes at once, which may cause stuttering in some performance-intensive situations.

**Note:** In C#, `property` must be in snake_case when referring to built-in Godot properties. Prefer using the names exposed in the `PropertyName` class to avoid allocating a new [`StringName`](StringName.md) on each call.

### SetGroupFlags

```csharp
public void SetGroupFlags(uint callFlags, StringName group, string property, Variant value)
```

引擎名：`set_group_flags`

Sets the given `property` to `value` on all nodes inside this tree added to the given `group`. Nodes that do not have the `property` are ignored. Use `callFlags` to customize this method's behavior (see [`SceneTree.GroupCallFlags`](SceneTree.md)).

**Note:** In C#, `property` must be in snake_case when referring to built-in Godot properties. Prefer using the names exposed in the `PropertyName` class to avoid allocating a new [`StringName`](StringName.md) on each call.

### SetMultiplayer

```csharp
public void SetMultiplayer(MultiplayerApi multiplayer, NodePath rootPath = null)
```

引擎名：`set_multiplayer`

Sets a custom [`MultiplayerApi`](MultiplayerApi.md) with the given `rootPath` (controlling also the relative subpaths), or override the default one if `rootPath` is empty.

**Note:** No [`MultiplayerApi`](MultiplayerApi.md) must be configured for the subpath containing `rootPath`, nested custom multiplayers are not allowed. I.e. if one is configured for `"/root/Foo"` setting one for `"/root/Foo/Bar"` will cause an error.

**Note:** `SetMultiplayer()` should be called *before* the child nodes are ready at the given `rootPath`. If multiplayer nodes like [`MultiplayerSpawner`](MultiplayerSpawner.md) or [`MultiplayerSynchronizer`](MultiplayerSynchronizer.md) are added to the tree before the custom multiplayer API is set, they will not work.

### SetMultiplayerPollEnabled

```csharp
public void SetMultiplayerPollEnabled(bool enabled)
```

引擎名：`set_multiplayer_poll_enabled`

### SetPause

```csharp
public void SetPause(bool enable)
```

引擎名：`set_pause`

### SetPhysicsInterpolationEnabled

```csharp
public void SetPhysicsInterpolationEnabled(bool enabled)
```

引擎名：`set_physics_interpolation_enabled`

### SetQuitOnGoBack

```csharp
public void SetQuitOnGoBack(bool enabled)
```

引擎名：`set_quit_on_go_back`

### UnloadCurrentScene

```csharp
public void UnloadCurrentScene()
```

引擎名：`unload_current_scene`

If a current scene is loaded, calling this method will unload it.

## 事件（信号）

### NodeAdded

```csharp
public event SceneTree.NodeAddedEventHandler NodeAdded
```

引擎名：`node_added`

Emitted when the `node` enters this tree.

### NodeConfigurationWarningChanged

```csharp
public event SceneTree.NodeConfigurationWarningChangedEventHandler NodeConfigurationWarningChanged
```

引擎名：`node_configuration_warning_changed`

Emitted when the `node`'s [`Node.UpdateConfigurationWarnings`](Node.md) is called. Only emitted in the editor.

### NodeRemoved

```csharp
public event SceneTree.NodeRemovedEventHandler NodeRemoved
```

引擎名：`node_removed`

Emitted when the `node` exits this tree.

### NodeRenamed

```csharp
public event SceneTree.NodeRenamedEventHandler NodeRenamed
```

引擎名：`node_renamed`

Emitted when the `node`'s [`Node.Name`](Node.md) is changed.

### PhysicsFrame

```csharp
public event Action PhysicsFrame
```

引擎名：`physics_frame`

Emitted immediately before [`Node._PhysicsProcess()`](Node.md) is called on every node in this tree.

### ProcessFrame

```csharp
public event Action ProcessFrame
```

引擎名：`process_frame`

Emitted immediately before [`Node._Process()`](Node.md) is called on every node in this tree.

### SceneChanged

```csharp
public event Action SceneChanged
```

引擎名：`scene_changed`

Emitted after the new scene is added to scene tree and initialized. Can be used to reliably access `CurrentScene` when changing scenes.

```csharp
# This code should be inside an autoload.
get_tree().change_scene_to_file(other_scene_path)
await get_tree().scene_changed
print(get_tree().current_scene) # Prints the new scene.

```

### TreeChanged

```csharp
public event Action TreeChanged
```

引擎名：`tree_changed`

Emitted any time the tree's hierarchy changes (nodes being moved, renamed, etc.).

### TreeProcessModeChanged

```csharp
public event Action TreeProcessModeChanged
```

引擎名：`tree_process_mode_changed`

Emitted when the [`Node.ProcessMode`](Node.md) of any node inside the tree is changed. Only emitted in the editor, to update the visibility of disabled nodes.

## GroupCallFlags

命名空间 `Godot`；enum

### 枚举值

- `Default = 0`：Call nodes within a group with no special behavior (default).
- `Reverse = 1`：Call nodes within a group in reverse tree hierarchy order (all nested children are called before their respective parent nodes).
- `Deferred = 2`：Call nodes within a group at the end of the current frame (can be either process or physics frame), similar to [`GodotObject.CallDeferred`](GodotObject.md).
- `Unique = 4`：Call nodes within a group only once, even if the call is executed many times in the same frame.

## NodeAddedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`SceneTree.NodeAdded`](SceneTree.md) event of a [`SceneTree`](SceneTree.md) class.

### 签名

#### Invoke

```csharp
delegate void NodeAddedEventHandler(Node node)
```

## NodeConfigurationWarningChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`SceneTree.NodeConfigurationWarningChanged`](SceneTree.md) event of a [`SceneTree`](SceneTree.md) class.

### 签名

#### Invoke

```csharp
delegate void NodeConfigurationWarningChangedEventHandler(Node node)
```

## NodeRemovedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`SceneTree.NodeRemoved`](SceneTree.md) event of a [`SceneTree`](SceneTree.md) class.

### 签名

#### Invoke

```csharp
delegate void NodeRemovedEventHandler(Node node)
```

## NodeRenamedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`SceneTree.NodeRenamed`](SceneTree.md) event of a [`SceneTree`](SceneTree.md) class.

### 签名

#### Invoke

```csharp
delegate void NodeRenamedEventHandler(Node node)
```
