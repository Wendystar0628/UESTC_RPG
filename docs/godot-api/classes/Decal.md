<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Decal

命名空间 `Godot`；class；继承 [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`Decal`s are used to project a texture onto a [`Mesh`](Mesh.md) in the scene. Use Decals to add detail to a scene without affecting the underlying [`Mesh`](Mesh.md). They are often used to add weathering to building, add dirt or mud to the ground, or add variety to props. Decals can be moved at any time, making them suitable for things like blob shadows or laser sight dots.

They are made of an [`Aabb`](Aabb.md) and a group of [`Texture2D`](Texture2D.md)s specifying [`Color`](Color.md), normal, ORM (ambient occlusion, roughness, metallic), and emission. Decals are projected within their [`Aabb`](Aabb.md) so altering the orientation of the Decal affects the direction in which they are projected. By default, Decals are projected down (i.e. from positive Y to negative Y).

The [`Texture2D`](Texture2D.md)s associated with the Decal are automatically stored in a texture atlas which is used for drawing the decals so all decals can be drawn at once. Godot uses clustered decals, meaning they are stored in cluster data and drawn when the mesh is drawn, they are not drawn as a post-processing effect after.

**Note:** Decals cannot affect an underlying material's transparency, regardless of its transparency mode (alpha blend, alpha scissor, alpha hash, opaque pre-pass). This means translucent or transparent areas of a material will remain translucent or transparent even if an opaque decal is applied on them.

**Note:** Decals are only supported in the Forward+ and Mobile rendering methods, not Compatibility. When using the Mobile rendering method, only 8 decals can be displayed on each mesh resource. Attempting to display more than 8 decals on a single mesh resource will result in decals flickering in and out as the camera moves.

**Note:** When using the Mobile rendering method, decals will only correctly affect meshes whose visibility AABB intersects with the decal's AABB. If using a shader to deform the mesh in a way that makes it go outside its AABB, [`GeometryInstance3D.ExtraCullMargin`](GeometryInstance3D.md) must be increased on the mesh. Otherwise, the decal may not be visible on the mesh.

## 构造函数

### Decal

```csharp
public Decal()
```

## 属性

### AlbedoMix

```csharp
public float AlbedoMix { get; set; }
```

引擎名：`albedo_mix`

Blends the albedo [`Color`](Color.md) of the decal with albedo [`Color`](Color.md) of the underlying mesh. This can be set to `0.0` to create a decal that only affects normal or ORM. In this case, an albedo texture is still required as its alpha channel will determine where the normal and ORM will be overridden. See also `Modulate`.

### CullMask

```csharp
public uint CullMask { get; set; }
```

引擎名：`cull_mask`

Specifies which [`VisualInstance3D.Layers`](VisualInstance3D.md) this decal will project on. By default, Decals affect all layers. This is used so you can specify which types of objects receive the Decal and which do not. This is especially useful so you can ensure that dynamic objects don't accidentally receive a Decal intended for the terrain under them.

### DistanceFadeBegin

```csharp
public float DistanceFadeBegin { get; set; }
```

引擎名：`distance_fade_begin`

The distance from the camera at which the Decal begins to fade away (in 3D units).

### DistanceFadeEnabled

```csharp
public bool DistanceFadeEnabled { get; set; }
```

引擎名：`distance_fade_enabled`

If `true`, decals will smoothly fade away when far from the active [`Camera3D`](Camera3D.md) starting at `DistanceFadeBegin`. The Decal will fade out over `DistanceFadeBegin` + `DistanceFadeLength`, after which it will be culled and not sent to the shader at all. Use this to reduce the number of active Decals in a scene and thus improve performance.

### DistanceFadeLength

```csharp
public float DistanceFadeLength { get; set; }
```

引擎名：`distance_fade_length`

The distance over which the Decal fades (in 3D units). The Decal becomes slowly more transparent over this distance and is completely invisible at the end. Higher values result in a smoother fade-out transition, which is more suited when the camera moves fast.

### EmissionEnergy

```csharp
public float EmissionEnergy { get; set; }
```

引擎名：`emission_energy`

Energy multiplier for the emission texture. This will make the decal emit light at a higher or lower intensity, independently of the albedo color. See also `Modulate`.

### LowerFade

```csharp
public float LowerFade { get; set; }
```

引擎名：`lower_fade`

Sets the curve over which the decal will fade as the surface gets further from the center of the [`Aabb`](Aabb.md). Only positive values are valid (negative values will be clamped to `0.0`). See also `UpperFade`.

### Modulate

```csharp
public Color Modulate { get; set; }
```

引擎名：`modulate`

Changes the [`Color`](Color.md) of the Decal by multiplying the albedo and emission colors with this value. The alpha component is only taken into account when multiplying the albedo color, not the emission color. See also `EmissionEnergy` and `AlbedoMix` to change the emission and albedo intensity independently of each other.

### NormalFade

```csharp
public float NormalFade { get; set; }
```

引擎名：`normal_fade`

Fades the Decal if the angle between the Decal's [`Aabb`](Aabb.md) and the target surface becomes too large. A value of `0` projects the Decal regardless of angle, a value of `1` limits the Decal to surfaces that are nearly perpendicular.

**Note:** Setting `NormalFade` to a value greater than `0.0` has a small performance cost due to the added normal angle computations.

### Size

```csharp
public Vector3 Size { get; set; }
```

引擎名：`size`

Sets the size of the [`Aabb`](Aabb.md) used by the decal. All dimensions must be set to a value greater than zero (they will be clamped to `0.001` if this is not the case). The AABB goes from `-size/2` to `size/2`.

**Note:** To improve culling efficiency of "hard surface" decals, set their `UpperFade` and `LowerFade` to `0.0` and set the Y component of the `Size` as low as possible. This will reduce the decals' AABB size without affecting their appearance.

### TextureAlbedo

```csharp
public Texture2D TextureAlbedo { get; set; }
```

引擎名：`texture_albedo`

[`Texture2D`](Texture2D.md) with the base [`Color`](Color.md) of the Decal. Either this or the `TextureEmission` must be set for the Decal to be visible. Use the alpha channel like a mask to smoothly blend the edges of the decal with the underlying object.

**Note:** Unlike [`BaseMaterial3D`](BaseMaterial3D.md) whose filter mode can be adjusted on a per-material basis, the filter mode for `Decal` textures is set globally with `ProjectSettings.rendering/textures/decals/filter`.

### TextureEmission

```csharp
public Texture2D TextureEmission { get; set; }
```

引擎名：`texture_emission`

[`Texture2D`](Texture2D.md) with the emission [`Color`](Color.md) of the Decal. Either this or the `TextureAlbedo` must be set for the Decal to be visible. Use the alpha channel like a mask to smoothly blend the edges of the decal with the underlying object.

**Note:** Unlike [`BaseMaterial3D`](BaseMaterial3D.md) whose filter mode can be adjusted on a per-material basis, the filter mode for `Decal` textures is set globally with `ProjectSettings.rendering/textures/decals/filter`.

### TextureNormal

```csharp
public Texture2D TextureNormal { get; set; }
```

引擎名：`texture_normal`

[`Texture2D`](Texture2D.md) with the per-pixel normal map for the decal. Use this to add extra detail to decals.

**Note:** Unlike [`BaseMaterial3D`](BaseMaterial3D.md) whose filter mode can be adjusted on a per-material basis, the filter mode for `Decal` textures is set globally with `ProjectSettings.rendering/textures/decals/filter`.

**Note:** Setting this texture alone will not result in a visible decal, as `TextureAlbedo` must also be set. To create a normal-only decal, load an albedo texture into `TextureAlbedo` and set `AlbedoMix` to `0.0`. The albedo texture's alpha channel will be used to determine where the underlying surface's normal map should be overridden (and its intensity).

### TextureOrm

```csharp
public Texture2D TextureOrm { get; set; }
```

引擎名：`texture_orm`

[`Texture2D`](Texture2D.md) storing ambient occlusion, roughness, and metallic for the decal. Use this to add extra detail to decals.

**Note:** Unlike [`BaseMaterial3D`](BaseMaterial3D.md) whose filter mode can be adjusted on a per-material basis, the filter mode for `Decal` textures is set globally with `ProjectSettings.rendering/textures/decals/filter`.

**Note:** Setting this texture alone will not result in a visible decal, as `TextureAlbedo` must also be set. To create an ORM-only decal, load an albedo texture into `TextureAlbedo` and set `AlbedoMix` to `0.0`. The albedo texture's alpha channel will be used to determine where the underlying surface's ORM map should be overridden (and its intensity).

**Note:** Due to technical limitations, modifying the underlying surface's roughness using `TextureOrm` does *not* affect screen-space reflections ([`Environment.SsrEnabled`](Environment.md)), reflections from [`VoxelGI`](VoxelGI.md), and reflections from SDFGI ([`Environment.SdfgiEnabled`](Environment.md)). Only reflections from [`ReflectionProbe`](ReflectionProbe.md)s are affected.

### UpperFade

```csharp
public float UpperFade { get; set; }
```

引擎名：`upper_fade`

Sets the curve over which the decal will fade as the surface gets further from the center of the [`Aabb`](Aabb.md). Only positive values are valid (negative values will be clamped to `0.0`). See also `LowerFade`.

## 方法

### GetAlbedoMix

```csharp
public float GetAlbedoMix()
```

引擎名：`get_albedo_mix`

### GetCullMask

```csharp
public uint GetCullMask()
```

引擎名：`get_cull_mask`

### GetDistanceFadeBegin

```csharp
public float GetDistanceFadeBegin()
```

引擎名：`get_distance_fade_begin`

### GetDistanceFadeLength

```csharp
public float GetDistanceFadeLength()
```

引擎名：`get_distance_fade_length`

### GetEmissionEnergy

```csharp
public float GetEmissionEnergy()
```

引擎名：`get_emission_energy`

### GetLowerFade

```csharp
public float GetLowerFade()
```

引擎名：`get_lower_fade`

### GetModulate

```csharp
public Color GetModulate()
```

引擎名：`get_modulate`

### GetNormalFade

```csharp
public float GetNormalFade()
```

引擎名：`get_normal_fade`

### GetSize

```csharp
public Vector3 GetSize()
```

引擎名：`get_size`

### GetTexture

```csharp
public Texture2D GetTexture(Decal.DecalTexture type)
```

引擎名：`get_texture`

Returns the [`Texture2D`](Texture2D.md) associated with the specified [`Decal.DecalTexture`](Decal.md). This is a convenience method, in most cases you should access the texture directly.

For example, instead of `albedo_tex = $Decal.get_texture(Decal.TEXTURE_ALBEDO)`, use `albedo_tex = $Decal.texture_albedo`.

One case where this is better than accessing the texture directly is when you want to copy one Decal's textures to another. For example:

```csharp
for (int i = 0; i < (int)Decal.DecalTexture.Max; i++)
{
GetNode<Decal>("NewDecal").SetTexture(i, GetNode<Decal>("OldDecal").GetTexture(i));
}

```

### GetUpperFade

```csharp
public float GetUpperFade()
```

引擎名：`get_upper_fade`

### IsDistanceFadeEnabled

```csharp
public bool IsDistanceFadeEnabled()
```

引擎名：`is_distance_fade_enabled`

### SetAlbedoMix

```csharp
public void SetAlbedoMix(float energy)
```

引擎名：`set_albedo_mix`

### SetCullMask

```csharp
public void SetCullMask(uint mask)
```

引擎名：`set_cull_mask`

### SetDistanceFadeBegin

```csharp
public void SetDistanceFadeBegin(float distance)
```

引擎名：`set_distance_fade_begin`

### SetDistanceFadeLength

```csharp
public void SetDistanceFadeLength(float distance)
```

引擎名：`set_distance_fade_length`

### SetEmissionEnergy

```csharp
public void SetEmissionEnergy(float energy)
```

引擎名：`set_emission_energy`

### SetEnableDistanceFade

```csharp
public void SetEnableDistanceFade(bool enable)
```

引擎名：`set_enable_distance_fade`

### SetLowerFade

```csharp
public void SetLowerFade(float fade)
```

引擎名：`set_lower_fade`

### SetModulate

```csharp
public void SetModulate(Color color)
```

引擎名：`set_modulate`

### SetNormalFade

```csharp
public void SetNormalFade(float fade)
```

引擎名：`set_normal_fade`

### SetSize

```csharp
public void SetSize(Vector3 size)
```

引擎名：`set_size`

### SetTexture

```csharp
public void SetTexture(Decal.DecalTexture type, Texture2D texture)
```

引擎名：`set_texture`

Sets the [`Texture2D`](Texture2D.md) associated with the specified [`Decal.DecalTexture`](Decal.md). This is a convenience method, in most cases you should access the texture directly.

For example, instead of `$Decal.set_texture(Decal.TEXTURE_ALBEDO, albedo_tex)`, use `$Decal.texture_albedo = albedo_tex`.

One case where this is better than accessing the texture directly is when you want to copy one Decal's textures to another. For example:

```csharp
for (int i = 0; i < (int)Decal.DecalTexture.Max; i++)
{
GetNode<Decal>("NewDecal").SetTexture(i, GetNode<Decal>("OldDecal").GetTexture(i));
}

```

### SetUpperFade

```csharp
public void SetUpperFade(float fade)
```

引擎名：`set_upper_fade`

## DecalTexture

命名空间 `Godot`；enum

### 枚举值

- `Albedo = 0`：[`Texture2D`](Texture2D.md) corresponding to [`Decal.TextureAlbedo`](Decal.md).
- `Normal = 1`：[`Texture2D`](Texture2D.md) corresponding to [`Decal.TextureNormal`](Decal.md).
- `Orm = 2`：[`Texture2D`](Texture2D.md) corresponding to [`Decal.TextureOrm`](Decal.md).
- `Emission = 3`：[`Texture2D`](Texture2D.md) corresponding to [`Decal.TextureEmission`](Decal.md).
- `Max = 4`：Max size of `Decal.DecalTexture` enum.
