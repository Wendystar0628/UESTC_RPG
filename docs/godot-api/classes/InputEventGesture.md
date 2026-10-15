<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# InputEventGesture

命名空间 `Godot`；class；继承 [`InputEventWithModifiers`](InputEventWithModifiers.md) → [`InputEventFromWindow`](InputEventFromWindow.md) → [`InputEvent`](InputEvent.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

InputEventGestures are sent when a user performs a supported gesture on a touch screen. Gestures can't be emulated using mouse, because they typically require multi-touch.

## 属性

### Position

```csharp
public Vector2 Position { get; set; }
```

引擎名：`position`

The local gesture position relative to the [`Viewport`](Viewport.md). If used in [`Control.GuiInput`](Control.md), the position is relative to the current [`Control`](Control.md) that received this gesture.

## 方法

### GetPosition

```csharp
public Vector2 GetPosition()
```

引擎名：`get_position`

### SetPosition

```csharp
public void SetPosition(Vector2 position)
```

引擎名：`set_position`
