<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioServer

命名空间 `Godot`；static class

`AudioServer` is a low-level server interface for audio access. It is in charge of creating sample data (playable audio) as well as its playback via a voice interface.

## 属性

### BusCount

```csharp
public static int BusCount { get; set; }
```

引擎名：`bus_count`

Number of available audio buses.

### InputDevice

```csharp
public static string InputDevice { get; set; }
```

引擎名：`input_device`

Name of the current device for audio input (see `GetInputDeviceList()`). On systems with multiple audio inputs (such as analog, USB and HDMI audio), this can be used to select the audio input device. The value `"Default"` will record audio on the system-wide default audio input. If an invalid device name is set, the value will be reverted back to `"Default"`.

**Note:** `ProjectSettings.audio/driver/enable_input` must be `true` for audio input to work. See also that setting's description for caveats related to permissions and operating system privacy settings.

### OutputDevice

```csharp
public static string OutputDevice { get; set; }
```

引擎名：`output_device`

Name of the current device for audio output (see `GetOutputDeviceList()`). On systems with multiple audio outputs (such as analog, USB and HDMI audio), this can be used to select the audio output device. The value `"Default"` will play audio on the system-wide default audio output. If an invalid device name is set, the value will be reverted back to `"Default"`.

### PlaybackSpeedScale

```csharp
public static float PlaybackSpeedScale { get; set; }
```

引擎名：`playback_speed_scale`

Scales the rate at which audio is played (i.e. setting it to `0.5` will make the audio be played at half its speed). See also [`Engine.TimeScale`](Engine.md) to affect the general simulation speed, which is independent from `PlaybackSpeedScale`.

### Singleton

```csharp
public static AudioServerInstance Singleton { get; }
```

## 方法

### AddBus

```csharp
public static void AddBus(int atPosition = -1)
```

引擎名：`add_bus`

Adds a bus at `atPosition`.

### AddBusEffect

```csharp
public static void AddBusEffect(int busIdx, AudioEffect effect, int atPosition = -1)
```

引擎名：`add_bus_effect`

Adds an [`AudioEffect`](AudioEffect.md) effect to the bus `busIdx` at `atPosition`.

### GenerateBusLayout

```csharp
public static AudioBusLayout GenerateBusLayout()
```

引擎名：`generate_bus_layout`

Generates an [`AudioBusLayout`](AudioBusLayout.md) using the available buses and effects.

### GetBusChannels

```csharp
public static int GetBusChannels(int busIdx)
```

引擎名：`get_bus_channels`

Returns the number of channels of the bus at index `busIdx`.

### GetBusCount

```csharp
public static int GetBusCount()
```

引擎名：`get_bus_count`

### GetBusEffect

```csharp
public static AudioEffect GetBusEffect(int busIdx, int effectIdx)
```

引擎名：`get_bus_effect`

Returns the [`AudioEffect`](AudioEffect.md) at position `effectIdx` in bus `busIdx`.

### GetBusEffectCount

```csharp
public static int GetBusEffectCount(int busIdx)
```

引擎名：`get_bus_effect_count`

Returns the number of effects on the bus at `busIdx`.

### GetBusEffectInstance

```csharp
public static AudioEffectInstance GetBusEffectInstance(int busIdx, int effectIdx, int channel = 0)
```

引擎名：`get_bus_effect_instance`

Returns the [`AudioEffectInstance`](AudioEffectInstance.md) assigned to the given bus and effect indices (and optionally channel).

### GetBusIndex

```csharp
public static int GetBusIndex(StringName busName)
```

引擎名：`get_bus_index`

Returns the index of the bus with the name `busName`. Returns `-1` if no bus with the specified name exist.

### GetBusName

```csharp
public static string GetBusName(int busIdx)
```

引擎名：`get_bus_name`

Returns the name of the bus with the index `busIdx`.

### GetBusPeakVolumeLeftDb

```csharp
public static float GetBusPeakVolumeLeftDb(int busIdx, int channel)
```

引擎名：`get_bus_peak_volume_left_db`

