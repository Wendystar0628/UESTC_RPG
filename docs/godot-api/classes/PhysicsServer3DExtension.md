<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicsServer3DExtension

命名空间 `Godot`；class；继承 [`PhysicsServer3DInstance`](PhysicsServer3DInstance.md) → [`GodotObject`](GodotObject.md)

This class extends [`PhysicsServer3D`](PhysicsServer3D.md) by providing additional virtual methods that can be overridden. When these methods are overridden, they will be called instead of the internal methods of the physics server.

Intended for use with GDExtension to create custom implementations of [`PhysicsServer3D`](PhysicsServer3D.md).

## 构造函数

### PhysicsServer3DExtension

```csharp
public PhysicsServer3DExtension()
```

## 方法

### BodyTestMotionIsExcludingBody

```csharp
public bool BodyTestMotionIsExcludingBody(Rid body)
```

引擎名：`body_test_motion_is_excluding_body`

### BodyTestMotionIsExcludingObject

```csharp
public bool BodyTestMotionIsExcludingObject(ulong object)
```

引擎名：`body_test_motion_is_excluding_object`

### _AreaAddShape

```csharp
public virtual void _AreaAddShape(Rid area, Rid shape, Transform3D transform, bool disabled)
```

引擎名：`_area_add_shape`

### _AreaAttachObjectInstanceId

```csharp
public virtual void _AreaAttachObjectInstanceId(Rid area, ulong id)
```

引擎名：`_area_attach_object_instance_id`

### _AreaClearShapes

```csharp
public virtual void _AreaClearShapes(Rid area)
```

引擎名：`_area_clear_shapes`

### _AreaCreate

```csharp
public virtual Rid _AreaCreate()
```

引擎名：`_area_create`

### _AreaGetCollisionLayer

```csharp
public virtual uint _AreaGetCollisionLayer(Rid area)
```

引擎名：`_area_get_collision_layer`

### _AreaGetCollisionMask

```csharp
public virtual uint _AreaGetCollisionMask(Rid area)
```

引擎名：`_area_get_collision_mask`

### _AreaGetObjectInstanceId

```csharp
public virtual ulong _AreaGetObjectInstanceId(Rid area)
```

引擎名：`_area_get_object_instance_id`

### _AreaGetParam

```csharp
public virtual Variant _AreaGetParam(Rid area, PhysicsServer3D.AreaParameter param)
```

引擎名：`_area_get_param`

### _AreaGetShape

```csharp
public virtual Rid _AreaGetShape(Rid area, int shapeIdx)
```

引擎名：`_area_get_shape`

### _AreaGetShapeCount

```csharp
public virtual int _AreaGetShapeCount(Rid area)
```

引擎名：`_area_get_shape_count`

### _AreaGetShapeTransform

```csharp
public virtual Transform3D _AreaGetShapeTransform(Rid area, int shapeIdx)
```

引擎名：`_area_get_shape_transform`

### _AreaGetSpace

```csharp
public virtual Rid _AreaGetSpace(Rid area)
```

引擎名：`_area_get_space`

### _AreaGetTransform

```csharp
public virtual Transform3D _AreaGetTransform(Rid area)
```

引擎名：`_area_get_transform`

### _AreaRemoveShape

```csharp
public virtual void _AreaRemoveShape(Rid area, int shapeIdx)
```

引擎名：`_area_remove_shape`

### _AreaSetAreaMonitorCallback

```csharp
public virtual void _AreaSetAreaMonitorCallback(Rid area, Callable callback)
```

引擎名：`_area_set_area_monitor_callback`

### _AreaSetCollisionLayer

```csharp
public virtual void _AreaSetCollisionLayer(Rid area, uint layer)
```

引擎名：`_area_set_collision_layer`

### _AreaSetCollisionMask

```csharp
public virtual void _AreaSetCollisionMask(Rid area, uint mask)
```

引擎名：`_area_set_collision_mask`

### _AreaSetMonitorCallback

```csharp
public virtual void _AreaSetMonitorCallback(Rid area, Callable callback)
```

引擎名：`_area_set_monitor_callback`

### _AreaSetMonitorable

```csharp
public virtual void _AreaSetMonitorable(Rid area, bool monitorable)
```

引擎名：`_area_set_monitorable`

### _AreaSetParam

```csharp
public virtual void _AreaSetParam(Rid area, PhysicsServer3D.AreaParameter param, Variant value)
```

引擎名：`_area_set_param`

