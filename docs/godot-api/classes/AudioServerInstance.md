<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioServerInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

[`AudioServer`](AudioServer.md) is a low-level server interface for audio access. It is in charge of creating sample data (playable audio) as well as its playback via a voice interface.

## 属性

### BusCount

```csharp
public int BusCount { get; set; }
```

Number of available audio buses.

### InputDevice

```csharp
public string InputDevice { get; set; }
```

Name of the current device for audio input (see `GetInputDeviceList()`). On systems with multiple audio inputs (such as analog, USB and HDMI audio), this can be used to select the audio input device. The value `"Default"` will record audio on the system-wide default audio input. If an invalid device name is set, the value will be reverted back to `"Default"`.

**Note:** `ProjectSettings.audio/driver/enable_input` must be `true` for audio input to work. See also that setting's description for caveats related to permissions and operating system privacy settings.

### OutputDevice

```csharp
public string OutputDevice { get; set; }
```

Name of the current device for audio output (see `GetOutputDeviceList()`). On systems with multiple audio outputs (such as analog, USB and HDMI audio), this can be used to select the audio output device. The value `"Default"` will play audio on the system-wide default audio output. If an invalid device name is set, the value will be reverted back to `"Default"`.

### PlaybackSpeedScale

```csharp
public float PlaybackSpeedScale { get; set; }
```

Scales the rate at which audio is played (i.e. setting it to `0.5` will make the audio be played at half its speed). See also [`Engine.TimeScale`](Engine.md) to affect the general simulation speed, which is independent from [`AudioServer.PlaybackSpeedScale`](AudioServer.md).

## 方法

### AddBus

```csharp
public void AddBus(int atPosition = -1)
```

Adds a bus at `atPosition`.

### AddBusEffect

```csharp
public void AddBusEffect(int busIdx, AudioEffect effect, int atPosition = -1)
```

Adds an [`AudioEffect`](AudioEffect.md) effect to the bus `busIdx` at `atPosition`.

### GenerateBusLayout

```csharp
public AudioBusLayout GenerateBusLayout()
```

Generates an [`AudioBusLayout`](AudioBusLayout.md) using the available buses and effects.

### GetBusChannels

```csharp
public int GetBusChannels(int busIdx)
```

Returns the number of channels of the bus at index `busIdx`.

### GetBusCount

```csharp
public int GetBusCount()
```

### GetBusEffect

```csharp
public AudioEffect GetBusEffect(int busIdx, int effectIdx)
```

Returns the [`AudioEffect`](AudioEffect.md) at position `effectIdx` in bus `busIdx`.

### GetBusEffectCount

```csharp
public int GetBusEffectCount(int busIdx)
```

Returns the number of effects on the bus at `busIdx`.

### GetBusEffectInstance

```csharp
public AudioEffectInstance GetBusEffectInstance(int busIdx, int effectIdx, int channel = 0)
```

Returns the [`AudioEffectInstance`](AudioEffectInstance.md) assigned to the given bus and effect indices (and optionally channel).

### GetBusIndex

```csharp
public int GetBusIndex(StringName busName)
```

Returns the index of the bus with the name `busName`. Returns `-1` if no bus with the specified name exist.

### GetBusName

```csharp
public string GetBusName(int busIdx)
```

Returns the name of the bus with the index `busIdx`.

### GetBusPeakVolumeLeftDb

```csharp
public float GetBusPeakVolumeLeftDb(int busIdx, int channel)
```

Returns the peak volume of the left speaker at bus index `busIdx` and channel index `channel`.

### GetBusPeakVolumeRightDb

```csharp
public float GetBusPeakVolumeRightDb(int busIdx, int channel)
```

Returns the peak volume of the right speaker at bus index `busIdx` and channel index `channel`.

### GetBusSend

```csharp
public StringName GetBusSend(int busIdx)
```

Returns the name of the bus that the bus at index `busIdx` sends to.

### GetBusVolumeDb

```csharp
public float GetBusVolumeDb(int busIdx)
```

Returns the volume of the bus at index `busIdx` in dB.

### GetBusVolumeLinear

```csharp
public float GetBusVolumeLinear(int busIdx)
```

Returns the volume of the bus at index `busIdx` as a linear value.

**Note:** The returned value is equivalent to the result of `Mathf.DbToLinear` on the result of `GetBusVolumeDb()`.

