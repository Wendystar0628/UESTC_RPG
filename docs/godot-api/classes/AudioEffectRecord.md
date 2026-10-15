<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioEffectRecord

命名空间 `Godot`；class；继承 [`AudioEffect`](AudioEffect.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Allows the user to record the sound from an audio bus into an [`AudioStreamWav`](AudioStreamWav.md). When used on the Master audio bus, this includes all audio output by Godot.

Unlike [`AudioEffectCapture`](AudioEffectCapture.md), this effect encodes the recording with the given format (8-bit, 16-bit, or compressed) instead of giving access to the raw audio samples.

Can be used (with an [`AudioStreamMicrophone`](AudioStreamMicrophone.md)) to record from a microphone.

**Note:** `ProjectSettings.audio/driver/enable_input` must be `true` for audio input to work. See also that setting's description for caveats related to permissions and operating system privacy settings.

## 构造函数

### AudioEffectRecord

```csharp
public AudioEffectRecord()
```

## 属性

### Format

```csharp
public AudioStreamWav.FormatEnum Format { get; set; }
```

引擎名：`format`

Specifies the format in which the sample will be recorded.

## 方法

### GetFormat

```csharp
public AudioStreamWav.FormatEnum GetFormat()
```

引擎名：`get_format`

### GetRecording

```csharp
public AudioStreamWav GetRecording()
```

引擎名：`get_recording`

Returns the recorded sample.

### IsRecordingActive

```csharp
public bool IsRecordingActive()
```

引擎名：`is_recording_active`

Returns whether the recording is active or not.

### SetFormat

```csharp
public void SetFormat(AudioStreamWav.FormatEnum format)
```

引擎名：`set_format`

### SetRecordingActive

```csharp
public void SetRecordingActive(bool record)
```

引擎名：`set_recording_active`

If `true`, the sound will be recorded. Note that restarting the recording will remove the previously recorded sample.
