<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GltfPhysicsBody

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Represents a physics body as an intermediary between the `OMI_physics_body` glTF data and Godot's nodes, and it's abstracted in a way that allows adding support for different glTF physics extensions in the future.

## 构造函数

### GltfPhysicsBody

```csharp
public GltfPhysicsBody()
```

## 属性

### AngularVelocity

```csharp
public Vector3 AngularVelocity { get; set; }
```

The angular velocity of the physics body, in radians per second. This is only used when the body type is "rigid" or "vehicle".

### BodyType

```csharp
public string BodyType { get; set; }
```

The type of the body.

When importing, this controls what type of [`CollisionObject3D`](CollisionObject3D.md) node Godot should generate. Valid values are `"static"`, `"animatable"`, `"character"`, `"rigid"`, `"vehicle"`, and `"trigger"`.

When exporting, this will be squashed down to one of `"static"`, `"kinematic"`, or `"dynamic"` motion types, or the `"trigger"` property.

### CenterOfMass

```csharp
public Vector3 CenterOfMass { get; set; }
```

The center of mass of the body, in meters. This is in local space relative to the body. By default, the center of the mass is the body's origin.

### InertiaDiagonal

```csharp
public Vector3 InertiaDiagonal { get; set; }
```

The inertia strength of the physics body, in kilogram meter squared (kg⋅m²). This represents the inertia around the principle axes, the diagonal of the inertia tensor matrix. This is only used when the body type is "rigid" or "vehicle".

When converted to a Godot [`RigidBody3D`](RigidBody3D.md) node, if this value is zero, then the inertia will be calculated automatically.

### InertiaOrientation

```csharp
public Quaternion InertiaOrientation { get; set; }
```

The inertia orientation of the physics body. This defines the rotation of the inertia's principle axes relative to the object's local axes. This is only used when the body type is "rigid" or "vehicle" and `InertiaDiagonal` is set to a non-zero value.

### InertiaTensor

```csharp
public Basis InertiaTensor { get; set; }
```

The inertia tensor of the physics body, in kilogram meter squared (kg⋅m²). This is only used when the body type is "rigid" or "vehicle".

When converted to a Godot [`RigidBody3D`](RigidBody3D.md) node, if this value is zero, then the inertia will be calculated automatically.

### LinearVelocity

```csharp
public Vector3 LinearVelocity { get; set; }
```

The linear velocity of the physics body, in meters per second. This is only used when the body type is "rigid" or "vehicle".

### Mass

```csharp
public float Mass { get; set; }
```

The mass of the physics body, in kilograms. This is only used when the body type is "rigid" or "vehicle".

## 方法

### FromDictionary

```csharp
public static GltfPhysicsBody FromDictionary(Dictionary dictionary)
```

Creates a new GLTFPhysicsBody instance by parsing the given [`Collections.Dictionary`](Collections.Dictionary.md) in the `OMI_physics_body` glTF extension format.

### FromNode

```csharp
public static GltfPhysicsBody FromNode(CollisionObject3D bodyNode)
```

Creates a new GLTFPhysicsBody instance from the given Godot [`CollisionObject3D`](CollisionObject3D.md) node.

### GetAngularVelocity

```csharp
public Vector3 GetAngularVelocity()
```

### GetBodyType

```csharp
public string GetBodyType()
```

### GetCenterOfMass

```csharp
public Vector3 GetCenterOfMass()
```

### GetInertiaDiagonal

```csharp
public Vector3 GetInertiaDiagonal()
```

### GetInertiaOrientation

```csharp
public Quaternion GetInertiaOrientation()
```

### GetInertiaTensor

```csharp
public Basis GetInertiaTensor()
```

### GetLinearVelocity

```csharp
public Vector3 GetLinearVelocity()
```

### GetMass

```csharp
public float GetMass()
```

### SetAngularVelocity

```csharp
public void SetAngularVelocity(Vector3 angularVelocity)
```

### SetBodyType

```csharp
public void SetBodyType(string bodyType)
```

### SetCenterOfMass

```csharp
public void SetCenterOfMass(Vector3 centerOfMass)
```

### SetInertiaDiagonal

```csharp
public void SetInertiaDiagonal(Vector3 inertiaDiagonal)
```

### SetInertiaOrientation

```csharp
public void SetInertiaOrientation(Quaternion inertiaOrientation)
```

### SetInertiaTensor

```csharp
public void SetInertiaTensor(Basis inertiaTensor)
```

### SetLinearVelocity

```csharp
public void SetLinearVelocity(Vector3 linearVelocity)
```

### SetMass

```csharp
public void SetMass(float mass)
```

### ToDictionary

```csharp
public Dictionary ToDictionary()
```

Serializes this GLTFPhysicsBody instance into a [`Collections.Dictionary`](Collections.Dictionary.md). It will be in the format expected by the `OMI_physics_body` glTF extension.

### ToNode

```csharp
public CollisionObject3D ToNode()
```

Converts this GLTFPhysicsBody instance into a Godot [`CollisionObject3D`](CollisionObject3D.md) node.
