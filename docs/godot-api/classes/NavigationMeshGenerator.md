<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# NavigationMeshGenerator

命名空间 `Godot`；static class

This class is responsible for creating and clearing 3D navigation meshes used as [`NavigationMesh`](NavigationMesh.md) resources inside [`NavigationRegion3D`](NavigationRegion3D.md). The `NavigationMeshGenerator` has very limited to no use for 2D as the navigation mesh baking process expects 3D node types and 3D source geometry to parse.

The entire navigation mesh baking is best done in a separate thread as the voxelization, collision tests and mesh optimization steps involved are very slow and performance-intensive operations.

Navigation mesh baking happens in multiple steps and the result depends on 3D source geometry and properties of the [`NavigationMesh`](NavigationMesh.md) resource. In the first step, starting from a root node and depending on [`NavigationMesh`](NavigationMesh.md) properties all valid 3D source geometry nodes are collected from the [`SceneTree`](SceneTree.md). Second, all collected nodes are parsed for their relevant 3D geometry data and a combined 3D mesh is build. Due to the many different types of parsable objects, from normal [`MeshInstance3D`](MeshInstance3D.md)s to [`CsgShape3D`](CsgShape3D.md)s or various [`CollisionObject3D`](CollisionObject3D.md)s, some operations to collect geometry data can trigger [`RenderingServer`](RenderingServer.md) and [`PhysicsServer3D`](PhysicsServer3D.md) synchronizations. Server synchronization can have a negative effect on baking time or framerate as it often involves [`Mutex`](Mutex.md) locking for thread security. Many parsable objects and the continuous synchronization with other threaded Servers can increase the baking time significantly. On the other hand only a few but very large and complex objects will take some time to prepare for the Servers which can noticeably stall the next frame render. As a general rule the total number of parsable objects and their individual size and complexity should be balanced to avoid framerate issues or very long baking times. The combined mesh is then passed to the Recast Navigation Object to test the source geometry for walkable terrain suitable to [`NavigationMesh`](NavigationMesh.md) agent properties by creating a voxel world around the meshes bounding area.

The finalized navigation mesh is then returned and stored inside the [`NavigationMesh`](NavigationMesh.md) for use as a resource inside [`NavigationRegion3D`](NavigationRegion3D.md) nodes.

**Note:** Using meshes to not only define walkable surfaces but also obstruct navigation baking does not always work. The navigation baking has no concept of what is a geometry "inside" when dealing with mesh source geometry and this is intentional. Depending on current baking parameters, as soon as the obstructing mesh is large enough to fit a navigation mesh area inside, the baking will generate navigation mesh areas that are inside the obstructing source geometry mesh.

## 属性

### Singleton

```csharp
public static NavigationMeshGeneratorInstance Singleton { get; }
```

## 方法

### Bake

```csharp
public static void Bake(NavigationMesh navigationMesh, Node rootNode)
```

引擎名：`bake`

Bakes the `navigationMesh` with source geometry collected starting from the `rootNode`.

### BakeFromSourceGeometryData

```csharp
public static void BakeFromSourceGeometryData(NavigationMesh navigationMesh, NavigationMeshSourceGeometryData3D sourceGeometryData, Callable callback = default)
```

引擎名：`bake_from_source_geometry_data`

Bakes the provided `navigationMesh` with the data from the provided `sourceGeometryData`. After the process is finished the optional `callback` will be called.

### Clear

```csharp
public static void Clear(NavigationMesh navigationMesh)
```

引擎名：`clear`

Removes all polygons and vertices from the provided `navigationMesh` resource.

### ParseSourceGeometryData

```csharp
public static void ParseSourceGeometryData(NavigationMesh navigationMesh, NavigationMeshSourceGeometryData3D sourceGeometryData, Node rootNode, Callable callback = default)
```

引擎名：`parse_source_geometry_data`

Parses the [`SceneTree`](SceneTree.md) for source geometry according to the properties of `navigationMesh`. Updates the provided `sourceGeometryData` resource with the resulting data. The resource can then be used to bake a navigation mesh with `BakeFromSourceGeometryData()`. After the process is finished the optional `callback` will be called.

**Note:** This function needs to run on the main thread or with a deferred call as the SceneTree is not thread-safe.

**Performance:** While convenient, reading data arrays from [`Mesh`](Mesh.md) resources can affect the frame rate negatively. The data needs to be received from the GPU, stalling the [`RenderingServer`](RenderingServer.md) in the process. For performance prefer the use of e.g. collision shapes or creating the data arrays entirely in code.
