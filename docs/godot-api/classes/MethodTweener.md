<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# MethodTweener

命名空间 `Godot`；class；继承 [`Tweener`](Tweener.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`MethodTweener` is similar to a combination of [`CallbackTweener`](CallbackTweener.md) and [`PropertyTweener`](PropertyTweener.md). It calls a method providing an interpolated value as a parameter. See [`Tween.TweenMethod`](Tween.md) for more usage information.

The tweener will finish automatically if the callback's target object is freed.

**Note:** [`Tween.TweenMethod`](Tween.md) is the only correct way to create `MethodTweener`. Any `MethodTweener` created manually will not function correctly.

## 构造函数

### MethodTweener

```csharp
public MethodTweener()
```

## 方法

### SetDelay

```csharp
public MethodTweener SetDelay(double delay)
```

引擎名：`set_delay`

Sets the time in seconds after which the `MethodTweener` will start interpolating. By default there's no delay.

### SetEase

```csharp
public MethodTweener SetEase(Tween.EaseType ease)
```

引擎名：`set_ease`

Sets the type of used easing from [`Tween.EaseType`](Tween.md). If not set, the default easing is used from the [`Tween`](Tween.md) that contains this Tweener.

### SetTrans

```csharp
public MethodTweener SetTrans(Tween.TransitionType trans)
```

引擎名：`set_trans`

Sets the type of used transition from [`Tween.TransitionType`](Tween.md). If not set, the default transition is used from the [`Tween`](Tween.md) that contains this Tweener.
