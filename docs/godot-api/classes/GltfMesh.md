<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GltfMesh

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

GLTFMesh handles 3D mesh data imported from glTF files. It includes properties for blend channels, blend weights, instance materials, and the mesh itself.

## 构造函数

### GltfMesh

```csharp
public GltfMesh()
```

## 属性

### BlendWeights

```csharp
public float[] BlendWeights { get; set; }
```

An array of floats representing the blend weights of the mesh.

### InstanceMaterials

```csharp
public Array<Material> InstanceMaterials { get; set; }
```

An array of Material objects representing the materials used in the mesh.

### Mesh

```csharp
public ImporterMesh Mesh { get; set; }
```

The [`ImporterMesh`](ImporterMesh.md) object representing the mesh itself.

### OriginalName

```csharp
public string OriginalName { get; set; }
```

The original name of the mesh.

## 方法

### GetAdditionalData

```csharp
public Variant GetAdditionalData(StringName extensionName)
```

Gets additional arbitrary data in this `GltfMesh` instance. This can be used to keep per-node state data in [`GltfDocumentExtension`](GltfDocumentExtension.md) classes, which is important because they are stateless.

The argument should be the [`GltfDocumentExtension`](GltfDocumentExtension.md) name (does not have to match the extension name in the glTF file), and the return value can be anything you set. If nothing was set, the return value is `null`.

### GetBlendWeights

```csharp
public float[] GetBlendWeights()
```

### GetInstanceMaterials

```csharp
public Array<Material> GetInstanceMaterials()
```

### GetMesh

```csharp
public ImporterMesh GetMesh()
```

### GetOriginalName

```csharp
public string GetOriginalName()
```

### SetAdditionalData

```csharp
public void SetAdditionalData(StringName extensionName, Variant additionalData)
```

Sets additional arbitrary data in this `GltfMesh` instance. This can be used to keep per-node state data in [`GltfDocumentExtension`](GltfDocumentExtension.md) classes, which is important because they are stateless.

The first argument should be the [`GltfDocumentExtension`](GltfDocumentExtension.md) name (does not have to match the extension name in the glTF file), and the second argument can be anything you want.

### SetBlendWeights

```csharp
public void SetBlendWeights(ReadOnlySpan<float> blendWeights)
```

### SetBlendWeights

```csharp
public void SetBlendWeights(float[] blendWeights)
```

### SetInstanceMaterials

```csharp
public void SetInstanceMaterials(Array<Material> instanceMaterials)
```

### SetMesh

```csharp
public void SetMesh(ImporterMesh mesh)
```

### SetOriginalName

```csharp
public void SetOriginalName(string originalName)
```