### _AreaSetRayPickable

```csharp
public virtual void _AreaSetRayPickable(Rid area, bool enable)
```

引擎名：`_area_set_ray_pickable`

### _AreaSetShape

```csharp
public virtual void _AreaSetShape(Rid area, int shapeIdx, Rid shape)
```

引擎名：`_area_set_shape`

### _AreaSetShapeDisabled

```csharp
public virtual void _AreaSetShapeDisabled(Rid area, int shapeIdx, bool disabled)
```

引擎名：`_area_set_shape_disabled`

### _AreaSetShapeTransform

```csharp
public virtual void _AreaSetShapeTransform(Rid area, int shapeIdx, Transform3D transform)
```

引擎名：`_area_set_shape_transform`

### _AreaSetSpace

```csharp
public virtual void _AreaSetSpace(Rid area, Rid space)
```

引擎名：`_area_set_space`

### _AreaSetTransform

```csharp
public virtual void _AreaSetTransform(Rid area, Transform3D transform)
```

引擎名：`_area_set_transform`

### _BodyAddCollisionException

```csharp
public virtual void _BodyAddCollisionException(Rid body, Rid exceptedBody)
```

引擎名：`_body_add_collision_exception`

### _BodyAddConstantCentralForce

```csharp
public virtual void _BodyAddConstantCentralForce(Rid body, Vector3 force)
```

引擎名：`_body_add_constant_central_force`

### _BodyAddConstantForce

```csharp
public virtual void _BodyAddConstantForce(Rid body, Vector3 force, Vector3 position)
```

引擎名：`_body_add_constant_force`

### _BodyAddConstantTorque

```csharp
public virtual void _BodyAddConstantTorque(Rid body, Vector3 torque)
```

引擎名：`_body_add_constant_torque`

### _BodyAddShape

```csharp
public virtual void _BodyAddShape(Rid body, Rid shape, Transform3D transform, bool disabled)
```

引擎名：`_body_add_shape`

### _BodyApplyCentralForce

```csharp
public virtual void _BodyApplyCentralForce(Rid body, Vector3 force)
```

引擎名：`_body_apply_central_force`

### _BodyApplyCentralImpulse

```csharp
public virtual void _BodyApplyCentralImpulse(Rid body, Vector3 impulse)
```

引擎名：`_body_apply_central_impulse`

### _BodyApplyForce

```csharp
public virtual void _BodyApplyForce(Rid body, Vector3 force, Vector3 position)
```

引擎名：`_body_apply_force`

### _BodyApplyImpulse

```csharp
public virtual void _BodyApplyImpulse(Rid body, Vector3 impulse, Vector3 position)
```

引擎名：`_body_apply_impulse`

### _BodyApplyTorque

```csharp
public virtual void _BodyApplyTorque(Rid body, Vector3 torque)
```

引擎名：`_body_apply_torque`

### _BodyApplyTorqueImpulse

```csharp
public virtual void _BodyApplyTorqueImpulse(Rid body, Vector3 impulse)
```

引擎名：`_body_apply_torque_impulse`

### _BodyAttachObjectInstanceId

```csharp
public virtual void _BodyAttachObjectInstanceId(Rid body, ulong id)
```

引擎名：`_body_attach_object_instance_id`

### _BodyClearShapes

```csharp
public virtual void _BodyClearShapes(Rid body)
```

引擎名：`_body_clear_shapes`

### _BodyCreate

```csharp
public virtual Rid _BodyCreate()
```

引擎名：`_body_create`

### _BodyGetCollisionExceptions

```csharp
public virtual Array<Rid> _BodyGetCollisionExceptions(Rid body)
```

引擎名：`_body_get_collision_exceptions`

### _BodyGetCollisionLayer

```csharp
public virtual uint _BodyGetCollisionLayer(Rid body)
```

引擎名：`_body_get_collision_layer`

### _BodyGetCollisionMask

```csharp
public virtual uint _BodyGetCollisionMask(Rid body)
```

引擎名：`_body_get_collision_mask`

### _BodyGetCollisionPriority

```csharp
public virtual float _BodyGetCollisionPriority(Rid body)
```

引擎名：`_body_get_collision_priority`

### _BodyGetConstantForce

```csharp
public virtual Vector3 _BodyGetConstantForce(Rid body)
```

引擎名：`_body_get_constant_force`

### _BodyGetConstantTorque

```csharp
public virtual Vector3 _BodyGetConstantTorque(Rid body)
```

