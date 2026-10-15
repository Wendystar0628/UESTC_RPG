<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GltfState

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Contains all nodes and resources of a glTF file. This is used by [`GltfDocument`](GltfDocument.md) as data storage, which allows [`GltfDocument`](GltfDocument.md) and all [`GltfDocumentExtension`](GltfDocumentExtension.md) classes to remain stateless.

GLTFState can be populated by [`GltfDocument`](GltfDocument.md) reading a file or by converting a Godot scene. Then the data can either be used to create a Godot scene or save to a glTF file. The code that converts to/from a Godot scene can be intercepted at arbitrary points by [`GltfDocumentExtension`](GltfDocumentExtension.md) classes. This allows for custom data to be stored in the glTF file or for custom data to be converted to/from Godot nodes.

## 构造函数

### GltfState

```csharp
public GltfState()
```

## 属性

### Accessors

```csharp
public Array<GltfAccessor> Accessors { get; set; }
```

### Animations

```csharp
public Array<GltfAnimation> Animations { get; set; }
```

### BakeFps

```csharp
public double BakeFps { get; set; }
```

The baking fps of the animation for either import or export.

### BasePath

```csharp
public string BasePath { get; set; }
```

The folder path associated with this glTF data. This is used to find other files the glTF file references, like images or binary buffers. This will be set during import when appending from a file, and will be set during export when writing to a file.

### BufferViews

```csharp
public Array<GltfBufferView> BufferViews { get; set; }
```

### Buffers

```csharp
public Array<byte[]> Buffers { get; set; }
```

### Cameras

```csharp
public Array<GltfCamera> Cameras { get; set; }
```

### Copyright

```csharp
public string Copyright { get; set; }
```

The copyright string in the asset header of the glTF file. This is set during import if present and export if non-empty. See the glTF asset header documentation for more information.

### CreateAnimations

```csharp
public bool CreateAnimations { get; set; }
```

### FileName

```csharp
public string FileName { get; set; }
```

The file name associated with this glTF data. If it ends with `.gltf`, this is text-based glTF, otherwise this is binary GLB. This will be set during import when appending from a file, and will be set during export when writing to a file. If writing to a buffer, this will be an empty string.

### GlbData

```csharp
public byte[] GlbData { get; set; }
```

The binary buffer attached to a .glb file.

### HandleBinaryImage

```csharp
public int HandleBinaryImage { get; set; }
```

### HandleBinaryImageMode

```csharp
public GltfState.HandleBinaryImageModeEnum HandleBinaryImageMode { get; set; }
```

When importing a glTF file with unimported raw binary images embedded inside of binary blob buffers, in data URIs, or separate files not imported by Godot, this controls how the images are handled. Images can be discarded, saved as separate files, or embedded in the scene lossily or losslessly. See [`GltfState.HandleBinaryImageModeEnum`](GltfState.md) for options.

This property does nothing for image files in the `res://` folder imported by Godot, as those are handled by Godot's image importer directly, and then the Godot scene generated from the glTF file will use the images as Godot imported them.

### Images

```csharp
public Array<Texture2D> Images { get; set; }
```

### ImportAsSkeletonBones

```csharp
public bool ImportAsSkeletonBones { get; set; }
```

If `true`, forces all GLTFNodes in the document to be bones of a single [`Skeleton3D`](Skeleton3D.md) Godot node.

### Json

```csharp
public Dictionary Json { get; set; }
```

The original raw JSON document corresponding to this GLTFState.

### Lights

```csharp
public Array<GltfLight> Lights { get; set; }
```

### MajorVersion

```csharp
public int MajorVersion { get; set; }
```

### Materials

```csharp
public Array<Material> Materials { get; set; }
```

### Meshes

```csharp
public Array<GltfMesh> Meshes { get; set; }
```

### MinorVersion

```csharp
public int MinorVersion { get; set; }
```

### Nodes

```csharp
public Array<GltfNode> Nodes { get; set; }
```

### RootNodes

```csharp
public int[] RootNodes { get; set; }
```

The root nodes of the glTF file. Typically, a glTF file will only have one scene, and therefore one root node. However, a glTF file may have multiple scenes and therefore multiple root nodes, which will be generated as siblings of each other and as children of the root node of the generated Godot scene.

### SceneName

```csharp
public string SceneName { get; set; }
```

