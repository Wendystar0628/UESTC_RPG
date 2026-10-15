<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AnimationPlayer

命名空间 `Godot`；class；继承 [`AnimationMixer`](AnimationMixer.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

An animation player is used for general-purpose playback of animations. It contains a dictionary of [`AnimationLibrary`](AnimationLibrary.md) resources and custom blend times between animation transitions.

Some methods and properties use a single key to reference an animation directly. These keys are formatted as the key for the library, followed by a forward slash, then the key for the animation within the library, for example `"movement/run"`. If the library's key is an empty string (known as the default library), the forward slash is omitted, being the same key used by the library.

`AnimationPlayer` is better-suited than [`Tween`](Tween.md) for more complex animations, for example ones with non-trivial timings. It can also be used over [`Tween`](Tween.md) if the animation track editor is more convenient than doing it in code.

Updating the target properties of animations occurs at the process frame.

## 构造函数

### AnimationPlayer

```csharp
public AnimationPlayer()
```

## 属性

### AssignedAnimation

```csharp
public StringName AssignedAnimation { get; set; }
```

引擎名：`assigned_animation`

If playing, the current animation's key, otherwise, the animation last played. When set, this changes the animation, but will not play it unless already playing. See also `CurrentAnimation`.

### Autoplay

```csharp
public StringName Autoplay { get; set; }
```

引擎名：`autoplay`

The key of the animation to play when the scene loads.

### CurrentAnimation

```csharp
public StringName CurrentAnimation { get; set; }
```

引擎名：`current_animation`

The key of the currently playing animation. If no animation is playing, the property's value is an empty string. Changing this value does not restart the animation. See `Play()` for more information on playing animations.

**Note:** While this property appears in the Inspector, it's not meant to be edited, and it's not saved in the scene. This property is mainly used to get the currently playing animation, and internally for animation playback tracks. For more information, see [`Animation`](Animation.md).

### CurrentAnimationLength

```csharp
public double CurrentAnimationLength { get; }
```

引擎名：`current_animation_length`

The length (in seconds) of the currently playing animation.

### CurrentAnimationPosition

```csharp
public double CurrentAnimationPosition { get; }
```

引擎名：`current_animation_position`

The position (in seconds) of the currently playing animation.

### MethodCallMode

```csharp
public AnimationPlayer.AnimationMethodCallMode MethodCallMode { get; set; }
```

The call mode used for "Call Method" tracks.

### MovieQuitOnFinish

```csharp
public bool MovieQuitOnFinish { get; set; }
```

引擎名：`movie_quit_on_finish`

If `true` and the engine is running in Movie Maker mode (see [`MovieWriter`](MovieWriter.md)), exits the engine with [`SceneTree.Quit`](SceneTree.md) as soon as an animation is done playing in this `AnimationPlayer`. A message is printed when the engine quits for this reason.

**Note:** This obeys the same logic as the [`AnimationMixer.AnimationFinished`](AnimationMixer.md) signal, so it will not quit the engine if the animation is set to be looping.

### PlaybackActive

```csharp
public bool PlaybackActive { get; set; }
```

If `true`, the [`AnimationMixer`](AnimationMixer.md) will be processing.

### PlaybackAutoCapture

```csharp
public bool PlaybackAutoCapture { get; set; }
```

引擎名：`playback_auto_capture`

If `true`, performs [`AnimationMixer.Capture`](AnimationMixer.md) before playback automatically. This means just `PlayWithCapture()` is executed with default arguments instead of `Play()`.

**Note:** Capture interpolation is only performed if the animation contains a capture track. See also [`Animation.UpdateMode.Capture`](Animation.md).

### PlaybackAutoCaptureDuration

```csharp
public double PlaybackAutoCaptureDuration { get; set; }
```

引擎名：`playback_auto_capture_duration`

See also `PlayWithCapture()` and [`AnimationMixer.Capture`](AnimationMixer.md).

If `PlaybackAutoCaptureDuration` is negative value, the duration is set to the interval between the current position and the first key.

### PlaybackAutoCaptureEaseType

```csharp
public Tween.EaseType PlaybackAutoCaptureEaseType { get; set; }
```

引擎名：`playback_auto_capture_ease_type`

The ease type of the capture interpolation. See also [`Tween.EaseType`](Tween.md).

### PlaybackAutoCaptureTransitionType

```csharp
public Tween.TransitionType PlaybackAutoCaptureTransitionType { get; set; }
```

引擎名：`playback_auto_capture_transition_type`

The transition type of the capture interpolation. See also [`Tween.TransitionType`](Tween.md).

### PlaybackDefaultBlendTime

```csharp
public double PlaybackDefaultBlendTime { get; set; }
```

引擎名：`playback_default_blend_time`

The default time in which to blend animations. Ranges from 0 to 4096 with 0.01 precision.

### PlaybackProcessMode

```csharp
public AnimationPlayer.AnimationProcessCallback PlaybackProcessMode { get; set; }
```

The process notification in which to update animations.

### SpeedScale

```csharp
public float SpeedScale { get; set; }
```

引擎名：`speed_scale`

The speed scaling ratio. For example, if this value is `1`, then the animation plays at normal speed. If it's `0.5`, then it plays at half speed. If it's `2`, then it plays at double speed.

If set to a negative value, the animation is played in reverse. If set to `0`, the animation will not advance.

## 方法

### AnimationGetNext

```csharp
public StringName AnimationGetNext(StringName animationFrom)
```

引擎名：`animation_get_next`

Returns the key of the animation which is queued to play after the `animationFrom` animation.

### AnimationSetNext

```csharp
public void AnimationSetNext(StringName animationFrom, StringName animationTo)
```

引擎名：`animation_set_next`

Triggers the `animationTo` animation when the `animationFrom` animation completes.

### ClearQueue

```csharp
public void ClearQueue()
```

引擎名：`clear_queue`

Clears all queued, unplayed animations.

### GetAssignedAnimation

```csharp
public StringName GetAssignedAnimation()
```

引擎名：`get_assigned_animation`

### GetAutoCaptureDuration

```csharp
public double GetAutoCaptureDuration()
```

引擎名：`get_auto_capture_duration`

### GetAutoCaptureEaseType

```csharp
public Tween.EaseType GetAutoCaptureEaseType()
```

引擎名：`get_auto_capture_ease_type`

### GetAutoCaptureTransitionType

```csharp
public Tween.TransitionType GetAutoCaptureTransitionType()
```

引擎名：`get_auto_capture_transition_type`

### GetAutoplay

```csharp
public StringName GetAutoplay()
```

引擎名：`get_autoplay`

### GetBlendTime

```csharp
public double GetBlendTime(StringName animationFrom, StringName animationTo)
```

引擎名：`get_blend_time`

Returns the blend time (in seconds) between two animations, referenced by their keys.

### GetCurrentAnimation

```csharp
public StringName GetCurrentAnimation()
```

引擎名：`get_current_animation`

### GetCurrentAnimationLength

```csharp
public double GetCurrentAnimationLength()
```

引擎名：`get_current_animation_length`

### GetCurrentAnimationPosition

```csharp
public double GetCurrentAnimationPosition()
```

引擎名：`get_current_animation_position`

### GetDefaultBlendTime

```csharp
public double GetDefaultBlendTime()
```

引擎名：`get_default_blend_time`

### GetMethodCallMode

```csharp
public AnimationPlayer.AnimationMethodCallMode GetMethodCallMode()
```

引擎名：`get_method_call_mode`

Returns the call mode used for "Call Method" tracks.

### GetPlayingSpeed

```csharp
public float GetPlayingSpeed()
```

引擎名：`get_playing_speed`

Returns the actual playing speed of current animation or `0` if not playing. This speed is the `SpeedScale` property multiplied by `custom_speed` argument specified when calling the `Play()` method.

Returns a negative value if the current animation is playing backwards.

### GetProcessCallback

```csharp
public AnimationPlayer.AnimationProcessCallback GetProcessCallback()
```

引擎名：`get_process_callback`

Returns the process notification in which to update animations.

### GetQueue

```csharp
public Array<StringName> GetQueue()
```

引擎名：`get_queue`

Returns a list of the animation keys that are currently queued to play.

### GetRoot

```csharp
public NodePath GetRoot()
```

引擎名：`get_root`

Returns the node which node path references will travel from.

### GetSectionEndTime

```csharp
public double GetSectionEndTime()
```

引擎名：`get_section_end_time`

Returns the end time of the section currently being played.

### GetSectionStartTime

```csharp
public double GetSectionStartTime()
```

引擎名：`get_section_start_time`

Returns the start time of the section currently being played.

### GetSpeedScale

```csharp
public float GetSpeedScale()
```

引擎名：`get_speed_scale`

### HasSection

```csharp
public bool HasSection()
```

引擎名：`has_section`

Returns `true` if an animation is currently playing with a section.

### IsAnimationActive

```csharp
public bool IsAnimationActive()
```

引擎名：`is_animation_active`

Returns `true` if the an animation is currently active. An animation is active if it was played by calling `Play()` and was not finished yet, or was stopped by calling `Stop()`.

This can be used to check whether an animation is currently paused or stopped.

```csharp
var is_paused = not is_playing() and is_animation_active()
var is_stopped = not is_playing() and not is_animation_active()

```

### IsAutoCapture

```csharp
public bool IsAutoCapture()
```

引擎名：`is_auto_capture`

### IsMovieQuitOnFinishEnabled

```csharp
public bool IsMovieQuitOnFinishEnabled()
```

引擎名：`is_movie_quit_on_finish_enabled`

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

Pauses the currently playing animation. The `CurrentAnimationPosition` will be kept and calling `Play()` or `PlayBackwards()` without arguments or with the same animation name as `AssignedAnimation` will resume the animation.

See also `Stop()`.

### Play

```csharp
public void Play(StringName name = null, double customBlend = -1, float customSpeed = 1f, bool fromEnd = false)
```

引擎名：`play`

Plays the animation with key `name`. Custom blend times and speed can be set.

The `fromEnd` option only affects when switching to a new animation track, or if the same track but at the start or end. It does not affect resuming playback that was paused in the middle of an animation. If `customSpeed` is negative and `fromEnd` is `true`, the animation will play backwards (which is equivalent to calling `PlayBackwards()`).

The `AnimationPlayer` keeps track of its current or last played animation with `AssignedAnimation`. If this method is called with that same animation `name`, or with no `name` parameter, the assigned animation will resume playing if it was paused.

**Note:** The animation will be updated the next time the `AnimationPlayer` is processed. If other variables are updated at the same time this is called, they may be updated too early. To perform the update immediately, call `advance(0)`.

### PlayBackwards

```csharp
public void PlayBackwards(StringName name = null, double customBlend = -1)
```

引擎名：`play_backwards`

Plays the animation with key `name` in reverse.

This method is a shorthand for `Play()` with `custom_speed = -1.0` and `from_end = true`, so see its description for more information.

### PlaySection

```csharp
public void PlaySection(StringName name = null, double startTime = -1, double endTime = -1, double customBlend = -1, float customSpeed = 1f, bool fromEnd = false)
```

引擎名：`play_section`

Plays the animation with key `name` and the section starting from `startTime` and ending on `endTime`. See also `Play()`.

Setting `startTime` to a value outside the range of the animation means the start of the animation will be used instead, and setting `endTime` to a value outside the range of the animation means the end of the animation will be used instead. `startTime` cannot be equal to `endTime`.

### PlaySectionBackwards

```csharp
public void PlaySectionBackwards(StringName name = null, double startTime = -1, double endTime = -1, double customBlend = -1)
```

引擎名：`play_section_backwards`

Plays the animation with key `name` and the section starting from `startTime` and ending on `endTime` in reverse.

This method is a shorthand for `PlaySection()` with `custom_speed = -1.0` and `from_end = true`, see its description for more information.

### PlaySectionWithMarkers

```csharp
public void PlaySectionWithMarkers(StringName name = null, StringName startMarker = null, StringName endMarker = null, double customBlend = -1, float customSpeed = 1f, bool fromEnd = false)
```

引擎名：`play_section_with_markers`

Plays the animation with key `name` and the section starting from `startMarker` and ending on `endMarker`.

If the start marker is empty, the section starts from the beginning of the animation. If the end marker is empty, the section ends on the end of the animation. See also `Play()`.

### PlaySectionWithMarkersBackwards

```csharp
public void PlaySectionWithMarkersBackwards(StringName name = null, StringName startMarker = null, StringName endMarker = null, double customBlend = -1)
```

引擎名：`play_section_with_markers_backwards`

Plays the animation with key `name` and the section starting from `startMarker` and ending on `endMarker` in reverse.

This method is a shorthand for `PlaySectionWithMarkers()` with `custom_speed = -1.0` and `from_end = true`, see its description for more information.

### PlayWithCapture

```csharp
public void PlayWithCapture(StringName name = null, double duration = -1, double customBlend = -1, float customSpeed = 1f, bool fromEnd = false, Tween.TransitionType transType = Tween.TransitionType.Linear, Tween.EaseType easeType = Tween.EaseType.In)
```

引擎名：`play_with_capture`

See also [`AnimationMixer.Capture`](AnimationMixer.md).

You can use this method to use more detailed options for capture than those performed by `PlaybackAutoCapture`. When `PlaybackAutoCapture` is `false`, this method is almost the same as the following:

```csharp
capture(name, duration, trans_type, ease_type)
play(name, custom_blend, custom_speed, from_end)

```

If `name` is blank, it specifies `AssignedAnimation`.

If `duration` is a negative value, the duration is set to the interval between the current position and the first key, when `fromEnd` is `true`, uses the interval between the current position and the last key instead.

**Note:** The `duration` takes `SpeedScale` into account, but `customSpeed` does not, because the capture cache is interpolated with the blend result and the result may contain multiple animations.

### Queue

```csharp
public void Queue(StringName name)
```

引擎名：`queue`

Queues an animation for playback once the current animation and all previously queued animations are done.

**Note:** If a looped animation is currently playing, the queued animation will never play unless the looped animation is stopped somehow.

### ResetSection

```csharp
public void ResetSection()
```

引擎名：`reset_section`

Resets the current section. Does nothing if a section has not been set.

### Seek

```csharp
public void Seek(double seconds, bool update = false, bool updateOnly = false)
```

引擎名：`seek`

Seeks the animation to the `seconds` point in time (in seconds). If `update` is `true`, the animation updates too, otherwise it updates at process time. Events between the current frame and `seconds` are skipped.

If `updateOnly` is `true`, the method / audio / animation playback tracks will not be processed.

**Note:** Seeking to the end of the animation doesn't emit [`AnimationMixer.AnimationFinished`](AnimationMixer.md). If you want to skip animation and emit the signal, use [`AnimationMixer.Advance`](AnimationMixer.md).

### Seek

```csharp
public void Seek(double seconds, bool update)
```

引擎名：`seek`

Seeks the animation to the `seconds` point in time (in seconds). If `update` is `true`, the animation updates too, otherwise it updates at process time. Events between the current frame and `seconds` are skipped.

If `updateOnly` is `true`, the method / audio / animation playback tracks will not be processed.

**Note:** Seeking to the end of the animation doesn't emit [`AnimationMixer.AnimationFinished`](AnimationMixer.md). If you want to skip animation and emit the signal, use [`AnimationMixer.Advance`](AnimationMixer.md).

### SetAssignedAnimation

```csharp
public void SetAssignedAnimation(StringName animation)
```

引擎名：`set_assigned_animation`

### SetAssignedAnimation

```csharp
public void SetAssignedAnimation(string animation)
```

引擎名：`set_assigned_animation`

### SetAutoCapture

```csharp
public void SetAutoCapture(bool autoCapture)
```

引擎名：`set_auto_capture`

### SetAutoCaptureDuration

```csharp
public void SetAutoCaptureDuration(double autoCaptureDuration)
```

引擎名：`set_auto_capture_duration`

### SetAutoCaptureEaseType

```csharp
public void SetAutoCaptureEaseType(Tween.EaseType autoCaptureEaseType)
```

引擎名：`set_auto_capture_ease_type`

### SetAutoCaptureTransitionType

```csharp
public void SetAutoCaptureTransitionType(Tween.TransitionType autoCaptureTransitionType)
```

引擎名：`set_auto_capture_transition_type`

### SetAutoplay

```csharp
public void SetAutoplay(StringName name)
```

引擎名：`set_autoplay`

### SetAutoplay

```csharp
public void SetAutoplay(string name)
```

引擎名：`set_autoplay`

### SetBlendTime

```csharp
public void SetBlendTime(StringName animationFrom, StringName animationTo, double sec)
```

引擎名：`set_blend_time`

Specifies a blend time (in seconds) between two animations, referenced by their keys.

### SetCurrentAnimation

```csharp
public void SetCurrentAnimation(StringName animation)
```

引擎名：`set_current_animation`

### SetCurrentAnimation

```csharp
public void SetCurrentAnimation(string animation)
```

引擎名：`set_current_animation`

### SetDefaultBlendTime

```csharp
public void SetDefaultBlendTime(double sec)
```

引擎名：`set_default_blend_time`

### SetMethodCallMode

```csharp
public void SetMethodCallMode(AnimationPlayer.AnimationMethodCallMode mode)
```

引擎名：`set_method_call_mode`

Sets the call mode used for "Call Method" tracks.

### SetMovieQuitOnFinishEnabled

```csharp
public void SetMovieQuitOnFinishEnabled(bool enabled)
```

引擎名：`set_movie_quit_on_finish_enabled`

### SetProcessCallback

```csharp
public void SetProcessCallback(AnimationPlayer.AnimationProcessCallback mode)
```

引擎名：`set_process_callback`

Sets the process notification in which to update animations.

### SetRoot

```csharp
public void SetRoot(NodePath path)
```

引擎名：`set_root`

Sets the node which node path references will travel from.

### SetSection

```csharp
public void SetSection(double startTime = -1, double endTime = -1)
```

引擎名：`set_section`

Changes the start and end times of the section being played. The current playback position will be clamped within the new section. See also `PlaySection()`.

### SetSectionWithMarkers

```csharp
public void SetSectionWithMarkers(StringName startMarker = null, StringName endMarker = null)
```

引擎名：`set_section_with_markers`

Changes the start and end markers of the section being played. The current playback position will be clamped within the new section. See also `PlaySectionWithMarkers()`.

If the argument is empty, the section uses the beginning or end of the animation. If both are empty, it means that the section is not set.

### SetSpeedScale

```csharp
public void SetSpeedScale(float speed)
```

引擎名：`set_speed_scale`

### Stop

```csharp
public void Stop(bool keepState = false)
```

引擎名：`stop`

Stops the currently playing animation. The animation position is reset to `0` and the `custom_speed` is reset to `1.0`. See also `Pause()`.

If `keepState` is `true`, the animation state is not updated visually.

**Note:** The method / audio / animation playback tracks will not be processed by this method.

## 事件（信号）

### AnimationChanged

```csharp
public event AnimationPlayer.AnimationChangedEventHandler AnimationChanged
```

引擎名：`animation_changed`

Emitted when a queued animation plays after the previous animation finished. See also `Queue()`.

**Note:** The signal is not emitted when the animation is changed via `Play()` or by an [`AnimationTree`](AnimationTree.md).

### CurrentAnimationChanged

```csharp
public event AnimationPlayer.CurrentAnimationChangedEventHandler CurrentAnimationChanged
```

引擎名：`current_animation_changed`

Emitted when `CurrentAnimation` changes.

## AnimationChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`AnimationPlayer.AnimationChanged`](AnimationPlayer.md) event of a [`AnimationPlayer`](AnimationPlayer.md) class.

### 签名

#### Invoke

```csharp
delegate void AnimationChangedEventHandler(StringName oldName, StringName newName)
```

## AnimationMethodCallMode

命名空间 `Godot`；enum

### 枚举值

- `Deferred = 0`
- `Immediate = 1`

## AnimationProcessCallback

命名空间 `Godot`；enum

### 枚举值

- `Physics = 0`
- `Idle = 1`
- `Manual = 2`

## CurrentAnimationChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`AnimationPlayer.CurrentAnimationChanged`](AnimationPlayer.md) event of a [`AnimationPlayer`](AnimationPlayer.md) class.

### 签名

#### Invoke

```csharp
delegate void CurrentAnimationChangedEventHandler(StringName animName)
```
