<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# HttpRequest

命名空间 `Godot`；class；继承 [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A node with the ability to send HTTP requests. Uses [`HttpClient`](HttpClient.md) internally.

Can be used to make HTTP requests, i.e. download or upload files or web content via HTTP.

**Warning:** See the notes and warnings on [`HttpClient`](HttpClient.md) for limitations, especially regarding TLS security.

**Note:** When exporting to Android, make sure to enable the `INTERNET` permission in the Android export preset before exporting the project or using one-click deploy. Otherwise, network communication of any kind will be blocked by Android.

**Example:** Contact a REST API and print one of its returned fields:

```csharp
public override void _Ready()
{
// Create an HTTP request node and connect its completion signal.
var httpRequest = new HttpRequest();
AddChild(httpRequest);
httpRequest.RequestCompleted += HttpRequestCompleted;

// Perform a GET request. The URL below returns JSON as of writing.
Error error = httpRequest.Request("https://httpbin.org/get");
if (error != Error.Ok)
{
GD.PushError("An error occurred in the HTTP request.");
}

// Perform a POST request. The URL below returns JSON as of writing.
// Note: Don't make simultaneous requests using a single HTTPRequest node.
// The snippet below is provided for reference only.
string body = Json.Stringify(new Godot.Collections.Dictionary
{
{ "name", "Godette" }
});
error = httpRequest.Request("https://httpbin.org/post", null, HttpClient.Method.Post, body);
if (error != Error.Ok)
{
GD.PushError("An error occurred in the HTTP request.");
}
}

// Called when the HTTP request is completed.
private void HttpRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
{
var json = new Json();
json.Parse(body.GetStringFromUtf8());
var response = json.GetData().AsGodotDictionary();

// Will print the user agent string used by the HTTPRequest node (as recognized by httpbin.org).
GD.Print((response["headers"].AsGodotDictionary())["User-Agent"]);
}

```

**Example:** Load an image using `HttpRequest` and display it:

```csharp
public override void _Ready()
{
// Create an HTTP request node and connect its completion signal.
var httpRequest = new HttpRequest();
AddChild(httpRequest);
httpRequest.RequestCompleted += HttpRequestCompleted;

// Perform the HTTP request. The URL below returns a PNG image as of writing.
Error error = httpRequest.Request("https://placehold.co/512.png");
if (error != Error.Ok)
{
GD.PushError("An error occurred in the HTTP request.");
}
}

// Called when the HTTP request is completed.
private void HttpRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
{
if (result != (long)HttpRequest.Result.Success)
{
GD.PushError("Image couldn't be downloaded. Try a different image.");
}
var image = new Image();
Error error = image.LoadPngFromBuffer(body);
if (error != Error.Ok)
{
GD.PushError("Couldn't load the image.");
}

var texture = ImageTexture.CreateFromImage(image);

// Display the image in a TextureRect node.
var textureRect = new TextureRect();
AddChild(textureRect);
textureRect.Texture = texture;
}

```

**Note:** `HttpRequest` nodes will automatically handle decompression of response bodies. An `Accept-Encoding` header will be automatically added to each of your requests, unless one is already specified. Any response with a `Content-Encoding: gzip` header will automatically be decompressed and delivered to you as uncompressed bytes.

## 构造函数

### HttpRequest

```csharp
public HttpRequest()
```

## 属性

### AcceptGZip

```csharp
public bool AcceptGZip { get; set; }
```

If `true`, this header will be added to each request: `Accept-Encoding: gzip, deflate` telling servers that it's okay to compress response bodies.

Any Response body declaring a `Content-Encoding` of either `gzip` or `deflate` will then be automatically decompressed, and the uncompressed bytes will be delivered via `RequestCompleted`.

If the user has specified their own `Accept-Encoding` header, then no header will be added regardless of `AcceptGZip`.

If `false` no header will be added, and no decompression will be performed on response bodies. The raw bytes of the response body will be returned via `RequestCompleted`.

### BodySizeLimit

```csharp
public int BodySizeLimit { get; set; }
```

Maximum allowed size for response bodies. If the response body is compressed, this will be used as the maximum allowed size for the decompressed body.

### DownloadChunkSize

```csharp
public int DownloadChunkSize { get; set; }
```

The size of the buffer used and maximum bytes to read per iteration. See [`HttpClient.ReadChunkSize`](HttpClient.md).

Set this to a lower value (e.g. 4096 for 4 KiB) when downloading small files to decrease memory usage at the cost of download speeds.

### DownloadFile

```csharp
public string DownloadFile { get; set; }
```

The file to download into. Will output any received file into it.

### MaxRedirects

```csharp
public int MaxRedirects { get; set; }
```

Maximum number of allowed redirects.

### Timeout

```csharp
public double Timeout { get; set; }
```

The duration to wait before a request times out, in seconds (independent of [`Engine.TimeScale`](Engine.md)). If `Timeout` is set to `0.0`, the request will never time out.

For simple requests, such as communication with a REST API, it is recommended to set `Timeout` to a value suitable for the server response time (commonly between `1.0` and `10.0`). This will help prevent unwanted timeouts caused by variation in response times while still allowing the application to detect when a request has timed out. For larger requests such as file downloads, it is recommended to set `Timeout` to `0.0`, disabling the timeout functionality. This will help prevent large transfers from failing due to exceeding the timeout value.

### UseThreads

```csharp
public bool UseThreads { get; set; }
```

If `true`, multithreading is used to improve performance.

## 方法

### CancelRequest

```csharp
public void CancelRequest()
```

Cancels the current request.

### GetBodySize

```csharp
public int GetBodySize()
```

Returns the response body length.

**Note:** Some Web servers may not send a body length. In this case, the value returned will be `-1`. If using chunked transfer encoding, the body length will also be `-1`.

### GetBodySizeLimit

```csharp
public int GetBodySizeLimit()
```

### GetDownloadChunkSize

```csharp
public int GetDownloadChunkSize()
```

### GetDownloadFile

```csharp
public string GetDownloadFile()
```

### GetDownloadedBytes

```csharp
public int GetDownloadedBytes()
```

Returns the number of bytes this HTTPRequest downloaded.

### GetHttpClientStatus

```csharp
public HttpClient.Status GetHttpClientStatus()
```

Returns the current status of the underlying [`HttpClient`](HttpClient.md).

### GetMaxRedirects

```csharp
public int GetMaxRedirects()
```

### GetTimeout

```csharp
public double GetTimeout()
```

### IsAcceptingGZip

```csharp
public bool IsAcceptingGZip()
```

### IsUsingThreads

```csharp
public bool IsUsingThreads()
```

### Request

```csharp
public Error Request(string url, ReadOnlySpan<string> customHeaders, HttpClient.Method method, string requestData)
```

Creates request on the underlying [`HttpClient`](HttpClient.md). If there is no configuration errors, it tries to connect using [`HttpClient.ConnectToHost`](HttpClient.md) and passes parameters onto [`HttpClient.Request`](HttpClient.md).

Returns [`Error.Ok`](Error.md) if request is successfully created. (Does not imply that the server has responded), [`Error.Unconfigured`](Error.md) if not in the tree, [`Error.Busy`](Error.md) if still processing previous request, [`Error.InvalidParameter`](Error.md) if given string is not a valid URL format, or [`Error.CantConnect`](Error.md) if not using thread and the [`HttpClient`](HttpClient.md) cannot connect to host.

**Note:** When `method` is [`HttpClient.Method.Get`](HttpClient.md), the payload sent via `requestData` might be ignored by the server or even cause the server to reject the request (check [RFC 7231 section 4.3.1](https://datatracker.ietf.org/doc/html/rfc7231#section-4.3.1) for more details). As a workaround, you can send data as a query string in the URL (see `String.uri_encode` for an example).

**Note:** It's recommended to use transport encryption (TLS) and to avoid sending sensitive information (such as login credentials) in HTTP GET URL parameters. Consider using HTTP POST requests or HTTP headers for such information instead.

### Request

```csharp
public Error Request(string url, string[] customHeaders = null, HttpClient.Method method = HttpClient.Method.Get, string requestData = "")
```

Creates request on the underlying [`HttpClient`](HttpClient.md). If there is no configuration errors, it tries to connect using [`HttpClient.ConnectToHost`](HttpClient.md) and passes parameters onto [`HttpClient.Request`](HttpClient.md).

Returns [`Error.Ok`](Error.md) if request is successfully created. (Does not imply that the server has responded), [`Error.Unconfigured`](Error.md) if not in the tree, [`Error.Busy`](Error.md) if still processing previous request, [`Error.InvalidParameter`](Error.md) if given string is not a valid URL format, or [`Error.CantConnect`](Error.md) if not using thread and the [`HttpClient`](HttpClient.md) cannot connect to host.

**Note:** When `method` is [`HttpClient.Method.Get`](HttpClient.md), the payload sent via `requestData` might be ignored by the server or even cause the server to reject the request (check [RFC 7231 section 4.3.1](https://datatracker.ietf.org/doc/html/rfc7231#section-4.3.1) for more details). As a workaround, you can send data as a query string in the URL (see `String.uri_encode` for an example).

**Note:** It's recommended to use transport encryption (TLS) and to avoid sending sensitive information (such as login credentials) in HTTP GET URL parameters. Consider using HTTP POST requests or HTTP headers for such information instead.

参数：
- `customHeaders`：If the parameter is null, then the default value is `Array.Empty<string>()`.

### RequestRaw

```csharp
public Error RequestRaw(string url, ReadOnlySpan<string> customHeaders, HttpClient.Method method, ReadOnlySpan<byte> requestDataRaw)
```

Creates request on the underlying [`HttpClient`](HttpClient.md) using a raw array of bytes for the request body. If there is no configuration errors, it tries to connect using [`HttpClient.ConnectToHost`](HttpClient.md) and passes parameters onto [`HttpClient.Request`](HttpClient.md).

Returns [`Error.Ok`](Error.md) if request is successfully created. (Does not imply that the server has responded), [`Error.Unconfigured`](Error.md) if not in the tree, [`Error.Busy`](Error.md) if still processing previous request, [`Error.InvalidParameter`](Error.md) if given string is not a valid URL format, or [`Error.CantConnect`](Error.md) if not using thread and the [`HttpClient`](HttpClient.md) cannot connect to host.

### RequestRaw

```csharp
public Error RequestRaw(string url, string[] customHeaders = null, HttpClient.Method method = HttpClient.Method.Get, byte[] requestDataRaw = null)
```

Creates request on the underlying [`HttpClient`](HttpClient.md) using a raw array of bytes for the request body. If there is no configuration errors, it tries to connect using [`HttpClient.ConnectToHost`](HttpClient.md) and passes parameters onto [`HttpClient.Request`](HttpClient.md).

Returns [`Error.Ok`](Error.md) if request is successfully created. (Does not imply that the server has responded), [`Error.Unconfigured`](Error.md) if not in the tree, [`Error.Busy`](Error.md) if still processing previous request, [`Error.InvalidParameter`](Error.md) if given string is not a valid URL format, or [`Error.CantConnect`](Error.md) if not using thread and the [`HttpClient`](HttpClient.md) cannot connect to host.

参数：
- `customHeaders`：If the parameter is null, then the default value is `Array.Empty<string>()`.
- `requestDataRaw`：If the parameter is null, then the default value is `Array.Empty<byte>()`.

### SetAcceptGZip

```csharp
public void SetAcceptGZip(bool enable)
```

### SetBodySizeLimit

```csharp
public void SetBodySizeLimit(int bytes)
```

### SetDownloadChunkSize

```csharp
public void SetDownloadChunkSize(int chunkSize)
```

### SetDownloadFile

```csharp
public void SetDownloadFile(string path)
```

### SetHttpProxy

```csharp
public void SetHttpProxy(string host, int port)
```

Sets the proxy server for HTTP requests.

The proxy server is unset if `host` is empty or `port` is -1.

### SetHttpsProxy

```csharp
public void SetHttpsProxy(string host, int port)
```

Sets the proxy server for HTTPS requests.

The proxy server is unset if `host` is empty or `port` is -1.

### SetMaxRedirects

```csharp
public void SetMaxRedirects(int amount)
```

### SetTimeout

```csharp
public void SetTimeout(double timeout)
```

### SetTlsOptions

```csharp
public void SetTlsOptions(TlsOptions clientOptions)
```

Sets the [`TlsOptions`](TlsOptions.md) to be used when connecting to an HTTPS server. See [`TlsOptions.Client`](TlsOptions.md).

### SetUseThreads

```csharp
public void SetUseThreads(bool enable)
```

## 事件（信号）

### RequestCompleted

```csharp
public event HttpRequest.RequestCompletedEventHandler RequestCompleted
```

Emitted when a request is completed.

## RequestCompletedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`HttpRequest.RequestCompleted`](HttpRequest.md) event of a [`HttpRequest`](HttpRequest.md) class.

### 签名

#### Invoke

```csharp
delegate void RequestCompletedEventHandler(long result, long responseCode, string[] headers, byte[] body)
```

## Result

命名空间 `Godot`；enum

### 枚举值

- `Success = 0`：Request successful.
- `ChunkedBodySizeMismatch = 1`：Request failed due to a mismatch between the expected and actual chunked body size during transfer.
- `CantConnect = 2`：Request failed while connecting.
- `CantResolve = 3`：Request failed while resolving.
- `ConnectionError = 4`：Request failed due to connection (read/write) error.
- `TlsHandshakeError = 5`：Request failed on TLS handshake.
- `NoResponse = 6`：Request does not have a response (yet).
- `BodySizeLimitExceeded = 7`：Request exceeded its maximum size limit, see [`HttpRequest.BodySizeLimit`](HttpRequest.md).
- `BodyDecompressFailed = 8`：Request failed due to an error while decompressing the response body.
- `RequestFailed = 9`：Request failed (currently unused).
- `DownloadFileCantOpen = 10`：HTTPRequest couldn't open the download file.
- `DownloadFileWriteError = 11`：HTTPRequest couldn't write to the download file.
- `RedirectLimitReached = 12`：Request reached its maximum redirect limit, see [`HttpRequest.MaxRedirects`](HttpRequest.md).
- `Timeout = 13`：Request failed due to a timeout.
