<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# JavaScriptBridgeInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

The JavaScriptBridge singleton is implemented only in the Web export. It's used to access the browser's JavaScript context. This allows interaction with embedding pages or calling third-party JavaScript APIs.

**Note:** This singleton can be disabled at build-time to improve security. By default, the JavaScriptBridge singleton is enabled. Official export templates also have the JavaScriptBridge singleton enabled. See [Compiling for the Web]($DOCS_URL/engine_details/development/compiling/compiling_for_web.html) in the documentation for more information.

## 方法

### CreateCallback

```csharp
public JavaScriptObject CreateCallback(Callable callable)
```

Creates a reference to a [`Callable`](Callable.md) that can be used as a callback by JavaScript. The reference must be kept until the callback happens, or it won't be called at all. See [`JavaScriptObject`](JavaScriptObject.md) for usage.

**Note:** The callback function must take exactly one [`Collections.Array`](Collections.Array.md) argument, which is going to be the JavaScript [arguments object](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Functions/arguments) converted to an array.

### CreateObject

```csharp
public Variant CreateObject(string object, ReadOnlySpan<Variant> args)
```

Creates a new JavaScript object using the `new` constructor. The `object` must a valid property of the JavaScript `window`. See [`JavaScriptObject`](JavaScriptObject.md) for usage.

### CreateObject

```csharp
public Variant CreateObject(string object, params Variant[] args)
```

Creates a new JavaScript object using the `new` constructor. The `object` must a valid property of the JavaScript `window`. See [`JavaScriptObject`](JavaScriptObject.md) for usage.

### DownloadBuffer

```csharp
public void DownloadBuffer(ReadOnlySpan<byte> buffer, string name, string mime)
```

Prompts the user to download a file containing the specified `buffer`. The file will have the given `name` and `mime` type.

**Note:** The browser may override the [MIME type](https://en.wikipedia.org/wiki/Media_type) provided based on the file `name`'s extension.

**Note:** Browsers might block the download if `DownloadBuffer()` is not being called from a user interaction (e.g. button click).

**Note:** Browsers might ask the user for permission or block the download if multiple download requests are made in a quick succession.

### DownloadBuffer

```csharp
public void DownloadBuffer(byte[] buffer, string name, string mime = "application/octet-stream")
```

Prompts the user to download a file containing the specified `buffer`. The file will have the given `name` and `mime` type.

**Note:** The browser may override the [MIME type](https://en.wikipedia.org/wiki/Media_type) provided based on the file `name`'s extension.

**Note:** Browsers might block the download if `DownloadBuffer()` is not being called from a user interaction (e.g. button click).

**Note:** Browsers might ask the user for permission or block the download if multiple download requests are made in a quick succession.

### Eval

```csharp
public Variant Eval(string code, bool useGlobalExecutionContext = false)
```

Execute the string `code` as JavaScript code within the browser window. This is a call to the actual global JavaScript function `eval()`.

If `useGlobalExecutionContext` is `true`, the code will be evaluated in the global execution context. Otherwise, it is evaluated in the execution context of a function within the engine's runtime environment.

### ForceFsSync

```csharp
public void ForceFsSync()
```

Force synchronization of the persistent file system (when enabled).

**Note:** This is only useful for modules or extensions that can't use [`FileAccess`](FileAccess.md) to write files.

### GetInterface

```csharp
public JavaScriptObject GetInterface(string interface)
```

Returns an interface to a JavaScript object that can be used by scripts. The `interface` must be a valid property of the JavaScript `window`. The callback must accept a single [`Collections.Array`](Collections.Array.md) argument, which will contain the JavaScript `arguments`. See [`JavaScriptObject`](JavaScriptObject.md) for usage.

### IsJsBuffer

```csharp
public bool IsJsBuffer(JavaScriptObject javascriptObject)
```

Returns `true` if the given `javascriptObject` is of type [ArrayBuffer](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/ArrayBuffer), [DataView](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/DataView), or one of the many [typed array objects](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/TypedArray).

### JsBufferToPackedByteArray

```csharp
public byte[] JsBufferToPackedByteArray(JavaScriptObject javascriptBuffer)
```

Returns a copy of `javascriptBuffer`'s contents as a `System.Byte`[]. See also `IsJsBuffer()`.

### PwaNeedsUpdate

```csharp
public bool PwaNeedsUpdate()
```

Returns `true` if a new version of the progressive web app is waiting to be activated.

**Note:** Only relevant when exported as a Progressive Web App.

### PwaUpdate

```csharp
public Error PwaUpdate()
```

Performs the live update of the progressive web app. Forcing the new version to be installed and the page to be reloaded.

**Note:** Your application will be **reloaded in all browser tabs**.

**Note:** Only relevant when exported as a Progressive Web App and `PwaNeedsUpdate()` returns `true`.

## 事件（信号）

### PwaUpdateAvailable

```csharp
public event Action PwaUpdateAvailable
```

Emitted when an update for this progressive web app has been detected but is waiting to be activated because a previous version is active. See `PwaUpdate()` to force the update to take place immediately.
