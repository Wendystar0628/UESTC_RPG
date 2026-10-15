<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# XROrigin3D

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This is a special node within the AR/VR system that maps the physical location of the center of our tracking space to the virtual location within our game world.

Multiple origin points can be added to the scene tree, but only one can used at a time. All the [`XRCamera3D`](XRCamera3D.md), [`XRController3D`](XRController3D.md), and [`XRAnchor3D`](XRAnchor3D.md) nodes should be direct children of this node for spatial tracking to work correctly.

It is the position of this node that you update when your character needs to move through your game world while we're not moving in the real world. Movement in the real world is always in relation to this origin point.

For example, if your character is driving a car, the `XROrigin3D` node should be a child node of this car. Or, if you're implementing a teleport system to move your character, you should change the position of this node.

## 构造函数

### XROrigin3D

```csharp
public XROrigin3D()
```

## 属性

### Current

```csharp
public bool Current { get; set; }
```

引擎名：`current`

If `true`, this origin node is currently being used by the [`XRServer`](XRServer.md). Only one origin point can be used at a time.

### WorldScale

```csharp
public float WorldScale { get; set; }
```

引擎名：`world_scale`

The scale of the game world compared to the real world. This is the same as [`XRServer.WorldScale`](XRServer.md). By default, most AR/VR platforms assume that 1 game unit corresponds to 1 real world meter.

## 方法

### GetWorldScale

```csharp
public float GetWorldScale()
```

引擎名：`get_world_scale`

### IsCurrent

```csharp
public bool IsCurrent()
```

引擎名：`is_current`

### SetCurrent

```csharp
public void SetCurrent(bool enabled)
```

引擎名：`set_current`

### SetWorldScale

```csharp
public void SetWorldScale(float worldScale)
```

引擎名：`set_world_scale`
