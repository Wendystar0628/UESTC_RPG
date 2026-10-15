<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TextureButton

命名空间 `Godot`；class；继承 [`BaseButton`](BaseButton.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`TextureButton` has the same functionality as [`Button`](Button.md), except it uses sprites instead of Godot's [`Theme`](Theme.md) resource. It is faster to create, but it doesn't support localization like more complex [`Control`](Control.md)s.

See also [`BaseButton`](BaseButton.md) which contains common properties and methods associated with this node.

**Note:** Setting a texture for the "normal" state (`TextureNormal`) is recommended. If `TextureNormal` is not set, the `TextureButton` will still receive input events and be clickable, but the user will not be able to see it unless they activate another one of its states with a texture assigned (e.g., hover over it to show `TextureHover`).

## 构造函数

### TextureButton

```csharp
public TextureButton()
```

## 属性

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

### IgnoreTextureSize

```csharp
public bool IgnoreTextureSize { get; set; }
```

引擎名：`ignore_texture_size`

If `true`, the size of the texture won't be considered for minimum size calculation, so the `TextureButton` can be shrunk down past the texture size.

### StretchMode

```csharp
public TextureButton.StretchModeEnum StretchMode { get; set; }
```

引擎名：`stretch_mode`

Controls the texture's behavior when you resize the node's bounding rectangle. See the [`TextureButton.StretchModeEnum`](TextureButton.md) constants for available options.

### TextureClickMask

```csharp
public Bitmap TextureClickMask { get; set; }
```

引擎名：`texture_click_mask`

Pure black and white [`Bitmap`](Bitmap.md) image to use for click detection. On the mask, white pixels represent the button's clickable area. Use it to create buttons with curved shapes.

### TextureDisabled

```csharp
public Texture2D TextureDisabled { get; set; }
```

引擎名：`texture_disabled`

Texture to display when the node is disabled. See [`BaseButton.Disabled`](BaseButton.md). If not assigned, the `TextureButton` displays `TextureNormal` instead.

### TextureFocused

```csharp
public Texture2D TextureFocused { get; set; }
```

引擎名：`texture_focused`

Texture to *overlay on the base texture* when the node has mouse or keyboard focus. Because `TextureFocused` is displayed on top of the base texture, a partially transparent texture should be used to ensure the base texture remains visible. A texture that represents an outline or an underline works well for this purpose. To disable the focus visual effect, assign a fully transparent texture of any size. Note that disabling the focus visual effect will harm keyboard/controller navigation usability, so this is not recommended for accessibility reasons.

### TextureHover

```csharp
public Texture2D TextureHover { get; set; }
```

引擎名：`texture_hover`

Texture to display when the mouse hovers over the node. If not assigned, the `TextureButton` displays `TextureNormal` instead when hovered over.

### TextureNormal

```csharp
public Texture2D TextureNormal { get; set; }
```

引擎名：`texture_normal`

Texture to display by default, when the node is **not** in the disabled, hover or pressed state. This texture is still displayed in the focused state, with `TextureFocused` drawn on top.

### TexturePressed

```csharp
public Texture2D TexturePressed { get; set; }
```

引擎名：`texture_pressed`

Texture to display on mouse down over the node, if the node has keyboard focus and the player presses the Enter key or if the player presses the [`BaseButton.Shortcut`](BaseButton.md) key. If not assigned, the `TextureButton` displays `TextureHover` instead when pressed.

## 方法

### GetClickMask

```csharp
public Bitmap GetClickMask()
```

引擎名：`get_click_mask`

### GetIgnoreTextureSize

```csharp
public bool GetIgnoreTextureSize()
```

引擎名：`get_ignore_texture_size`

### GetStretchMode

```csharp
public TextureButton.StretchModeEnum GetStretchMode()
```

引擎名：`get_stretch_mode`

### GetTextureDisabled

```csharp
public Texture2D GetTextureDisabled()
```

引擎名：`get_texture_disabled`

### GetTextureFocused

```csharp
public Texture2D GetTextureFocused()
```

引擎名：`get_texture_focused`

### GetTextureHover

```csharp
public Texture2D GetTextureHover()
```

引擎名：`get_texture_hover`

### GetTextureNormal

```csharp
public Texture2D GetTextureNormal()
```

引擎名：`get_texture_normal`

### GetTexturePressed

```csharp
public Texture2D GetTexturePressed()
```

引擎名：`get_texture_pressed`

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

### SetClickMask

```csharp
public void SetClickMask(Bitmap mask)
```

引擎名：`set_click_mask`

### SetFlipH

```csharp
public void SetFlipH(bool enable)
```

引擎名：`set_flip_h`

### SetFlipV

```csharp
public void SetFlipV(bool enable)
```

引擎名：`set_flip_v`

### SetIgnoreTextureSize

```csharp
public void SetIgnoreTextureSize(bool ignore)
```

引擎名：`set_ignore_texture_size`

### SetStretchMode

```csharp
public void SetStretchMode(TextureButton.StretchModeEnum mode)
```

引擎名：`set_stretch_mode`

### SetTextureDisabled

```csharp
public void SetTextureDisabled(Texture2D texture)
```

引擎名：`set_texture_disabled`

### SetTextureFocused

```csharp
public void SetTextureFocused(Texture2D texture)
```

引擎名：`set_texture_focused`

### SetTextureHover

```csharp
public void SetTextureHover(Texture2D texture)
```

引擎名：`set_texture_hover`

### SetTextureNormal

```csharp
public void SetTextureNormal(Texture2D texture)
```

引擎名：`set_texture_normal`

### SetTexturePressed

```csharp
public void SetTexturePressed(Texture2D texture)
```

引擎名：`set_texture_pressed`

## StretchModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Scale = 0`：Scale to fit the node's bounding rectangle.
- `Tile = 1`：Tile inside the node's bounding rectangle.
- `Keep = 2`：The texture keeps its original size and stays in the bounding rectangle's top-left corner.
- `KeepCentered = 3`：The texture keeps its original size and stays centered in the node's bounding rectangle.
- `KeepAspect = 4`：Scale the texture to fit the node's bounding rectangle, but maintain the texture's aspect ratio.
- `KeepAspectCentered = 5`：Scale the texture to fit the node's bounding rectangle, center it, and maintain its aspect ratio.
- `KeepAspectCovered = 6`：Scale the texture so that the shorter side fits the bounding rectangle.
