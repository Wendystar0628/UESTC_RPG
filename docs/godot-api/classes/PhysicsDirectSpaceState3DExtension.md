<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicsDirectSpaceState3DExtension

命名空间 `Godot`；class；继承 [`PhysicsDirectSpaceState3D`](PhysicsDirectSpaceState3D.md) → [`GodotObject`](GodotObject.md)

This class extends [`PhysicsDirectSpaceState3D`](PhysicsDirectSpaceState3D.md) by providing additional virtual methods that can be overridden. When these methods are overridden, they will be called instead of the internal methods of the physics server.

Intended for use with GDExtension to create custom implementations of [`PhysicsDirectSpaceState3D`](PhysicsDirectSpaceState3D.md).

## 构造函数

### PhysicsDirectSpaceState3DExtension

```csharp
public PhysicsDirectSpaceState3DExtension()
```

## 方法

### IsBodyExcludedFromQuery

```csharp
public bool IsBodyExcludedFromQuery(Rid body)
```

引擎名：`is_body_excluded_from_query`

### _GetClosestPointToObjectVolume

```csharp
public virtual Vector3 _GetClosestPointToObjectVolume(Rid object, Vector3 point)
```

引擎名：`_get_closest_point_to_object_volume`
