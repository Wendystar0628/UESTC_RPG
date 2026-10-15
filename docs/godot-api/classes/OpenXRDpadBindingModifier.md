<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRDpadBindingModifier

命名空间 `Godot`；class；继承 [`OpenXripBindingModifier`](OpenXripBindingModifier.md) → [`OpenXRBindingModifier`](OpenXRBindingModifier.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

The DPad binding modifier converts an axis input to a dpad output, emulating a DPad. New input paths for each dpad direction will be added to the interaction profile. When bound to actions the DPad emulation will be activated. You should **not** combine dpad inputs with normal inputs in the same action set for the same control, this will result in an error being returned when suggested bindings are submitted to OpenXR.

See [XR_EXT_dpad_binding](https://registry.khronos.org/OpenXR/specs/1.1/html/xrspec.html#XR_EXT_dpad_binding) for in-depth details.

**Note:** If the DPad binding modifier extension is enabled, all dpad binding paths will be available in the action map. Adding the modifier to an interaction profile allows you to further customize the behavior.

## 构造函数

### OpenXRDpadBindingModifier

```csharp
public OpenXRDpadBindingModifier()
```

## 属性

### ActionSet

```csharp
public OpenXRActionSet ActionSet { get; set; }
```

引擎名：`action_set`

Action set for which this dpad binding modifier is active.

### CenterRegion

```csharp
public float CenterRegion { get; set; }
```

引擎名：`center_region`

Center region in which our center position of our dpad return `true`.

### InputPath

```csharp
public string InputPath { get; set; }
```

引擎名：`input_path`

Input path for this dpad binding modifier.

### IsSticky

```csharp
public bool IsSticky { get; set; }
```

引擎名：`is_sticky`

If `false`, when the joystick enters a new dpad zone this becomes `true`.

If `true`, when the joystick remains in active dpad zone, this remains `true` even if we overlap with another zone.

### OffHaptic

```csharp
public OpenXRHapticBase OffHaptic { get; set; }
```

引擎名：`off_haptic`

Haptic pulse to emit when the user releases the input.

### OnHaptic

```csharp
public OpenXRHapticBase OnHaptic { get; set; }
```

引擎名：`on_haptic`

Haptic pulse to emit when the user presses the input.

### Threshold

```csharp
public float Threshold { get; set; }
```

引擎名：`threshold`

When our input value is equal or larger than this value, our dpad in that direction becomes `true`. It stays `true` until it falls under the `ThresholdReleased` value.

### ThresholdReleased

```csharp
public float ThresholdReleased { get; set; }
```

引擎名：`threshold_released`

When our input value falls below this, our output becomes `false`.

### WedgeAngle

```csharp
public float WedgeAngle { get; set; }
```

引擎名：`wedge_angle`

The angle of each wedge that identifies the 4 directions of the emulated dpad.

## 方法

### GetActionSet

```csharp
public OpenXRActionSet GetActionSet()
```

引擎名：`get_action_set`

### GetCenterRegion

```csharp
public float GetCenterRegion()
```

引擎名：`get_center_region`

### GetInputPath

```csharp
public string GetInputPath()
```

引擎名：`get_input_path`

### GetIsSticky

```csharp
public bool GetIsSticky()
```

引擎名：`get_is_sticky`

### GetOffHaptic

```csharp
public OpenXRHapticBase GetOffHaptic()
```

引擎名：`get_off_haptic`

### GetOnHaptic

```csharp
public OpenXRHapticBase GetOnHaptic()
```

引擎名：`get_on_haptic`

### GetThreshold

```csharp
public float GetThreshold()
```

引擎名：`get_threshold`

### GetThresholdReleased

```csharp
public float GetThresholdReleased()
```

引擎名：`get_threshold_released`

### GetWedgeAngle

```csharp
public float GetWedgeAngle()
```

引擎名：`get_wedge_angle`

### SetActionSet

```csharp
public void SetActionSet(OpenXRActionSet actionSet)
```

引擎名：`set_action_set`

### SetCenterRegion

```csharp
public void SetCenterRegion(float centerRegion)
```

引擎名：`set_center_region`

### SetInputPath

```csharp
public void SetInputPath(string inputPath)
```

引擎名：`set_input_path`

### SetIsSticky

```csharp
public void SetIsSticky(bool isSticky)
```

引擎名：`set_is_sticky`

### SetOffHaptic

```csharp
public void SetOffHaptic(OpenXRHapticBase haptic)
```

引擎名：`set_off_haptic`

### SetOnHaptic

```csharp
public void SetOnHaptic(OpenXRHapticBase haptic)
```

引擎名：`set_on_haptic`

### SetThreshold

```csharp
public void SetThreshold(float threshold)
```

引擎名：`set_threshold`

### SetThresholdReleased

```csharp
public void SetThresholdReleased(float thresholdReleased)
```

引擎名：`set_threshold_released`

### SetWedgeAngle

```csharp
public void SetWedgeAngle(float wedgeAngle)
```

引擎名：`set_wedge_angle`
