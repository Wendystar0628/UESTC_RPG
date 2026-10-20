<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SkeletonIK3D

命名空间 `Godot`；class；继承 [`SkeletonModifier3D`](SkeletonModifier3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

SkeletonIK3D is used to rotate all bones of a [`Skeleton3D`](Skeleton3D.md) bone chain a way that places the end bone at a desired 3D position. A typical scenario for IK in games is to place a character's feet on the ground or a character's hands on a currently held object. SkeletonIK uses FabrikInverseKinematic internally to solve the bone chain and applies the results to the [`Skeleton3D`](Skeleton3D.md) `bones_global_pose_override` property for all affected bones in the chain. If fully applied, this overwrites any bone transform from [`Animation`](Animation.md)s or bone custom poses set by users. The applied amount can be controlled with the [`SkeletonModifier3D.Influence`](SkeletonModifier3D.md) property.

```csharp
# Apply IK effect automatically on every new frame (not the current)
skeleton_ik_node.start()

# Apply IK effect only on the current frame
skeleton_ik_node.start(true)

# Stop IK effect and reset bones_global_pose_override on Skeleton
skeleton_ik_node.stop()

# Apply full IK effect
skeleton_ik_node.set_influence(1.0)

# Apply half IK effect
skeleton_ik_node.set_influence(0.5)

# Apply zero IK effect (a value at or below 0.01 also removes bones_global_pose_override on Skeleton)
skeleton_ik_node.set_influence(0.0)

```

## 构造函数

### SkeletonIK3D

```csharp
public SkeletonIK3D()
```

## 属性

### Interpolation

```csharp
public float Interpolation { get; set; }
```

引擎名：`interpolation`

Interpolation value for how much the IK results are applied to the current skeleton bone chain. A value of `1.0` will overwrite all skeleton bone transforms completely while a value of `0.0` will visually disable the SkeletonIK.

### Magnet

```csharp
public Vector3 Magnet { get; set; }
```

引擎名：`magnet`

Secondary target position (first is `Target` property or `TargetNode`) for the IK chain. Use magnet position (pole target) to control the bending of the IK chain. Only works if the bone chain has more than 2 bones. The middle chain bone position will be linearly interpolated with the magnet position.

### MaxIterations

```csharp
public int MaxIterations { get; set; }
```

引擎名：`max_iterations`

Number of iteration loops used by the IK solver to produce more accurate (and elegant) bone chain results.

### MinDistance

```csharp
public float MinDistance { get; set; }
```

引擎名：`min_distance`

The minimum distance between bone and goal target. If the distance is below this value, the IK solver stops further iterations.

### OverrideTipBasis

```csharp
public bool OverrideTipBasis { get; set; }
```

引擎名：`override_tip_basis`

If `true` overwrites the rotation of the tip bone with the rotation of the `Target` (or `TargetNode` if defined).

### RootBone

```csharp
public StringName RootBone { get; set; }
```

引擎名：`root_bone`

The name of the current root bone, the first bone in the IK chain.

### Target

```csharp
public Transform3D Target { get; set; }
```

引擎名：`target`

First target of the IK chain where the tip bone is placed and, if `OverrideTipBasis` is `true`, how the tip bone is rotated. If a `TargetNode` path is available the nodes transform is used instead and this property is ignored.

### TargetNode

```csharp
public NodePath TargetNode { get; set; }
```

引擎名：`target_node`

Target node [`NodePath`](NodePath.md) for the IK chain. If available, the node's current [`Transform3D`](Transform3D.md) is used instead of the `Target` property.

### TipBone

```csharp
public StringName TipBone { get; set; }
```

引擎名：`tip_bone`

The name of the current tip bone, the last bone in the IK chain placed at the `Target` transform (or `TargetNode` if defined).

### UseMagnet

```csharp
public bool UseMagnet { get; set; }
```

引擎名：`use_magnet`

If `true`, instructs the IK solver to consider the secondary magnet target (pole target) when calculating the bone chain. Use the magnet position (pole target) to control the bending of the IK chain.

## 方法

### GetInterpolation

```csharp
public float GetInterpolation()
```

引擎名：`get_interpolation`

### GetMagnetPosition

```csharp
public Vector3 GetMagnetPosition()
```

引擎名：`get_magnet_position`

### GetMaxIterations

```csharp
public int GetMaxIterations()
```

引擎名：`get_max_iterations`

### GetMinDistance

```csharp
public float GetMinDistance()
```

引擎名：`get_min_distance`

### GetParentSkeleton

```csharp
public Skeleton3D GetParentSkeleton()
```

引擎名：`get_parent_skeleton`

Returns the parent [`Skeleton3D`](Skeleton3D.md) node that was present when SkeletonIK entered the scene tree. Returns `null` if the parent node was not a [`Skeleton3D`](Skeleton3D.md) node when SkeletonIK3D entered the scene tree.

### GetRootBone

```csharp
public StringName GetRootBone()
```

引擎名：`get_root_bone`

### GetTargetNode

```csharp
public NodePath GetTargetNode()
```

引擎名：`get_target_node`

### GetTargetTransform

```csharp
public Transform3D GetTargetTransform()
```

引擎名：`get_target_transform`

### GetTipBone

```csharp
public StringName GetTipBone()
```

引擎名：`get_tip_bone`

### IsOverrideTipBasis

```csharp
public bool IsOverrideTipBasis()
```

引擎名：`is_override_tip_basis`

### IsRunning

```csharp
public bool IsRunning()
```

引擎名：`is_running`

Returns `true` if SkeletonIK is applying IK effects on continues frames to the [`Skeleton3D`](Skeleton3D.md) bones. Returns `false` if SkeletonIK is stopped or `Start()` was used with the `one_time` parameter set to `true`.

### IsUsingMagnet

```csharp
public bool IsUsingMagnet()
```

引擎名：`is_using_magnet`

### SetInterpolation

```csharp
public void SetInterpolation(float interpolation)
```

引擎名：`set_interpolation`

### SetMagnetPosition

```csharp
public void SetMagnetPosition(Vector3 localPosition)
```

引擎名：`set_magnet_position`

### SetMaxIterations

```csharp
public void SetMaxIterations(int iterations)
```

引擎名：`set_max_iterations`

### SetMinDistance

```csharp
public void SetMinDistance(float minDistance)
```

引擎名：`set_min_distance`

### SetOverrideTipBasis

```csharp
public void SetOverrideTipBasis(bool override)
```

引擎名：`set_override_tip_basis`

### SetRootBone

```csharp
public void SetRootBone(StringName rootBone)
```

引擎名：`set_root_bone`

### SetTargetNode

```csharp
public void SetTargetNode(NodePath node)
```

引擎名：`set_target_node`

### SetTargetTransform

```csharp
public void SetTargetTransform(Transform3D target)
```

引擎名：`set_target_transform`

### SetTipBone

```csharp
public void SetTipBone(StringName tipBone)
```

引擎名：`set_tip_bone`

### SetUseMagnet

```csharp
public void SetUseMagnet(bool use)
```

引擎名：`set_use_magnet`

### Start

```csharp
public void Start(bool oneTime = false)
```

引擎名：`start`

Starts applying IK effects on each frame to the [`Skeleton3D`](Skeleton3D.md) bones but will only take effect starting on the next frame. If `oneTime` is `true`, this will take effect immediately but also reset on the next frame.

### Stop

```csharp
public void Stop()
```

引擎名：`stop`

Stops applying IK effects on each frame to the [`Skeleton3D`](Skeleton3D.md) bones and also calls [`Skeleton3D.ClearBonesGlobalPoseOverride`](Skeleton3D.md) to remove existing overrides on all bones.
