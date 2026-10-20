<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# JsonRpc

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

[JSON-RPC](https://www.jsonrpc.org/) is a standard which wraps a method call in a [`Json`](Json.md) object. The object has a particular structure and identifies which method is called, the parameters to that function, and carries an ID to keep track of responses. This class implements that standard on top of [`Collections.Dictionary`](Collections.Dictionary.md); you will have to convert between a [`Collections.Dictionary`](Collections.Dictionary.md) and [`Json`](Json.md) with other functions.

## 构造函数

### JsonRpc

```csharp
public JsonRpc()
```

## 方法

### MakeNotification

```csharp
public Dictionary MakeNotification(string method, Variant params)
```

Returns a dictionary in the form of a JSON-RPC notification. Notifications are one-shot messages which do not expect a response.

- `method`: Name of the method being called.

- `params`: An array or dictionary of parameters being passed to the method.

### MakeRequest

```csharp
public Dictionary MakeRequest(string method, Variant params, Variant id)
```

Returns a dictionary in the form of a JSON-RPC request. Requests are sent to a server with the expectation of a response. The ID field is used for the server to specify which exact request it is responding to.

- `method`: Name of the method being called.

- `params`: An array or dictionary of parameters being passed to the method.

- `id`: Uniquely identifies this request. The server is expected to send a response with the same ID.

### MakeResponse

```csharp
public Dictionary MakeResponse(Variant result, Variant id)
```

When a server has received and processed a request, it is expected to send a response. If you did not want a response then you need to have sent a Notification instead.

- `result`: The return value of the function which was called.

- `id`: The ID of the request this response is targeted to.

### MakeResponseError

```csharp
public Dictionary MakeResponseError(int code, string message, Variant id = default)
```

Creates a response which indicates a previous reply has failed in some way.

- `code`: The error code corresponding to what kind of error this is. See the [`JsonRpc.ErrorCode`](JsonRpc.md) constants.

- `message`: A custom message about this error.

- `id`: The request this error is a response to.

### ProcessAction

```csharp
public Variant ProcessAction(Variant action, bool recurse = false)
```

Given a Dictionary which takes the form of a JSON-RPC request: unpack the request and run it. Methods are resolved by looking at the field called "method" and looking for an equivalently named function in the JSONRPC object. If one is found that method is called.

To add new supported methods extend the JSONRPC class and call `ProcessAction()` on your subclass.

`action`: The action to be run, as a Dictionary in the form of a JSON-RPC request or notification.

### ProcessString

```csharp
public string ProcessString(string action)
```

### SetMethod

```csharp
public void SetMethod(string name, Callable callback)
```

Registers a callback for the given method name.

- `name`: The name that clients can use to access the callback.

- `callback`: The callback which will handle the specified method.

### SetScope

```csharp
public void SetScope(string scope, GodotObject target)
```

## ErrorCode

命名空间 `Godot`；enum

### 枚举值

- `ParseError = -32700`：The request could not be parsed as it was not valid by JSON standard ([`Json.Parse`](Json.md) failed).
- `InvalidRequest = -32600`：A method call was requested but the request's format is not valid.
- `MethodNotFound = -32601`：A method call was requested but no function of that name existed in the JSONRPC subclass.
- `InvalidParams = -32602`：A method call was requested but the given method parameters are not valid.
- `InternalError = -32603`：An internal error occurred while processing the request.