The name of the scene. When importing, if not specified, this will be the file name. When exporting, if specified, the scene name will be saved to the glTF file.

### Skeletons

```csharp
public Array<GltfSkeleton> Skeletons { get; set; }
```

### Skins

```csharp
public Array<GltfSkin> Skins { get; set; }
```

### TextureSamplers

```csharp
public Array<GltfTextureSampler> TextureSamplers { get; set; }
```

### Textures

```csharp
public Array<GltfTexture> Textures { get; set; }
```

### UniqueAnimationNames

```csharp
public Array<string> UniqueAnimationNames { get; set; }
```

### UniqueNames

```csharp
public Array<string> UniqueNames { get; set; }
```

### UseNamedSkinBinds

```csharp
public bool UseNamedSkinBinds { get; set; }
```

## 方法

### AddUsedExtension

```csharp
public void AddUsedExtension(string extensionName, bool required)
```

Appends an extension to the list of extensions used by this glTF file during serialization. If `required` is `true`, the extension will also be added to the list of required extensions. Do not run this in [`GltfDocumentExtension._ExportPost()`](GltfDocumentExtension.md), as that stage is too late to add extensions. The final list is sorted alphabetically.

### AppendDataToBuffers

```csharp
public int AppendDataToBuffers(ReadOnlySpan<byte> data, bool deduplication)
```

Appends the given byte array `data` to the buffers and creates a [`GltfBufferView`](GltfBufferView.md) for it. The index of the destination [`GltfBufferView`](GltfBufferView.md) is returned. If `deduplication` is `true`, the buffers are first searched for duplicate data, otherwise new bytes are always appended.

### AppendDataToBuffers

```csharp
public int AppendDataToBuffers(byte[] data, bool deduplication)
```

Appends the given byte array `data` to the buffers and creates a [`GltfBufferView`](GltfBufferView.md) for it. The index of the destination [`GltfBufferView`](GltfBufferView.md) is returned. If `deduplication` is `true`, the buffers are first searched for duplicate data, otherwise new bytes are always appended.

### AppendGltfNode

```csharp
public int AppendGltfNode(GltfNode gltfNode, Node godotSceneNode, int parentNodeIndex)
```

Appends the given [`GltfNode`](GltfNode.md) to the state, and returns its new index. This can be used to export one Godot node as multiple glTF nodes, or inject new glTF nodes at import time. On import, this must be called before [`GltfDocumentExtension._GenerateSceneNode()`](GltfDocumentExtension.md) finishes for the parent node. On export, this must be called before [`GltfDocumentExtension._ExportNode()`](GltfDocumentExtension.md) runs for the parent node.

The `godotSceneNode` parameter is the Godot scene node that corresponds to this glTF node. This is highly recommended to be set to a valid node, but may be `null` if there is no corresponding Godot scene node. One Godot scene node may be used for multiple glTF nodes, so if exporting multiple glTF nodes for one Godot scene node, use the same Godot scene node for each.

The `parentNodeIndex` parameter is the index of the parent [`GltfNode`](GltfNode.md) in the state. If `-1`, the node will be a root node, otherwise the new node will be added to the parent's list of children. The index will also be written to the [`GltfNode.Parent`](GltfNode.md) property of the new node.

### GetAccessors

```csharp
public Array<GltfAccessor> GetAccessors()
```

### GetAdditionalData

```csharp
public Variant GetAdditionalData(StringName extensionName)
```

Gets additional arbitrary data in this `GltfState` instance. This can be used to keep per-file state data in [`GltfDocumentExtension`](GltfDocumentExtension.md) classes, which is important because they are stateless.

The argument should be the [`GltfDocumentExtension`](GltfDocumentExtension.md) name (does not have to match the extension name in the glTF file), and the return value can be anything you set. If nothing was set, the return value is `null`.

### GetAnimationPlayer

```csharp
public AnimationPlayer GetAnimationPlayer(int animPlayerIndex)
```

Returns the [`AnimationPlayer`](AnimationPlayer.md) node with the given index. These nodes are only used during the export process when converting Godot [`AnimationPlayer`](AnimationPlayer.md) nodes to glTF animations.

### GetAnimationPlayersCount

```csharp
public int GetAnimationPlayersCount(int animPlayerIndex)
```

