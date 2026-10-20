<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GltfTextureSampler

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Represents a texture sampler as defined by the base glTF spec. Texture samplers in glTF specify how to sample data from the texture's base image, when rendering the texture on an object.

## 构造函数

### GltfTextureSampler

```csharp
public GltfTextureSampler()
```

## 属性

### MagFilter

```csharp
public int MagFilter { get; set; }
```

Texture's magnification filter, used when texture appears larger on screen than the source image.

### MinFilter

```csharp
public int MinFilter { get; set; }
```

Texture's minification filter, used when the texture appears smaller on screen than the source image.

### WrapS

```csharp
public int WrapS { get; set; }
```

Wrapping mode to use for S-axis (horizontal) texture coordinates.

### WrapT

```csharp
public int WrapT { get; set; }
```

Wrapping mode to use for T-axis (vertical) texture coordinates.

## 方法

### GetMagFilter

```csharp
public int GetMagFilter()
```

### GetMinFilter

```csharp
public int GetMinFilter()
```

### GetWrapS

```csharp
public int GetWrapS()
```

### GetWrapT

```csharp
public int GetWrapT()
```

### SetMagFilter

```csharp
public void SetMagFilter(int filterMode)
```

### SetMinFilter

```csharp
public void SetMinFilter(int filterMode)
```

### SetWrapS

```csharp
public void SetWrapS(int wrapMode)
```

### SetWrapT

```csharp
public void SetWrapT(int wrapMode)
```
