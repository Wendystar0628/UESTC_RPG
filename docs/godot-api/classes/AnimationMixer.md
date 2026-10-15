<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AnimationMixer

命名空间 `Godot`；class；继承 [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Base class for [`AnimationPlayer`](AnimationPlayer.md) and [`AnimationTree`](AnimationTree.md) to manage animation lists. It also has general properties and methods for playback and blending.

After instantiating the playback information data within the extended class, the blending is processed by the `AnimationMixer`.

## 属性

### Active

```csharp
public bool Active { get; set; }
```

引擎名：`active`

If `true`, the `AnimationMixer` will be processing.

### AudioMaxPolyphony

```csharp
public int AudioMaxPolyphony { get; set; }
```

引擎名：`audio_max_polyphony`

The number of possible simultaneous sounds for each of the assigned AudioStreamPlayers.

For example, if this value is `32` and the animation has two audio tracks, the two [`AudioStreamPlayer`](AudioStreamPlayer.md)s assigned can play simultaneously up to `32` voices each.

### CallbackModeDiscrete

```csharp
public AnimationMixer.AnimationCallbackModeDiscrete CallbackModeDiscrete { get; set; }
```

引擎名：`callback_mode_discrete`

Ordinarily, tracks can be set to [`Animation.UpdateMode.Discrete`](Animation.md) to update infrequently, usually when using nearest interpolation.

However, when blending with [`Animation.UpdateMode.Continuous`](Animation.md) several results are considered. The `CallbackModeDiscrete` specify it explicitly. See also [`AnimationMixer.AnimationCallbackModeDiscrete`](AnimationMixer.md).

To make the blended results look good, it is recommended to set this to [`AnimationMixer.AnimationCallbackModeDiscrete.ForceContinuous`](AnimationMixer.md) to update every frame during blending. Other values exist for compatibility and they are fine if there is no blending, but not so, may produce artifacts.

### CallbackModeMethod

```csharp
public AnimationMixer.AnimationCallbackModeMethod CallbackModeMethod { get; set; }
```

引擎名：`callback_mode_method`

The call mode used for "Call Method" tracks.

### CallbackModeProcess

```csharp
public AnimationMixer.AnimationCallbackModeProcess CallbackModeProcess { get; set; }
```

引擎名：`callback_mode_process`

The process notification in which to update animations.

### Deterministic

```csharp
public bool Deterministic { get; set; }
```

引擎名：`deterministic`

If `true`, the blending uses the deterministic algorithm. The total weight is not normalized and the result is accumulated with an initial value (`0` or a `"RESET"` animation if present).

This means that if the total amount of blending is `0.0`, the result is equal to the `"RESET"` animation.

If the number of tracks between the blended animations is different, the animation with the missing track is treated as if it had the initial value.

If `false`, The blend does not use the deterministic algorithm. The total weight is normalized and always `1.0`. If the number of tracks between the blended animations is different, nothing is done about the animation that is missing a track.

**Note:** In [`AnimationTree`](AnimationTree.md), the blending with [`AnimationNodeAdd2`](AnimationNodeAdd2.md), [`AnimationNodeAdd3`](AnimationNodeAdd3.md), [`AnimationNodeSub2`](AnimationNodeSub2.md) or the weight greater than `1.0` may produce unexpected results.

For example, if [`AnimationNodeAdd2`](AnimationNodeAdd2.md) blends two nodes with the amount `1.0`, then total weight is `2.0` but it will be normalized to make the total amount `1.0` and the result will be equal to [`AnimationNodeBlend2`](AnimationNodeBlend2.md) with the amount `0.5`.

### ResetOnSave

```csharp
public bool ResetOnSave { get; set; }
```

引擎名：`reset_on_save`

This is used by the editor. If set to `true`, the scene will be saved with the effects of the reset animation (the animation with the key `"RESET"`) applied as if it had been seeked to time 0, with the editor keeping the values that the scene had before saving.

This makes it more convenient to preview and edit animations in the editor, as changes to the scene will not be saved as long as they are set in the reset animation.

### RootMotionLocal

```csharp
public bool RootMotionLocal { get; set; }
```

引擎名：`root_motion_local`

If `true`, `GetRootMotionPosition()` value is extracted as a local translation value before blending. In other words, it is treated like the translation is done after the rotation.

### RootMotionTrack

```csharp
public NodePath RootMotionTrack { get; set; }
```

引擎名：`root_motion_track`

The path to the Animation track used for root motion. Paths must be valid scene-tree paths to a node, and must be specified starting from the parent node of the node that will reproduce the animation. The `RootMotionTrack` uses the same format as [`Animation.TrackSetPath`](Animation.md), but note that a bone must be specified.

If the track has type [`Animation.TrackType.Position3D`](Animation.md), [`Animation.TrackType.Rotation3D`](Animation.md), or [`Animation.TrackType.Scale3D`](Animation.md) the transformation will be canceled visually, and the animation will appear to stay in place. See also `GetRootMotionPosition()`, `GetRootMotionRotation()`, `GetRootMotionScale()`, and [`RootMotionView`](RootMotionView.md).

### RootNode

```csharp
public NodePath RootNode { get; set; }
```

引擎名：`root_node`

The node which node path references will travel from.

## 方法

### AddAnimationLibrary

```csharp
public Error AddAnimationLibrary(StringName name, AnimationLibrary library)
```

引擎名：`add_animation_library`

Adds `library` to the animation player, under the key `name`.

AnimationMixer has a global library by default with an empty string as key. For adding an animation to the global library:

### Advance

```csharp
public void Advance(double delta)
```

引擎名：`advance`

Manually advance the animations by the specified time (in seconds).

### Capture

```csharp
public void Capture(StringName name, double duration, Tween.TransitionType transType = Tween.TransitionType.Linear, Tween.EaseType easeType = Tween.EaseType.In)
```

引擎名：`capture`

If the animation track specified by `name` has an option [`Animation.UpdateMode.Capture`](Animation.md), stores current values of the objects indicated by the track path as a cache. If there is already a captured cache, the old cache is discarded.

After this it will interpolate with current animation blending result during the playback process for the time specified by `duration`, working like a crossfade.

You can specify `transType` as the curve for the interpolation. For better results, it may be appropriate to specify [`Tween.TransitionType.Linear`](Tween.md) for cases where the first key of the track begins with a non-zero value or where the key value does not change, and [`Tween.TransitionType.Quad`](Tween.md) for cases where the key value changes linearly.

### ClearCaches

```csharp
public void ClearCaches()
```

引擎名：`clear_caches`

`AnimationMixer` caches animated nodes. It may not notice if a node disappears; `ClearCaches()` forces it to update the cache again.

### FindAnimation

```csharp
public StringName FindAnimation(Animation animation)
```

引擎名：`find_animation`

Returns the key of `animation` or an empty [`StringName`](StringName.md) if not found.

### FindAnimationLibrary

```csharp
public StringName FindAnimationLibrary(Animation animation)
```

引擎名：`find_animation_library`

Returns the key for the [`AnimationLibrary`](AnimationLibrary.md) that contains `animation` or an empty [`StringName`](StringName.md) if not found.

### GetAnimation

```csharp
public Animation GetAnimation(StringName name)
```

引擎名：`get_animation`

Returns the [`Animation`](Animation.md) with the key `name`. If the animation does not exist, `null` is returned and an error is logged.

### GetAnimationLibrary

```csharp
public AnimationLibrary GetAnimationLibrary(StringName name)
```

引擎名：`get_animation_library`

Returns the first [`AnimationLibrary`](AnimationLibrary.md) with key `name` or `null` if not found.

To get the `AnimationMixer`'s global animation library, use `get_animation_library("")`.

### GetAnimationLibraryList

```csharp
public Array<StringName> GetAnimationLibraryList()
```

引擎名：`get_animation_library_list`

Returns the list of stored library keys.

### GetAnimationList

```csharp
public string[] GetAnimationList()
```

引擎名：`get_animation_list`

Returns the list of stored animation keys.

### GetAudioMaxPolyphony

```csharp
public int GetAudioMaxPolyphony()
```

引擎名：`get_audio_max_polyphony`

### GetCallbackModeDiscrete

```csharp
public AnimationMixer.AnimationCallbackModeDiscrete GetCallbackModeDiscrete()
```

引擎名：`get_callback_mode_discrete`

### GetCallbackModeMethod

```csharp
public AnimationMixer.AnimationCallbackModeMethod GetCallbackModeMethod()
```

引擎名：`get_callback_mode_method`

### GetCallbackModeProcess

```csharp
public AnimationMixer.AnimationCallbackModeProcess GetCallbackModeProcess()
```

引擎名：`get_callback_mode_process`

### GetRootMotionPosition

```csharp
public Vector3 GetRootMotionPosition()
```

引擎名：`get_root_motion_position`

Retrieve the motion delta of position with the `RootMotionTrack` as a [`Vector3`](Vector3.md) that can be used elsewhere.

If `RootMotionTrack` is not a path to a track of type [`Animation.TrackType.Position3D`](Animation.md), returns `Vector3(0, 0, 0)`.

See also `RootMotionTrack` and [`RootMotionView`](RootMotionView.md).

The most basic example is applying position to [`CharacterBody3D`](CharacterBody3D.md):

By using this in combination with `GetRootMotionRotationAccumulator()`, you can apply the root motion position more correctly to account for the rotation of the node.

If `RootMotionLocal` is `true`, returns the pre-multiplied translation value with the inverted rotation.

In this case, the code can be written as follows:

### GetRootMotionPositionAccumulator

```csharp
public Vector3 GetRootMotionPositionAccumulator()
```

引擎名：`get_root_motion_position_accumulator`

Retrieve the blended value of the position tracks with the `RootMotionTrack` as a [`Vector3`](Vector3.md) that can be used elsewhere.

This is useful in cases where you want to respect the initial key values of the animation.

For example, if an animation with only one key `Vector3(0, 0, 0)` is played in the previous frame and then an animation with only one key `Vector3(1, 0, 1)` is played in the next frame, the difference can be calculated as follows:

However, if the animation loops, an unintended discrete change may occur, so this is only useful for some simple use cases.

### GetRootMotionRotation

```csharp
public Quaternion GetRootMotionRotation()
```

引擎名：`get_root_motion_rotation`

Retrieve the motion delta of rotation with the `RootMotionTrack` as a [`Quaternion`](Quaternion.md) that can be used elsewhere.

If `RootMotionTrack` is not a path to a track of type [`Animation.TrackType.Rotation3D`](Animation.md), returns `Quaternion(0, 0, 0, 1)`.

See also `RootMotionTrack` and [`RootMotionView`](RootMotionView.md).

The most basic example is applying rotation to [`CharacterBody3D`](CharacterBody3D.md):

### GetRootMotionRotationAccumulator

```csharp
public Quaternion GetRootMotionRotationAccumulator()
```

引擎名：`get_root_motion_rotation_accumulator`

Retrieve the blended value of the rotation tracks with the `RootMotionTrack` as a [`Quaternion`](Quaternion.md) that can be used elsewhere.

This is necessary to apply the root motion position correctly, taking rotation into account. See also `GetRootMotionPosition()`.

Also, this is useful in cases where you want to respect the initial key values of the animation.

For example, if an animation with only one key `Quaternion(0, 0, 0, 1)` is played in the previous frame and then an animation with only one key `Quaternion(0, 0.707, 0, 0.707)` is played in the next frame, the difference can be calculated as follows:

However, if the animation loops, an unintended discrete change may occur, so this is only useful for some simple use cases.

### GetRootMotionScale

```csharp
public Vector3 GetRootMotionScale()
```

引擎名：`get_root_motion_scale`

Retrieve the motion delta of scale with the `RootMotionTrack` as a [`Vector3`](Vector3.md) that can be used elsewhere.

If `RootMotionTrack` is not a path to a track of type [`Animation.TrackType.Scale3D`](Animation.md), returns `Vector3(0, 0, 0)`.

See also `RootMotionTrack` and [`RootMotionView`](RootMotionView.md).

The most basic example is applying scale to [`CharacterBody3D`](CharacterBody3D.md):

### GetRootMotionScaleAccumulator

```csharp
public Vector3 GetRootMotionScaleAccumulator()
```

引擎名：`get_root_motion_scale_accumulator`

Retrieve the blended value of the scale tracks with the `RootMotionTrack` as a [`Vector3`](Vector3.md) that can be used elsewhere.

For example, if an animation with only one key `Vector3(1, 1, 1)` is played in the previous frame and then an animation with only one key `Vector3(2, 2, 2)` is played in the next frame, the difference can be calculated as follows:

However, if the animation loops, an unintended discrete change may occur, so this is only useful for some simple use cases.

### GetRootMotionTrack

```csharp
public NodePath GetRootMotionTrack()
```

引擎名：`get_root_motion_track`

### GetRootNode

```csharp
public NodePath GetRootNode()
```

引擎名：`get_root_node`

### HasAnimation

```csharp
public bool HasAnimation(StringName name)
```

引擎名：`has_animation`

Returns `true` if the `AnimationMixer` stores an [`Animation`](Animation.md) with key `name`.

### HasAnimationLibrary

```csharp
public bool HasAnimationLibrary(StringName name)
```

引擎名：`has_animation_library`

Returns `true` if the `AnimationMixer` stores an [`AnimationLibrary`](AnimationLibrary.md) with key `name`.

### IsActive

```csharp
public bool IsActive()
```

引擎名：`is_active`

### IsDeterministic

```csharp
public bool IsDeterministic()
```

引擎名：`is_deterministic`

### IsResetOnSaveEnabled

```csharp
public bool IsResetOnSaveEnabled()
```

引擎名：`is_reset_on_save_enabled`

### IsRootMotionLocal

```csharp
public bool IsRootMotionLocal()
```

引擎名：`is_root_motion_local`

### RemoveAnimationLibrary

```csharp
public void RemoveAnimationLibrary(StringName name)
```

引擎名：`remove_animation_library`

Removes the [`AnimationLibrary`](AnimationLibrary.md) associated with the key `name`.

### RenameAnimationLibrary

```csharp
public void RenameAnimationLibrary(StringName name, StringName newname)
```

引擎名：`rename_animation_library`

Moves the [`AnimationLibrary`](AnimationLibrary.md) associated with the key `name` to the key `newname`.

### SetActive

```csharp
public void SetActive(bool active)
```

引擎名：`set_active`

### SetAudioMaxPolyphony

```csharp
public void SetAudioMaxPolyphony(int maxPolyphony)
```

引擎名：`set_audio_max_polyphony`

### SetCallbackModeDiscrete

```csharp
public void SetCallbackModeDiscrete(AnimationMixer.AnimationCallbackModeDiscrete mode)
```

引擎名：`set_callback_mode_discrete`

### SetCallbackModeMethod

```csharp
public void SetCallbackModeMethod(AnimationMixer.AnimationCallbackModeMethod mode)
```

引擎名：`set_callback_mode_method`

### SetCallbackModeProcess

```csharp
public void SetCallbackModeProcess(AnimationMixer.AnimationCallbackModeProcess mode)
```

引擎名：`set_callback_mode_process`

### SetDeterministic

```csharp
public void SetDeterministic(bool deterministic)
```

引擎名：`set_deterministic`

### SetResetOnSaveEnabled

```csharp
public void SetResetOnSaveEnabled(bool enabled)
```

引擎名：`set_reset_on_save_enabled`

### SetRootMotionLocal

```csharp
public void SetRootMotionLocal(bool enabled)
```

引擎名：`set_root_motion_local`

### SetRootMotionTrack

```csharp
public void SetRootMotionTrack(NodePath path)
```

引擎名：`set_root_motion_track`

### SetRootNode

```csharp
public void SetRootNode(NodePath path)
```

引擎名：`set_root_node`

### _PostProcessKeyValue

```csharp
public Variant _PostProcessKeyValue(Animation animation, int track, Variant value, GodotObject object, int objectIdx)
```

引擎名：`_post_process_key_value`

A virtual function for processing after getting a key during playback.

### _PostProcessKeyValue

```csharp
public virtual Variant _PostProcessKeyValue(Animation animation, int track, Variant value, ulong objectId, int objectSubIdx)
```

引擎名：`_post_process_key_value`

A virtual function for processing after getting a key during playback.

## 事件（信号）

### AnimationFinished

```csharp
public event AnimationMixer.AnimationFinishedEventHandler AnimationFinished
```

引擎名：`animation_finished`

Notifies when an animation finished playing.

**Note:** This signal is not emitted if an animation is looping.

### AnimationLibrariesUpdated

```csharp
public event Action AnimationLibrariesUpdated
```

引擎名：`animation_libraries_updated`

Notifies when the animation libraries have changed.

### AnimationListChanged

```csharp
public event Action AnimationListChanged
```

引擎名：`animation_list_changed`

Notifies when an animation list is changed.

### AnimationStarted

```csharp
public event AnimationMixer.AnimationStartedEventHandler AnimationStarted
```

引擎名：`animation_started`

Notifies when an animation starts playing.

**Note:** This signal is not emitted if an animation is looping.

### CachesCleared

```csharp
public event Action CachesCleared
```

引擎名：`caches_cleared`

Notifies when the caches have been cleared, either automatically, or manually via `ClearCaches()`.

### MixerApplied

```csharp
public event Action MixerApplied
```

引擎名：`mixer_applied`

Notifies when the blending result related have been applied to the target objects.

### MixerUpdated

```csharp
public event Action MixerUpdated
```

引擎名：`mixer_updated`

Notifies when the property related process have been updated.

## AnimationCallbackModeDiscrete

命名空间 `Godot`；enum

### 枚举值

- `Dominant = 0`：An [`Animation.UpdateMode.Discrete`](Animation.md) track value takes precedence when blending [`Animation.UpdateMode.Continuous`](Animation.md) or [`Animation.UpdateMode.Capture`](Animation.md) track values and [`Animati…
- `Recessive = 1`：An [`Animation.UpdateMode.Continuous`](Animation.md) or [`Animation.UpdateMode.Capture`](Animation.md) track value takes precedence when blending the [`Animation.UpdateMode.Continuous`](Animation.md) or [`Animation.Updat…
- `ForceContinuous = 2`：Always treat the [`Animation.UpdateMode.Discrete`](Animation.md) track value as [`Animation.UpdateMode.Continuous`](Animation.md) with [`Animation.InterpolationType.Nearest`](Animation.md).

## AnimationCallbackModeMethod

命名空间 `Godot`；enum

### 枚举值

- `Deferred = 0`：Batch method calls during the animation process, then do the calls after events are processed.
- `Immediate = 1`：Make method calls immediately when reached in the animation.

## AnimationCallbackModeProcess

命名空间 `Godot`；enum

### 枚举值

- `Physics = 0`：Process animation during physics frames (see [`Node.NotificationInternalPhysicsProcess`](Node.md)).
- `Idle = 1`：Process animation during process frames (see [`Node.NotificationInternalProcess`](Node.md)).
- `Manual = 2`：Do not process animation.

## AnimationFinishedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`AnimationMixer.AnimationFinished`](AnimationMixer.md) event of a [`AnimationMixer`](AnimationMixer.md) class.

### 签名

#### Invoke

```csharp
delegate void AnimationFinishedEventHandler(StringName animName)
```

## AnimationStartedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`AnimationMixer.AnimationStarted`](AnimationMixer.md) event of a [`AnimationMixer`](AnimationMixer.md) class.

### 签名

#### Invoke

```csharp
delegate void AnimationStartedEventHandler(StringName animName)
```
