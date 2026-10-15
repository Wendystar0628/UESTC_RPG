<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CurveXyzTexture

命名空间 `Godot`；class；继承 [`Texture2D`](Texture2D.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A 1D texture where the red, green, and blue color channels correspond to points on 3 unit [`Curve`](Curve.md) resources. Compared to using separate [`CurveTexture`](CurveTexture.md)s, this further simplifies the task of saving curves as image files.

If you only need to store one curve within a single texture, use [`CurveTexture`](CurveTexture.md) instead. See also [`GradientTexture1D`](GradientTexture1D.md) and [`GradientTexture2D`](GradientTexture2D.md).

## 构造函数

### CurveXyzTexture

```csharp
public CurveXyzTexture()
```

## 属性

### CurveX

```csharp
public Curve CurveX { get; set; }
```

The [`Curve`](Curve.md) that is rendered onto the texture's red channel. Should be a unit [`Curve`](Curve.md).

### CurveY

```csharp
public Curve CurveY { get; set; }
```

The [`Curve`](Curve.md) that is rendered onto the texture's green channel. Should be a unit [`Curve`](Curve.md).

### CurveZ

```csharp
public Curve CurveZ { get; set; }
```

The [`Curve`](Curve.md) that is rendered onto the texture's blue channel. Should be a unit [`Curve`](Curve.md).

### Width

```csharp
public int Width { get; set; }
```

The width of the texture (in pixels). Higher values make it possible to represent high-frequency data better (such as sudden direction changes), at the cost of increased generation time and memory usage.

## 方法

### GetCurveX

```csharp
public Curve GetCurveX()
```

### GetCurveY

```csharp
public Curve GetCurveY()
```

### GetCurveZ

```csharp
public Curve GetCurveZ()
```

### SetCurveX

```csharp
public void SetCurveX(Curve curve)
```

### SetCurveY

```csharp
public void SetCurveY(Curve curve)
```

### SetCurveZ

```csharp
public void SetCurveZ(Curve curve)
```

### SetWidth

```csharp
public void SetWidth(int width)
```
