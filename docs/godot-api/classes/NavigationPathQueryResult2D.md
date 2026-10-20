<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# NavigationPathQueryResult2D

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class stores the result of a 2D navigation path query from the [`NavigationServer2D`](NavigationServer2D.md).

## 构造函数

### NavigationPathQueryResult2D

```csharp
public NavigationPathQueryResult2D()
```

## 属性

### Path

```csharp
public Vector2[] Path { get; set; }
```

引擎名：`path`

The resulting path array from the navigation query. All path array positions are in global coordinates. Without customized query parameters this is the same path as returned by [`NavigationServer2D.MapGetPath`](NavigationServer2D.md).

### PathLength

```csharp
public float PathLength { get; set; }
```

引擎名：`path_length`

Returns the length of the path.

### PathOwnerIds

```csharp
public long[] PathOwnerIds { get; set; }
```

引擎名：`path_owner_ids`

The `ObjectID`s of the [`GodotObject`](GodotObject.md)s which manage the regions and links each point of the path goes through.

### PathRids

```csharp
public Array<Rid> PathRids { get; set; }
```

引擎名：`path_rids`

The [`Rid`](Rid.md)s of the regions and links that each point of the path goes through.

### PathTypes

```csharp
public int[] PathTypes { get; set; }
```

引擎名：`path_types`

The type of navigation primitive (region or link) that each point of the path goes through.

## 方法

### GetPath

```csharp
public Vector2[] GetPath()
```

引擎名：`get_path`

### GetPathLength

```csharp
public float GetPathLength()
```

引擎名：`get_path_length`

### GetPathOwnerIds

```csharp
public long[] GetPathOwnerIds()
```

引擎名：`get_path_owner_ids`

### GetPathRids

```csharp
public Array<Rid> GetPathRids()
```

引擎名：`get_path_rids`

### GetPathTypes

```csharp
public int[] GetPathTypes()
```

引擎名：`get_path_types`

### Reset

```csharp
public void Reset()
```

引擎名：`reset`

Reset the result object to its initial state. This is useful to reuse the object across multiple queries.

### SetPath

```csharp
public void SetPath(ReadOnlySpan<Vector2> path)
```

引擎名：`set_path`

### SetPath

```csharp
public void SetPath(Vector2[] path)
```

引擎名：`set_path`

### SetPathLength

```csharp
public void SetPathLength(float length)
```

引擎名：`set_path_length`

### SetPathOwnerIds

```csharp
public void SetPathOwnerIds(ReadOnlySpan<long> pathOwnerIds)
```

引擎名：`set_path_owner_ids`

### SetPathOwnerIds

```csharp
public void SetPathOwnerIds(long[] pathOwnerIds)
```

引擎名：`set_path_owner_ids`

### SetPathRids

```csharp
public void SetPathRids(Array<Rid> pathRids)
```

引擎名：`set_path_rids`

### SetPathTypes

```csharp
public void SetPathTypes(ReadOnlySpan<int> pathTypes)
```

引擎名：`set_path_types`

### SetPathTypes

```csharp
public void SetPathTypes(int[] pathTypes)
```

引擎名：`set_path_types`

## PathSegmentType

命名空间 `Godot`；enum

### 枚举值

- `Region = 0`：This segment of the path goes through a region.
- `Link = 1`：This segment of the path goes through a link.
