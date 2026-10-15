<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# HttpClient

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Hyper-text transfer protocol client (sometimes called "User Agent"). Used to make HTTP requests to download web content, upload files and other data or to communicate with various services, among other use cases.

See the [`HttpRequest`](HttpRequest.md) node for a higher-level alternative.

**Note:** This client only needs to connect to a host once (see `ConnectToHost()`) to send multiple requests. Because of this, methods that take URLs usually take just the part after the host instead of the full URL, as the client is already connected to a host. See `Request()` for a full example and to get started.

An `HttpClient` should be reused between multiple requests or to connect to different hosts instead of creating one client per request. Supports Transport Layer Security (TLS), including server certificate verification. HTTP status codes in the 2xx range indicate success, 3xx redirection (i.e. "try again, but over here"), 4xx something was wrong with the request, and 5xx something went wrong on the server's side.

For more information on HTTP, see [MDN's documentation on HTTP](https://developer.mozilla.org/en-US/docs/Web/HTTP) (or read [RFC 2616](https://tools.ietf.org/html/rfc2616) to get it straight from the source).

**Note:** When exporting to Android, make sure to enable the `INTERNET` permission in the Android export preset before exporting the project or using one-click deploy. Otherwise, network communication of any kind will be blocked by Android.

**Note:** It's recommended to use transport encryption (TLS) and to avoid sending sensitive information (such as login credentials) in HTTP GET URL parameters. Consider using HTTP POST requests or HTTP headers for such information instead.

**Note:** When performing HTTP requests from a project exported to Web, keep in mind the remote server may not allow requests from foreign origins due to [CORS](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS). If you host the server in question, you should modify its backend to allow requests from foreign origins by adding the `Access-Control-Allow-Origin: *` HTTP header.

**Note:** TLS support is currently limited to TLSv1.2 and TLSv1.3. Attempting to connect to a server that only supports older (insecure) TLS versions will return an error.

**Warning:** TLS certificate revocation and certificate pinning are currently not supported. Revoked certificates are accepted as long as they are otherwise valid. If this is a concern, you may want to use automatically managed certificates with a short validity period.

## 构造函数

### HttpClient

```csharp
public HttpClient()
```

## 属性

### BlockingModeEnabled

```csharp
public bool BlockingModeEnabled { get; set; }
```

If `true`, execution will block until all data is read from the response.

### Connection

```csharp
public StreamPeer Connection { get; set; }
```

The connection to use for this client.

### ReadChunkSize

```csharp
public int ReadChunkSize { get; set; }
```

The size of the buffer used and maximum bytes to read per iteration. See `ReadResponseBodyChunk()`.

## 方法

### Close

```csharp
public void Close()
```

Closes the current connection, allowing reuse of this `HttpClient`.

### ConnectToHost

```csharp
public Error ConnectToHost(string host, int port = -1, TlsOptions tlsOptions = null)
```

Connects to a host. This needs to be done before any requests are sent.

If no `port` is specified (or `-1` is used), it is automatically set to 80 for HTTP and 443 for HTTPS. You can pass the optional `tlsOptions` parameter to customize the trusted certification authorities, or the common name verification when using HTTPS. See [`TlsOptions.Client`](TlsOptions.md) and [`TlsOptions.ClientUnsafe`](TlsOptions.md).

### GetConnection

```csharp
public StreamPeer GetConnection()
```

### GetReadChunkSize

```csharp
public int GetReadChunkSize()
```

### GetResponseBodyLength

```csharp
public long GetResponseBodyLength()
```

Returns the response's body length.

**Note:** Some Web servers may not send a body length. In this case, the value returned will be `-1`. If using chunked transfer encoding, the body length will also be `-1`.

**Note:** This function always returns `-1` on the Web platform due to browsers limitations.

### GetResponseCode

```csharp
public int GetResponseCode()
```

Returns the response's HTTP status code.

### GetResponseHeaders

```csharp
public string[] GetResponseHeaders()
```

Returns the response headers.

### GetResponseHeadersAsDictionary

```csharp
public Dictionary GetResponseHeadersAsDictionary()
```

Returns all response headers as a [`Collections.Dictionary`](Collections.Dictionary.md). Each entry is composed by the header name, and a `System.String` containing the values separated by `"; "`. The casing is kept the same as the headers were received.

```csharp
{
"content-length": 12,
"Content-Type": "application/json; charset=UTF-8",
}

```

### GetStatus

```csharp
public HttpClient.Status GetStatus()
```

Returns a [`HttpClient.Status`](HttpClient.md) constant. Need to call `Poll()` in order to get status updates.

### HasResponse

```csharp
public bool HasResponse()
```

If `true`, this `HttpClient` has a response available.

### IsBlockingModeEnabled

```csharp
public bool IsBlockingModeEnabled()
```

### IsResponseChunked

```csharp
public bool IsResponseChunked()
```

If `true`, this `HttpClient` has a response that is chunked.

### Poll

```csharp
public Error Poll()
```

This needs to be called in order to have any request processed. Check results with `GetStatus()`.

### QueryStringFromDict

```csharp
public string QueryStringFromDict(Dictionary fields)
```

Generates a GET/POST application/x-www-form-urlencoded style query string from a provided dictionary, e.g.:

```csharp
var fields = new Godot.Collections.Dictionary { { "username", "user" }, { "password", "pass" } };
string queryString = httpClient.QueryStringFromDict(fields);
// Returns "username=user&password=pass"

```

Furthermore, if a key has a `null` value, only the key itself is added, without equal sign and value. If the value is an array, for each value in it a pair with the same key is added.

```csharp
var fields = new Godot.Collections.Dictionary
{
{ "single", 123 },
{ "notValued", default },
{ "multiple", new Godot.Collections.Array { 22, 33, 44 } },
};
string queryString = httpClient.QueryStringFromDict(fields);
// Returns "single=123&not_valued&multiple=22&multiple=33&multiple=44"

```

### ReadResponseBodyChunk

```csharp
public byte[] ReadResponseBodyChunk()
```

Reads one chunk from the response.

### Request

```csharp
public Error Request(HttpClient.Method method, string url, ReadOnlySpan<string> headers, string body)
```

Sends an HTTP request to the connected host with the given `method`.

The URL parameter is usually just the part after the host, so for `https://example.com/index.php`, it is `/index.php`. When sending requests to an HTTP proxy server, it should be an absolute URL. For [`HttpClient.Method.Options`](HttpClient.md) requests, `*` is also allowed. For [`HttpClient.Method.Connect`](HttpClient.md) requests, it should be the authority component (`host:port`).

`headers` are HTTP request headers.

To create a POST request with query strings to push to the server, do:

```csharp
var fields = new Godot.Collections.Dictionary { { "username", "user" }, { "password", "pass" } };
string queryString = new HttpClient().QueryStringFromDict(fields);
string[] headers = ["Content-Type: application/x-www-form-urlencoded", $"Content-Length: {queryString.Length}"];
var result = new HttpClient().Request(HttpClient.Method.Post, "index.php", headers, queryString);

```

**Note:** The `body` parameter is ignored if `method` is [`HttpClient.Method.Get`](HttpClient.md). This is because GET methods can't contain request data. As a workaround, you can pass request data as a query string in the URL. See `String.uri_encode` for an example.

### Request

```csharp
public Error Request(HttpClient.Method method, string url, string[] headers, string body = "")
```

Sends an HTTP request to the connected host with the given `method`.

The URL parameter is usually just the part after the host, so for `https://example.com/index.php`, it is `/index.php`. When sending requests to an HTTP proxy server, it should be an absolute URL. For [`HttpClient.Method.Options`](HttpClient.md) requests, `*` is also allowed. For [`HttpClient.Method.Connect`](HttpClient.md) requests, it should be the authority component (`host:port`).

`headers` are HTTP request headers.

To create a POST request with query strings to push to the server, do:

```csharp
var fields = new Godot.Collections.Dictionary { { "username", "user" }, { "password", "pass" } };
string queryString = new HttpClient().QueryStringFromDict(fields);
string[] headers = ["Content-Type: application/x-www-form-urlencoded", $"Content-Length: {queryString.Length}"];
var result = new HttpClient().Request(HttpClient.Method.Post, "index.php", headers, queryString);

```

**Note:** The `body` parameter is ignored if `method` is [`HttpClient.Method.Get`](HttpClient.md). This is because GET methods can't contain request data. As a workaround, you can pass request data as a query string in the URL. See `String.uri_encode` for an example.

### RequestRaw

```csharp
public Error RequestRaw(HttpClient.Method method, string url, ReadOnlySpan<string> headers, ReadOnlySpan<byte> body)
```

Sends a raw HTTP request to the connected host with the given `method`.

The URL parameter is usually just the part after the host, so for `https://example.com/index.php`, it is `/index.php`. When sending requests to an HTTP proxy server, it should be an absolute URL. For [`HttpClient.Method.Options`](HttpClient.md) requests, `*` is also allowed. For [`HttpClient.Method.Connect`](HttpClient.md) requests, it should be the authority component (`host:port`).

`headers` are HTTP request headers.

Sends the body data raw, as a byte array and does not encode it in any way.

### RequestRaw

```csharp
public Error RequestRaw(HttpClient.Method method, string url, string[] headers, byte[] body)
```

Sends a raw HTTP request to the connected host with the given `method`.

The URL parameter is usually just the part after the host, so for `https://example.com/index.php`, it is `/index.php`. When sending requests to an HTTP proxy server, it should be an absolute URL. For [`HttpClient.Method.Options`](HttpClient.md) requests, `*` is also allowed. For [`HttpClient.Method.Connect`](HttpClient.md) requests, it should be the authority component (`host:port`).

`headers` are HTTP request headers.

Sends the body data raw, as a byte array and does not encode it in any way.

### SetBlockingMode

```csharp
public void SetBlockingMode(bool enabled)
```

### SetConnection

```csharp
public void SetConnection(StreamPeer connection)
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

### SetReadChunkSize

```csharp
public void SetReadChunkSize(int bytes)
```

## Method

命名空间 `Godot`；enum

### 枚举值

- `Get = 0`：HTTP GET method.
- `Head = 1`：HTTP HEAD method.
- `Post = 2`：HTTP POST method.
- `Put = 3`：HTTP PUT method.
- `Delete = 4`：HTTP DELETE method.
- `Options = 5`：HTTP OPTIONS method.
- `Trace = 6`：HTTP TRACE method.
- `Connect = 7`：HTTP CONNECT method.
- `Patch = 8`：HTTP PATCH method.
- `Max = 9`：Represents the size of the `HttpClient.Method` enum.

## ResponseCode

命名空间 `Godot`；enum

### 枚举值

- `Continue = 100`：HTTP status code `100 Continue`.
- `SwitchingProtocols = 101`：HTTP status code `101 Switching Protocol`.
- `Processing = 102`：HTTP status code `102 Processing` (WebDAV).
- `Ok = 200`：HTTP status code `200 OK`.
- `Created = 201`：HTTP status code `201 Created`.
- `Accepted = 202`：HTTP status code `202 Accepted`.
- `NonAuthoritativeInformation = 203`：HTTP status code `203 Non-Authoritative Information`.
- `NoContent = 204`：HTTP status code `204 No Content`.
- `ResetContent = 205`：HTTP status code `205 Reset Content`.
- `PartialContent = 206`：HTTP status code `206 Partial Content`.
- `MultiStatus = 207`：HTTP status code `207 Multi-Status` (WebDAV).
- `AlreadyReported = 208`：HTTP status code `208 Already Reported` (WebDAV).
- `ImUsed = 226`：HTTP status code `226 IM Used` (WebDAV).
- `MultipleChoices = 300`：HTTP status code `300 Multiple Choice`.
- `MovedPermanently = 301`：HTTP status code `301 Moved Permanently`.
- `Found = 302`：HTTP status code `302 Found`.
- `SeeOther = 303`：HTTP status code `303 See Other`.
- `NotModified = 304`：HTTP status code `304 Not Modified`.
- `UseProxy = 305`：HTTP status code `305 Use Proxy`.
- `SwitchProxy = 306`：HTTP status code `306 Switch Proxy`.
- `TemporaryRedirect = 307`：HTTP status code `307 Temporary Redirect`.
- `PermanentRedirect = 308`：HTTP status code `308 Permanent Redirect`.
- `BadRequest = 400`：HTTP status code `400 Bad Request`.
- `Unauthorized = 401`：HTTP status code `401 Unauthorized`.
- `PaymentRequired = 402`：HTTP status code `402 Payment Required`.
- `Forbidden = 403`：HTTP status code `403 Forbidden`.
- `NotFound = 404`：HTTP status code `404 Not Found`.
- `MethodNotAllowed = 405`：HTTP status code `405 Method Not Allowed`.
- `NotAcceptable = 406`：HTTP status code `406 Not Acceptable`.
- `ProxyAuthenticationRequired = 407`：HTTP status code `407 Proxy Authentication Required`.
- `RequestTimeout = 408`：HTTP status code `408 Request Timeout`.
- `Conflict = 409`：HTTP status code `409 Conflict`.
- `Gone = 410`：HTTP status code `410 Gone`.
- `LengthRequired = 411`：HTTP status code `411 Length Required`.
- `PreconditionFailed = 412`：HTTP status code `412 Precondition Failed`.
- `RequestEntityTooLarge = 413`：HTTP status code `413 Entity Too Large`.
- `RequestUriTooLong = 414`：HTTP status code `414 Request-URI Too Long`.
- `UnsupportedMediaType = 415`：HTTP status code `415 Unsupported Media Type`.
- `RequestedRangeNotSatisfiable = 416`：HTTP status code `416 Requested Range Not Satisfiable`.
- `ExpectationFailed = 417`：HTTP status code `417 Expectation Failed`.
- `ImATeapot = 418`：HTTP status code `418 I'm A Teapot`.
- `MisdirectedRequest = 421`：HTTP status code `421 Misdirected Request`.
- `UnprocessableEntity = 422`：HTTP status code `422 Unprocessable Entity` (WebDAV).
- `Locked = 423`：HTTP status code `423 Locked` (WebDAV).
- `FailedDependency = 424`：HTTP status code `424 Failed Dependency` (WebDAV).
- `UpgradeRequired = 426`：HTTP status code `426 Upgrade Required`.
- `PreconditionRequired = 428`：HTTP status code `428 Precondition Required`.
- `TooManyRequests = 429`：HTTP status code `429 Too Many Requests`.
- `RequestHeaderFieldsTooLarge = 431`：HTTP status code `431 Request Header Fields Too Large`.
- `UnavailableForLegalReasons = 451`：HTTP status code `451 Response Unavailable For Legal Reasons`.
- `InternalServerError = 500`：HTTP status code `500 Internal Server Error`.
- `NotImplemented = 501`：HTTP status code `501 Not Implemented`.
- `BadGateway = 502`：HTTP status code `502 Bad Gateway`.
- `ServiceUnavailable = 503`：HTTP status code `503 Service Unavailable`.
- `GatewayTimeout = 504`：HTTP status code `504 Gateway Timeout`.
- `HttpVersionNotSupported = 505`：HTTP status code `505 HTTP Version Not Supported`.
- `VariantAlsoNegotiates = 506`：HTTP status code `506 Variant Also Negotiates`.
- `InsufficientStorage = 507`：HTTP status code `507 Insufficient Storage`.
- `LoopDetected = 508`：HTTP status code `508 Loop Detected`.
- `NotExtended = 510`：HTTP status code `510 Not Extended`.
- `NetworkAuthRequired = 511`：HTTP status code `511 Network Authentication Required`.

## Status

命名空间 `Godot`；enum

### 枚举值

- `Disconnected = 0`：Status: Disconnected from the server.
- `Resolving = 1`：Status: Currently resolving the hostname for the given URL into an IP.
- `CantResolve = 2`：Status: DNS failure: Can't resolve the hostname for the given URL.
- `Connecting = 3`：Status: Currently connecting to server.
- `CantConnect = 4`：Status: Can't connect to the server.
- `Connected = 5`：Status: Connection established.
- `Requesting = 6`：Status: Currently sending request.
- `Body = 7`：Status: HTTP body received.
- `ConnectionError = 8`：Status: Error in HTTP connection.
- `TlsHandshakeError = 9`：Status: Error in TLS handshake.
