<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioStreamPlayer3D

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Plays audio with positional sound effects, based on the relative position of the audio listener. Positional effects include distance attenuation, directionality, and the Doppler effect. For greater realism, a low-pass filter is applied to distant sounds. This can be disabled by setting `AttenuationFilterCutoffHz` to `20500`.

By default, audio is heard from the camera position. This can be changed by adding an [`AudioListener3D`](AudioListener3D.md) node to the scene and enabling it by calling [`AudioListener3D.MakeCurrent`](AudioListener3D.md) on it.

See also [`AudioStreamPlayer`](AudioStreamPlayer.md) to play a sound non-positionally.

**Note:** Hiding an `AudioStreamPlayer3D` node does not disable its audio output. To temporarily disable an `AudioStreamPlayer3D`'s audio output, set `VolumeDb` to a very low value like `-100` (which isn't audible to human hearing).

## 构造函数

### AudioStreamPlayer3D

```csharp
public AudioStreamPlayer3D()
```

## 属性

### AreaMask

```csharp
public uint AreaMask { get; set; }
```

引擎名：`area_mask`

Determines which [`Area3D`](Area3D.md) layers affect the sound for reverb and audio bus effects. Areas can be used to redirect [`AudioStream`](AudioStream.md)s so that they play in a certain audio bus. An example of how you might use this is making a "water" area so that sounds played in the water are redirected through an audio bus to make them sound like they are being played underwater.

### AttenuationFilterCutoffHz

```csharp
public float AttenuationFilterCutoffHz { get; set; }
```

引擎名：`attenuation_filter_cutoff_hz`

The cutoff frequency of the attenuation low-pass filter, in Hz. A sound above this frequency is attenuated more than a sound below this frequency. To disable this effect, set this to `20500` as this frequency is above the human hearing limit.

### AttenuationFilterDb

```csharp
public float AttenuationFilterDb { get; set; }
```

引擎名：`attenuation_filter_db`

Amount how much the filter affects the loudness, in decibels.

### AttenuationModel

```csharp
public AudioStreamPlayer3D.AttenuationModelEnum AttenuationModel { get; set; }
```

引擎名：`attenuation_model`

Decides if audio should get quieter with distance linearly, quadratically, logarithmically, or not be affected by distance, effectively disabling attenuation.

### Autoplay

```csharp
public bool Autoplay { get; set; }
```

引擎名：`autoplay`

If `true`, audio plays when the AudioStreamPlayer3D node is added to scene tree.

### Bus

```csharp
public StringName Bus { get; set; }
```

引擎名：`bus`

The bus on which this audio is playing.

**Note:** When setting this property, keep in mind that no validation is performed to see if the given name matches an existing bus. This is because audio bus layouts might be loaded after this property is set. If this given name can't be resolved at runtime, it will fall back to `"Master"`.

### DopplerTracking

```csharp
public AudioStreamPlayer3D.DopplerTrackingEnum DopplerTracking { get; set; }
```

引擎名：`doppler_tracking`

Decides in which step the Doppler effect should be calculated.

**Note:** If `DopplerTracking` is not [`AudioStreamPlayer3D.DopplerTrackingEnum.Disabled`](AudioStreamPlayer3D.md) but the current [`Camera3D`](Camera3D.md)/[`AudioListener3D`](AudioListener3D.md) has doppler tracking disabled, the Doppler effect will be heard but will not take the movement of the current listener into account. If accurate Doppler effect is desired, doppler tracking should be enabled on both the `AudioStreamPlayer3D` and the current [`Camera3D`](Camera3D.md)/[`AudioListener3D`](AudioListener3D.md).

### EmissionAngleDegrees

```csharp
public float EmissionAngleDegrees { get; set; }
```

引擎名：`emission_angle_degrees`

The angle in which the audio reaches a listener unattenuated.

### EmissionAngleEnabled

```csharp
public bool EmissionAngleEnabled { get; set; }
```

引擎名：`emission_angle_enabled`

If `true`, the audio should be attenuated according to the direction of the sound.

### EmissionAngleFilterAttenuationDb

```csharp
public float EmissionAngleFilterAttenuationDb { get; set; }
```

引擎名：`emission_angle_filter_attenuation_db`

Attenuation factor used if listener is outside of `EmissionAngleDegrees` and `EmissionAngleEnabled` is set, in decibels.

### MaxDb

```csharp
public float MaxDb { get; set; }
```

引擎名：`max_db`

Sets the absolute maximum of the sound level, in decibels.

### MaxDistance

```csharp
public float MaxDistance { get; set; }
```

引擎名：`max_distance`

The distance past which the sound can no longer be heard at all. Only has an effect if set to a value greater than `0.0`. `MaxDistance` works in tandem with `UnitSize`. However, unlike `UnitSize` whose behavior depends on the `AttenuationModel`, `MaxDistance` always works in a linear fashion. This can be used to prevent the `AudioStreamPlayer3D` from requiring audio mixing when the listener is far away, which saves CPU resources.

### MaxPolyphony

```csharp
public int MaxPolyphony { get; set; }
```

