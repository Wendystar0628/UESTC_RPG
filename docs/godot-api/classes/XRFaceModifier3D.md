<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# XRFaceModifier3D

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This node applies weights from an [`XRFaceTracker`](XRFaceTracker.md) to a mesh with supporting face blend shapes.

The [Unified Expressions](https://docs.vrcft.io/docs/tutorial-avatars/tutorial-avatars-extras/unified-blendshapes) blend shapes are supported, as well as ARKit and SRanipal blend shapes.

The node attempts to identify blend shapes based on name matching. Blend shapes should match the names listed in the [Unified Expressions Compatibility](https://docs.vrcft.io/docs/tutorial-avatars/tutorial-avatars-extras/compatibility/overview) chart.

## 构造函数

### XRFaceModifier3D

```csharp
public XRFaceModifier3D()
```

## 属性

### FaceTracker

```csharp
public StringName FaceTracker { get; set; }
```

引擎名：`face_tracker`

The [`XRFaceTracker`](XRFaceTracker.md) path.

### Target

```csharp
public NodePath Target { get; set; }
```

引擎名：`target`

The [`NodePath`](NodePath.md) of the face [`MeshInstance3D`](MeshInstance3D.md).

## 方法

### GetFaceTracker

```csharp
public StringName GetFaceTracker()
```

引擎名：`get_face_tracker`

### GetTarget

```csharp
public NodePath GetTarget()
```

引擎名：`get_target`

### SetFaceTracker

```csharp
public void SetFaceTracker(StringName trackerName)
```

引擎名：`set_face_tracker`

### SetTarget

```csharp
public void SetTarget(NodePath target)
```

引擎名：`set_target`
