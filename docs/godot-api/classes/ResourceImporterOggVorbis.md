<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ResourceImporterOggVorbis

命名空间 `Godot`；class；继承 [`ResourceImporter`](ResourceImporter.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

Ogg Vorbis is a lossy audio format, with better audio quality compared to [`ResourceImporterMP3`](ResourceImporterMP3.md) at a given bitrate.

In most cases, it's recommended to use Ogg Vorbis over MP3. However, if you're using an MP3 sound source with no higher quality source available, then it's recommended to use the MP3 file directly to avoid double lossy compression.

Ogg Vorbis requires more CPU to decode than [`ResourceImporterWav`](ResourceImporterWav.md). If you need to play a lot of simultaneous sounds, it's recommended to use WAV for those sounds instead, especially if targeting low-end devices.

## 构造函数

### ResourceImporterOggVorbis

```csharp
public ResourceImporterOggVorbis()
```

## 方法

### LoadFromBuffer

```csharp
public static AudioStreamOggVorbis LoadFromBuffer(ReadOnlySpan<byte> streamData)
```

引擎名：`load_from_buffer`

Creates a new [`AudioStreamOggVorbis`](AudioStreamOggVorbis.md) instance from the given buffer. The buffer must contain Ogg Vorbis data.

### LoadFromBuffer

```csharp
public static AudioStreamOggVorbis LoadFromBuffer(byte[] streamData)
```

引擎名：`load_from_buffer`

Creates a new [`AudioStreamOggVorbis`](AudioStreamOggVorbis.md) instance from the given buffer. The buffer must contain Ogg Vorbis data.

### LoadFromFile

```csharp
public static AudioStreamOggVorbis LoadFromFile(string path)
```

引擎名：`load_from_file`

Creates a new [`AudioStreamOggVorbis`](AudioStreamOggVorbis.md) instance from the given file path. The file must be in Ogg Vorbis format.
