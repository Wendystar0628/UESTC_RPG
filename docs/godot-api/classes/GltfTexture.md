<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GltfTexture

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

## 构造函数

### GltfTexture

```csharp
public GltfTexture()
```

## 属性

### Sampler

```csharp
public int Sampler { get; set; }
```

ID of the texture sampler to use when sampling the image. If -1, then the default texture sampler is used (linear filtering, and repeat wrapping in both axes).

### SrcImage

```csharp
public int SrcImage { get; set; }
```

The index of the image associated with this texture, see [`GltfState.GetImages`](GltfState.md). If -1, then this texture does not have an image assigned.

## 方法

### GetSampler

```csharp
public int GetSampler()
```

### GetSrcImage

```csharp
public int GetSrcImage()
```

### SetSampler

```csharp
public void SetSampler(int sampler)
```

### SetSrcImage

```csharp
public void SetSrcImage(int srcImage)
```
