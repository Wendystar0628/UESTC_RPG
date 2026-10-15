<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AnimatedTexture

命名空间 `Godot`；class；继承 [`Texture2D`](Texture2D.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`AnimatedTexture` is a resource format for frame-based animations, where multiple textures can be chained automatically with a predefined delay for each frame. Unlike [`AnimationPlayer`](AnimationPlayer.md) or [`AnimatedSprite2D`](AnimatedSprite2D.md), it isn't a [`Node`](Node.md), but has the advantage of being usable anywhere a [`Texture2D`](Texture2D.md) resource can be used, e.g. in a [`TileSet`](TileSet.md).

The playback of the animation is controlled by the `SpeedScale` property, as well as each frame's duration (see `SetFrameDuration()`). The animation loops, i.e. it will restart at frame 0 automatically after playing the last frame.

`AnimatedTexture` currently requires all frame textures to have the same size, otherwise the bigger ones will be cropped to match the smallest one.

**Note:** AnimatedTexture doesn't support using [`AtlasTexture`](AtlasTexture.md)s. Each frame needs to be a separate [`Texture2D`](Texture2D.md).

**Warning:** The current implementation is not efficient for the modern renderers.

## 构造函数

### AnimatedTexture

```csharp
public AnimatedTexture()
```

## 属性

### CurrentFrame

```csharp
public int CurrentFrame { get; set; }
```

引擎名：`current_frame`

Sets the currently visible frame of the texture. Setting this frame while playing resets the current frame time, so the newly selected frame plays for its whole configured frame duration.

### Frames

```csharp
public int Frames { get; set; }
```

引擎名：`frames`

Number of frames to use in the animation. While you can create the frames independently with `SetFrameTexture()`, you need to set this value for the animation to take new frames into account. The maximum number of frames is `MaxFrames`.

### OneShot

```csharp
public bool OneShot { get; set; }
```

引擎名：`one_shot`

If `true`, the animation will only play once and will not loop back to the first frame after reaching the end. Note that reaching the end will not set `Pause` to `true`.

### Pause

```csharp
public bool Pause { get; set; }
```

引擎名：`pause`

If `true`, the animation will pause where it currently is (i.e. at `CurrentFrame`). The animation will continue from where it was paused when changing this property to `false`.

### SpeedScale

```csharp
public float SpeedScale { get; set; }
```

引擎名：`speed_scale`

The animation speed is multiplied by this value. If set to a negative value, the animation is played in reverse.

## 方法

### GetCurrentFrame

```csharp
public int GetCurrentFrame()
```

引擎名：`get_current_frame`

### GetFrameDuration

```csharp
public float GetFrameDuration(int frame)
```

引擎名：`get_frame_duration`

Returns the given `frame`'s duration, in seconds.

### GetFrameTexture

```csharp
public Texture2D GetFrameTexture(int frame)
```

引擎名：`get_frame_texture`

Returns the given frame's [`Texture2D`](Texture2D.md).

### GetFrames

```csharp
public int GetFrames()
```

引擎名：`get_frames`

### GetOneShot

```csharp
public bool GetOneShot()
```

引擎名：`get_one_shot`

### GetPause

```csharp
public bool GetPause()
```

引擎名：`get_pause`

### GetSpeedScale

```csharp
public float GetSpeedScale()
```

引擎名：`get_speed_scale`

### SetCurrentFrame

```csharp
public void SetCurrentFrame(int frame)
```

引擎名：`set_current_frame`

### SetFrameDuration

```csharp
public void SetFrameDuration(int frame, float duration)
```

引擎名：`set_frame_duration`

Sets the duration of any given `frame`. The final duration is affected by the `SpeedScale`. If set to `0`, the frame is skipped during playback.

### SetFrameTexture

```csharp
public void SetFrameTexture(int frame, Texture2D texture)
```

引擎名：`set_frame_texture`

Assigns a [`Texture2D`](Texture2D.md) to the given frame. Frame IDs start at 0, so the first frame has ID 0, and the last frame of the animation has ID `Frames` - 1.

You can define any number of textures up to `MaxFrames`, but keep in mind that only frames from 0 to `Frames` - 1 will be part of the animation.

### SetFrames

```csharp
public void SetFrames(int frames)
```

引擎名：`set_frames`

### SetOneShot

```csharp
public void SetOneShot(bool oneShot)
```

引擎名：`set_one_shot`

### SetPause

```csharp
public void SetPause(bool pause)
```

引擎名：`set_pause`

### SetSpeedScale

```csharp
public void SetSpeedScale(float scale)
```

引擎名：`set_speed_scale`

## 常量

### MaxFrames

```csharp
public const long MaxFrames = 256
```

The maximum number of frames supported by `AnimatedTexture`. If you need more frames in your animation, use [`AnimationPlayer`](AnimationPlayer.md) or [`AnimatedSprite2D`](AnimatedSprite2D.md).
