<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GltfDocumentExtension

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Extends the functionality of the [`GltfDocument`](GltfDocument.md) class by allowing you to run arbitrary code at various stages of glTF import or export.

To use, make a new class extending GLTFDocumentExtension, override any methods you need, make an instance of your class, and register it using [`GltfDocument.RegisterGltfDocumentExtension`](GltfDocument.md).

**Note:** All GLTFDocumentExtension classes are duplicated when beginning the import or export process. Except for configuration values, these classes must be stateless in order to function properly. If you need to store data, use the `set_additional_data` and `get_additional_data` methods in [`GltfState`](GltfState.md) or [`GltfNode`](GltfNode.md).

## 构造函数

### GltfDocumentExtension

```csharp
public GltfDocumentExtension()
```

## 方法

### _ConvertSceneNode

```csharp
public virtual void _ConvertSceneNode(GltfState state, GltfNode gltfNode, Node sceneNode)
```

Part of the export process. This method is run after `_ExportPreflight()` and before `_ExportPostConvert()`.

Runs when converting the data from a Godot scene node. This method can be used to process the Godot scene node data into a format that can be used by `_ExportNode()`.

### _ExportGetPropertyList

```csharp
public virtual Array<Dictionary> _ExportGetPropertyList(Node rootNode)
```

Runs prior to the export process. This method is run before `_ExportPreflight()` when exporting a scene from the editor, or it may not be run at all in other situations.

Unlike the rest of the export methods, this does not run when calling a [`GltfDocument`](GltfDocument.md)'s export methods in sequence with everything else, but rather runs before that entire process occurs, allowing configuration to occur beforehand, potentially minutes or hours in advance of `_ExportPreflight()`. This allows extensions to decide which properties to show in the editor export settings dialog based on the contents of the scene, hiding any settings that are not relevant for that scene. The `rootNode` parameter may be `null`, in which case all properties should be shown.

### _ExportNode

```csharp
public virtual Error _ExportNode(GltfState state, GltfNode gltfNode, Dictionary json, Node node)
```

Part of the export process. This method is run after `_GetSaveableImageFormats()` and before `_ExportPost()`. If this `GltfDocumentExtension` is used for exporting images, this runs after `_SerializeTextureJson()`.

This method can be used to modify the final JSON of each node. Data should be primarily stored in `gltfNode` prior to serializing the JSON, but the original Godot [`Node`](Node.md) is also provided if available. `node` may be `null` if not available, such as when exporting glTF data not generated from a Godot scene.

### _ExportObjectModelProperty

```csharp
public virtual GltfObjectModelProperty _ExportObjectModelProperty(GltfState state, NodePath nodePath, Node godotNode, int gltfNodeIndex, GodotObject targetObject, int targetDepth)
```

Part of the export process. Allows GLTFDocumentExtension classes to provide mappings for properties of nodes in the Godot scene tree, to JSON pointers to glTF properties, as defined by the glTF object model.

Returns a [`GltfObjectModelProperty`](GltfObjectModelProperty.md) instance that defines how the property should be mapped. If your extension can't handle the property, return `null` or an instance without any JSON pointers (see [`GltfObjectModelProperty.HasJsonPointers`](GltfObjectModelProperty.md)). You should use [`GltfObjectModelProperty.SetTypes`](GltfObjectModelProperty.md) to set the types, and set the JSON pointer(s) using the [`GltfObjectModelProperty.JsonPointers`](GltfObjectModelProperty.md) property.

The parameters provide context for the property, including the NodePath, the Godot node, the GLTF node index, and the target object. The `targetObject` will be equal to `godotNode` if no sub-object can be found, otherwise it will point to a sub-object. For example, if the path is `^"A/B/C/MeshInstance3D:mesh:surface_0/material:emission_intensity"`, it will get the node, then the mesh, and then the material, so `targetObject` will be the [`Material`](Material.md) resource, and `targetDepth` will be 2 because 2 levels were traversed to get to the target.

### _ExportPost

```csharp
public virtual Error _ExportPost(GltfState state)
```

Part of the export process. This method is run last, after all other parts of the export process.

This method can be used to modify the final JSON of the generated glTF file.

### _ExportPostConvert

```csharp
public virtual Error _ExportPostConvert(GltfState state, Node root)
```

Part of the export process. This method is run after `_ConvertSceneNode()` and before `_ExportPreserialize()`.

This method can be used to modify the converted node data structures before serialization with any additional data from the scene tree.

