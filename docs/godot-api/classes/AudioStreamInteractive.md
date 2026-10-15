<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AudioStreamInteractive

命名空间 `Godot`；class；继承 [`AudioStream`](AudioStream.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This is an audio stream that can playback music interactively, combining clips and a transition table. Clips must be added first, and then the transition rules via the `AddTransition()`. Additionally, this stream exports a property parameter to control the playback via [`AudioStreamPlayer`](AudioStreamPlayer.md), [`AudioStreamPlayer2D`](AudioStreamPlayer2D.md), or [`AudioStreamPlayer3D`](AudioStreamPlayer3D.md).

The way this is used is by filling a number of clips, then configuring the transition table. From there, clips are selected for playback and the music will smoothly go from the current to the new one while using the corresponding transition rule defined in the transition table.

## 构造函数

### AudioStreamInteractive

```csharp
public AudioStreamInteractive()
```

## 属性

### ClipCount

```csharp
public int ClipCount { get; set; }
```

引擎名：`clip_count`

Amount of clips contained in this interactive player.

### InitialClip

```csharp
public int InitialClip { get; set; }
```

引擎名：`initial_clip`

Index of the initial clip, which will be played first when this stream is played.

### _Transitions

```csharp
public Dictionary _Transitions { get; set; }
```

## 方法

### AddTransition

```csharp
public void AddTransition(int fromClip, int toClip, AudioStreamInteractive.TransitionFromTime fromTime, AudioStreamInteractive.TransitionToTime toTime, AudioStreamInteractive.FadeMode fadeMode, float fadeBeats, bool useFillerClip = false, int fillerClip = -1, bool holdPrevious = false)
```

引擎名：`add_transition`

Add a transition between two clips. Provide the indices of the source and destination clips, or use the `ClipAny` constant to indicate that transition happens to/from any clip to this one.

* `fromTime` indicates the moment in the current clip the transition will begin after triggered.

* `toTime` indicates the time in the next clip that the playback will start from.

* `fadeMode` indicates how the fade will happen between clips. If unsure, just use [`AudioStreamInteractive.FadeMode.Automatic`](AudioStreamInteractive.md) which uses the most common type of fade for each situation.

* `fadeBeats` indicates how many beats the fade will take. Using decimals is allowed.

* `useFillerClip` indicates that there will be a filler clip used between the source and destination clips.

* `fillerClip` the index of the filler clip.

* If `holdPrevious` is used, then this clip will be remembered. This can be used together with [`AudioStreamInteractive.AutoAdvanceMode.ReturnToHold`](AudioStreamInteractive.md) to return to this clip after another is done playing.

### EraseTransition

```csharp
public void EraseTransition(int fromClip, int toClip)
```

引擎名：`erase_transition`

Erase a transition by providing `fromClip` and `toClip` clip indices. `ClipAny` can be used for either argument or both.

### GetClipAutoAdvance

```csharp
public AudioStreamInteractive.AutoAdvanceMode GetClipAutoAdvance(int clipIndex)
```

引擎名：`get_clip_auto_advance`

Return whether a clip has auto-advance enabled. See `SetClipAutoAdvance()`.

### GetClipAutoAdvanceNextClip

```csharp
public int GetClipAutoAdvanceNextClip(int clipIndex)
```

引擎名：`get_clip_auto_advance_next_clip`

Return the clip towards which the clip referenced by `clipIndex` will auto-advance to.

### GetClipCount

```csharp
public int GetClipCount()
```

引擎名：`get_clip_count`

### GetClipName

```csharp
public StringName GetClipName(int clipIndex)
```

引擎名：`get_clip_name`

Return the name of a clip.

### GetClipStream

```csharp
public AudioStream GetClipStream(int clipIndex)
```

引擎名：`get_clip_stream`

Return the [`AudioStream`](AudioStream.md) associated with a clip.

### GetInitialClip

```csharp
public int GetInitialClip()
```

引擎名：`get_initial_clip`

### GetTransitionFadeBeats

```csharp
public float GetTransitionFadeBeats(int fromClip, int toClip)
```

引擎名：`get_transition_fade_beats`

Return the time (in beats) for a transition (see `AddTransition()`).

### GetTransitionFadeMode

```csharp
public AudioStreamInteractive.FadeMode GetTransitionFadeMode(int fromClip, int toClip)
```

引擎名：`get_transition_fade_mode`

Return the mode for a transition (see `AddTransition()`).

### GetTransitionFillerClip

```csharp
public int GetTransitionFillerClip(int fromClip, int toClip)
```

引擎名：`get_transition_filler_clip`

Return the filler clip for a transition (see `AddTransition()`).

### GetTransitionFromTime

```csharp
public AudioStreamInteractive.TransitionFromTime GetTransitionFromTime(int fromClip, int toClip)
```

引擎名：`get_transition_from_time`

Return the source time position for a transition (see `AddTransition()`).

### GetTransitionList

```csharp
public int[] GetTransitionList()
```

引擎名：`get_transition_list`

Return the list of transitions (from, to interleaved).

### GetTransitionToTime

```csharp
public AudioStreamInteractive.TransitionToTime GetTransitionToTime(int fromClip, int toClip)
```

引擎名：`get_transition_to_time`

Return the destination time position for a transition (see `AddTransition()`).

### HasTransition

```csharp
public bool HasTransition(int fromClip, int toClip)
```

引擎名：`has_transition`

Returns `true` if a given transition exists (was added via `AddTransition()`).

### IsTransitionHoldingPrevious

```csharp
public bool IsTransitionHoldingPrevious(int fromClip, int toClip)
```

引擎名：`is_transition_holding_previous`

Return whether a transition uses the *hold previous* functionality (see `AddTransition()`).

### IsTransitionUsingFillerClip

```csharp
public bool IsTransitionUsingFillerClip(int fromClip, int toClip)
```

引擎名：`is_transition_using_filler_clip`

Return whether a transition uses the *filler clip* functionality (see `AddTransition()`).

### SetClipAutoAdvance

```csharp
public void SetClipAutoAdvance(int clipIndex, AudioStreamInteractive.AutoAdvanceMode mode)
```

引擎名：`set_clip_auto_advance`

Set whether a clip will auto-advance by changing the auto-advance mode.

### SetClipAutoAdvanceNextClip

```csharp
public void SetClipAutoAdvanceNextClip(int clipIndex, int autoAdvanceNextClip)
```

引擎名：`set_clip_auto_advance_next_clip`

Set the index of the next clip towards which this clip will auto advance to when finished. If the clip being played loops, then auto-advance will be ignored.

### SetClipCount

```csharp
public void SetClipCount(int clipCount)
```

引擎名：`set_clip_count`

### SetClipName

```csharp
public void SetClipName(int clipIndex, StringName name)
```

引擎名：`set_clip_name`

Set the name of the current clip (for easier identification).

### SetClipStream

```csharp
public void SetClipStream(int clipIndex, AudioStream stream)
```

引擎名：`set_clip_stream`

Set the [`AudioStream`](AudioStream.md) associated with the current clip.

### SetInitialClip

```csharp
public void SetInitialClip(int clipIndex)
```

引擎名：`set_initial_clip`

## 常量

### ClipAny

```csharp
public const long ClipAny = -1
```

This constant describes that any clip is valid for a specific transition as either source or destination.

## AutoAdvanceMode

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Disable auto-advance (default).
- `Enabled = 1`：Enable auto-advance, a clip must be specified.
- `ReturnToHold = 2`：Enable auto-advance, but instead of specifying a clip, the playback will return to hold (see [`AudioStreamInteractive.AddTransition`](AudioStreamInteractive.md)).

## FadeMode

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Do not use fade for the transition.
- `In = 1`：Use a fade-in in the next clip, let the current clip finish.
- `Out = 2`：Use a fade-out in the current clip, the next clip will start by itself.
- `Cross = 3`：Use a cross-fade between clips.
- `Automatic = 4`：Use automatic fade logic depending on the transition from/to.

## TransitionFromTime

命名空间 `Godot`；enum

### 枚举值

- `Immediate = 0`：Start transition as soon as possible, don't wait for any specific time position.
- `NextBeat = 1`：Transition when the clip playback position reaches the next beat.
- `NextBar = 2`：Transition when the clip playback position reaches the next bar.
- `End = 3`：Transition when the current clip finished playing.

## TransitionToTime

命名空间 `Godot`；enum

### 枚举值

- `SamePosition = 0`：Transition to the same position in the destination clip.
- `Start = 1`：Transition to the start of the destination clip.
- `PreviousPosition = 2`：Transition to the last played position in the destination clip, if there was a previous transition from that clip.
