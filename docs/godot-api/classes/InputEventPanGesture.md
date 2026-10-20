<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# InputEventPanGesture

命名空间 `Godot`；class；继承 [`InputEventGesture`](InputEventGesture.md) → [`InputEventWithModifiers`](InputEventWithModifiers.md) → [`InputEventFromWindow`](InputEventFromWindow.md) → [`InputEvent`](InputEvent.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Stores information about pan gestures. A pan gesture is performed when the user swipes the touch screen with two fingers. It's typically used for panning/scrolling.

**Note:** On Android, this requires the `ProjectSettings.input_devices/pointing/android/enable_pan_and_scale_gestures` project setting to be enabled.

## 构造函数

### InputEventPanGesture

```csharp
public InputEventPanGesture()
```

## 属性

### Delta

```csharp
public Vector2 Delta { get; set; }
```

引擎名：`delta`

Panning amount since last pan event.

## 方法

### GetDelta

```csharp
public Vector2 GetDelta()
```

引擎名：`get_delta`

### SetDelta

```csharp
public void SetDelta(Vector2 delta)
```

引擎名：`set_delta`
