<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ImageTexture3D

命名空间 `Godot`；class；继承 [`Texture3D`](Texture3D.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`ImageTexture3D` is a 3-dimensional [`ImageTexture`](ImageTexture.md) that has a width, height, and depth. See also [`ImageTextureLayered`](ImageTextureLayered.md).

3D textures are typically used to store density maps for [`FogMaterial`](FogMaterial.md), color correction LUTs for [`Environment`](Environment.md), vector fields for [`GpuParticlesAttractorVectorField3D`](GpuParticlesAttractorVectorField3D.md) and collision maps for [`GpuParticlesCollisionSdf3D`](GpuParticlesCollisionSdf3D.md). 3D textures can also be used in custom shaders.

## 构造函数

### ImageTexture3D

```csharp
public ImageTexture3D()
```

## 属性

### _Images

```csharp
public Array<Image> _Images { get; set; }
```

## 方法

### Create

```csharp
public Error Create(Image.Format format, int width, int height, int depth, bool useMipmaps, Array<Image> data)
```

引擎名：`create`

Creates the `ImageTexture3D` with specified `format`, `width`, `height`, and `depth`. If `useMipmaps` is `true`, generates mipmaps for the `ImageTexture3D`.

### Update

```csharp
public void Update(Array<Image> data)
```

引擎名：`update`

Replaces the texture's existing data with the layers specified in `data`. The size of `data` must match the parameters that were used for `Create()`. In other words, the texture cannot be resized or have its format changed by calling `Update()`.
