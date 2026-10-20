<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisibleOnScreenEnabler2D

命名空间 `Godot`；class；继承 [`VisibleOnScreenNotifier2D`](VisibleOnScreenNotifier2D.md) → [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`VisibleOnScreenEnabler2D` contains a rectangular region of 2D space and a target node. The target node will be automatically enabled (via its [`Node.ProcessMode`](Node.md) property) when any part of this region becomes visible on the screen, and automatically disabled otherwise. This can for example be used to activate enemies only when the player approaches them.

See [`VisibleOnScreenNotifier2D`](VisibleOnScreenNotifier2D.md) if you only want to be notified when the region is visible on screen.

**Note:** `VisibleOnScreenEnabler2D` uses the render culling code to determine whether it's visible on screen, so it won't function unless [`CanvasItem.Visible`](CanvasItem.md) is set to `true`.

## 构造函数

### VisibleOnScreenEnabler2D

```csharp
public VisibleOnScreenEnabler2D()
```

## 属性

### EnableMode

```csharp
public VisibleOnScreenEnabler2D.EnableModeEnum EnableMode { get; set; }
```

引擎名：`enable_mode`

Determines how the target node is enabled. Corresponds to [`Node.ProcessModeEnum`](Node.md). When the node is disabled, it always uses [`Node.ProcessModeEnum.Disabled`](Node.md).

### EnableNodePath

```csharp
public NodePath EnableNodePath { get; set; }
```

引擎名：`enable_node_path`

The path to the target node, relative to the `VisibleOnScreenEnabler2D`. The target node is cached; it's only assigned when setting this property (if the `VisibleOnScreenEnabler2D` is inside the scene tree) and every time the `VisibleOnScreenEnabler2D` enters the scene tree. If the path is empty, no node will be affected. If the path is invalid, an error is also generated.

## 方法

### GetEnableMode

```csharp
public VisibleOnScreenEnabler2D.EnableModeEnum GetEnableMode()
```

引擎名：`get_enable_mode`

### GetEnableNodePath

```csharp
public NodePath GetEnableNodePath()
```

引擎名：`get_enable_node_path`

### SetEnableMode

```csharp
public void SetEnableMode(VisibleOnScreenEnabler2D.EnableModeEnum mode)
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
