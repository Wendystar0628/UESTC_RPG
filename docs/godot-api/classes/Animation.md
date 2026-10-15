<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Animation

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This resource holds data that can be used to animate anything in the engine. Animations are divided into tracks and each track must be linked to a node. The state of that node can be changed through time, by adding timed keys (events) to the track.

```csharp
// This creates an animation that makes the node "Enemy" move to the right by
// 100 pixels in 2.0 seconds.
var animation = new Animation();
int trackIndex = animation.AddTrack(Animation.TrackType.Value);
animation.TrackSetPath(trackIndex, "Enemy:position:x");
animation.TrackInsertKey(trackIndex, 0.0f, 0);
animation.TrackInsertKey(trackIndex, 2.0f, 100);
animation.Length = 2.0f;

```

Animations are just data containers, and must be added to nodes such as an [`AnimationPlayer`](AnimationPlayer.md) to be played back. Animation tracks have different types, each with its own set of dedicated methods. Check [`Animation.TrackType`](Animation.md) to see available types.

**Note:** For 3D position/rotation/scale, using the dedicated [`Animation.TrackType.Position3D`](Animation.md), [`Animation.TrackType.Rotation3D`](Animation.md) and [`Animation.TrackType.Scale3D`](Animation.md) track types instead of [`Animation.TrackType.Value`](Animation.md) is recommended for performance reasons.

## 构造函数

### Animation

```csharp
public Animation()
```

## 属性

### CaptureIncluded

```csharp
public bool CaptureIncluded { get; }
```

引擎名：`capture_included`

Returns `true` if the capture track is included. This is a cached readonly value for performance.

### Length

```csharp
public double Length { get; set; }
```

引擎名：`length`

The total length of the animation (in seconds).

**Note:** Length is not delimited by the last key, as this one may be before or after the end to ensure correct interpolation and looping.

### LoopMode

```csharp
public Animation.LoopModeEnum LoopMode { get; set; }
```

引擎名：`loop_mode`

Determines the behavior of both ends of the animation timeline during animation playback. This indicates whether and how the animation should be restarted, and is also used to correctly interpolate animation cycles.

### Step

```csharp
public float Step { get; set; }
```

引擎名：`step`

The animation step value.

## 方法

### AddMarker

```csharp
public void AddMarker(StringName name, double time)
```

引擎名：`add_marker`

Adds a marker to this Animation.

### AddTrack

```csharp
public int AddTrack(Animation.TrackType type, int atPosition = -1)
```

引擎名：`add_track`

Adds a track to the Animation.

### AnimationTrackGetKeyAnimation

```csharp
public StringName AnimationTrackGetKeyAnimation(int trackIdx, int keyIdx)
```

引擎名：`animation_track_get_key_animation`

Returns the animation name at the key identified by `keyIdx`. The `trackIdx` must be the index of an Animation Track.

### AnimationTrackInsertKey

```csharp
public int AnimationTrackInsertKey(int trackIdx, double time, StringName animation)
```

引擎名：`animation_track_insert_key`

Inserts a key with value `animation` at the given `time` (in seconds). The `trackIdx` must be the index of an Animation Track.

### AnimationTrackSetKeyAnimation

```csharp
public void AnimationTrackSetKeyAnimation(int trackIdx, int keyIdx, StringName animation)
```

引擎名：`animation_track_set_key_animation`

Sets the key identified by `keyIdx` to value `animation`. The `trackIdx` must be the index of an Animation Track.

### AudioTrackGetKeyEndOffset

```csharp
public float AudioTrackGetKeyEndOffset(int trackIdx, int keyIdx)
```

引擎名：`audio_track_get_key_end_offset`

Returns the end offset of the key identified by `keyIdx`. The `trackIdx` must be the index of an Audio Track.

End offset is the number of seconds cut off at the ending of the audio stream.

### AudioTrackGetKeyStartOffset

```csharp
public float AudioTrackGetKeyStartOffset(int trackIdx, int keyIdx)
```

