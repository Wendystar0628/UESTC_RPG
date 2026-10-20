<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# InputEventMagnifyGesture

命名空间 `Godot`；class；继承 [`InputEventGesture`](InputEventGesture.md) → [`InputEventWithModifiers`](InputEventWithModifiers.md) → [`InputEventFromWindow`](InputEventFromWindow.md) → [`InputEvent`](InputEvent.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Stores the factor of a magnifying touch gesture. This is usually performed when the user pinches the touch screen and used for zooming in/out.

**Note:** On Android, this requires the `ProjectSettings.input_devices/pointing/android/enable_pan_and_scale_gestures` project setting to be enabled.

## 构造函数

### InputEventMagnifyGesture

```csharp
public InputEventMagnifyGesture()
```

## 属性

### Factor

```csharp
public float Factor { get; set; }
```

引擎名：`factor`

The amount (or delta) of the event. This value is closer to `1.0` the slower the gesture is performed.

## 方法

### GetFactor

```csharp
public float GetFactor()
```

引擎名：`get_factor`

### SetFactor

```csharp
public void SetFactor(float factor)
```

引擎名：`set_factor`