### GetDriverName

```csharp
public string GetDriverName()
```

Returns the name of the current audio driver. The default usually depends on the operating system, but may be overridden via the `--audio-driver` [command line argument]($DOCS_URL/tutorials/editor/command_line_tutorial.html). `--headless` also automatically sets the audio driver to `Dummy`. See also `ProjectSettings.audio/driver/driver`.

### GetInputBufferLengthFrames

```csharp
public int GetInputBufferLengthFrames()
```

Returns the absolute size of the microphone input buffer. This is set to a multiple of the audio latency and can be used to estimate the minimum rate at which the frames need to be fetched.

### GetInputDevice

```csharp
public string GetInputDevice()
```

### GetInputDeviceList

```csharp
public string[] GetInputDeviceList()
```

Returns the names of all audio input devices detected on the system.

**Note:** `ProjectSettings.audio/driver/enable_input` must be `true` for audio input to work. See also that setting's description for caveats related to permissions and operating system privacy settings.

### GetInputFrames

```csharp
public Vector2[] GetInputFrames(int frames)
```

Returns a [`Vector2`](Vector2.md)[] containing exactly `frames` audio samples from the internal microphone buffer if available, otherwise returns an empty [`Vector2`](Vector2.md)[].

The buffer is filled at the rate of `GetInputMixRate()` frames per second when `SetInputDeviceActive()` has successfully been set to `true`.

The samples are signed floating-point PCM values between `-1` and `1`.

### GetInputFramesAvailable

```csharp
public int GetInputFramesAvailable()
```

Returns the number of frames available to read using `GetInputFrames()`.

### GetInputMixRate

```csharp
public float GetInputMixRate()
```

Returns the sample rate at the input of the [`AudioServer`](AudioServer.md).

### GetMixRate

```csharp
public float GetMixRate()
```

Returns the sample rate at the output of the [`AudioServer`](AudioServer.md).

### GetOutputDevice

```csharp
public string GetOutputDevice()
```

### GetOutputDeviceList

```csharp
public string[] GetOutputDeviceList()
```

Returns the names of all audio output devices detected on the system.

### GetOutputLatency

```csharp
public double GetOutputLatency()
```

Returns the audio driver's effective output latency. This is based on `ProjectSettings.audio/driver/output_latency`, but the exact returned value will differ depending on the operating system and audio driver.

**Note:** This can be expensive; it is not recommended to call `GetOutputLatency()` every frame.

### GetPlaybackSpeedScale

```csharp
public float GetPlaybackSpeedScale()
```

### GetSpeakerMode

```csharp
public AudioServer.SpeakerMode GetSpeakerMode()
```

Returns the speaker configuration.

### GetTimeSinceLastMix

```csharp
public double GetTimeSinceLastMix()
```

Returns the relative time since the last mix occurred, in seconds.

### GetTimeToNextMix

```csharp
public double GetTimeToNextMix()
```

Returns the relative time until the next mix occurs, in seconds.

### IsBusBypassingEffects

```csharp
public bool IsBusBypassingEffects(int busIdx)
```

If `true`, the bus at index `busIdx` is bypassing effects.

### IsBusEffectEnabled

```csharp
public bool IsBusEffectEnabled(int busIdx, int effectIdx)
```

If `true`, the effect at index `effectIdx` on the bus at index `busIdx` is enabled.

### IsBusMute

```csharp
public bool IsBusMute(int busIdx)
```

If `true`, the bus at index `busIdx` is muted.

### IsBusSolo

```csharp
public bool IsBusSolo(int busIdx)
```

If `true`, the bus at index `busIdx` is in solo mode.

### IsStreamRegisteredAsSample

```csharp
public bool IsStreamRegisteredAsSample(AudioStream stream)
```

If `true`, the stream is registered as a sample. The engine will not have to register it before playing the sample.

If `false`, the stream will have to be registered before playing it. To prevent lag spikes, register the stream as sample with `RegisterStreamAsSample()`.

### Lock

```csharp
public void Lock()
```

Locks the audio driver's main loop.

**Note:** Remember to unlock it afterwards.

### MoveBus

```csharp
public void MoveBus(int index, int toIndex)
```

Moves the bus from index `index` to index `toIndex`.

### RegisterStreamAsSample