引擎名：`_body_get_constant_torque`

### _BodyGetContactsReportedDepthThreshold

```csharp
public virtual float _BodyGetContactsReportedDepthThreshold(Rid body)
```

引擎名：`_body_get_contacts_reported_depth_threshold`

### _BodyGetDirectState

```csharp
public virtual PhysicsDirectBodyState3D _BodyGetDirectState(Rid body)
```

引擎名：`_body_get_direct_state`

### _BodyGetMaxContactsReported

```csharp
public virtual int _BodyGetMaxContactsReported(Rid body)
```

引擎名：`_body_get_max_contacts_reported`

### _BodyGetMode

```csharp
public virtual PhysicsServer3D.BodyMode _BodyGetMode(Rid body)
```

引擎名：`_body_get_mode`

### _BodyGetObjectInstanceId

```csharp
public virtual ulong _BodyGetObjectInstanceId(Rid body)
```

引擎名：`_body_get_object_instance_id`

### _BodyGetParam

```csharp
public virtual Variant _BodyGetParam(Rid body, PhysicsServer3D.BodyParameter param)
```

引擎名：`_body_get_param`

### _BodyGetShape

```csharp
public virtual Rid _BodyGetShape(Rid body, int shapeIdx)
```

引擎名：`_body_get_shape`

### _BodyGetShapeCount

```csharp
public virtual int _BodyGetShapeCount(Rid body)
```

引擎名：`_body_get_shape_count`

### _BodyGetShapeTransform

```csharp
public virtual Transform3D _BodyGetShapeTransform(Rid body, int shapeIdx)
```

引擎名：`_body_get_shape_transform`

### _BodyGetSpace

```csharp
public virtual Rid _BodyGetSpace(Rid body)
```

引擎名：`_body_get_space`

### _BodyGetState

```csharp
public virtual Variant _BodyGetState(Rid body, PhysicsServer3D.BodyState state)
```

引擎名：`_body_get_state`

### _BodyGetUserFlags

```csharp
public virtual uint _BodyGetUserFlags(Rid body)
```

引擎名：`_body_get_user_flags`

### _BodyIsAxisLocked

```csharp
public virtual bool _BodyIsAxisLocked(Rid body, PhysicsServer3D.BodyAxis axis)
```

引擎名：`_body_is_axis_locked`

### _BodyIsContinuousCollisionDetectionEnabled

```csharp
public virtual bool _BodyIsContinuousCollisionDetectionEnabled(Rid body)
```

引擎名：`_body_is_continuous_collision_detection_enabled`

### _BodyIsOmittingForceIntegration

```csharp
public virtual bool _BodyIsOmittingForceIntegration(Rid body)
```

引擎名：`_body_is_omitting_force_integration`

### _BodyRemoveCollisionException

```csharp
public virtual void _BodyRemoveCollisionException(Rid body, Rid exceptedBody)
```

引擎名：`_body_remove_collision_exception`

### _BodyRemoveShape

```csharp
public virtual void _BodyRemoveShape(Rid body, int shapeIdx)
```

引擎名：`_body_remove_shape`

### _BodyResetMassProperties

```csharp
public virtual void _BodyResetMassProperties(Rid body)
```

引擎名：`_body_reset_mass_properties`

### _BodySetAxisLock

```csharp
public virtual void _BodySetAxisLock(Rid body, PhysicsServer3D.BodyAxis axis, bool lock)
```

引擎名：`_body_set_axis_lock`

### _BodySetAxisVelocity

```csharp
public virtual void _BodySetAxisVelocity(Rid body, Vector3 axisVelocity)
```

引擎名：`_body_set_axis_velocity`

### _BodySetCollisionLayer

```csharp
public virtual void _BodySetCollisionLayer(Rid body, uint layer)
```

引擎名：`_body_set_collision_layer`

### _BodySetCollisionMask

```csharp
public virtual void _BodySetCollisionMask(Rid body, uint mask)
```

引擎名：`_body_set_collision_mask`

### _BodySetCollisionPriority

```csharp
public virtual void _BodySetCollisionPriority(Rid body, float priority)
```

引擎名：`_body_set_collision_priority`

### _BodySetConstantForce

```csharp
public virtual void _BodySetConstantForce(Rid body, Vector3 force)
```

引擎名：`_body_set_constant_force`

### _BodySetConstantTorque

```csharp
public virtual void _BodySetConstantTorque(Rid body, Vector3 torque)
```

