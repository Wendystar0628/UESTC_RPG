<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PackedScene

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A simplified interface to a scene file. Provides access to operations and checks that can be performed on the scene resource itself.

Can be used to save a node to a file. When saving, the node as well as all the nodes it owns get saved (see [`Node.Owner`](Node.md) property).

**Note:** The node doesn't need to own itself.

**Example:** Load a saved scene:

```csharp
// C# has no preload, so you have to always use ResourceLoader.Load<PackedScene>().
var scene = ResourceLoader.Load<PackedScene>("res://scene.tscn").Instantiate();
// Add the node as a child of the node the script is attached to.
AddChild(scene);

```

**Example:** Save a node with different owners. The following example creates 3 objects: [`Node2D`](Node2D.md) (`node`), [`RigidBody2D`](RigidBody2D.md) (`body`) and [`CollisionObject2D`](CollisionObject2D.md) (`collision`). `collision` is a child of `body` which is a child of `node`. Only `body` is owned by `node` and `Pack()` will therefore only save those two nodes, but not `collision`.

```csharp
// Create the objects.
var node = new Node2D();
var body = new RigidBody2D();
var collision = new CollisionShape2D();

// Create the object hierarchy.
body.AddChild(collision);
node.AddChild(body);

// Change owner of `body`, but not of `collision`.
body.Owner = node;
var scene = new PackedScene();

// Only `node` and `body` are now packed.
Error result = scene.Pack(node);
if (result == Error.Ok)
{
Error error = ResourceSaver.Save(scene, "res://path/name.tscn"); // Or "user://..."
if (error != Error.Ok)
{
GD.PushError("An error occurred while saving the scene to disk.");
}
}

```

## 构造函数

### PackedScene

```csharp
public PackedScene()
```

## 属性

### _Bundled

```csharp
public Dictionary _Bundled { get; set; }
```

## 方法

### CanInstantiate

```csharp
public bool CanInstantiate()
```

引擎名：`can_instantiate`

Returns `true` if the scene file has nodes.

### GetState

```csharp
public SceneState GetState()
```

引擎名：`get_state`

Returns the [`SceneState`](SceneState.md) representing the scene file contents.

### Instantiate

```csharp
public Node Instantiate(PackedScene.GenEditState editState = PackedScene.GenEditState.Disabled)
```

引擎名：`instantiate`

Instantiates the scene's node hierarchy. Triggers child scene instantiation(s). Triggers a [`Node.NotificationSceneInstantiated`](Node.md) notification on the root node.

### Instantiate

```csharp
public T Instantiate<T>(PackedScene.GenEditState editState = PackedScene.GenEditState.Disabled)
```

引擎名：`instantiate`

Instantiates the scene's node hierarchy, erroring on failure.
Triggers child scene instantiation(s). Triggers a
[`Node.NotificationSceneInstantiated`](Node.md) notification on the root node.

返回：The instantiated scene.

### InstantiateOrNull

```csharp
public T InstantiateOrNull<T>(PackedScene.GenEditState editState = PackedScene.GenEditState.Disabled)
```

Instantiates the scene's node hierarchy, returning `null` on failure.
Triggers child scene instantiation(s). Triggers a
[`Node.NotificationSceneInstantiated`](Node.md) notification on the root node.

返回：The instantiated scene.

### Pack

```csharp
public Error Pack(Node path)
```

引擎名：`pack`

Packs the `path` node, and all owned sub-nodes, into this `PackedScene`. Any existing data will be cleared. See [`Node.Owner`](Node.md).

## GenEditState

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：If passed to [`PackedScene.Instantiate`](PackedScene.md), blocks edits to the scene state.
- `Instance = 1`：If passed to [`PackedScene.Instantiate`](PackedScene.md), provides local scene resources to the local scene.
- `Main = 2`：If passed to [`PackedScene.Instantiate`](PackedScene.md), provides local scene resources to the local scene.
- `MainInherited = 3`：It's similar to `Main`, but for the case where the scene is being instantiated to be the base of another one.
