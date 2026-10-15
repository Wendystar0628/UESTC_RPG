<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GltfDocument

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

GLTFDocument supports reading data from a glTF file, buffer, or Godot scene. This data can then be written to the filesystem, buffer, or used to create a Godot scene.

All of the data in a glTF scene is stored in the [`GltfState`](GltfState.md) class. GLTFDocument processes state objects, but does not contain any scene data itself. GLTFDocument has member variables to store export configuration settings such as the image format, but is otherwise stateless. Multiple scenes can be processed with the same settings using the same GLTFDocument object and different [`GltfState`](GltfState.md) objects.

GLTFDocument can be extended with arbitrary functionality by extending the [`GltfDocumentExtension`](GltfDocumentExtension.md) class and registering it with GLTFDocument via `RegisterGltfDocumentExtension()`. This allows for custom data to be imported and exported.

## 构造函数

### GltfDocument

```csharp
public GltfDocument()
```

## 属性

### FallbackImageFormat

```csharp
public string FallbackImageFormat { get; set; }
```

The user-friendly name of the fallback image format. This is used when exporting the glTF file, including writing to a file and writing to a byte array.

This property may only be one of "None", "PNG", or "JPEG", and is only used when the `ImageFormat` is not one of "None", "PNG", or "JPEG". If having multiple extension image formats is desired, that can be done using a [`GltfDocumentExtension`](GltfDocumentExtension.md) class - this property only covers the use case of providing a base glTF fallback image when using a custom image format.

### FallbackImageQuality

```csharp
public float FallbackImageQuality { get; set; }
```

The quality of the fallback image, if any. For PNG files, this downscales the image on both dimensions by this factor. For JPEG files, this is the lossy quality of the image. A low value is recommended, since including multiple high quality images in a glTF file defeats the file size gains of using a more efficient image format.

### ImageFormat

```csharp
public string ImageFormat { get; set; }
```

The user-friendly name of the export image format. This is used when exporting the glTF file, including writing to a file and writing to a byte array.

By default, Godot allows the following options: "None", "PNG", "JPEG", "Lossless WebP", and "Lossy WebP". Support for more image formats can be added in [`GltfDocumentExtension`](GltfDocumentExtension.md) classes. A single extension class can provide multiple options for the specific format to use, or even an option that uses multiple formats at once.

### LossyQuality

```csharp
public float LossyQuality { get; set; }
```

If `ImageFormat` is a lossy image format, this determines the lossy quality of the image. On a range of `0.0` to `1.0`, where `0.0` is the lowest quality and `1.0` is the highest quality. A lossy quality of `1.0` is not the same as lossless.

### RootNodeMode

```csharp
public GltfDocument.RootNodeModeEnum RootNodeMode { get; set; }
```

How to process the root node during export. The default and recommended value is [`GltfDocument.RootNodeModeEnum.SingleRoot`](GltfDocument.md).

**Note:** Regardless of how the glTF file is exported, when importing, the root node type and name can be overridden in the scene import settings tab.

### TextureMapMode

```csharp
public GltfDocument.TextureMapModeEnum TextureMapMode { get; set; }
```

How to handle texture maps during import. The default and recommended value is [`GltfDocument.TextureMapModeEnum.RemapToStandardMaterial`](GltfDocument.md), which automatically remaps from glTF's flexible texture map system to the more specific texture map slots in Godot's [`StandardMaterial3D`](StandardMaterial3D.md) class. Alternatively, [`GltfDocument.TextureMapModeEnum.DoNotRemap`](GltfDocument.md) can be used to preserve the original texture maps from the glTF file, which may be desirable if using the glTF file with custom shaders, but may not display correctly with Godot's built-in materials.

### VisibilityMode

```csharp
public GltfDocument.VisibilityModeEnum VisibilityMode { get; set; }
```

How to deal with node visibility during export. This setting does nothing if all nodes are visible. The default and recommended value is [`GltfDocument.VisibilityModeEnum.IncludeRequired`](GltfDocument.md), which uses the `KHR_node_visibility` extension.

## 方法

### AppendFromBuffer