引擎名：`_body_set_constant_torque`

### _BodySetContactsReportedDepthThreshold

```csharp
public virtual void _BodySetContactsReportedDepthThreshold(Rid body, float threshold)
```

引擎名：`_body_set_contacts_reported_depth_threshold`

### _BodySetEnableContinuousCollisionDetection

```csharp
public virtual void _BodySetEnableContinuousCollisionDetection(Rid body, bool enable)
```

引擎名：`_body_set_enable_continuous_collision_detection`

### _BodySetForceIntegrationCallback

```csharp
public virtual void _BodySetForceIntegrationCallback(Rid body, Callable callable, Variant userdata)
```

引擎名：`_body_set_force_integration_callback`

### _BodySetMaxContactsReported

```csharp
public virtual void _BodySetMaxContactsReported(Rid body, int amount)
```

引擎名：`_body_set_max_contacts_reported`

### _BodySetMode

```csharp
public virtual void _BodySetMode(Rid body, PhysicsServer3D.BodyMode mode)
```

引擎名：`_body_set_mode`

### _BodySetOmitForceIntegration

```csharp
public virtual void _BodySetOmitForceIntegration(Rid body, bool enable)
```

引擎名：`_body_set_omit_force_integration`

### _BodySetParam

```csharp
public virtual void _BodySetParam(Rid body, PhysicsServer3D.BodyParameter param, Variant value)
```

引擎名：`_body_set_param`

### _BodySetRayPickable

```csharp
public virtual void _BodySetRayPickable(Rid body, bool enable)
```

引擎名：`_body_set_ray_pickable`

### _BodySetShape

```csharp
public virtual void _BodySetShape(Rid body, int shapeIdx, Rid shape)
```

引擎名：`_body_set_shape`

### _BodySetShapeDisabled

```csharp
public virtual void _BodySetShapeDisabled(Rid body, int shapeIdx, bool disabled)
```

引擎名：`_body_set_shape_disabled`

### _BodySetShapeTransform

```csharp
public virtual void _BodySetShapeTransform(Rid body, int shapeIdx, Transform3D transform)
```

引擎名：`_body_set_shape_transform`

### _BodySetSpace

```csharp
public virtual void _BodySetSpace(Rid body, Rid space)
```

引擎名：`_body_set_space`

### _BodySetState

```csharp
public virtual void _BodySetState(Rid body, PhysicsServer3D.BodyState state, Variant value)
```

引擎名：`_body_set_state`

### _BodySetStateSyncCallback

```csharp
public virtual void _BodySetStateSyncCallback(Rid body, Callable callable)
```

引擎名：`_body_set_state_sync_callback`

### _BodySetUserFlags

```csharp
public virtual void _BodySetUserFlags(Rid body, uint flags)
```

引擎名：`_body_set_user_flags`

### _BoxShapeCreate

```csharp
public virtual Rid _BoxShapeCreate()
```

引擎名：`_box_shape_create`

### _CapsuleShapeCreate

```csharp
public virtual Rid _CapsuleShapeCreate()
```

引擎名：`_capsule_shape_create`

### _ConcavePolygonShapeCreate

```csharp
public virtual Rid _ConcavePolygonShapeCreate()
```

引擎名：`_concave_polygon_shape_create`

### _ConeTwistJointGetParam

```csharp
public virtual float _ConeTwistJointGetParam(Rid joint, PhysicsServer3D.ConeTwistJointParam param)
```

引擎名：`_cone_twist_joint_get_param`

### _ConeTwistJointSetParam

```csharp
public virtual void _ConeTwistJointSetParam(Rid joint, PhysicsServer3D.ConeTwistJointParam param, float value)
```

引擎名：`_cone_twist_joint_set_param`

### _ConvexPolygonShapeCreate

```csharp
public virtual Rid _ConvexPolygonShapeCreate()
```

引擎名：`_convex_polygon_shape_create`

### _CustomShapeCreate

```csharp
public virtual Rid _CustomShapeCreate()
```

引擎名：`_custom_shape_create`

### _CylinderShapeCreate

```csharp
public virtual Rid _CylinderShapeCreate()
```

引擎名：`_cylinder_shape_create`

### _EndSync

```csharp
public virtual void _EndSync()
```

引擎名：`_end_sync`

### _Finish

```csharp
public virtual void _Finish()
```

引擎名：`_finish`

### _FlushQueries

```csharp
public virtual void _FlushQueries()
```

