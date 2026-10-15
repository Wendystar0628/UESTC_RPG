<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# LightmapProbe

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`LightmapProbe` represents the position of a single manually placed probe for dynamic object lighting with [`LightmapGI`](LightmapGI.md). Lightmap probes affect the lighting of [`GeometryInstance3D`](GeometryInstance3D.md)-derived nodes that have their [`GeometryInstance3D.GIMode`](GeometryInstance3D.md) set to [`GeometryInstance3D.GIModeEnum.Dynamic`](GeometryInstance3D.md).

Typically, [`LightmapGI`](LightmapGI.md) probes are placed automatically by setting [`LightmapGI.GenerateProbesSubdiv`](LightmapGI.md) to a value other than [`LightmapGI.GenerateProbes.Disabled`](LightmapGI.md). By creating `LightmapProbe` nodes before baking lightmaps, you can add more probes in specific areas for greater detail, or disable automatic generation and rely only on manually placed probes instead.

**Note:** `LightmapProbe` nodes that are placed after baking lightmaps are ignored by dynamic objects. You must bake lightmaps again after creating or modifying `LightmapProbe`s for the probes to be effective.

## 构造函数

### LightmapProbe

```csharp
public LightmapProbe()
```
