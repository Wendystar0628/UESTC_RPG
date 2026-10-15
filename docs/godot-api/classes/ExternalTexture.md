<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ExternalTexture

命名空间 `Godot`；class；继承 [`Texture2D`](Texture2D.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Displays the content of an external buffer provided by the platform.

Requires the [OES_EGL_image_external](https://registry.khronos.org/OpenGL/extensions/OES/OES_EGL_image_external.txt) extension (OpenGL) or [VK_ANDROID_external_memory_android_hardware_buffer](https://registry.khronos.org/vulkan/specs/1.1-extensions/html/vkspec.html#VK_ANDROID_external_memory_android_hardware_buffer) extension (Vulkan).

**Note:** This is currently only supported in Android builds.

## 构造函数

### ExternalTexture

```csharp
public ExternalTexture()
```

## 属性

### Size

```csharp
public Vector2 Size { get; set; }
```

引擎名：`size`

External texture size.

## 方法

### GetExternalTextureId

```csharp
public ulong GetExternalTextureId()
```

引擎名：`get_external_texture_id`

Returns the external texture ID.

Depending on your use case, you may need to pass this to platform APIs, for example, when creating an `android.graphics.SurfaceTexture` on Android.

### SetExternalBufferId

```csharp
public void SetExternalBufferId(ulong externalBufferId)
```

引擎名：`set_external_buffer_id`

Sets the external buffer ID.

Depending on your use case, you may need to call this with data received from a platform API, for example, `SurfaceTexture.getHardwareBuffer()` on Android.

### SetSize

```csharp
public void SetSize(Vector2 size)
```

引擎名：`set_size`