引擎名：`audio_track_get_key_start_offset`

Returns the start offset of the key identified by `keyIdx`. The `trackIdx` must be the index of an Audio Track.

Start offset is the number of seconds cut off at the beginning of the audio stream.

### AudioTrackGetKeyStream

```csharp
public Resource AudioTrackGetKeyStream(int trackIdx, int keyIdx)
```

引擎名：`audio_track_get_key_stream`

Returns the audio stream of the key identified by `keyIdx`. The `trackIdx` must be the index of an Audio Track.

### AudioTrackInsertKey

```csharp
public int AudioTrackInsertKey(int trackIdx, double time, Resource stream, float startOffset = 0f, float endOffset = 0f)
```

引擎名：`audio_track_insert_key`

Inserts an Audio Track key at the given `time` in seconds. The `trackIdx` must be the index of an Audio Track.

`stream` is the [`AudioStream`](AudioStream.md) resource to play. `startOffset` is the number of seconds cut off at the beginning of the audio stream, while `endOffset` is at the ending.

### AudioTrackIsUseBlend

```csharp
public bool AudioTrackIsUseBlend(int trackIdx)
```

引擎名：`audio_track_is_use_blend`

Returns `true` if the track at `trackIdx` will be blended with other animations.

### AudioTrackSetKeyEndOffset

```csharp
public void AudioTrackSetKeyEndOffset(int trackIdx, int keyIdx, float offset)
```

引擎名：`audio_track_set_key_end_offset`

Sets the end offset of the key identified by `keyIdx` to value `offset`. The `trackIdx` must be the index of an Audio Track.

### AudioTrackSetKeyStartOffset

```csharp
public void AudioTrackSetKeyStartOffset(int trackIdx, int keyIdx, float offset)
```

引擎名：`audio_track_set_key_start_offset`

Sets the start offset of the key identified by `keyIdx` to value `offset`. The `trackIdx` must be the index of an Audio Track.

### AudioTrackSetKeyStream

```csharp
public void AudioTrackSetKeyStream(int trackIdx, int keyIdx, Resource stream)
```

引擎名：`audio_track_set_key_stream`

Sets the stream of the key identified by `keyIdx` to value `stream`. The `trackIdx` must be the index of an Audio Track.

### AudioTrackSetUseBlend

```csharp
public void AudioTrackSetUseBlend(int trackIdx, bool enable)
```

引擎名：`audio_track_set_use_blend`

Sets whether the track will be blended with other animations. If `true`, the audio playback volume changes depending on the blend value.

### BezierTrackGetKeyInHandle

```csharp
public Vector2 BezierTrackGetKeyInHandle(int trackIdx, int keyIdx)
```

引擎名：`bezier_track_get_key_in_handle`

Returns the in handle of the key identified by `keyIdx`. The `trackIdx` must be the index of a Bezier Track.

### BezierTrackGetKeyOutHandle

```csharp
public Vector2 BezierTrackGetKeyOutHandle(int trackIdx, int keyIdx)
```

引擎名：`bezier_track_get_key_out_handle`

Returns the out handle of the key identified by `keyIdx`. The `trackIdx` must be the index of a Bezier Track.

### BezierTrackGetKeyValue

```csharp
public float BezierTrackGetKeyValue(int trackIdx, int keyIdx)
```

引擎名：`bezier_track_get_key_value`

Returns the value of the key identified by `keyIdx`. The `trackIdx` must be the index of a Bezier Track.

### BezierTrackInsertKey

```csharp
public int BezierTrackInsertKey(int trackIdx, double time, float value, Vector2? inHandle = null, Vector2? outHandle = null)
```

引擎名：`bezier_track_insert_key`

Inserts a Bezier Track key at the given `time` in seconds. The `trackIdx` must be the index of a Bezier Track.

`inHandle` is the left-side weight of the added Bezier curve point, `outHandle` is the right-side one, while `value` is the actual value at this point.