引擎名：`_flush_queries`

### _FreeRid

```csharp
public virtual void _FreeRid(Rid rid)
```

引擎名：`_free_rid`

### _Generic6DofJointGetFlag

```csharp
public virtual bool _Generic6DofJointGetFlag(Rid joint, Vector3.Axis axis, PhysicsServer3D.G6DofJointAxisFlag flag)
```

### _Generic6DofJointGetParam

```csharp
public virtual float _Generic6DofJointGetParam(Rid joint, Vector3.Axis axis, PhysicsServer3D.G6DofJointAxisParam param)
```

### _Generic6DofJointSetFlag

```csharp
public virtual void _Generic6DofJointSetFlag(Rid joint, Vector3.Axis axis, PhysicsServer3D.G6DofJointAxisFlag flag, bool enable)
```

### _Generic6DofJointSetParam

```csharp
public virtual void _Generic6DofJointSetParam(Rid joint, Vector3.Axis axis, PhysicsServer3D.G6DofJointAxisParam param, float value)
```

### _GetProcessInfo

```csharp
public virtual int _GetProcessInfo(PhysicsServer3D.ProcessInfo processInfo)
```

引擎名：`_get_process_info`

### _HeightmapShapeCreate

```csharp
public virtual Rid _HeightmapShapeCreate()
```

引擎名：`_heightmap_shape_create`

### _HingeJointGetFlag

```csharp
public virtual bool _HingeJointGetFlag(Rid joint, PhysicsServer3D.HingeJointFlag flag)
```

引擎名：`_hinge_joint_get_flag`

### _HingeJointGetParam

```csharp
public virtual float _HingeJointGetParam(Rid joint, PhysicsServer3D.HingeJointParam param)
```

引擎名：`_hinge_joint_get_param`

### _HingeJointSetFlag

```csharp
public virtual void _HingeJointSetFlag(Rid joint, PhysicsServer3D.HingeJointFlag flag, bool enabled)
```

引擎名：`_hinge_joint_set_flag`

### _HingeJointSetParam

```csharp
public virtual void _HingeJointSetParam(Rid joint, PhysicsServer3D.HingeJointParam param, float value)
```

引擎名：`_hinge_joint_set_param`

### _Init

```csharp
public virtual void _Init()
```

引擎名：`_init`

### _IsFlushingQueries

```csharp
public virtual bool _IsFlushingQueries()
```

引擎名：`_is_flushing_queries`

### _JointClear

```csharp
public virtual void _JointClear(Rid joint)
```

引擎名：`_joint_clear`

### _JointCreate

```csharp
public virtual Rid _JointCreate()
```

引擎名：`_joint_create`

### _JointDisableCollisionsBetweenBodies

```csharp
public virtual void _JointDisableCollisionsBetweenBodies(Rid joint, bool disable)
```

引擎名：`_joint_disable_collisions_between_bodies`

### _JointGetSolverPriority

```csharp
public virtual int _JointGetSolverPriority(Rid joint)
```

引擎名：`_joint_get_solver_priority`

### _JointGetType

```csharp
public virtual PhysicsServer3D.JointType _JointGetType(Rid joint)
```

引擎名：`_joint_get_type`

### _JointIsDisabledCollisionsBetweenBodies

```csharp
public virtual bool _JointIsDisabledCollisionsBetweenBodies(Rid joint)
```

引擎名：`_joint_is_disabled_collisions_between_bodies`

### _JointMakeConeTwist

```csharp
public virtual void _JointMakeConeTwist(Rid joint, Rid bodyA, Transform3D localRefA, Rid bodyB, Transform3D localRefB)
```

引擎名：`_joint_make_cone_twist`

### _JointMakeGeneric6Dof

```csharp
public virtual void _JointMakeGeneric6Dof(Rid joint, Rid bodyA, Transform3D localRefA, Rid bodyB, Transform3D localRefB)
```

### _JointMakeHinge

```csharp
public virtual void _JointMakeHinge(Rid joint, Rid bodyA, Transform3D hingeA, Rid bodyB, Transform3D hingeB)
```

引擎名：`_joint_make_hinge`

### _JointMakeHingeSimple

```csharp
public virtual void _JointMakeHingeSimple(Rid joint, Rid bodyA, Vector3 pivotA, Vector3 axisA, Rid bodyB, Vector3 pivotB, Vector3 axisB)
```

引擎名：`_joint_make_hinge_simple`

