<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GltfSpecGloss

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

KHR_materials_pbrSpecularGlossiness is an archived glTF extension. This means that it is deprecated and not recommended for new files. However, it is still supported for loading old files.

## 构造函数

### GltfSpecGloss

```csharp
public GltfSpecGloss()
```

## 属性

### DiffuseFactor

```csharp
public Color DiffuseFactor { get; set; }
```

The reflected diffuse factor of the material.

### DiffuseImg

```csharp
public Image DiffuseImg { get; set; }
```

The diffuse texture.

### GlossFactor

```csharp
public float GlossFactor { get; set; }
```

The glossiness or smoothness of the material.

### SpecGlossImg

```csharp
public Image SpecGlossImg { get; set; }
```

The specular-glossiness texture.

### SpecularFactor

```csharp
public Color SpecularFactor { get; set; }
```

The specular RGB color of the material. The alpha channel is unused.

## 方法

### GetDiffuseFactor

```csharp
public Color GetDiffuseFactor()
```

### GetDiffuseImg

```csharp
public Image GetDiffuseImg()
```

### GetGlossFactor

```csharp
public float GetGlossFactor()
```

### GetSpecGlossImg

```csharp
public Image GetSpecGlossImg()
```

### GetSpecularFactor

```csharp
public Color GetSpecularFactor()
```

### SetDiffuseFactor

```csharp
public void SetDiffuseFactor(Color diffuseFactor)
```

### SetDiffuseImg

```csharp
public void SetDiffuseImg(Image diffuseImg)
```

### SetGlossFactor

```csharp
public void SetGlossFactor(float glossFactor)
```

### SetSpecGlossImg

```csharp
public void SetSpecGlossImg(Image specGlossImg)
```

### SetSpecularFactor

```csharp
public void SetSpecularFactor(Color specularFactor)
```
