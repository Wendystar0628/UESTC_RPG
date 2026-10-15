<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Node

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

Nodes are Godot's building blocks. They can be assigned as the child of another node, resulting in a tree arrangement. A given node can contain any number of nodes as children with the requirement that all siblings (direct children of a node) should have unique names.

A tree of nodes is called a *scene*. Scenes can be saved to the disk and then instantiated into other scenes. This allows for very high flexibility in the architecture and data model of Godot projects.

**Scene tree:** The [`SceneTree`](SceneTree.md) contains the active tree of nodes. When a node is added to the scene tree, it receives the `NotificationEnterTree` notification and its `_EnterTree()` callback is triggered. Child nodes are always added *after* their parent node, i.e. the `_EnterTree()` callback of a parent node will be triggered before its child's.

Once all nodes have been added in the scene tree, they receive the `NotificationReady` notification and their respective `_Ready()` callbacks are triggered. For groups of nodes, the `_Ready()` callback is called in reverse order, starting with the children and moving up to the parent nodes.

This means that when adding a node to the scene tree, the following order will be used for the callbacks: `_EnterTree()` of the parent, `_EnterTree()` of the children, `_Ready()` of the children and finally `_Ready()` of the parent (recursively for the entire scene tree).

**Processing:** Nodes can override the "process" state, so that they receive a callback on each frame requesting them to process (do something). Normal processing (callback `_Process()`, toggled with `SetProcess()`) happens as fast as possible and is dependent on the frame rate, so the processing time *delta* (in seconds) is passed as an argument. Physics processing (callback `_PhysicsProcess()`, toggled with `SetPhysicsProcess()`) happens a fixed number of times per second (60 by default) and is useful for code related to the physics engine.

Nodes can also process input events. When present, the `_Input()` function will be called for each input that the program receives. In many cases, this can be overkill (unless used for simple projects), and the `_UnhandledInput()` function might be preferred; it is called when the input event was not handled by anyone else (typically, GUI [`Control`](Control.md) nodes), ensuring that the node only receives the events that were meant for it.

To keep track of the scene hierarchy (especially when instantiating scenes into other scenes), an "owner" can be set for the node with the `Owner` property. This keeps track of who instantiated what. This is mostly useful when writing editors and tools, though.

Finally, when a node is freed with [`GodotObject.Free`](GodotObject.md) or `QueueFree()`, it will also free all its children.

**Groups:** Nodes can be added to as many groups as you want to be easy to manage, you could create groups like "enemies" or "collectables" for example, depending on your game. See `AddToGroup()`, `IsInGroup()` and `RemoveFromGroup()`. You can then retrieve all nodes in these groups, iterate them and even call methods on groups via the methods on [`SceneTree`](SceneTree.md).

**Networking with nodes:** After connecting to a server (or making one, see [`ENetMultiplayerPeer`](ENetMultiplayerPeer.md)), it is possible to use the built-in RPC (remote procedure call) system to communicate over the network. By calling `Rpc()` with a method name, it will be called locally and in all connected peers (peers = clients and the server that accepts connections). To identify which node receives the RPC call, Godot will use its [`NodePath`](NodePath.md) (make sure node names are the same on all peers). Also, take a look at the high-level networking tutorial and corresponding demos.

**Note:** The `script` property is part of the [`GodotObject`](GodotObject.md) class, not `Node`. It isn't exposed like most properties but does have a setter and getter (see [`GodotObject.SetScript`](GodotObject.md) and [`GodotObject.GetScript`](GodotObject.md)).

## 构造函数

### Node

```csharp
public Node()
```

## 属性

### AutoTranslateMode

```csharp
public Node.AutoTranslateModeEnum AutoTranslateMode { get; set; }
```

引擎名：`auto_translate_mode`

Defines if any text should automatically change to its translated version depending on the current locale (for nodes such as [`Label`](Label.md), [`RichTextLabel`](RichTextLabel.md), [`Window`](Window.md), etc.). Also decides if the node's strings should be parsed for translation template generation.

**Note:** For the root node, auto translate mode can also be set via `ProjectSettings.internationalization/rendering/root_node_auto_translate`.

### EditorDescription

```csharp
public string EditorDescription { get; set; }
```

引擎名：`editor_description`

An optional description to the node. It will be displayed as a tooltip when hovering over the node in the editor's Scene dock.

### Multiplayer

```csharp
public MultiplayerApi Multiplayer { get; }
```

引擎名：`multiplayer`

The [`MultiplayerApi`](MultiplayerApi.md) instance associated with this node. See [`SceneTree.GetMultiplayer`](SceneTree.md).

**Note:** Renaming the node, or moving it in the tree, will not move the [`MultiplayerApi`](MultiplayerApi.md) to the new path, you will have to update this manually.

### Name

```csharp
public StringName Name { get; set; }
```

引擎名：`name`

The name of the node. This name must be unique among the siblings (other child nodes from the same parent). When set to an existing sibling's name, the node is automatically renamed.

**Note:** When changing the name, the following characters will be replaced with an underscore: (`.` `:` `@` `/` `"` `%`). In particular, the `@` character is reserved for auto-generated names. See also `String.validate_node_name`.

### Owner

```csharp
public Node Owner { get; set; }
```

引擎名：`owner`

The owner of this node. The owner must be an ancestor of this node. When packing the owner node in a [`PackedScene`](PackedScene.md), all the nodes it owns are also saved with it. See also `UniqueNameInOwner`.

**Note:** In the editor, nodes not owned by the scene root are usually not displayed in the Scene dock, and will **not** be saved. To prevent this, remember to set the owner after calling `AddChild()`.

