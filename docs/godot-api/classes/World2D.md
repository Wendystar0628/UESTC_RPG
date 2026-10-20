<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# World2D

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Class that has everything pertaining to a 2D world: A physics space, a canvas, and a sound space. 2D nodes register their resources into the current 2D world.

## 构造函数

### World2D

```csharp
public World2D()
```

## 属性

### Canvas

```csharp
public Rid Canvas { get; }
```

引擎名：`canvas`

The [`Rid`](Rid.md) of this world's canvas resource. Used by the [`RenderingServer`](RenderingServer.md) for 2D drawing.

### DirectSpaceState

```csharp
public PhysicsDirectSpaceState2D DirectSpaceState { get; }
```

引擎名：`direct_space_state`

Direct access to the world's physics 2D space state. Used for querying current and potential collisions. When using multi-threaded physics, access is limited to [`Node._PhysicsProcess()`](Node.md) in the main thread.

### NavigationMap

```csharp
public Rid NavigationMap { get; }
```

引擎名：`navigation_map`

The [`Rid`](Rid.md) of this world's navigation map. Used by the [`NavigationServer2D`](NavigationServer2D.md).

### Space

```csharp
public Rid Space { get; }
```

引擎名：`space`

The [`Rid`](Rid.md) of this world's physics space resource. Used by the [`PhysicsServer2D`](PhysicsServer2D.md) for 2D physics, treating it as both a space and an area.

## 方法

### GetCanvas

```csharp
public Rid GetCanvas()
```

引擎名：`get_canvas`

### GetDirectSpaceState

```csharp
public PhysicsDirectSpaceState2D GetDirectSpaceState()
```

引擎名：`get_direct_space_state`

### GetNavigationMap

```csharp
public Rid GetNavigationMap()
```

引擎名：`get_navigation_map`

### GetSpace

```csharp
public Rid GetSpace()
```

引擎名：`get_space`