```csharp
public void RegisterStreamAsSample(AudioStream stream)
```

Forces the registration of a stream as a sample.

**Note:** Lag spikes may occur when calling this method, especially on single-threaded builds. It is suggested to call this method while loading assets, where the lag spike could be masked, instead of registering the sample right before it needs to be played.

### RemoveBus

```csharp
public void RemoveBus(int index)
```

Removes the bus at index `index`.

### RemoveBusEffect

```csharp
public void RemoveBusEffect(int busIdx, int effectIdx)
```

Removes the effect at index `effectIdx` from the bus at index `busIdx`.

### SetBusBypassEffects

```csharp
public void SetBusBypassEffects(int busIdx, bool enable)
```

If `true`, the bus at index `busIdx` is bypassing effects.

### SetBusCount

```csharp
public void SetBusCount(int amount)
```

### SetBusEffectEnabled

```csharp
public void SetBusEffectEnabled(int busIdx, int effectIdx, bool enabled)
```

If `true`, the effect at index `effectIdx` on the bus at index `busIdx` is enabled.

### SetBusLayout

```csharp
public void SetBusLayout(AudioBusLayout busLayout)
```

Overwrites the currently used [`AudioBusLayout`](AudioBusLayout.md).

### SetBusMute

```csharp
public void SetBusMute(int busIdx, bool enable)
```

If `true`, the bus at index `busIdx` is muted.

### SetBusName

```csharp
public void SetBusName(int busIdx, string name)
```

Sets the name of the bus at index `busIdx` to `name`.

### SetBusSend

```csharp
public void SetBusSend(int busIdx, StringName send)
```

Connects the output of the bus at `busIdx` to the bus named `send`.

### SetBusSolo

```csharp
public void SetBusSolo(int busIdx, bool enable)
```

If `true`, the bus at index `busIdx` is in solo mode.

### SetBusVolumeDb

```csharp
public void SetBusVolumeDb(int busIdx, float volumeDb)
```

Sets the volume in decibels of the bus at index `busIdx` to `volumeDb`.

### SetBusVolumeLinear

```csharp
public void SetBusVolumeLinear(int busIdx, float volumeLinear)
```

Sets the volume as a linear value of the bus at index `busIdx` to `volumeLinear`.

**Note:** Using this method is equivalent to calling `SetBusVolumeDb()` with the result of `Mathf.LinearToDb` on a value.

### SetEnableTaggingUsedAudioStreams

```csharp
public void SetEnableTaggingUsedAudioStreams(bool enable)
```

If set to `true`, all instances of [`AudioStreamPlayback`](AudioStreamPlayback.md) will call [`AudioStreamPlayback._TagUsedStreams()`](AudioStreamPlayback.md) every mix step.

**Note:** This is enabled by default in the editor, as it is used by editor plugins for the audio stream previews.

### SetInputDevice

```csharp
public void SetInputDevice(string name)
```

### SetInputDeviceActive

```csharp
public Error SetInputDeviceActive(bool active)
```

If `active` is `true`, starts the microphone input stream specified by `InputDevice` or returns an error if it failed.

If `active` is `false`, stops the input stream if it is running.

### SetOutputDevice

```csharp
public void SetOutputDevice(string name)
```

### SetPlaybackSpeedScale

```csharp
public void SetPlaybackSpeedScale(float scale)
```

### SwapBusEffects

```csharp
public void SwapBusEffects(int busIdx, int effectIdx, int byEffectIdx)
```

Swaps the position of two effects in bus `busIdx`.

### Unlock

```csharp
public void Unlock()
```

Unlocks the audio driver's main loop. (After locking it, you should always unlock it.)

## 事件（信号）

### BusLayoutChanged

```csharp
public event Action BusLayoutChanged
```

Emitted when an audio bus is added, deleted, or moved.

### BusRenamed

```csharp
public event AudioServerInstance.BusRenamedEventHandler BusRenamed
```

Emitted when the audio bus at `busIndex` is renamed from `oldName` to `newName`.

## BusRenamedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`AudioServerInstance.BusRenamed`](AudioServerInstance.md) event of a [`AudioServerInstance`](AudioServerInstance.md) class.

### 签名

#### Invoke

```csharp
delegate void BusRenamedEventHandler(long busIndex, StringName oldName, StringName newName)
```