参数：
- `inHandle`：If the parameter is null, then the default value is `new Vector2(0.0f, 0.0f)`.
- `outHandle`：If the parameter is null, then the default value is `new Vector2(0.0f, 0.0f)`.

### BezierTrackInterpolate

```csharp
public float BezierTrackInterpolate(int trackIdx, double time)
```

引擎名：`bezier_track_interpolate`

Returns the interpolated value at the given `time` (in seconds). The `trackIdx` must be the index of a Bezier Track.

### BezierTrackSetKeyInHandle

```csharp
public void BezierTrackSetKeyInHandle(int trackIdx, int keyIdx, Vector2 inHandle, float balancedValueTimeRatio = 1f)
```

引擎名：`bezier_track_set_key_in_handle`

Sets the in handle of the key identified by `keyIdx` to value `inHandle`. The `trackIdx` must be the index of a Bezier Track.

### BezierTrackSetKeyOutHandle

```csharp
public void BezierTrackSetKeyOutHandle(int trackIdx, int keyIdx, Vector2 outHandle, float balancedValueTimeRatio = 1f)
```

引擎名：`bezier_track_set_key_out_handle`

Sets the out handle of the key identified by `keyIdx` to value `outHandle`. The `trackIdx` must be the index of a Bezier Track.

### BezierTrackSetKeyValue

```csharp
public void BezierTrackSetKeyValue(int trackIdx, int keyIdx, float value)
```

引擎名：`bezier_track_set_key_value`

Sets the value of the key identified by `keyIdx` to the given value. The `trackIdx` must be the index of a Bezier Track.

### BlendShapeTrackInsertKey

```csharp
public int BlendShapeTrackInsertKey(int trackIdx, double time, float amount)
```

引擎名：`blend_shape_track_insert_key`

Inserts a key in a given blend shape track. Returns the key index.

### BlendShapeTrackInterpolate

```csharp
public float BlendShapeTrackInterpolate(int trackIdx, double timeSec)
```

引擎名：`blend_shape_track_interpolate`

Returns the interpolated blend shape value at the given time (in seconds). The `trackIdx` must be the index of a blend shape track.

### BlendShapeTrackInterpolate

```csharp
public float BlendShapeTrackInterpolate(int trackIdx, double timeSec, bool backward = false)
```

引擎名：`blend_shape_track_interpolate`

Returns the interpolated blend shape value at the given time (in seconds). The `trackIdx` must be the index of a blend shape track.

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Clear the animation (clear all tracks and reset all).

### Compress

```csharp
public void Compress(uint pageSize = 8192, uint fps = 120, float splitTolerance = 4f)
```

引擎名：`compress`

Compress the animation and all its tracks in-place. This will make `TrackIsCompressed()` return `true` once called on this `Animation`. Compressed tracks require less memory to be played, and are designed to be used for complex 3D animations (such as cutscenes) imported from external 3D software. Compression is lossy, but the difference is usually not noticeable in real world conditions.

**Note:** Compressed tracks have various limitations (such as not being editable from the editor), so only use compressed animations if you actually need them.

### CopyTrack

```csharp
public void CopyTrack(int trackIdx, Animation toAnimation)
```

引擎名：`copy_track`

Adds a new track to `toAnimation` that is a copy of the given track from this animation.

### FindTrack

```csharp
public int FindTrack(NodePath path, Animation.TrackType type)
```

引擎名：`find_track`

Returns the index of the specified track. If the track is not found, return -1.

### GetLength

```csharp
public double GetLength()
```

引擎名：`get_length`

### GetLoopMode

```csharp
public Animation.LoopModeEnum GetLoopMode()
```

引擎名：`get_loop_mode`

### GetMarkerAtTime

```csharp
public StringName GetMarkerAtTime(double time)
```

引擎名：`get_marker_at_time`

Returns the name of the marker located at the given time.

### GetMarkerColor

```csharp
public Color GetMarkerColor(StringName name)
```

引擎名：`get_marker_color`

Returns the given marker's color.

### GetMarkerNames

```csharp
public string[] GetMarkerNames()
```

