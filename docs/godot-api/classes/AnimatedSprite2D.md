<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AnimatedSprite2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`AnimatedSprite2D` is similar to the [`Sprite2D`](Sprite2D.md) node, except it carries multiple textures as animation frames. Animations are created using a [`SpriteFrames`](SpriteFrames.md) resource, which allows you to import image files (or a folder containing said files) to provide the animation frames for the sprite. The [`SpriteFrames`](SpriteFrames.md) resource can be configured in the editor via the SpriteFrames bottom panel.

## 构造函数

### AnimatedSprite2D

```csharp
public AnimatedSprite2D()
```

## 属性

### Animation

```csharp
public StringName Animation { get; set; }
```

引擎名：`animation`

The current animation from the `SpriteFrames` resource. If this value is changed, the `Frame` counter and the `FrameProgress` are reset.

### Autoplay

```csharp
public string Autoplay { get; set; }
```

引擎名：`autoplay`

The key of the animation to play when the scene loads.

### Centered

```csharp
public bool Centered { get; set; }
```

引擎名：`centered`

If `true`, texture will be centered.

**Note:** For games with a pixel art aesthetic, textures may appear deformed when centered. This is caused by their position being between pixels. To prevent this, set this property to `false`, or consider enabling `ProjectSettings.rendering/2d/snap/snap_2d_vertices_to_pixel` and `ProjectSettings.rendering/2d/snap/snap_2d_transforms_to_pixel`.

### FlipH

```csharp
public bool FlipH { get; set; }
```

引擎名：`flip_h`

If `true`, texture is flipped horizontally.

### FlipV

```csharp
public bool FlipV { get; set; }
```

引擎名：`flip_v`

If `true`, texture is flipped vertically.

### Frame

```csharp
public int Frame { get; set; }
```

引擎名：`frame`

The displayed animation frame's index. Setting this property also resets `FrameProgress`. If this is not desired, use `SetFrameAndProgress()`.

### FrameProgress

```csharp
public float FrameProgress { get; set; }
```

引擎名：`frame_progress`

The progress value between `0.0` and `1.0` until the current frame transitions to the next frame. If the animation is playing backwards, the value transitions from `1.0` to `0.0`.

### Offset

```csharp
public Vector2 Offset { get; set; }
```

引擎名：`offset`

The texture's drawing offset.

### SpeedScale

```csharp
public float SpeedScale { get; set; }
```

引擎名：`speed_scale`

The speed scaling ratio. For example, if this value is `1`, then the animation plays at normal speed. If it's `0.5`, then it plays at half speed. If it's `2`, then it plays at double speed.

If set to a negative value, the animation is played in reverse. If set to `0`, the animation will not advance.

### SpriteFrames

```csharp
public SpriteFrames SpriteFrames { get; set; }
```

引擎名：`sprite_frames`

The [`SpriteFrames`](SpriteFrames.md) resource containing the animation(s). Allows you the option to load, edit, clear, make unique and save the states of the [`SpriteFrames`](SpriteFrames.md) resource.

## 方法

### GetAnimation

```csharp
public StringName GetAnimation()
```

引擎名：`get_animation`

### GetAutoplay

```csharp
public string GetAutoplay()
```

引擎名：`get_autoplay`

### GetFrame

```csharp
public int GetFrame()
```

引擎名：`get_frame`

### GetFrameProgress

```csharp
public float GetFrameProgress()
```

引擎名：`get_frame_progress`

### GetOffset

```csharp
public Vector2 GetOffset()
```

引擎名：`get_offset`

### GetPlayingSpeed

```csharp
public float GetPlayingSpeed()
```

引擎名：`get_playing_speed`

Returns the actual playing speed of current animation or `0` if not playing. This speed is the `SpeedScale` property multiplied by `custom_speed` argument specified when calling the `Play()` method.

Returns a negative value if the current animation is playing backwards.

### GetSpeedScale

```csharp
public float GetSpeedScale()
```

引擎名：`get_speed_scale`

### GetSpriteFrames

```csharp
public SpriteFrames GetSpriteFrames()
```