**Note:** The owner needs to be the current scene root. See [Instancing scenes]($DOCS_URL/tutorials/plugins/running_code_in_the_editor.html#instancing-scenes) in the documentation for more information.

### PhysicsInterpolationMode

```csharp
public Node.PhysicsInterpolationModeEnum PhysicsInterpolationMode { get; set; }
```

引擎名：`physics_interpolation_mode`

The physics interpolation mode to use for this node. Only effective if `ProjectSettings.physics/common/physics_interpolation` or [`SceneTree.PhysicsInterpolation`](SceneTree.md) is `true`.

By default, nodes inherit the physics interpolation mode from their parent. This property can enable or disable physics interpolation individually for each node, regardless of their parents' physics interpolation mode.

**Note:** Some node types like [`VehicleWheel3D`](VehicleWheel3D.md) have physics interpolation disabled by default, as they rely on their own custom solution.

**Note:** When teleporting a node to a distant position, it's recommended to temporarily disable interpolation with `ResetPhysicsInterpolation()` *after* moving the node. This avoids creating a visual streak between the old and new positions.

### ProcessMode

```csharp
public Node.ProcessModeEnum ProcessMode { get; set; }
```

引擎名：`process_mode`

The node's processing behavior. To check if the node can process in its current mode, use `CanProcess()`.

### ProcessPhysicsPriority

```csharp
public int ProcessPhysicsPriority { get; set; }
```

引擎名：`process_physics_priority`

Similar to `ProcessPriority` but for `NotificationPhysicsProcess`, `_PhysicsProcess()`, or `NotificationInternalPhysicsProcess`.

### ProcessPriority

```csharp
public int ProcessPriority { get; set; }
```

引擎名：`process_priority`

The node's execution order of the process callbacks (`_Process()`, `NotificationProcess`, and `NotificationInternalProcess`). Nodes whose priority value is *lower* call their process callbacks first, regardless of tree order.

### ProcessThreadGroup

```csharp
public Node.ProcessThreadGroupEnum ProcessThreadGroup { get; set; }
```

引擎名：`process_thread_group`

Set the process thread group for this node (basically, whether it receives `NotificationProcess`, `NotificationPhysicsProcess`, `_Process()` or `_PhysicsProcess()` (and the internal versions) on the main thread or in a sub-thread.

By default, the thread group is [`Node.ProcessThreadGroupEnum.Inherit`](Node.md), which means that this node belongs to the same thread group as the parent node. The thread groups means that nodes in a specific thread group will process together, separate to other thread groups (depending on `ProcessThreadGroupOrder`). If the value is set is [`Node.ProcessThreadGroupEnum.SubThread`](Node.md), this thread group will occur on a sub thread (not the main thread), otherwise if set to [`Node.ProcessThreadGroupEnum.MainThread`](Node.md) it will process on the main thread. If there is not a parent or grandparent node set to something other than inherit, the node will belong to the *default thread group*. This default group will process on the main thread and its group order is 0.

During processing in a sub-thread, accessing most functions in nodes outside the thread group is forbidden (and it will result in an error in debug mode). Use [`GodotObject.CallDeferred`](GodotObject.md), `CallThreadSafe()`, `CallDeferredThreadGroup()` and the likes in order to communicate from the thread groups to the main thread (or to other thread groups).

To better understand process thread groups, the idea is that any node set to any other value than [`Node.ProcessThreadGroupEnum.Inherit`](Node.md) will include any child (and grandchild) nodes set to inherit into its process thread group. This means that the processing of all the nodes in the group will happen together, at the same time as the node including them.

### ProcessThreadGroupOrder

```csharp
public int ProcessThreadGroupOrder { get; set; }
```

引擎名：`process_thread_group_order`

Change the process thread group order. Groups with a lesser order will process before groups with a greater order. This is useful when a large amount of nodes process in sub thread and, afterwards, another group wants to collect their result in the main thread, as an example.

### ProcessThreadMessages

```csharp
public Node.ProcessThreadMessagesEnum ProcessThreadMessages { get; set; }
```

引擎名：`process_thread_messages`

Set whether the current thread group will process messages (calls to `CallDeferredThreadGroup()` on threads), and whether it wants to receive them during regular process or physics process callbacks.

### SceneFilePath

```csharp
public string SceneFilePath { get; set; }
```

引擎名：`scene_file_path`

The original scene's file path, if the node has been instantiated from a [`PackedScene`](PackedScene.md) file. Only scene root nodes contains this.

### UniqueNameInOwner

```csharp
public bool UniqueNameInOwner { get; set; }
```

引擎名：`unique_name_in_owner`

If `true`, the node can be accessed from any node sharing the same `Owner` or from the `Owner` itself, with special `%Name` syntax in `GetNode()`.

**Note:** If another node with the same `Owner` shares the same `Name` as this node, the other node will no longer be accessible as unique.

## 方法

### AddChild

```csharp
public void AddChild(Node node, bool forceReadableName = false, Node.InternalMode internal = Node.InternalMode.Disabled)
```

引擎名：`add_child`

Adds a child `node`. Nodes can have any number of children, but every child must have a unique name. Child nodes are automatically deleted when the parent node is deleted, so an entire scene can be removed by deleting its topmost node.

If `forceReadableName` is `true`, improves the readability of the added `node`. If not named, the `node` is renamed to its type, and if it shares `Name` with a sibling, a number is suffixed more appropriately. This operation is very slow. As such, it is recommended leaving this to `false`, which assigns a dummy name featuring `@` in both situations.

If `internal` is different than [`Node.InternalMode.Disabled`](Node.md), the child will be added as internal node. These nodes are ignored by methods like `GetChildren()`, unless their parameter `include_internal` is `true`. It also prevents these nodes being duplicated with their parent. The intended usage is to hide the internal nodes from the user, so the user won't accidentally delete or modify them. Used by some GUI nodes, e.g. [`ColorPicker`](ColorPicker.md).

**Note:** If `node` already has a parent, this method will fail. Use `RemoveChild()` first to remove `node` from its current parent. For example:

```csharp
Node childNode = GetChild(0);
if (childNode.GetParent() != null)
{
childNode.GetParent().RemoveChild(childNode);
}
AddChild(childNode);

```

If you need the child node to be added below a specific node in the list of children, use `AddSibling()` instead of this method.

**Note:** If you want a child to be persisted to a [`PackedScene`](PackedScene.md), you must set `Owner` in addition to calling `AddChild()`. This is typically relevant for [tool scripts]($DOCS_URL/tutorials/plugins/running_code_in_the_editor.html) and [editor plugins]($DOCS_URL/tutorials/plugins/editor/index.html). If `AddChild()` is called without setting `Owner`, the newly added `Node` will not be visible in the scene tree, though it will be visible in the 2D/3D view.

### AddSibling

```csharp
public void AddSibling(Node sibling, bool forceReadableName = false)
```

引擎名：`add_sibling`

Adds a `sibling` node to this node's parent, and moves the added sibling right below this node.

If `forceReadableName` is `true`, improves the readability of the added `sibling`. If not named, the `sibling` is renamed to its type, and if it shares `Name` with a sibling, a number is suffixed more appropriately. This operation is very slow. As such, it is recommended leaving this to `false`, which assigns a dummy name featuring `@` in both situations.

Use `AddChild()` instead of this method if you don't need the child node to be added below a specific node in the list of children.

**Note:** If this node is internal, the added sibling will be internal too (see `AddChild()`'s `internal` parameter).

### AddToGroup

```csharp
public void AddToGroup(StringName group, bool persistent = false)
```

引擎名：`add_to_group`

Adds the node to the `group`. Groups can be helpful to organize a subset of nodes, for example `"enemies"` or `"collectables"`. See notes in the description, and the group methods in [`SceneTree`](SceneTree.md).

If `persistent` is `true`, the group will be stored when saved inside a [`PackedScene`](PackedScene.md). All groups created and displayed in the Groups dock are persistent.

**Note:** To improve performance, the order of group names is *not* guaranteed and may vary between project runs. Therefore, do not rely on the group order.

**Note:** [`SceneTree`](SceneTree.md)'s group methods will *not* work on this node if not inside the tree (see `IsInsideTree()`).

### Atr

```csharp
public string Atr(string message, StringName context = null)
```

引擎名：`atr`

Translates a `message`, using the translation catalogs configured in the Project Settings. Further `context` can be specified to help with the translation. Note that most [`Control`](Control.md) nodes automatically translate their strings, so this method is mostly useful for formatted strings or custom drawn text.

This method works the same as [`GodotObject.Tr`](GodotObject.md), with the addition of respecting the `AutoTranslateMode` state.

If [`GodotObject.CanTranslateMessages`](GodotObject.md) is `false`, or no translation is available, this method returns the `message` without changes. See [`GodotObject.SetMessageTranslation`](GodotObject.md).

For detailed examples, see [Internationalizing games]($DOCS_URL/tutorials/i18n/internationalizing_games.html).

### AtrN

```csharp
public string AtrN(string message, StringName pluralMessage, int n, StringName context = null)
```

引擎名：`atr_n`

Translates a `message` or `pluralMessage`, using the translation catalogs configured in the Project Settings. Further `context` can be specified to help with the translation.

This method works the same as [`GodotObject.TrN`](GodotObject.md), with the addition of respecting the `AutoTranslateMode` state.

If [`GodotObject.CanTranslateMessages`](GodotObject.md) is `false`, or no translation is available, this method returns `message` or `pluralMessage`, without changes. See [`GodotObject.SetMessageTranslation`](GodotObject.md).

The `n` is the number, or amount, of the message's subject. It is used by the translation system to fetch the correct plural form for the current language.

For detailed examples, see [Localization using gettext]($DOCS_URL/tutorials/i18n/localization_using_gettext.html).

**Note:** Negative and `System.Single` numbers may not properly apply to some countable subjects. It's recommended to handle these cases with `Atr()`.

### CallDeferredThreadGroup

```csharp
public Variant CallDeferredThreadGroup(StringName method, ReadOnlySpan<Variant> args)
```

引擎名：`call_deferred_thread_group`

This function is similar to [`GodotObject.CallDeferred`](GodotObject.md) except that the call will take place when the node thread group is processed. If the node thread group processes in sub-threads, then the call will be done on that thread, right before `NotificationProcess` or `NotificationPhysicsProcess`, the `_Process()` or `_PhysicsProcess()` or their internal versions are called.

### CallDeferredThreadGroup

```csharp
public Variant CallDeferredThreadGroup(StringName method, params Variant[] args)
```

引擎名：`call_deferred_thread_group`

This function is similar to [`GodotObject.CallDeferred`](GodotObject.md) except that the call will take place when the node thread group is processed. If the node thread group processes in sub-threads, then the call will be done on that thread, right before `NotificationProcess` or `NotificationPhysicsProcess`, the `_Process()` or `_PhysicsProcess()` or their internal versions are called.

### CallThreadSafe

```csharp
public Variant CallThreadSafe(StringName method, ReadOnlySpan<Variant> args)
```

引擎名：`call_thread_safe`

This function ensures that the calling of this function will succeed, no matter whether it's being done from a thread or not. If called from a thread that is not allowed to call the function, the call will become deferred. Otherwise, the call will go through directly.

### CallThreadSafe

```csharp
public Variant CallThreadSafe(StringName method, params Variant[] args)
```

引擎名：`call_thread_safe`

This function ensures that the calling of this function will succeed, no matter whether it's being done from a thread or not. If called from a thread that is not allowed to call the function, the call will become deferred. Otherwise, the call will go through directly.

### CanAutoTranslate

```csharp
public bool CanAutoTranslate()
```

引擎名：`can_auto_translate`

Returns `true` if this node can automatically translate messages depending on the current locale. See `AutoTranslateMode`, `Atr()`, and `AtrN()`.

### CanProcess

```csharp
public bool CanProcess()
```

引擎名：`can_process`

Returns `true` if the node can receive processing notifications and input callbacks (`NotificationProcess`, `_Input()`, etc.) from the [`SceneTree`](SceneTree.md) and [`Viewport`](Viewport.md). The returned value depends on `ProcessMode`:

- If set to [`Node.ProcessModeEnum.Pausable`](Node.md), returns `true` when the game is processing, i.e. [`SceneTree.Paused`](SceneTree.md) is `false`;

- If set to [`Node.ProcessModeEnum.WhenPaused`](Node.md), returns `true` when the game is paused, i.e. [`SceneTree.Paused`](SceneTree.md) is `true`;

- If set to [`Node.ProcessModeEnum.Always`](Node.md), always returns `true`;

- If set to [`Node.ProcessModeEnum.Disabled`](Node.md), always returns `false`;

- If set to [`Node.ProcessModeEnum.Inherit`](Node.md), use the parent node's `ProcessMode` to determine the result.

If the node is not inside the tree, returns `false` no matter the value of `ProcessMode`.

### CreateTween

```csharp
public Tween CreateTween()
```

引擎名：`create_tween`

Creates a new [`Tween`](Tween.md) and binds it to this node.

This is the equivalent of doing:

```csharp
GetTree().CreateTween().BindNode(this);

```

The Tween will start automatically on the next process frame or physics frame (depending on [`Tween.TweenProcessMode`](Tween.md)). See [`Tween.BindNode`](Tween.md) for more info on Tweens bound to nodes.

**Note:** The method can still be used when the node is not inside [`SceneTree`](SceneTree.md). It can fail in an unlikely case of using a custom [`MainLoop`](MainLoop.md).

### Duplicate

```csharp
public Node Duplicate(int flags = 15)
```

引擎名：`duplicate`

Duplicates the node, returning a new node with all of its properties, signals, groups, and children copied from the original, recursively. The behavior can be tweaked through the `flags` (see [`Node.DuplicateFlags`](Node.md)). Internal nodes are not duplicated.

**Note:** For nodes with a [`Script`](Script.md) attached, if [`GodotObject`](GodotObject.md) has been defined with required parameters, the duplicated node will not have a [`Script`](Script.md).

**Note:** By default, this method will duplicate only properties marked for serialization (i.e. using [`PropertyUsageFlags.Storage`](PropertyUsageFlags.md), or in GDScript, [annotation @GDScript.@export]). If you want to duplicate all properties, use [`Node.DuplicateFlags.InternalState`](Node.md).

### FindChild

```csharp
public Node FindChild(string pattern, bool recursive = true, bool owned = true)
```

引擎名：`find_child`

Finds the first descendant of this node whose `Name` matches `pattern`, returning `null` if no match is found. The matching is done against node names, *not* their paths, through `String.match`. As such, it is case-sensitive, `"*"` matches zero or more characters, and `"?"` matches any single character.

If `recursive` is `false`, only this node's direct children are checked. Nodes are checked in tree order, so this node's first direct child is checked first, then its own direct children, etc., before moving to the second direct child, and so on. Internal children are also included in the search (see `internal` parameter in `AddChild()`).

If `owned` is `true`, only descendants with a valid `Owner` node are checked.

**Note:** This method can be very slow. Consider storing a reference to the found node in a variable. Alternatively, use `GetNode()` with unique names (see `UniqueNameInOwner`).

**Note:** To find all descendant nodes matching a pattern or a class type, see `FindChildren()`.

### FindChildren

```csharp
public Array<Node> FindChildren(string pattern, string type = "", bool recursive = true, bool owned = true)
```

引擎名：`find_children`

Finds all descendants of this node whose names match `pattern`, returning an empty [`Collections.Array`](Collections.Array.md) if no match is found. The matching is done against node names, *not* their paths, through `String.match`. As such, it is case-sensitive, `"*"` matches zero or more characters, and `"?"` matches any single character.

If `type` is not empty, only descendants inheriting from `type` are included (see [`GodotObject.IsClass`](GodotObject.md)).

If `recursive` is `false`, only this node's direct children are checked. Nodes are checked in tree order, so this node's first direct child is checked first, then its own direct children, etc., before moving to the second direct child, and so on. Internal children are also included in the search (see `internal` parameter in `AddChild()`).

If `owned` is `true`, only descendants with a valid `Owner` node are checked.

**Note:** This method can be very slow. Consider storing references to the found nodes in a variable.

**Note:** To find a single descendant node matching a pattern, see `FindChild()`.

### FindParent

```csharp
public Node FindParent(string pattern)
```

引擎名：`find_parent`

Finds the first ancestor of this node whose `Name` matches `pattern`, returning `null` if no match is found. The matching is done through `String.match`. As such, it is case-sensitive, `"*"` matches zero or more characters, and `"?"` matches any single character. See also `FindChild()` and `FindChildren()`.

**Note:** As this method walks upwards in the scene tree, it can be slow in large, deeply nested nodes. Consider storing a reference to the found node in a variable. Alternatively, use `GetNode()` with unique names (see `UniqueNameInOwner`).

### GetAccessibilityElement

```csharp
public Rid GetAccessibilityElement()
```

引擎名：`get_accessibility_element`

Returns main accessibility element RID.

**Note:** This method should be called only during accessibility information updates (`NotificationAccessibilityUpdate`).

### GetAutoTranslateMode

```csharp
public Node.AutoTranslateModeEnum GetAutoTranslateMode()
```

引擎名：`get_auto_translate_mode`

### GetChild

```csharp
public Node GetChild(int idx, bool includeInternal = false)
```

引擎名：`get_child`

Fetches a child node by its index. Each child node has an index relative to its siblings (see `GetIndex()`). The first child is at index 0. Negative values can also be used to start from the end of the list. This method can be used in combination with `GetChildCount()` to iterate over this node's children. If no child exists at the given index, this method returns `null` and an error is generated.

If `includeInternal` is `false`, internal children are ignored (see `AddChild()`'s `internal` parameter).

```csharp
# Assuming the following are children of this node, in order:
# First, Middle, Last.

var a = get_child(0).name  # a is "First"
var b = get_child(1).name  # b is "Middle"
var b = get_child(2).name  # b is "Last"
var c = get_child(-1).name # c is "Last"

```

**Note:** To fetch a node by [`NodePath`](NodePath.md), use `GetNode()`.

### GetChild

```csharp
public T GetChild<T>(int idx, bool includeInternal = false)
```

引擎名：`get_child`

Returns a child node by its index (see `GetChildCount()`).
This method is often used for iterating all children of a node.
Negative indices access the children from the last one.
To access a child node via its name, use `GetNode()`.

参数：
- `idx`：Child index.
- `includeInternal`：If `false`, internal children are skipped (see `internal`
parameter in `AddChild()`).

返回：The child `Node` at the given index `idx`.

### GetChildCount

```csharp
public int GetChildCount(bool includeInternal = false)
```

引擎名：`get_child_count`

Returns the number of children of this node.

If `includeInternal` is `false`, internal children are not counted (see `AddChild()`'s `internal` parameter).

### GetChildOrNull

```csharp
public T GetChildOrNull<T>(int idx, bool includeInternal = false)
```

Returns a child node by its index (see `GetChildCount()`).
This method is often used for iterating all children of a node.
Negative indices access the children from the last one.
To access a child node via its name, use `GetNode()`.

参数：
- `idx`：Child index.
- `includeInternal`：If `false`, internal children are skipped (see `internal`
parameter in `AddChild()`).

返回：The child `Node` at the given index `idx`, or `null` if not found.

### GetChildren

```csharp
public Array<Node> GetChildren(bool includeInternal = false)
```

引擎名：`get_children`

Returns all children of this node inside an [`Collections.Array`](Collections.Array.md).

If `includeInternal` is `false`, excludes internal children from the returned array (see `AddChild()`'s `internal` parameter).

### GetEditorDescription

```csharp
public string GetEditorDescription()
```

引擎名：`get_editor_description`

### GetGroups

```csharp
public Array<StringName> GetGroups()
```

引擎名：`get_groups`

Returns an [`Collections.Array`](Collections.Array.md) of group names that the node has been added to.

**Note:** To improve performance, the order of group names is *not* guaranteed and may vary between project runs. Therefore, do not rely on the group order.

**Note:** This method may also return some group names starting with an underscore (`_`). These are internally used by the engine. To avoid conflicts, do not use custom groups starting with underscores. To exclude internal groups, see the following code snippet:

```csharp
// Stores the node's non-internal groups only (as a List of StringNames).
List<string> nonInternalGroups = new List<string>();
foreach (string group in GetGroups())
{
if (!group.BeginsWith("_"))
nonInternalGroups.Add(group);
}

```

### GetIndex

```csharp
public int GetIndex(bool includeInternal = false)
```

引擎名：`get_index`

Returns this node's order among its siblings. The first node's index is `0`. See also `GetChild()`.

If `includeInternal` is `false`, returns the index ignoring internal children. The first, non-internal child will have an index of `0` (see `AddChild()`'s `internal` parameter).

### GetLastExclusiveWindow

```csharp
public Window GetLastExclusiveWindow()
```

引擎名：`get_last_exclusive_window`

Returns the [`Window`](Window.md) that contains this node, or the last exclusive child in a chain of windows starting with the one that contains this node.

### GetMultiplayer

```csharp
public MultiplayerApi GetMultiplayer()
```

引擎名：`get_multiplayer`

### GetMultiplayerAuthority

```csharp
public int GetMultiplayerAuthority()
```

引擎名：`get_multiplayer_authority`

Returns the peer ID of the multiplayer authority for this node. See `SetMultiplayerAuthority()`.

### GetName

```csharp
public StringName GetName()
```

引擎名：`get_name`

### GetNode

```csharp
public Node GetNode(NodePath path)
```

引擎名：`get_node`

Fetches a node. The [`NodePath`](NodePath.md) can either be a relative path (from this node), or an absolute path (from the [`SceneTree.Root`](SceneTree.md)) to a node. If `path` does not point to a valid node, generates an error and returns `null`. Attempts to access methods on the return value will result in an *"Attempt to call <method> on a null instance."* error.

**Note:** Fetching by absolute path only works when the node is inside the scene tree (see `IsInsideTree()`).

**Example:** Assume this method is called from the Character node, inside the following tree:

```csharp
┖╴root
┠╴Character (you are here!)
┃  ┠╴Sword
┃  ┖╴Backpack
┃     ┖╴Dagger
┠╴MyGame
┖╴Swamp
┠╴Alligator
┠╴Mosquito
┖╴Goblin

```

The following calls will return a valid node:

```csharp
GetNode("Sword");
GetNode("Backpack/Dagger");
GetNode("../Swamp/Alligator");
GetNode("/root/MyGame");

```

### GetNode

```csharp
public T GetNode<T>(NodePath path)
```

引擎名：`get_node`

Fetches a node. The [`NodePath`](NodePath.md) can be either a relative path (from
the current node) or an absolute path (in the scene tree) to a node. If the path
does not exist, a `null` instance is returned and an error
is logged. Attempts to access methods on the return value will result in an
"Attempt to call <method> on a null instance." error.
Note: Fetching absolute paths only works when the node is inside the scene tree
(see `IsInsideTree()`).

参数：
- `path`：The path to the node to fetch.

返回：The `Node` at the given `path`.

### GetNodeAndResource

```csharp
public Array GetNodeAndResource(NodePath path)
```

引擎名：`get_node_and_resource`

Fetches a node and its most nested resource as specified by the [`NodePath`](NodePath.md)'s subname. Returns an [`Collections.Array`](Collections.Array.md) of size `3` where:

- Element `0` is the `Node`, or `null` if not found;

- Element `1` is the subname's last nested [`Resource`](Resource.md), or `null` if not found;

- Element `2` is the remaining [`NodePath`](NodePath.md), referring to an existing, non-[`Resource`](Resource.md) property (see [`GodotObject.GetIndexed`](GodotObject.md)).

**Example:** Assume that the child's [`Sprite2D.Texture`](Sprite2D.md) has been assigned an [`AtlasTexture`](AtlasTexture.md):

```csharp
var a = GetNodeAndResource(NodePath("Area2D/Sprite2D"));
GD.Print(a[0].Name); // Prints Sprite2D
GD.Print(a[1]);      // Prints <null>
GD.Print(a[2]);      // Prints ^"

var b = GetNodeAndResource(NodePath("Area2D/Sprite2D:texture:atlas"));
GD.Print(b[0].name);        // Prints Sprite2D
GD.Print(b[1].get_class()); // Prints AtlasTexture
GD.Print(b[2]);             // Prints ^""

var c = GetNodeAndResource(NodePath("Area2D/Sprite2D:texture:atlas:region"));
GD.Print(c[0].name);        // Prints Sprite2D
GD.Print(c[1].get_class()); // Prints AtlasTexture
GD.Print(c[2]);             // Prints ^":region"

```

### GetNodeOrNull

```csharp
public Node GetNodeOrNull(NodePath path)
```

引擎名：`get_node_or_null`

Fetches a node by [`NodePath`](NodePath.md). Similar to `GetNode()`, but does not generate an error if `path` does not point to a valid node.

### GetNodeOrNull

```csharp
public T GetNodeOrNull<T>(NodePath path)
```

引擎名：`get_node_or_null`

Similar to `GetNode()`, but does not log an error if `path`
does not point to a valid `Node`.

参数：
- `path`：The path to the node to fetch.

返回：The `Node` at the given `path`, or `null` if not found.

### GetNodeRpcConfig

```csharp
public Variant GetNodeRpcConfig()
```

引擎名：`get_node_rpc_config`

Returns a [`Collections.Dictionary`](Collections.Dictionary.md) mapping method names to their RPC configuration defined for this node using `RpcConfig()`.

**Note:** This method only returns the RPC configuration assigned via `RpcConfig()`. See [`Script.GetRpcConfig`](Script.md) to retrieve the RPCs defined by the [`Script`](Script.md).

### GetOrphanNodeIds

```csharp
public static Array<int> GetOrphanNodeIds()
```

引擎名：`get_orphan_node_ids`

Returns object IDs of all orphan nodes (nodes outside the [`SceneTree`](SceneTree.md)). Used for debugging.

**Note:** `GetOrphanNodeIds()` only works in debug builds. When called in a project exported in release mode, `GetOrphanNodeIds()` will return an empty array.

### GetOwner

```csharp
public Node GetOwner()
```

引擎名：`get_owner`

### GetOwner

```csharp
public T GetOwner<T>()
```

引擎名：`get_owner`

The node owner. A node can have any other node as owner (as long as it is
a valid parent, grandparent, etc. ascending in the tree). When saving a
node (using [`PackedScene`](PackedScene.md)), all the nodes it owns will be saved
with it. This allows for the creation of complex [`SceneTree`](SceneTree.md)s,
with instancing and subinstancing.

返回：The owner `Node`.

### GetOwnerOrNull

```csharp
public T GetOwnerOrNull<T>()
```

The node owner. A node can have any other node as owner (as long as it is
a valid parent, grandparent, etc. ascending in the tree). When saving a
node (using [`PackedScene`](PackedScene.md)), all the nodes it owns will be saved
with it. This allows for the creation of complex [`SceneTree`](SceneTree.md)s,
with instancing and subinstancing.

返回：The owner `Node`, or `null` if there is no owner.

### GetParent

```csharp
public Node GetParent()
```

引擎名：`get_parent`

Returns this node's parent node, or `null` if the node doesn't have a parent.

### GetParent

```csharp
public T GetParent<T>()
```

引擎名：`get_parent`

Returns the parent node of the current node, or a `null` instance
if the node lacks a parent.

返回：The parent `Node`.

### GetParentOrNull

```csharp
public T GetParentOrNull<T>()
```

Returns the parent node of the current node, or a `null` instance
if the node lacks a parent.

返回：The parent `Node`, or `null` if the node has no parent.

### GetPath

```csharp
public NodePath GetPath()
```

引擎名：`get_path`

Returns the node's absolute path, relative to the [`SceneTree.Root`](SceneTree.md). If the node is not inside the scene tree, this method fails and returns an empty [`NodePath`](NodePath.md).

### GetPathTo

```csharp
public NodePath GetPathTo(Node node, bool useUniquePath = false)
```

引擎名：`get_path_to`

Returns the relative [`NodePath`](NodePath.md) from this node to the specified `node`. Both nodes must be in the same [`SceneTree`](SceneTree.md) or scene hierarchy, otherwise this method fails and returns an empty [`NodePath`](NodePath.md).

If `useUniquePath` is `true`, returns the shortest path accounting for this node's unique name (see `UniqueNameInOwner`).

**Note:** If you get a relative path which starts from a unique node, the path may be longer than a normal relative path, due to the addition of the unique node's name.

### GetPhysicsInterpolationMode

```csharp
public Node.PhysicsInterpolationModeEnum GetPhysicsInterpolationMode()
```

引擎名：`get_physics_interpolation_mode`

### GetPhysicsProcessDeltaTime

```csharp
public double GetPhysicsProcessDeltaTime()
```

引擎名：`get_physics_process_delta_time`

Returns the time elapsed (in seconds) since the last physics callback. This value is identical to `_PhysicsProcess()`'s `delta` parameter, and is often consistent at run-time, unless [`Engine.PhysicsTicksPerSecond`](Engine.md) is changed. See also `NotificationPhysicsProcess`.

**Note:** The returned value will be larger than expected if running at a framerate lower than [`Engine.PhysicsTicksPerSecond`](Engine.md) / [`Engine.MaxPhysicsStepsPerFrame`](Engine.md) FPS. This is done to avoid "spiral of death" scenarios where performance would plummet due to an ever-increasing number of physics steps per frame. This behavior affects both `_Process()` and `_PhysicsProcess()`. As a result, avoid using `delta` for time measurements in real-world seconds. Use the [`Time`](Time.md) singleton's methods for this purpose instead, such as [`Time.GetTicksUsec`](Time.md).

### GetPhysicsProcessPriority

```csharp
public int GetPhysicsProcessPriority()
```

引擎名：`get_physics_process_priority`

### GetProcessDeltaTime

```csharp
public double GetProcessDeltaTime()
```

引擎名：`get_process_delta_time`

Returns the time elapsed (in seconds) since the last process callback. This value is identical to `_Process()`'s `delta` parameter, and may vary from frame to frame. See also `NotificationProcess`.

**Note:** The returned value will be larger than expected if running at a framerate lower than [`Engine.PhysicsTicksPerSecond`](Engine.md) / [`Engine.MaxPhysicsStepsPerFrame`](Engine.md) FPS. This is done to avoid "spiral of death" scenarios where performance would plummet due to an ever-increasing number of physics steps per frame. This behavior affects both `_Process()` and `_PhysicsProcess()`. As a result, avoid using `delta` for time measurements in real-world seconds. Use the [`Time`](Time.md) singleton's methods for this purpose instead, such as [`Time.GetTicksUsec`](Time.md).

### GetProcessMode

```csharp
public Node.ProcessModeEnum GetProcessMode()
```

引擎名：`get_process_mode`

### GetProcessPriority

```csharp
public int GetProcessPriority()
```

引擎名：`get_process_priority`

### GetProcessThreadGroup

```csharp
public Node.ProcessThreadGroupEnum GetProcessThreadGroup()
```

引擎名：`get_process_thread_group`

### GetProcessThreadGroupOrder

```csharp
public int GetProcessThreadGroupOrder()
```

引擎名：`get_process_thread_group_order`

### GetProcessThreadMessages

```csharp
public Node.ProcessThreadMessagesEnum GetProcessThreadMessages()
```

引擎名：`get_process_thread_messages`

### GetRpcConfig

```csharp
public Variant GetRpcConfig()
```

### GetSceneFilePath

```csharp
public string GetSceneFilePath()
```

引擎名：`get_scene_file_path`

### GetSceneInstanceLoadPlaceholder

```csharp
public bool GetSceneInstanceLoadPlaceholder()
```

引擎名：`get_scene_instance_load_placeholder`

Returns `true` if this node is an instance load placeholder. See [`InstancePlaceholder`](InstancePlaceholder.md) and `SetSceneInstanceLoadPlaceholder()`.

### GetTree

```csharp
public SceneTree GetTree()
```

引擎名：`get_tree`

Returns the [`SceneTree`](SceneTree.md) that contains this node. If this node is not inside the tree, generates an error and returns `null`. See also `IsInsideTree()`.

### GetTreeString

```csharp
public string GetTreeString()
```

引擎名：`get_tree_string`

Returns the tree as a `System.String`. Used mainly for debugging purposes. This version displays the path relative to the current node, and is good for copy/pasting into the `GetNode()` function. It also can be used in game UI/UX.

May print, for example:

```csharp
TheGame
TheGame/Menu
TheGame/Menu/Label
TheGame/Menu/Camera2D
TheGame/SplashScreen
TheGame/SplashScreen/Camera2D

```

### GetTreeStringPretty

```csharp
public string GetTreeStringPretty()
```

引擎名：`get_tree_string_pretty`

Similar to `GetTreeString()`, this returns the tree as a `System.String`. This version displays a more graphical representation similar to what is displayed in the Scene Dock. It is useful for inspecting larger trees.

May print, for example:

```csharp
┖╴TheGame
┠╴Menu
┃  ┠╴Label
┃  ┖╴Camera2D
┖╴SplashScreen
┖╴Camera2D

```

### GetViewport

```csharp
public Viewport GetViewport()
```

引擎名：`get_viewport`

Returns the node's closest [`Viewport`](Viewport.md) ancestor, if the node is inside the tree. Otherwise, returns `null`.

### GetWindow

```csharp
public Window GetWindow()
```

引擎名：`get_window`

Returns the [`Window`](Window.md) that contains this node. If the node is in the main window, this is equivalent to getting the root node (`get_tree().get_root()`).

### HasNode

```csharp
public bool HasNode(NodePath path)
```

引擎名：`has_node`

Returns `true` if the `path` points to a valid node. See also `GetNode()`.

### HasNodeAndResource

```csharp
public bool HasNodeAndResource(NodePath path)
```

引擎名：`has_node_and_resource`

Returns `true` if `path` points to a valid node and its subnames point to a valid [`Resource`](Resource.md), e.g. `Area2D/CollisionShape2D:shape`. Properties that are not [`Resource`](Resource.md) types (such as nodes or other [`Variant`](Variant.md) types) are not considered. See also `GetNodeAndResource()`.

### IsAncestorOf

```csharp
public bool IsAncestorOf(Node node)
```

引擎名：`is_ancestor_of`

Returns `true` if the given `node` is a direct or indirect child of this node.

### IsDisplayedFolded

```csharp
public bool IsDisplayedFolded()
```

引擎名：`is_displayed_folded`

Returns `true` if the node is folded (collapsed) in the Scene dock. This method is intended to be used in editor plugins and tools. See also `SetDisplayFolded()`.

### IsEditableInstance

```csharp
public bool IsEditableInstance(Node node)
```

引擎名：`is_editable_instance`

Returns `true` if `node` has editable children enabled relative to this node. This method is intended to be used in editor plugins and tools. See also `SetEditableInstance()`.

### IsGreaterThan

```csharp
public bool IsGreaterThan(Node node)
```

引擎名：`is_greater_than`

Returns `true` if the given `node` occurs later in the scene hierarchy than this node. A node occurring later is usually processed last.

### IsInGroup

```csharp
public bool IsInGroup(StringName group)
```

引擎名：`is_in_group`

Returns `true` if this node has been added to the given `group`. See `AddToGroup()` and `RemoveFromGroup()`. See also notes in the description, and the [`SceneTree`](SceneTree.md)'s group methods.

### IsInsideTree

```csharp
public bool IsInsideTree()
```

引擎名：`is_inside_tree`

Returns `true` if this node is currently inside a [`SceneTree`](SceneTree.md). See also `GetTree()`.

### IsMultiplayerAuthority

```csharp
public bool IsMultiplayerAuthority()
```

引擎名：`is_multiplayer_authority`

Returns `true` if the local system is the multiplayer authority of this node.

### IsNodeReady

```csharp
public bool IsNodeReady()
```

引擎名：`is_node_ready`

Returns `true` if the node is ready, i.e. it's inside scene tree and all its children are initialized.

`RequestReady()` resets it back to `false`.

### IsPartOfEditedScene

```csharp
public bool IsPartOfEditedScene()
```

引擎名：`is_part_of_edited_scene`

Returns `true` if the node is part of the scene currently opened in the editor.

### IsPhysicsInterpolated

```csharp
public bool IsPhysicsInterpolated()
```

引擎名：`is_physics_interpolated`

Returns `true` if physics interpolation is enabled for this node (see `PhysicsInterpolationMode`).

**Note:** Interpolation will only be active if both the flag is set **and** physics interpolation is enabled within the [`SceneTree`](SceneTree.md). This can be tested using `IsPhysicsInterpolatedAndEnabled()`.

### IsPhysicsInterpolatedAndEnabled

```csharp
public bool IsPhysicsInterpolatedAndEnabled()
```

引擎名：`is_physics_interpolated_and_enabled`

Returns `true` if physics interpolation is enabled (see `PhysicsInterpolationMode`) **and** enabled in the [`SceneTree`](SceneTree.md).

This is a convenience version of `IsPhysicsInterpolated()` that also checks whether physics interpolation is enabled globally.

See [`SceneTree.PhysicsInterpolation`](SceneTree.md) and `ProjectSettings.physics/common/physics_interpolation`.

### IsPhysicsProcessing

```csharp
public bool IsPhysicsProcessing()
```

引擎名：`is_physics_processing`

Returns `true` if physics processing is enabled (see `SetPhysicsProcess()`).

### IsPhysicsProcessingInternal

```csharp
public bool IsPhysicsProcessingInternal()
```

引擎名：`is_physics_processing_internal`

Returns `true` if internal physics processing is enabled (see `SetPhysicsProcessInternal()`).

### IsProcessing

```csharp
public bool IsProcessing()
```

引擎名：`is_processing`

Returns `true` if processing is enabled (see `SetProcess()`).

### IsProcessingInput

```csharp
public bool IsProcessingInput()
```

引擎名：`is_processing_input`

Returns `true` if the node is processing input (see `SetProcessInput()`).

### IsProcessingInternal

```csharp
public bool IsProcessingInternal()
```

引擎名：`is_processing_internal`

Returns `true` if internal processing is enabled (see `SetProcessInternal()`).

### IsProcessingShortcutInput

```csharp
public bool IsProcessingShortcutInput()
```

引擎名：`is_processing_shortcut_input`

Returns `true` if the node is processing shortcuts (see `SetProcessShortcutInput()`).

### IsProcessingUnhandledInput

```csharp
public bool IsProcessingUnhandledInput()
```

引擎名：`is_processing_unhandled_input`

Returns `true` if the node is processing unhandled input (see `SetProcessUnhandledInput()`).

### IsProcessingUnhandledKeyInput

```csharp
public bool IsProcessingUnhandledKeyInput()
```

引擎名：`is_processing_unhandled_key_input`

Returns `true` if the node is processing unhandled key input (see `SetProcessUnhandledKeyInput()`).

### IsUniqueNameInOwner

```csharp
public bool IsUniqueNameInOwner()
```

引擎名：`is_unique_name_in_owner`

### MoveChild

```csharp
public void MoveChild(Node childNode, int toIndex)
```

引擎名：`move_child`

Moves `childNode` to the given index. A node's index is the order among its siblings. If `toIndex` is negative, the index is counted from the end of the list. See also `GetChild()` and `GetIndex()`.

**Note:** The processing order of several engine callbacks (`_Ready()`, `_Process()`, etc.) and notifications sent through `PropagateNotification()` is affected by tree order. [`CanvasItem`](CanvasItem.md) nodes are also rendered in tree order. See also `ProcessPriority`.

### NotifyDeferredThreadGroup

```csharp
public void NotifyDeferredThreadGroup(int what)
```

引擎名：`notify_deferred_thread_group`

Similar to `CallDeferredThreadGroup()`, but for notifications.

### NotifyThreadSafe

```csharp
public void NotifyThreadSafe(int what)
```

引擎名：`notify_thread_safe`

Similar to `CallThreadSafe()`, but for notifications.

### PrintOrphanNodes

```csharp
public static void PrintOrphanNodes()
```

引擎名：`print_orphan_nodes`

Prints all orphan nodes (nodes outside the [`SceneTree`](SceneTree.md)). Useful for debugging.

**Note:** This method only works in debug builds. It does nothing in a project exported in release mode.

### PrintTree

```csharp
public void PrintTree()
```

引擎名：`print_tree`

Prints the node and its children to the console, recursively. The node does not have to be inside the tree. This method outputs [`NodePath`](NodePath.md)s relative to this node, and is good for copy/pasting into `GetNode()`. See also `PrintTreePretty()`.

May print, for example:

```csharp
.
Menu
Menu/Label
Menu/Camera2D
SplashScreen
SplashScreen/Camera2D

```

### PrintTreePretty

```csharp
public void PrintTreePretty()
```

引擎名：`print_tree_pretty`

Prints the node and its children to the console, recursively. The node does not have to be inside the tree. Similar to `PrintTree()`, but the graphical representation looks like what is displayed in the editor's Scene dock. It is useful for inspecting larger trees.

May print, for example:

```csharp
┖╴TheGame
┠╴Menu
┃  ┠╴Label
┃  ┖╴Camera2D
┖╴SplashScreen
┖╴Camera2D

```

### PropagateCall

```csharp
public void PropagateCall(StringName method, Array args = null, bool parentFirst = false)
```

引擎名：`propagate_call`

Calls the given `method` name, passing `args` as arguments, on this node and all of its children, recursively.

If `parentFirst` is `true`, the method is called on this node first, then on all of its children. If `false`, the children's methods are called first.

### PropagateNotification

```csharp
public void PropagateNotification(int what)
```

引擎名：`propagate_notification`

Calls [`GodotObject.Notification`](GodotObject.md) with `what` on this node and all of its children, recursively.

### QueueAccessibilityUpdate

```csharp
public void QueueAccessibilityUpdate()
```

引擎名：`queue_accessibility_update`

Queues an accessibility information update for this node.

### QueueFree

```csharp
public void QueueFree()
```

引擎名：`queue_free`

Queues this node to be deleted at the end of the current frame. When deleted, all of its children are deleted as well, and all references to the node and its children become invalid.

Unlike with [`GodotObject.Free`](GodotObject.md), the node is not deleted instantly, and it can still be accessed before deletion. It is also safe to call `QueueFree()` multiple times. Use [`GodotObject.IsQueuedForDeletion`](GodotObject.md) to check if the node will be deleted at the end of the frame.

**Note:** The node will only be freed after all other deferred calls are finished. Using this method is not always the same as calling [`GodotObject.Free`](GodotObject.md) through [`GodotObject.CallDeferred`](GodotObject.md).

### RemoveChild

```csharp
public void RemoveChild(Node node)
```

引擎名：`remove_child`

Removes a child `node`. The `node`, along with its children, are **not** deleted. To delete a node, see `QueueFree()`.

**Note:** When this node is inside the tree, this method sets the `Owner` of the removed `node` (or its descendants) to `null`, if their `Owner` is no longer an ancestor (see `IsAncestorOf()`).

### RemoveFromGroup

```csharp
public void RemoveFromGroup(StringName group)
```

引擎名：`remove_from_group`

Removes the node from the given `group`. Does nothing if the node is not in the `group`. See also notes in the description, and the [`SceneTree`](SceneTree.md)'s group methods.

### Reparent

```csharp
public void Reparent(Node newParent, bool keepGlobalTransform = true)
```

引擎名：`reparent`

Changes the parent of this `Node` to the `newParent`. The node needs to already have a parent. The node's `Owner` is preserved if its owner is still reachable from the new location (i.e., the node is still a descendant of the new parent after the operation).

If `keepGlobalTransform` is `true`, the node's global transform will be preserved if supported. [`Node2D`](Node2D.md), [`Node3D`](Node3D.md) and [`Control`](Control.md) support this argument (but [`Control`](Control.md) keeps only position).

**Warning:** If `ProjectSettings.physics/common/physics_interpolation` is enabled and reparenting causes a large change in global transform, the object may appear to move from its old position to its new one over the next physics tick. To avoid this, call `ResetPhysicsInterpolation()` after reparenting.

### ReplaceBy

```csharp
public void ReplaceBy(Node node, bool keepGroups = false)
```

引擎名：`replace_by`

Replaces this node by the given `node`. All children of this node are moved to `node`.

If `keepGroups` is `true`, the `node` is added to the same groups that the replaced node is in (see `AddToGroup()`).

**Warning:** The replaced node is removed from the tree, but it is **not** deleted. To prevent memory leaks, store a reference to the node in a variable, or use [`GodotObject.Free`](GodotObject.md).

### RequestReady

```csharp
public void RequestReady()
```

引擎名：`request_ready`

Requests `_Ready()` to be called again the next time the node enters the tree. Does **not** immediately call `_Ready()`.

**Note:** This method only affects the current node. If the node's children also need to request ready, this method needs to be called for each one of them. When the node and its children enter the tree again, the order of `_Ready()` callbacks will be the same as normal.

### ResetPhysicsInterpolation

```csharp
public void ResetPhysicsInterpolation()
```

引擎名：`reset_physics_interpolation`

When physics interpolation is active, moving a node to a radically different transform (such as placement within a level) can result in a visible glitch as the object is rendered moving from the old to new position over the physics tick.

That glitch can be prevented by calling this method, which temporarily disables interpolation until the physics tick is complete.

The notification `NotificationResetPhysicsInterpolation` will be received by the node and all children recursively.

**Note:** This function should be called **after** moving the node, rather than before.

### Rpc

```csharp
public Error Rpc(StringName method, ReadOnlySpan<Variant> args)
```

引擎名：`rpc`

Sends a remote procedure call request for the given `method` to peers on the network (and locally), sending additional arguments to the method called by the RPC. The call request will only be received by nodes with the same [`NodePath`](NodePath.md), including the exact same `Name`. Behavior depends on the RPC configuration for the given `method` (see `RpcConfig()` and [annotation @GDScript.@rpc]). By default, methods are not exposed to RPCs.

May return [`Error.Ok`](Error.md) if the call is successful, [`Error.InvalidParameter`](Error.md) if the arguments passed in the `method` do not match, [`Error.Unconfigured`](Error.md) if the node's `Multiplayer` cannot be fetched (such as when the node is not inside the tree), [`Error.ConnectionError`](Error.md) if `Multiplayer`'s connection is not available.

**Note:** You can only safely use RPCs on clients after you received the [`MultiplayerApi.ConnectedToServer`](MultiplayerApi.md) signal from the [`MultiplayerApi`](MultiplayerApi.md). You also need to keep track of the connection state, either by the [`MultiplayerApi`](MultiplayerApi.md) signals like [`MultiplayerApi.ServerDisconnected`](MultiplayerApi.md) or by checking (`get_multiplayer().peer.get_connection_status() == CONNECTION_CONNECTED`).

### Rpc

```csharp
public Error Rpc(StringName method, params Variant[] args)
```

引擎名：`rpc`

Sends a remote procedure call request for the given `method` to peers on the network (and locally), sending additional arguments to the method called by the RPC. The call request will only be received by nodes with the same [`NodePath`](NodePath.md), including the exact same `Name`. Behavior depends on the RPC configuration for the given `method` (see `RpcConfig()` and [annotation @GDScript.@rpc]). By default, methods are not exposed to RPCs.

May return [`Error.Ok`](Error.md) if the call is successful, [`Error.InvalidParameter`](Error.md) if the arguments passed in the `method` do not match, [`Error.Unconfigured`](Error.md) if the node's `Multiplayer` cannot be fetched (such as when the node is not inside the tree), [`Error.ConnectionError`](Error.md) if `Multiplayer`'s connection is not available.

**Note:** You can only safely use RPCs on clients after you received the [`MultiplayerApi.ConnectedToServer`](MultiplayerApi.md) signal from the [`MultiplayerApi`](MultiplayerApi.md). You also need to keep track of the connection state, either by the [`MultiplayerApi`](MultiplayerApi.md) signals like [`MultiplayerApi.ServerDisconnected`](MultiplayerApi.md) or by checking (`get_multiplayer().peer.get_connection_status() == CONNECTION_CONNECTED`).

### RpcConfig

```csharp
public void RpcConfig(StringName method, Variant config)
```

引擎名：`rpc_config`

Changes the RPC configuration for the given `method`. `config` should either be `null` to disable the feature (as by default), or a [`Collections.Dictionary`](Collections.Dictionary.md) containing the following entries:

- `rpc_mode`: see [`MultiplayerApi.RpcMode`](MultiplayerApi.md);

- `transfer_mode`: see [`MultiplayerPeer.TransferModeEnum`](MultiplayerPeer.md);

- `call_local`: if `true`, the method will also be called locally;

- `channel`: an `System.Int32` representing the channel to send the RPC on.

**Note:** In GDScript, this method corresponds to the [annotation @GDScript.@rpc] annotation, with various parameters passed (`@rpc(any)`, `@rpc(authority)`...). See also the [high-level multiplayer]($DOCS_URL/tutorials/networking/high_level_multiplayer.html) tutorial.

### RpcId

```csharp
public Error RpcId(long peerId, StringName method, ReadOnlySpan<Variant> args)
```

引擎名：`rpc_id`

Sends a `Rpc()` to a specific peer identified by `peerId` (see [`MultiplayerPeer.SetTargetPeer`](MultiplayerPeer.md)).

May return [`Error.Ok`](Error.md) if the call is successful, [`Error.InvalidParameter`](Error.md) if the arguments passed in the `method` do not match, [`Error.Unconfigured`](Error.md) if the node's `Multiplayer` cannot be fetched (such as when the node is not inside the tree), [`Error.ConnectionError`](Error.md) if `Multiplayer`'s connection is not available.

### RpcId

```csharp
public Error RpcId(long peerId, StringName method, params Variant[] args)
```

引擎名：`rpc_id`

Sends a `Rpc()` to a specific peer identified by `peerId` (see [`MultiplayerPeer.SetTargetPeer`](MultiplayerPeer.md)).

May return [`Error.Ok`](Error.md) if the call is successful, [`Error.InvalidParameter`](Error.md) if the arguments passed in the `method` do not match, [`Error.Unconfigured`](Error.md) if the node's `Multiplayer` cannot be fetched (such as when the node is not inside the tree), [`Error.ConnectionError`](Error.md) if `Multiplayer`'s connection is not available.

### SetAutoTranslateMode

```csharp
public void SetAutoTranslateMode(Node.AutoTranslateModeEnum mode)
```

引擎名：`set_auto_translate_mode`

### SetDeferredThreadGroup

```csharp
public void SetDeferredThreadGroup(StringName property, Variant value)
```

引擎名：`set_deferred_thread_group`

Similar to `CallDeferredThreadGroup()`, but for setting properties.

### SetDisplayFolded

```csharp
public void SetDisplayFolded(bool fold)
```

引擎名：`set_display_folded`

If set to `true`, the node appears folded in the Scene dock. As a result, all of its children are hidden. This method is intended to be used in editor plugins and tools, but it also works in release builds. See also `IsDisplayedFolded()`.

### SetEditableInstance

```csharp
public void SetEditableInstance(Node node, bool isEditable)
```

引擎名：`set_editable_instance`

Set to `true` to allow all nodes owned by `node` to be available, and editable, in the Scene dock, even if their `Owner` is not the scene root. This method is intended to be used in editor plugins and tools, but it also works in release builds. See also `IsEditableInstance()`.

### SetEditorDescription

```csharp
public void SetEditorDescription(string editorDescription)
```

引擎名：`set_editor_description`

### SetMultiplayerAuthority

```csharp
public void SetMultiplayerAuthority(int id, bool recursive = true)
```

引擎名：`set_multiplayer_authority`

Sets the node's multiplayer authority to the peer with the given peer `id`. The multiplayer authority is the peer that has authority over the node on the network. Defaults to peer ID 1 (the server). Useful in conjunction with `RpcConfig()` and the [`MultiplayerApi`](MultiplayerApi.md).

If `recursive` is `true`, the given peer is recursively set as the authority for all children of this node.

**Warning:** This does **not** automatically replicate the new authority to other peers. It is the developer's responsibility to do so. You may replicate the new authority's information using [`MultiplayerSpawner.SpawnFunction`](MultiplayerSpawner.md), an RPC, or a [`MultiplayerSynchronizer`](MultiplayerSynchronizer.md). Furthermore, the parent's authority does **not** propagate to newly added children.

### SetName

```csharp
public void SetName(StringName name)
```

引擎名：`set_name`

### SetName

```csharp
public void SetName(string name)
```

引擎名：`set_name`

### SetOwner

```csharp
public void SetOwner(Node owner)
```

引擎名：`set_owner`

### SetPhysicsInterpolationMode

```csharp
public void SetPhysicsInterpolationMode(Node.PhysicsInterpolationModeEnum mode)
```

引擎名：`set_physics_interpolation_mode`

### SetPhysicsProcess

```csharp
public void SetPhysicsProcess(bool enable)
```

引擎名：`set_physics_process`

If set to `true`, enables physics (fixed framerate) processing. When a node is being processed, it will receive a `NotificationPhysicsProcess` at a fixed (usually 60 FPS, see [`Engine.PhysicsTicksPerSecond`](Engine.md) to change) interval (and the `_PhysicsProcess()` callback will be called if it exists).

**Note:** If `_PhysicsProcess()` is overridden, this will be automatically enabled before `_Ready()` is called.

### SetPhysicsProcessInternal

```csharp
public void SetPhysicsProcessInternal(bool enable)
```

引擎名：`set_physics_process_internal`

If set to `true`, enables internal physics for this node. Internal physics processing happens in isolation from the normal `_PhysicsProcess()` calls and is used by some nodes internally to guarantee proper functioning even if the node is paused or physics processing is disabled for scripting (`SetPhysicsProcess()`).

**Warning:** Built-in nodes rely on internal processing for their internal logic. Disabling it is unsafe and may lead to unexpected behavior. Use this method if you know what you are doing.

### SetPhysicsProcessPriority

```csharp
public void SetPhysicsProcessPriority(int priority)
```

引擎名：`set_physics_process_priority`

### SetProcess

```csharp
public void SetProcess(bool enable)
```

引擎名：`set_process`

If set to `true`, enables processing. When a node is being processed, it will receive a `NotificationProcess` on every drawn frame (and the `_Process()` callback will be called if it exists).

**Note:** If `_Process()` is overridden, this will be automatically enabled before `_Ready()` is called.

**Note:** This method only affects the `_Process()` callback, i.e. it has no effect on other callbacks like `_PhysicsProcess()`. If you want to disable all processing for the node, set `ProcessMode` to [`Node.ProcessModeEnum.Disabled`](Node.md).

### SetProcessInput

```csharp
public void SetProcessInput(bool enable)
```

引擎名：`set_process_input`

If set to `true`, enables input processing.

**Note:** If `_Input()` is overridden, this will be automatically enabled before `_Ready()` is called. Input processing is also already enabled for GUI controls, such as [`Button`](Button.md) and [`TextEdit`](TextEdit.md).

### SetProcessInternal

```csharp
public void SetProcessInternal(bool enable)
```

引擎名：`set_process_internal`

If set to `true`, enables internal processing for this node. Internal processing happens in isolation from the normal `_Process()` calls and is used by some nodes internally to guarantee proper functioning even if the node is paused or processing is disabled for scripting (`SetProcess()`).

**Warning:** Built-in nodes rely on internal processing for their internal logic. Disabling it is unsafe and may lead to unexpected behavior. Use this method if you know what you are doing.

### SetProcessMode

```csharp
public void SetProcessMode(Node.ProcessModeEnum mode)
```

引擎名：`set_process_mode`

### SetProcessPriority

```csharp
public void SetProcessPriority(int priority)
```

引擎名：`set_process_priority`

### SetProcessShortcutInput

```csharp
public void SetProcessShortcutInput(bool enable)
```

引擎名：`set_process_shortcut_input`

If set to `true`, enables shortcut processing for this node.

**Note:** If `_ShortcutInput()` is overridden, this will be automatically enabled before `_Ready()` is called.

### SetProcessThreadGroup

```csharp
public void SetProcessThreadGroup(Node.ProcessThreadGroupEnum mode)
```

引擎名：`set_process_thread_group`

### SetProcessThreadGroupOrder

```csharp
public void SetProcessThreadGroupOrder(int order)
```

引擎名：`set_process_thread_group_order`

### SetProcessThreadMessages

```csharp
public void SetProcessThreadMessages(Node.ProcessThreadMessagesEnum flags)
```

引擎名：`set_process_thread_messages`

### SetProcessUnhandledInput

```csharp
public void SetProcessUnhandledInput(bool enable)
```

引擎名：`set_process_unhandled_input`

If set to `true`, enables unhandled input processing. It enables the node to receive all input that was not previously handled (usually by a [`Control`](Control.md)).

**Note:** If `_UnhandledInput()` is overridden, this will be automatically enabled before `_Ready()` is called. Unhandled input processing is also already enabled for GUI controls, such as [`Button`](Button.md) and [`TextEdit`](TextEdit.md).

### SetProcessUnhandledKeyInput

```csharp
public void SetProcessUnhandledKeyInput(bool enable)
```

引擎名：`set_process_unhandled_key_input`

If set to `true`, enables unhandled key input processing.

**Note:** If `_UnhandledKeyInput()` is overridden, this will be automatically enabled before `_Ready()` is called.

### SetSceneFilePath

```csharp
public void SetSceneFilePath(string sceneFilePath)
```

引擎名：`set_scene_file_path`

### SetSceneInstanceLoadPlaceholder

```csharp
public void SetSceneInstanceLoadPlaceholder(bool loadPlaceholder)
```

引擎名：`set_scene_instance_load_placeholder`

If set to `true`, the node becomes an [`InstancePlaceholder`](InstancePlaceholder.md) when packed and instantiated from a [`PackedScene`](PackedScene.md). See also `GetSceneInstanceLoadPlaceholder()`.

### SetThreadSafe

```csharp
public void SetThreadSafe(StringName property, Variant value)
```

引擎名：`set_thread_safe`

Similar to `CallThreadSafe()`, but for setting properties.

### SetTranslationDomainInherited

```csharp
public void SetTranslationDomainInherited()
```

引擎名：`set_translation_domain_inherited`

Makes this node inherit the translation domain from its parent node. If this node has no parent, the main translation domain will be used.

This is the default behavior for all nodes. Calling [`GodotObject.SetTranslationDomain`](GodotObject.md) disables this behavior.

### SetUniqueNameInOwner

```csharp
public void SetUniqueNameInOwner(bool enable)
```

引擎名：`set_unique_name_in_owner`

### UpdateConfigurationWarnings

```csharp
public void UpdateConfigurationWarnings()
```

引擎名：`update_configuration_warnings`

Refreshes the warnings displayed for this node in the Scene dock. Use `_GetConfigurationWarnings()` to customize the warning messages to display.

### _EnterTree

```csharp
public virtual void _EnterTree()
```

引擎名：`_enter_tree`

Called when the node enters the [`SceneTree`](SceneTree.md) (e.g. upon instantiating, scene changing, or after calling `AddChild()` in a script). If the node has children, its `_EnterTree()` callback will be called first, and then that of the children.

Corresponds to the `NotificationEnterTree` notification in [`GodotObject.Notification`](GodotObject.md).

### _ExitTree

```csharp
public virtual void _ExitTree()
```

引擎名：`_exit_tree`

Called when the node is about to leave the [`SceneTree`](SceneTree.md) (e.g. upon freeing, scene changing, or after calling `RemoveChild()` in a script). If the node has children, its `_ExitTree()` callback will be called last, after all its children have left the tree.

Corresponds to the `NotificationExitTree` notification in [`GodotObject.Notification`](GodotObject.md) and signal `TreeExiting`. To get notified when the node has already left the active tree, connect to the `TreeExited`.

### _GetAccessibilityConfigurationWarnings

```csharp
public virtual string[] _GetAccessibilityConfigurationWarnings()
```

引擎名：`_get_accessibility_configuration_warnings`

The elements in the array returned from this method are displayed as warnings in the Scene dock if the script that overrides it is a `tool` script, and accessibility warnings are enabled in the editor settings.

Returning an empty array produces no warnings.

### _GetConfigurationWarnings

```csharp
public virtual string[] _GetConfigurationWarnings()
```

引擎名：`_get_configuration_warnings`

The elements in the array returned from this method are displayed as warnings in the Scene dock if the script that overrides it is a `tool` script.

Returning an empty array produces no warnings.

Call `UpdateConfigurationWarnings()` when the warnings need to be updated for this node.

```csharp
@export var energy = 0:
set(value):
energy = value
update_configuration_warnings()

func _get_configuration_warnings():
if energy < 0:
return ["Energy must be 0 or greater."]
else:
return []

```

### _GetFocusedAccessibilityElement

```csharp
public virtual Rid _GetFocusedAccessibilityElement()
```

引擎名：`_get_focused_accessibility_element`

Called during accessibility information updates to determine the currently focused sub-element, should return a sub-element RID or the value returned by `GetAccessibilityElement()`.

### _Input

```csharp
public virtual void _Input(InputEvent event)
```

引擎名：`_input`

Called when there is an input event. The input event propagates up through the node tree until a node consumes it.

It is only called if input processing is enabled, which is done automatically if this method is overridden, and can be toggled with `SetProcessInput()`.

To consume the input event and stop it propagating further to other nodes, [`Viewport.SetInputAsHandled`](Viewport.md) can be called.

For gameplay input, `_UnhandledInput()` and `_UnhandledKeyInput()` are usually a better fit as they allow the GUI to intercept the events first.

**Note:** This method is only called if the node is present in the scene tree (i.e. if it's not an orphan).

### _PhysicsProcess

```csharp
public virtual void _PhysicsProcess(double delta)
```

引擎名：`_physics_process`

Called once on each physics tick, and allows Nodes to synchronize their logic with physics ticks. `delta` is the logical time between physics ticks in seconds and is equal to [`Engine.TimeScale`](Engine.md) / [`Engine.PhysicsTicksPerSecond`](Engine.md).

It is only called if physics processing is enabled for this Node, which is done automatically if this method is overridden, and can be toggled with `SetPhysicsProcess()`.

Processing happens in order of `ProcessPhysicsPriority`, lower priority values are called first. Nodes with the same priority are processed in tree order, or top to bottom as seen in the editor (also known as pre-order traversal).

Corresponds to the `NotificationPhysicsProcess` notification in [`GodotObject.Notification`](GodotObject.md).

**Note:** This method is only called if the node is present in the scene tree (i.e. if it's not an orphan).

**Note:** Accumulated `delta` may diverge from real world seconds.

### _Process

```csharp
public virtual void _Process(double delta)
```

引擎名：`_process`

Called on each idle frame, prior to rendering, and after physics ticks have been processed. `delta` is the time between frames in seconds.

It is only called if processing is enabled for this Node, which is done automatically if this method is overridden, and can be toggled with `SetProcess()`.

Processing happens in order of `ProcessPriority`, lower priority values are called first. Nodes with the same priority are processed in tree order, or top to bottom as seen in the editor (also known as pre-order traversal).

Corresponds to the `NotificationProcess` notification in [`GodotObject.Notification`](GodotObject.md).

**Note:** This method is only called if the node is present in the scene tree (i.e. if it's not an orphan).

**Note:** When the engine is struggling and the frame rate is lowered, `delta` will increase. When `delta` is increased, it's capped at a maximum of [`Engine.TimeScale`](Engine.md) * [`Engine.MaxPhysicsStepsPerFrame`](Engine.md) / [`Engine.PhysicsTicksPerSecond`](Engine.md). As a result, accumulated `delta` may not represent real world time.

**Note:** When `--fixed-fps` is enabled or the engine is running in Movie Maker mode (see [`MovieWriter`](MovieWriter.md)), process `delta` will always be the same for every frame, regardless of how much time the frame took to render.

**Note:** Frame delta may be post-processed by [`OS.DeltaSmoothing`](OS.md) if this is enabled for the project.

### _Ready

```csharp
public virtual void _Ready()
```

引擎名：`_ready`

Called when the node is "ready", i.e. when both the node and its children have entered the scene tree. If the node has children, their `_Ready()` callbacks get triggered first, and the parent node will receive the ready notification afterwards.

Corresponds to the `NotificationReady` notification in [`GodotObject.Notification`](GodotObject.md). See also the `@onready` annotation for variables.

Usually used for initialization. For even earlier initialization, [`GodotObject`](GodotObject.md) may be used. See also `_EnterTree()`.

**Note:** This method may be called only once for each node. After removing a node from the scene tree and adding it again, `_Ready()` will **not** be called a second time. This can be bypassed by requesting another call with `RequestReady()`, which may be called anywhere before adding the node again.

### _ShortcutInput

```csharp
public virtual void _ShortcutInput(InputEvent event)
```

引擎名：`_shortcut_input`

Called when an [`InputEventKey`](InputEventKey.md), [`InputEventShortcut`](InputEventShortcut.md), or [`InputEventJoypadButton`](InputEventJoypadButton.md) hasn't been consumed by `_Input()` or any GUI [`Control`](Control.md) item. It is called before `_UnhandledKeyInput()` and `_UnhandledInput()`. The input event propagates up through the node tree until a node consumes it.

It is only called if shortcut processing is enabled, which is done automatically if this method is overridden, and can be toggled with `SetProcessShortcutInput()`.

To consume the input event and stop it propagating further to other nodes, [`Viewport.SetInputAsHandled`](Viewport.md) can be called.

This method can be used to handle shortcuts. For generic GUI events, use `_Input()` instead. Gameplay events should usually be handled with either `_UnhandledInput()` or `_UnhandledKeyInput()`.

**Note:** This method is only called if the node is present in the scene tree (i.e. if it's not orphan).

### _UnhandledInput

```csharp
public virtual void _UnhandledInput(InputEvent event)
```

引擎名：`_unhandled_input`

Called when an [`InputEvent`](InputEvent.md) hasn't been consumed by `_Input()` or any GUI [`Control`](Control.md) item. It is called after `_ShortcutInput()` and after `_UnhandledKeyInput()`. The input event propagates up through the node tree until a node consumes it.

It is only called if unhandled input processing is enabled, which is done automatically if this method is overridden, and can be toggled with `SetProcessUnhandledInput()`.

To consume the input event and stop it propagating further to other nodes, [`Viewport.SetInputAsHandled`](Viewport.md) can be called.

For gameplay input, this method is usually a better fit than `_Input()`, as GUI events need a higher priority. For keyboard shortcuts, consider using `_ShortcutInput()` instead, as it is called before this method. Finally, to handle keyboard events, consider using `_UnhandledKeyInput()` for performance reasons.

**Note:** This method is only called if the node is present in the scene tree (i.e. if it's not an orphan).

### _UnhandledKeyInput

```csharp
public virtual void _UnhandledKeyInput(InputEvent event)
```

引擎名：`_unhandled_key_input`

Called when an [`InputEventKey`](InputEventKey.md) hasn't been consumed by `_Input()` or any GUI [`Control`](Control.md) item. It is called after `_ShortcutInput()` but before `_UnhandledInput()`. The input event propagates up through the node tree until a node consumes it.

It is only called if unhandled key input processing is enabled, which is done automatically if this method is overridden, and can be toggled with `SetProcessUnhandledKeyInput()`.

To consume the input event and stop it propagating further to other nodes, [`Viewport.SetInputAsHandled`](Viewport.md) can be called.

This method can be used to handle Unicode character input with Alt, Alt + Ctrl, and Alt + Shift modifiers, after shortcuts were handled.

For gameplay input, this and `_UnhandledInput()` are usually a better fit than `_Input()`, as GUI events should be handled first. This method also performs better than `_UnhandledInput()`, since unrelated events such as [`InputEventMouseMotion`](InputEventMouseMotion.md) are automatically filtered. For shortcuts, consider using `_ShortcutInput()` instead.

**Note:** This method is only called if the node is present in the scene tree (i.e. if it's not an orphan).

## 事件（信号）

### ChildEnteredTree

```csharp
public event Node.ChildEnteredTreeEventHandler ChildEnteredTree
```

引擎名：`child_entered_tree`

Emitted when the child `node` enters the [`SceneTree`](SceneTree.md), usually because this node entered the tree (see `TreeEntered`), or `AddChild()` has been called.

This signal is emitted *after* the child node's own `NotificationEnterTree` and `TreeEntered`.

### ChildExitingTree

```csharp
public event Node.ChildExitingTreeEventHandler ChildExitingTree
```

引擎名：`child_exiting_tree`

Emitted when the child `node` is about to exit the [`SceneTree`](SceneTree.md), usually because this node is exiting the tree (see `TreeExiting`), or because the child `node` is being removed or freed.

When this signal is received, the child `node` is still accessible inside the tree. This signal is emitted *after* the child node's own `TreeExiting` and `NotificationExitTree`.

### ChildOrderChanged

```csharp
public event Action ChildOrderChanged
```

引擎名：`child_order_changed`

Emitted when the list of children is changed. This happens when child nodes are added, moved or removed.

### EditorDescriptionChanged

```csharp
public event Node.EditorDescriptionChangedEventHandler EditorDescriptionChanged
```

引擎名：`editor_description_changed`

Emitted when the node's editor description field changed.

### EditorStateChanged

```csharp
public event Action EditorStateChanged
```

引擎名：`editor_state_changed`

Emitted when an attribute of the node that is relevant to the editor is changed. Only emitted in the editor.

### Ready

```csharp
public event Action Ready
```

引擎名：`ready`

Emitted when the node is considered ready, after `_Ready()` is called.

### Renamed

```csharp
public event Action Renamed
```

引擎名：`renamed`

Emitted when the node's `Name` is changed, if the node is inside the tree.

### ReplacingBy

```csharp
public event Node.ReplacingByEventHandler ReplacingBy
```

引擎名：`replacing_by`

Emitted when this node is being replaced by the `node`, see `ReplaceBy()`.

This signal is emitted *after* `node` has been added as a child of the original parent node, but *before* all original child nodes have been reparented to `node`.

### TreeEntered

```csharp
public event Action TreeEntered
```

引擎名：`tree_entered`

Emitted when the node enters the tree.

This signal is emitted *after* the related `NotificationEnterTree` notification.

### TreeExited

```csharp
public event Action TreeExited
```

引擎名：`tree_exited`

Emitted after the node exits the tree and is no longer active.

This signal is emitted *after* the related `NotificationExitTree` notification.

### TreeExiting

```csharp
public event Action TreeExiting
```

引擎名：`tree_exiting`

Emitted when the node is just about to exit the tree. The node is still valid. As such, this is the right place for de-initialization (or a "destructor", if you will).

This signal is emitted *after* the node's `_ExitTree()`, and *before* the related `NotificationExitTree`.

## 常量

### NotificationAccessibilityInvalidate

```csharp
public const long NotificationAccessibilityInvalidate = 3001
```

Notification received when accessibility elements are invalidated. All node accessibility elements are automatically deleted after receiving this message, therefore all existing references to such elements should be discarded.

### NotificationAccessibilityUpdate

```csharp
public const long NotificationAccessibilityUpdate = 3000
```

Notification received when an accessibility information update is required.

### NotificationApplicationFocusIn

```csharp
public const long NotificationApplicationFocusIn = 2016
```

Notification received from the OS when the application is focused, i.e. when changing the focus from the OS desktop or a thirdparty application to any open window of the Godot instance.

Implemented on desktop and mobile platforms.

### NotificationApplicationFocusOut

```csharp
public const long NotificationApplicationFocusOut = 2017
```

Notification received from the OS when the application is defocused, i.e. when changing the focus from any open window of the Godot instance to the OS desktop or a thirdparty application.

Implemented on desktop and mobile platforms.

### NotificationApplicationPaused

```csharp
public const long NotificationApplicationPaused = 2015
```

Notification received from the OS when the application is paused.

Specific to the Android and iOS platforms.

**Note:** On iOS, you only have approximately 5 seconds to finish a task started by this signal. If you go over this allotment, iOS will kill the app instead of pausing it.

### NotificationApplicationPipModeEntered

```csharp
public const long NotificationApplicationPipModeEntered = 2019
```

Notification received when the application enters picture-in-picture mode.

### NotificationApplicationPipModeExited

```csharp
public const long NotificationApplicationPipModeExited = 2020
```

Notification received when the application exits picture-in-picture mode.

### NotificationApplicationResumed

```csharp
public const long NotificationApplicationResumed = 2014
```

Notification received from the OS when the application is resumed.

Specific to the Android and iOS platforms.

### NotificationChildOrderChanged

```csharp
public const long NotificationChildOrderChanged = 24
```

Notification received when the list of children is changed. This happens when child nodes are added, moved or removed.

### NotificationCrash

```csharp
public const long NotificationCrash = 2012
```

Notification received from Godot's crash handler when the engine is about to crash.

Implemented on desktop platforms, if the crash handler is enabled.

### NotificationDisabled

```csharp
public const long NotificationDisabled = 28
```

Notification received when the node is disabled. See [`Node.ProcessModeEnum.Disabled`](Node.md).

### NotificationDragBegin

```csharp
public const long NotificationDragBegin = 21
```

Notification received when a drag operation begins. All nodes receive this notification, not only the dragged one.

Can be triggered either by dragging a [`Control`](Control.md) that provides drag data (see [`Control._GetDragData()`](Control.md)) or using [`Control.ForceDrag`](Control.md).

Use [`Viewport.GuiGetDragData`](Viewport.md) to get the dragged data.

### NotificationDragEnd

```csharp
public const long NotificationDragEnd = 22
```

Notification received when a drag operation ends.

Use [`Viewport.GuiIsDragSuccessful`](Viewport.md) to check if the drag succeeded.

### NotificationEditorPostSave

```csharp
public const long NotificationEditorPostSave = 9002
```

Notification received right after the scene with the node is saved in the editor. This notification is only sent in the Godot editor and will not occur in exported projects.

### NotificationEditorPreSave

```csharp
public const long NotificationEditorPreSave = 9001
```

Notification received right before the scene with the node is saved in the editor. This notification is only sent in the Godot editor and will not occur in exported projects.

### NotificationEnabled

```csharp
public const long NotificationEnabled = 29
```

Notification received when the node is enabled again after being disabled. See [`Node.ProcessModeEnum.Disabled`](Node.md).

### NotificationEnterTree

```csharp
public const long NotificationEnterTree = 10
```

Notification received when the node enters a [`SceneTree`](SceneTree.md). See `_EnterTree()`.

This notification is received *before* the related `TreeEntered` signal.

### NotificationExitTree

```csharp
public const long NotificationExitTree = 11
```

Notification received when the node is about to exit a [`SceneTree`](SceneTree.md). See `_ExitTree()`.

This notification is received *after* the related `TreeExiting` signal.

This notification is sent in reversed order.

### NotificationInternalPhysicsProcess

```csharp
public const long NotificationInternalPhysicsProcess = 26
```

Notification received from the tree every physics frame when `IsPhysicsProcessingInternal()` returns `true`.

### NotificationInternalProcess

```csharp
public const long NotificationInternalProcess = 25
```

Notification received from the tree every rendered frame when `IsProcessingInternal()` returns `true`.

### NotificationMovedInParent

```csharp
public const long NotificationMovedInParent = 12
```

### NotificationOsImeUpdate

```csharp
public const long NotificationOsImeUpdate = 2013
```

Notification received from the OS when an update of the Input Method Engine occurs (e.g. change of IME cursor position or composition string).

Implemented on desktop and web platforms.

### NotificationOsMemoryWarning

```csharp
public const long NotificationOsMemoryWarning = 2009
```

Notification received from the OS when the application is exceeding its allocated memory.

Implemented only on iOS.

### NotificationParented

```csharp
public const long NotificationParented = 18
```

Notification received when the node is set as a child of another node (see `AddChild()` and `AddSibling()`).

**Note:** This does *not* mean that the node entered the [`SceneTree`](SceneTree.md).

### NotificationPathRenamed

```csharp
public const long NotificationPathRenamed = 23
```

Notification received when the node's `Name` or one of its ancestors' `Name` is changed. This notification is *not* received when the node is removed from the [`SceneTree`](SceneTree.md).

### NotificationPaused

```csharp
public const long NotificationPaused = 14
```

Notification received when the node is paused. See `ProcessMode`.

### NotificationPhysicsProcess

```csharp
public const long NotificationPhysicsProcess = 16
```

Notification received from the tree every physics frame when `IsPhysicsProcessing()` returns `true`. See `_PhysicsProcess()`.

### NotificationPostEnterTree

```csharp
public const long NotificationPostEnterTree = 27
```

Notification received when the node enters the tree, just before `NotificationReady` may be received. Unlike the latter, it is sent every time the node enters tree, not just once.

### NotificationProcess

```csharp
public const long NotificationProcess = 17
```

Notification received from the tree every rendered frame when `IsProcessing()` returns `true`. See `_Process()`.

### NotificationReady

```csharp
public const long NotificationReady = 13
```

Notification received when the node is ready. See `_Ready()`.

### NotificationResetPhysicsInterpolation

```csharp
public const long NotificationResetPhysicsInterpolation = 2001
```

Notification received when `ResetPhysicsInterpolation()` is called on the node or its ancestors.

### NotificationSceneInstantiated

```csharp
public const long NotificationSceneInstantiated = 20
```

Notification received *only* by the newly instantiated scene root node, when [`PackedScene.Instantiate`](PackedScene.md) is completed.

### NotificationTextServerChanged

```csharp
public const long NotificationTextServerChanged = 2018
```

Notification received when the [`TextServer`](TextServer.md) is changed.

### NotificationTranslationChanged

```csharp
public const long NotificationTranslationChanged = 2010
```

Notification received when translations may have changed. Can be triggered by the user changing the locale, changing `AutoTranslateMode` or when the node enters the scene tree. Can be used to respond to language changes, for example to change the UI strings on the fly. Useful when working with the built-in translation support, like [`GodotObject.Tr`](GodotObject.md).

**Note:** This notification is received alongside `NotificationEnterTree`, so if you are instantiating a scene, the child nodes will not be initialized yet. You can use it to setup translations for this node, child nodes created from script, or if you want to access child nodes added in the editor, make sure the node is ready using `IsNodeReady()`.

```csharp
func _notification(what):
if what == NOTIFICATION_TRANSLATION_CHANGED:
if not is_node_ready():
await ready # Wait until ready signal.
$Label.text = atr("%d Bananas") % banana_counter

```

### NotificationUnparented

```csharp
public const long NotificationUnparented = 19
```

Notification received when the parent node calls `RemoveChild()` on this node.

**Note:** This does *not* mean that the node exited the [`SceneTree`](SceneTree.md).

### NotificationUnpaused

```csharp
public const long NotificationUnpaused = 15
```

Notification received when the node is unpaused. See `ProcessMode`.

### NotificationVpMouseEnter

```csharp
public const long NotificationVpMouseEnter = 1010
```

Notification received when the mouse cursor enters the [`Viewport`](Viewport.md)'s visible area, that is not occluded behind other [`Control`](Control.md)s or [`Window`](Window.md)s, provided its [`Viewport.GuiDisableInput`](Viewport.md) is `false` and regardless if it's currently focused or not.

### NotificationVpMouseExit

```csharp
public const long NotificationVpMouseExit = 1011
```

Notification received when the mouse cursor leaves the [`Viewport`](Viewport.md)'s visible area, that is not occluded behind other [`Control`](Control.md)s or [`Window`](Window.md)s, provided its [`Viewport.GuiDisableInput`](Viewport.md) is `false` and regardless if it's currently focused or not.

### NotificationWMAbout

```csharp
public const long NotificationWMAbout = 2011
```

Notification received from the OS when a request for "About" information is sent.

Implemented only on macOS.

### NotificationWMCloseRequest

```csharp
public const long NotificationWMCloseRequest = 1006
```

Notification received from the OS when a close request is sent (e.g. closing the window with a "Close" button or Alt + F4).

Implemented on desktop platforms.

### NotificationWMDpiChange

```csharp
public const long NotificationWMDpiChange = 1009
```

Notification received from the OS when the screen's dots per inch (DPI) scale is changed. Only implemented on macOS.

### NotificationWMGoBackRequest

```csharp
public const long NotificationWMGoBackRequest = 1007
```

Notification received from the OS when a go back request is sent (e.g. pressing the "Back" button on Android).

Implemented only on Android.

### NotificationWMMouseEnter

```csharp
public const long NotificationWMMouseEnter = 1002
```

Notification received when the mouse enters the window.

Implemented for embedded windows and on desktop and web platforms.

### NotificationWMMouseExit

```csharp
public const long NotificationWMMouseExit = 1003
```

Notification received when the mouse leaves the window.

Implemented for embedded windows and on desktop and web platforms.

### NotificationWMOutputMaxLinearValueChanged

```csharp
public const long NotificationWMOutputMaxLinearValueChanged = 1013
```

Notification received when the output max linear value returned by [`Window.GetOutputMaxLinearValue`](Window.md) has changed.

This occurs when HDR output is enabled or disabled and when any HDR output luminance values of the window have changed, such as when the player adjusts their screen brightness setting or moves the window to a different screen.

### NotificationWMPositionChanged

```csharp
public const long NotificationWMPositionChanged = 1012
```

Notification received when the window is moved.

### NotificationWMSizeChanged

```csharp
public const long NotificationWMSizeChanged = 1008
```

Notification received when the window is resized.

**Note:** Only the resized [`Window`](Window.md) node receives this notification, and it's not propagated to the child nodes.

### NotificationWMWindowFocusIn

```csharp
public const long NotificationWMWindowFocusIn = 1004
```

Notification received from the OS when the node's [`Window`](Window.md) ancestor is focused. This may be a change of focus between two windows of the same engine instance, or from the OS desktop or a third-party application to a window of the game (in which case `NotificationApplicationFocusIn` is also received).

A [`Window`](Window.md) node receives this notification when it is focused.

### NotificationWMWindowFocusOut

```csharp
public const long NotificationWMWindowFocusOut = 1005
```

Notification received from the OS when the node's [`Window`](Window.md) ancestor is defocused. This may be a change of focus between two windows of the same engine instance, or from a window of the game to the OS desktop or a third-party application (in which case `NotificationApplicationFocusOut` is also received).

A [`Window`](Window.md) node receives this notification when it is defocused.

## AutoTranslateModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Inherit = 0`：Inherits [`Node.AutoTranslateMode`](Node.md) from the node's parent.
- `Always = 1`：Always automatically translate.
- `Disabled = 2`：Never automatically translate.

## ChildEnteredTreeEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Node.ChildEnteredTree`](Node.md) event of a [`Node`](Node.md) class.

### 签名

#### Invoke

```csharp
delegate void ChildEnteredTreeEventHandler(Node node)
```

## ChildExitingTreeEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Node.ChildExitingTree`](Node.md) event of a [`Node`](Node.md) class.

### 签名

#### Invoke

```csharp
delegate void ChildExitingTreeEventHandler(Node node)
```

## DuplicateFlags

命名空间 `Godot`；enum

### 枚举值

- `Signals = 1`：Duplicate the node's signal connections that are connected with the [`GodotObject.ConnectFlags.Persist`](GodotObject.md) flag.
- `Groups = 2`：Duplicate the node's groups.
- `Scripts = 4`：Duplicate the node's script (also overriding the duplicated children's scripts, if combined with `UseInstantiation`).
- `UseInstantiation = 8`：Duplicate using [`PackedScene.Instantiate`](PackedScene.md).
- `InternalState = 16`：Duplicate also non-serializable variables (i.e.
- `Default = 15`：Duplicate using default flags.

## EditorDescriptionChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Node.EditorDescriptionChanged`](Node.md) event of a [`Node`](Node.md) class.

### 签名

#### Invoke

```csharp
delegate void EditorDescriptionChangedEventHandler(Node node)
```

## InternalMode

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：The node will not be internal.
- `Front = 1`：The node will be placed at the beginning of the parent's children, before any non-internal sibling.
- `Back = 2`：The node will be placed at the end of the parent's children, after any non-internal sibling.

## PhysicsInterpolationModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Inherit = 0`：Inherits [`Node.PhysicsInterpolationMode`](Node.md) from the node's parent.
- `On = 1`：Enables physics interpolation for this node and for children set to `Inherit`.
- `Off = 2`：Disables physics interpolation for this node and for children set to `Inherit`.

## ProcessModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Inherit = 0`：Inherits [`Node.ProcessMode`](Node.md) from the node's parent.
- `Pausable = 1`：Processes when [`SceneTree.Paused`](SceneTree.md) is `false`.
- `WhenPaused = 2`：Processes **only** when [`SceneTree.Paused`](SceneTree.md) is `true`.
- `Always = 3`：Always processes.
- `Disabled = 4`：Never processes.

## ProcessThreadGroupEnum

命名空间 `Godot`；enum

### 枚举值

- `Inherit = 0`：Process this node based on the thread group mode of the first parent (or grandparent) node that has a thread group mode that is not inherit.
- `MainThread = 1`：Process this node (and child nodes set to inherit) on the main thread.
- `SubThread = 2`：Process this node (and child nodes set to inherit) on a sub-thread.

## ProcessThreadMessagesEnum

命名空间 `Godot`；enum

### 枚举值

- `Messages = 1`：Allows this node to process threaded messages created with [`Node.CallDeferredThreadGroup`](Node.md) right before [`Node._Process()`](Node.md) is called.
- `MessagesPhysics = 2`：Allows this node to process threaded messages created with [`Node.CallDeferredThreadGroup`](Node.md) right before [`Node._PhysicsProcess()`](Node.md) is called.
- `MessagesAll = 3`：Allows this node to process threaded messages created with [`Node.CallDeferredThreadGroup`](Node.md) right before either [`Node._Process()`](Node.md) or [`Node._PhysicsProcess()`](Node.md) are called.

## ReplacingByEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Node.ReplacingBy`](Node.md) event of a [`Node`](Node.md) class.

### 签名

#### Invoke

```csharp
delegate void ReplacingByEventHandler(Node node)
```