Returns the number of [`AnimationPlayer`](AnimationPlayer.md) nodes in this `GltfState`. These nodes are only used during the export process when converting Godot [`AnimationPlayer`](AnimationPlayer.md) nodes to glTF animations.

### GetAnimations

```csharp
public Array<GltfAnimation> GetAnimations()
```

Returns an array of all [`GltfAnimation`](GltfAnimation.md)s in the glTF file. When importing, these will be generated as animations in an [`AnimationPlayer`](AnimationPlayer.md) node. When exporting, these will be generated from Godot [`AnimationPlayer`](AnimationPlayer.md) nodes.

### GetBakeFps

```csharp
public double GetBakeFps()
```

### GetBasePath

```csharp
public string GetBasePath()
```

### GetBufferViews

```csharp
public Array<GltfBufferView> GetBufferViews()
```

### GetBuffers

```csharp
public Array<byte[]> GetBuffers()
```

### GetCameras

```csharp
public Array<GltfCamera> GetCameras()
```

Returns an array of all [`GltfCamera`](GltfCamera.md)s in the glTF file. These are the cameras that the [`GltfNode.Camera`](GltfNode.md) index refers to.

### GetCopyright

```csharp
public string GetCopyright()
```

### GetCreateAnimations

```csharp
public bool GetCreateAnimations()
```

### GetDiscardMeshesAndMaterials

```csharp
public bool GetDiscardMeshesAndMaterials()
```

### GetExtractPath

```csharp
public string GetExtractPath()
```

### GetExtractPrefix

```csharp
public string GetExtractPrefix()
```

### GetFileName

```csharp
public string GetFileName()
```

### GetGlbData

```csharp
public byte[] GetGlbData()
```

### GetHandleBinaryImage

```csharp
public int GetHandleBinaryImage()
```

Deprecated untyped alias for `HandleBinaryImageMode`. When importing a glTF file with unimported raw binary images embedded inside of binary blob buffers, in data URIs, or separate files not imported by Godot, this controls how the images are handled.

### GetHandleBinaryImageMode

```csharp
public GltfState.HandleBinaryImageModeEnum GetHandleBinaryImageMode()
```

### GetImages

```csharp
public Array<Texture2D> GetImages()
```

Gets the images of the glTF file as an array of [`Texture2D`](Texture2D.md)s. These are the images that the [`GltfTexture.SrcImage`](GltfTexture.md) index refers to.

### GetImportAsSkeletonBones

```csharp
public bool GetImportAsSkeletonBones()
```

### GetJson

```csharp
public Dictionary GetJson()
```

### GetLights

```csharp
public Array<GltfLight> GetLights()
```

Returns an array of all [`GltfLight`](GltfLight.md)s in the glTF file. These are the lights that the [`GltfNode.Light`](GltfNode.md) index refers to.

### GetMajorVersion

```csharp
public int GetMajorVersion()
```

### GetMaterials

```csharp
public Array<Material> GetMaterials()
```

### GetMeshes

```csharp
public Array<GltfMesh> GetMeshes()
```

Returns an array of all [`GltfMesh`](GltfMesh.md)es in the glTF file. These are the meshes that the [`GltfNode.Mesh`](GltfNode.md) index refers to.

### GetMinorVersion

```csharp
public int GetMinorVersion()
```

### GetNodeIndex

```csharp
public int GetNodeIndex(Node sceneNode)
```

Returns the index of the [`GltfNode`](GltfNode.md) corresponding to this Godot scene node. This is the inverse of `GetSceneNode()`. Useful during the export process.

**Note:** Not every Godot scene node will have a corresponding [`GltfNode`](GltfNode.md), and not every [`GltfNode`](GltfNode.md) will have a scene node generated. If there is no [`GltfNode`](GltfNode.md) index for this scene node, `-1` is returned.

### GetNodes

```csharp
public Array<GltfNode> GetNodes()
```

Returns an array of all [`GltfNode`](GltfNode.md)s in the glTF file. These are the nodes that [`GltfNode.Children`](GltfNode.md) and `RootNodes` refer to. This includes nodes that may not be generated in the Godot scene, or nodes that may generate multiple Godot scene nodes.

### GetRootNodes

```csharp
public int[] GetRootNodes()
```

### GetSceneName

```csharp
public string GetSceneName()
```

### GetSceneNode

```csharp
public Node GetSceneNode(int gltfNodeIndex)
```

