<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# MultiMeshInstance3D

命名空间 `Godot`；class；继承 [`GeometryInstance3D`](GeometryInstance3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`MultiMeshInstance3D` is a specialized node to instance [`GeometryInstance3D`](GeometryInstance3D.md)s based on a [`MultiMesh`](MultiMesh.md) resource.

This is useful to optimize the rendering of a high number of instances of a given mesh (for example trees in a forest or grass strands).

## 构造函数

### MultiMeshInstance3D

```csharp
public MultiMeshInstance3D()
```

## 属性

### Multimesh

```csharp
public MultiMesh Multimesh { get; set; }
```

引擎名：`multimesh`

The [`MultiMesh`](MultiMesh.md) resource that will be used and shared among all instances of the `MultiMeshInstance3D`.

## 方法

### GetMultimesh

```csharp
public MultiMesh GetMultimesh()
```

引擎名：`get_multimesh`

### SetMultimesh

```csharp
public void SetMultimesh(MultiMesh multimesh)
```

引擎名：`set_multimesh`