引擎名：`get_sprite_frames`

### IsCentered

```csharp
public bool IsCentered()
```

引擎名：`is_centered`

### IsFlippedH

```csharp
public bool IsFlippedH()
```

引擎名：`is_flipped_h`

### IsFlippedV

```csharp
public bool IsFlippedV()
```

引擎名：`is_flipped_v`

### IsPlaying

```csharp
public bool IsPlaying()
```

引擎名：`is_playing`

Returns `true` if an animation is currently playing (even if `SpeedScale` and/or `custom_speed` are `0`).

### Pause

```csharp
public void Pause()
```

引擎名：`pause`

Pauses the currently playing animation. The `Frame` and `FrameProgress` will be kept and calling `Play()` or `PlayBackwards()` without arguments will resume the animation from the current playback position.

See also `Stop()`.

### Play

```csharp
public void Play(StringName name = null, float customSpeed = 1f, bool fromEnd = false)
```

引擎名：`play`

Plays the animation with key `name`. If `customSpeed` is negative and `fromEnd` is `true`, the animation will play backwards (which is equivalent to calling `PlayBackwards()`).

If this method is called with that same animation `name`, or with no `name` parameter, the assigned animation will resume playing if it was paused.

### PlayBackwards

```csharp
public void PlayBackwards(StringName name = null)
```

引擎名：`play_backwards`

Plays the animation with key `name` in reverse.

This method is a shorthand for `Play()` with `custom_speed = -1.0` and `from_end = true`, so see its description for more information.

### SetAnimation

```csharp
public void SetAnimation(StringName name)
```

引擎名：`set_animation`

### SetAutoplay

```csharp
public void SetAutoplay(string name)
```

引擎名：`set_autoplay`

### SetCentered

```csharp
public void SetCentered(bool centered)
```

引擎名：`set_centered`

### SetFlipH

```csharp
public void SetFlipH(bool flipH)
```

引擎名：`set_flip_h`

### SetFlipV

```csharp
public void SetFlipV(bool flipV)
```

引擎名：`set_flip_v`

### SetFrame

```csharp
public void SetFrame(int frame)
```

引擎名：`set_frame`

### SetFrameAndProgress

```csharp
public void SetFrameAndProgress(int frame, float progress)
```

引擎名：`set_frame_and_progress`

Sets `Frame` and `FrameProgress` to the given values. Unlike setting `Frame`, this method does not reset the `FrameProgress` to `0.0` implicitly.

**Example:** Change the animation while keeping the same `Frame` and `FrameProgress`:

### SetFrameProgress

```csharp
public void SetFrameProgress(float progress)
```

引擎名：`set_frame_progress`

### SetOffset

```csharp
public void SetOffset(Vector2 offset)
```

引擎名：`set_offset`

### SetSpeedScale

```csharp
public void SetSpeedScale(float speedScale)
```

引擎名：`set_speed_scale`

### SetSpriteFrames

```csharp
public void SetSpriteFrames(SpriteFrames spriteFrames)
```

引擎名：`set_sprite_frames`

### Stop

```csharp
public void Stop()
```

引擎名：`stop`

Stops the currently playing animation. The animation position is reset to `0` and the `custom_speed` is reset to `1.0`. See also `Pause()`.

## 事件（信号）

### AnimationChanged

```csharp
public event Action AnimationChanged
```

引擎名：`animation_changed`

Emitted when `Animation` changes.

### AnimationFinished

```csharp
public event Action AnimationFinished
```

引擎名：`animation_finished`

Emitted when the animation reaches the end, or the start if it is played in reverse. When the animation finishes, it pauses the playback.

**Note:** This signal is not emitted if an animation is looping.

### AnimationLooped

```csharp
public event Action AnimationLooped
```

引擎名：`animation_looped`

Emitted when the animation loops.

### FrameChanged

```csharp
public event Action FrameChanged
```

引擎名：`frame_changed`

Emitted when `Frame` changes.

### SpriteFramesChanged

```csharp
public event Action SpriteFramesChanged
```

引擎名：`sprite_frames_changed`

Emitted when `SpriteFrames` changes.
