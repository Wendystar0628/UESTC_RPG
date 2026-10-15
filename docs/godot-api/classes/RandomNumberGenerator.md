<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RandomNumberGenerator

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

RandomNumberGenerator is a class for generating pseudo-random numbers. It currently uses [PCG32](https://www.pcg-random.org/).

**Note:** The underlying algorithm is an implementation detail and should not be depended upon.

To generate a random float number (within a given range) based on a time-dependent seed:

```csharp
var rng = RandomNumberGenerator.new()
func _ready():
var my_random_number = rng.randf_range(-10.0, 10.0)

```

## 构造函数

### RandomNumberGenerator

```csharp
public RandomNumberGenerator()
```

## 属性

### Seed

```csharp
public ulong Seed { get; set; }
```

引擎名：`seed`

Initializes the random number generator state based on the given seed value. A given seed will give a reproducible sequence of pseudo-random numbers.

**Note:** The RNG does not have an avalanche effect, and can output similar random streams given similar seeds. Consider using a hash function to improve your seed quality if they're sourced externally.

**Note:** The default value of this property is pseudo-random, and changes when calling `Randomize()`. The `0` value documented here is a placeholder, and not the actual default seed.

**Note:** Setting this property produces a side effect of changing the internal `State`, so make sure to initialize the seed *before* modifying the `State`:

```csharp
var rng = RandomNumberGenerator.new()
rng.seed = hash("Godot")
rng.state = 100 # Restore to some previously saved state.

```

### State

```csharp
public ulong State { get; set; }
```

引擎名：`state`

The current state of the random number generator. Save and restore this property to restore the generator to a previous state:

```csharp
var rng = RandomNumberGenerator.new()
print(rng.randf())
var saved_state = rng.state # Store current state.
print(rng.randf()) # Advance internal state.
rng.state = saved_state # Restore the state.
print(rng.randf()) # Prints the same value as previously.

```

**Note:** Do not set state to arbitrary values, since the random number generator requires the state to have certain qualities to behave properly. It should only be set to values that came from the state property itself. To initialize the random number generator with arbitrary input, use `Seed` instead.

**Note:** The default value of this property is pseudo-random, and changes when calling `Randomize()`. The `0` value documented here is a placeholder, and not the actual default state.

## 方法

### GetSeed

```csharp
public ulong GetSeed()
```

引擎名：`get_seed`

### GetState

```csharp
public ulong GetState()
```

引擎名：`get_state`

### RandWeighted

```csharp
public long RandWeighted(ReadOnlySpan<float> weights)
```

引擎名：`rand_weighted`

Returns a random integer between `0` and the size of the array that is passed as a parameter. Each value in the array should be a non-negative floating-point number that represents the relative likelihood that it will be returned as an index. A higher value means the value is more likely to be returned as an index, while a value of `0` means it will never be returned as an index.

For example, if `[0.5, 1, 1, 2]` is passed as a parameter, then the method is twice as likely to return `3` (the index of the value `2`) and twice as unlikely to return `0` (the index of the value `0.5`) compared to the indices `1` and `2`.

Prints an error and returns `-1` if the array is empty or contains any negative values.

### RandWeighted

```csharp
public long RandWeighted(float[] weights)
```

引擎名：`rand_weighted`

Returns a random integer between `0` and the size of the array that is passed as a parameter. Each value in the array should be a non-negative floating-point number that represents the relative likelihood that it will be returned as an index. A higher value means the value is more likely to be returned as an index, while a value of `0` means it will never be returned as an index.

For example, if `[0.5, 1, 1, 2]` is passed as a parameter, then the method is twice as likely to return `3` (the index of the value `2`) and twice as unlikely to return `0` (the index of the value `0.5`) compared to the indices `1` and `2`.

Prints an error and returns `-1` if the array is empty or contains any negative values.

### Randf

```csharp
public float Randf()
```

引擎名：`randf`

Returns a pseudo-random float between `0.0` and `1.0` (inclusive).

### RandfRange

```csharp
public float RandfRange(float from, float to)
```

引擎名：`randf_range`

Returns a pseudo-random float between `from` and `to` (inclusive).

### Randfn

```csharp
public float Randfn(float mean = 0f, float deviation = 1f)
```

引擎名：`randfn`

Returns a [normally-distributed](https://en.wikipedia.org/wiki/Normal_distribution), pseudo-random floating-point number from the specified `mean` and a standard `deviation`. This is also known as a Gaussian distribution.

**Note:** This method uses the [Box-Muller transform](https://en.wikipedia.org/wiki/Box%E2%80%93Muller_transform) algorithm.

### Randi

```csharp
public uint Randi()
```

引擎名：`randi`

Returns a pseudo-random 32-bit unsigned integer between `0` and `4294967295` (inclusive).

### RandiRange

```csharp
public int RandiRange(int from, int to)
```

引擎名：`randi_range`

Returns a pseudo-random 32-bit signed integer between `from` and `to` (inclusive).

### Randomize

```csharp
public void Randomize()
```

引擎名：`randomize`

Sets up a time-based seed for this `RandomNumberGenerator` instance. Unlike the `@GlobalScope` random number generation functions, different `RandomNumberGenerator` instances can use different seeds.

### SetSeed

```csharp
public void SetSeed(ulong seed)
```

引擎名：`set_seed`

### SetState

```csharp
public void SetState(ulong state)
```

引擎名：`set_state`
