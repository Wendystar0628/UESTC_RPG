<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Xrvrs

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

This class is used by various XR interfaces to generate VRS textures that can be used to speed up rendering.

## 构造函数

### Xrvrs

```csharp
public Xrvrs()
```

## 属性

### VrsMinRadius

```csharp
public float VrsMinRadius { get; set; }
```

The minimum radius around the focal point where full quality is guaranteed if VRS is used as a percentage of screen size.

### VrsRenderRegion

```csharp
public Rect2I VrsRenderRegion { get; set; }
```

The render region that the VRS texture will be scaled to when generated.

### VrsStrength

```csharp
public float VrsStrength { get; set; }
```

The strength used to calculate the VRS density map. The greater this value, the more noticeable VRS is.

## 方法

### GetVrsMinRadius

```csharp
public float GetVrsMinRadius()
```

### GetVrsRenderRegion

```csharp
public Rect2I GetVrsRenderRegion()
```

### GetVrsStrength

```csharp
public float GetVrsStrength()
```

### MakeVrsTexture

```csharp
public Rid MakeVrsTexture(Vector2 targetSize, ReadOnlySpan<Vector2> eyeFoci)
```

Generates the VRS texture based on a render `targetSize` adjusted by our VRS tile size. For each eyes focal point passed in `eyeFoci` a layer is created. Focal point should be in NDC.

The result will be cached, requesting a VRS texture with unchanged parameters and settings will return the cached RID.

### MakeVrsTexture

```csharp
public Rid MakeVrsTexture(Vector2 targetSize, Vector2[] eyeFoci)
```

Generates the VRS texture based on a render `targetSize` adjusted by our VRS tile size. For each eyes focal point passed in `eyeFoci` a layer is created. Focal point should be in NDC.

The result will be cached, requesting a VRS texture with unchanged parameters and settings will return the cached RID.

### SetVrsMinRadius

```csharp
public void SetVrsMinRadius(float radius)
```

### SetVrsRenderRegion

```csharp
public void SetVrsRenderRegion(Rect2I renderRegion)
```

### SetVrsStrength

```csharp
public void SetVrsStrength(float strength)
```