Returns the peak volume of the left speaker at bus index `busIdx` and channel index `channel`.

### GetBusPeakVolumeRightDb

```csharp
public static float GetBusPeakVolumeRightDb(int busIdx, int channel)
```

引擎名：`get_bus_peak_volume_right_db`

Returns the peak volume of the right speaker at bus index `busIdx` and channel index `channel`.

### GetBusSend

```csharp
public static StringName GetBusSend(int busIdx)
```

引擎名：`get_bus_send`

Returns the name of the bus that the bus at index `busIdx` sends to.

### GetBusVolumeDb

```csharp
public static float GetBusVolumeDb(int busIdx)
```

引擎名：`get_bus_volume_db`

Returns the volume of the bus at index `busIdx` in dB.

### GetBusVolumeLinear

```csharp
public static float GetBusVolumeLinear(int busIdx)
```

引擎名：`get_bus_volume_linear`

Returns the volume of the bus at index `busIdx` as a linear value.

**Note:** The returned value is equivalent to the result of `Mathf.DbToLinear` on the result of `GetBusVolumeDb()`.

### GetDriverName

```csharp
public static string GetDriverName()
```

引擎名：`get_driver_name`

Returns the name of the current audio driver. The default usually depends on the operating system, but may be overridden via the `--audio-driver` [command line argument]($DOCS_URL/tutorials/editor/command_line_tutorial.html). `--headless` also automatically sets the audio driver to `Dummy`. See also `ProjectSettings.audio/driver/driver`.

### GetInputBufferLengthFrames

```csharp
public static int GetInputBufferLengthFrames()
```

引擎名：`get_input_buffer_length_frames`

Returns the absolute size of the microphone input buffer. This is set to a multiple of the audio latency and can be used to estimate the minimum rate at which the frames need to be fetched.

### GetInputDevice

```csharp
public static string GetInputDevice()
```

引擎名：`get_input_device`

### GetInputDeviceList

```csharp
public static string[] GetInputDeviceList()
```

引擎名：`get_input_device_list`

Returns the names of all audio input devices detected on the system.

**Note:** `ProjectSettings.audio/driver/enable_input` must be `true` for audio input to work. See also that setting's description for caveats related to permissions and operating system privacy settings.

### GetInputFrames

```csharp
public static Vector2[] GetInputFrames(int frames)
```

引擎名：`get_input_frames`

Returns a [`Vector2`](Vector2.md)[] containing exactly `frames` audio samples from the internal microphone buffer if available, otherwise returns an empty [`Vector2`](Vector2.md)[].

The buffer is filled at the rate of `GetInputMixRate()` frames per second when `SetInputDeviceActive()` has successfully been set to `true`.

The samples are signed floating-point PCM values between `-1` and `1`.

### GetInputFramesAvailable

```csharp
public static int GetInputFramesAvailable()
```

引擎名：`get_input_frames_available`

Returns the number of frames available to read using `GetInputFrames()`.

### GetInputMixRate

```csharp
public static float GetInputMixRate()
```

引擎名：`get_input_mix_rate`

Returns the sample rate at the input of the `AudioServer`.

### GetMixRate

```csharp
public static float GetMixRate()
```

引擎名：`get_mix_rate`

Returns the sample rate at the output of the `AudioServer`.

### GetOutputDevice

```csharp
public static string GetOutputDevice()
```

引擎名：`get_output_device`

### GetOutputDeviceList

```csharp
public static string[] GetOutputDeviceList()
```

引擎名：`get_output_device_list`

Returns the names of all audio output devices detected on the system.

### GetOutputLatency

```csharp
public static double GetOutputLatency()
```

引擎名：`get_output_latency`

Returns the audio driver's effective output latency. This is based on `ProjectSettings.audio/driver/output_latency`, but the exact returned value will differ depending on the operating system and audio driver.

**Note:** This can be expensive; it is not recommended to call `GetOutputLatency()` every frame.

### GetPlaybackSpeedScale

```csharp
public static float GetPlaybackSpeedScale()
```

引擎名：`get_playback_speed_scale`

