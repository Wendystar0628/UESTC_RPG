<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# MultiplayerSpawner

命名空间 `Godot`；class；继承 [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Spawnable scenes can be configured in the editor or through code (see `AddSpawnableScene()`).

Also supports custom node spawns through `Spawn()`, calling `SpawnFunction` on all peers.

Internally, `MultiplayerSpawner` uses [`MultiplayerApi.ObjectConfigurationAdd`](MultiplayerApi.md) to notify spawns passing the spawned node as the `object` and itself as the `configuration`, and [`MultiplayerApi.ObjectConfigurationRemove`](MultiplayerApi.md) to notify despawns in a similar way.

## 构造函数

### MultiplayerSpawner

```csharp
public MultiplayerSpawner()
```

## 属性

### SpawnFunction

```csharp
public Callable SpawnFunction { get; set; }
```

引擎名：`spawn_function`

Method called on all peers when a custom `Spawn()` is requested by the authority. Will receive the `data` parameter, and should return a [`Node`](Node.md) that is not in the scene tree.

**Note:** The returned node should **not** be added to the scene with [`Node.AddChild`](Node.md). This is done automatically.

### SpawnLimit

```csharp
public uint SpawnLimit { get; set; }
```

引擎名：`spawn_limit`

Maximum number of nodes allowed to be spawned by this spawner. Includes both spawnable scenes and custom spawns.

When set to `0` (the default), there is no limit.

### SpawnPath

```csharp
public NodePath SpawnPath { get; set; }
```

引擎名：`spawn_path`

Path to the spawn root. Spawnable scenes that are added as direct children are replicated to other peers.

### _SpawnableScenes

```csharp
public string[] _SpawnableScenes { get; set; }
```

## 方法

### AddSpawnableScene

```csharp
public void AddSpawnableScene(string path)
```

引擎名：`add_spawnable_scene`

Adds a scene path to spawnable scenes, making it automatically replicated from the multiplayer authority to other peers when added as children of the node pointed by `SpawnPath`.

### ClearSpawnableScenes

```csharp
public void ClearSpawnableScenes()
```

引擎名：`clear_spawnable_scenes`

Clears all spawnable scenes. Does not despawn existing instances on remote peers.

### GetSpawnFunction

```csharp
public Callable GetSpawnFunction()
```

引擎名：`get_spawn_function`

### GetSpawnLimit

```csharp
public uint GetSpawnLimit()
```

引擎名：`get_spawn_limit`

### GetSpawnPath

```csharp
public NodePath GetSpawnPath()
```

引擎名：`get_spawn_path`

### GetSpawnableScene

```csharp
public string GetSpawnableScene(int index)
```

引擎名：`get_spawnable_scene`

Returns the spawnable scene path by index.

### GetSpawnableSceneCount

```csharp
public int GetSpawnableSceneCount()
```

引擎名：`get_spawnable_scene_count`

Returns the count of spawnable scene paths.

### SetSpawnFunction

```csharp
public void SetSpawnFunction(Callable spawnFunction)
```

引擎名：`set_spawn_function`

### SetSpawnLimit

```csharp
public void SetSpawnLimit(uint limit)
```

引擎名：`set_spawn_limit`

### SetSpawnPath

```csharp
public void SetSpawnPath(NodePath path)
```

引擎名：`set_spawn_path`

### Spawn

```csharp
public Node Spawn(Variant data = default)
```

引擎名：`spawn`

Requests a custom spawn, with `data` passed to `SpawnFunction` on all peers. Returns the locally spawned node instance already inside the scene tree, and added as a child of the node pointed by `SpawnPath`.

**Note:** Spawnable scenes are spawned automatically. `Spawn()` is only needed for custom spawns.

## 事件（信号）

### Despawned

```csharp
public event MultiplayerSpawner.DespawnedEventHandler Despawned
```

引擎名：`despawned`

Emitted when a spawnable scene or custom spawn was despawned by the multiplayer authority. Only called on remote peers.

### Spawned

```csharp
public event MultiplayerSpawner.SpawnedEventHandler Spawned
```

引擎名：`spawned`

Emitted when a spawnable scene or custom spawn was spawned by the multiplayer authority. Only called on remote peers.

## DespawnedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`MultiplayerSpawner.Despawned`](MultiplayerSpawner.md) event of a [`MultiplayerSpawner`](MultiplayerSpawner.md) class.

### 签名

#### Invoke

```csharp
delegate void DespawnedEventHandler(Node node)
```

## SpawnedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`MultiplayerSpawner.Spawned`](MultiplayerSpawner.md) event of a [`MultiplayerSpawner`](MultiplayerSpawner.md) class.

### 签名

#### Invoke

```csharp
delegate void SpawnedEventHandler(Node node)
```