### _ExportPreflight

```csharp
public virtual Error _ExportPreflight(GltfState state, Node root)
```

Part of the export process. This method is run first, before all other parts of the export process.

The return value is used to determine if this `GltfDocumentExtension` instance should be used for exporting a given glTF file. If [`Error.Ok`](Error.md), the export will use this `GltfDocumentExtension` instance. If not overridden, [`Error.Ok`](Error.md) is returned.

### _ExportPreserialize

```csharp
public virtual Error _ExportPreserialize(GltfState state)
```

Part of the export process. This method is run after `_ExportPostConvert()` and before `_GetSaveableImageFormats()`.

This method can be used to alter the state before performing serialization. It runs every time when generating a buffer with [`GltfDocument.GenerateBuffer`](GltfDocument.md) or writing to the file system with [`GltfDocument.WriteToFilesystem`](GltfDocument.md).

### _GenerateSceneNode

```csharp
public virtual Node3D _GenerateSceneNode(GltfState state, GltfNode gltfNode, Node sceneParent)
```

Part of the import process. This method is run after `_ImportPreGenerate()` and before `_ImportNode()`.

Runs when generating a Godot scene node from a GLTFNode. The returned node will be added to the scene tree. Multiple nodes can be generated in this step if they are added as a child of the returned node.

**Note:** The `sceneParent` parameter may be `null` if this is the single root node.

### _GetImageFileExtension

```csharp
public virtual string _GetImageFileExtension()
```

Returns the file extension to use for saving image data into, for example, `".png"`. If defined, when this extension is used to handle images, and the images are saved to a separate file, the image bytes will be copied to a file with this extension. If this is set, there should be a [`ResourceImporter`](ResourceImporter.md) class able to import the file. If not defined or empty, Godot will save the image into a PNG file.

### _GetSaveableImageFormats

```csharp
public virtual string[] _GetSaveableImageFormats()
```

Part of the export process. This method is run after `_ConvertSceneNode()` and before `_ExportNode()`.

Returns an array of the image formats that can be saved/exported by this extension. This extension will only be selected as the image exporter if the [`GltfDocument`](GltfDocument.md)'s [`GltfDocument.ImageFormat`](GltfDocument.md) is in this array. If this `GltfDocumentExtension` is selected as the image exporter, one of the `_SaveImageAtPath()` or `_SerializeImageToBytes()` methods will run next, otherwise `_ExportNode()` will run next. If the format name contains `"Lossy"`, the lossy quality slider will be displayed.

### _GetSupportedExtensions

```csharp
public virtual string[] _GetSupportedExtensions()
```

Part of the import process. This method is run after `_ImportPreflight()` and before `_ParseNodeExtensions()`.

Returns an array of the glTF extensions supported by this GLTFDocumentExtension class. This is used to validate if a glTF file with required extensions can be loaded.

### _ImportNode

```csharp
public virtual Error _ImportNode(GltfState state, GltfNode gltfNode, Dictionary json, Node node)
```

Part of the import process. This method is run after `_GenerateSceneNode()` and before `_ImportPost()`.

This method can be used to make modifications to each of the generated Godot scene nodes.

### _ImportObjectModelProperty

```csharp
public virtual GltfObjectModelProperty _ImportObjectModelProperty(GltfState state, string[] splitJsonPointer, Array<NodePath> partialPaths)
```

Part of the import process. Allows GLTFDocumentExtension classes to provide mappings for JSON pointers to glTF properties, as defined by the glTF object model, to properties of nodes in the Godot scene tree.

Returns a [`GltfObjectModelProperty`](GltfObjectModelProperty.md) instance that defines how the property should be mapped. If your extension can't handle the property, return `null` or an instance without any NodePaths (see [`GltfObjectModelProperty.HasNodePaths`](GltfObjectModelProperty.md)). You should use [`GltfObjectModelProperty.SetTypes`](GltfObjectModelProperty.md) to set the types, and [`GltfObjectModelProperty.AppendPathToProperty`](GltfObjectModelProperty.md) function is useful for most simple cases.

In many cases, `partialPaths` will contain the start of a path, allowing the extension to complete the path. For example, for `/nodes/3/extensions/MY_ext/prop`, Godot will pass you a NodePath that leads to node 3, so the GLTFDocumentExtension class only needs to resolve the last `MY_ext/prop` part of the path. In this example, the extension should check `split.size() > 4 and split[0] == "nodes" and split[2] == "extensions" and split[3] == "MY_ext"` at the start of the function to check if this JSON pointer applies to it, then it can use `partialPaths` and handle `split[4]`.