引擎名：`get_marker_names`

Returns every marker in this Animation, sorted ascending by time.

### GetMarkerTime

```csharp
public double GetMarkerTime(StringName name)
```

引擎名：`get_marker_time`

Returns the given marker's time.

### GetNextMarker

```csharp
public StringName GetNextMarker(double time)
```

引擎名：`get_next_marker`

Returns the closest marker that comes after the given time. If no such marker exists, an empty string is returned.

### GetPrevMarker

```csharp
public StringName GetPrevMarker(double time)
```

引擎名：`get_prev_marker`

Returns the closest marker that comes before the given time. If no such marker exists, an empty string is returned.

### GetStep

```csharp
public float GetStep()
```

引擎名：`get_step`

### GetTrackCount

```csharp
public int GetTrackCount()
```

引擎名：`get_track_count`

Returns the amount of tracks in the animation.

### HasMarker

```csharp
public bool HasMarker(StringName name)
```

引擎名：`has_marker`

Returns `true` if this Animation contains a marker with the given name.

### IsCaptureIncluded

```csharp
public bool IsCaptureIncluded()
```

引擎名：`is_capture_included`

### MethodTrackGetName

```csharp
public StringName MethodTrackGetName(int trackIdx, int keyIdx)
```

引擎名：`method_track_get_name`

Returns the method name of a method track.

### MethodTrackGetParams

```csharp
public Array MethodTrackGetParams(int trackIdx, int keyIdx)
```

引擎名：`method_track_get_params`

Returns the arguments values to be called on a method track for a given key in a given track.

### Optimize

```csharp
public void Optimize(float allowedVelocityErr = 0.01f, float allowedAngularErr = 0.01f, int precision = 3)
```

引擎名：`optimize`

Optimize the animation and all its tracks in-place. This will preserve only as many keys as are necessary to keep the animation within the specified bounds.

### PositionTrackInsertKey

```csharp
public int PositionTrackInsertKey(int trackIdx, double time, Vector3 position)
```

引擎名：`position_track_insert_key`

Inserts a key in a given 3D position track. Returns the key index.

### PositionTrackInterpolate

```csharp
public Vector3 PositionTrackInterpolate(int trackIdx, double timeSec)
```

引擎名：`position_track_interpolate`

Returns the interpolated position value at the given time (in seconds). The `trackIdx` must be the index of a 3D position track.

### PositionTrackInterpolate

```csharp
public Vector3 PositionTrackInterpolate(int trackIdx, double timeSec, bool backward = false)
```

引擎名：`position_track_interpolate`

Returns the interpolated position value at the given time (in seconds). The `trackIdx` must be the index of a 3D position track.

### RemoveMarker

```csharp
public void RemoveMarker(StringName name)
```

引擎名：`remove_marker`

Removes the marker with the given name from this Animation.

### RemoveTrack

```csharp
public void RemoveTrack(int trackIdx)
```

引擎名：`remove_track`

Removes a track by specifying the track index.

### RotationTrackInsertKey

```csharp
public int RotationTrackInsertKey(int trackIdx, double time, Quaternion rotation)
```

引擎名：`rotation_track_insert_key`

Inserts a key in a given 3D rotation track. Returns the key index.

### RotationTrackInterpolate

```csharp
public Quaternion RotationTrackInterpolate(int trackIdx, double timeSec)
```

引擎名：`rotation_track_interpolate`

Returns the interpolated rotation value at the given time (in seconds). The `trackIdx` must be the index of a 3D rotation track.

### RotationTrackInterpolate

```csharp
public Quaternion RotationTrackInterpolate(int trackIdx, double timeSec, bool backward = false)
```

引擎名：`rotation_track_interpolate`

Returns the interpolated rotation value at the given time (in seconds). The `trackIdx` must be the index of a 3D rotation track.

### ScaleTrackInsertKey

```csharp
public int ScaleTrackInsertKey(int trackIdx, double time, Vector3 scale)
```

引擎名：`scale_track_insert_key`

