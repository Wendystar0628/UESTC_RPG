<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ResourceImporterTextureAtlas

命名空间 `Godot`；class；继承 [`ResourceImporter`](ResourceImporter.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

This imports a collection of textures from a PNG image into an [`AtlasTexture`](AtlasTexture.md) or 2D [`ArrayMesh`](ArrayMesh.md). This can be used to save memory when importing 2D animations from spritesheets. Texture atlases are only supported in 2D rendering, not 3D. See also [`ResourceImporterTexture`](ResourceImporterTexture.md) and [`ResourceImporterLayeredTexture`](ResourceImporterLayeredTexture.md).

**Note:** `ResourceImporterTextureAtlas` does not handle importing [`TileSetAtlasSource`](TileSetAtlasSource.md), which is created using the [`TileSet`](TileSet.md) editor instead.

## 构造函数

### ResourceImporterTextureAtlas

```csharp
public ResourceImporterTextureAtlas()
```
