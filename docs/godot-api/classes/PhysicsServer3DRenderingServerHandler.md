<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicsServer3DRenderingServerHandler

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

## 构造函数

### PhysicsServer3DRenderingServerHandler

```csharp
public PhysicsServer3DRenderingServerHandler()
```

## 方法

### SetAabb

```csharp
public void SetAabb(Aabb aabb)
```

引擎名：`set_aabb`

Sets the bounding box for the [`SoftBody3D`](SoftBody3D.md).

### SetNormal

```csharp
public void SetNormal(int vertexId, Vector3 normal)
```

引擎名：`set_normal`

Sets the normal for the [`SoftBody3D`](SoftBody3D.md) vertex at the index specified by `vertexId`.

### SetVertex

```csharp
public void SetVertex(int vertexId, Vector3 vertex)
```

引擎名：`set_vertex`

Sets the position for the [`SoftBody3D`](SoftBody3D.md) vertex at the index specified by `vertexId`.

### _SetAabb

```csharp
public virtual void _SetAabb(Aabb aabb)
```

引擎名：`_set_aabb`

Called by the [`PhysicsServer3D`](PhysicsServer3D.md) to set the bounding box for the [`SoftBody3D`](SoftBody3D.md).

### _SetNormal

```csharp
public virtual void _SetNormal(int vertexId, Vector3 normal)
```

引擎名：`_set_normal`

Called by the [`PhysicsServer3D`](PhysicsServer3D.md) to set the normal for the [`SoftBody3D`](SoftBody3D.md) vertex at the index specified by `vertexId`.

**Note:** The `normal` parameter used to be of type `const void*` prior to Godot 4.2.

### _SetVertex

```csharp
public virtual void _SetVertex(int vertexId, Vector3 vertex)
```

引擎名：`_set_vertex`

Called by the [`PhysicsServer3D`](PhysicsServer3D.md) to set the position for the [`SoftBody3D`](SoftBody3D.md) vertex at the index specified by `vertexId`.

**Note:** The `vertex` parameter used to be of type `const void*` prior to Godot 4.2.
