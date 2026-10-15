<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioStreamWav

命名空间 `Godot`；class；继承 [`AudioStream`](AudioStream.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

AudioStreamWAV stores sound samples loaded from WAV files. To play the stored sound, use an [`AudioStreamPlayer`](AudioStreamPlayer.md) (for non-positional audio) or [`AudioStreamPlayer2D`](AudioStreamPlayer2D.md)/[`AudioStreamPlayer3D`](AudioStreamPlayer3D.md) (for positional audio). The sound can be looped.

This class can also be used to store dynamically-generated PCM audio data. See also [`AudioStreamGenerator`](AudioStreamGenerator.md) for procedural audio generation.

## 构造函数

### AudioStreamWav

```csharp
public AudioStreamWav()
```

## 属性

### Data

```csharp
public byte[] Data { get; set; }
```

Contains the audio data in bytes.

**Note:** If `Format` is set to [`AudioStreamWav.FormatEnum.Format8Bits`](AudioStreamWav.md), this property expects signed 8-bit PCM data. To convert from unsigned 8-bit PCM, subtract 128 from each byte.

**Note:** If `Format` is set to [`AudioStreamWav.FormatEnum.Qoa`](AudioStreamWav.md), this property expects data from a full QOA file.

### Format

```csharp
public AudioStreamWav.FormatEnum Format { get; set; }
```

Audio format.

### LoopBegin

```csharp
public int LoopBegin { get; set; }
```

The loop start point (in number of samples, relative to the beginning of the stream).

### LoopEnd

```csharp
public int LoopEnd { get; set; }
```

The loop end point (in number of samples, relative to the beginning of the stream).

### LoopMode

```csharp
public AudioStreamWav.LoopModeEnum LoopMode { get; set; }
```

The loop mode.

### MixRate

```csharp
public int MixRate { get; set; }
```

The sample rate for mixing this audio. Higher values require more storage space, but result in better quality.

In games, common sample rates in use are `11025`, `16000`, `22050`, `32000`, `44100`, and `48000`.

According to the [Nyquist-Shannon sampling theorem](https://en.wikipedia.org/wiki/Nyquist%E2%80%93Shannon_sampling_theorem), there is no quality difference to human hearing when going past 40,000 Hz (since most humans can only hear up to ~20,000 Hz, often less). If you are using lower-pitched sounds such as voices, lower sample rates such as `32000` or `22050` may be usable with no loss in quality.

### Stereo

```csharp
public bool Stereo { get; set; }
```

If `true`, audio is stereo.

### Tags

```csharp
public Dictionary Tags { get; set; }
```

Contains user-defined tags if found in the WAV data.

Commonly used tags include `title`, `artist`, `album`, `tracknumber`, and `date` (`date` does not have a standard date format).

**Note:** No tag is *guaranteed* to be present in every file, so make sure to account for the keys not always existing.

**Note:** Only WAV files using a `LIST` chunk with an identifier of `INFO` to encode the tags are currently supported.

## 方法

### GetData

```csharp
public byte[] GetData()
```

### GetFormat

```csharp
public AudioStreamWav.FormatEnum GetFormat()
```

### GetLoopBegin

```csharp
public int GetLoopBegin()
```

### GetLoopEnd

```csharp
public int GetLoopEnd()
```

### GetLoopMode

```csharp
public AudioStreamWav.LoopModeEnum GetLoopMode()
```

### GetMixRate

```csharp
public int GetMixRate()
```

### GetTags

```csharp
public Dictionary GetTags()
```

### IsStereo

```csharp
public bool IsStereo()
```

### LoadFromBuffer

```csharp
public static AudioStreamWav LoadFromBuffer(ReadOnlySpan<byte> streamData, Dictionary options)
```

Creates a new `AudioStreamWav` instance from the given buffer. The buffer must contain WAV data.

The keys and values of `options` match the properties of `ResourceImporterWav`. The usage of `options` is identical to `LoadFromFile()`.

### LoadFromBuffer

```csharp
public static AudioStreamWav LoadFromBuffer(byte[] streamData, Dictionary options = null)
```

Creates a new `AudioStreamWav` instance from the given buffer. The buffer must contain WAV data.

The keys and values of `options` match the properties of `ResourceImporterWav`. The usage of `options` is identical to `LoadFromFile()`.

### LoadFromFile

```csharp
public static AudioStreamWav LoadFromFile(string path, Dictionary options = null)
```

Creates a new `AudioStreamWav` instance from the given file path. The file must be in WAV format.

The keys and values of `options` match the properties of `ResourceImporterWav`.

**Example:** Load the first file dropped as a WAV and play it:

```csharp
@onready var audio_player = $AudioStreamPlayer

func _ready():
get_window().files_dropped.connect(_on_files_dropped)

func _on_files_dropped(files):
if files[0].get_extension() == "wav":
audio_player.stream = AudioStreamWAV.load_from_file(files[0], {
"force/max_rate": true,
"force/max_rate_hz": 11025
})
audio_player.play()

```

### SaveToWav

```csharp
public Error SaveToWav(string path)
```

Saves the AudioStreamWAV as a WAV file to `path`. Samples with IMA ADPCM or Quite OK Audio formats can't be saved.

**Note:** A `.wav` extension is automatically appended to `path` if it is missing.

### SetData

```csharp
public void SetData(ReadOnlySpan<byte> data)
```

### SetData

```csharp
public void SetData(byte[] data)
```

### SetFormat

```csharp
public void SetFormat(AudioStreamWav.FormatEnum format)
```

### SetLoopBegin

```csharp
public void SetLoopBegin(int loopBegin)
```

### SetLoopEnd

```csharp
public void SetLoopEnd(int loopEnd)
```

### SetLoopMode

```csharp
public void SetLoopMode(AudioStreamWav.LoopModeEnum loopMode)
```

### SetMixRate

```csharp
public void SetMixRate(int mixRate)
```

### SetStereo

```csharp
public void SetStereo(bool stereo)
```

### SetTags

```csharp
public void SetTags(Dictionary tags)
```

## FormatEnum

命名空间 `Godot`；enum

### 枚举值

- `Format8Bits = 0`：8-bit PCM audio codec.
- `Format16Bits = 1`：16-bit PCM audio codec.
- `ImaAdpcm = 2`：Audio is lossily compressed as IMA ADPCM.
- `Qoa = 3`：Audio is lossily compressed as [Quite OK Audio](https://qoaformat.org/).

## LoopModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Audio does not loop.
- `Forward = 1`：Audio loops the data between [`AudioStreamWav.LoopBegin`](AudioStreamWav.md) and [`AudioStreamWav.LoopEnd`](AudioStreamWav.md), playing forward only.
- `Pingpong = 2`：Audio loops the data between [`AudioStreamWav.LoopBegin`](AudioStreamWav.md) and [`AudioStreamWav.LoopEnd`](AudioStreamWav.md), playing back and forth.
- `Backward = 3`：Audio loops the data between [`AudioStreamWav.LoopBegin`](AudioStreamWav.md) and [`AudioStreamWav.LoopEnd`](AudioStreamWav.md), playing backward only.
