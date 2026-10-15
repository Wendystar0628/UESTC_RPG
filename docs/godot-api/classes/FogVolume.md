<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# FogVolume

命名空间 `Godot`；class；继承 [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`FogVolume`s are used to add localized fog into the global volumetric fog effect. `FogVolume`s can also remove volumetric fog from specific areas if using a [`FogMaterial`](FogMaterial.md) with a negative [`FogMaterial.Density`](FogMaterial.md).

Performance of `FogVolume`s is directly related to their relative size on the screen and the complexity of their attached [`FogMaterial`](FogMaterial.md). It is best to keep `FogVolume`s relatively small and simple where possible.

**Note:** `FogVolume`s only have a visible effect if [`Environment.VolumetricFogEnabled`](Environment.md) is `true`. If you don't want fog to be globally visible (but only within `FogVolume` nodes), set [`Environment.VolumetricFogDensity`](Environment.md) to `0.0`.

## 构造函数

### FogVolume

```csharp
public FogVolume()
```

## 属性

### Material

```csharp
public Material Material { get; set; }
```

引擎名：`material`

The [`Material`](Material.md) used by the `FogVolume`. Can be either a built-in [`FogMaterial`](FogMaterial.md) or a custom [`ShaderMaterial`](ShaderMaterial.md).

### Shape

```csharp
public RenderingServer.FogVolumeShape Shape { get; set; }
```

引擎名：`shape`

The shape of the `FogVolume`. This can be set to either [`RenderingServer.FogVolumeShape.Ellipsoid`](RenderingServer.md), [`RenderingServer.FogVolumeShape.Cone`](RenderingServer.md), [`RenderingServer.FogVolumeShape.Cylinder`](RenderingServer.md), [`RenderingServer.FogVolumeShape.Box`](RenderingServer.md) or [`RenderingServer.FogVolumeShape.World`](RenderingServer.md).

### Size

```csharp
public Vector3 Size { get; set; }
```

引擎名：`size`

The size of the `FogVolume` when `Shape` is [`RenderingServer.FogVolumeShape.Ellipsoid`](RenderingServer.md), [`RenderingServer.FogVolumeShape.Cone`](RenderingServer.md), [`RenderingServer.FogVolumeShape.Cylinder`](RenderingServer.md) or [`RenderingServer.FogVolumeShape.Box`](RenderingServer.md).

**Note:** Thin fog volumes may appear to flicker when the camera moves or rotates. This can be alleviated by increasing `ProjectSettings.rendering/environment/volumetric_fog/volume_depth` (at a performance cost) or by decreasing [`Environment.VolumetricFogLength`](Environment.md) (at no performance cost, but at the cost of lower fog range). Alternatively, the `FogVolume` can be made thicker and use a lower density in the `Material`.

**Note:** If `Shape` is [`RenderingServer.FogVolumeShape.Cone`](RenderingServer.md) or [`RenderingServer.FogVolumeShape.Cylinder`](RenderingServer.md), the cone/cylinder will be adjusted to fit within the size. Non-uniform scaling of cone/cylinder shapes via the `Size` property is not supported, but you can scale the `FogVolume` node instead.

## 方法

### GetMaterial

```csharp
public Material GetMaterial()
```

引擎名：`get_material`

### GetShape

```csharp
public RenderingServer.FogVolumeShape GetShape()
```

引擎名：`get_shape`

### GetSize

```csharp
public Vector3 GetSize()
```

引擎名：`get_size`

### SetMaterial

```csharp
public void SetMaterial(Material material)
```

引擎名：`set_material`

### SetShape

```csharp
public void SetShape(RenderingServer.FogVolumeShape shape)
```

引擎名：`set_shape`

### SetSize

```csharp
public void SetSize(Vector3 size)
```

引擎名：`set_size`
