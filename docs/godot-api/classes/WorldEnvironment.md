<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# WorldEnvironment

命名空间 `Godot`；class；继承 [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

The `WorldEnvironment` node is used to configure the default [`Environment`](Environment.md) for the scene.

The parameters defined in the `WorldEnvironment` can be overridden by an [`Environment`](Environment.md) node set on the current [`Camera3D`](Camera3D.md). Additionally, only one `WorldEnvironment` may be instantiated in a given scene at a time.

The `WorldEnvironment` allows the user to specify default lighting parameters (e.g. ambient lighting), various post-processing effects (e.g. SSAO, DOF, Tonemapping), and how to draw the background (e.g. solid color, skybox). Usually, these are added in order to improve the realism/color balance of the scene.

## 构造函数

### WorldEnvironment

```csharp
public WorldEnvironment()
```

## 属性

### CameraAttributes

```csharp
public CameraAttributes CameraAttributes { get; set; }
```

引擎名：`camera_attributes`

The default [`CameraAttributes`](CameraAttributes.md) resource to use if none set on the [`Camera3D`](Camera3D.md).

### Compositor

```csharp
public Compositor Compositor { get; set; }
```

引擎名：`compositor`

The default [`Compositor`](Compositor.md) resource to use if none set on the [`Camera3D`](Camera3D.md).

### Environment

```csharp
public Environment Environment { get; set; }
```

引擎名：`environment`

The [`Environment`](Environment.md) resource used by this `WorldEnvironment`, defining the default properties.

## 方法

### GetCameraAttributes

```csharp
public CameraAttributes GetCameraAttributes()
```

引擎名：`get_camera_attributes`

### GetCompositor

```csharp
public Compositor GetCompositor()
```

引擎名：`get_compositor`

### GetEnvironment

```csharp
public Environment GetEnvironment()
```

引擎名：`get_environment`

### SetCameraAttributes

```csharp
public void SetCameraAttributes(CameraAttributes cameraAttributes)
```

引擎名：`set_camera_attributes`

### SetCompositor

```csharp
public void SetCompositor(Compositor compositor)
```

引擎名：`set_compositor`

### SetEnvironment

```csharp
public void SetEnvironment(Environment env)
```

引擎名：`set_environment`
