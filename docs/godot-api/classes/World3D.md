<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# World3D

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Class that has everything pertaining to a world: A physics space, a visual scenario, and a sound space. 3D nodes register their resources into the current 3D world.

## 构造函数

### World3D

```csharp
public World3D()
```

## 属性

### CameraAttributes

```csharp
public CameraAttributes CameraAttributes { get; set; }
```

引擎名：`camera_attributes`

The default [`CameraAttributes`](CameraAttributes.md) resource to use if none set on the [`Camera3D`](Camera3D.md).

### DirectSpaceState

```csharp
public PhysicsDirectSpaceState3D DirectSpaceState { get; }
```

引擎名：`direct_space_state`

Direct access to the world's physics 3D space state. Used for querying current and potential collisions. When using multi-threaded physics, access is limited to [`Node._PhysicsProcess()`](Node.md) in the main thread.

### Environment

```csharp
public Environment Environment { get; set; }
```

引擎名：`environment`

The World3D's [`Environment`](Environment.md).

### FallbackEnvironment

```csharp
public Environment FallbackEnvironment { get; set; }
```

引擎名：`fallback_environment`

The World3D's fallback environment will be used if `Environment` fails or is missing.

### NavigationMap

```csharp
public Rid NavigationMap { get; }
```

引擎名：`navigation_map`

The [`Rid`](Rid.md) of this world's navigation map. Used by the [`NavigationServer3D`](NavigationServer3D.md).

### Scenario

```csharp
public Rid Scenario { get; }
```

引擎名：`scenario`

The World3D's visual scenario.

### Space

```csharp
public Rid Space { get; }
```

引擎名：`space`

The World3D's physics space.

## 方法

### GetCameraAttributes

```csharp
public CameraAttributes GetCameraAttributes()
```

引擎名：`get_camera_attributes`

### GetDirectSpaceState

```csharp
public PhysicsDirectSpaceState3D GetDirectSpaceState()
```

引擎名：`get_direct_space_state`

### GetEnvironment

```csharp
public Environment GetEnvironment()
```

引擎名：`get_environment`

### GetFallbackEnvironment

```csharp
public Environment GetFallbackEnvironment()
```

引擎名：`get_fallback_environment`

### GetNavigationMap

```csharp
public Rid GetNavigationMap()
```

引擎名：`get_navigation_map`

### GetScenario

```csharp
public Rid GetScenario()
```

引擎名：`get_scenario`

### GetSpace

```csharp
public Rid GetSpace()
```

引擎名：`get_space`

### SetCameraAttributes

```csharp
public void SetCameraAttributes(CameraAttributes attributes)
```

引擎名：`set_camera_attributes`

### SetEnvironment

```csharp
public void SetEnvironment(Environment env)
```

引擎名：`set_environment`

### SetFallbackEnvironment

```csharp
public void SetFallbackEnvironment(Environment env)
```

引擎名：`set_fallback_environment`
