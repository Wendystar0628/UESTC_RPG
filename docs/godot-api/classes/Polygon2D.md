<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Polygon2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A Polygon2D is defined by a set of points. Each point is connected to the next, with the final point being connected to the first, resulting in a closed polygon. Polygon2Ds can be filled with color (solid or gradient) or filled with a given texture.

## 构造函数

### Polygon2D

```csharp
public Polygon2D()
```

## 属性

### Antialiased

```csharp
public bool Antialiased { get; set; }
```

引擎名：`antialiased`

If `true`, polygon edges will be anti-aliased.

### Bones

```csharp
public Array Bones { get; set; }
```

引擎名：`bones`

### Color

```csharp
public Color Color { get; set; }
```

引擎名：`color`

The polygon's fill color. If `Texture` is set, it will be multiplied by this color. It will also be the default color for vertices not set in `VertexColors`.

### InternalVertexCount

```csharp
public int InternalVertexCount { get; set; }
```

引擎名：`internal_vertex_count`

Number of internal vertices, used for UV mapping.

### InvertBorder

```csharp
public float InvertBorder { get; set; }
```

引擎名：`invert_border`

Added padding applied to the bounding box when `InvertEnabled` is set to `true`. Setting this value too small may result in a "Bad Polygon" error.

### InvertEnabled

```csharp
public bool InvertEnabled { get; set; }
```

引擎名：`invert_enabled`

If `true`, the polygon will be inverted, containing the area outside the defined points and extending to the `InvertBorder`.

### Offset

```csharp
public Vector2 Offset { get; set; }
```

引擎名：`offset`

The offset applied to each vertex.

### Polygon

```csharp
public Vector2[] Polygon { get; set; }
```

引擎名：`polygon`

The polygon's list of vertices. The final point will be connected to the first.

### Polygons

```csharp
public Array Polygons { get; set; }
```

引擎名：`polygons`

The list of polygons, in case more than one is being represented. Every individual polygon is stored as a `System.Int32`[] where each `System.Int32` is an index to a point in `Polygon`. If empty, this property will be ignored, and the resulting single polygon will be composed of all points in `Polygon`, using the order they are stored in.

### Skeleton

```csharp
public NodePath Skeleton { get; set; }
```

引擎名：`skeleton`

Path to a [`Skeleton2D`](Skeleton2D.md) node used for skeleton-based deformations of this polygon. If empty or invalid, skeletal deformations will not be used.

### Texture

```csharp
public Texture2D Texture { get; set; }
```

引擎名：`texture`

The polygon's fill texture. Use `UV` to set texture coordinates.

### TextureOffset

```csharp
public Vector2 TextureOffset { get; set; }
```

引擎名：`texture_offset`

Amount to offset the polygon's `Texture`. If set to `Vector2(0, 0)`, the texture's origin (its top-left corner) will be placed at the polygon's position.

### TextureRotation

```csharp
public float TextureRotation { get; set; }
```

引擎名：`texture_rotation`

The texture's rotation in radians.

### TextureScale

```csharp
public Vector2 TextureScale { get; set; }
```

引擎名：`texture_scale`

Amount to multiply the `UV` coordinates when using `Texture`. Larger values make the texture smaller, and vice versa.

### UV

```csharp
public Vector2[] UV { get; set; }
```

引擎名：`uv`

Texture coordinates for each vertex of the polygon. There should be one UV value per polygon vertex. If there are fewer, undefined vertices will use `Vector2(0, 0)`.

### VertexColors

```csharp
public Color[] VertexColors { get; set; }
```

引擎名：`vertex_colors`

Color for each vertex. Colors are interpolated between vertices, resulting in smooth gradients. There should be one per polygon vertex. If there are fewer, undefined vertices will use `Color`.

## 方法

### AddBone

```csharp
public void AddBone(NodePath path, ReadOnlySpan<float> weights)
```

引擎名：`add_bone`

Adds a bone with the specified `path` and `weights`.

### AddBone

```csharp
public void AddBone(NodePath path, float[] weights)
```

引擎名：`add_bone`

Adds a bone with the specified `path` and `weights`.

### ClearBones

```csharp
public void ClearBones()
```

引擎名：`clear_bones`

Removes all bones from this `Polygon2D`.

### EraseBone

```csharp
public void EraseBone(int index)
```

引擎名：`erase_bone`

Removes the specified bone from this `Polygon2D`.

### GetAntialiased

```csharp
public bool GetAntialiased()
```

引擎名：`get_antialiased`

### GetBoneCount

```csharp
public int GetBoneCount()
```

引擎名：`get_bone_count`

Returns the number of bones in this `Polygon2D`.

