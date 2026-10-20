<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VehicleBody3D

命名空间 `Godot`；class；继承 [`RigidBody3D`](RigidBody3D.md) → [`PhysicsBody3D`](PhysicsBody3D.md) → [`CollisionObject3D`](CollisionObject3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This physics body implements all the physics logic needed to simulate a car. It is based on the raycast vehicle system commonly found in physics engines. Aside from a [`CollisionShape3D`](CollisionShape3D.md) for the main body of the vehicle, you must also add a [`VehicleWheel3D`](VehicleWheel3D.md) node for each wheel. You should also add a [`MeshInstance3D`](MeshInstance3D.md) to this node for the 3D model of the vehicle, but this model should generally not include meshes for the wheels. You can control the vehicle by using the `Brake`, `EngineForce`, and `Steering` properties. The position or orientation of this node shouldn't be changed directly.

**Note:** The local forward for this node is `Vector3.ModelFront`.

**Note:** The origin point of your VehicleBody3D will determine the center of gravity of your vehicle. To make the vehicle more grounded, the origin point is usually kept low, moving the [`CollisionShape3D`](CollisionShape3D.md) and [`MeshInstance3D`](MeshInstance3D.md) upwards.

**Note:** This class has known issues and isn't designed to provide realistic 3D vehicle physics. If you want advanced vehicle physics, you may have to write your own physics integration using [`CharacterBody3D`](CharacterBody3D.md) or [`RigidBody3D`](RigidBody3D.md).

## 构造函数

### VehicleBody3D

```csharp
public VehicleBody3D()
```

## 属性

### Brake

```csharp
public float Brake { get; set; }
```

引擎名：`brake`

Slows down the vehicle by applying a braking force. The vehicle is only slowed down if the wheels are in contact with a surface. The force you need to apply to adequately slow down your vehicle depends on the [`RigidBody3D.Mass`](RigidBody3D.md) of the vehicle. For a vehicle with a mass set to 1000, try a value in the 25 - 30 range for hard braking.

### EngineForce

```csharp
public float EngineForce { get; set; }
```

引擎名：`engine_force`

Accelerates the vehicle by applying an engine force. The vehicle is only sped up if the wheels that have [`VehicleWheel3D.UseAsTraction`](VehicleWheel3D.md) set to `true` and are in contact with a surface. The [`RigidBody3D.Mass`](RigidBody3D.md) of the vehicle has an effect on the acceleration of the vehicle. For a vehicle with a mass set to 1000, try a value in the 25 - 50 range for acceleration.

**Note:** The simulation does not take the effect of gears into account, you will need to add logic for this if you wish to simulate gears.

A negative value will result in the vehicle reversing.

### Steering

```csharp
public float Steering { get; set; }
```

引擎名：`steering`

The steering angle for the vehicle. Setting this to a non-zero value will result in the vehicle turning when it's moving. Wheels that have [`VehicleWheel3D.UseAsSteering`](VehicleWheel3D.md) set to `true` will automatically be rotated.

**Note:** This property is edited in the inspector in degrees. In code the property is set in radians.

## 方法

### GetBrake

```csharp
public float GetBrake()
```

引擎名：`get_brake`

### GetEngineForce

```csharp
public float GetEngineForce()
```

引擎名：`get_engine_force`

### GetSteering

```csharp
public float GetSteering()
```

引擎名：`get_steering`

### SetBrake

```csharp
public void SetBrake(float brake)
```

引擎名：`set_brake`

### SetEngineForce

```csharp
public void SetEngineForce(float engineForce)
```

引擎名：`set_engine_force`

### SetSteering

```csharp
public void SetSteering(float steering)
```

引擎名：`set_steering`
