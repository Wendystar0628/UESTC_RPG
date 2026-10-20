<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisibleOnScreenNotifier2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`VisibleOnScreenNotifier2D` represents a rectangular region of 2D space. When any part of this region becomes visible on screen or in a viewport, it will emit a `ScreenEntered` signal, and likewise it will emit a `ScreenExited` signal when no part of it remains visible.

If you want a node to be enabled automatically when this region is visible on screen, use [`VisibleOnScreenEnabler2D`](VisibleOnScreenEnabler2D.md).

**Note:** `VisibleOnScreenNotifier2D` uses the render culling code to determine whether it's visible on screen, so it won't function unless [`CanvasItem.Visible`](CanvasItem.md) is set to `true`.

## 构造函数

### VisibleOnScreenNotifier2D

```csharp
public VisibleOnScreenNotifier2D()
```

## 属性

### Rect

```csharp
public Rect2 Rect { get; set; }
```

引擎名：`rect`

The VisibleOnScreenNotifier2D's bounding rectangle.

### ShowRect

```csharp
public bool ShowRect { get; set; }
```

引擎名：`show_rect`

If `true`, shows the rectangle area of `Rect` in the editor with a translucent magenta fill. Unlike changing the visibility of the VisibleOnScreenNotifier2D, this does not affect the screen culling detection.

## 方法

### GetRect

```csharp
public Rect2 GetRect()
```

引擎名：`get_rect`

### IsOnScreen

```csharp
public bool IsOnScreen()
```

引擎名：`is_on_screen`

If `true`, the bounding rectangle is on the screen.

**Note:** It takes one frame for the `VisibleOnScreenNotifier2D`'s visibility to be determined once added to the scene tree, so this method will always return `false` right after it is instantiated, before the draw pass.

### IsShowingRect

```csharp
public bool IsShowingRect()
```

引擎名：`is_showing_rect`

### SetRect

```csharp
public void SetRect(Rect2 rect)
```

引擎名：`set_rect`

### SetShowRect

```csharp
public void SetShowRect(bool showRect)
```

引擎名：`set_show_rect`

## 事件（信号）

### ScreenEntered

```csharp
public event Action ScreenEntered
```

引擎名：`screen_entered`

Emitted when the VisibleOnScreenNotifier2D enters the screen.

### ScreenExited

```csharp
public event Action ScreenExited
```

引擎名：`screen_exited`

Emitted when the VisibleOnScreenNotifier2D exits the screen.