Returns the Godot scene node that corresponds to the same index as the [`GltfNode`](GltfNode.md) it was generated from. This is the inverse of `GetNodeIndex()`. Useful during the import process.

**Note:** Not every [`GltfNode`](GltfNode.md) will have a scene node generated, and not every generated scene node will have a corresponding [`GltfNode`](GltfNode.md). If there is no scene node for this [`GltfNode`](GltfNode.md) index, `null` is returned.

### GetSkeletons

```csharp
public Array<GltfSkeleton> GetSkeletons()
```

Returns an array of all [`GltfSkeleton`](GltfSkeleton.md)s in the glTF file. These are the skeletons that the [`GltfNode.Skeleton`](GltfNode.md) index refers to.

### GetSkins

```csharp
public Array<GltfSkin> GetSkins()
```

Returns an array of all [`GltfSkin`](GltfSkin.md)s in the glTF file. These are the skins that the [`GltfNode.Skin`](GltfNode.md) index refers to.

### GetTextureSamplers

```csharp
public Array<GltfTextureSampler> GetTextureSamplers()
```

Retrieves the array of texture samplers that are used by the textures contained in the glTF.

### GetTextures

```csharp
public Array<GltfTexture> GetTextures()
```

### GetUniqueAnimationNames

```csharp
public Array<string> GetUniqueAnimationNames()
```

Returns an array of unique animation names. This is only used during the import process.

### GetUniqueNames

```csharp
public Array<string> GetUniqueNames()
```

Returns an array of unique node names. This is used in both the import process and export process.

### GetUseNamedSkinBinds

```csharp
public bool GetUseNamedSkinBinds()
```

### SetAccessors

```csharp
public void SetAccessors(Array<GltfAccessor> accessors)
```

### SetAdditionalData

```csharp
public void SetAdditionalData(StringName extensionName, Variant additionalData)
```

Sets additional arbitrary data in this `GltfState` instance. This can be used to keep per-file state data in [`GltfDocumentExtension`](GltfDocumentExtension.md) classes, which is important because they are stateless.

The first argument should be the [`GltfDocumentExtension`](GltfDocumentExtension.md) name (does not have to match the extension name in the glTF file), and the second argument can be anything you want.

### SetAnimations

```csharp
public void SetAnimations(Array<GltfAnimation> animations)
```

Sets the [`GltfAnimation`](GltfAnimation.md)s in the state. When importing, these will be generated as animations in an [`AnimationPlayer`](AnimationPlayer.md) node. When exporting, these will be generated from Godot [`AnimationPlayer`](AnimationPlayer.md) nodes.

### SetBakeFps

```csharp
public void SetBakeFps(double value)
```

### SetBasePath

```csharp
public void SetBasePath(string basePath)
```

### SetBufferViews

```csharp
public void SetBufferViews(Array<GltfBufferView> bufferViews)
```

### SetBuffers

```csharp
public void SetBuffers(Array<byte[]> buffers)
```

### SetCameras

```csharp
public void SetCameras(Array<GltfCamera> cameras)
```

Sets the [`GltfCamera`](GltfCamera.md)s in the state. These are the cameras that the [`GltfNode.Camera`](GltfNode.md) index refers to.

### SetCopyright

```csharp
public void SetCopyright(string copyright)
```

### SetCreateAnimations

```csharp
public void SetCreateAnimations(bool createAnimations)
```

### SetFileName

```csharp
public void SetFileName(string fileName)
```

### SetGlbData

```csharp
public void SetGlbData(ReadOnlySpan<byte> glbData)
```

### SetGlbData

```csharp
public void SetGlbData(byte[] glbData)
```

### SetHandleBinaryImage

```csharp
public void SetHandleBinaryImage(int method)
```

Deprecated untyped alias for `HandleBinaryImageMode`. When importing a glTF file with unimported raw binary images embedded inside of binary blob buffers, in data URIs, or separate files not imported by Godot, this controls how the images are handled.

### SetHandleBinaryImageMode

```csharp
public void SetHandleBinaryImageMode(GltfState.HandleBinaryImageModeEnum method)
```

### SetImages

```csharp
public void SetImages(Array<Texture2D> images)
```

Sets the images in the state stored as an array of [`Texture2D`](Texture2D.md)s. This can be used during export. These are the images that the [`GltfTexture.SrcImage`](GltfTexture.md) index refers to.

