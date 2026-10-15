<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TextureRect

命名空间 `Godot`；class；继承 [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A control that displays a texture, for example an icon inside a GUI. The texture's placement can be controlled with the `StretchMode` property. It can scale, tile, or stay centered inside its bounding rectangle.

## 构造函数

### TextureRect

```csharp
public TextureRect()
```

## 属性

### ExpandMode

```csharp
public TextureRect.ExpandModeEnum ExpandMode { get; set; }
```

引擎名：`expand_mode`

Defines how minimum size is determined based on the texture's size.

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

### StretchMode

```csharp
public TextureRect.StretchModeEnum StretchMode { get; set; }
```

引擎名：`stretch_mode`

Controls the texture's behavior when resizing the node's bounding rectangle.

### Texture

```csharp
public Texture2D Texture { get; set; }
```

引擎名：`texture`

The node's [`Texture2D`](Texture2D.md) resource.

## 方法

### GetExpandMode

```csharp
public TextureRect.ExpandModeEnum GetExpandMode()
```

引擎名：`get_expand_mode`

### GetStretchMode

```csharp
public TextureRect.StretchModeEnum GetStretchMode()
```

引擎名：`get_stretch_mode`

### GetTexture

```csharp
public Texture2D GetTexture()
```

引擎名：`get_texture`

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

### SetExpandMode

```csharp
public void SetExpandMode(TextureRect.ExpandModeEnum expandMode)
```

引擎名：`set_expand_mode`

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

### SetStretchMode

```csharp
public void SetStretchMode(TextureRect.StretchModeEnum stretchMode)
```

引擎名：`set_stretch_mode`

### SetTexture

```csharp
public void SetTexture(Texture2D texture)
```

引擎名：`set_texture`

## ExpandModeEnum

命名空间 `Godot`；enum

### 枚举值

- `KeepSize = 0`：The minimum size will be equal to texture size, i.e.
- `IgnoreSize = 1`：The size of the texture won't be considered for minimum size calculation, so the [`TextureRect`](TextureRect.md) can be shrunk down past the texture size.
- `FitWidth = 2`：The height of the texture will be ignored.
- `FitWidthProportional = 3`：Same as `FitWidth`, but keeps texture's aspect ratio.
- `FitHeight = 4`：The width of the texture will be ignored.
- `FitHeightProportional = 5`：Same as `FitHeight`, but keeps texture's aspect ratio.

## StretchModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Scale = 0`：Scale to fit the node's bounding rectangle.
- `Tile = 1`：Tile inside the node's bounding rectangle.
- `Keep = 2`：The texture keeps its original size and stays in the bounding rectangle's top-left corner.
- `KeepCentered = 3`：The texture keeps its original size and stays centered in the node's bounding rectangle.
- `KeepAspect = 4`：Scale the texture to fit the node's bounding rectangle, but maintain the texture's aspect ratio.
- `KeepAspectCentered = 5`：Scale the texture to fit the node's bounding rectangle, center it and maintain its aspect ratio.
- `KeepAspectCovered = 6`：Scale the texture so that the shorter side fits the bounding rectangle.
