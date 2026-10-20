<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TextureLayeredRD

命名空间 `Godot`；class；继承 [`TextureLayered`](TextureLayered.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Base class for [`Texture2DArrayRD`](Texture2DArrayRD.md), [`TextureCubemapRD`](TextureCubemapRD.md) and [`TextureCubemapArrayRD`](TextureCubemapArrayRD.md). Cannot be used directly, but contains all the functions necessary for accessing the derived resource types.

**Note:** `TextureLayeredRD` is intended for low-level usage with [`RenderingDevice`](RenderingDevice.md). For most use cases, use [`TextureLayered`](TextureLayered.md) instead.

## 属性

### TextureRdRid

```csharp
public Rid TextureRdRid { get; set; }
```

引擎名：`texture_rd_rid`

The RID of the texture object created on the [`RenderingDevice`](RenderingDevice.md).

## 方法

### GetTextureRdRid

```csharp
public Rid GetTextureRdRid()
```

引擎名：`get_texture_rd_rid`

### SetTextureRdRid

```csharp
public void SetTextureRdRid(Rid textureRdRid)
```

引擎名：`set_texture_rd_rid`
