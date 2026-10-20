<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ParallaxLayer

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A ParallaxLayer must be the child of a [`ParallaxBackground`](ParallaxBackground.md) node. Each ParallaxLayer can be set to move at different speeds relative to the camera movement or the [`ParallaxBackground.ScrollOffset`](ParallaxBackground.md) value.

This node's children will be affected by its scroll offset.

**Note:** Any changes to this node's position and scale made after it enters the scene will be ignored.

## 构造函数

### ParallaxLayer

```csharp
public ParallaxLayer()
```

## 属性

### MotionMirroring

```csharp
public Vector2 MotionMirroring { get; set; }
```

引擎名：`motion_mirroring`

The interval, in pixels, at which the `ParallaxLayer` is drawn repeatedly. Useful for creating an infinitely scrolling background. If an axis is set to `0`, the `ParallaxLayer` will be drawn only once along that direction.

**Note:** If you want the repetition to pixel-perfect match a [`Texture2D`](Texture2D.md) displayed by a child node, you should account for any scale applied to the texture when defining this interval. For example, if you use a child [`Sprite2D`](Sprite2D.md) scaled to `0.5` to display a 600x600 texture, and want this sprite to be repeated continuously horizontally, you should set the mirroring to `Vector2(300, 0)`.

**Note:** If the length of the viewport axis is bigger than twice the repeated axis size, it will not repeat infinitely, as the parallax layer only draws 2 instances of the layer at any given time. The visibility window is calculated from the parent [`ParallaxBackground`](ParallaxBackground.md)'s position, not the layer's own position. So, if you use mirroring, **do not** change the `ParallaxLayer` position relative to its parent. Instead, if you need to adjust the background's position, set the [`CanvasLayer.Offset`](CanvasLayer.md) property in the parent [`ParallaxBackground`](ParallaxBackground.md).

**Note:** Despite the name, the layer will not be mirrored, it will only be repeated.

### MotionOffset

```csharp
public Vector2 MotionOffset { get; set; }
```

引擎名：`motion_offset`

The ParallaxLayer's offset relative to the parent ParallaxBackground's [`ParallaxBackground.ScrollOffset`](ParallaxBackground.md).

### MotionScale

```csharp
public Vector2 MotionScale { get; set; }
```

引擎名：`motion_scale`

Multiplies the ParallaxLayer's motion. If an axis is set to `0`, it will not scroll.

## 方法

### GetMirroring

```csharp
public Vector2 GetMirroring()
```

引擎名：`get_mirroring`

### GetMotionOffset

```csharp
public Vector2 GetMotionOffset()
```

引擎名：`get_motion_offset`

### GetMotionScale

```csharp
public Vector2 GetMotionScale()
```

引擎名：`get_motion_scale`

### SetMirroring

```csharp
public void SetMirroring(Vector2 mirror)
```

引擎名：`set_mirroring`

### SetMotionOffset

```csharp
public void SetMotionOffset(Vector2 offset)
```

引擎名：`set_motion_offset`

### SetMotionScale

```csharp
public void SetMotionScale(Vector2 scale)
```

引擎名：`set_motion_scale`
