<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisibleOnScreenNotifier3D

命名空间 `Godot`；class；继承 [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`VisibleOnScreenNotifier3D` represents a box-shaped region of 3D space. When any part of this region becomes visible on screen or in a [`Camera3D`](Camera3D.md)'s view, it will emit a `ScreenEntered` signal, and likewise it will emit a `ScreenExited` signal when no part of it remains visible.

If you want a node to be enabled automatically when this region is visible on screen, use [`VisibleOnScreenEnabler3D`](VisibleOnScreenEnabler3D.md).

**Note:** `VisibleOnScreenNotifier3D` uses an approximate heuristic that doesn't take walls and other occlusion into account, unless occlusion culling is used. It also won't function unless [`Node3D.Visible`](Node3D.md) is set to `true`.

## 构造函数

### VisibleOnScreenNotifier3D

```csharp
public VisibleOnScreenNotifier3D()
```

## 属性

### Aabb

```csharp
public Aabb Aabb { get; set; }
```

引擎名：`aabb`

The `VisibleOnScreenNotifier3D`'s bounding box.

## 方法

### IsOnScreen

```csharp
public bool IsOnScreen()
```

引擎名：`is_on_screen`

Returns `true` if the bounding box is on the screen.

**Note:** It takes one frame for the `VisibleOnScreenNotifier3D`'s visibility to be assessed once added to the scene tree, so this method will always return `false` right after it is instantiated.

### SetAabb

```csharp
public void SetAabb(Aabb rect)
```

引擎名：`set_aabb`

## 事件（信号）

### ScreenEntered

```csharp
public event Action ScreenEntered
```

引擎名：`screen_entered`

Emitted when the `VisibleOnScreenNotifier3D` enters the screen.

### ScreenExited

```csharp
public event Action ScreenExited
```

引擎名：`screen_exited`

Emitted when the `VisibleOnScreenNotifier3D` exits the screen.