Inserts a key in a given 3D scale track. Returns the key index.

### ScaleTrackInterpolate

```csharp
public Vector3 ScaleTrackInterpolate(int trackIdx, double timeSec)
```

引擎名：`scale_track_interpolate`

Returns the interpolated scale value at the given time (in seconds). The `trackIdx` must be the index of a 3D scale track.

### ScaleTrackInterpolate

```csharp
public Vector3 ScaleTrackInterpolate(int trackIdx, double timeSec, bool backward = false)
```

引擎名：`scale_track_interpolate`

Returns the interpolated scale value at the given time (in seconds). The `trackIdx` must be the index of a 3D scale track.

### SetLength

```csharp
public void SetLength(double timeSec)
```

引擎名：`set_length`

### SetLoopMode

```csharp
public void SetLoopMode(Animation.LoopModeEnum loopMode)
```

引擎名：`set_loop_mode`

### SetMarkerColor

```csharp
public void SetMarkerColor(StringName name, Color color)
```

引擎名：`set_marker_color`

Sets the given marker's color.

### SetStep

```csharp
public void SetStep(float sizeSec)
```

引擎名：`set_step`

### TrackFindKey

```csharp
public int TrackFindKey(int trackIdx, double time, Animation.FindMode findMode = Animation.FindMode.Nearest, bool limit = false, bool backward = false)
```

引擎名：`track_find_key`

Finds the key index by time in a given track. Optionally, only find it if the approx/exact time is given.

If `limit` is `true`, it does not return keys outside the animation range.

If `backward` is `true`, the direction is reversed in methods that rely on one directional processing.

For example, in case `findMode` is [`Animation.FindMode.Nearest`](Animation.md), if there is no key in the current position just after seeked, the first key found is retrieved by searching before the position, but if `backward` is `true`, the first key found is retrieved after the position.

### TrackFindKey

```csharp
public int TrackFindKey(int trackIdx, double time, Animation.FindMode findMode)
```

引擎名：`track_find_key`

Finds the key index by time in a given track. Optionally, only find it if the approx/exact time is given.

If `limit` is `true`, it does not return keys outside the animation range.

If `backward` is `true`, the direction is reversed in methods that rely on one directional processing.

For example, in case `findMode` is [`Animation.FindMode.Nearest`](Animation.md), if there is no key in the current position just after seeked, the first key found is retrieved by searching before the position, but if `backward` is `true`, the first key found is retrieved after the position.

### TrackGetInterpolationLoopWrap

```csharp
public bool TrackGetInterpolationLoopWrap(int trackIdx)
```

引擎名：`track_get_interpolation_loop_wrap`

Returns `true` if the track at `trackIdx` wraps the interpolation loop. New tracks wrap the interpolation loop by default.

### TrackGetInterpolationType

```csharp
public Animation.InterpolationType TrackGetInterpolationType(int trackIdx)
```

引擎名：`track_get_interpolation_type`

Returns the interpolation type of a given track.

### TrackGetKeyCount

```csharp
public int TrackGetKeyCount(int trackIdx)
```

引擎名：`track_get_key_count`

Returns the number of keys in a given track.

### TrackGetKeyTime

```csharp
public double TrackGetKeyTime(int trackIdx, int keyIdx)
```

引擎名：`track_get_key_time`

Returns the time at which the key is located.

### TrackGetKeyTransition

```csharp
public float TrackGetKeyTransition(int trackIdx, int keyIdx)
```

引擎名：`track_get_key_transition`

Returns the transition curve (easing) for a specific key (see the built-in math function `Mathf.Ease`).

### TrackGetKeyValue

```csharp
public Variant TrackGetKeyValue(int trackIdx, int keyIdx)
```

引擎名：`track_get_key_value`

Returns the value of a given key in a given track.

### TrackGetPath

```csharp
public NodePath TrackGetPath(int trackIdx)
```

引擎名：`track_get_path`

Gets the path of a track. For more information on the path format, see `TrackSetPath()`.

### TrackGetType

