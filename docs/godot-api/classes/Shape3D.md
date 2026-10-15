<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Shape3D

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Abstract base class for all 3D shapes, intended for use in physics.

**Performance:** Primitive shapes, especially [`SphereShape3D`](SphereShape3D.md), are fast to check collisions against. [`ConvexPolygonShape3D`](ConvexPolygonShape3D.md) and [`HeightMapShape3D`](HeightMapShape3D.md) are slower, and [`ConcavePolygonShape3D`](ConcavePolygonShape3D.md) is the slowest.

## 属性

### CustomSolverBias

```csharp
public float CustomSolverBias { get; set; }
```

引擎名：`custom_solver_bias`

The shape's custom solver bias. Defines how much bodies react to enforce contact separation when this shape is involved.

When set to `0`, the default value from `ProjectSettings.physics/3d/solver/default_contact_bias` is used.

**Note:** `CustomSolverBias` is only effective when using GodotPhysics3D. It has no effect when using Jolt Physics.

### Margin

```csharp
public float Margin { get; set; }
```

引擎名：`margin`

The collision margin for the shape. This is not used in Godot Physics.

Collision margins allow collision detection to be more efficient by adding an extra shell around shapes. Collision algorithms are more expensive when objects overlap by more than their margin, so a higher value for margins is better for performance, at the cost of accuracy around edges as it makes them less sharp.

## 方法

### GetCustomSolverBias

```csharp
public float GetCustomSolverBias()
```

引擎名：`get_custom_solver_bias`

### GetDebugMesh

```csharp
public ArrayMesh GetDebugMesh()
```

引擎名：`get_debug_mesh`

Returns the [`ArrayMesh`](ArrayMesh.md) used to draw the debug collision for this `Shape3D`.

### GetMargin

```csharp
public float GetMargin()
```

引擎名：`get_margin`

### SetCustomSolverBias

```csharp
public void SetCustomSolverBias(float bias)
```

引擎名：`set_custom_solver_bias`

### SetMargin

```csharp
public void SetMargin(float margin)
```

引擎名：`set_margin`
