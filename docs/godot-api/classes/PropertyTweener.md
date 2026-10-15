<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PropertyTweener

命名空间 `Godot`；class；继承 [`Tweener`](Tweener.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`PropertyTweener` is used to interpolate a property in an object. See [`Tween.TweenProperty`](Tween.md) for more usage information.

The tweener will finish automatically if the target object is freed.

**Note:** [`Tween.TweenProperty`](Tween.md) is the only correct way to create `PropertyTweener`. Any `PropertyTweener` created manually will not function correctly.

## 构造函数

### PropertyTweener

```csharp
public PropertyTweener()
```

## 方法

### AsRelative

```csharp
public PropertyTweener AsRelative()
```

引擎名：`as_relative`

When called, the final value will be used as a relative value instead.

**Example:** Move the node by `100` pixels to the right.

```csharp
Tween tween = GetTree().CreateTween();
tween.TweenProperty(this, "position", Vector2.Right * 100.0f, 1.0f).AsRelative();

```

### From

```csharp
public PropertyTweener From(Variant value)
```

引擎名：`from`

Sets a custom initial value to the `PropertyTweener`.

**Example:** Move the node from position `(100, 100)` to `(200, 100)`.

```csharp
Tween tween = GetTree().CreateTween();
tween.TweenProperty(this, "position", new Vector2(200.0f, 100.0f), 1.0f).From(new Vector2(100.0f, 100.0f));

```

### FromCurrent

```csharp
public PropertyTweener FromCurrent()
```

引擎名：`from_current`

Makes the `PropertyTweener` use the current property value (i.e. at the time of creating this `PropertyTweener`) as a starting point. This is equivalent of using `From()` with the current value. These two calls will do the same:

```csharp
tween.TweenProperty(this, "position", new Vector2(200.0f, 100.0f), 1.0f).From(Position);
tween.TweenProperty(this, "position", new Vector2(200.0f, 100.0f), 1.0f).FromCurrent();

```

### SetCustomInterpolator

```csharp
public PropertyTweener SetCustomInterpolator(Callable interpolatorMethod)
```

引擎名：`set_custom_interpolator`

Allows interpolating the value with a custom easing function. The provided `interpolatorMethod` will be called with a value ranging from `0.0` to `1.0` and is expected to return a value within the same range (values outside the range can be used for overshoot). The return value of the method is then used for interpolation between initial and final value. Note that the parameter passed to the method is still subject to the tweener's own easing.

```csharp
[Export]
public Curve Curve { get; set; }

public override void _Ready()
{
Tween tween = CreateTween();
// Interpolate the value using a custom curve.
Callable tweenCurveCallable = Callable.From<float, float>(TweenCurve);
tween.TweenProperty(this, "position:x", 300.0f, 1.0f).AsRelative().SetCustomInterpolator(tweenCurveCallable);
}

private float TweenCurve(float value)
{
return Curve.SampleBaked(value);
}

```

### SetDelay

```csharp
public PropertyTweener SetDelay(double delay)
```

引擎名：`set_delay`

Sets the time in seconds after which the `PropertyTweener` will start interpolating. By default there's no delay.

### SetEase

```csharp
public PropertyTweener SetEase(Tween.EaseType ease)
```

引擎名：`set_ease`

Sets the type of used easing from [`Tween.EaseType`](Tween.md). If not set, the default easing is used from the [`Tween`](Tween.md) that contains this Tweener.

### SetTrans

```csharp
public PropertyTweener SetTrans(Tween.TransitionType trans)
```

引擎名：`set_trans`

Sets the type of used transition from [`Tween.TransitionType`](Tween.md). If not set, the default transition is used from the [`Tween`](Tween.md) that contains this Tweener.
