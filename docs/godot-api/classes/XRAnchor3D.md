<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# XRAnchor3D

命名空间 `Godot`；class；继承 [`XRNode3D`](XRNode3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

The `XRAnchor3D` point is an [`XRNode3D`](XRNode3D.md) that maps a real world location identified by the AR platform to a position within the game world. For example, as long as plane detection in ARKit is on, ARKit will identify and update the position of planes (tables, floors, etc.) and create anchors for them.

This node is mapped to one of the anchors through its unique ID. When you receive a signal that a new anchor is available, you should add this node to your scene for that anchor. You can predefine nodes and set the ID; the nodes will simply remain on `(0, 0, 0)` until a plane is recognized.

Keep in mind that, as long as plane detection is enabled, the size, placing and orientation of an anchor will be updated as the detection logic learns more about the real world out there especially if only part of the surface is in view.

## 构造函数

### XRAnchor3D

```csharp
public XRAnchor3D()
```

## 方法

### GetPlane

```csharp
public Plane GetPlane()
```

引擎名：`get_plane`

Returns a plane aligned with our anchor; handy for intersection testing.

### GetSize

```csharp
public Vector3 GetSize()
```

引擎名：`get_size`

Returns the estimated size of the plane that was detected. Say when the anchor relates to a table in the real world, this is the estimated size of the surface of that table.
