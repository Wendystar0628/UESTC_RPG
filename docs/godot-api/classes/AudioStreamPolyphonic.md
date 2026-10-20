<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioStreamPolyphonic

命名空间 `Godot`；class；继承 [`AudioStream`](AudioStream.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

AudioStream that lets the user play custom streams at any time from code, simultaneously using a single player.

Playback control is done via the [`AudioStreamPlaybackPolyphonic`](AudioStreamPlaybackPolyphonic.md) instance set inside the player, which can be obtained via [`AudioStreamPlayer.GetStreamPlayback`](AudioStreamPlayer.md), [`AudioStreamPlayer2D.GetStreamPlayback`](AudioStreamPlayer2D.md) or [`AudioStreamPlayer3D.GetStreamPlayback`](AudioStreamPlayer3D.md) methods. Obtaining the playback instance is only valid after the `stream` property is set as an `AudioStreamPolyphonic` in those players.

## 构造函数

### AudioStreamPolyphonic

```csharp
public AudioStreamPolyphonic()
```

## 属性

### Polyphony

```csharp
public int Polyphony { get; set; }
```

引擎名：`polyphony`

Maximum amount of simultaneous streams that can be played.

## 方法

### GetPolyphony

```csharp
public int GetPolyphony()
```

引擎名：`get_polyphony`

### SetPolyphony

```csharp
public void SetPolyphony(int voices)
```

引擎名：`set_polyphony`
