<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicsDirectSpaceState2DExtension

命名空间 `Godot`；class；继承 [`PhysicsDirectSpaceState2D`](PhysicsDirectSpaceState2D.md) → [`GodotObject`](GodotObject.md)

This class extends [`PhysicsDirectSpaceState2D`](PhysicsDirectSpaceState2D.md) by providing additional virtual methods that can be overridden. When these methods are overridden, they will be called instead of the internal methods of the physics server.

Intended for use with GDExtension to create custom implementations of [`PhysicsDirectSpaceState2D`](PhysicsDirectSpaceState2D.md).

## 构造函数

### PhysicsDirectSpaceState2DExtension

```csharp
public PhysicsDirectSpaceState2DExtension()
```

## 方法

### IsBodyExcludedFromQuery

```csharp
public bool IsBodyExcludedFromQuery(Rid body)
```

引擎名：`is_body_excluded_from_query`