### GetSpeakerMode

```csharp
public static AudioServer.SpeakerMode GetSpeakerMode()
```

引擎名：`get_speaker_mode`

Returns the speaker configuration.

### GetTimeSinceLastMix

```csharp
public static double GetTimeSinceLastMix()
```

引擎名：`get_time_since_last_mix`

Returns the relative time since the last mix occurred, in seconds.

### GetTimeToNextMix

```csharp
public static double GetTimeToNextMix()
```

引擎名：`get_time_to_next_mix`

Returns the relative time until the next mix occurs, in seconds.

### IsBusBypassingEffects

```csharp
public static bool IsBusBypassingEffects(int busIdx)
```

引擎名：`is_bus_bypassing_effects`

If `true`, the bus at index `busIdx` is bypassing effects.

### IsBusEffectEnabled

```csharp
public static bool IsBusEffectEnabled(int busIdx, int effectIdx)
```

引擎名：`is_bus_effect_enabled`

If `true`, the effect at index `effectIdx` on the bus at index `busIdx` is enabled.

### IsBusMute

```csharp
public static bool IsBusMute(int busIdx)
```

引擎名：`is_bus_mute`

If `true`, the bus at index `busIdx` is muted.

### IsBusSolo

```csharp
public static bool IsBusSolo(int busIdx)
```

引擎名：`is_bus_solo`

If `true`, the bus at index `busIdx` is in solo mode.

### IsStreamRegisteredAsSample

```csharp
public static bool IsStreamRegisteredAsSample(AudioStream stream)
```

引擎名：`is_stream_registered_as_sample`

If `true`, the stream is registered as a sample. The engine will not have to register it before playing the sample.

If `false`, the stream will have to be registered before playing it. To prevent lag spikes, register the stream as sample with `RegisterStreamAsSample()`.

### Lock

```csharp
public static void Lock()
```

引擎名：`lock`

Locks the audio driver's main loop.

**Note:** Remember to unlock it afterwards.

### MoveBus

```csharp
public static void MoveBus(int index, int toIndex)
```

引擎名：`move_bus`

Moves the bus from index `index` to index `toIndex`.

### RegisterStreamAsSample

```csharp
public static void RegisterStreamAsSample(AudioStream stream)
```

引擎名：`register_stream_as_sample`

Forces the registration of a stream as a sample.

**Note:** Lag spikes may occur when calling this method, especially on single-threaded builds. It is suggested to call this method while loading assets, where the lag spike could be masked, instead of registering the sample right before it needs to be played.

### RemoveBus

```csharp
public static void RemoveBus(int index)
```

引擎名：`remove_bus`

Removes the bus at index `index`.

### RemoveBusEffect

```csharp
public static void RemoveBusEffect(int busIdx, int effectIdx)
```

引擎名：`remove_bus_effect`

Removes the effect at index `effectIdx` from the bus at index `busIdx`.

### SetBusBypassEffects

```csharp
public static void SetBusBypassEffects(int busIdx, bool enable)
```

引擎名：`set_bus_bypass_effects`

If `true`, the bus at index `busIdx` is bypassing effects.

### SetBusCount

```csharp
public static void SetBusCount(int amount)
```

引擎名：`set_bus_count`

### SetBusEffectEnabled

```csharp
public static void SetBusEffectEnabled(int busIdx, int effectIdx, bool enabled)
```

引擎名：`set_bus_effect_enabled`

If `true`, the effect at index `effectIdx` on the bus at index `busIdx` is enabled.

### SetBusLayout

```csharp
public static void SetBusLayout(AudioBusLayout busLayout)
```

引擎名：`set_bus_layout`

Overwrites the currently used [`AudioBusLayout`](AudioBusLayout.md).

### SetBusMute

```csharp
public static void SetBusMute(int busIdx, bool enable)
```

引擎名：`set_bus_mute`

If `true`, the bus at index `busIdx` is muted.

### SetBusName

```csharp
public static void SetBusName(int busIdx, string name)
```

引擎名：`set_bus_name`

Sets the name of the bus at index `busIdx` to `name`.

