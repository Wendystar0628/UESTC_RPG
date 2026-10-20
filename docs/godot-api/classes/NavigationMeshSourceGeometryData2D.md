<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# NavigationMeshSourceGeometryData2D

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Container for parsed source geometry data used in navigation mesh baking.

## 构造函数

### NavigationMeshSourceGeometryData2D

```csharp
public NavigationMeshSourceGeometryData2D()
```

## 属性

### ObstructionOutlines

```csharp
public Array<Vector2[]> ObstructionOutlines { get; set; }
```

引擎名：`obstruction_outlines`

### ProjectedObstructions

```csharp
public Array ProjectedObstructions { get; set; }
```

引擎名：`projected_obstructions`

### TraversableOutlines

```csharp
public Array<Vector2[]> TraversableOutlines { get; set; }
```

引擎名：`traversable_outlines`

## 方法

### AddObstructionOutline

```csharp
public void AddObstructionOutline(ReadOnlySpan<Vector2> shapeOutline)
```

引擎名：`add_obstruction_outline`

Adds the outline points of a shape as obstructed area.

### AddObstructionOutline

```csharp
public void AddObstructionOutline(Vector2[] shapeOutline)
```

引擎名：`add_obstruction_outline`

Adds the outline points of a shape as obstructed area.

### AddProjectedObstruction

```csharp
public void AddProjectedObstruction(ReadOnlySpan<Vector2> vertices, bool carve)
```

引擎名：`add_projected_obstruction`

Adds a projected obstruction shape to the source geometry. If `carve` is `true` the carved shape will not be affected by additional offsets (e.g. agent radius) of the navigation mesh baking process.

### AddProjectedObstruction

```csharp
public void AddProjectedObstruction(Vector2[] vertices, bool carve)
```

引擎名：`add_projected_obstruction`

Adds a projected obstruction shape to the source geometry. If `carve` is `true` the carved shape will not be affected by additional offsets (e.g. agent radius) of the navigation mesh baking process.

### AddTraversableOutline

```csharp
public void AddTraversableOutline(ReadOnlySpan<Vector2> shapeOutline)
```

引擎名：`add_traversable_outline`

Adds the outline points of a shape as traversable area.

### AddTraversableOutline

```csharp
public void AddTraversableOutline(Vector2[] shapeOutline)
```

引擎名：`add_traversable_outline`

Adds the outline points of a shape as traversable area.

### AppendObstructionOutlines

```csharp
public void AppendObstructionOutlines(Array<Vector2[]> obstructionOutlines)
```

引擎名：`append_obstruction_outlines`

Appends another array of `obstructionOutlines` at the end of the existing obstruction outlines array.

### AppendTraversableOutlines

```csharp
public void AppendTraversableOutlines(Array<Vector2[]> traversableOutlines)
```

引擎名：`append_traversable_outlines`

Appends another array of `traversableOutlines` at the end of the existing traversable outlines array.

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Clears the internal data.

### ClearProjectedObstructions

```csharp
public void ClearProjectedObstructions()
```

引擎名：`clear_projected_obstructions`

Clears all projected obstructions.

### GetBounds

```csharp
public Rect2 GetBounds()
```

引擎名：`get_bounds`

Returns an axis-aligned bounding box that covers all the stored geometry data. The bounds are calculated when calling this function with the result cached until further geometry changes are made.

### GetObstructionOutlines

```csharp
public Array<Vector2[]> GetObstructionOutlines()
```

引擎名：`get_obstruction_outlines`

Returns all the obstructed area outlines arrays.

### GetProjectedObstructions

```csharp
public Array GetProjectedObstructions()
```

引擎名：`get_projected_obstructions`

Returns the projected obstructions as an [`Collections.Array`](Collections.Array.md) of dictionaries. Each [`Collections.Dictionary`](Collections.Dictionary.md) contains the following entries:

- `vertices` - A `System.Single`[] that defines the outline points of the projected shape.

- `carve` - A `System.Boolean` that defines how the projected shape affects the navigation mesh baking. If `true` the projected shape will not be affected by addition offsets, e.g. agent radius.

### GetTraversableOutlines

```csharp
public Array<Vector2[]> GetTraversableOutlines()
```

引擎名：`get_traversable_outlines`

Returns all the traversable area outlines arrays.

### HasData

```csharp
public bool HasData()
```

引擎名：`has_data`

Returns `true` when parsed source geometry data exists.

### Merge

```csharp
public void Merge(NavigationMeshSourceGeometryData2D otherGeometry)
```

引擎名：`merge`

Adds the geometry data of another `NavigationMeshSourceGeometryData2D` to the navigation mesh baking data.

### SetObstructionOutlines

```csharp
public void SetObstructionOutlines(Array<Vector2[]> obstructionOutlines)
```

引擎名：`set_obstruction_outlines`

Sets all the obstructed area outlines arrays.

### SetProjectedObstructions

```csharp
public void SetProjectedObstructions(Array projectedObstructions)
```

引擎名：`set_projected_obstructions`

Sets the projected obstructions with an Array of Dictionaries with the following key value pairs:

### SetTraversableOutlines

```csharp
public void SetTraversableOutlines(Array<Vector2[]> traversableOutlines)
```

引擎名：`set_traversable_outlines`

Sets all the traversable area outlines arrays.
