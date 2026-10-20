<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Range

命名空间 `Godot`；class；继承 [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Range is an abstract base class for controls that represent a number within a range, using a configured `Step` and `Page` size. See e.g. [`ScrollBar`](ScrollBar.md) and [`Slider`](Slider.md) for examples of higher-level nodes using Range.

## 构造函数

### Range

```csharp
public Range()
```

## 属性

### AllowGreater

```csharp
public bool AllowGreater { get; set; }
```

引擎名：`allow_greater`

If `true`, `Value` may be greater than `MaxValue`.

### AllowLesser

```csharp
public bool AllowLesser { get; set; }
```

引擎名：`allow_lesser`

If `true`, `Value` may be less than `MinValue`.

### ExpEdit

```csharp
public bool ExpEdit { get; set; }
```

引擎名：`exp_edit`

If `true`, and `MinValue` is greater or equal to `0`, `Value` will be represented exponentially rather than linearly.

### MaxValue

```csharp
public double MaxValue { get; set; }
```

引擎名：`max_value`

Maximum value. Range is clamped if `Value` is greater than `MaxValue`.

### MinValue

```csharp
public double MinValue { get; set; }
```

引擎名：`min_value`

Minimum value. Range is clamped if `Value` is less than `MinValue`.

### Page

```csharp
public double Page { get; set; }
```

引擎名：`page`

Page size. Used mainly for [`ScrollBar`](ScrollBar.md). A [`ScrollBar`](ScrollBar.md)'s grabber length is the [`ScrollBar`](ScrollBar.md)'s size multiplied by `Page` over the difference between `MinValue` and `MaxValue`.

### Ratio

```csharp
public double Ratio { get; set; }
```

引擎名：`ratio`

The value mapped between 0 and 1.

### Rounded

```csharp
public bool Rounded { get; set; }
```

引擎名：`rounded`

If `true`, `Value` will always be rounded to the nearest integer.

### Step

```csharp
public double Step { get; set; }
```

引擎名：`step`

If greater than `0.0`, `Value` will always be rounded to a multiple of this property's value above `MinValue`. For example, if `MinValue` is `0.1` and step is `0.2`, then `Value` is limited to `0.1`, `0.3`, `0.5`, and so on. If `Rounded` is also `true`, `Value` will first be rounded to a multiple of this property's value, then rounded to the nearest integer.

### Value

```csharp
public double Value { get; set; }
```

引擎名：`value`

Range's current value. Changing this property (even via code) will trigger `ValueChanged` signal. Use `SetValueNoSignal()` if you want to avoid it.

## 方法

### GetAsRatio

```csharp
public double GetAsRatio()
```

引擎名：`get_as_ratio`

### GetMax

```csharp
public double GetMax()
```

引擎名：`get_max`

### GetMin

```csharp
public double GetMin()
```

引擎名：`get_min`

### GetPage

```csharp
public double GetPage()
```

引擎名：`get_page`

### GetStep

```csharp
public double GetStep()
```

引擎名：`get_step`

### GetValue

```csharp
public double GetValue()
```

引擎名：`get_value`

### IsGreaterAllowed

```csharp
public bool IsGreaterAllowed()
```

引擎名：`is_greater_allowed`

### IsLesserAllowed

```csharp
public bool IsLesserAllowed()
```

引擎名：`is_lesser_allowed`

### IsRatioExp

```csharp
public bool IsRatioExp()
```

引擎名：`is_ratio_exp`

### IsUsingRoundedValues

```csharp
public bool IsUsingRoundedValues()
```

引擎名：`is_using_rounded_values`

### SetAllowGreater

```csharp
public void SetAllowGreater(bool allow)
```

引擎名：`set_allow_greater`

### SetAllowLesser

```csharp
public void SetAllowLesser(bool allow)
```

引擎名：`set_allow_lesser`

### SetAsRatio

```csharp
public void SetAsRatio(double value)
```

引擎名：`set_as_ratio`

### SetExpRatio

```csharp
public void SetExpRatio(bool enabled)
```

引擎名：`set_exp_ratio`

### SetMax

```csharp
public void SetMax(double maximum)
```

引擎名：`set_max`

### SetMin

```csharp
public void SetMin(double minimum)
```

引擎名：`set_min`

### SetPage

```csharp
public void SetPage(double pagesize)
```

引擎名：`set_page`

### SetStep

```csharp
public void SetStep(double step)
```

引擎名：`set_step`

### SetUseRoundedValues

```csharp
public void SetUseRoundedValues(bool enabled)
```

引擎名：`set_use_rounded_values`

### SetValue

```csharp
public void SetValue(double value)
```

引擎名：`set_value`

### SetValueNoSignal

```csharp
public void SetValueNoSignal(double value)
```

引擎名：`set_value_no_signal`

Sets the `Range`'s current value to the specified `value`, without emitting the `ValueChanged` signal.

### Share

```csharp
public void Share(Node with)
```

引擎名：`share`

Binds two `Range`s together along with any ranges previously grouped with either of them. When any of range's member variables change, it will share the new value with all other ranges in its group.

### Unshare

```csharp
public void Unshare()
```

引擎名：`unshare`

Stops the `Range` from sharing its member variables with any other.

### _ValueChanged

```csharp
public virtual void _ValueChanged(double newValue)
```

引擎名：`_value_changed`

Called when the `Range`'s value is changed (following the same conditions as `ValueChanged`).

## 事件（信号）

### Changed

```csharp
public event Action Changed
```

引擎名：`changed`

Emitted when `MinValue`, `MaxValue`, `Page`, or `Step` change.

### ValueChanged

```csharp
public event Range.ValueChangedEventHandler ValueChanged
```

引擎名：`value_changed`

Emitted when `Value` changes. When used on a [`Slider`](Slider.md), this is called continuously while dragging (potentially every frame). If you are performing an expensive operation in a function connected to `ValueChanged`, consider using a *debouncing* [`Timer`](Timer.md) to call the function less often.

**Note:** Unlike signals such as [`LineEdit.TextChanged`](LineEdit.md), `ValueChanged` is also emitted when `value` is set directly via code.

## ValueChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Range.ValueChanged`](Range.md) event of a [`Range`](Range.md) class.

### 签名

#### Invoke

```csharp
delegate void ValueChangedEventHandler(double value)
```
