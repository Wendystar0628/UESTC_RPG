<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VideoStream

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Base resource type for all video streams. Classes that derive from `VideoStream` can all be used as resource types to play back videos in [`VideoStreamPlayer`](VideoStreamPlayer.md).

## 构造函数

### VideoStream

```csharp
public VideoStream()
```

## 属性

### File

```csharp
public string File { get; set; }
```

引擎名：`file`

The video file path or URI that this `VideoStream` resource handles.

For [`VideoStreamTheora`](VideoStreamTheora.md), this filename should be an Ogg Theora video file with the `.ogv` extension.

## 方法

### GetFile

```csharp
public string GetFile()
```

引擎名：`get_file`

### SetFile

```csharp
public void SetFile(string file)
```

引擎名：`set_file`

### _InstantiatePlayback

```csharp
public virtual VideoStreamPlayback _InstantiatePlayback()
```

引擎名：`_instantiate_playback`

Called when the video starts playing, to initialize and return a subclass of [`VideoStreamPlayback`](VideoStreamPlayback.md).
