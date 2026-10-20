<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# MovieWriter

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

Godot can record videos with non-real-time simulation. Like the `--fixed-fps` [command line argument]($DOCS_URL/tutorials/editor/command_line_tutorial.html), this forces the reported `delta` in [`Node._Process()`](Node.md) functions to be identical across frames, regardless of how long it actually took to render the frame. This can be used to record high-quality videos with perfect frame pacing regardless of your hardware's capabilities.

Godot has 3 built-in `MovieWriter`s:

- OGV container with Theora for video and Vorbis for audio (`.ogv` file extension). Lossy compression, medium file sizes, fast encoding. The lossy compression quality can be adjusted by changing `ProjectSettings.editor/movie_writer/video_quality` and `ProjectSettings.editor/movie_writer/ogv/audio_quality`. The resulting file can be viewed in Godot with [`VideoStreamPlayer`](VideoStreamPlayer.md) and most video players, but not web browsers as they don't support Theora.

- AVI container with MJPEG for video and uncompressed audio (`.avi` file extension). Lossy compression, medium file sizes, fast encoding. The lossy compression quality can be adjusted by changing `ProjectSettings.editor/movie_writer/video_quality`. The resulting file can be viewed in most video players, but it must be converted to another format for viewing on the web or by Godot with [`VideoStreamPlayer`](VideoStreamPlayer.md). MJPEG does not support transparency. AVI output is currently limited to a file of 4 GB in size at most.

- PNG image sequence for video and WAV for audio (`.png` file extension). Lossless compression, large file sizes, slow encoding. Designed to be encoded to a video file with another tool such as [FFmpeg](https://ffmpeg.org/) after recording. Transparency is currently not supported, even if the root viewport is set to be transparent.

If you need to encode to a different format or pipe a stream through third-party software, you can extend the `MovieWriter` class to create your own movie writers. This should typically be done using GDExtension for performance reasons.

**Editor usage:** A default movie file path can be specified in `ProjectSettings.editor/movie_writer/movie_file`. Alternatively, for running single scenes, a `movie_file` metadata can be added to the root node, specifying the path to a movie file that will be used when recording that scene. Once a path is set, click the video reel icon in the top-right corner of the editor to enable Movie Maker mode, then run any scene as usual. The engine will start recording as soon as the splash screen is finished, and it will only stop recording when the engine quits. Click the video reel icon again to disable Movie Maker mode. Note that toggling Movie Maker mode does not affect project instances that are already running.

**Note:** MovieWriter is available for use in both the editor and exported projects, but it is *not* designed for use by end users to record videos while playing. Players wishing to record gameplay videos should install tools such as [OBS Studio](https://obsproject.com/) or [SimpleScreenRecorder](https://www.maartenbaert.be/simplescreenrecorder/) instead.

**Note:** MJPEG support (`.avi` file extension) depends on the `jpg` module being enabled at compile time (default behavior).

**Note:** OGV support (`.ogv` file extension) depends on the `theora` module being enabled at compile time (default behavior). Theora compression is only available in editor binaries.

## 构造函数

### MovieWriter

```csharp
public MovieWriter()
```

## 方法

### AddWriter

```csharp
public static void AddWriter(MovieWriter writer)
```

引擎名：`add_writer`

Adds a writer to be usable by the engine. The supported file extensions can be set by overriding `_HandlesFile()`.

**Note:** `AddWriter()` must be called early enough in the engine initialization to work, as movie writing is designed to start at the same time as the rest of the engine.

### _GetAudioMixRate

```csharp
public virtual uint _GetAudioMixRate()
```

引擎名：`_get_audio_mix_rate`

Called when the audio sample rate used for recording the audio is requested by the engine. The value returned must be specified in Hz. Defaults to 48000 Hz if `_GetAudioMixRate()` is not overridden.

### _GetAudioSpeakerMode

```csharp
public virtual AudioServer.SpeakerMode _GetAudioSpeakerMode()
```

引擎名：`_get_audio_speaker_mode`

Called when the audio speaker mode used for recording the audio is requested by the engine. This can affect the number of output channels in the resulting audio file/stream. Defaults to [`AudioServer.SpeakerMode.ModeStereo`](AudioServer.md) if `_GetAudioSpeakerMode()` is not overridden.

### _GetSupportedExtensions

```csharp
public virtual string[] _GetSupportedExtensions()
```

引擎名：`_get_supported_extensions`

Returns the list of supported filename extensions for movies written with this `MovieWriter`.

### _HandlesFile

```csharp
public virtual bool _HandlesFile(string path)
```

引擎名：`_handles_file`

Called when the engine determines whether this `MovieWriter` is able to handle the file at `path`. Must return `true` if this `MovieWriter` is able to handle the given file path, `false` otherwise. Typically, `_HandlesFile()` is overridden as follows to allow the user to record a file at any path with a given file extension:

```csharp
func _handles_file(path):
# Allows specifying an output file with a `.mkv` file extension (case-insensitive),
# either in the Project Settings or with the `--write-movie <path>` command line argument.
return path.get_extension().to_lower() == "mkv"

```

### _WriteBegin

```csharp
public virtual Error _WriteBegin(Vector2I movieSize, uint fps, string basePath)
```

引擎名：`_write_begin`

Called once before the engine starts writing video and audio data. `movieSize` is the width and height of the video to save. `fps` is the number of frames per second specified in the project settings or using the `--fixed-fps <fps>` [command line argument]($DOCS_URL/tutorials/editor/command_line_tutorial.html).

### _WriteEnd

```csharp
public virtual void _WriteEnd()
```

引擎名：`_write_end`

Called when the engine finishes writing. This occurs when the engine quits by pressing the window manager's close button, or when [`SceneTree.Quit`](SceneTree.md) is called.

**Note:** Pressing Ctrl + C on the terminal running the editor/project does *not* result in `_WriteEnd()` being called.
