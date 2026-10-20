<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SpriteBase3D

命名空间 `Godot`；class；继承 [`GeometryInstance3D`](GeometryInstance3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A node that displays 2D texture information in a 3D environment. See also [`Sprite3D`](Sprite3D.md) where many other properties are defined.

## 属性

### AlphaAntialiasingEdge

```csharp
public float AlphaAntialiasingEdge { get; set; }
```

引擎名：`alpha_antialiasing_edge`

Threshold at which antialiasing will be applied on the alpha channel.

### AlphaAntialiasingMode

```csharp
public BaseMaterial3D.AlphaAntiAliasing AlphaAntialiasingMode { get; set; }
```

引擎名：`alpha_antialiasing_mode`

The type of alpha antialiasing to apply.

### AlphaCut

```csharp
public SpriteBase3D.AlphaCutMode AlphaCut { get; set; }
```

引擎名：`alpha_cut`

The alpha cutting mode to use for the sprite.

### AlphaHashScale

```csharp
public float AlphaHashScale { get; set; }
```

引擎名：`alpha_hash_scale`

The hashing scale for Alpha Hash. Recommended values between `0` and `2`.

### AlphaScissorThreshold

```csharp
public float AlphaScissorThreshold { get; set; }
```

引擎名：`alpha_scissor_threshold`

Threshold at which the alpha scissor will discard values.

### Axis

```csharp
public Vector3.Axis Axis { get; set; }
```

引擎名：`axis`

The direction in which the front of the texture faces.

### Billboard

```csharp
public BaseMaterial3D.BillboardModeEnum Billboard { get; set; }
```

引擎名：`billboard`

The billboard mode to use for the sprite.

**Note:** When billboarding is enabled and the material also casts shadows, billboards will face **the** camera in the scene when rendering shadows. In scenes with multiple cameras, the intended shadow cannot be determined and this will result in undefined behavior. See [GitHub Pull Request #72638](https://github.com/godotengine/godot/pull/72638) for details.

### Centered

```csharp
public bool Centered { get; set; }
```

引擎名：`centered`

If `true`, texture will be centered.

### DoubleSided

```csharp
public bool DoubleSided { get; set; }
```

引擎名：`double_sided`

If `true`, texture can be seen from the back as well, if `false`, it is invisible when looking at it from behind.

### FixedSize

```csharp
public bool FixedSize { get; set; }
```

引擎名：`fixed_size`

If `true`, the texture is rendered at the same size regardless of distance. The texture's size on screen is the same as if the camera was `1.0` units away from the texture's origin, regardless of the actual distance from the camera. The [`Camera3D`](Camera3D.md)'s field of view (or [`Camera3D.Size`](Camera3D.md) when in orthogonal/frustum mode) still affects the size the sprite is drawn at.

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

### Modulate

```csharp
public Color Modulate { get; set; }
```

引擎名：`modulate`

A color value used to *multiply* the texture's colors. Can be used for mood-coloring or to simulate the color of ambient light.

**Note:** Unlike [`CanvasItem.Modulate`](CanvasItem.md) for 2D, colors with values above `1.0` (overbright) are not supported.

**Note:** If a [`GeometryInstance3D.MaterialOverride`](GeometryInstance3D.md) is defined on the `SpriteBase3D`, the material override must be configured to take vertex colors into account for albedo. Otherwise, the color defined in `Modulate` will be ignored. For a [`BaseMaterial3D`](BaseMaterial3D.md), [`BaseMaterial3D.VertexColorUseAsAlbedo`](BaseMaterial3D.md) must be `true`. For a [`ShaderMaterial`](ShaderMaterial.md), `ALBEDO *= COLOR.rgb;` must be inserted in the shader's `fragment()` function.

### NoDepthTest

```csharp
public bool NoDepthTest { get; set; }
```

引擎名：`no_depth_test`

If `true`, depth testing is disabled and the object will be drawn in render order.

### Offset

```csharp
public Vector2 Offset { get; set; }
```

引擎名：`offset`

The texture's drawing offset.

**Note:** When you increase `Offset`.y in Sprite3D, the sprite moves upward in world space (i.e., +Y is up).

### PixelSize

```csharp
public float PixelSize { get; set; }
```

引擎名：`pixel_size`

The size of one pixel's width on the sprite to scale it in 3D.

### RenderPriority

```csharp
public int RenderPriority { get; set; }
```

引擎名：`render_priority`

Sets the render priority for the sprite. Higher priority objects will be sorted in front of lower priority objects.

**Note:** This only applies if `AlphaCut` is set to [`SpriteBase3D.AlphaCutMode.Disabled`](SpriteBase3D.md) (default value).

**Note:** This only applies to sorting of transparent objects. This will not impact how transparent objects are sorted relative to opaque objects. This is because opaque objects are not sorted, while transparent objects are sorted from back to front (subject to priority).

### Shaded

```csharp
public bool Shaded { get; set; }
```

引擎名：`shaded`

If `true`, the [`Light3D`](Light3D.md) in the [`Environment`](Environment.md) has effects on the sprite.

### TextureFilter

```csharp
public BaseMaterial3D.TextureFilterEnum TextureFilter { get; set; }
```

引擎名：`texture_filter`

Filter flags for the texture.

**Note:** Linear filtering may cause artifacts around the edges, which are especially noticeable on opaque textures. To prevent this, use textures with transparent or identical colors around the edges.

### Transparent

```csharp
public bool Transparent { get; set; }
```

引擎名：`transparent`

If `true`, the texture's transparency and the opacity are used to make those parts of the sprite invisible.

## 方法

### GenerateTriangleMesh

```csharp
public TriangleMesh GenerateTriangleMesh()
```

引擎名：`generate_triangle_mesh`

Returns a [`TriangleMesh`](TriangleMesh.md) with the sprite's vertices following its current configuration (such as its `Axis` and `PixelSize`).

### GetAlphaAntialiasing

```csharp
public BaseMaterial3D.AlphaAntiAliasing GetAlphaAntialiasing()
```

引擎名：`get_alpha_antialiasing`

### GetAlphaAntialiasingEdge

```csharp
public float GetAlphaAntialiasingEdge()
```

引擎名：`get_alpha_antialiasing_edge`

### GetAlphaCutMode

```csharp
public SpriteBase3D.AlphaCutMode GetAlphaCutMode()
```

引擎名：`get_alpha_cut_mode`

### GetAlphaHashScale

```csharp
public float GetAlphaHashScale()
```

引擎名：`get_alpha_hash_scale`

### GetAlphaScissorThreshold

```csharp
public float GetAlphaScissorThreshold()
```

引擎名：`get_alpha_scissor_threshold`

### GetAxis

```csharp
public Vector3.Axis GetAxis()
```

引擎名：`get_axis`

### GetBillboardMode

```csharp
public BaseMaterial3D.BillboardModeEnum GetBillboardMode()
```

引擎名：`get_billboard_mode`

### GetDrawFlag

```csharp
public bool GetDrawFlag(SpriteBase3D.DrawFlags flag)
```

引擎名：`get_draw_flag`

Returns the value of the specified flag.

### GetItemRect

```csharp
public Rect2 GetItemRect()
```

引擎名：`get_item_rect`

Returns the rectangle representing this sprite.

### GetModulate

```csharp
public Color GetModulate()
```

引擎名：`get_modulate`

### GetOffset

```csharp
public Vector2 GetOffset()
```

引擎名：`get_offset`

### GetPixelSize

```csharp
public float GetPixelSize()
```

引擎名：`get_pixel_size`

### GetRenderPriority

```csharp
public int GetRenderPriority()
```

引擎名：`get_render_priority`

### GetTextureFilter

```csharp
public BaseMaterial3D.TextureFilterEnum GetTextureFilter()
```

引擎名：`get_texture_filter`

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

### SetAlphaAntialiasing

```csharp
public void SetAlphaAntialiasing(BaseMaterial3D.AlphaAntiAliasing alphaAA)
```

引擎名：`set_alpha_antialiasing`

### SetAlphaAntialiasingEdge

```csharp
public void SetAlphaAntialiasingEdge(float edge)
```

引擎名：`set_alpha_antialiasing_edge`

### SetAlphaCutMode

```csharp
public void SetAlphaCutMode(SpriteBase3D.AlphaCutMode mode)
```

引擎名：`set_alpha_cut_mode`

### SetAlphaHashScale

```csharp
public void SetAlphaHashScale(float threshold)
```

引擎名：`set_alpha_hash_scale`

### SetAlphaScissorThreshold

```csharp
public void SetAlphaScissorThreshold(float threshold)
```

引擎名：`set_alpha_scissor_threshold`

### SetAxis

```csharp
public void SetAxis(Vector3.Axis axis)
```

引擎名：`set_axis`

### SetBillboardMode

```csharp
public void SetBillboardMode(BaseMaterial3D.BillboardModeEnum mode)
```

引擎名：`set_billboard_mode`

### SetCentered

```csharp
public void SetCentered(bool centered)
```

引擎名：`set_centered`

### SetDrawFlag

```csharp
public void SetDrawFlag(SpriteBase3D.DrawFlags flag, bool enabled)
```

引擎名：`set_draw_flag`

If `true`, the specified flag will be enabled.

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

### SetModulate

```csharp
public void SetModulate(Color modulate)
```

引擎名：`set_modulate`

### SetOffset

```csharp
public void SetOffset(Vector2 offset)
```

引擎名：`set_offset`

### SetPixelSize

```csharp
public void SetPixelSize(float pixelSize)
```

引擎名：`set_pixel_size`

### SetRenderPriority

```csharp
public void SetRenderPriority(int priority)
```

引擎名：`set_render_priority`

### SetTextureFilter

```csharp
public void SetTextureFilter(BaseMaterial3D.TextureFilterEnum mode)
```

引擎名：`set_texture_filter`

## AlphaCutMode

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：This mode performs standard alpha blending.
- `Discard = 1`：This mode only allows fully transparent or fully opaque pixels.
- `OpaquePrepass = 2`：This mode draws fully opaque pixels in the depth prepass.
- `Hash = 3`：This mode draws cuts off all values below a spatially-deterministic threshold, the rest will remain opaque.

## DrawFlags

命名空间 `Godot`；enum

### 枚举值

- `Transparent = 0`：If set, the texture's transparency and the opacity are used to make those parts of the sprite invisible.
- `Shaded = 1`：If set, lights in the environment affect the sprite.
- `DoubleSided = 2`：If set, texture can be seen from the back as well.
- `DisableDepthTest = 3`：Disables the depth test, so this object is drawn on top of all others.
- `FixedSize = 4`：Label is scaled by depth so that it always appears the same size on screen.
- `Max = 5`：Represents the size of the `SpriteBase3D.DrawFlags` enum.