### _JointMakePin

```csharp
public virtual void _JointMakePin(Rid joint, Rid bodyA, Vector3 localA, Rid bodyB, Vector3 localB)
```

引擎名：`_joint_make_pin`

### _JointMakeSlider

```csharp
public virtual void _JointMakeSlider(Rid joint, Rid bodyA, Transform3D localRefA, Rid bodyB, Transform3D localRefB)
```

引擎名：`_joint_make_slider`

### _JointSetSolverPriority

```csharp
public virtual void _JointSetSolverPriority(Rid joint, int priority)
```

引擎名：`_joint_set_solver_priority`

### _PinJointGetLocalA

```csharp
public virtual Vector3 _PinJointGetLocalA(Rid joint)
```

引擎名：`_pin_joint_get_local_a`

### _PinJointGetLocalB

```csharp
public virtual Vector3 _PinJointGetLocalB(Rid joint)
```

引擎名：`_pin_joint_get_local_b`

### _PinJointGetParam

```csharp
public virtual float _PinJointGetParam(Rid joint, PhysicsServer3D.PinJointParam param)
```

引擎名：`_pin_joint_get_param`

### _PinJointSetLocalA

```csharp
public virtual void _PinJointSetLocalA(Rid joint, Vector3 localA)
```

引擎名：`_pin_joint_set_local_a`

### _PinJointSetLocalB

```csharp
public virtual void _PinJointSetLocalB(Rid joint, Vector3 localB)
```

引擎名：`_pin_joint_set_local_b`

### _PinJointSetParam

```csharp
public virtual void _PinJointSetParam(Rid joint, PhysicsServer3D.PinJointParam param, float value)
```

引擎名：`_pin_joint_set_param`

### _SeparationRayShapeCreate

```csharp
public virtual Rid _SeparationRayShapeCreate()
```

引擎名：`_separation_ray_shape_create`

### _SetActive

```csharp
public virtual void _SetActive(bool active)
```

引擎名：`_set_active`

### _ShapeGetCustomSolverBias

```csharp
public virtual float _ShapeGetCustomSolverBias(Rid shape)
```

引擎名：`_shape_get_custom_solver_bias`

### _ShapeGetData

```csharp
public virtual Variant _ShapeGetData(Rid shape)
```

引擎名：`_shape_get_data`

### _ShapeGetMargin

```csharp
public virtual float _ShapeGetMargin(Rid shape)
```

引擎名：`_shape_get_margin`

### _ShapeGetType

```csharp
public virtual PhysicsServer3D.ShapeType _ShapeGetType(Rid shape)
```

引擎名：`_shape_get_type`

### _ShapeSetCustomSolverBias

```csharp
public virtual void _ShapeSetCustomSolverBias(Rid shape, float bias)
```

引擎名：`_shape_set_custom_solver_bias`

### _ShapeSetData

```csharp
public virtual void _ShapeSetData(Rid shape, Variant data)
```

引擎名：`_shape_set_data`

### _ShapeSetMargin

```csharp
public virtual void _ShapeSetMargin(Rid shape, float margin)
```

引擎名：`_shape_set_margin`

### _SliderJointGetParam

```csharp
public virtual float _SliderJointGetParam(Rid joint, PhysicsServer3D.SliderJointParam param)
```

引擎名：`_slider_joint_get_param`

### _SliderJointSetParam

```csharp
public virtual void _SliderJointSetParam(Rid joint, PhysicsServer3D.SliderJointParam param, float value)
```

引擎名：`_slider_joint_set_param`

### _SoftBodyAddCollisionException

```csharp
public virtual void _SoftBodyAddCollisionException(Rid body, Rid bodyB)
```

引擎名：`_soft_body_add_collision_exception`

### _SoftBodyApplyCentralForce

```csharp
public virtual void _SoftBodyApplyCentralForce(Rid body, Vector3 force)
```

引擎名：`_soft_body_apply_central_force`

### _SoftBodyApplyCentralImpulse

```csharp
public virtual void _SoftBodyApplyCentralImpulse(Rid body, Vector3 impulse)
```

引擎名：`_soft_body_apply_central_impulse`

### _SoftBodyApplyPointForce

```csharp
public virtual void _SoftBodyApplyPointForce(Rid body, int pointIndex, Vector3 force)
```

引擎名：`_soft_body_apply_point_force`

### _SoftBodyApplyPointImpulse

