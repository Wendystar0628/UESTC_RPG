<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicsMaterial

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Holds physics-related properties of a surface, namely its roughness and bounciness. This class is used to apply these properties to a physics body.

## 构造函数

### PhysicsMaterial

```csharp
public PhysicsMaterial()
```

## 属性

### Absorbent

```csharp
public bool Absorbent { get; set; }
```

引擎名：`absorbent`

If `true`, subtracts the bounciness from the colliding object's bounciness instead of adding it.

### Bounce

```csharp
public float Bounce { get; set; }
```

引擎名：`bounce`

The body's bounciness. Values range from `0` (no bounce) to `1` (full bounciness).

**Note:** Even with `Bounce` set to `1.0`, some energy will be lost over time due to linear and angular damping. To have a physics body that preserves all its energy over time, set `Bounce` to `1.0`, the body's linear damp mode to **Replace** (if applicable), its linear damp to `0.0`, its angular damp mode to **Replace** (if applicable), and its angular damp to `0.0`.

### Friction

```csharp
public float Friction { get; set; }
```

引擎名：`friction`

The body's friction. Values range from `0` (frictionless) to `1` (maximum friction).

### Rough

```csharp
public bool Rough { get; set; }
```

引擎名：`rough`

If `true`, the physics engine will use the friction of the object marked as "rough" when two objects collide. If `false`, the physics engine will use the lowest friction of all colliding objects instead. If `true` for both colliding objects, the physics engine will use the highest friction.

## 方法

### GetBounce

```csharp
public float GetBounce()
```

引擎名：`get_bounce`

### GetFriction

```csharp
public float GetFriction()
```

引擎名：`get_friction`

### IsAbsorbent

```csharp
public bool IsAbsorbent()
```

引擎名：`is_absorbent`

### IsRough

```csharp
public bool IsRough()
```

引擎名：`is_rough`

### SetAbsorbent

```csharp
public void SetAbsorbent(bool absorbent)
```

引擎名：`set_absorbent`

### SetBounce

```csharp
public void SetBounce(float bounce)
```

引擎名：`set_bounce`

### SetFriction

```csharp
public void SetFriction(float friction)
```

引擎名：`set_friction`

### SetRough

```csharp
public void SetRough(bool rough)
```

引擎名：`set_rough`
