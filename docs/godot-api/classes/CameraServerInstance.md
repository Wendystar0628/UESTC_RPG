<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CameraServerInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

The [`CameraServer`](CameraServer.md) keeps track of different cameras accessible in Godot. These are external cameras such as webcams or the cameras on your phone.

It is notably used to provide AR modules with a video feed from the camera.

**Note:** This class is currently only implemented on Linux, Android, macOS, and iOS. On other platforms no [`CameraFeed`](CameraFeed.md)s will be available. To get a [`CameraFeed`](CameraFeed.md) on iOS, enable `EditorExportPlatformIOS.modules/camera`.

## 属性

### MonitoringFeeds

```csharp
public bool MonitoringFeeds { get; set; }
```

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

## 方法

### AddFeed

```csharp
public void AddFeed(CameraFeed feed)
```

Adds the camera `feed` to the camera server.

### Feeds

```csharp
public Array<CameraFeed> Feeds()
```

Returns an array of [`CameraFeed`](CameraFeed.md)s.

### GetFeed

```csharp
public CameraFeed GetFeed(int index)
```

Returns the [`CameraFeed`](CameraFeed.md) corresponding to the camera with the given `index`.

### GetFeedCount

```csharp
public int GetFeedCount()
```

Returns the number of [`CameraFeed`](CameraFeed.md)s registered.

### IsMonitoringFeeds

```csharp
public bool IsMonitoringFeeds()
```

### RemoveFeed

```csharp
public void RemoveFeed(CameraFeed feed)
```

Removes the specified camera `feed`.

### SetMonitoringFeeds

```csharp
public void SetMonitoringFeeds(bool isMonitoringFeeds)
```

## 事件（信号）

### CameraFeedAdded

```csharp
public event CameraServerInstance.CameraFeedAddedEventHandler CameraFeedAdded
```

Emitted when a [`CameraFeed`](CameraFeed.md) is added (e.g. a webcam is plugged in).

### CameraFeedRemoved

```csharp
public event CameraServerInstance.CameraFeedRemovedEventHandler CameraFeedRemoved
```

Emitted when a [`CameraFeed`](CameraFeed.md) is removed (e.g. a webcam is unplugged).

### CameraFeedsUpdated

```csharp
public event Action CameraFeedsUpdated
```

Emitted when camera feeds are updated.

## CameraFeedAddedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`CameraServerInstance.CameraFeedAdded`](CameraServerInstance.md) event of a [`CameraServerInstance`](CameraServerInstance.md) class.

### 签名

#### Invoke

```csharp
delegate void CameraFeedAddedEventHandler(long id)
```

## CameraFeedRemovedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`CameraServerInstance.CameraFeedRemoved`](CameraServerInstance.md) event of a [`CameraServerInstance`](CameraServerInstance.md) class.

### 签名

#### Invoke

```csharp
delegate void CameraFeedRemovedEventHandler(long id)
```
