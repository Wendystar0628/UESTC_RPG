<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# BoxMesh

命名空间 `Godot`；class；继承 [`PrimitiveMesh`](PrimitiveMesh.md) → [`Mesh`](Mesh.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Generate an axis-aligned box [`PrimitiveMesh`](PrimitiveMesh.md).

The box's UV layout is arranged in a 3×2 layout that allows texturing each face individually. To apply the same texture on all faces, change the material's UV property to `Vector3(3, 2, 1)`. This is equivalent to adding `UV *= vec2(3.0, 2.0)` in a vertex shader.

**Note:** When using a large textured `BoxMesh` (e.g. as a floor), you may stumble upon UV jittering issues depending on the camera angle. To solve this, increase `SubdivideDepth`, `SubdivideHeight` and `SubdivideWidth` until you no longer notice UV jittering.

## 构造函数

### BoxMesh

```csharp
public BoxMesh()
```

## 属性

### Size

```csharp
public Vector3 Size { get; set; }
```

引擎名：`size`

The box's width, height and depth.

### SubdivideDepth

```csharp
public int SubdivideDepth { get; set; }
```

引擎名：`subdivide_depth`

Number of extra edge loops inserted along the Z axis.

### SubdivideHeight

```csharp
public int SubdivideHeight { get; set; }
```

引擎名：`subdivide_height`

Number of extra edge loops inserted along the Y axis.

### SubdivideWidth

```csharp
public int SubdivideWidth { get; set; }
```

引擎名：`subdivide_width`

Number of extra edge loops inserted along the X axis.

## 方法

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

### SetSize

```csharp
public void SetSize(Vector3 size)
```

引擎名：`set_size`

### SetSubdivideDepth

```csharp
public void SetSubdivideDepth(int divisions)
```

引擎名：`set_subdivide_depth`

### SetSubdivideHeight

```csharp
public void SetSubdivideHeight(int divisions)
```

引擎名：`set_subdivide_height`

### SetSubdivideWidth

```csharp
public void SetSubdivideWidth(int subdivide)
```

引擎名：`set_subdivide_width`
