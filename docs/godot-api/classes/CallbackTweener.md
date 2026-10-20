<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CallbackTweener

命名空间 `Godot`；class；继承 [`Tweener`](Tweener.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`CallbackTweener` is used to call a method in a tweening sequence. See [`Tween.TweenCallback`](Tween.md) for more usage information.

The tweener will finish automatically if the callback's target object is freed.

**Note:** [`Tween.TweenCallback`](Tween.md) is the only correct way to create `CallbackTweener`. Any `CallbackTweener` created manually will not function correctly.

## 构造函数

### CallbackTweener

```csharp
public CallbackTweener()
```

## 方法

### SetDelay

```csharp
public CallbackTweener SetDelay(double delay)
```

引擎名：`set_delay`

Makes the callback call delayed by given time in seconds.

**Example:** Call [`Node.QueueFree`](Node.md) after 2 seconds:

```csharp
var tween = get_tree().create_tween()
tween.tween_callback(queue_free).set_delay(2)

```
