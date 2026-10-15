<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Callable

命名空间 `Godot`；struct

Callable is a first class object which can be held in variables and passed to functions.
It represents a given method in an Object, and is typically used for signal callbacks.

## 构造函数

### Callable

```csharp
public Callable(GodotObject target, StringName method)
```

Constructs a new `Callable` for the method called `method`
in the specified `target`.

参数：
- `target`：Object that contains the method.
- `method`：Name of the method that will be called.

## 属性

### Delegate

```csharp
public Delegate Delegate { get; }
```

Delegate of the method that will be called.

### Method

```csharp
public StringName Method { get; }
```

Name of the method that will be called.

### Target

```csharp
public GodotObject Target { get; }
```

Object that contains the method.

### Trampoline

```csharp
public  Trampoline { get; }
```

Trampoline function pointer for dynamically invoking `Delegate`.

## 方法

### Call

```csharp
public Variant Call(ReadOnlySpan<Variant> args)
```

引擎名：`call`

Calls the method represented by this `Callable`.
Arguments can be passed and should match the method's signature.

参数：
- `args`：Arguments that will be passed to the method call.

返回：The value returned by the method.

### Call

```csharp
public Variant Call(params Variant[] args)
```

引擎名：`call`

Calls the method represented by this `Callable`.
Arguments can be passed and should match the method's signature.

参数：
- `args`：Arguments that will be passed to the method call.

返回：The value returned by the method.

### CallDeferred

```csharp
public void CallDeferred(ReadOnlySpan<Variant> args)
```

引擎名：`call_deferred`

Calls the method represented by this `Callable` in deferred mode, i.e. during the idle frame.
Arguments can be passed and should match the method's signature.

参数：
- `args`：Arguments that will be passed to the method call.

### CallDeferred

```csharp
public void CallDeferred(params Variant[] args)
```

引擎名：`call_deferred`

Calls the method represented by this `Callable` in deferred mode, i.e. during the idle frame.
Arguments can be passed and should match the method's signature.

参数：
- `args`：Arguments that will be passed to the method call.

### CreateWithUnsafeTrampoline

```csharp
public static Callable CreateWithUnsafeTrampoline(Delegate delegate,  trampoline)
```

Constructs a new `Callable` using the `trampoline`
function pointer to dynamically invoke the given `delegate`.

The parameters passed to the `trampoline` function are:

- delegateObj
The given `delegate`, upcast to `System.Object`.
- args
Array of `NativeInterop.godot_variant` arguments.
- ret
Return value of type `NativeInterop.godot_variant`.

The delegate should be downcast to a more specific delegate type before invoking.

参数：
- `delegate`：Delegate method that will be called.
- `trampoline`：Trampoline function pointer for invoking the delegate.

### From

```csharp
public static Callable From(Action action)
```

Constructs a new `Callable` for the given `action`.

参数：
- `action`：Action method that will be called.

### From

```csharp
public static Callable From<T0, T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, TResult> func)
```

Constructs a new `Callable` for the given `func`.

参数：
- `func`：Action method that will be called.

### From

```csharp
public static Callable From<T0, T1, T2, T3, T4, T5, T6, T7, T8>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8> action)
```

Constructs a new `Callable` for the given `action`.

参数：
- `action`：Action method that will be called.

### From

```csharp
public static Callable From<T0, T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T0, T1, T2, T3, T4, T5, T6, T7, TResult> func)
```

Constructs a new `Callable` for the given `func`.

参数：
- `func`：Action method that will be called.

### From

```csharp
public static Callable From<T0, T1, T2, T3, T4, T5, T6, T7>(Action<T0, T1, T2, T3, T4, T5, T6, T7> action)
```

Constructs a new `Callable` for the given `action`.

参数：
- `action`：Action method that will be called.

### From

```csharp
public static Callable From<T0, T1, T2, T3, T4, T5, T6, TResult>(Func<T0, T1, T2, T3, T4, T5, T6, TResult> func)
```

Constructs a new `Callable` for the given `func`.

参数：
- `func`：Action method that will be called.

### From

```csharp
public static Callable From<T0, T1, T2, T3, T4, T5, T6>(Action<T0, T1, T2, T3, T4, T5, T6> action)
```

Constructs a new `Callable` for the given `action`.

参数：
- `action`：Action method that will be called.

### From

```csharp
public static Callable From<T0, T1, T2, T3, T4, T5, TResult>(Func<T0, T1, T2, T3, T4, T5, TResult> func)
```

Constructs a new `Callable` for the given `func`.

参数：
- `func`：Action method that will be called.

### From

```csharp
public static Callable From<T0, T1, T2, T3, T4, T5>(Action<T0, T1, T2, T3, T4, T5> action)
```

Constructs a new `Callable` for the given `action`.

参数：
- `action`：Action method that will be called.

### From

```csharp
public static Callable From<T0, T1, T2, T3, T4, TResult>(Func<T0, T1, T2, T3, T4, TResult> func)
```

Constructs a new `Callable` for the given `func`.

参数：
- `func`：Action method that will be called.

### From

```csharp
public static Callable From<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> action)
```

Constructs a new `Callable` for the given `action`.

参数：
- `action`：Action method that will be called.

### From

```csharp
public static Callable From<T0, T1, T2, T3, TResult>(Func<T0, T1, T2, T3, TResult> func)
```

Constructs a new `Callable` for the given `func`.

参数：
- `func`：Action method that will be called.

### From

```csharp
public static Callable From<T0, T1, T2, T3>(Action<T0, T1, T2, T3> action)
```

Constructs a new `Callable` for the given `action`.

参数：
- `action`：Action method that will be called.

### From

```csharp
public static Callable From<T0, T1, T2, TResult>(Func<T0, T1, T2, TResult> func)
```

Constructs a new `Callable` for the given `func`.

参数：
- `func`：Action method that will be called.

### From

```csharp
public static Callable From<T0, T1, T2>(Action<T0, T1, T2> action)
```

Constructs a new `Callable` for the given `action`.

参数：
- `action`：Action method that will be called.

### From

```csharp
public static Callable From<T0, T1, TResult>(Func<T0, T1, TResult> func)
```

Constructs a new `Callable` for the given `func`.

参数：
- `func`：Action method that will be called.

### From

```csharp
public static Callable From<T0, T1>(Action<T0, T1> action)
```

Constructs a new `Callable` for the given `action`.

参数：
- `action`：Action method that will be called.

### From

```csharp
public static Callable From<T0, TResult>(Func<T0, TResult> func)
```

Constructs a new `Callable` for the given `func`.

参数：
- `func`：Action method that will be called.

### From

```csharp
public static Callable From<T0>(Action<T0> action)
```

Constructs a new `Callable` for the given `action`.

参数：
- `action`：Action method that will be called.

### From

```csharp
public static Callable From<TResult>(Func<TResult> func)
```

Constructs a new `Callable` for the given `func`.

参数：
- `func`：Action method that will be called.
