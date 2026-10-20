<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioStreamPlaybackInteractive

命名空间 `Godot`；class；继承 [`AudioStreamPlayback`](AudioStreamPlayback.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Playback component of [`AudioStreamInteractive`](AudioStreamInteractive.md). Contains functions to change the currently played clip.

## 方法

### GetCurrentClipIndex

```csharp
public int GetCurrentClipIndex()
```

引擎名：`get_current_clip_index`

Return the index of the currently playing clip. You can use this to get the name of the currently playing clip with [`AudioStreamInteractive.GetClipName`](AudioStreamInteractive.md).

**Example:** Get the currently playing clip name from inside an [`AudioStreamPlayer`](AudioStreamPlayer.md) node.

### SwitchToClip

```csharp
public void SwitchToClip(int clipIndex)
```

引擎名：`switch_to_clip`

Switch to a clip (by index).

### SwitchToClipByName

```csharp
public void SwitchToClipByName(StringName clipName)
```

引擎名：`switch_to_clip_by_name`

Switch to a clip (by name).
