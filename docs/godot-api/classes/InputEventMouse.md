<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# InputEventMouse

命名空间 `Godot`；class；继承 [`InputEventWithModifiers`](InputEventWithModifiers.md) → [`InputEventFromWindow`](InputEventFromWindow.md) → [`InputEvent`](InputEvent.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Stores general information about mouse events.

## 属性

### ButtonMask

```csharp
public MouseButtonMask ButtonMask { get; set; }
```

引擎名：`button_mask`

The mouse button mask identifier, one of or a bitwise combination of the [`MouseButton`](MouseButton.md) button masks.

### GlobalPosition

```csharp
public Vector2 GlobalPosition { get; set; }
```

引擎名：`global_position`

When received in [`Node._Input()`](Node.md) or [`Node._UnhandledInput()`](Node.md), returns the mouse's position in the root [`Viewport`](Viewport.md) using the coordinate system of the root [`Viewport`](Viewport.md).

When received in [`Control.GuiInput`](Control.md), returns the mouse's position in the [`CanvasLayer`](CanvasLayer.md) that the [`Control`](Control.md) is in using the coordinate system of the [`CanvasLayer`](CanvasLayer.md).

### Position

```csharp
public Vector2 Position { get; set; }
```

引擎名：`position`

When received in [`Node._Input()`](Node.md) or [`Node._UnhandledInput()`](Node.md), returns the mouse's position in the [`Viewport`](Viewport.md) this [`Node`](Node.md) is in using the coordinate system of this [`Viewport`](Viewport.md).

When received in [`Control.GuiInput`](Control.md), returns the mouse's position in the [`Control`](Control.md) using the local coordinate system of the [`Control`](Control.md).

## 方法

### GetButtonMask

```csharp
public MouseButtonMask GetButtonMask()
```

引擎名：`get_button_mask`

### GetGlobalPosition

```csharp
public Vector2 GetGlobalPosition()
```

引擎名：`get_global_position`

### GetPosition

```csharp
public Vector2 GetPosition()
```

引擎名：`get_position`

### SetButtonMask

```csharp
public void SetButtonMask(MouseButtonMask buttonMask)
```

引擎名：`set_button_mask`

### SetGlobalPosition

```csharp
public void SetGlobalPosition(Vector2 globalPosition)
```

引擎名：`set_global_position`

### SetPosition

```csharp
public void SetPosition(Vector2 position)
```

引擎名：`set_position`