```csharp
public Error AppendFromBuffer(ReadOnlySpan<byte> bytes, string basePath, GltfState state, uint flags)
```

Takes a `System.Byte`[] defining a glTF and imports the data to the given [`GltfState`](GltfState.md) object through the `state` parameter.

**Note:** The `basePath` tells `AppendFromBuffer()` where to find dependencies and can be empty.

### AppendFromBuffer

```csharp
public Error AppendFromBuffer(byte[] bytes, string basePath, GltfState state, uint flags = 0)
```

Takes a `System.Byte`[] defining a glTF and imports the data to the given [`GltfState`](GltfState.md) object through the `state` parameter.

**Note:** The `basePath` tells `AppendFromBuffer()` where to find dependencies and can be empty.

### AppendFromFile

```csharp
public Error AppendFromFile(string path, GltfState state, uint flags = 0, string basePath = "")
```

Takes a path to a glTF file and imports the data at that file path to the given [`GltfState`](GltfState.md) object through the `state` parameter.

**Note:** The `basePath` tells `AppendFromFile()` where to find dependencies and can be empty.

### AppendFromScene

```csharp
public Error AppendFromScene(Node node, GltfState state, uint flags = 0)
```

Takes a Godot Engine scene node and exports it and its descendants to the given [`GltfState`](GltfState.md) object through the `state` parameter.

### ExportObjectModelProperty

```csharp
public static GltfObjectModelProperty ExportObjectModelProperty(GltfState state, NodePath nodePath, Node godotNode, int gltfNodeIndex)
```

Determines a mapping between the given Godot `nodePath` and the corresponding glTF Object Model JSON pointer(s) in the generated glTF file. The details of this mapping are returned in a [`GltfObjectModelProperty`](GltfObjectModelProperty.md) object. Additional mappings can be supplied via the [`GltfDocumentExtension._ImportObjectModelProperty()`](GltfDocumentExtension.md) callback method.

### GenerateBuffer

```csharp
public byte[] GenerateBuffer(GltfState state)
```

Takes a [`GltfState`](GltfState.md) object through the `state` parameter and returns a glTF `System.Byte`[].

### GenerateScene

```csharp
public Node GenerateScene(GltfState state, float bakeFps = 30f, bool trimming = false, bool removeImmutableTracks = true)
```

Takes a [`GltfState`](GltfState.md) object through the `state` parameter and returns a Godot Engine scene node.

The `bakeFps` parameter overrides the bake_fps in `state`.

### GetFallbackImageFormat

```csharp
public string GetFallbackImageFormat()
```

### GetFallbackImageQuality

```csharp
public float GetFallbackImageQuality()
```

### GetImageFormat

```csharp
public string GetImageFormat()
```

### GetLossyQuality

```csharp
public float GetLossyQuality()
```

### GetRootNodeMode

```csharp
public GltfDocument.RootNodeModeEnum GetRootNodeMode()
```

### GetSupportedGltfExtensions

```csharp
public static string[] GetSupportedGltfExtensions()
```

Returns a list of all support glTF extensions, including extensions supported directly by the engine, and extensions supported by user plugins registering [`GltfDocumentExtension`](GltfDocumentExtension.md) classes.

**Note:** If this method is run before a GLTFDocumentExtension is registered, its extensions won't be included in the list. Be sure to only run this method after all extensions are registered. If you run this when the engine starts, consider waiting a frame before calling this method to ensure all extensions are registered.

### GetTextureMapMode

```csharp
public GltfDocument.TextureMapModeEnum GetTextureMapMode()
```

### GetVisibilityMode

```csharp
public GltfDocument.VisibilityModeEnum GetVisibilityMode()
```

### ImportObjectModelProperty

```csharp
public static GltfObjectModelProperty ImportObjectModelProperty(GltfState state, string jsonPointer)
```

Determines a mapping between the given glTF Object Model `jsonPointer` and the corresponding Godot node path(s) in the generated Godot scene. The details of this mapping are returned in a [`GltfObjectModelProperty`](GltfObjectModelProperty.md) object. Additional mappings can be supplied via the [`GltfDocumentExtension._ExportObjectModelProperty()`](GltfDocumentExtension.md) callback method.

