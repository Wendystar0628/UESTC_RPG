<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GltfLight

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Represents a light as defined by the `KHR_lights_punctual` glTF extension.

## 构造函数

### GltfLight

```csharp
public GltfLight()
```

## 属性

### Color

```csharp
public Color Color { get; set; }
```

The [`Color`](Color.md) of the light in linear space. Defaults to white. A black color causes the light to have no effect.

This value is linear to match glTF, but will be converted to nonlinear sRGB when creating a Godot [`Light3D`](Light3D.md) node upon import, or converted to linear when exporting a Godot [`Light3D`](Light3D.md) to glTF.

### InnerConeAngle

```csharp
public float InnerConeAngle { get; set; }
```

The inner angle of the cone in a spotlight. Must be less than or equal to the outer cone angle.

Within this angle, the light is at full brightness. Between the inner and outer cone angles, there is a transition from full brightness to zero brightness. When creating a Godot [`SpotLight3D`](SpotLight3D.md), the ratio between the inner and outer cone angles is used to calculate the attenuation of the light.

### Intensity

```csharp
public float Intensity { get; set; }
```

The intensity of the light. This is expressed in candelas (lumens per steradian) for point and spot lights, and lux (lumens per m²) for directional lights. When creating a Godot light, this value is converted to a unitless multiplier.

### LightType

```csharp
public string LightType { get; set; }
```

The type of the light. The values accepted by Godot are "point", "spot", and "directional", which correspond to Godot's [`OmniLight3D`](OmniLight3D.md), [`SpotLight3D`](SpotLight3D.md), and [`DirectionalLight3D`](DirectionalLight3D.md) respectively.

### OuterConeAngle

```csharp
public float OuterConeAngle { get; set; }
```

The outer angle of the cone in a spotlight. Must be greater than or equal to the inner angle.

At this angle, the light drops off to zero brightness. Between the inner and outer cone angles, there is a transition from full brightness to zero brightness. If this angle is a half turn, then the spotlight emits in all directions. When creating a Godot [`SpotLight3D`](SpotLight3D.md), the outer cone angle is used as the angle of the spotlight.

### Range

```csharp
public float Range { get; set; }
```

The range of the light, beyond which the light has no effect. glTF lights with no range defined behave like physical lights (which have infinite range). When creating a Godot light, the range is clamped to `4096.0`.

## 方法

### FromDictionary

```csharp
public static GltfLight FromDictionary(Dictionary dictionary)
```

Creates a new GLTFLight instance by parsing the given [`Collections.Dictionary`](Collections.Dictionary.md).

### FromNode

```csharp
public static GltfLight FromNode(Light3D lightNode)
```

Create a new GLTFLight instance from the given Godot [`Light3D`](Light3D.md) node.

### GetAdditionalData

```csharp
public Variant GetAdditionalData(StringName extensionName)
```

### GetColor

```csharp
public Color GetColor()
```

### GetInnerConeAngle

```csharp
public float GetInnerConeAngle()
```

### GetIntensity

```csharp
public float GetIntensity()
```

### GetLightType

```csharp
public string GetLightType()
```

### GetOuterConeAngle

```csharp
public float GetOuterConeAngle()
```

### GetRange

```csharp
public float GetRange()
```

### SetAdditionalData

```csharp
public void SetAdditionalData(StringName extensionName, Variant additionalData)
```

### SetColor

```csharp
public void SetColor(Color color)
```

### SetInnerConeAngle

```csharp
public void SetInnerConeAngle(float innerConeAngle)
```

### SetIntensity

```csharp
public void SetIntensity(float intensity)
```

### SetLightType

```csharp
public void SetLightType(string lightType)
```

### SetOuterConeAngle

```csharp
public void SetOuterConeAngle(float outerConeAngle)
```

### SetRange

```csharp
public void SetRange(float range)
```

### ToDictionary

```csharp
public Dictionary ToDictionary()
```

Serializes this GLTFLight instance into a [`Collections.Dictionary`](Collections.Dictionary.md).

### ToNode

```csharp
public Light3D ToNode()
```

Converts this GLTFLight instance into a Godot [`Light3D`](Light3D.md) node.
