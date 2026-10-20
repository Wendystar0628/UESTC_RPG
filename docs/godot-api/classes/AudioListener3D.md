<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioListener3D

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Once added to the scene tree and enabled using `MakeCurrent()`, this node will override the location sounds are heard from. This can be used to listen from a location different from the [`Camera3D`](Camera3D.md).

## 构造函数

### AudioListener3D

```csharp
public AudioListener3D()
```

## 属性

### DopplerTracking

```csharp
public AudioListener3D.DopplerTrackingEnum DopplerTracking { get; set; }
```

引擎名：`doppler_tracking`

If not [`AudioListener3D.DopplerTrackingEnum.Disabled`](AudioListener3D.md), this listener will simulate the [Doppler effect](https://en.wikipedia.org/wiki/Doppler_effect) for objects changed in particular `_process` methods.

**Note:** The Doppler effect will only be heard on [`AudioStreamPlayer3D`](AudioStreamPlayer3D.md)s if [`AudioStreamPlayer3D.DopplerTracking`](AudioStreamPlayer3D.md) is not set to [`AudioStreamPlayer3D.DopplerTrackingEnum.Disabled`](AudioStreamPlayer3D.md).

## 方法

### ClearCurrent

```csharp
public void ClearCurrent()
```

引擎名：`clear_current`

Disables the listener to use the current camera's listener instead.

### GetDopplerTracking

```csharp
public AudioListener3D.DopplerTrackingEnum GetDopplerTracking()
```

引擎名：`get_doppler_tracking`

### GetListenerTransform

```csharp
public Transform3D GetListenerTransform()
```

引擎名：`get_listener_transform`

Returns the listener's global orthonormalized [`Transform3D`](Transform3D.md).

### IsCurrent

```csharp
public bool IsCurrent()
```

引擎名：`is_current`

Returns `true` if the listener was made current using `MakeCurrent()`, `false` otherwise.

**Note:** There may be more than one AudioListener3D marked as "current" in the scene tree, but only the one that was made current last will be used.

### MakeCurrent

```csharp
public void MakeCurrent()
```

引擎名：`make_current`

Enables the listener. This will override the current camera's listener.

### SetDopplerTracking

```csharp
public void SetDopplerTracking(AudioListener3D.DopplerTrackingEnum mode)
```

引擎名：`set_doppler_tracking`

## DopplerTrackingEnum

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Disables [Doppler effect](https://en.wikipedia.org/wiki/Doppler_effect) simulation (default).
- `IdleStep = 1`：Simulate [Doppler effect](https://en.wikipedia.org/wiki/Doppler_effect) by tracking positions of objects that are changed in `_process`.
- `PhysicsStep = 2`：Simulate [Doppler effect](https://en.wikipedia.org/wiki/Doppler_effect) by tracking positions of objects that are changed in `_physics_process`.
