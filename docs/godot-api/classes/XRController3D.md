<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# XRController3D

命名空间 `Godot`；class；继承 [`XRNode3D`](XRNode3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This is a helper 3D node that is linked to the tracking of controllers. It also offers several handy passthroughs to the state of buttons and such on the controllers.

Controllers are linked by their ID. You can create controller nodes before the controllers are available. If your game always uses two controllers (one for each hand), you can predefine the controllers with ID 1 and 2; they will become active as soon as the controllers are identified. If you expect additional controllers to be used, you should react to the signals and add XRController3D nodes to your scene.

The position of the controller node is automatically updated by the [`XRServer`](XRServer.md). This makes this node ideal to add child nodes to visualize the controller.

The current [`XRInterface`](XRInterface.md) defines the names of inputs. In the case of OpenXR, these are the names of actions in the current action set from the OpenXR action map.

## 构造函数

### XRController3D

```csharp
public XRController3D()
```

## 方法

### GetFloat

```csharp
public float GetFloat(StringName name)
```

引擎名：`get_float`

Returns a numeric value for the input with the given `name`. This is used for triggers and grip sensors.

**Note:** The current [`XRInterface`](XRInterface.md) defines the `name` for each input. In the case of OpenXR, these are the names of actions in the current action set.

### GetInput

```csharp
public Variant GetInput(StringName name)
```

引擎名：`get_input`

Returns a [`Variant`](Variant.md) for the input with the given `name`. This works for any input type, the variant will be typed according to the actions configuration.

**Note:** The current [`XRInterface`](XRInterface.md) defines the `name` for each input. In the case of OpenXR, these are the names of actions in the current action set.

### GetTrackerHand

```csharp
public XRPositionalTracker.TrackerHand GetTrackerHand()
```

引擎名：`get_tracker_hand`

Returns the hand holding this controller, if known.

### GetVector2

```csharp
public Vector2 GetVector2(StringName name)
```

引擎名：`get_vector2`

Returns a [`Vector2`](Vector2.md) for the input with the given `name`. This is used for thumbsticks and thumbpads found on many controllers.

**Note:** The current [`XRInterface`](XRInterface.md) defines the `name` for each input. In the case of OpenXR, these are the names of actions in the current action set.

### IsButtonPressed

```csharp
public bool IsButtonPressed(StringName name)
```

引擎名：`is_button_pressed`

Returns `true` if the button with the given `name` is pressed.

**Note:** The current [`XRInterface`](XRInterface.md) defines the `name` for each input. In the case of OpenXR, these are the names of actions in the current action set.

## 事件（信号）

### ButtonPressed

```csharp
public event XRController3D.ButtonPressedEventHandler ButtonPressed
```

引擎名：`button_pressed`

Emitted when a button on this controller is pressed.

### ButtonReleased

```csharp
public event XRController3D.ButtonReleasedEventHandler ButtonReleased
```

引擎名：`button_released`

Emitted when a button on this controller is released.

### InputFloatChanged

```csharp
public event XRController3D.InputFloatChangedEventHandler InputFloatChanged
```

引擎名：`input_float_changed`

Emitted when a trigger or similar input on this controller changes value.

### InputVector2Changed

```csharp
public event XRController3D.InputVector2ChangedEventHandler InputVector2Changed
```

引擎名：`input_vector2_changed`

Emitted when a thumbstick or thumbpad on this controller is moved.

### ProfileChanged

```csharp
public event XRController3D.ProfileChangedEventHandler ProfileChanged
```

引擎名：`profile_changed`

Emitted when the interaction profile on this controller is changed.

## ButtonPressedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`XRController3D.ButtonPressed`](XRController3D.md) event of a [`XRController3D`](XRController3D.md) class.

### 签名

#### Invoke

```csharp
delegate void ButtonPressedEventHandler(string actionName)
```

## ButtonReleasedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`XRController3D.ButtonReleased`](XRController3D.md) event of a [`XRController3D`](XRController3D.md) class.

### 签名

#### Invoke

```csharp
delegate void ButtonReleasedEventHandler(string actionName)
```

## InputFloatChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`XRController3D.InputFloatChanged`](XRController3D.md) event of a [`XRController3D`](XRController3D.md) class.

### 签名

#### Invoke

```csharp
delegate void InputFloatChangedEventHandler(string actionName, double value)
```

## InputVector2ChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`XRController3D.InputVector2Changed`](XRController3D.md) event of a [`XRController3D`](XRController3D.md) class.

### 签名

#### Invoke

```csharp
delegate void InputVector2ChangedEventHandler(string actionName, Vector2 value)
```

## ProfileChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`XRController3D.ProfileChanged`](XRController3D.md) event of a [`XRController3D`](XRController3D.md) class.

### 签名

#### Invoke

```csharp
delegate void ProfileChangedEventHandler(string role)
```
