<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CameraTexture

命名空间 `Godot`；class；继承 [`Texture2D`](Texture2D.md) → [`Texture`](Texture.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This texture gives access to the camera texture provided by a [`CameraFeed`](CameraFeed.md).

**Note:** Many cameras supply YCbCr images which need to be converted in a shader.

## 构造函数

### CameraTexture

```csharp
public CameraTexture()
```

## 属性

### CameraFeedId

```csharp
public int CameraFeedId { get; set; }
```

引擎名：`camera_feed_id`

The ID of the [`CameraFeed`](CameraFeed.md) for which we want to display the image.

### CameraIsActive

```csharp
public bool CameraIsActive { get; set; }
```

引擎名：`camera_is_active`

Convenience property that gives access to the active property of the [`CameraFeed`](CameraFeed.md).

### WhichFeed

```csharp
public CameraServer.FeedImage WhichFeed { get; set; }
```

引擎名：`which_feed`

Which image within the [`CameraFeed`](CameraFeed.md) we want access to, important if the camera image is split in a Y and CbCr component.

## 方法

### GetCameraActive

```csharp
public bool GetCameraActive()
```

引擎名：`get_camera_active`

### GetCameraFeedId

```csharp
public int GetCameraFeedId()
```

引擎名：`get_camera_feed_id`

### GetWhichFeed

```csharp
public CameraServer.FeedImage GetWhichFeed()
```

引擎名：`get_which_feed`

### SetCameraActive

```csharp
public void SetCameraActive(bool active)
```

引擎名：`set_camera_active`

### SetCameraFeedId

```csharp
public void SetCameraFeedId(int feedId)
```

引擎名：`set_camera_feed_id`

### SetWhichFeed

```csharp
public void SetWhichFeed(CameraServer.FeedImage whichFeed)
```

引擎名：`set_which_feed`
