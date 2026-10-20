<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Sprite2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A node that displays a 2D texture. The texture displayed can be a region from a larger atlas texture, or a frame from a sprite sheet animation.

## 构造函数

### Sprite2D

```csharp
public Sprite2D()
```

## 属性

### Centered

```csharp
public bool Centered { get; set; }
```

引擎名：`centered`

If `true`, texture is centered.

**Note:** For games with a pixel art aesthetic, textures may appear deformed when centered. This is caused by their position being between pixels. To prevent this, set this property to `false`, or consider enabling `ProjectSettings.rendering/2d/snap/snap_2d_vertices_to_pixel` and `ProjectSettings.rendering/2d/snap/snap_2d_transforms_to_pixel`.

### FlipH

```csharp
public bool FlipH { get; set; }
```

引擎名：`flip_h`

If `true`, texture is flipped horizontally.

### FlipV

```csharp
public bool FlipV { get; set; }
```

引擎名：`flip_v`

If `true`, texture is flipped vertically.

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

### Offset

```csharp
public Vector2 Offset { get; set; }
```

引擎名：`offset`

The texture's drawing offset.

**Note:** When you increase `Offset`.y in Sprite2D, the sprite moves downward on screen (i.e., +Y is down).

### RegionEnabled

```csharp
public bool RegionEnabled { get; set; }
```

引擎名：`region_enabled`

If `true`, texture is cut from a larger atlas texture. See `RegionRect`.

**Note:** When using a custom [`Shader`](Shader.md) on a `Sprite2D`, the `UV` shader built-in will refer to the entire texture space. Use the `REGION_RECT` built-in to get the currently visible region defined in `RegionRect` instead. See [CanvasItem shaders]($DOCS_URL/tutorials/shaders/shader_reference/canvas_item_shader.html) for details.

### RegionFilterClipEnabled

```csharp
public bool RegionFilterClipEnabled { get; set; }
```

引擎名：`region_filter_clip_enabled`

If `true`, the area outside of the `RegionRect` is clipped to avoid bleeding of the surrounding texture pixels. `RegionEnabled` must be `true`.

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

[`Texture2D`](Texture2D.md) object to draw.

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

### GetOffset

```csharp
public Vector2 GetOffset()
```

引擎名：`get_offset`

### GetRect

```csharp
public Rect2 GetRect()
```

引擎名：`get_rect`

Returns a [`Rect2`](Rect2.md) representing the Sprite2D's boundary in local coordinates.

**Example:** Detect if the Sprite2D was clicked:

```csharp
public override void _Input(InputEvent @event)
{
if (@event is InputEventMouseButton inputEventMouse)
{
if (inputEventMouse.Pressed && inputEventMouse.ButtonIndex == MouseButton.Left)
{
if (GetRect().HasPoint(ToLocal(inputEventMouse.Position)))
{
GD.Print("A click!");
}
}
}
}

```

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

### IsCentered

```csharp
public bool IsCentered()
```

引擎名：`is_centered`

### IsFlippedH

```csharp
public bool IsFlippedH()
```

引擎名：`is_flipped_h`

### IsFlippedV

```csharp
public bool IsFlippedV()
```

引擎名：`is_flipped_v`

### IsPixelOpaque

```csharp
public bool IsPixelOpaque(Vector2 pos)
```

引擎名：`is_pixel_opaque`

Returns `true` if the pixel at the given position is opaque, `false` otherwise. Also returns `false` if the given position is out of bounds or this sprite's `Texture` is `null`. `pos` is in local coordinates.

### IsRegionEnabled

```csharp
public bool IsRegionEnabled()
```

引擎名：`is_region_enabled`

### IsRegionFilterClipEnabled

```csharp
public bool IsRegionFilterClipEnabled()
```

引擎名：`is_region_filter_clip_enabled`

### SetCentered

```csharp
public void SetCentered(bool centered)
```

引擎名：`set_centered`

### SetFlipH

```csharp
public void SetFlipH(bool flipH)
```

引擎名：`set_flip_h`

### SetFlipV

```csharp
public void SetFlipV(bool flipV)
```

引擎名：`set_flip_v`

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

### SetOffset

```csharp
public void SetOffset(Vector2 offset)
```

引擎名：`set_offset`

### SetRegionEnabled

```csharp
public void SetRegionEnabled(bool enabled)
```

引擎名：`set_region_enabled`

### SetRegionFilterClipEnabled

```csharp
public void SetRegionFilterClipEnabled(bool enabled)
```

引擎名：`set_region_filter_clip_enabled`

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
