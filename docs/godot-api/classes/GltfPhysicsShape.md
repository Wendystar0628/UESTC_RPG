<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GltfPhysicsShape

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Represents a physics shape as defined by the `OMI_physics_shape` or `OMI_collider` glTF extensions. This class is an intermediary between the glTF data and Godot's nodes, and it's abstracted in a way that allows adding support for different glTF physics extensions in the future.

## 构造函数

### GltfPhysicsShape

```csharp
public GltfPhysicsShape()
```

## 属性

### Height

```csharp
public float Height { get; set; }
```

The height of the shape, in meters. This is only used when the shape type is `"capsule"` or `"cylinder"`. This value should not be negative, and for `"capsule"` it should be at least twice the radius.

### ImporterMesh

```csharp
public ImporterMesh ImporterMesh { get; set; }
```

The [`ImporterMesh`](ImporterMesh.md) resource of the shape. This is only used when the shape type is `"hull"` (convex hull) or `"trimesh"` (concave trimesh).

### IsTrigger

```csharp
public bool IsTrigger { get; set; }
```

If `true`, indicates that this shape is a trigger. For Godot, this means that the shape should be a child of an [`Area3D`](Area3D.md) node.

This is the only variable not used in the `ToNode()` method, it's intended to be used alongside when deciding where to add the generated node as a child.

### MeshIndex

```csharp
public int MeshIndex { get; set; }
```

The index of the shape's mesh in the glTF file. This is only used when the shape type is `"hull"` (convex hull) or `"trimesh"` (concave trimesh).

### Radius

```csharp
public float Radius { get; set; }
```

The radius of the shape, in meters. This is only used when the shape type is `"capsule"`, `"cylinder"`, or `"sphere"`. This value should not be negative.

### ShapeType

```csharp
public string ShapeType { get; set; }
```

The type of shape this shape represents. Valid values are `"box"`, `"capsule"`, `"cylinder"`, `"sphere"`, `"hull"`, and `"trimesh"`.

### Size

```csharp
public Vector3 Size { get; set; }
```

The size of the shape, in meters. This is only used when the shape type is `"box"`, and it represents the `"diameter"` of the box. This value should not be negative.

## 方法

### FromDictionary

```csharp
public static GltfPhysicsShape FromDictionary(Dictionary dictionary)
```

Creates a new GLTFPhysicsShape instance by parsing the given [`Collections.Dictionary`](Collections.Dictionary.md).

### FromNode

```csharp
public static GltfPhysicsShape FromNode(CollisionShape3D shapeNode)
```

Creates a new GLTFPhysicsShape instance from the given Godot [`CollisionShape3D`](CollisionShape3D.md) node.

### FromResource

```csharp
public static GltfPhysicsShape FromResource(Shape3D shapeResource)
```

Creates a new GLTFPhysicsShape instance from the given Godot [`Shape3D`](Shape3D.md) resource.

### GetHeight

```csharp
public float GetHeight()
```

### GetImporterMesh

```csharp
public ImporterMesh GetImporterMesh()
```

### GetIsTrigger

```csharp
public bool GetIsTrigger()
```

### GetMeshIndex

```csharp
public int GetMeshIndex()
```

### GetRadius

```csharp
public float GetRadius()
```

### GetShapeType

```csharp
public string GetShapeType()
```

### GetSize

```csharp
public Vector3 GetSize()
```

### SetHeight

```csharp
public void SetHeight(float height)
```

### SetImporterMesh

```csharp
public void SetImporterMesh(ImporterMesh importerMesh)
```

### SetIsTrigger

```csharp
public void SetIsTrigger(bool isTrigger)
```

### SetMeshIndex

```csharp
public void SetMeshIndex(int meshIndex)
```

### SetRadius

```csharp
public void SetRadius(float radius)
```

### SetShapeType

```csharp
public void SetShapeType(string shapeType)
```

### SetSize

```csharp
public void SetSize(Vector3 size)
```

### ToDictionary

```csharp
public Dictionary ToDictionary()
```

Serializes this GLTFPhysicsShape instance into a [`Collections.Dictionary`](Collections.Dictionary.md) in the format defined by `OMI_physics_shape`.

### ToNode

```csharp
public CollisionShape3D ToNode(bool cacheShapes = false)
```

Converts this GLTFPhysicsShape instance into a Godot [`CollisionShape3D`](CollisionShape3D.md) node.

### ToResource

```csharp
public Shape3D ToResource(bool cacheShapes = false)
```

Converts this GLTFPhysicsShape instance into a Godot [`Shape3D`](Shape3D.md) resource.
