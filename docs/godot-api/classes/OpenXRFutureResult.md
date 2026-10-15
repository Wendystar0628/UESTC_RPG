<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRFutureResult

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Result object tracking the asynchronous result of an OpenXR Future object, you can use this object to track the result status.

## 方法

### CancelFuture

```csharp
public void CancelFuture()
```

引擎名：`cancel_future`

Cancel this future, this will interrupt and stop the asynchronous function.

### GetFuture

```csharp
public ulong GetFuture()
```

引擎名：`get_future`

Return the `XrFutureEXT` value this result relates to.

### GetResultValue

```csharp
public Variant GetResultValue()
```

引擎名：`get_result_value`

Returns the result value of our asynchronous function (if set by the extension). The type of this result value depends on the function being called. Consult the documentation of the relevant function.

### GetStatus

```csharp
public OpenXRFutureResult.ResultStatus GetStatus()
```

引擎名：`get_status`

Returns the status of this result.

### SetResultValue

```csharp
public void SetResultValue(Variant resultValue)
```

引擎名：`set_result_value`

Stores the result value we expose to the user.

**Note:** This method should only be called by an OpenXR extension that implements an asynchronous function.

## 事件（信号）

### Completed

```csharp
public event OpenXRFutureResult.CompletedEventHandler Completed
```

引擎名：`completed`

Emitted when the asynchronous function is finished or has been cancelled.

## CompletedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`OpenXRFutureResult.Completed`](OpenXRFutureResult.md) event of a [`OpenXRFutureResult`](OpenXRFutureResult.md) class.

### 签名

#### Invoke

```csharp
delegate void CompletedEventHandler(OpenXRFutureResult result)
```

## ResultStatus

命名空间 `Godot`；enum

### 枚举值

- `Running = 0`：The asynchronous function is running.
- `Finished = 1`：The asynchronous function has finished.
- `Cancelled = 2`：The asynchronous function has been cancelled.
