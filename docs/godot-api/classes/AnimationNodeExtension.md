<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AnimationNodeExtension

命名空间 `Godot`；class；继承 [`AnimationNode`](AnimationNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`AnimationNodeExtension` exposes the APIs of [`AnimationRootNode`](AnimationRootNode.md) to allow users to extend it from GDScript, C#, or C++. This class is not meant to be used directly, but to be extended by other classes. It is used to create custom nodes for the [`AnimationTree`](AnimationTree.md) system.

## 构造函数

### AnimationNodeExtension

```csharp
public AnimationNodeExtension()
```

## 方法

### GetRemainingTime

```csharp
public static double GetRemainingTime(ReadOnlySpan<float> nodeInfo, bool breakLoop)
```

引擎名：`get_remaining_time`

Returns the animation's remaining time for the given node info. For looping animations, it will only return the remaining time if `breakLoop` is `true`, a large integer value will be returned otherwise.

### GetRemainingTime

```csharp
public static double GetRemainingTime(float[] nodeInfo, bool breakLoop)
```

引擎名：`get_remaining_time`

Returns the animation's remaining time for the given node info. For looping animations, it will only return the remaining time if `breakLoop` is `true`, a large integer value will be returned otherwise.

### IsLooping

```csharp
public static bool IsLooping(ReadOnlySpan<float> nodeInfo)
```

引擎名：`is_looping`

Returns `true` if the animation for the given `nodeInfo` is looping.

### IsLooping

```csharp
public static bool IsLooping(float[] nodeInfo)
```

引擎名：`is_looping`

Returns `true` if the animation for the given `nodeInfo` is looping.

### _ProcessAnimationNode

```csharp
public virtual float[] _ProcessAnimationNode(double[] playbackInfo, bool testOnly)
```

引擎名：`_process_animation_node`

A version of the [`AnimationNode._Process()`](AnimationNode.md) method that is meant to be overridden by custom nodes. It returns a `System.Single`[] with the processed animation data.

The `System.Double`[] parameter contains the playback information, containing the following values encoded as floating point numbers (in order): playback time and delta, start and end times, whether a seek was requested (encoded as a float greater than `0`), whether the seek request was externally requested (encoded as a float greater than `0`), the current [`Animation.LoopedFlag`](Animation.md) (encoded as a float), and the current blend weight.

The function must return a `System.Single`[] of the node's time info, containing the following values (in order): animation length, time position, delta, [`Animation.LoopModeEnum`](Animation.md) (encoded as a float), whether the animation is about to end (encoded as a float greater than `0`) and whether the animation is infinite (encoded as a float greater than `0`). All values must be included in the returned array.