### _ImportPost

```csharp
public virtual Error _ImportPost(GltfState state, Node root)
```

Part of the import process. This method is run last, after all other parts of the import process.

This method can be used to modify the final Godot scene generated by the import process.

### _ImportPostParse

```csharp
public virtual Error _ImportPostParse(GltfState state)
```

Part of the import process. This method is run after `_ParseNodeExtensions()` and before `_ImportPreGenerate()`.

This method can be used to modify any of the data imported so far after parsing each node, but before generating the scene or any of its nodes.

### _ImportPreGenerate

```csharp
public virtual Error _ImportPreGenerate(GltfState state)
```

Part of the import process. This method is run after `_ImportPostParse()` and before `_GenerateSceneNode()`.

This method can be used to modify or read from any of the processed data structures, before generating the nodes and then running the final per-node import step.

### _ImportPreflight

```csharp
public virtual Error _ImportPreflight(GltfState state, string[] extensions)
```

Part of the import process. This method is run first, before all other parts of the import process.

The return value is used to determine if this `GltfDocumentExtension` instance should be used for importing a given glTF file. If [`Error.Ok`](Error.md), the import will use this `GltfDocumentExtension` instance. If not overridden, [`Error.Ok`](Error.md) is returned.

### _ParseImageData

```csharp
public virtual Error _ParseImageData(GltfState state, byte[] imageData, string mimeType, Image retImage)
```

Part of the import process. This method is run after `_ParseNodeExtensions()` and before `_ParseTextureJson()`.

Runs when parsing image data from a glTF file. The data could be sourced from a separate file, a URI, or a buffer, and then is passed as a byte array.

### _ParseNodeExtensions

```csharp
public virtual Error _ParseNodeExtensions(GltfState state, GltfNode gltfNode, Dictionary extensions)
```

Part of the import process. This method is run after `_GetSupportedExtensions()` and before `_ImportPostParse()`.

Runs when parsing the node extensions of a GLTFNode. This method can be used to process the extension JSON data into a format that can be used by `_GenerateSceneNode()`. The return value should be a member of the [`Error`](Error.md) enum.

### _ParseTextureJson

```csharp
public virtual Error _ParseTextureJson(GltfState state, Dictionary textureJson, GltfTexture retGltfTexture)
```

Part of the import process. This method is run after `_ParseImageData()` and before `_GenerateSceneNode()`.

Runs when parsing the texture JSON from the glTF textures array. This can be used to set the source image index to use as the texture.

### _SaveImageAtPath

```csharp
public virtual Error _SaveImageAtPath(GltfState state, Image image, string filePath, string imageFormat, float lossyQuality)
```

Part of the export process. This method is run after `_GetSaveableImageFormats()` and before `_SerializeTextureJson()`.

This method is run when saving images separately from the glTF file. When images are embedded, `_SerializeImageToBytes()` runs instead. Note that these methods only run when this `GltfDocumentExtension` is selected as the image exporter.

### _SerializeImageToBytes

```csharp
public virtual byte[] _SerializeImageToBytes(GltfState state, Image image, Dictionary imageDict, string imageFormat, float lossyQuality)
```

Part of the export process. This method is run after `_GetSaveableImageFormats()` and before `_SerializeTextureJson()`.

This method is run when embedding images in the glTF file. When images are saved separately, `_SaveImageAtPath()` runs instead. Note that these methods only run when this `GltfDocumentExtension` is selected as the image exporter.

This method must set the image MIME type in the `imageDict` with the `"mimeType"` key. For example, for a PNG image, it would be set to `"image/png"`. The return value must be a `System.Byte`[] containing the image data.

### _SerializeTextureJson

```csharp
public virtual Error _SerializeTextureJson(GltfState state, Dictionary textureJson, GltfTexture gltfTexture, string imageFormat)
```

Part of the export process. This method is run after `_SaveImageAtPath()` or `_SerializeImageToBytes()`, and before `_ExportNode()`. Note that this method only runs when this `GltfDocumentExtension` is selected as the image exporter.

This method can be used to set up the extensions for the texture JSON by editing `textureJson`. The extension must also be added as used extension with [`GltfState.AddUsedExtension`](GltfState.md), be sure to set `required` to `true` if you are not providing a fallback.
