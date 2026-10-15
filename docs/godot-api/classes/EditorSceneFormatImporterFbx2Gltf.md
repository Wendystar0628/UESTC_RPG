<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorSceneFormatImporterFbx2Gltf

命名空间 `Godot`；class；继承 [`EditorSceneFormatImporter`](EditorSceneFormatImporter.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

Imports Autodesk FBX 3D scenes by way of converting them to glTF 2.0 using the FBX2glTF command line tool.

The location of the FBX2glTF binary is set via the `EditorSettings.filesystem/import/fbx/fbx2gltf_path` editor setting.

This importer is only used if `ProjectSettings.filesystem/import/fbx2gltf/enabled` is set to `true`.

## 构造函数

### EditorSceneFormatImporterFbx2Gltf

```csharp
public EditorSceneFormatImporterFbx2Gltf()
```
