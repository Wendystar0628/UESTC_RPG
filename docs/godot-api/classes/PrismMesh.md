<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PrismMesh

命名空间 `Godot`；class；继承 [`PrimitiveMesh`](PrimitiveMesh.md) → [`Mesh`](Mesh.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Class representing a prism-shaped [`PrimitiveMesh`](PrimitiveMesh.md).

## 构造函数

### PrismMesh

```csharp
public PrismMesh()
```

## 属性

### LeftToRight

```csharp
public float LeftToRight { get; set; }
```

引擎名：`left_to_right`

Displacement of the upper edge along the X axis. 0.0 positions edge straight above the bottom-left edge.

### Size

```csharp
public Vector3 Size { get; set; }
```

引擎名：`size`

Size of the prism.

### SubdivideDepth

```csharp
public int SubdivideDepth { get; set; }
```

引擎名：`subdivide_depth`

Number of added edge loops along the Z axis.

### SubdivideHeight

```csharp
public int SubdivideHeight { get; set; }
```

引擎名：`subdivide_height`

Number of added edge loops along the Y axis.

### SubdivideWidth

```csharp
public int SubdivideWidth { get; set; }
```

引擎名：`subdivide_width`

Number of added edge loops along the X axis.

## 方法

### GetLeftToRight

```csharp
public float GetLeftToRight()
```

引擎名：`get_left_to_right`

### GetSize

```csharp
public Vector3 GetSize()
```

引擎名：`get_size`

### GetSubdivideDepth

```csharp
public int GetSubdivideDepth()
```

引擎名：`get_subdivide_depth`

### GetSubdivideHeight

```csharp
public int GetSubdivideHeight()
```

引擎名：`get_subdivide_height`

### GetSubdivideWidth

```csharp
public int GetSubdivideWidth()
```

引擎名：`get_subdivide_width`

### SetLeftToRight

```csharp
public void SetLeftToRight(float leftToRight)
```

引擎名：`set_left_to_right`

### SetSize

```csharp
public void SetSize(Vector3 size)
```

引擎名：`set_size`

### SetSubdivideDepth

```csharp
public void SetSubdivideDepth(int segments)
```

引擎名：`set_subdivide_depth`

### SetSubdivideHeight

```csharp
public void SetSubdivideHeight(int segments)
```

引擎名：`set_subdivide_height`

### SetSubdivideWidth

```csharp
public void SetSubdivideWidth(int segments)
```

引擎名：`set_subdivide_width`
