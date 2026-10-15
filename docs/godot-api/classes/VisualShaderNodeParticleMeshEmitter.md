<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeParticleMeshEmitter

命名空间 `Godot`；class；继承 [`VisualShaderNodeParticleEmitter`](VisualShaderNodeParticleEmitter.md) → [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

[`VisualShaderNodeParticleEmitter`](VisualShaderNodeParticleEmitter.md) that makes the particles emitted in a shape of the assigned `Mesh`. It will emit from the mesh's surfaces, either all or only the specified one.

## 构造函数

### VisualShaderNodeParticleMeshEmitter

```csharp
public VisualShaderNodeParticleMeshEmitter()
```

## 属性

### Mesh

```csharp
public Mesh Mesh { get; set; }
```

引擎名：`mesh`

The [`Mesh`](Mesh.md) that defines emission shape.

### SurfaceIndex

```csharp
public int SurfaceIndex { get; set; }
```

引擎名：`surface_index`

Index of the surface that emits particles. `UseAllSurfaces` must be `false` for this to take effect.

### UseAllSurfaces

```csharp
public bool UseAllSurfaces { get; set; }
```

引擎名：`use_all_surfaces`

If `true`, the particles will emit from all surfaces of the mesh.

## 方法

### GetMesh

```csharp
public Mesh GetMesh()
```

引擎名：`get_mesh`

### GetSurfaceIndex

```csharp
public int GetSurfaceIndex()
```

引擎名：`get_surface_index`

### IsUseAllSurfaces

```csharp
public bool IsUseAllSurfaces()
```

引擎名：`is_use_all_surfaces`

### SetMesh

```csharp
public void SetMesh(Mesh mesh)
```

引擎名：`set_mesh`

### SetSurfaceIndex

```csharp
public void SetSurfaceIndex(int surfaceIndex)
```

引擎名：`set_surface_index`

### SetUseAllSurfaces

```csharp
public void SetUseAllSurfaces(bool enabled)
```

引擎名：`set_use_all_surfaces`
