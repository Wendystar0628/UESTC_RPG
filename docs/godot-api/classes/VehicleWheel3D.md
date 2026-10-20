<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VehicleWheel3D

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A node used as a child of a [`VehicleBody3D`](VehicleBody3D.md) parent to simulate the behavior of one of its wheels. This node also acts as a collider to detect if the wheel is touching a surface.

**Note:** This class has known issues and isn't designed to provide realistic 3D vehicle physics. If you want advanced vehicle physics, you may need to write your own physics integration using another [`PhysicsBody3D`](PhysicsBody3D.md) class.

## 构造函数

### VehicleWheel3D

```csharp
public VehicleWheel3D()
```

## 属性

### Brake

```csharp
public float Brake { get; set; }
```

引擎名：`brake`

Slows down the wheel by applying a braking force. The wheel is only slowed down if it is in contact with a surface. The force you need to apply to adequately slow down your vehicle depends on the [`RigidBody3D.Mass`](RigidBody3D.md) of the vehicle. For a vehicle with a mass set to 1000, try a value in the 25 - 30 range for hard braking.

### DampingCompression

```csharp
public float DampingCompression { get; set; }
```

引擎名：`damping_compression`

The damping applied to the suspension spring when being compressed, meaning when the wheel is moving up relative to the vehicle. It is measured in Newton-seconds per millimeter (N⋅s/mm), or megagrams per second (Mg/s). This value should be between 0.0 (no damping) and 1.0, but may be more. A value of 0.0 means the car will keep bouncing as the spring keeps its energy. A good value for this is around 0.3 for a normal car, 0.5 for a race car.

### DampingRelaxation

```csharp
public float DampingRelaxation { get; set; }
```

引擎名：`damping_relaxation`

The damping applied to the suspension spring when rebounding or extending, meaning when the wheel is moving down relative to the vehicle. It is measured in Newton-seconds per millimeter (N⋅s/mm), or megagrams per second (Mg/s). This value should be between 0.0 (no damping) and 1.0, but may be more. This value should always be slightly higher than the `DampingCompression` property. For a `DampingCompression` value of 0.3, try a relaxation value of 0.5.

### EngineForce

```csharp
public float EngineForce { get; set; }
```

引擎名：`engine_force`

Accelerates the wheel by applying an engine force. The wheel is only sped up if it is in contact with a surface. The [`RigidBody3D.Mass`](RigidBody3D.md) of the vehicle has an effect on the acceleration of the vehicle. For a vehicle with a mass set to 1000, try a value in the 25 - 50 range for acceleration.

**Note:** The simulation does not take the effect of gears into account, you will need to add logic for this if you wish to simulate gears.

A negative value will result in the wheel reversing.

### Steering

```csharp
public float Steering { get; set; }
```

引擎名：`steering`

The steering angle for the wheel, in radians. Setting this to a non-zero value will result in the vehicle turning when it's moving.

### SuspensionMaxForce

```csharp
public float SuspensionMaxForce { get; set; }
```

引擎名：`suspension_max_force`

The maximum force the spring can resist. This value should be higher than a quarter of the [`RigidBody3D.Mass`](RigidBody3D.md) of the [`VehicleBody3D`](VehicleBody3D.md) or the spring will not carry the weight of the vehicle. Good results are often obtained by a value that is about 3× to 4× this number.

### SuspensionStiffness

```csharp
public float SuspensionStiffness { get; set; }
```

引擎名：`suspension_stiffness`

The stiffness of the suspension, measured in Newtons per millimeter (N/mm), or megagrams per second squared (Mg/s²). Use a value lower than 50 for an off-road car, a value between 50 and 100 for a race car and try something around 200 for something like a Formula 1 car.

### SuspensionTravel

```csharp
public float SuspensionTravel { get; set; }
```

引擎名：`suspension_travel`

This is the distance the suspension can travel. As Godot units are equivalent to meters, keep this setting relatively low. Try a value between 0.1 and 0.3 depending on the type of car.

### UseAsSteering

```csharp
public bool UseAsSteering { get; set; }
```

引擎名：`use_as_steering`

If `true`, this wheel will be turned when the car steers. This value is used in conjunction with [`VehicleBody3D.Steering`](VehicleBody3D.md) and ignored if you are using the per-wheel `Steering` value instead.

### UseAsTraction

```csharp
public bool UseAsTraction { get; set; }
```

引擎名：`use_as_traction`

If `true`, this wheel transfers engine force to the ground to propel the vehicle forward. This value is used in conjunction with [`VehicleBody3D.EngineForce`](VehicleBody3D.md) and ignored if you are using the per-wheel `EngineForce` value instead.

### WheelFrictionSlip

```csharp
public float WheelFrictionSlip { get; set; }
```

引擎名：`wheel_friction_slip`

This determines how much grip this wheel has. It is combined with the friction setting of the surface the wheel is in contact with. 0.0 means no grip, 1.0 is normal grip. For a drift car setup, try setting the grip of the rear wheels slightly lower than the front wheels, or use a lower value to simulate tire wear.

It's best to set this to 1.0 when starting out.

### WheelRadius

```csharp
public float WheelRadius { get; set; }
```

引擎名：`wheel_radius`

The radius of the wheel in meters.

### WheelRestLength

```csharp
public float WheelRestLength { get; set; }
```

引擎名：`wheel_rest_length`

This is the distance in meters the wheel is lowered from its origin point. Don't set this to 0.0 and move the wheel into position, instead move the origin point of your wheel (the gizmo in Godot) to the position the wheel will take when bottoming out, then use the rest length to move the wheel down to the position it should be in when the car is in rest.

