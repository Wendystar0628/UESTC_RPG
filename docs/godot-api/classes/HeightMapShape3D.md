<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# HeightMapShape3D

命名空间 `Godot`；class；继承 [`Shape3D`](Shape3D.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A 3D heightmap shape, intended for use in physics to provide a shape for a [`CollisionShape3D`](CollisionShape3D.md). This type is most commonly used for terrain with vertices placed in a fixed-width grid.

The heightmap is represented as a 2D grid of height values, which represent the position of grid points on the Y axis. Grid points are spaced 1 unit apart on the X and Z axes, and the grid is centered on the origin of the [`CollisionShape3D`](CollisionShape3D.md) node. Internally, each grid square is divided into two triangles.

Due to the nature of the heightmap, it cannot be used to model overhangs or caves, which would require multiple vertices at the same vertical location. Holes can be punched through the collision by assigning `@GDScript.NAN` to the height of the desired vertices (this is supported in both GodotPhysics3D and Jolt Physics). You could then insert meshes with their own separate collision to provide overhangs, caves, and so on.

**Performance:** `HeightMapShape3D` is faster to check collisions against than [`ConcavePolygonShape3D`](ConcavePolygonShape3D.md), but it is significantly slower than primitive shapes like [`BoxShape3D`](BoxShape3D.md).

A heightmap collision shape can also be built by using an [`Image`](Image.md) reference:

**Note:** If you need to use a spacing different than 1 unit, you can adjust the [`Node3D.Scale`](Node3D.md) of the shape. However, keep in mind that GodotPhysics3D does not support non-uniform scaling: you'll need to scale the Y axis by the same amount as the X and Z axes, which means the values in `MapData` will need to be pre-scaled by the inverse of that scale. Also note that GodotPhysics3D does not support scaling at all for dynamic bodies (that is, non-frozen [`RigidBody3D`](RigidBody3D.md) nodes); to use a scaled `HeightMapShape3D` with those, you will need to use Jolt Physics.

## 构造函数

### HeightMapShape3D

```csharp
public HeightMapShape3D()
```

## 属性

### MapData

```csharp
public float[] MapData { get; set; }
```

引擎名：`map_data`

Heightmap data. The array's size must be equal to `MapWidth` multiplied by `MapDepth`.

### MapDepth

```csharp
public int MapDepth { get; set; }
```

引擎名：`map_depth`

Number of vertices in the depth of the heightmap. Changing this will resize the `MapData`.

### MapWidth

```csharp
public int MapWidth { get; set; }
```

引擎名：`map_width`

Number of vertices in the width of the heightmap. Changing this will resize the `MapData`.

## 方法

### GetMapData

```csharp
public float[] GetMapData()
```

引擎名：`get_map_data`

### GetMapDepth

```csharp
public int GetMapDepth()
```

引擎名：`get_map_depth`

### GetMapWidth

```csharp
public int GetMapWidth()
```

引擎名：`get_map_width`

### GetMaxHeight

```csharp
public float GetMaxHeight()
```

引擎名：`get_max_height`

Returns the largest height value found in `MapData`. Recalculates only when `MapData` changes.

### GetMinHeight

```csharp
public float GetMinHeight()
```

引擎名：`get_min_height`

Returns the smallest height value found in `MapData`. Recalculates only when `MapData` changes.

### SetMapData

```csharp
public void SetMapData(ReadOnlySpan<float> data)
```

引擎名：`set_map_data`

### SetMapData

```csharp
public void SetMapData(float[] data)
```

引擎名：`set_map_data`

### SetMapDepth

```csharp
public void SetMapDepth(int height)
```

引擎名：`set_map_depth`

### SetMapWidth

```csharp
public void SetMapWidth(int width)
```

引擎名：`set_map_width`

### UpdateMapDataFromImage

```csharp
public void UpdateMapDataFromImage(Image image, float heightMin, float heightMax)
```

引擎名：`update_map_data_from_image`

Updates `MapData` with data read from an [`Image`](Image.md) reference. Automatically resizes heightmap `MapWidth` and `MapDepth` to fit the full image width and height.

The image needs to be in either [`Image.Format.Rf`](Image.md) (32 bit), [`Image.Format.Rh`](Image.md) (16 bit), or [`Image.Format.R8`](Image.md) (8 bit).

Each image pixel is read in as a float on the range from `0.0` (black pixel) to `1.0` (white pixel). This range value gets remapped to `heightMin` and `heightMax` to form the final height value.

**Note:** Using a heightmap with 16-bit or 32-bit data, stored in EXR or HDR format is recommended. Using 8-bit height data, or a format like PNG that Godot imports as 8-bit, will result in a terraced terrain.