### RegisterGltfDocumentExtension

```csharp
public static void RegisterGltfDocumentExtension(GltfDocumentExtension extension, bool firstPriority = false)
```

Registers the given [`GltfDocumentExtension`](GltfDocumentExtension.md) instance with GLTFDocument. If `firstPriority` is `true`, this extension will be run first. Otherwise, it will be run last.

**Note:** Like GLTFDocument itself, all GLTFDocumentExtension classes must be stateless in order to function properly. If you need to store data, use the `set_additional_data` and `get_additional_data` methods in [`GltfState`](GltfState.md) or [`GltfNode`](GltfNode.md).

### SetFallbackImageFormat

```csharp
public void SetFallbackImageFormat(string fallbackImageFormat)
```

### SetFallbackImageQuality

```csharp
public void SetFallbackImageQuality(float fallbackImageQuality)
```

### SetImageFormat

```csharp
public void SetImageFormat(string imageFormat)
```

### SetLossyQuality

```csharp
public void SetLossyQuality(float lossyQuality)
```

### SetRootNodeMode

```csharp
public void SetRootNodeMode(GltfDocument.RootNodeModeEnum rootNodeMode)
```

### SetTextureMapMode

```csharp
public void SetTextureMapMode(GltfDocument.TextureMapModeEnum textureMapMode)
```

### SetVisibilityMode

```csharp
public void SetVisibilityMode(GltfDocument.VisibilityModeEnum visibilityMode)
```

### UnregisterGltfDocumentExtension

```csharp
public static void UnregisterGltfDocumentExtension(GltfDocumentExtension extension)
```

Unregisters the given [`GltfDocumentExtension`](GltfDocumentExtension.md) instance.

### WriteToFilesystem

```csharp
public Error WriteToFilesystem(GltfState state, string path)
```

Takes a [`GltfState`](GltfState.md) object through the `state` parameter and writes a glTF file to the filesystem.

**Note:** The extension of the glTF file determines if it is a .glb binary file or a .gltf text file.

## ImportFlags

命名空间 `Godot`；enum

### 枚举值

- `GenerateTangentArrays = 8`：If `true`, generate vertex tangents using [Mikktspace](http://www.mikktspace.com/) if the input meshes don't have tangent data.
- `UseNamedSkinBinds = 16`：If checked, use named [`Skin`](Skin.md)s for animation.
- `DiscardMeshesAndMaterials = 32`：Ignore meshes and materials on import.
- `ForceDisableMeshCompression = 64`：If `true`, mesh compression will not be used.

## RootNodeModeEnum

命名空间 `Godot`；enum

### 枚举值

- `SingleRoot = 0`：Treat the Godot scene's root node as the root node of the glTF file, and mark it as the single root node via the `GODOT_single_root` glTF extension.
- `KeepRoot = 1`：Treat the Godot scene's root node as the root node of the glTF file, but do not mark it as anything special.
- `MultiRoot = 2`：Treat the Godot scene's root node as the name of the glTF scene, and add all of its children as root nodes of the glTF file.

## TextureMapModeEnum

命名空间 `Godot`；enum

### 枚举值

- `DoNotRemap = 0`：Import the texture maps in the glTF file as they are, without trying to fit them into specific texture slots suitable for Godot's built-in materials.
- `RemapToStandardMaterial = 1`：Import the texture maps in the glTF file remapped to the most suitable texture slots based on Godot's [`StandardMaterial3D`](StandardMaterial3D.md) class.

## VisibilityModeEnum

命名空间 `Godot`；enum

### 枚举值

- `IncludeRequired = 0`：If the scene contains any non-visible nodes, include them, mark them as non-visible with `KHR_node_visibility`, and require that importers respect their non-visibility.
- `IncludeOptional = 1`：If the scene contains any non-visible nodes, include them, mark them as non-visible with `KHR_node_visibility`, and do not impose any requirements on importers.
- `Exclude = 2`：If the scene contains any non-visible nodes, do not include them in the export.
