<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ResourceImporterObj

命名空间 `Godot`；class；继承 [`ResourceImporter`](ResourceImporter.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

Unlike [`ResourceImporterScene`](ResourceImporterScene.md), `ResourceImporterObj` will import a single [`Mesh`](Mesh.md) resource by default instead of importing a [`PackedScene`](PackedScene.md). This makes it easier to use the [`Mesh`](Mesh.md) resource in nodes that expect direct [`Mesh`](Mesh.md) resources, such as [`GridMap`](GridMap.md), [`GpuParticles3D`](GpuParticles3D.md) or [`CpuParticles3D`](CpuParticles3D.md). Note that it is still possible to save mesh resources from 3D scenes using the **Advanced Import Settings** dialog, regardless of the source format.

See also [`ResourceImporterScene`](ResourceImporterScene.md), which is used for more advanced 3D formats such as glTF.

## 构造函数

### ResourceImporterObj

```csharp
public ResourceImporterObj()
```
