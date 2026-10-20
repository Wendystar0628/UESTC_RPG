<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# DirectionalLight2D

命名空间 `Godot`；class；继承 [`Light2D`](Light2D.md) → [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A directional light is a type of [`Light2D`](Light2D.md) node that models an infinite number of parallel rays covering the entire scene. It is used for lights with strong intensity that are located far away from the scene (for example: to model sunlight or moonlight).

Light is emitted in the +Y direction of the node's global basis. For an unrotated light, this means that the light is emitted downwards. The position of the node is ignored; only the basis is used to determine light direction.

**Note:** `DirectionalLight2D` does not support light cull masks (but it supports shadow cull masks). It will always light up 2D nodes, regardless of the 2D node's [`CanvasItem.LightMask`](CanvasItem.md).

## 构造函数

### DirectionalLight2D

```csharp
public DirectionalLight2D()
```

## 属性

### Height

```csharp
public float Height { get; set; }
```

引擎名：`height`

The height of the light. Used with 2D normal mapping. Ranges from 0 (parallel to the plane) to 1 (perpendicular to the plane).

### MaxDistance

```csharp
public float MaxDistance { get; set; }
```

引擎名：`max_distance`

The maximum distance from the camera center objects can be before their shadows are culled (in pixels). Decreasing this value can prevent objects located outside the camera from casting shadows (while also improving performance). [`Camera2D.Zoom`](Camera2D.md) is not taken into account by `MaxDistance`, which means that at higher zoom values, shadows will appear to fade out sooner when zooming onto a given point.

## 方法

### GetMaxDistance

```csharp
public float GetMaxDistance()
```

引擎名：`get_max_distance`

### SetMaxDistance

```csharp
public void SetMaxDistance(float pixels)
```

引擎名：`set_max_distance`