```csharp
public Animation.TrackType TrackGetType(int trackIdx)
```

引擎名：`track_get_type`

Gets the type of a track.

### TrackInsertKey

```csharp
public int TrackInsertKey(int trackIdx, double time, Variant key, float transition = 1f)
```

引擎名：`track_insert_key`

Inserts a generic key in a given track. Returns the key index.

### TrackIsCompressed

```csharp
public bool TrackIsCompressed(int trackIdx)
```

引擎名：`track_is_compressed`

Returns `true` if the track is compressed, `false` otherwise. See also `Compress()`.

### TrackIsEnabled

```csharp
public bool TrackIsEnabled(int trackIdx)
```

引擎名：`track_is_enabled`

Returns `true` if the track at index `trackIdx` is enabled.

### TrackIsImported

```csharp
public bool TrackIsImported(int trackIdx)
```

引擎名：`track_is_imported`

Returns `true` if the given track is imported. Else, return `false`.

### TrackMoveDown

```csharp
public void TrackMoveDown(int trackIdx)
```

引擎名：`track_move_down`

Moves a track down.

### TrackMoveTo

```csharp
public void TrackMoveTo(int trackIdx, int toIdx)
```

引擎名：`track_move_to`

Changes the index position of track `trackIdx` to the one defined in `toIdx`.

### TrackMoveUp

```csharp
public void TrackMoveUp(int trackIdx)
```

引擎名：`track_move_up`

Moves a track up.

### TrackRemoveKey

```csharp
public void TrackRemoveKey(int trackIdx, int keyIdx)
```

引擎名：`track_remove_key`

Removes a key by index in a given track.

### TrackRemoveKeyAtTime

```csharp
public void TrackRemoveKeyAtTime(int trackIdx, double time)
```

引擎名：`track_remove_key_at_time`

Removes a key at `time` in a given track.

### TrackSetEnabled

```csharp
public void TrackSetEnabled(int trackIdx, bool enabled)
```

引擎名：`track_set_enabled`

Enables/disables the given track. Tracks are enabled by default.

### TrackSetImported

```csharp
public void TrackSetImported(int trackIdx, bool imported)
```

引擎名：`track_set_imported`

Sets the given track as imported or not.

### TrackSetInterpolationLoopWrap

```csharp
public void TrackSetInterpolationLoopWrap(int trackIdx, bool interpolation)
```

引擎名：`track_set_interpolation_loop_wrap`

If `true`, the track at `trackIdx` wraps the interpolation loop.

### TrackSetInterpolationType

```csharp
public void TrackSetInterpolationType(int trackIdx, Animation.InterpolationType interpolation)
```

引擎名：`track_set_interpolation_type`

Sets the interpolation type of a given track.

### TrackSetKeyTime

```csharp
public void TrackSetKeyTime(int trackIdx, int keyIdx, double time)
```

引擎名：`track_set_key_time`

Sets the time of an existing key.

### TrackSetKeyTransition

```csharp
public void TrackSetKeyTransition(int trackIdx, int keyIdx, float transition)
```

引擎名：`track_set_key_transition`

Sets the transition curve (easing) for a specific key (see the built-in math function `Mathf.Ease`).

### TrackSetKeyValue

```csharp
public void TrackSetKeyValue(int trackIdx, int key, Variant value)
```

引擎名：`track_set_key_value`

Sets the value of an existing key.

### TrackSetPath

```csharp
public void TrackSetPath(int trackIdx, NodePath path)
```

引擎名：`track_set_path`

Sets the path of a track. Paths must be valid scene-tree paths to a node and must be specified starting from the [`AnimationMixer.RootNode`](AnimationMixer.md) that will reproduce the animation. Tracks that control properties or bones must append their name after the path, separated by `":"`.

For example, `"character/skeleton:ankle"` or `"character/mesh:transform/local"`.

### TrackSwap

```csharp
public void TrackSwap(int trackIdx, int withIdx)
```

引擎名：`track_swap`