### WheelRollInfluence

```csharp
public float WheelRollInfluence { get; set; }
```

引擎名：`wheel_roll_influence`

This value affects the roll of your vehicle. If set to 1.0 for all wheels, your vehicle will resist body roll, while a value of 0.0 will be prone to rolling over.

## 方法

### GetBrake

```csharp
public float GetBrake()
```

引擎名：`get_brake`

### GetContactBody

```csharp
public Node3D GetContactBody()
```

引擎名：`get_contact_body`

Returns the contacting body node if valid in the tree, as [`Node3D`](Node3D.md). At the moment, [`GridMap`](GridMap.md) is not supported so the node will be always of type [`PhysicsBody3D`](PhysicsBody3D.md).

Returns `null` if the wheel is not in contact with a surface, or the contact body is not a [`PhysicsBody3D`](PhysicsBody3D.md).

### GetContactNormal

```csharp
public Vector3 GetContactNormal()
```

引擎名：`get_contact_normal`

Returns the normal of the suspension's collision in world space if the wheel is in contact. If the wheel isn't in contact with anything, returns a vector pointing directly along the suspension axis toward the vehicle in world space.

### GetContactPoint

```csharp
public Vector3 GetContactPoint()
```

引擎名：`get_contact_point`

Returns the point of the suspension's collision in world space if the wheel is in contact. If the wheel isn't in contact with anything, returns the maximum point of the wheel's ray cast in world space, which is defined by `wheel_rest_length + wheel_radius`.

### GetDampingCompression

```csharp
public float GetDampingCompression()
```

引擎名：`get_damping_compression`

### GetDampingRelaxation

```csharp
public float GetDampingRelaxation()
```

引擎名：`get_damping_relaxation`

### GetEngineForce

```csharp
public float GetEngineForce()
```

引擎名：`get_engine_force`

### GetFrictionSlip

```csharp
public float GetFrictionSlip()
```

引擎名：`get_friction_slip`

### GetRadius

```csharp
public float GetRadius()
```

引擎名：`get_radius`

### GetRollInfluence

```csharp
public float GetRollInfluence()
```

引擎名：`get_roll_influence`

### GetRpm

```csharp
public float GetRpm()
```

引擎名：`get_rpm`

Returns the rotational speed of the wheel in revolutions per minute.

### GetSkidinfo

```csharp
public float GetSkidinfo()
```

引擎名：`get_skidinfo`

Returns a value between 0.0 and 1.0 that indicates whether this wheel is skidding. 0.0 is skidding (the wheel has lost grip, e.g. icy terrain), 1.0 means not skidding (the wheel has full grip, e.g. dry asphalt road).

### GetSteering

```csharp
public float GetSteering()
```

引擎名：`get_steering`

### GetSuspensionMaxForce

```csharp
public float GetSuspensionMaxForce()
```

引擎名：`get_suspension_max_force`

### GetSuspensionRestLength

```csharp
public float GetSuspensionRestLength()
```

引擎名：`get_suspension_rest_length`

### GetSuspensionStiffness

```csharp
public float GetSuspensionStiffness()
```

引擎名：`get_suspension_stiffness`

### GetSuspensionTravel

```csharp
public float GetSuspensionTravel()
```

引擎名：`get_suspension_travel`

### IsInContact

```csharp
public bool IsInContact()
```

引擎名：`is_in_contact`

Returns `true` if this wheel is in contact with a surface.

### IsUsedAsSteering

```csharp
public bool IsUsedAsSteering()
```

引擎名：`is_used_as_steering`

### IsUsedAsTraction

```csharp
public bool IsUsedAsTraction()
```

引擎名：`is_used_as_traction`

### SetBrake

```csharp
public void SetBrake(float brake)
```

引擎名：`set_brake`

### SetDampingCompression

```csharp
public void SetDampingCompression(float length)
```

引擎名：`set_damping_compression`

### SetDampingRelaxation

```csharp
public void SetDampingRelaxation(float length)
```

引擎名：`set_damping_relaxation`

### SetEngineForce

```csharp
public void SetEngineForce(float engineForce)
```

引擎名：`set_engine_force`

### SetFrictionSlip

```csharp
public void SetFrictionSlip(float length)
```

引擎名：`set_friction_slip`

### SetRadius

```csharp
public void SetRadius(float length)
```

引擎名：`set_radius`

### SetRollInfluence

```csharp
public void SetRollInfluence(float rollInfluence)
```

引擎名：`set_roll_influence`

### SetSteering

```csharp
public void SetSteering(float steering)
```

引擎名：`set_steering`

### SetSuspensionMaxForce

```csharp
public void SetSuspensionMaxForce(float length)
```

引擎名：`set_suspension_max_force`

### SetSuspensionRestLength

```csharp
public void SetSuspensionRestLength(float length)
```

引擎名：`set_suspension_rest_length`

### SetSuspensionStiffness

```csharp
public void SetSuspensionStiffness(float length)
```

引擎名：`set_suspension_stiffness`

### SetSuspensionTravel

```csharp
public void SetSuspensionTravel(float length)
```

引擎名：`set_suspension_travel`

### SetUseAsSteering

```csharp
public void SetUseAsSteering(bool enable)
```

引擎名：`set_use_as_steering`

### SetUseAsTraction

```csharp
public void SetUseAsTraction(bool enable)
```

引擎名：`set_use_as_traction`
