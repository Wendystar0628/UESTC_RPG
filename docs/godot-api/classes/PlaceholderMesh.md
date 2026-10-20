<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PlaceholderMesh

命名空间 `Godot`；class；继承 [`Mesh`](Mesh.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class is used when loading a project that uses a [`Mesh`](Mesh.md) subclass in 2 conditions:

- When running the project exported in dedicated server mode, only the texture's dimensions are kept (as they may be relied upon for gameplay purposes or positioning of other elements). This allows reducing the exported PCK's size significantly.

- When this subclass is missing due to using a different engine version or build (e.g. modules disabled).

## 构造函数

### PlaceholderMesh

```csharp
public PlaceholderMesh()
```

## 属性

### Aabb

```csharp
public Aabb Aabb { get; set; }
```

引擎名：`aabb`

The smallest [`Aabb`](Aabb.md) enclosing this mesh in local space.

## 方法

### SetAabb

```csharp
public void SetAabb(Aabb aabb)
```

引擎名：`set_aabb`
