<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisibleOnScreenEnabler3D

命名空间 `Godot`；class；继承 [`VisibleOnScreenNotifier3D`](VisibleOnScreenNotifier3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`VisibleOnScreenEnabler3D` contains a box-shaped region of 3D space and a target node. The target node will be automatically enabled (via its [`Node.ProcessMode`](Node.md) property) when any part of this region becomes visible on the screen, and automatically disabled otherwise. This can for example be used to activate enemies only when the player approaches them.

See [`VisibleOnScreenNotifier3D`](VisibleOnScreenNotifier3D.md) if you only want to be notified when the region is visible on screen.

**Note:** `VisibleOnScreenEnabler3D` uses an approximate heuristic that doesn't take walls and other occlusion into account, unless occlusion culling is used. It also won't function unless [`Node3D.Visible`](Node3D.md) is set to `true`.

## 构造函数

### VisibleOnScreenEnabler3D

```csharp
public VisibleOnScreenEnabler3D()
```

## 属性

### EnableMode

```csharp
public VisibleOnScreenEnabler3D.EnableModeEnum EnableMode { get; set; }
```

引擎名：`enable_mode`

Determines how the target node is enabled. Corresponds to [`Node.ProcessModeEnum`](Node.md). When the node is disabled, it always uses [`Node.ProcessModeEnum.Disabled`](Node.md).

### EnableNodePath

```csharp
public NodePath EnableNodePath { get; set; }
```

引擎名：`enable_node_path`

The path to the target node, relative to the `VisibleOnScreenEnabler3D`. The target node is cached; it's only assigned when setting this property (if the `VisibleOnScreenEnabler3D` is inside the scene tree) and every time the `VisibleOnScreenEnabler3D` enters the scene tree. If the path is empty, no node will be affected. If the path is invalid, an error is also generated.

## 方法

### GetEnableMode

```csharp
public VisibleOnScreenEnabler3D.EnableModeEnum GetEnableMode()
```

引擎名：`get_enable_mode`

### GetEnableNodePath

```csharp
public NodePath GetEnableNodePath()
```

引擎名：`get_enable_node_path`

### SetEnableMode

```csharp
public void SetEnableMode(VisibleOnScreenEnabler3D.EnableModeEnum mode)
```

引擎名：`set_enable_mode`

### SetEnableNodePath

```csharp
public void SetEnableNodePath(NodePath path)
```

引擎名：`set_enable_node_path`

## EnableModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Inherit = 0`：Corresponds to [`Node.ProcessModeEnum.Inherit`](Node.md).
- `Always = 1`：Corresponds to [`Node.ProcessModeEnum.Always`](Node.md).
- `WhenPaused = 2`：Corresponds to [`Node.ProcessModeEnum.WhenPaused`](Node.md).
