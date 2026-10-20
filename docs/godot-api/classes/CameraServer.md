<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CameraServer

命名空间 `Godot`；static class

The `CameraServer` keeps track of different cameras accessible in Godot. These are external cameras such as webcams or the cameras on your phone.

It is notably used to provide AR modules with a video feed from the camera.

**Note:** This class is currently only implemented on Linux, Android, macOS, and iOS. On other platforms no [`CameraFeed`](CameraFeed.md)s will be available. To get a [`CameraFeed`](CameraFeed.md) on iOS, enable `EditorExportPlatformIOS.modules/camera`.

## 属性

### MonitoringFeeds

```csharp
public static bool MonitoringFeeds { get; set; }
```

引擎名：`monitoring_feeds`

If `true`, the server is actively monitoring available camera feeds.

This has a performance cost, so only set it to `true` when you're actively accessing the camera.

**Note:** After setting it to `true`, you can receive updated camera feeds through the `CameraFeedsUpdated` signal.

```csharp
public override void _Ready()
{
CameraServer.CameraFeedsUpdated += OnCameraFeedsUpdated;
CameraServer.MonitoringFeeds = true;
}

void OnCameraFeedsUpdated()
{
var feeds = CameraServer.Feeds();
}

```

### Singleton

```csharp
public static CameraServerInstance Singleton { get; }
```

## 方法

### AddFeed

```csharp
public static void AddFeed(CameraFeed feed)
```

引擎名：`add_feed`

Adds the camera `feed` to the camera server.

### Feeds

```csharp
public static Array<CameraFeed> Feeds()
```

引擎名：`feeds`

Returns an array of [`CameraFeed`](CameraFeed.md)s.

### GetFeed

```csharp
public static CameraFeed GetFeed(int index)
```

引擎名：`get_feed`

Returns the [`CameraFeed`](CameraFeed.md) corresponding to the camera with the given `index`.

### GetFeedCount

```csharp
public static int GetFeedCount()
```

引擎名：`get_feed_count`

Returns the number of [`CameraFeed`](CameraFeed.md)s registered.

### IsMonitoringFeeds

```csharp
public static bool IsMonitoringFeeds()
```

引擎名：`is_monitoring_feeds`

### RemoveFeed

```csharp
public static void RemoveFeed(CameraFeed feed)
```

引擎名：`remove_feed`

Removes the specified camera `feed`.

### SetMonitoringFeeds

```csharp
public static void SetMonitoringFeeds(bool isMonitoringFeeds)
```

引擎名：`set_monitoring_feeds`

## 事件（信号）

### CameraFeedAdded

```csharp
public event CameraServer.CameraFeedAddedEventHandler CameraFeedAdded
```

引擎名：`camera_feed_added`

Emitted when a [`CameraFeed`](CameraFeed.md) is added (e.g. a webcam is plugged in).

### CameraFeedRemoved

```csharp
public event CameraServer.CameraFeedRemovedEventHandler CameraFeedRemoved
```

引擎名：`camera_feed_removed`

Emitted when a [`CameraFeed`](CameraFeed.md) is removed (e.g. a webcam is unplugged).

### CameraFeedsUpdated

```csharp
public event Action CameraFeedsUpdated
```

引擎名：`camera_feeds_updated`

Emitted when camera feeds are updated.

## CameraFeedAddedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`CameraServer.CameraFeedAdded`](CameraServer.md) event of a [`CameraServer`](CameraServer.md) class.

### 签名

#### Invoke

```csharp
delegate void CameraFeedAddedEventHandler(long id)
```

## CameraFeedRemovedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`CameraServer.CameraFeedRemoved`](CameraServer.md) event of a [`CameraServer`](CameraServer.md) class.

### 签名

#### Invoke

```csharp
delegate void CameraFeedRemovedEventHandler(long id)
```

## FeedImage

命名空间 `Godot`；enum

### 枚举值

- `RgbaImage = 0`：The RGBA camera image.
- `YcbcrImage = 0`：The [YCbCr](https://en.wikipedia.org/wiki/YCbCr) camera image.
- `YImage = 0`：The Y component camera image.
- `CbcrImage = 1`：The CbCr component camera image.
