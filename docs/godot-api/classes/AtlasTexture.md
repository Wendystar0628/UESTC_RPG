<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AtlasTexture

命名空间 `Godot`；class；继承 [`Texture2D`](Texture2D.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

[`Texture2D`](Texture2D.md) resource that draws only part of its `Atlas` texture, as defined by the `Region`. An additional `Margin` can also be set, which is useful for small adjustments.

Multiple `AtlasTexture` resources can be cropped from the same `Atlas`. Packing many smaller textures into a singular large texture helps to optimize video memory costs and render calls.

**Note:** `AtlasTexture` cannot be used in an [`AnimatedTexture`](AnimatedTexture.md), and will not tile properly in nodes such as [`TextureRect`](TextureRect.md) or [`Sprite2D`](Sprite2D.md). To tile an `AtlasTexture`, modify its `Region` instead.

## 构造函数

### AtlasTexture

```csharp
public AtlasTexture()
```

## 属性

### Atlas

```csharp
public Texture2D Atlas { get; set; }
```

引擎名：`atlas`

The texture that contains the atlas. Can be any type inheriting from [`Texture2D`](Texture2D.md), including another `AtlasTexture`.

### FilterClip

```csharp
public bool FilterClip { get; set; }
```

引擎名：`filter_clip`

If `true`, the area outside of the `Region` is clipped to avoid bleeding of the surrounding texture pixels.

### Margin

```csharp
public Rect2 Margin { get; set; }
```

引擎名：`margin`

The margin around the `Region`. Useful for small adjustments. If the `Rect2.Size` of this property ("w" and "h" in the editor) is set, the drawn texture is resized to fit within the margin.

### Region

```csharp
public Rect2 Region { get; set; }
```

引擎名：`region`

The region used to draw the `Atlas`. If either dimension of the region's size is `0`, the value from `Atlas` size will be used for that axis instead.

**Note:** The image size is always an integer, so the actual region size is rounded down.

## 方法

### GetAtlas

```csharp
public Texture2D GetAtlas()
```

引擎名：`get_atlas`

### GetMargin

```csharp
public Rect2 GetMargin()
```

引擎名：`get_margin`

### GetRegion

```csharp
public Rect2 GetRegion()
```

引擎名：`get_region`

### HasFilterClip

```csharp
public bool HasFilterClip()
```

引擎名：`has_filter_clip`

### SetAtlas

```csharp
public void SetAtlas(Texture2D atlas)
```

引擎名：`set_atlas`

### SetFilterClip

```csharp
public void SetFilterClip(bool enable)
```

引擎名：`set_filter_clip`

### SetMargin

```csharp
public void SetMargin(Rect2 margin)
```

引擎名：`set_margin`

### SetRegion

```csharp
public void SetRegion(Rect2 region)
```

引擎名：`set_region`
