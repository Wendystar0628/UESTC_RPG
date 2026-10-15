<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CameraFeed

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A camera feed gives you access to a single physical camera attached to your device. When enabled, Godot will start capturing frames from the camera which can then be used. See also [`CameraServer`](CameraServer.md).

**Note:** Many cameras will return YCbCr images which are split into two textures and need to be combined in a shader. Godot does this automatically for you if you set the environment to show the camera image in the background.

**Note:** This class is currently only implemented on Linux, Android, macOS, and iOS. On other platforms no `CameraFeed`s will be available. To get a `CameraFeed` on iOS, enable `EditorExportPlatformIOS.modules/camera`.

## 构造函数

### CameraFeed

```csharp
public CameraFeed()
```

## 属性

### FeedIsActive

```csharp
public bool FeedIsActive { get; set; }
```

引擎名：`feed_is_active`

If `true`, the feed is active.

### FeedTransform

```csharp
public Transform2D FeedTransform { get; set; }
```

引擎名：`feed_transform`

The transform applied to the camera's image.

### Formats

```csharp
public Array Formats { get; }
```

引擎名：`formats`

Formats supported by the feed. Each entry is a [`Collections.Dictionary`](Collections.Dictionary.md) describing format parameters.

## 方法

### GetDatatype

```csharp
public CameraFeed.FeedDataType GetDatatype()
```

引擎名：`get_datatype`

Returns feed image data type.

### GetFormats

```csharp
public Array GetFormats()
```

引擎名：`get_formats`

### GetId

```csharp
public int GetId()
```

引擎名：`get_id`

Returns the unique ID for this feed.

### GetName

```csharp
public string GetName()
```

引擎名：`get_name`

Returns the camera's name.

### GetPosition

```csharp
public CameraFeed.FeedPosition GetPosition()
```

引擎名：`get_position`

Returns the position of camera on the device.

### GetTextureTexId

```csharp
public ulong GetTextureTexId(CameraServer.FeedImage feedImageType)
```

引擎名：`get_texture_tex_id`

Returns the texture backend ID (usable by some external libraries that need a handle to a texture to write data).

### GetTransform

```csharp
public Transform2D GetTransform()
```

引擎名：`get_transform`

### IsActive

```csharp
public bool IsActive()
```

引擎名：`is_active`

### SetActive

```csharp
public void SetActive(bool active)
```

引擎名：`set_active`

### SetExternal

```csharp
public void SetExternal(int width, int height)
```

引擎名：`set_external`

Sets the feed as external feed provided by another library.

### SetFormat

```csharp
public bool SetFormat(int index, Dictionary parameters)
```

引擎名：`set_format`

Sets the feed format parameters for the given `index` in the `Formats` array. Returns `true` on success. By default, the YUYV encoded stream is transformed to [`CameraFeed.FeedDataType.Rgb`](CameraFeed.md). The YUYV encoded stream output format can be changed by setting `parameters`'s `output` entry to one of the following:

- `"separate"` will result in [`CameraFeed.FeedDataType.YcbcrSep`](CameraFeed.md);

- `"grayscale"` will result in desaturated [`CameraFeed.FeedDataType.Rgb`](CameraFeed.md);

- `"copy"` will result in [`CameraFeed.FeedDataType.Ycbcr`](CameraFeed.md).

### SetName

```csharp
public void SetName(string name)
```

引擎名：`set_name`

Sets the camera's name.

### SetPosition

```csharp
public void SetPosition(CameraFeed.FeedPosition position)
```

引擎名：`set_position`

Sets the position of this camera.

### SetRgbImage

```csharp
public void SetRgbImage(Image rgbImage)
```

引擎名：`set_rgb_image`

Sets RGB image for this feed.

### SetTransform

```csharp
public void SetTransform(Transform2D transform)
```

引擎名：`set_transform`

### SetYcbcrImage

```csharp
public void SetYcbcrImage(Image ycbcrImage)
```

引擎名：`set_ycbcr_image`

Sets YCbCr image for this feed.

### SetYcbcrImages

```csharp
public void SetYcbcrImages(Image yImage, Image cbcrImage)
```

引擎名：`set_ycbcr_images`

Sets Y and CbCr images for this feed.

### _ActivateFeed

```csharp
public virtual bool _ActivateFeed()
```

引擎名：`_activate_feed`

Called when the camera feed is activated.

### _DeactivateFeed

```csharp
public virtual void _DeactivateFeed()
```

引擎名：`_deactivate_feed`

Called when the camera feed is deactivated.

### _GetFormats

```csharp
public virtual Array _GetFormats()
```

引擎名：`_get_formats`

Override this method to define supported formats of the camera feed.

### _SetFormat

```csharp
public virtual bool _SetFormat(int index, Dictionary parameters)
```

引擎名：`_set_format`

Override this method to set the format of the camera feed.

## 事件（信号）

### FormatChanged

```csharp
public event Action FormatChanged
```

引擎名：`format_changed`

Emitted when the format has changed.

### FrameChanged

```csharp
public event Action FrameChanged
```

引擎名：`frame_changed`

Emitted when a new frame is available.

## FeedDataType

命名空间 `Godot`；enum

### 枚举值

- `Noimage = 0`：No image set for the feed.
- `Rgb = 1`：Feed supplies RGB images.
- `Ycbcr = 2`：Feed supplies YCbCr images that need to be converted to RGB.
- `YcbcrSep = 3`：Feed supplies separate Y and CbCr images that need to be combined and converted to RGB.
- `External = 4`：Feed supplies external image.

## FeedPosition

命名空间 `Godot`；enum

### 枚举值

- `Unspecified = 0`：Unspecified position.
- `Front = 1`：Camera is mounted at the front of the device.
- `Back = 2`：Camera is mounted at the back of the device.
