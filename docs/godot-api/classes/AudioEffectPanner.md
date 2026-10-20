<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioEffectPanner

命名空间 `Godot`；class；继承 [`AudioEffect`](AudioEffect.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Determines how much of the audio signal is sent to the left and right channels. This helps with audio spatialization, giving sounds distinct places in a mix.

[`AudioStreamPlayer2D`](AudioStreamPlayer2D.md) and [`AudioStreamPlayer3D`](AudioStreamPlayer3D.md) handle panning automatically, following where the source of the sound is on the screen.

## 构造函数

### AudioEffectPanner

```csharp
public AudioEffectPanner()
```

## 属性

### Pan

```csharp
public float Pan { get; set; }
```

引擎名：`pan`

Pan position. Negative values pan the sound to the left, positive pan to the right. Value can range from -1 to 1.

## 方法

### GetPan

```csharp
public float GetPan()
```

引擎名：`get_pan`

### SetPan

```csharp
public void SetPan(float cpanume)
```

引擎名：`set_pan`
