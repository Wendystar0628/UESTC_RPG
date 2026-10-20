<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Texture3Drd

命名空间 `Godot`；class；继承 [`Texture3D`](Texture3D.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This texture class allows you to use a 3D texture created directly on the [`RenderingDevice`](RenderingDevice.md) as a texture for materials, meshes, etc.

**Note:** `Texture3Drd` is intended for low-level usage with [`RenderingDevice`](RenderingDevice.md). For most use cases, use [`Texture3D`](Texture3D.md) instead.

## 构造函数

### Texture3Drd

```csharp
public Texture3Drd()
```

## 属性

### TextureRdRid

```csharp
public Rid TextureRdRid { get; set; }
```

The RID of the texture object created on the [`RenderingDevice`](RenderingDevice.md).

## 方法

### GetTextureRdRid

```csharp
public Rid GetTextureRdRid()
```

### SetTextureRdRid

```csharp
public void SetTextureRdRid(Rid textureRdRid)
```
