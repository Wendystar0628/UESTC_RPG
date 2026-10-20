<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SpriteFrames

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Sprite frame library for an [`AnimatedSprite2D`](AnimatedSprite2D.md) or [`AnimatedSprite3D`](AnimatedSprite3D.md) node. Contains frames and animation data for playback.

## 构造函数

### SpriteFrames

```csharp
public SpriteFrames()
```

## 属性

### Animations

```csharp
public Array Animations { get; set; }
```

引擎名：`animations`

## 方法

### AddAnimation

```csharp
public void AddAnimation(StringName anim)
```

引擎名：`add_animation`

Adds a new `anim` animation to the library.

### AddFrame

```csharp
public void AddFrame(StringName anim, Texture2D texture, float duration = 1f, int atPosition = -1)
```

引擎名：`add_frame`

Adds a frame to the `anim` animation. If `atPosition` is `-1`, the frame will be added to the end of the animation. `duration` specifies the relative duration, see `GetFrameDuration()` for details.

### Clear

```csharp
public void Clear(StringName anim)
```

引擎名：`clear`

Removes all frames from the `anim` animation.

### ClearAll

```csharp
public void ClearAll()
```

引擎名：`clear_all`

Removes all animations. An empty `default` animation will be created.

### DuplicateAnimation

```csharp
public void DuplicateAnimation(StringName animFrom, StringName animTo)
```

引擎名：`duplicate_animation`

Duplicates the animation `animFrom` to a new animation named `animTo`. Fails if `animTo` already exists, or if `animFrom` does not exist.

### GetAnimationLoop

```csharp
public bool GetAnimationLoop(StringName anim)
```

引擎名：`get_animation_loop`

Returns `true` if `get_animation_loop_mode(anim) == LOOP_LINEAR`. Otherwise, returns `false`.

### GetAnimationLoopMode

```csharp
public SpriteFrames.LoopMode GetAnimationLoopMode(StringName anim)
```

引擎名：`get_animation_loop_mode`

Returns the loop mode for the `anim` animation.

### GetAnimationNames

```csharp
public string[] GetAnimationNames()
```

引擎名：`get_animation_names`

Returns an array containing the names associated to each animation. Values are placed in alphabetical order.

### GetAnimationSpeed

```csharp
public double GetAnimationSpeed(StringName anim)
```

引擎名：`get_animation_speed`

Returns the speed in frames per second for the `anim` animation.

### GetFrameCount

```csharp
public int GetFrameCount(StringName anim)
```

引擎名：`get_frame_count`

Returns the number of frames for the `anim` animation.

### GetFrameDuration

```csharp
public float GetFrameDuration(StringName anim, int idx)
```

引擎名：`get_frame_duration`

Returns a relative duration of the frame `idx` in the `anim` animation (defaults to `1.0`). For example, a frame with a duration of `2.0` is displayed twice as long as a frame with a duration of `1.0`. You can calculate the absolute duration (in seconds) of a frame using the following formula:

```csharp
absolute_duration = relative_duration / (animation_fps * abs(playing_speed))

```

In this example, `playing_speed` refers to either [`AnimatedSprite2D.GetPlayingSpeed`](AnimatedSprite2D.md) or [`AnimatedSprite3D.GetPlayingSpeed`](AnimatedSprite3D.md).

### GetFrameTexture

```csharp
public Texture2D GetFrameTexture(StringName anim, int idx)
```

引擎名：`get_frame_texture`

Returns the texture of the frame `idx` in the `anim` animation.

### HasAnimation

```csharp
public bool HasAnimation(StringName anim)
```

引擎名：`has_animation`

Returns `true` if the `anim` animation exists.

### RemoveAnimation

```csharp
public void RemoveAnimation(StringName anim)
```

引擎名：`remove_animation`

Removes the `anim` animation.

### RemoveFrame

```csharp
public void RemoveFrame(StringName anim, int idx)
```

引擎名：`remove_frame`

Removes the `anim` animation's frame `idx`.

### RenameAnimation

```csharp
public void RenameAnimation(StringName anim, StringName newname)
```

引擎名：`rename_animation`

Changes the `anim` animation's name to `newname`.

### SetAnimationLoop

```csharp
public void SetAnimationLoop(StringName anim, bool loop)
```

引擎名：`set_animation_loop`

If `loop` is `false` equivalent to `set_animation_loop_mode(LOOP_NONE)`.

If `loop` is `true` equivalent to `set_animation_loop_mode(LOOP_LINEAR)`.

### SetAnimationLoopMode

```csharp
public void SetAnimationLoopMode(StringName anim, SpriteFrames.LoopMode loopMode)
```

引擎名：`set_animation_loop_mode`

Sets the `loopMode` for the `anim` animation.

### SetAnimationSpeed

```csharp
public void SetAnimationSpeed(StringName anim, double fps)
```

引擎名：`set_animation_speed`

Sets the speed for the `anim` animation in frames per second.

### SetFrame

```csharp
public void SetFrame(StringName anim, int idx, Texture2D texture, float duration = 1f)
```

引擎名：`set_frame`

Sets the `texture` and the `duration` of the frame `idx` in the `anim` animation. `duration` specifies the relative duration, see `GetFrameDuration()` for details.

## LoopMode

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：The animation plays once and stops when it reaches the end, or the start if played in reverse.
- `Linear = 1`：The animation restarts from the beginning when it reaches the end, or from the end if played in reverse, repeating continuously.
- `Pingpong = 2`：The animation alternates direction each time it reaches the end or start, playing forward and then in reverse repeatedly.
