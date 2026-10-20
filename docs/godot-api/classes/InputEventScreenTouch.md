<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# InputEventScreenTouch

命名空间 `Godot`；class；继承 [`InputEventFromWindow`](InputEventFromWindow.md) → [`InputEvent`](InputEvent.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Stores information about multi-touch press/release input events. Supports touch press, touch release and `Index` for multi-touch count and order.

## 构造函数

### InputEventScreenTouch

```csharp
public InputEventScreenTouch()
```

## 属性

### Canceled

```csharp
public bool Canceled { get; set; }
```

引擎名：`canceled`

If `true`, the touch event has been canceled.

### DoubleTap

```csharp
public bool DoubleTap { get; set; }
```

引擎名：`double_tap`

If `true`, the touch's state is a double tap.

### Index

```csharp
public int Index { get; set; }
```

引擎名：`index`

The touch index in the case of a multi-touch event. One index = one finger.

### Position

```csharp
public Vector2 Position { get; set; }
```

引擎名：`position`

The touch position in the viewport the node is in, using the coordinate system of this viewport.

### Pressed

```csharp
public bool Pressed { get; set; }
```

引擎名：`pressed`

If `true`, the touch's state is pressed. If `false`, the touch's state is released.

## 方法

### GetIndex

```csharp
public int GetIndex()
```

引擎名：`get_index`

### GetPosition

```csharp
public Vector2 GetPosition()
```

引擎名：`get_position`

### IsDoubleTap

```csharp
public bool IsDoubleTap()
```

引擎名：`is_double_tap`

### SetCanceled

```csharp
public void SetCanceled(bool canceled)
```

引擎名：`set_canceled`

### SetDoubleTap

```csharp
public void SetDoubleTap(bool doubleTap)
```

引擎名：`set_double_tap`

### SetIndex

```csharp
public void SetIndex(int index)
```

引擎名：`set_index`

### SetPosition

```csharp
public void SetPosition(Vector2 position)
```

引擎名：`set_position`

### SetPressed

```csharp
public void SetPressed(bool pressed)
```

引擎名：`set_pressed`
