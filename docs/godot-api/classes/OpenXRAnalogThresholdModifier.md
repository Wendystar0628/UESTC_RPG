<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRAnalogThresholdModifier

命名空间 `Godot`；class；继承 [`OpenXRActionBindingModifier`](OpenXRActionBindingModifier.md) → [`OpenXRBindingModifier`](OpenXRBindingModifier.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

The analog threshold binding modifier can modify a float input to a boolean input with specified thresholds.

See [XR_VALVE_analog_threshold](https://registry.khronos.org/OpenXR/specs/1.1/html/xrspec.html#XR_VALVE_analog_threshold) for in-depth details.

## 构造函数

### OpenXRAnalogThresholdModifier

```csharp
public OpenXRAnalogThresholdModifier()
```

## 属性

### OffHaptic

```csharp
public OpenXRHapticBase OffHaptic { get; set; }
```

引擎名：`off_haptic`

Haptic pulse to emit when the user releases the input.

### OffThreshold

```csharp
public float OffThreshold { get; set; }
```

引擎名：`off_threshold`

When our input value falls below this, our output becomes `false`.

### OnHaptic

```csharp
public OpenXRHapticBase OnHaptic { get; set; }
```

引擎名：`on_haptic`

Haptic pulse to emit when the user presses the input.

### OnThreshold

```csharp
public float OnThreshold { get; set; }
```

引擎名：`on_threshold`

When our input value is equal or larger than this value, our output becomes `true`. It stays `true` until it falls under the `OffThreshold` value.

## 方法

### GetOffHaptic

```csharp
public OpenXRHapticBase GetOffHaptic()
```

引擎名：`get_off_haptic`

### GetOffThreshold

```csharp
public float GetOffThreshold()
```

引擎名：`get_off_threshold`

### GetOnHaptic

```csharp
public OpenXRHapticBase GetOnHaptic()
```

引擎名：`get_on_haptic`

### GetOnThreshold

```csharp
public float GetOnThreshold()
```

引擎名：`get_on_threshold`

### SetOffHaptic

```csharp
public void SetOffHaptic(OpenXRHapticBase haptic)
```

引擎名：`set_off_haptic`

### SetOffThreshold

```csharp
public void SetOffThreshold(float offThreshold)
```

引擎名：`set_off_threshold`

### SetOnHaptic

```csharp
public void SetOnHaptic(OpenXRHapticBase haptic)
```

引擎名：`set_on_haptic`

### SetOnThreshold

```csharp
public void SetOnThreshold(float onThreshold)
```

引擎名：`set_on_threshold`