Swaps the track `trackIdx`'s index position with the track `withIdx`.

### ValueTrackGetUpdateMode

```csharp
public Animation.UpdateMode ValueTrackGetUpdateMode(int trackIdx)
```

引擎名：`value_track_get_update_mode`

Returns the update mode of a value track.

### ValueTrackInterpolate

```csharp
public Variant ValueTrackInterpolate(int trackIdx, double timeSec)
```

引擎名：`value_track_interpolate`

Returns the interpolated value at the given time (in seconds). The `trackIdx` must be the index of a value track.

A `backward` mainly affects the direction of key retrieval of the track with [`Animation.UpdateMode.Discrete`](Animation.md) converted by [`AnimationMixer.AnimationCallbackModeDiscrete.ForceContinuous`](AnimationMixer.md) to match the result with `TrackFindKey()`.

### ValueTrackInterpolate

```csharp
public Variant ValueTrackInterpolate(int trackIdx, double timeSec, bool backward = false)
```

引擎名：`value_track_interpolate`

Returns the interpolated value at the given time (in seconds). The `trackIdx` must be the index of a value track.

A `backward` mainly affects the direction of key retrieval of the track with [`Animation.UpdateMode.Discrete`](Animation.md) converted by [`AnimationMixer.AnimationCallbackModeDiscrete.ForceContinuous`](AnimationMixer.md) to match the result with `TrackFindKey()`.

### ValueTrackSetUpdateMode

```csharp
public void ValueTrackSetUpdateMode(int trackIdx, Animation.UpdateMode mode)
```

引擎名：`value_track_set_update_mode`

Sets the update mode of a value track.

## FindMode

命名空间 `Godot`；enum

### 枚举值

- `Nearest = 0`：Finds the nearest time key.
- `Approx = 1`：Finds only the key with approximating the time.
- `Exact = 2`：Finds only the key with matching the time.

## InterpolationType

命名空间 `Godot`；enum

### 枚举值

- `Nearest = 0`：No interpolation (nearest value).
- `Linear = 1`：Linear interpolation.
- `Cubic = 2`：Cubic interpolation.
- `LinearAngle = 3`：Linear interpolation with shortest path rotation.
- `CubicAngle = 4`：Cubic interpolation with shortest path rotation.

## LoopModeEnum

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：At both ends of the animation, the animation will stop playing.
- `Linear = 1`：At both ends of the animation, the animation will be repeated without changing the playback direction.
- `Pingpong = 2`：Repeats playback and reverse playback at both ends of the animation.

## LoopedFlag

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：This flag indicates that the animation proceeds without any looping.
- `End = 1`：This flag indicates that the animation has reached the end of the animation and just after loop processed.
- `Start = 2`：This flag indicates that the animation has reached the start of the animation and just after loop processed.

## TrackType

命名空间 `Godot`；enum

### 枚举值

- `Value = 0`：Value tracks set values in node properties, but only those which can be interpolated.
- `Position3D = 1`：3D position track (values are stored in [`Vector3`](Vector3.md)s).
- `Rotation3D = 2`：3D rotation track (values are stored in [`Quaternion`](Quaternion.md)s).
- `Scale3D = 3`：3D scale track (values are stored in [`Vector3`](Vector3.md)s).
- `BlendShape = 4`：Blend shape track.
- `Method = 5`：Method tracks call functions with given arguments per key.
- `Bezier = 6`：Bezier tracks are used to interpolate a value using custom curves.
- `Audio = 7`：Audio tracks are used to play an audio stream with either type of [`AudioStreamPlayer`](AudioStreamPlayer.md).
- `Animation = 8`：Animation tracks play animations in other [`AnimationPlayer`](AnimationPlayer.md) nodes.

## UpdateMode

命名空间 `Godot`；enum

### 枚举值

- `Continuous = 0`：Update between keyframes and hold the value.
- `Discrete = 1`：Update at the keyframes.
- `Capture = 2`：Same as `Continuous` but works as a flag to capture the value of the current object and perform interpolation in some methods.
