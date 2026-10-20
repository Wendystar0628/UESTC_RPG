<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RenderSceneBuffersExtension

命名空间 `Godot`；class；继承 [`RenderSceneBuffers`](RenderSceneBuffers.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class allows for a RenderSceneBuffer implementation to be made in GDExtension.

## 构造函数

### RenderSceneBuffersExtension

```csharp
public RenderSceneBuffersExtension()
```

## 方法

### _Configure

```csharp
public virtual void _Configure(RenderSceneBuffersConfiguration config)
```

引擎名：`_configure`

Implement this in GDExtension to handle the (re)sizing of a viewport.

### _SetAnisotropicFilteringLevel

```csharp
public virtual void _SetAnisotropicFilteringLevel(int anisotropicFilteringLevel)
```

引擎名：`_set_anisotropic_filtering_level`

Implement this in GDExtension to change the anisotropic filtering level.

### _SetFsrSharpness

```csharp
public virtual void _SetFsrSharpness(float fsrSharpness)
```

引擎名：`_set_fsr_sharpness`

Implement this in GDExtension to record a new FSR sharpness value.

### _SetTextureMipmapBias

```csharp
public virtual void _SetTextureMipmapBias(float textureMipmapBias)
```

引擎名：`_set_texture_mipmap_bias`

Implement this in GDExtension to change the texture mipmap bias.

### _SetUseDebanding

```csharp
public virtual void _SetUseDebanding(bool useDebanding)
```

引擎名：`_set_use_debanding`

Implement this in GDExtension to react to the debanding flag changing.