### SetBusSend

```csharp
public static void SetBusSend(int busIdx, StringName send)
```

引擎名：`set_bus_send`

Connects the output of the bus at `busIdx` to the bus named `send`.

### SetBusSolo

```csharp
public static void SetBusSolo(int busIdx, bool enable)
```

引擎名：`set_bus_solo`

If `true`, the bus at index `busIdx` is in solo mode.

### SetBusVolumeDb

```csharp
public static void SetBusVolumeDb(int busIdx, float volumeDb)
```

引擎名：`set_bus_volume_db`

Sets the volume in decibels of the bus at index `busIdx` to `volumeDb`.

### SetBusVolumeLinear

```csharp
public static void SetBusVolumeLinear(int busIdx, float volumeLinear)
```

引擎名：`set_bus_volume_linear`

Sets the volume as a linear value of the bus at index `busIdx` to `volumeLinear`.

**Note:** Using this method is equivalent to calling `SetBusVolumeDb()` with the result of `Mathf.LinearToDb` on a value.

### SetEnableTaggingUsedAudioStreams

```csharp
public static void SetEnableTaggingUsedAudioStreams(bool enable)
```

引擎名：`set_enable_tagging_used_audio_streams`

If set to `true`, all instances of [`AudioStreamPlayback`](AudioStreamPlayback.md) will call [`AudioStreamPlayback._TagUsedStreams()`](AudioStreamPlayback.md) every mix step.

**Note:** This is enabled by default in the editor, as it is used by editor plugins for the audio stream previews.

### SetInputDevice

```csharp
public static void SetInputDevice(string name)
```

引擎名：`set_input_device`

### SetInputDeviceActive

```csharp
public static Error SetInputDeviceActive(bool active)
```

引擎名：`set_input_device_active`

If `active` is `true`, starts the microphone input stream specified by `InputDevice` or returns an error if it failed.

If `active` is `false`, stops the input stream if it is running.

### SetOutputDevice

```csharp
public static void SetOutputDevice(string name)
```

引擎名：`set_output_device`

### SetPlaybackSpeedScale

```csharp
public static void SetPlaybackSpeedScale(float scale)
```

引擎名：`set_playback_speed_scale`

### SwapBusEffects

```csharp
public static void SwapBusEffects(int busIdx, int effectIdx, int byEffectIdx)
```

引擎名：`swap_bus_effects`

Swaps the position of two effects in bus `busIdx`.

### Unlock

```csharp
public static void Unlock()
```

引擎名：`unlock`

Unlocks the audio driver's main loop. (After locking it, you should always unlock it.)

## 事件（信号）

### BusLayoutChanged

```csharp
public event Action BusLayoutChanged
```

引擎名：`bus_layout_changed`

Emitted when an audio bus is added, deleted, or moved.

### BusRenamed

```csharp
public event AudioServer.BusRenamedEventHandler BusRenamed
```

引擎名：`bus_renamed`

Emitted when the audio bus at `busIndex` is renamed from `oldName` to `newName`.

## BusRenamedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`AudioServer.BusRenamed`](AudioServer.md) event of a [`AudioServer`](AudioServer.md) class.

### 签名

#### Invoke

```csharp
delegate void BusRenamedEventHandler(long busIndex, StringName oldName, StringName newName)
```

## PlaybackType

命名空间 `Godot`；enum

### 枚举值

- `Default = 0`：The playback will be considered of the type declared at `ProjectSettings.audio/general/default_playback_type`.
- `Stream = 1`：Force the playback to be considered as a stream.
- `Sample = 2`：Force the playback to be considered as a sample.
- `Max = 3`：Represents the size of the `AudioServer.PlaybackType` enum.

## SpeakerMode

命名空间 `Godot`；enum

### 枚举值

- `ModeStereo = 0`：Two or fewer speakers were detected.
- `Surround31 = 1`：A 3.1 channel surround setup was detected.
- `Surround51 = 2`：A 5.1 channel surround setup was detected.
- `Surround71 = 3`：A 7.1 channel surround setup was detected.
