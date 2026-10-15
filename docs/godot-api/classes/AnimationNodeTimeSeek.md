<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AnimationNodeTimeSeek

命名空间 `Godot`；class；继承 [`AnimationNode`](AnimationNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This animation node can be used to cause a seek command to happen to any sub-children of the animation graph. Use to play an [`Animation`](Animation.md) from the start or a certain playback position inside the [`AnimationNodeBlendTree`](AnimationNodeBlendTree.md).

After setting the time and changing the animation playback, the time seek node automatically goes into sleep mode on the next process frame by setting its `seek_request` value to `-1.0`.

```csharp
// Play child animation from the start.
animationTree.Set("parameters/TimeSeek/seek_request", 0.0);

// Play child animation from 12 second timestamp.
animationTree.Set("parameters/TimeSeek/seek_request", 12.0);

```

## 构造函数

### AnimationNodeTimeSeek

```csharp
public AnimationNodeTimeSeek()
```

## 属性

### ExplicitElapse

```csharp
public bool ExplicitElapse { get; set; }
```

引擎名：`explicit_elapse`

If `true`, some processes are executed to handle keys between seeks, such as calculating root motion and finding the nearest discrete key.

## 方法

### IsExplicitElapse

```csharp
public bool IsExplicitElapse()
```

引擎名：`is_explicit_elapse`

### SetExplicitElapse

```csharp
public void SetExplicitElapse(bool enable)
```

引擎名：`set_explicit_elapse`
