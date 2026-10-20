<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ResourceImporterMP3

命名空间 `Godot`；class；继承 [`ResourceImporter`](ResourceImporter.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

MP3 is a lossy audio format, with worse audio quality compared to [`ResourceImporterOggVorbis`](ResourceImporterOggVorbis.md) at a given bitrate.

In most cases, it's recommended to use Ogg Vorbis over MP3. However, if you're using an MP3 sound source with no higher quality source available, then it's recommended to use the MP3 file directly to avoid double lossy compression.

MP3 requires more CPU to decode than [`ResourceImporterWav`](ResourceImporterWav.md). If you need to play a lot of simultaneous sounds, it's recommended to use WAV for those sounds instead, especially if targeting low-end devices.

## 构造函数

### ResourceImporterMP3

```csharp
public ResourceImporterMP3()
```
