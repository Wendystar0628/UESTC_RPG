<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# FileDialog

命名空间 `Godot`；class；继承 [`ConfirmationDialog`](ConfirmationDialog.md) → [`AcceptDialog`](AcceptDialog.md) → [`Window`](Window.md) → [`Viewport`](Viewport.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`FileDialog` is a preset dialog used to choose files and directories in the filesystem. It supports filter masks. `FileDialog` automatically sets its window title according to the `FileMode`. If you want to use a custom title, disable this by setting `ModeOverridesTitle` to `false`.

**Note:** `FileDialog` is invisible by default. To make it visible, call one of the `popup_*` methods from [`Window`](Window.md) on the node, such as [`Window.PopupCenteredClamped`](Window.md).

## 构造函数

### FileDialog

```csharp
public FileDialog()
```

## 属性

### Access

```csharp
public FileDialog.AccessEnum Access { get; set; }
```

引擎名：`access`

The file system access scope.

**Warning:** In Web builds, FileDialog cannot access the host file system. In sandboxed Linux and macOS environments, `UseNativeDialog` is automatically used to allow limited access to host file system.

### CurrentDir

```csharp
public string CurrentDir { get; set; }
```

引擎名：`current_dir`

The current working directory of the file dialog.

**Note:** For native file dialogs, this property is only treated as a hint and may not be respected by specific OS implementations.

### CurrentFile

```csharp
public string CurrentFile { get; set; }
```

引擎名：`current_file`

The currently selected file of the file dialog.

### CurrentPath

```csharp
public string CurrentPath { get; set; }
```

引擎名：`current_path`

The currently selected file path of the file dialog.

### DeletingEnabled

```csharp
public bool DeletingEnabled { get; set; }
```

引擎名：`deleting_enabled`

If `true`, the context menu will show the "Delete" option, which allows moving files and folders to trash.

### DisplayMode

```csharp
public FileDialog.DisplayModeEnum DisplayMode { get; set; }
```

引擎名：`display_mode`

Display mode of the dialog's file list.

### FavoritesEnabled

```csharp
public bool FavoritesEnabled { get; set; }
```

引擎名：`favorites_enabled`

If `true`, shows the toggle favorite button and favorite list on the left side of the dialog.

### FileFilterToggleEnabled

```csharp
public bool FileFilterToggleEnabled { get; set; }
```

引擎名：`file_filter_toggle_enabled`

If `true`, shows the toggle file filter button.

### FileMode

```csharp
public FileDialog.FileModeEnum FileMode { get; set; }
```

引擎名：`file_mode`

The dialog's open or save mode, which affects the selection behavior.

### FileNameFilter

```csharp
public string FileNameFilter { get; set; }
```

The filter for file names (case-insensitive). When set to a non-empty string, only files that contains the substring will be shown. `FileNameFilter` can be edited by the user with the filter button at the top of the file dialog.

See also `Filters`, which should be used to restrict the file types that can be selected instead of `FileNameFilter` which is meant to be set by the user.

### FileSortOptionsEnabled

```csharp
public bool FileSortOptionsEnabled { get; set; }
```

引擎名：`file_sort_options_enabled`

If `true`, shows the file sorting options button.

### Filters

```csharp
public string[] Filters { get; set; }
```

引擎名：`filters`

The available file type filters. Each filter string in the array should be formatted like this: `*.png,*.jpg,*.jpeg;Image Files;image/png,image/jpeg`. The description text of the filter is optional and can be omitted. Both file extensions and MIME type should be always set.

**Note:** Embedded file dialogs and Windows file dialogs support only file extensions, while Android, Linux, and macOS file dialogs also support MIME types.

### FolderCreationEnabled

```csharp
public bool FolderCreationEnabled { get; set; }
```

引擎名：`folder_creation_enabled`

If `true`, shows the button for creating new directories (when using [`FileDialog.FileModeEnum.OpenDir`](FileDialog.md), [`FileDialog.FileModeEnum.OpenAny`](FileDialog.md), or [`FileDialog.FileModeEnum.SaveFile`](FileDialog.md)), and the context menu will have the "New Folder..." option.

### HiddenFilesToggleEnabled

```csharp
public bool HiddenFilesToggleEnabled { get; set; }
```

引擎名：`hidden_files_toggle_enabled`

If `true`, shows the toggle hidden files button.

### LayoutToggleEnabled

```csharp
public bool LayoutToggleEnabled { get; set; }
```

引擎名：`layout_toggle_enabled`

If `true`, shows the layout switch buttons (list/thumbnails).

### ModeOverridesTitle

```csharp
public bool ModeOverridesTitle { get; set; }
```

引擎名：`mode_overrides_title`

If `true`, changing the `FileMode` property will set the window title accordingly (e.g. setting `FileMode` to [`FileDialog.FileModeEnum.OpenFile`](FileDialog.md) will change the window title to "Open a File").

### OptionCount

```csharp
public int OptionCount { get; set; }
```

引擎名：`option_count`

The number of additional [`OptionButton`](OptionButton.md)s and [`CheckBox`](CheckBox.md)es in the dialog.

### OverwriteWarningEnabled

```csharp
public bool OverwriteWarningEnabled { get; set; }
```

引擎名：`overwrite_warning_enabled`

If `true`, the `FileDialog` will warn the user before overwriting files in save mode.

### RecentListEnabled

```csharp
public bool RecentListEnabled { get; set; }
```

引擎名：`recent_list_enabled`

If `true`, shows the recent directories list on the left side of the dialog.

### RootSubfolder

```csharp
public string RootSubfolder { get; set; }
```

引擎名：`root_subfolder`

If non-empty, the given sub-folder will be "root" of this `FileDialog`, i.e. user won't be able to go to its parent directory.

**Note:** This property is ignored by native file dialogs.

### ShowHiddenFiles

```csharp
public bool ShowHiddenFiles { get; set; }
```

引擎名：`show_hidden_files`

If `true`, the dialog will show hidden files.

**Note:** This property is ignored by native file dialogs on Android and Linux.

### UseNativeDialog

```csharp
public bool UseNativeDialog { get; set; }
```

引擎名：`use_native_dialog`

If `true`, and if supported by the current [`DisplayServer`](DisplayServer.md), OS native dialog will be used instead of custom one.

**Note:** On Android, it is only supported when using [`FileDialog.AccessEnum.Filesystem`](FileDialog.md). For access mode [`FileDialog.AccessEnum.Resources`](FileDialog.md) and [`FileDialog.AccessEnum.Userdata`](FileDialog.md), the system will fall back to custom FileDialog.

**Note:** On Linux and macOS, sandboxed apps always use native dialogs to access the host file system.

**Note:** On macOS, sandboxed apps will save security-scoped bookmarks to retain access to the opened folders across multiple sessions. Use [`OS.GetGrantedPermissions`](OS.md) to get a list of saved bookmarks.

**Note:** Native dialogs are isolated from the base process, file dialog properties can't be modified once the dialog is shown.

**Note:** This property is ignored in `EditorFileDialog`.

## 方法

### AddFilter

```csharp
public void AddFilter(string filter, string description = "", string mimeType = "")
```

引擎名：`add_filter`

Adds a comma-separated file extension `filter` and comma-separated MIME type `mimeType` option to the `FileDialog` with an optional `description`, which restricts what files can be picked.

A `filter` should be of the form `"filename.extension"`, where filename and extension can be `*` to match any string. Filters starting with `.` (i.e. empty filenames) are not allowed.

For example, a `filter` of `"*.png, *.jpg"`, a `mimeType` of `image/png, image/jpeg`, and a `description` of `"Images"` results in filter text "Images (*.png, *.jpg)".

**Note:** Embedded file dialogs and Windows file dialogs support only file extensions, while Android, Linux, and macOS file dialogs also support MIME types.

### AddFilter

```csharp
public void AddFilter(string filter, string description)
```

引擎名：`add_filter`

Adds a comma-separated file extension `filter` and comma-separated MIME type `mimeType` option to the `FileDialog` with an optional `description`, which restricts what files can be picked.

A `filter` should be of the form `"filename.extension"`, where filename and extension can be `*` to match any string. Filters starting with `.` (i.e. empty filenames) are not allowed.

For example, a `filter` of `"*.png, *.jpg"`, a `mimeType` of `image/png, image/jpeg`, and a `description` of `"Images"` results in filter text "Images (*.png, *.jpg)".

**Note:** Embedded file dialogs and Windows file dialogs support only file extensions, while Android, Linux, and macOS file dialogs also support MIME types.

### AddOption

```csharp
public void AddOption(string name, ReadOnlySpan<string> values, int defaultValueIndex)
```

引擎名：`add_option`

Adds an additional [`OptionButton`](OptionButton.md) to the file dialog. If `values` is empty, a [`CheckBox`](CheckBox.md) is added instead.

`defaultValueIndex` should be an index of the value in the `values`. If `values` is empty it should be either `1` (checked), or `0` (unchecked).

### AddOption

```csharp
public void AddOption(string name, string[] values, int defaultValueIndex)
```

引擎名：`add_option`

Adds an additional [`OptionButton`](OptionButton.md) to the file dialog. If `values` is empty, a [`CheckBox`](CheckBox.md) is added instead.

`defaultValueIndex` should be an index of the value in the `values`. If `values` is empty it should be either `1` (checked), or `0` (unchecked).

### ClearFileNameFilter

```csharp
public void ClearFileNameFilter()
```

Clear the filter for file names.

### ClearFilters

```csharp
public void ClearFilters()
```

引擎名：`clear_filters`

Clear all the added filters in the dialog.

### DeselectAll

```csharp
public void DeselectAll()
```

引擎名：`deselect_all`

Clear all currently selected items in the dialog.

### GetAccess

```csharp
public FileDialog.AccessEnum GetAccess()
```

引擎名：`get_access`

### GetCurrentDir

```csharp
public string GetCurrentDir()
```

引擎名：`get_current_dir`

### GetCurrentFile

```csharp
public string GetCurrentFile()
```

引擎名：`get_current_file`

### GetCurrentPath

```csharp
public string GetCurrentPath()
```

引擎名：`get_current_path`

### GetDisplayMode

```csharp
public FileDialog.DisplayModeEnum GetDisplayMode()
```

引擎名：`get_display_mode`

### GetFavoriteList

```csharp
public static string[] GetFavoriteList()
```

引擎名：`get_favorite_list`

Returns the list of favorite directories, which is shared by all `FileDialog` nodes. Useful to store the list of favorites between project sessions. This method can be called only from the main thread.

### GetFileMode

```csharp
public FileDialog.FileModeEnum GetFileMode()
```

引擎名：`get_file_mode`

### GetFileNameFilter

```csharp
public string GetFileNameFilter()
```

### GetFilters

```csharp
public string[] GetFilters()
```

引擎名：`get_filters`

### GetLineEdit

```csharp
public LineEdit GetLineEdit()
```

引擎名：`get_line_edit`

Returns the LineEdit for the selected file.

**Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their [`CanvasItem.Visible`](CanvasItem.md) property.

### GetOptionCount

```csharp
public int GetOptionCount()
```

引擎名：`get_option_count`

### GetOptionDefault

```csharp
public int GetOptionDefault(int option)
```

引擎名：`get_option_default`

Returns the default value index of the [`OptionButton`](OptionButton.md) or [`CheckBox`](CheckBox.md) with index `option`.

### GetOptionName

```csharp
public string GetOptionName(int option)
```

引擎名：`get_option_name`

Returns the name of the [`OptionButton`](OptionButton.md) or [`CheckBox`](CheckBox.md) with index `option`.

### GetOptionValues

```csharp
public string[] GetOptionValues(int option)
```

引擎名：`get_option_values`

Returns an array of values of the [`OptionButton`](OptionButton.md) with index `option`.

### GetRecentList

```csharp
public static string[] GetRecentList()
```

引擎名：`get_recent_list`

Returns the list of recent directories, which is shared by all `FileDialog` nodes. Useful to store the list of recents between project sessions. This method can be called only from the main thread.

### GetRootSubfolder

```csharp
public string GetRootSubfolder()
```

引擎名：`get_root_subfolder`

### GetSelectedOptions

```csharp
public Dictionary GetSelectedOptions()
```

引擎名：`get_selected_options`

Returns a [`Collections.Dictionary`](Collections.Dictionary.md) with the selected values of the additional [`OptionButton`](OptionButton.md)s and/or [`CheckBox`](CheckBox.md)es. [`Collections.Dictionary`](Collections.Dictionary.md) keys are names and values are selected value indices.

### GetUseNativeDialog

```csharp
public bool GetUseNativeDialog()
```

引擎名：`get_use_native_dialog`

### GetVBox

```csharp
public VBoxContainer GetVBox()
```

Returns the vertical box container of the dialog, custom controls can be added to it.

**Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their [`CanvasItem.Visible`](CanvasItem.md) property.

**Note:** Changes to this node are ignored by native file dialogs, use `AddOption()` to add custom elements to the dialog instead.

### Invalidate

```csharp
public void Invalidate()
```

引擎名：`invalidate`

Invalidates and updates this dialog's content list.

**Note:** This method does nothing on native file dialogs.

### IsCustomizationFlagEnabled

```csharp
public bool IsCustomizationFlagEnabled(FileDialog.Customization flag)
```

引擎名：`is_customization_flag_enabled`

Returns `true` if the provided `flag` is enabled.

### IsModeOverridingTitle

```csharp
public bool IsModeOverridingTitle()
```

引擎名：`is_mode_overriding_title`

### IsShowingHiddenFiles

```csharp
public bool IsShowingHiddenFiles()
```

引擎名：`is_showing_hidden_files`

### PopupFileDialog

```csharp
public void PopupFileDialog()
```

引擎名：`popup_file_dialog`

Shows the `FileDialog` using the default size and position for file dialogs, and selects the file name if there is a current file.

### SetAccess

```csharp
public void SetAccess(FileDialog.AccessEnum access)
```

引擎名：`set_access`

### SetCurrentDir

```csharp
public void SetCurrentDir(string dir)
```

引擎名：`set_current_dir`

### SetCurrentFile

```csharp
public void SetCurrentFile(string file)
```

引擎名：`set_current_file`

### SetCurrentPath

```csharp
public void SetCurrentPath(string path)
```

引擎名：`set_current_path`

### SetCustomizationFlagEnabled

```csharp
public void SetCustomizationFlagEnabled(FileDialog.Customization flag, bool enabled)
```

引擎名：`set_customization_flag_enabled`

Sets the specified customization `flag`, allowing to customize the features available in this `FileDialog`.

### SetDisplayMode

```csharp
public void SetDisplayMode(FileDialog.DisplayModeEnum mode)
```

引擎名：`set_display_mode`

### SetFavoriteList

```csharp
public static void SetFavoriteList(ReadOnlySpan<string> favorites)
```

引擎名：`set_favorite_list`

Sets the list of favorite directories, which is shared by all `FileDialog` nodes. Useful to restore the list of favorites saved with `GetFavoriteList()`. This method can be called only from the main thread.

**Note:** `FileDialog` will update its internal [`ItemList`](ItemList.md) of favorites when its visibility changes. Be sure to call this method earlier if you want your changes to have effect.

### SetFavoriteList

```csharp
public static void SetFavoriteList(string[] favorites)
```

引擎名：`set_favorite_list`

Sets the list of favorite directories, which is shared by all `FileDialog` nodes. Useful to restore the list of favorites saved with `GetFavoriteList()`. This method can be called only from the main thread.

**Note:** `FileDialog` will update its internal [`ItemList`](ItemList.md) of favorites when its visibility changes. Be sure to call this method earlier if you want your changes to have effect.

### SetFileMode

```csharp
public void SetFileMode(FileDialog.FileModeEnum mode)
```

引擎名：`set_file_mode`

### SetFileNameFilter

```csharp
public void SetFileNameFilter(string filter)
```

### SetFilters

```csharp
public void SetFilters(ReadOnlySpan<string> filters)
```

引擎名：`set_filters`

### SetFilters

```csharp
public void SetFilters(string[] filters)
```

引擎名：`set_filters`

### SetGetIconCallback

```csharp
public static void SetGetIconCallback(Callable callback)
```

引擎名：`set_get_icon_callback`

Sets the callback used by the `FileDialog` nodes to get a file icon, when [`FileDialog.DisplayModeEnum.List`](FileDialog.md) mode is used. The callback should take a single `System.String` argument (file path), and return a [`Texture2D`](Texture2D.md). If an invalid texture is returned, the `file` icon will be used instead.

### SetGetThumbnailCallback

```csharp
public static void SetGetThumbnailCallback(Callable callback)
```

引擎名：`set_get_thumbnail_callback`

Sets the callback used by the `FileDialog` nodes to get a file icon, when [`FileDialog.DisplayModeEnum.Thumbnails`](FileDialog.md) mode is used. The callback should take a single `System.String` argument (file path), and return a [`Texture2D`](Texture2D.md). If an invalid texture is returned, the `file_thumbnail` icon will be used instead.

Thumbnails are usually more complex and may take a while to load. To avoid stalling the application, you can use [`ImageTexture`](ImageTexture.md) to asynchronously create the thumbnail.

```csharp
func _ready():
FileDialog.set_get_thumbnail_callback(thumbnail_method)

func thumbnail_method(path):
var image_texture = ImageTexture.new()
make_thumbnail_async(path, image_texture)
return image_texture

func make_thumbnail_async(path, image_texture):
var thumbnail_texture = await generate_thumbnail(path) # Some method that generates a thumbnail.
image_texture.set_image(thumbnail_texture.get_image())

```

### SetModeOverridesTitle

```csharp
public void SetModeOverridesTitle(bool override)
```

引擎名：`set_mode_overrides_title`

### SetOptionCount

```csharp
public void SetOptionCount(int count)
```

引擎名：`set_option_count`

### SetOptionDefault

```csharp
public void SetOptionDefault(int option, int defaultValueIndex)
```

引擎名：`set_option_default`

Sets the default value index of the [`OptionButton`](OptionButton.md) or [`CheckBox`](CheckBox.md) with index `option`.

### SetOptionName

```csharp
public void SetOptionName(int option, string name)
```

引擎名：`set_option_name`

Sets the name of the [`OptionButton`](OptionButton.md) or [`CheckBox`](CheckBox.md) with index `option`.

### SetOptionValues

```csharp
public void SetOptionValues(int option, ReadOnlySpan<string> values)
```

引擎名：`set_option_values`

Sets the option values of the [`OptionButton`](OptionButton.md) with index `option`.

### SetOptionValues

```csharp
public void SetOptionValues(int option, string[] values)
```

引擎名：`set_option_values`

Sets the option values of the [`OptionButton`](OptionButton.md) with index `option`.

### SetRecentList

```csharp
public static void SetRecentList(ReadOnlySpan<string> recents)
```

引擎名：`set_recent_list`

Sets the list of recent directories, which is shared by all `FileDialog` nodes. Useful to restore the list of recents saved with `SetRecentList()`. This method can be called only from the main thread.

**Note:** `FileDialog` will update its internal [`ItemList`](ItemList.md) of recent directories when its visibility changes. Be sure to call this method earlier if you want your changes to have effect.

### SetRecentList

```csharp
public static void SetRecentList(string[] recents)
```

引擎名：`set_recent_list`

Sets the list of recent directories, which is shared by all `FileDialog` nodes. Useful to restore the list of recents saved with `SetRecentList()`. This method can be called only from the main thread.

**Note:** `FileDialog` will update its internal [`ItemList`](ItemList.md) of recent directories when its visibility changes. Be sure to call this method earlier if you want your changes to have effect.

### SetRootSubfolder

```csharp
public void SetRootSubfolder(string dir)
```

引擎名：`set_root_subfolder`

### SetShowHiddenFiles

```csharp
public void SetShowHiddenFiles(bool show)
```

引擎名：`set_show_hidden_files`

### SetUseNativeDialog

```csharp
public void SetUseNativeDialog(bool native)
```

引擎名：`set_use_native_dialog`

## 事件（信号）

### DirSelected

```csharp
public event FileDialog.DirSelectedEventHandler DirSelected
```

引擎名：`dir_selected`

Emitted when the user selects a directory.

### FileNameFilterChanged

```csharp
public event FileDialog.FileNameFilterChangedEventHandler FileNameFilterChanged
```

Emitted when the filter for file names changes.

### FileSelected

```csharp
public event FileDialog.FileSelectedEventHandler FileSelected
```

引擎名：`file_selected`

Emitted when the user selects a file by double-clicking it or pressing the **OK** button.

### FilesSelected

```csharp
public event FileDialog.FilesSelectedEventHandler FilesSelected
```

引擎名：`files_selected`

Emitted when the user selects multiple files.

## AccessEnum

命名空间 `Godot`；enum

### 枚举值

- `Resources = 0`：The dialog only allows accessing files under the [`Resource`](Resource.md) path (`res://`).
- `Userdata = 1`：The dialog only allows accessing files under user data path (`user://`).
- `Filesystem = 2`：The dialog allows accessing files on the whole file system.

## Customization

命名空间 `Godot`；enum

### 枚举值

- `HiddenFiles = 0`：Toggles visibility of the favorite button, and the favorite list on the left side of the dialog.
- `CreateFolder = 1`：If enabled, shows the button for creating new directories (when using [`FileDialog.FileModeEnum.OpenDir`](FileDialog.md), [`FileDialog.FileModeEnum.OpenAny`](FileDialog.md), or [`FileDialog.FileModeEnum.SaveFile`](FileDi…
- `FileFilter = 2`：If enabled, shows the toggle file filter button.
- `FileSort = 3`：If enabled, shows the file sorting options button.
- `Favorites = 4`：If enabled, shows the toggle favorite button and favorite list on the left side of the dialog.
- `Recent = 5`：If enabled, shows the recent directories list on the left side of the dialog.
- `Layout = 6`：If enabled, shows the layout switch buttons (list/thumbnails).
- `OverwriteWarning = 7`：If enabled, the [`FileDialog`](FileDialog.md) will warn the user before overwriting files in save mode.
- `Delete = 8`：If enabled, the context menu will show the "Delete" option, which allows moving files and folders to trash.

## DirSelectedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`FileDialog.DirSelected`](FileDialog.md) event of a [`FileDialog`](FileDialog.md) class.

### 签名

#### Invoke

```csharp
delegate void DirSelectedEventHandler(string dir)
```

## DisplayModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Thumbnails = 0`：The dialog displays files as a grid of thumbnails.
- `List = 1`：The dialog displays files as a list of filenames.

## FileModeEnum

命名空间 `Godot`；enum

### 枚举值

- `OpenFile = 0`：The dialog allows selecting one, and only one file.
- `OpenFiles = 1`：The dialog allows selecting multiple files.
- `OpenDir = 2`：The dialog only allows selecting a directory, disallowing the selection of any file.
- `OpenAny = 3`：The dialog allows selecting one file or directory.
- `SaveFile = 4`：The dialog will warn when a file exists.

## FileNameFilterChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`FileDialog.FileNameFilterChanged`](FileDialog.md) event of a [`FileDialog`](FileDialog.md) class.

### 签名

#### Invoke

```csharp
delegate void FileNameFilterChangedEventHandler(string filter)
```

## FileSelectedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`FileDialog.FileSelected`](FileDialog.md) event of a [`FileDialog`](FileDialog.md) class.

### 签名

#### Invoke

```csharp
delegate void FileSelectedEventHandler(string path)
```

## FilesSelectedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`FileDialog.FilesSelected`](FileDialog.md) event of a [`FileDialog`](FileDialog.md) class.

### 签名

#### Invoke

```csharp
delegate void FilesSelectedEventHandler(string[] paths)
```
