<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AnimationNodeAnimation

命名空间 `Godot`；class；继承 [`AnimationRootNode`](AnimationRootNode.md) → [`AnimationNode`](AnimationNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A resource to add to an [`AnimationNodeBlendTree`](AnimationNodeBlendTree.md). Only has one output port using the `Animation` property. Used as an input for [`AnimationNode`](AnimationNode.md)s that blend animations together.

## 构造函数

### AnimationNodeAnimation

```csharp
public AnimationNodeAnimation()
```

## 属性

### AdvanceOnStart

```csharp
public bool AdvanceOnStart { get; set; }
```

引擎名：`advance_on_start`

If `true`, on receiving a request to play an animation from the start, the first frame is not drawn, but only processed, and playback starts from the next frame.

See also the notes of [`AnimationPlayer.Play`](AnimationPlayer.md).

### Animation

```csharp
public StringName Animation { get; set; }
```

引擎名：`animation`

Animation to use as an output. It is one of the animations provided by [`AnimationTree.AnimPlayer`](AnimationTree.md).

### LoopMode

```csharp
public Animation.LoopModeEnum LoopMode { get; set; }
```

引擎名：`loop_mode`

If `UseCustomTimeline` is `true`, override the loop settings of the original [`Animation`](Animation.md) resource with the value.

**Note:** If the [`Animation.LoopMode`](Animation.md) isn't set to looping, the [`Animation.TrackSetInterpolationLoopWrap`](Animation.md) option will not be respected. If you cannot get the expected behavior, consider duplicating the [`Animation`](Animation.md) resource and changing the loop settings.

### PlayMode

```csharp
public AnimationNodeAnimation.PlayModeEnum PlayMode { get; set; }
```

引擎名：`play_mode`

Determines the playback direction of the animation.

### StartOffset

```csharp
public double StartOffset { get; set; }
```

引擎名：`start_offset`

If `UseCustomTimeline` is `true`, offset the start position of the animation.

This is useful for adjusting which foot steps first in 3D walking animations.

### StretchTimeScale

```csharp
public bool StretchTimeScale { get; set; }
```

引擎名：`stretch_time_scale`

If `true`, scales the time so that the length specified in `TimelineLength` is one cycle.

This is useful for matching the periods of walking and running animations.

If `false`, the original animation length is respected. If you set the loop to `LoopMode`, the animation will loop in `TimelineLength`.

### TimelineLength

```csharp
public double TimelineLength { get; set; }
```

引擎名：`timeline_length`

The length of the custom timeline.

If `StretchTimeScale` is `true`, scales the animation to this length.

### UseCustomTimeline

```csharp
public bool UseCustomTimeline { get; set; }
```

引擎名：`use_custom_timeline`

If `true`, [`AnimationNode`](AnimationNode.md) provides an animation based on the [`Animation`](Animation.md) resource with some parameters adjusted.

## 方法

### GetAnimation

```csharp
public StringName GetAnimation()
```

引擎名：`get_animation`

### GetLoopMode

```csharp
public Animation.LoopModeEnum GetLoopMode()
```

引擎名：`get_loop_mode`

### GetPlayMode

```csharp
public AnimationNodeAnimation.PlayModeEnum GetPlayMode()
```

引擎名：`get_play_mode`

### GetStartOffset

```csharp
public double GetStartOffset()
```

引擎名：`get_start_offset`

### GetTimelineLength

```csharp
public double GetTimelineLength()
```

引擎名：`get_timeline_length`

### IsAdvanceOnStart

```csharp
public bool IsAdvanceOnStart()
```

引擎名：`is_advance_on_start`

### IsStretchingTimeScale

```csharp
public bool IsStretchingTimeScale()
```

引擎名：`is_stretching_time_scale`

### IsUsingCustomTimeline

```csharp
public bool IsUsingCustomTimeline()
```

引擎名：`is_using_custom_timeline`

### SetAdvanceOnStart

```csharp
public void SetAdvanceOnStart(bool advanceOnStart)
```

引擎名：`set_advance_on_start`

### SetAnimation

```csharp
public void SetAnimation(StringName name)
```

引擎名：`set_animation`

### SetLoopMode

```csharp
public void SetLoopMode(Animation.LoopModeEnum loopMode)
```

引擎名：`set_loop_mode`

### SetPlayMode

```csharp
public void SetPlayMode(AnimationNodeAnimation.PlayModeEnum mode)
```

引擎名：`set_play_mode`

### SetStartOffset

```csharp
public void SetStartOffset(double startOffset)
```

引擎名：`set_start_offset`

### SetStretchTimeScale

```csharp
public void SetStretchTimeScale(bool stretchTimeScale)
```

引擎名：`set_stretch_time_scale`

### SetTimelineLength

```csharp
public void SetTimelineLength(double timelineLength)
```

引擎名：`set_timeline_length`

### SetUseCustomTimeline

```csharp
public void SetUseCustomTimeline(bool useCustomTimeline)
```

引擎名：`set_use_custom_timeline`

## PlayModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Forward = 0`：Plays animation in forward direction.
- `Backward = 1`：Plays animation in backward direction.