### GetBonePath

```csharp
public NodePath GetBonePath(int index)
```

引擎名：`get_bone_path`

Returns the path to the node associated with the specified bone.

### GetBoneWeights

```csharp
public float[] GetBoneWeights(int index)
```

引擎名：`get_bone_weights`

Returns the weight values of the specified bone.

### GetColor

```csharp
public Color GetColor()
```

引擎名：`get_color`

### GetInternalVertexCount

```csharp
public int GetInternalVertexCount()
```

引擎名：`get_internal_vertex_count`

### GetInvertBorder

```csharp
public float GetInvertBorder()
```

引擎名：`get_invert_border`

### GetInvertEnabled

```csharp
public bool GetInvertEnabled()
```

引擎名：`get_invert_enabled`

### GetOffset

```csharp
public Vector2 GetOffset()
```

引擎名：`get_offset`

### GetPolygon

```csharp
public Vector2[] GetPolygon()
```

引擎名：`get_polygon`

### GetPolygons

```csharp
public Array GetPolygons()
```

引擎名：`get_polygons`

### GetSkeleton

```csharp
public NodePath GetSkeleton()
```

引擎名：`get_skeleton`

### GetTexture

```csharp
public Texture2D GetTexture()
```

引擎名：`get_texture`

### GetTextureOffset

```csharp
public Vector2 GetTextureOffset()
```

引擎名：`get_texture_offset`

### GetTextureRotation

```csharp
public float GetTextureRotation()
```

引擎名：`get_texture_rotation`

### GetTextureScale

```csharp
public Vector2 GetTextureScale()
```

引擎名：`get_texture_scale`

### GetUV

```csharp
public Vector2[] GetUV()
```

引擎名：`get_uv`

### GetVertexColors

```csharp
public Color[] GetVertexColors()
```

引擎名：`get_vertex_colors`

### SetAntialiased

```csharp
public void SetAntialiased(bool antialiased)
```

引擎名：`set_antialiased`

### SetBonePath

```csharp
public void SetBonePath(int index, NodePath path)
```

引擎名：`set_bone_path`

Sets the path to the node associated with the specified bone.

### SetBoneWeights

```csharp
public void SetBoneWeights(int index, ReadOnlySpan<float> weights)
```

引擎名：`set_bone_weights`

Sets the weight values for the specified bone.

### SetBoneWeights

```csharp
public void SetBoneWeights(int index, float[] weights)
```

引擎名：`set_bone_weights`

Sets the weight values for the specified bone.

### SetColor

```csharp
public void SetColor(Color color)
```

引擎名：`set_color`

### SetInternalVertexCount

```csharp
public void SetInternalVertexCount(int internalVertexCount)
```

引擎名：`set_internal_vertex_count`

### SetInvertBorder

```csharp
public void SetInvertBorder(float invertBorder)
```

引擎名：`set_invert_border`

### SetInvertEnabled

```csharp
public void SetInvertEnabled(bool invert)
```

引擎名：`set_invert_enabled`

### SetOffset

```csharp
public void SetOffset(Vector2 offset)
```

引擎名：`set_offset`

### SetPolygon

```csharp
public void SetPolygon(ReadOnlySpan<Vector2> polygon)
```

引擎名：`set_polygon`

### SetPolygon

```csharp
public void SetPolygon(Vector2[] polygon)
```

引擎名：`set_polygon`

### SetPolygons

```csharp
public void SetPolygons(Array polygons)
```

引擎名：`set_polygons`

### SetSkeleton

```csharp
public void SetSkeleton(NodePath skeleton)
```

引擎名：`set_skeleton`

### SetTexture

```csharp
public void SetTexture(Texture2D texture)
```

引擎名：`set_texture`

### SetTextureOffset

```csharp
public void SetTextureOffset(Vector2 textureOffset)
```

引擎名：`set_texture_offset`

### SetTextureRotation

```csharp
public void SetTextureRotation(float textureRotation)
```

引擎名：`set_texture_rotation`

### SetTextureScale

```csharp
public void SetTextureScale(Vector2 textureScale)
```

引擎名：`set_texture_scale`

### SetUV

```csharp
public void SetUV(ReadOnlySpan<Vector2> uV)
```

引擎名：`set_uv`

### SetUV

```csharp
public void SetUV(Vector2[] uV)
```

引擎名：`set_uv`

### SetVertexColors

```csharp
public void SetVertexColors(Color[] vertexColors)
```

引擎名：`set_vertex_colors`

### SetVertexColors

```csharp
public void SetVertexColors(ReadOnlySpan<Color> vertexColors)
```

引擎名：`set_vertex_colors`
