<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRAndroidThreadSettingsExtension

命名空间 `Godot`；class；继承 [`OpenXRExtensionWrapper`](OpenXRExtensionWrapper.md) → [`GodotObject`](GodotObject.md)

For XR to be comfortable, it is important for applications to deliver frames quickly and consistently. In order to make sure the important application threads get their full share of time, these threads must be identified to the system, which will adjust their scheduling priority accordingly.

## 构造函数

### OpenXRAndroidThreadSettingsExtension

```csharp
public OpenXRAndroidThreadSettingsExtension()
```

## 方法

### SetApplicationThreadType

```csharp
public bool SetApplicationThreadType(OpenXRAndroidThreadSettingsExtension.ThreadType threadType, uint threadId = 0)
```

引擎名：`set_application_thread_type`

Sets the thread type of the given thread, so that the XR runtime can adjust its scheduling priority accordingly.

`threadId` refers to the OS thread id (ie from `gettid()`). When `threadId` is `0`, it will set the thread type of the current thread.

**NOTE:** The id returned by [`GodotThread.GetId`](GodotThread.md) is incompatible with `threadId`.

## ThreadType

命名空间 `Godot`；enum

### 枚举值

- `ApplicationMain = 0`：Hints to the XR runtime that the thread is doing time critical CPU tasks.
- `ApplicationWorker = 1`：Hints to the XR runtime that the thread is doing background CPU tasks.
- `RendererMain = 2`：Hints to the XR runtime that the thread is doing time critical graphics device tasks.
- `RendererWorker = 3`：Hints to the XR runtime that the thread is doing background graphics device tasks.
