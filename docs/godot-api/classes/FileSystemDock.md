<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# FileSystemDock

命名空间 `Godot`；class；继承 [`EditorDock`](EditorDock.md) → [`MarginContainer`](MarginContainer.md) → [`Container`](Container.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

This class is available only in [`EditorPlugin`](EditorPlugin.md)s and can't be instantiated. You can access it using [`EditorInterface.GetFileSystemDock`](EditorInterface.md).

While `FileSystemDock` doesn't expose any methods for file manipulation, it can listen for various file-related signals.

## 方法

### AddResourceTooltipPlugin

```csharp
public void AddResourceTooltipPlugin(EditorResourceTooltipPlugin plugin)
```

引擎名：`add_resource_tooltip_plugin`

Registers a new [`EditorResourceTooltipPlugin`](EditorResourceTooltipPlugin.md).

### NavigateToPath

```csharp
public void NavigateToPath(string path)
```

引擎名：`navigate_to_path`

Sets the given `path` as currently selected, ensuring that the selected file/directory is visible.

### RemoveResourceTooltipPlugin

```csharp
public void RemoveResourceTooltipPlugin(EditorResourceTooltipPlugin plugin)
```

引擎名：`remove_resource_tooltip_plugin`

Removes an [`EditorResourceTooltipPlugin`](EditorResourceTooltipPlugin.md). Fails if the plugin wasn't previously added.

## 事件（信号）

### DisplayModeChanged

```csharp
public event Action DisplayModeChanged
```

引擎名：`display_mode_changed`

Emitted when the user switches file display mode or split mode.

### FileRemoved

```csharp
public event FileSystemDock.FileRemovedEventHandler FileRemoved
```

引擎名：`file_removed`

Emitted when the given `file` was removed.

### FilesMoved

```csharp
public event FileSystemDock.FilesMovedEventHandler FilesMoved
```

引擎名：`files_moved`

Emitted when a file is moved from `oldFile` path to `newFile` path.

### FolderColorChanged

```csharp
public event Action FolderColorChanged
```

引擎名：`folder_color_changed`

Emitted when folders change color.

### FolderMoved

```csharp
public event FileSystemDock.FolderMovedEventHandler FolderMoved
```

引擎名：`folder_moved`

Emitted when a folder is moved from `oldFolder` path to `newFolder` path.

### FolderRemoved

```csharp
public event FileSystemDock.FolderRemovedEventHandler FolderRemoved
```

引擎名：`folder_removed`

Emitted when the given `folder` was removed.

### Inherit

```csharp
public event FileSystemDock.InheritEventHandler Inherit
```

引擎名：`inherit`

Emitted when a new scene is created that inherits the scene at `file` path.

### Instantiate

```csharp
public event FileSystemDock.InstantiateEventHandler Instantiate
```

引擎名：`instantiate`

Emitted when the given scenes are being instantiated in the editor.

### ResourceRemoved

```csharp
public event FileSystemDock.ResourceRemovedEventHandler ResourceRemoved
```

引擎名：`resource_removed`

Emitted when an external `resource` had its file removed.

### SelectionChanged

```csharp
public event Action SelectionChanged
```

引擎名：`selection_changed`

Emitted when the selection changes. Use [`EditorInterface.GetSelectedPaths`](EditorInterface.md) in the connected method to get the selected paths.

## FileRemovedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`FileSystemDock.FileRemoved`](FileSystemDock.md) event of a [`FileSystemDock`](FileSystemDock.md) class.

### 签名

#### Invoke

```csharp
delegate void FileRemovedEventHandler(string file)
```

## FilesMovedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`FileSystemDock.FilesMoved`](FileSystemDock.md) event of a [`FileSystemDock`](FileSystemDock.md) class.

### 签名

#### Invoke

```csharp
delegate void FilesMovedEventHandler(string oldFile, string newFile)
```

## FolderMovedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`FileSystemDock.FolderMoved`](FileSystemDock.md) event of a [`FileSystemDock`](FileSystemDock.md) class.

### 签名

#### Invoke

```csharp
delegate void FolderMovedEventHandler(string oldFolder, string newFolder)
```

## FolderRemovedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`FileSystemDock.FolderRemoved`](FileSystemDock.md) event of a [`FileSystemDock`](FileSystemDock.md) class.

### 签名

#### Invoke

```csharp
delegate void FolderRemovedEventHandler(string folder)
```

## InheritEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`FileSystemDock.Inherit`](FileSystemDock.md) event of a [`FileSystemDock`](FileSystemDock.md) class.

### 签名

#### Invoke

```csharp
delegate void InheritEventHandler(string file)
```

## InstantiateEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`FileSystemDock.Instantiate`](FileSystemDock.md) event of a [`FileSystemDock`](FileSystemDock.md) class.

### 签名

#### Invoke

```csharp
delegate void InstantiateEventHandler(string[] files)
```

## ResourceRemovedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`FileSystemDock.ResourceRemoved`](FileSystemDock.md) event of a [`FileSystemDock`](FileSystemDock.md) class.

### 签名

#### Invoke

```csharp
delegate void ResourceRemovedEventHandler(Resource resource)
```
