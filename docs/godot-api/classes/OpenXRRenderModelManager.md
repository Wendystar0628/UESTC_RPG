<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRRenderModelManager

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This helper node will automatically manage displaying render models. It will create new [`OpenXRRenderModel`](OpenXRRenderModel.md) nodes as controllers and other hand held devices are detected, and remove those nodes when they are deactivated.

**Note:** If you want more control over this logic you can alternatively call [`OpenXRRenderModelExtension.RenderModelGetAll`](OpenXRRenderModelExtension.md) to obtain a list of active render model ids and create [`OpenXRRenderModel`](OpenXRRenderModel.md) instances for each render model id provided.

## 构造函数

### OpenXRRenderModelManager

```csharp
public OpenXRRenderModelManager()
```

## 属性

### MakeLocalToPose

```csharp
public string MakeLocalToPose { get; set; }
```

引擎名：`make_local_to_pose`

Position render models local to this pose (this will adjust the position of the render models container node).

### Tracker

```csharp
public OpenXRRenderModelManager.RenderModelTracker Tracker { get; set; }
```

引擎名：`tracker`

Limits render models to the specified tracker. Include: 0 = All render models, 1 = Render models not related to a tracker, 2 = Render models related to the left hand tracker, 3 = Render models related to the right hand tracker.

## 方法

### GetMakeLocalToPose

```csharp
public string GetMakeLocalToPose()
```

引擎名：`get_make_local_to_pose`

### GetTracker

```csharp
public OpenXRRenderModelManager.RenderModelTracker GetTracker()
```

引擎名：`get_tracker`

### SetMakeLocalToPose

```csharp
public void SetMakeLocalToPose(string makeLocalToPose)
```

引擎名：`set_make_local_to_pose`

### SetTracker

```csharp
public void SetTracker(OpenXRRenderModelManager.RenderModelTracker tracker)
```

引擎名：`set_tracker`

## 事件（信号）

### RenderModelAdded

```csharp
public event OpenXRRenderModelManager.RenderModelAddedEventHandler RenderModelAdded
```

引擎名：`render_model_added`

Emitted when a render model node is added as a child to this node.

### RenderModelRemoved

```csharp
public event OpenXRRenderModelManager.RenderModelRemovedEventHandler RenderModelRemoved
```

引擎名：`render_model_removed`

Emitted when a render model child node is about to be removed from this node.

## RenderModelAddedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`OpenXRRenderModelManager.RenderModelAdded`](OpenXRRenderModelManager.md) event of a [`OpenXRRenderModelManager`](OpenXRRenderModelManager.md) class.

### 签名

#### Invoke

```csharp
delegate void RenderModelAddedEventHandler(OpenXRRenderModel renderModel)
```

## RenderModelRemovedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`OpenXRRenderModelManager.RenderModelRemoved`](OpenXRRenderModelManager.md) event of a [`OpenXRRenderModelManager`](OpenXRRenderModelManager.md) class.

### 签名

#### Invoke

```csharp
delegate void RenderModelRemovedEventHandler(OpenXRRenderModel renderModel)
```

## RenderModelTracker

命名空间 `Godot`；enum

### 枚举值

- `Any = 0`：All active render models are shown regardless of what tracker they relate to.
- `NoneSet = 1`：Only active render models are shown that are not related to any tracker we manage.
- `LeftHand = 2`：Only active render models are shown that are related to the left hand tracker.
- `RightHand = 3`：Only active render models are shown that are related to the right hand tracker.