```csharp
public virtual void _SoftBodyApplyPointImpulse(Rid body, int pointIndex, Vector3 impulse)
```

引擎名：`_soft_body_apply_point_impulse`

### _SoftBodyCreate

```csharp
public virtual Rid _SoftBodyCreate()
```

引擎名：`_soft_body_create`

### _SoftBodyGetBounds

```csharp
public virtual Aabb _SoftBodyGetBounds(Rid body)
```

引擎名：`_soft_body_get_bounds`

### _SoftBodyGetCollisionExceptions

```csharp
public virtual Array<Rid> _SoftBodyGetCollisionExceptions(Rid body)
```

引擎名：`_soft_body_get_collision_exceptions`

### _SoftBodyGetCollisionLayer

```csharp
public virtual uint _SoftBodyGetCollisionLayer(Rid body)
```

引擎名：`_soft_body_get_collision_layer`

### _SoftBodyGetCollisionMask

```csharp
public virtual uint _SoftBodyGetCollisionMask(Rid body)
```

引擎名：`_soft_body_get_collision_mask`

### _SoftBodyGetDampingCoefficient

```csharp
public virtual float _SoftBodyGetDampingCoefficient(Rid body)
```

引擎名：`_soft_body_get_damping_coefficient`

### _SoftBodyGetDragCoefficient

```csharp
public virtual float _SoftBodyGetDragCoefficient(Rid body)
```

引擎名：`_soft_body_get_drag_coefficient`

### _SoftBodyGetLinearStiffness

```csharp
public virtual float _SoftBodyGetLinearStiffness(Rid body)
```

引擎名：`_soft_body_get_linear_stiffness`

### _SoftBodyGetPointGlobalPosition

```csharp
public virtual Vector3 _SoftBodyGetPointGlobalPosition(Rid body, int pointIndex)
```

引擎名：`_soft_body_get_point_global_position`

### _SoftBodyGetPressureCoefficient

```csharp
public virtual float _SoftBodyGetPressureCoefficient(Rid body)
```

引擎名：`_soft_body_get_pressure_coefficient`

### _SoftBodyGetShrinkingFactor

```csharp
public virtual float _SoftBodyGetShrinkingFactor(Rid body)
```

引擎名：`_soft_body_get_shrinking_factor`

### _SoftBodyGetSimulationPrecision

```csharp
public virtual int _SoftBodyGetSimulationPrecision(Rid body)
```

引擎名：`_soft_body_get_simulation_precision`

### _SoftBodyGetSpace

```csharp
public virtual Rid _SoftBodyGetSpace(Rid body)
```

引擎名：`_soft_body_get_space`

### _SoftBodyGetState

```csharp
public virtual Variant _SoftBodyGetState(Rid body, PhysicsServer3D.BodyState state)
```

引擎名：`_soft_body_get_state`

### _SoftBodyGetTotalMass

```csharp
public virtual float _SoftBodyGetTotalMass(Rid body)
```

引擎名：`_soft_body_get_total_mass`

### _SoftBodyIsPointPinned

```csharp
public virtual bool _SoftBodyIsPointPinned(Rid body, int pointIndex)
```

引擎名：`_soft_body_is_point_pinned`

### _SoftBodyMovePoint

```csharp
public virtual void _SoftBodyMovePoint(Rid body, int pointIndex, Vector3 globalPosition)
```

引擎名：`_soft_body_move_point`

### _SoftBodyPinPoint

```csharp
public virtual void _SoftBodyPinPoint(Rid body, int pointIndex, bool pin)
```

引擎名：`_soft_body_pin_point`

### _SoftBodyRemoveAllPinnedPoints

```csharp
public virtual void _SoftBodyRemoveAllPinnedPoints(Rid body)
```

引擎名：`_soft_body_remove_all_pinned_points`

### _SoftBodyRemoveCollisionException

```csharp
public virtual void _SoftBodyRemoveCollisionException(Rid body, Rid bodyB)
```

引擎名：`_soft_body_remove_collision_exception`

### _SoftBodySetCollisionLayer

```csharp
public virtual void _SoftBodySetCollisionLayer(Rid body, uint layer)
```

引擎名：`_soft_body_set_collision_layer`

### _SoftBodySetCollisionMask

```csharp
public virtual void _SoftBodySetCollisionMask(Rid body, uint mask)
```

引擎名：`_soft_body_set_collision_mask`

### _SoftBodySetDampingCoefficient

```csharp
public virtual void _SoftBodySetDampingCoefficient(Rid body, float dampingCoefficient)
```

