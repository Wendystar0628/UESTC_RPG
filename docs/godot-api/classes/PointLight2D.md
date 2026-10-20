<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PointLight2D

命名空间 `Godot`；class；继承 [`Light2D`](Light2D.md) → [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Casts light in a 2D environment. This light's shape is defined by a (usually grayscale) texture.

## 构造函数

### PointLight2D

```csharp
public PointLight2D()
```

## 属性

### Height

```csharp
public float Height { get; set; }
```

引擎名：`height`

The height of the light. Used with 2D normal mapping. The units are in pixels, e.g. if the height is 100, then it will illuminate an object 100 pixels away at a 45° angle to the plane.

### Offset

```csharp
public Vector2 Offset { get; set; }
```

引擎名：`offset`

The offset of the light's `Texture`.

### Texture

```csharp
public Texture2D Texture { get; set; }
```

引擎名：`texture`

[`Texture2D`](Texture2D.md) used for the light's appearance.

### TextureScale

```csharp
public float TextureScale { get; set; }
```

引擎名：`texture_scale`

The `Texture`'s scale factor.

## 方法

### GetTexture

```csharp
public Texture2D GetTexture()
```

引擎名：`get_texture`

### GetTextureOffset

```csharp
public Vector2 GetTextureOffset()
```

引擎名：`get_texture_offset`

### GetTextureScale

```csharp
public float GetTextureScale()
```

引擎名：`get_texture_scale`

### SetTexture

```csharp
public void SetTexture(Texture2D texture)
```

引擎名：`set_texture`

### SetTextureOffset

```csharp
public void SetTextureOffset(Vector2 textureOffset)
```

引擎名：`set_texture_offset`

### SetTextureScale

```csharp
public void SetTextureScale(float textureScale)
```

引擎名：`set_texture_scale`
