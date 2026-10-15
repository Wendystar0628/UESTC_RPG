<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GltfNode

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Represents a glTF node. glTF nodes may have names, transforms, children (other glTF nodes), and more specialized properties (represented by their own classes).

glTF nodes generally exist inside of [`GltfState`](GltfState.md) which represents all data of a glTF file. Most of GLTFNode's properties are indices of other data in the glTF file. You can extend a glTF node with additional properties by using `GetAdditionalData()` and `SetAdditionalData()`.

## 构造函数

### GltfNode

```csharp
public GltfNode()
```

## 属性

### Camera

```csharp
public int Camera { get; set; }
```

If this glTF node is a camera, the index of the [`GltfCamera`](GltfCamera.md) in the [`GltfState`](GltfState.md) that describes the camera's properties. If `-1`, this node is not a camera.

### Children

```csharp
public int[] Children { get; set; }
```

The indices of the child nodes in the [`GltfState`](GltfState.md). If this glTF node has no children, this will be an empty array.

### Height

```csharp
public int Height { get; set; }
```

How deep into the node hierarchy this node is. A root node will have a height of 0, its children will have a height of 1, and so on. If -1, the height has not been calculated.

### Light

```csharp
public int Light { get; set; }
```

If this glTF node is a light, the index of the [`GltfLight`](GltfLight.md) in the [`GltfState`](GltfState.md) that describes the light's properties. If -1, this node is not a light.

### Mesh

```csharp
public int Mesh { get; set; }
```

If this glTF node is a mesh, the index of the [`GltfMesh`](GltfMesh.md) in the [`GltfState`](GltfState.md) that describes the mesh's properties. If -1, this node is not a mesh.

### OriginalName

```csharp
public string OriginalName { get; set; }
```

The original name of the node.

### Parent

```csharp
public int Parent { get; set; }
```

The index of the parent node in the [`GltfState`](GltfState.md). If -1, this node is a root node.

### Position

```csharp
public Vector3 Position { get; set; }
```

The position of the glTF node relative to its parent.

### Rotation

```csharp
public Quaternion Rotation { get; set; }
```

The rotation of the glTF node relative to its parent.

### Scale

```csharp
public Vector3 Scale { get; set; }
```

The scale of the glTF node relative to its parent.

### Skeleton

```csharp
public int Skeleton { get; set; }
```

If this glTF node has a skeleton, the index of the [`GltfSkeleton`](GltfSkeleton.md) in the [`GltfState`](GltfState.md) that describes the skeleton's properties. If -1, this node does not have a skeleton.

### Skin

```csharp
public int Skin { get; set; }
```

If this glTF node has a skin, the index of the [`GltfSkin`](GltfSkin.md) in the [`GltfState`](GltfState.md) that describes the skin's properties. If -1, this node does not have a skin.

### Visible

```csharp
public bool Visible { get; set; }
```

If `true`, the GLTF node is visible. If `false`, the GLTF node is not visible. This is converted to the [`Node3D.Visible`](Node3D.md) property in the Godot scene, and is exported to `KHR_node_visibility` when `false`.

### Xform

```csharp
public Transform3D Xform { get; set; }
```

The transform of the glTF node relative to its parent. This property is usually unused since the position, rotation, and scale properties are preferred.

## 方法

### AppendChildIndex

```csharp
public void AppendChildIndex(int childIndex)
```

Appends the given child node index to the `Children` array.

### GetAdditionalData

```csharp
public Variant GetAdditionalData(StringName extensionName)
```

Gets additional arbitrary data in this `GltfNode` instance. This can be used to keep per-node state data in [`GltfDocumentExtension`](GltfDocumentExtension.md) classes, which is important because they are stateless.

The argument should be the [`GltfDocumentExtension`](GltfDocumentExtension.md) name (does not have to match the extension name in the glTF file), and the return value can be anything you set. If nothing was set, the return value is `null`.

### GetCamera

```csharp
public int GetCamera()
```

### GetChildren

```csharp
public int[] GetChildren()
```

### GetHeight

```csharp
public int GetHeight()
```

### GetLight

```csharp
public int GetLight()
```

### GetMesh

```csharp
public int GetMesh()
```

### GetOriginalName

```csharp
public string GetOriginalName()
```

### GetParent

```csharp
public int GetParent()
```

### GetPosition

```csharp
public Vector3 GetPosition()
```

### GetRotation

```csharp
public Quaternion GetRotation()
```

### GetScale

```csharp
public Vector3 GetScale()
```

### GetSceneNodePath

```csharp
public NodePath GetSceneNodePath(GltfState gltfState, bool handleSkeletons = true)
```

Returns the [`NodePath`](NodePath.md) that this GLTF node will have in the Godot scene tree after being imported. This is useful when importing glTF object model pointers with [`GltfObjectModelProperty`](GltfObjectModelProperty.md), for handling extensions such as `KHR_animation_pointer` or `KHR_interactivity`.

If `handleSkeletons` is `true`, paths to skeleton bone glTF nodes will be resolved properly. For example, a path that would be `^"A/B/C/Bone1/Bone2/Bone3"` if `false` will become `^"A/B/C/Skeleton3D:Bone3"`.

### GetSkeleton

```csharp
public int GetSkeleton()
```

### GetSkin

```csharp
public int GetSkin()
```

### GetVisible

```csharp
public bool GetVisible()
```

### GetXform

```csharp
public Transform3D GetXform()
```

### SetAdditionalData

```csharp
public void SetAdditionalData(StringName extensionName, Variant additionalData)
```

Sets additional arbitrary data in this `GltfNode` instance. This can be used to keep per-node state data in [`GltfDocumentExtension`](GltfDocumentExtension.md) classes, which is important because they are stateless.

The first argument should be the [`GltfDocumentExtension`](GltfDocumentExtension.md) name (does not have to match the extension name in the glTF file), and the second argument can be anything you want.

### SetCamera

```csharp
public void SetCamera(int camera)
```

### SetChildren

```csharp
public void SetChildren(ReadOnlySpan<int> children)
```

### SetChildren

```csharp
public void SetChildren(int[] children)
```

### SetHeight

```csharp
public void SetHeight(int height)
```

### SetLight

```csharp
public void SetLight(int light)
```

### SetMesh

```csharp
public void SetMesh(int mesh)
```

### SetOriginalName

```csharp
public void SetOriginalName(string originalName)
```

### SetParent

```csharp
public void SetParent(int parent)
```

### SetPosition

```csharp
public void SetPosition(Vector3 position)
```

### SetRotation

```csharp
public void SetRotation(Quaternion rotation)
```

### SetScale

```csharp
public void SetScale(Vector3 scale)
```

### SetSkeleton

```csharp
public void SetSkeleton(int skeleton)
```

### SetSkin

```csharp
public void SetSkin(int skin)
```

### SetVisible

```csharp
public void SetVisible(bool visible)
```

### SetXform

```csharp
public void SetXform(Transform3D xform)
```
