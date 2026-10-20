<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioEffectAmplify

命名空间 `Godot`；class；继承 [`AudioEffect`](AudioEffect.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Increases or decreases the volume being routed through the audio bus.

## 构造函数

### AudioEffectAmplify

```csharp
public AudioEffectAmplify()
```

## 属性

### VolumeDb

```csharp
public float VolumeDb { get; set; }
```

引擎名：`volume_db`

Amount of amplification in dB. Positive values make the sound louder, negative values make it quieter. Value can range from -80 to 24.

### VolumeLinear

```csharp
public float VolumeLinear { get; set; }
```

引擎名：`volume_linear`

Amount of amplification as a linear value.

**Note:** This member modifies `VolumeDb` for convenience. The returned value is equivalent to the result of `Mathf.DbToLinear` on `VolumeDb`. Setting this member is equivalent to setting `VolumeDb` to the result of `Mathf.LinearToDb` on a value.

## 方法

### GetVolumeDb

```csharp
public float GetVolumeDb()
```

引擎名：`get_volume_db`

### GetVolumeLinear

```csharp
public float GetVolumeLinear()
```

引擎名：`get_volume_linear`

### SetVolumeDb

```csharp
public void SetVolumeDb(float volume)
```

引擎名：`set_volume_db`

### SetVolumeLinear

```csharp
public void SetVolumeLinear(float volume)
```

引擎名：`set_volume_linear`
