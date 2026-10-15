<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# BackBufferCopy

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Node for back-buffering the currently-displayed screen. The region defined in the `BackBufferCopy` node is buffered with the content of the screen it covers, or the entire screen according to the `CopyMode`. It can be accessed in shader scripts using the screen texture (i.e. a uniform sampler with `hint_screen_texture`).

**Note:** Since this node inherits from [`Node2D`](Node2D.md) (and not [`Control`](Control.md)), anchors and margins won't apply to child [`Control`](Control.md)-derived nodes. This can be problematic when resizing the window. To avoid this, add [`Control`](Control.md)-derived nodes as *siblings* to the `BackBufferCopy` node instead of adding them as children.

## 构造函数

### BackBufferCopy

```csharp
public BackBufferCopy()
```

## 属性

### CopyMode

```csharp
public BackBufferCopy.CopyModeEnum CopyMode { get; set; }
```

引擎名：`copy_mode`

Buffer mode.

### Rect

```csharp
public Rect2 Rect { get; set; }
```

引擎名：`rect`

The area covered by the `BackBufferCopy`. Only used if `CopyMode` is [`BackBufferCopy.CopyModeEnum.Rect`](BackBufferCopy.md).

## 方法

### GetCopyMode

```csharp
public BackBufferCopy.CopyModeEnum GetCopyMode()
```

引擎名：`get_copy_mode`

### GetRect

```csharp
public Rect2 GetRect()
```

引擎名：`get_rect`

### SetCopyMode

```csharp
public void SetCopyMode(BackBufferCopy.CopyModeEnum copyMode)
```

引擎名：`set_copy_mode`

### SetRect

```csharp
public void SetRect(Rect2 rect)
```

引擎名：`set_rect`

## CopyModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Disables the buffering mode.
- `Rect = 1`：[`BackBufferCopy`](BackBufferCopy.md) buffers a rectangular region.
- `Viewport = 2`：[`BackBufferCopy`](BackBufferCopy.md) buffers the entire screen.
