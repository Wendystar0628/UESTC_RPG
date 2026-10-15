<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRFutureExtension

命名空间 `Godot`；class；继承 [`OpenXRExtensionWrapper`](OpenXRExtensionWrapper.md) → [`GodotObject`](GodotObject.md)

This is a support extension in OpenXR that allows other OpenXR extensions to start asynchronous functions and get a callback after this function finishes. It is not intended for consumption within GDScript but can be accessed from GDExtension.

## 构造函数

### OpenXRFutureExtension

```csharp
public OpenXRFutureExtension()
```

## 方法

### CancelFuture

```csharp
public void CancelFuture(ulong future)
```

引擎名：`cancel_future`

Cancels an in-progress future. `future` must be an `XrFutureEXT` value previously returned by an API that started an asynchronous function.

### IsActive

```csharp
public bool IsActive()
```

引擎名：`is_active`

Returns `true` if futures are available in the OpenXR runtime used. This function will only return a usable result after OpenXR has been initialized.

### RegisterFuture

```csharp
public OpenXRFutureResult RegisterFuture(ulong future, Callable onSuccess = default)
```

引擎名：`register_future`

Register an OpenXR Future object so we monitor for completion. `future` must be an `XrFutureEXT` value previously returned by an API that started an asynchronous function.

You can optionally specify `onSuccess`, it will be invoked on successful completion of the future.

Or you can use the returned [`OpenXRFutureResult`](OpenXRFutureResult.md) object to `await` its [`OpenXRFutureResult.Completed`](OpenXRFutureResult.md) signal.

```csharp
var future_result = OpenXRFutureExtension.register_future(future)
await future_result.completed
if future_result.get_status() == OpenXRFutureResult.RESULT_FINISHED:
# Handle your success
pass

```
