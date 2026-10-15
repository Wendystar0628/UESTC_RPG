<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Sprite3D

命名空间 `Godot`；class；继承 [`SpriteBase3D`](SpriteBase3D.md) → [`GeometryInstance3D`](GeometryInstance3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A node that displays a 2D texture in a 3D environment. The texture displayed can be a region from a larger atlas texture, or a frame from a sprite sheet animation. See also [`SpriteBase3D`](SpriteBase3D.md) where properties such as the billboard mode are defined.

## 构造函数

### Sprite3D

```csharp
public Sprite3D()
```

## 属性

### Frame

```csharp
public int Frame { get; set; }
```

引擎名：`frame`

Current frame to display from sprite sheet. `Hframes` or `Vframes` must be greater than 1. This property is automatically adjusted when `Hframes` or `Vframes` are changed to keep pointing to the same visual frame (same column and row). If that's impossible, this value is reset to `0`.

### FrameCoords

```csharp
public Vector2I FrameCoords { get; set; }
```

引擎名：`frame_coords`

Coordinates of the frame to display from sprite sheet. This is as an alias for the `Frame` property. `Hframes` or `Vframes` must be greater than 1.

### Hframes

```csharp
public int Hframes { get; set; }
```

引擎名：`hframes`

The number of columns in the sprite sheet. When this property is changed, `Frame` is adjusted so that the same visual frame is maintained (same row and column). If that's impossible, `Frame` is reset to `0`.

### RegionEnabled

```csharp
public bool RegionEnabled { get; set; }
```

引擎名：`region_enabled`

If `true`, the sprite will use `RegionRect` and display only the specified part of its texture.

### RegionRect

```csharp
public Rect2 RegionRect { get; set; }
```

引擎名：`region_rect`

The region of the atlas texture to display. `RegionEnabled` must be `true`.

### Texture

```csharp
public Texture2D Texture { get; set; }
```

引擎名：`texture`

[`Texture2D`](Texture2D.md) object to draw. If [`GeometryInstance3D.MaterialOverride`](GeometryInstance3D.md) is used, this will be overridden. The size information is still used.

### Vframes

```csharp
public int Vframes { get; set; }
```

引擎名：`vframes`

The number of rows in the sprite sheet. When this property is changed, `Frame` is adjusted so that the same visual frame is maintained (same row and column). If that's impossible, `Frame` is reset to `0`.

## 方法

### GetFrame

```csharp
public int GetFrame()
```

引擎名：`get_frame`

### GetFrameCoords

```csharp
public Vector2I GetFrameCoords()
```

引擎名：`get_frame_coords`

### GetHframes

```csharp
public int GetHframes()
```

引擎名：`get_hframes`

### GetRegionRect

```csharp
public Rect2 GetRegionRect()
```

引擎名：`get_region_rect`

### GetTexture

```csharp
public Texture2D GetTexture()
```

引擎名：`get_texture`

### GetVframes

```csharp
public int GetVframes()
```

引擎名：`get_vframes`

### IsRegionEnabled

```csharp
public bool IsRegionEnabled()
```

引擎名：`is_region_enabled`

### SetFrame

```csharp
public void SetFrame(int frame)
```

引擎名：`set_frame`

### SetFrameCoords

```csharp
public void SetFrameCoords(Vector2I coords)
```

引擎名：`set_frame_coords`

### SetHframes

```csharp
public void SetHframes(int hframes)
```

引擎名：`set_hframes`

### SetRegionEnabled

```csharp
public void SetRegionEnabled(bool enabled)
```

引擎名：`set_region_enabled`

### SetRegionRect

```csharp
public void SetRegionRect(Rect2 rect)
```

引擎名：`set_region_rect`

### SetTexture

```csharp
public void SetTexture(Texture2D texture)
```

引擎名：`set_texture`

### SetVframes

```csharp
public void SetVframes(int vframes)
```

引擎名：`set_vframes`

## 事件（信号）

### FrameChanged

```csharp
public event Action FrameChanged
```

引擎名：`frame_changed`

Emitted when the `Frame` changes.

### TextureChanged

```csharp
public event Action TextureChanged
```

引擎名：`texture_changed`

Emitted when the `Texture` changes.