引擎名：`_soft_body_set_damping_coefficient`

### _SoftBodySetDragCoefficient

```csharp
public virtual void _SoftBodySetDragCoefficient(Rid body, float dragCoefficient)
```

引擎名：`_soft_body_set_drag_coefficient`

### _SoftBodySetLinearStiffness

```csharp
public virtual void _SoftBodySetLinearStiffness(Rid body, float linearStiffness)
```

引擎名：`_soft_body_set_linear_stiffness`

### _SoftBodySetMesh

```csharp
public virtual void _SoftBodySetMesh(Rid body, Rid mesh)
```

引擎名：`_soft_body_set_mesh`

### _SoftBodySetPressureCoefficient

```csharp
public virtual void _SoftBodySetPressureCoefficient(Rid body, float pressureCoefficient)
```

引擎名：`_soft_body_set_pressure_coefficient`

### _SoftBodySetRayPickable

```csharp
public virtual void _SoftBodySetRayPickable(Rid body, bool enable)
```

引擎名：`_soft_body_set_ray_pickable`

### _SoftBodySetShrinkingFactor

```csharp
public virtual void _SoftBodySetShrinkingFactor(Rid body, float shrinkingFactor)
```

引擎名：`_soft_body_set_shrinking_factor`

### _SoftBodySetSimulationPrecision

```csharp
public virtual void _SoftBodySetSimulationPrecision(Rid body, int simulationPrecision)
```

引擎名：`_soft_body_set_simulation_precision`

### _SoftBodySetSpace

```csharp
public virtual void _SoftBodySetSpace(Rid body, Rid space)
```

引擎名：`_soft_body_set_space`

### _SoftBodySetState

```csharp
public virtual void _SoftBodySetState(Rid body, PhysicsServer3D.BodyState state, Variant variant)
```

引擎名：`_soft_body_set_state`

### _SoftBodySetTotalMass

```csharp
public virtual void _SoftBodySetTotalMass(Rid body, float totalMass)
```

引擎名：`_soft_body_set_total_mass`

### _SoftBodySetTransform

```csharp
public virtual void _SoftBodySetTransform(Rid body, Transform3D transform)
```

引擎名：`_soft_body_set_transform`

### _SoftBodyUpdateRenderingServer

```csharp
public virtual void _SoftBodyUpdateRenderingServer(Rid body, PhysicsServer3DRenderingServerHandler renderingServerHandler)
```

引擎名：`_soft_body_update_rendering_server`

### _SpaceCreate

```csharp
public virtual Rid _SpaceCreate()
```

引擎名：`_space_create`

### _SpaceGetContactCount

```csharp
public virtual int _SpaceGetContactCount(Rid space)
```

引擎名：`_space_get_contact_count`

### _SpaceGetContacts

```csharp
public virtual Vector3[] _SpaceGetContacts(Rid space)
```

引擎名：`_space_get_contacts`

### _SpaceGetDirectState

```csharp
public virtual PhysicsDirectSpaceState3D _SpaceGetDirectState(Rid space)
```

引擎名：`_space_get_direct_state`

### _SpaceGetParam

```csharp
public virtual float _SpaceGetParam(Rid space, PhysicsServer3D.SpaceParameter param)
```

引擎名：`_space_get_param`

### _SpaceIsActive

```csharp
public virtual bool _SpaceIsActive(Rid space)
```

引擎名：`_space_is_active`

### _SpaceSetActive

```csharp
public virtual void _SpaceSetActive(Rid space, bool active)
```

引擎名：`_space_set_active`

### _SpaceSetDebugContacts

```csharp
public virtual void _SpaceSetDebugContacts(Rid space, int maxContacts)
```

引擎名：`_space_set_debug_contacts`

### _SpaceSetParam

```csharp
public virtual void _SpaceSetParam(Rid space, PhysicsServer3D.SpaceParameter param, float value)
```

引擎名：`_space_set_param`

### _SphereShapeCreate

```csharp
public virtual Rid _SphereShapeCreate()
```

引擎名：`_sphere_shape_create`

### _Step

```csharp
public virtual void _Step(float step)
```

引擎名：`_step`

### _Sync

```csharp
public virtual void _Sync()
```

引擎名：`_sync`

### _WorldBoundaryShapeCreate

```csharp
public virtual Rid _WorldBoundaryShapeCreate()
```

引擎名：`_world_boundary_shape_create`