### SetImportAsSkeletonBones

```csharp
public void SetImportAsSkeletonBones(bool importAsSkeletonBones)
```

### SetJson

```csharp
public void SetJson(Dictionary json)
```

### SetLights

```csharp
public void SetLights(Array<GltfLight> lights)
```

Sets the [`GltfLight`](GltfLight.md)s in the state. These are the lights that the [`GltfNode.Light`](GltfNode.md) index refers to.

### SetMajorVersion

```csharp
public void SetMajorVersion(int majorVersion)
```

### SetMaterials

```csharp
public void SetMaterials(Array<Material> materials)
```

### SetMeshes

```csharp
public void SetMeshes(Array<GltfMesh> meshes)
```

Sets the [`GltfMesh`](GltfMesh.md)es in the state. These are the meshes that the [`GltfNode.Mesh`](GltfNode.md) index refers to.

### SetMinorVersion

```csharp
public void SetMinorVersion(int minorVersion)
```

### SetNodes

```csharp
public void SetNodes(Array<GltfNode> nodes)
```

Sets the [`GltfNode`](GltfNode.md)s in the state. These are the nodes that [`GltfNode.Children`](GltfNode.md) and `RootNodes` refer to. Some of the nodes set here may not be generated in the Godot scene, or may generate multiple Godot scene nodes.

### SetRootNodes

```csharp
public void SetRootNodes(ReadOnlySpan<int> rootNodes)
```

### SetRootNodes

```csharp
public void SetRootNodes(int[] rootNodes)
```

### SetSceneName

```csharp
public void SetSceneName(string sceneName)
```

### SetSkeletons

```csharp
public void SetSkeletons(Array<GltfSkeleton> skeletons)
```

Sets the [`GltfSkeleton`](GltfSkeleton.md)s in the state. These are the skeletons that the [`GltfNode.Skeleton`](GltfNode.md) index refers to.

### SetSkins

```csharp
public void SetSkins(Array<GltfSkin> skins)
```

Sets the [`GltfSkin`](GltfSkin.md)s in the state. These are the skins that the [`GltfNode.Skin`](GltfNode.md) index refers to.

### SetTextureSamplers

```csharp
public void SetTextureSamplers(Array<GltfTextureSampler> textureSamplers)
```

Sets the array of texture samplers that are used by the textures contained in the glTF.

### SetTextures

```csharp
public void SetTextures(Array<GltfTexture> textures)
```

### SetUniqueAnimationNames

```csharp
public void SetUniqueAnimationNames(Array<string> uniqueAnimationNames)
```

Sets the unique animation names in the state. This is only used during the import process.

### SetUniqueNames

```csharp
public void SetUniqueNames(Array<string> uniqueNames)
```

Sets the unique node names in the state. This is used in both the import process and export process.

### SetUseNamedSkinBinds

```csharp
public void SetUseNamedSkinBinds(bool useNamedSkinBinds)
```

## 常量

### HandleBinaryDiscardTextures

```csharp
public const long HandleBinaryDiscardTextures = 0
```

Discards all embedded textures and uses untextured materials.

### HandleBinaryEmbedAsBasisu

```csharp
public const long HandleBinaryEmbedAsBasisu = 2
```

Embeds textures VRAM compressed with Basis Universal into the generated scene.

### HandleBinaryEmbedAsUncompressed

```csharp
public const long HandleBinaryEmbedAsUncompressed = 3
```

Embeds textures compressed losslessly into the generated scene, matching old behavior.

### HandleBinaryExtractTextures

```csharp
public const long HandleBinaryExtractTextures = 1
```

Extracts embedded textures to be reimported and compressed. Editor only. Acts as uncompressed at runtime.

## HandleBinaryImageModeEnum

命名空间 `Godot`；enum

### 枚举值

- `DiscardTextures = 0`：When importing a glTF file with embedded binary images, discards all images and uses untextured materials in their place.
- `ExtractTextures = 1`：When importing a glTF file with embedded binary images, extracts them and saves them to their own files.
- `EmbedAsBasisu = 2`：When importing a glTF file with embedded binary images, embeds textures VRAM compressed with Basis Universal into the generated scene.
- `EmbedAsUncompressed = 3`：When importing a glTF file with embedded binary images, embeds textures compressed losslessly into the generated scene.
