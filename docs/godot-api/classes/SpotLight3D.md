<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SpotLight3D

命名空间 `Godot`；class；继承 [`Light3D`](Light3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A Spotlight is a type of [`Light3D`](Light3D.md) node that emits lights in a specific direction, in the shape of a cone. The light is attenuated through the distance. This attenuation can be configured by changing the energy, radius and attenuation parameters of [`Light3D`](Light3D.md).

Light is emitted in the -Z direction of the node's global basis. For an unrotated light, this means that the light is emitted forwards, illuminating the front side of a 3D model (see `Vector3.Forward` and `Vector3.ModelFront`).

**Note:** When using the Mobile rendering method, only 8 spot lights can be displayed on each mesh resource. Attempting to display more than 8 spot lights on a single mesh resource will result in spot lights flickering in and out as the camera moves. When using the Compatibility rendering method, only 8 spot lights can be displayed on each mesh resource by default, but this can be increased by adjusting `ProjectSettings.rendering/limits/opengl/max_lights_per_object`.

**Note:** When using the Mobile or Compatibility rendering methods, spot lights will only correctly affect meshes whose visibility AABB intersects with the light's AABB. If using a shader to deform the mesh in a way that makes it go outside its AABB, [`GeometryInstance3D.ExtraCullMargin`](GeometryInstance3D.md) must be increased on the mesh. Otherwise, the light may not be visible on the mesh.

## 构造函数

### SpotLight3D

```csharp
public SpotLight3D()
```

## 属性

### SpotAngle

```csharp
public float SpotAngle { get; set; }
```

引擎名：`spot_angle`

The spotlight's angle in degrees. This is the angular radius, meaning the angle from the -Z axis, the cone's center, to the edge of the cone. The default angular radius of 45 degrees corresponds to a cone with an angular diameter of 90 degrees.

**Note:** `SpotAngle` is not affected by [`Node3D.Scale`](Node3D.md) (the light's scale or its parent's scale).

### SpotAngleAttenuation

```csharp
public float SpotAngleAttenuation { get; set; }
```

引擎名：`spot_angle_attenuation`

The spotlight's *angular* attenuation curve. See also `SpotAttenuation`.

### SpotAttenuation

```csharp
public float SpotAttenuation { get; set; }
```

引擎名：`spot_attenuation`

Controls the distance attenuation function for spotlights.

A value of `0.0` will maintain a constant brightness through most of the range, but smoothly attenuate the light at the edge of the range. Use a value of `2.0` for physically accurate lights as it results in the proper inverse square attenutation.

**Note:** Setting attenuation to `2.0` or higher may result in distant objects receiving minimal light, even within range. For example, with a range of `4096`, an object at `100` units is attenuated by a factor of `0.0001`. With a default brightness of `1`, the light would not be visible at that distance.

**Note:** Using negative or values higher than `10.0` may lead to unexpected results.

### SpotRange

```csharp
public float SpotRange { get; set; }
```

引擎名：`spot_range`

The maximal range that can be reached by the spotlight. Note that the effectively lit area may appear to be smaller depending on the `SpotAttenuation` in use. No matter the `SpotAttenuation` in use, the light will never reach anything outside this range.

**Note:** `SpotRange` is not affected by [`Node3D.Scale`](Node3D.md) (the light's scale or its parent's scale).
