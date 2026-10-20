<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RootMotionView

命名空间 `Godot`；class；继承 [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

*Root motion* refers to an animation technique where a mesh's skeleton is used to give impulse to a character. When working with 3D animations, a popular technique is for animators to use the root skeleton bone to give motion to the rest of the skeleton. This allows animating characters in a way where steps actually match the floor below. It also allows precise interaction with objects during cinematics. See also [`AnimationMixer`](AnimationMixer.md).

**Note:** `RootMotionView` is only visible in the editor. It will be hidden automatically in the running project.

## 构造函数

### RootMotionView

```csharp
public RootMotionView()
```

## 属性

### AnimationPath

```csharp
public NodePath AnimationPath { get; set; }
```

引擎名：`animation_path`

Path to an [`AnimationMixer`](AnimationMixer.md) node to use as a basis for root motion.

### CellSize

```csharp
public float CellSize { get; set; }
```

引擎名：`cell_size`

The grid's cell size in 3D units.

### Color

```csharp
public Color Color { get; set; }
```

引擎名：`color`

The grid's color.

### Radius

```csharp
public float Radius { get; set; }
```

引擎名：`radius`

The grid's radius in 3D units. The grid's opacity will fade gradually as the distance from the origin increases until this `Radius` is reached.

### ZeroY

```csharp
public bool ZeroY { get; set; }
```

引擎名：`zero_y`

If `true`, the grid's points will all be on the same Y coordinate (*local* Y = 0). If `false`, the points' original Y coordinate is preserved.

## 方法

### GetAnimationPath

```csharp
public NodePath GetAnimationPath()
```

引擎名：`get_animation_path`

### GetCellSize

```csharp
public float GetCellSize()
```

引擎名：`get_cell_size`

### GetColor

```csharp
public Color GetColor()
```

引擎名：`get_color`

### GetRadius

```csharp
public float GetRadius()
```

引擎名：`get_radius`

### GetZeroY

```csharp
public bool GetZeroY()
```

引擎名：`get_zero_y`

### SetAnimationPath

```csharp
public void SetAnimationPath(NodePath path)
```

引擎名：`set_animation_path`

### SetCellSize

```csharp
public void SetCellSize(float size)
```

引擎名：`set_cell_size`

### SetColor

```csharp
public void SetColor(Color color)
```

引擎名：`set_color`

### SetRadius

```csharp
public void SetRadius(float size)
```

引擎名：`set_radius`

### SetZeroY

```csharp
public void SetZeroY(bool enable)
```

引擎名：`set_zero_y`