引擎名：`max_polyphony`

The maximum number of sounds this node can play at the same time. Playing additional sounds after this value is reached will cut off the oldest sounds.

### PanningStrength

```csharp
public float PanningStrength { get; set; }
```

引擎名：`panning_strength`

Scales the panning strength for this node by multiplying the base `ProjectSettings.audio/general/3d_panning_strength` by this factor. If the product is `0.0` then stereo panning is disabled and the volume is the same for all channels. If the product is `1.0` then one of the channels will be muted when the sound is located exactly to the left (or right) of the listener.

Two speaker stereo arrangements implement the [WebAudio standard for StereoPannerNode Panning](https://webaudio.github.io/web-audio-api/#stereopanner-algorithm) where the volume is cosine of half the azimuth angle to the ear.

For other speaker arrangements such as the 5.1 and 7.1 the SPCAP (Speaker-Placement Correction Amplitude) algorithm is implemented.

### PitchScale

```csharp
public float PitchScale { get; set; }
```

引擎名：`pitch_scale`

The pitch and the tempo of the audio, as a multiplier of the audio sample's sample rate.

### PlaybackType

```csharp
public AudioServer.PlaybackType PlaybackType { get; set; }
```

引擎名：`playback_type`

The playback type of the stream player. If set other than to the default value, it will force that playback type.

### Playing

```csharp
public bool Playing { get; set; }
```

引擎名：`playing`

If `true`, audio is playing or is queued to be played (see `Play()`).

### Stream

```csharp
public AudioStream Stream { get; set; }
```

引擎名：`stream`

The [`AudioStream`](AudioStream.md) resource to be played.

### StreamPaused

```csharp
public bool StreamPaused { get; set; }
```

引擎名：`stream_paused`

If `true`, the playback is paused. You can resume it by setting `StreamPaused` to `false`.

### UnitSize

```csharp
public float UnitSize { get; set; }
```

引擎名：`unit_size`

The factor for the attenuation effect. Higher values make the sound audible over a larger distance.

### VolumeDb

```csharp
public float VolumeDb { get; set; }
```

引擎名：`volume_db`

The base sound level before attenuation, in decibels.

### VolumeLinear

```csharp
public float VolumeLinear { get; set; }
```

引擎名：`volume_linear`

The base sound level before attenuation, as a linear value.

**Note:** This member modifies `VolumeDb` for convenience. The returned value is equivalent to the result of `Mathf.DbToLinear` on `VolumeDb`. Setting this member is equivalent to setting `VolumeDb` to the result of `Mathf.LinearToDb` on a value.

## 方法

### GetAreaMask

```csharp
public uint GetAreaMask()
```

引擎名：`get_area_mask`

### GetAttenuationFilterCutoffHz

```csharp
public float GetAttenuationFilterCutoffHz()
```

引擎名：`get_attenuation_filter_cutoff_hz`

### GetAttenuationFilterDb

```csharp
public float GetAttenuationFilterDb()
```

引擎名：`get_attenuation_filter_db`

### GetAttenuationModel

```csharp
public AudioStreamPlayer3D.AttenuationModelEnum GetAttenuationModel()
```

引擎名：`get_attenuation_model`

### GetBus

```csharp
public StringName GetBus()
```

引擎名：`get_bus`

### GetDopplerTracking

```csharp
public AudioStreamPlayer3D.DopplerTrackingEnum GetDopplerTracking()
```

引擎名：`get_doppler_tracking`

### GetEmissionAngle

```csharp
public float GetEmissionAngle()
```

引擎名：`get_emission_angle`

### GetEmissionAngleFilterAttenuationDb

```csharp
public float GetEmissionAngleFilterAttenuationDb()
```

引擎名：`get_emission_angle_filter_attenuation_db`

### GetMaxDb

```csharp
public float GetMaxDb()
```

引擎名：`get_max_db`

### GetMaxDistance

```csharp
public float GetMaxDistance()
```

引擎名：`get_max_distance`

### GetMaxPolyphony

```csharp
public int GetMaxPolyphony()
```

引擎名：`get_max_polyphony`

### GetPanningStrength

```csharp
public float GetPanningStrength()
```

引擎名：`get_panning_strength`

### GetPitchScale

```csharp
public float GetPitchScale()
```

引擎名：`get_pitch_scale`

### GetPlaybackPosition

```csharp
public float GetPlaybackPosition()
```

引擎名：`get_playback_position`

Returns the position in the [`AudioStream`](AudioStream.md).

### GetPlaybackType

```csharp
public AudioServer.PlaybackType GetPlaybackType()
```

引擎名：`get_playback_type`

### GetStream

```csharp
public AudioStream GetStream()
```

引擎名：`get_stream`

### GetStreamPaused

```csharp
public bool GetStreamPaused()
```

引擎名：`get_stream_paused`

### GetStreamPlayback

```csharp
public AudioStreamPlayback GetStreamPlayback()
```

引擎名：`get_stream_playback`

Returns the [`AudioStreamPlayback`](AudioStreamPlayback.md) object associated with this `AudioStreamPlayer3D`.

### GetUnitSize

```csharp
public float GetUnitSize()
```

引擎名：`get_unit_size`

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

### HasStreamPlayback

```csharp
public bool HasStreamPlayback()
```

引擎名：`has_stream_playback`

Returns whether the [`AudioStreamPlayer`](AudioStreamPlayer.md) can return the [`AudioStreamPlayback`](AudioStreamPlayback.md) object or not.

### IsAutoplayEnabled

```csharp
public bool IsAutoplayEnabled()
```

引擎名：`is_autoplay_enabled`

### IsEmissionAngleEnabled

```csharp
public bool IsEmissionAngleEnabled()
```

引擎名：`is_emission_angle_enabled`

### IsPlaying

```csharp
public bool IsPlaying()
```

引擎名：`is_playing`

### Play

```csharp
public void Play(float fromPosition = 0f)
```

引擎名：`play`

Queues the audio to play on the next physics frame, from the given position `fromPosition`, in seconds.

### Seek

```csharp
public void Seek(float toPosition)
```

引擎名：`seek`

Sets the position from which audio will be played, in seconds.

### SetAreaMask

```csharp
public void SetAreaMask(uint mask)
```

引擎名：`set_area_mask`

### SetAttenuationFilterCutoffHz

```csharp
public void SetAttenuationFilterCutoffHz(float degrees)
```

引擎名：`set_attenuation_filter_cutoff_hz`

### SetAttenuationFilterDb

```csharp
public void SetAttenuationFilterDb(float db)
```

引擎名：`set_attenuation_filter_db`

### SetAttenuationModel

```csharp
public void SetAttenuationModel(AudioStreamPlayer3D.AttenuationModelEnum model)
```

引擎名：`set_attenuation_model`

### SetAutoplay

```csharp
public void SetAutoplay(bool enable)
```

引擎名：`set_autoplay`

### SetBus

```csharp
public void SetBus(StringName bus)
```

引擎名：`set_bus`

### SetDopplerTracking

```csharp
public void SetDopplerTracking(AudioStreamPlayer3D.DopplerTrackingEnum mode)
```

引擎名：`set_doppler_tracking`

### SetEmissionAngle

```csharp
public void SetEmissionAngle(float degrees)
```

引擎名：`set_emission_angle`

### SetEmissionAngleEnabled

```csharp
public void SetEmissionAngleEnabled(bool enabled)
```

引擎名：`set_emission_angle_enabled`

### SetEmissionAngleFilterAttenuationDb

```csharp
public void SetEmissionAngleFilterAttenuationDb(float db)
```

引擎名：`set_emission_angle_filter_attenuation_db`

### SetMaxDb

```csharp
public void SetMaxDb(float maxDb)
```

引擎名：`set_max_db`

### SetMaxDistance

```csharp
public void SetMaxDistance(float meters)
```

引擎名：`set_max_distance`

### SetMaxPolyphony

```csharp
public void SetMaxPolyphony(int maxPolyphony)
```

引擎名：`set_max_polyphony`

### SetPanningStrength

```csharp
public void SetPanningStrength(float panningStrength)
```

引擎名：`set_panning_strength`

### SetPitchScale

```csharp
public void SetPitchScale(float pitchScale)
```

引擎名：`set_pitch_scale`

### SetPlaybackType

```csharp
public void SetPlaybackType(AudioServer.PlaybackType playbackType)
```

引擎名：`set_playback_type`

### SetPlaying

```csharp
public void SetPlaying(bool enable)
```

引擎名：`set_playing`

### SetStream

```csharp
public void SetStream(AudioStream stream)
```

引擎名：`set_stream`

### SetStreamPaused

```csharp
public void SetStreamPaused(bool pause)
```

引擎名：`set_stream_paused`

### SetUnitSize

```csharp
public void SetUnitSize(float unitSize)
```

引擎名：`set_unit_size`

### SetVolumeDb

```csharp
public void SetVolumeDb(float volumeDb)
```

引擎名：`set_volume_db`

### SetVolumeLinear

```csharp
public void SetVolumeLinear(float volumeLinear)
```

引擎名：`set_volume_linear`

### Stop

```csharp
public void Stop()
```

引擎名：`stop`

Stops the audio.

## 事件（信号）

### Finished

```csharp
public event Action Finished
```

引擎名：`finished`

Emitted when the audio stops playing.

## AttenuationModelEnum

命名空间 `Godot`；enum

### 枚举值

- `InverseDistance = 0`：Attenuation of loudness according to linear distance.
- `InverseSquareDistance = 1`：Attenuation of loudness according to squared distance.
- `Logarithmic = 2`：Attenuation of loudness according to logarithmic distance.
- `Disabled = 3`：No attenuation of loudness according to distance.

## DopplerTrackingEnum

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Disables doppler tracking.
- `IdleStep = 1`：Executes doppler tracking during process frames (see [`Node.NotificationInternalProcess`](Node.md)).
- `PhysicsStep = 2`：Executes doppler tracking during physics frames (see [`Node.NotificationInternalPhysicsProcess`](Node.md)).
