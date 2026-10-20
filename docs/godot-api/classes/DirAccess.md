<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# DirAccess

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class is used to manage directories and their content, even outside of the project folder.

`DirAccess` can't be instantiated directly. Instead it is created with a static method that takes a path for which it will be opened.

Most of the methods have a static alternative that can be used without creating a `DirAccess`. Static methods only support absolute paths (including `res://` and `user://`).

```csharp
# Standard
var dir = DirAccess.open("user://levels")
dir.make_dir("world1")
# Static
DirAccess.make_dir_absolute("user://levels/world1")

```

**Note:** Accessing project ("res://") directories once exported may behave unexpectedly as some files are converted to engine-specific formats and their original source files may not be present in the expected PCK package. Because of this, to access resources in an exported project, it is recommended to use [`ResourceLoader`](ResourceLoader.md) instead of [`FileAccess`](FileAccess.md).

Here is an example on how to iterate through the files of a directory:

```csharp
public void DirContents(string path)
{
using var dir = DirAccess.Open(path);
if (dir != null)
{
dir.ListDirBegin();
string fileName = dir.GetNext();
while (fileName != "")
{
if (dir.CurrentIsDir())
{
GD.Print($"Found directory: {fileName}");
}
else
{
GD.Print($"Found file: {fileName}");
}
fileName = dir.GetNext();
}
}
else
{
GD.Print("An error occurred when trying to access the path.");
}
}

```

Keep in mind that file names may change or be remapped after export. If you want to see the actual resource file list as it appears in the editor, use [`ResourceLoader.ListDirectory`](ResourceLoader.md) instead.

## 属性

### IncludeHidden

```csharp
public bool IncludeHidden { get; set; }
```

引擎名：`include_hidden`

If `true`, hidden files are included when navigating the directory.

Affects `ListDirBegin()`, `GetDirectories()` and `GetFiles()`.

### IncludeNavigational

```csharp
public bool IncludeNavigational { get; set; }
```

引擎名：`include_navigational`

If `true`, `.` and `..` are included when navigating the directory.

Affects `ListDirBegin()` and `GetDirectories()`.

## 方法

### ChangeDir

```csharp
public Error ChangeDir(string toDir)
```

引擎名：`change_dir`

Changes the currently opened directory to the one passed as an argument. The argument can be relative to the current directory (e.g. `newdir` or `../newdir`), or an absolute path (e.g. `/tmp/newdir` or `res://somedir/newdir`).

Returns one of the [`Error`](Error.md) code constants ([`Error.Ok`](Error.md) on success).

**Note:** The new directory must be within the same scope, e.g. when you had opened a directory inside `res://`, you can't change it to `user://` directory. If you need to open a directory in another access scope, use `Open()` to create a new instance instead.

### Copy

```csharp
public Error Copy(string from, string to, int chmodFlags = -1)
```

引擎名：`copy`

Copies the `from` file to the `to` destination. Both arguments should be paths to files, either relative or absolute. If the destination file exists and is not access-protected, it will be overwritten.

If `chmodFlags` is different than `-1`, the Unix permissions for the destination path will be set to the provided value, if available on the current operating system.

Returns one of the [`Error`](Error.md) code constants ([`Error.Ok`](Error.md) on success).

### CopyAbsolute

```csharp
public static Error CopyAbsolute(string from, string to, int chmodFlags = -1)
```

引擎名：`copy_absolute`

Static version of `Copy()`. Supports only absolute paths.

### CreateLink

```csharp
public Error CreateLink(string source, string target)
```

引擎名：`create_link`

Creates symbolic link between files or folders.

**Note:** On Windows, this method works only if the application is running with elevated privileges or Developer Mode is enabled.

**Note:** This method is implemented on macOS, Linux, and Windows.

### CreateTemp

```csharp
public static DirAccess CreateTemp(string prefix = "", bool keep = false)
```

引擎名：`create_temp`

Creates a temporary directory. This directory will be freed when the returned `DirAccess` is freed.

If `prefix` is not empty, it will be prefixed to the directory name, separated by a `-`.

If `keep` is `true`, the directory is not deleted when the returned `DirAccess` is freed.

Returns `null` if opening the directory failed. You can use `GetOpenError()` to check the error that occurred.

### CurrentIsDir

```csharp
public bool CurrentIsDir()
```

引擎名：`current_is_dir`

Returns whether the current item processed with the last `GetNext()` call is a directory (`.` and `..` are considered directories).

### DirExists

```csharp
public bool DirExists(string path)
```

引擎名：`dir_exists`

Returns whether the target directory exists. The argument can be relative to the current directory, or an absolute path.

**Note:** The returned `System.Boolean` in the editor and after exporting when used on a path in the `res://` directory may be different. Some files are converted to engine-specific formats when exported, potentially changing the directory structure.

### DirExistsAbsolute

```csharp
public static bool DirExistsAbsolute(string path)
```

引擎名：`dir_exists_absolute`

Static version of `DirExists()`. Supports only absolute paths.

**Note:** The returned `System.Boolean` in the editor and after exporting when used on a path in the `res://` directory may be different. Some files are converted to engine-specific formats when exported, potentially changing the directory structure.

### FileExists

```csharp
public bool FileExists(string path)
```

引擎名：`file_exists`

Returns whether the target file exists. The argument can be relative to the current directory, or an absolute path.

For a static equivalent, use [`FileAccess.FileExists`](FileAccess.md).

**Note:** Many resources types are imported (e.g. textures or sound files), and their source asset will not be included in the exported game, as only the imported version is used. See [`ResourceLoader.Exists`](ResourceLoader.md) for an alternative approach that takes resource remapping into account.

### GetCurrentDir

```csharp
public string GetCurrentDir(bool includeDrive = true)
```

引擎名：`get_current_dir`

Returns the absolute path to the currently opened directory (e.g. `res://folder` or `C:\tmp\folder`).

### GetCurrentDrive

```csharp
public int GetCurrentDrive()
```

引擎名：`get_current_drive`

Returns the currently opened directory's drive index. See `GetDriveName()` to convert returned index to the name of the drive.

### GetDirectories

```csharp
public string[] GetDirectories()
```

引擎名：`get_directories`

Returns a `System.String`[] containing filenames of the directory contents, excluding files. The array is sorted alphabetically.

Affected by `IncludeHidden` and `IncludeNavigational`.

**Note:** The returned directories in the editor and after exporting in the `res://` directory may differ as some files are converted to engine-specific formats when exported.

### GetDirectoriesAt

```csharp
public static string[] GetDirectoriesAt(string path)
```

引擎名：`get_directories_at`

Returns a `System.String`[] containing filenames of the directory contents, excluding files, at the given `path`. The array is sorted alphabetically.

Use `GetDirectories()` if you want more control of what gets included.

**Note:** The returned directories in the editor and after exporting in the `res://` directory may differ as some files are converted to engine-specific formats when exported.

### GetDriveCount

```csharp
public static int GetDriveCount()
```

引擎名：`get_drive_count`

On Windows, returns the number of drives (partitions) mounted on the current filesystem.

On macOS and Android, returns the number of mounted volumes.

On Linux, returns the number of mounted volumes and GTK 3 bookmarks.

On other platforms, the method returns 0.

### GetDriveLabel

```csharp
public static string GetDriveLabel(int idx)
```

引擎名：`get_drive_label`

On Windows, returns the label of the drive (partition) passed as an argument.

On other platforms, or if the requested drive does not exist, returns an empty String.

### GetDriveName

```csharp
public static string GetDriveName(int idx)
```

引擎名：`get_drive_name`

On Windows, returns the name of the drive (partition) passed as an argument (e.g. `C:`).

On macOS, returns the path to the mounted volume passed as an argument.

On Linux, returns the path to the mounted volume or GTK 3 bookmark passed as an argument.

On Android (API level 30+), returns the path to the mounted volume as an argument.

On other platforms, or if the requested drive does not exist, returns an empty String.

### GetFiles

```csharp
public string[] GetFiles()
```

引擎名：`get_files`

Returns a `System.String`[] containing filenames of the directory contents, excluding directories. The array is sorted alphabetically.

Affected by `IncludeHidden`.

**Note:** When used on a `res://` path in an exported project, only the files actually included in the PCK at the given folder level are returned. In practice, this means that since imported resources are stored in a top-level `.godot/` folder, only paths to `*.gd` and `*.import` files are returned (plus a few files such as `project.godot` or `project.binary` and the project icon). In an exported project, the list of returned files will also vary depending on whether `ProjectSettings.editor/export/convert_text_resources_to_binary` is `true`.

### GetFilesAt

```csharp
public static string[] GetFilesAt(string path)
```

引擎名：`get_files_at`

Returns a `System.String`[] containing filenames of the directory contents, excluding directories, at the given `path`. The array is sorted alphabetically.

Use `GetFiles()` if you want more control of what gets included.

**Note:** When used on a `res://` path in an exported project, only the files included in the PCK at the given folder level are returned. In practice, this means that since imported resources are stored in a top-level `.godot/` folder, only paths to `.gd` and `.import` files are returned (plus a few other files, such as `project.godot` or `project.binary` and the project icon). In an exported project, the list of returned files will also vary depending on `ProjectSettings.editor/export/convert_text_resources_to_binary`.

### GetFilesystemType

```csharp
public string GetFilesystemType()
```

引擎名：`get_filesystem_type`

Returns file system type name of the current directory's disk. Returned values are uppercase strings like `NTFS`, `FAT32`, `EXFAT`, `APFS`, `EXT4`, `BTRFS`, and so on.

**Note:** This method is implemented on macOS, Linux, Windows and for PCK virtual file system.

### GetIncludeHidden

```csharp
public bool GetIncludeHidden()
```

引擎名：`get_include_hidden`

### GetIncludeNavigational

```csharp
public bool GetIncludeNavigational()
```

引擎名：`get_include_navigational`

### GetNext

```csharp
public string GetNext()
```

引擎名：`get_next`

Returns the next element (file or directory) in the current directory.

The name of the file or directory is returned (and not its full path). Once the stream has been fully processed, the method returns an empty `System.String` and closes the stream automatically (i.e. `ListDirEnd()` would not be mandatory in such a case).

### GetOpenError

```csharp
public static Error GetOpenError()
```

引擎名：`get_open_error`

Returns the result of the last `Open()` call in the current thread.

### GetSpaceLeft

```csharp
public ulong GetSpaceLeft()
```

引擎名：`get_space_left`

Returns the available space on the current directory's disk, in bytes. Returns `0` if the platform-specific method to query the available space fails.

### IsBundle

```csharp
public bool IsBundle(string path)
```

引擎名：`is_bundle`

Returns `true` if the directory is a macOS bundle.

**Note:** This method is implemented on macOS.

### IsCaseSensitive

```csharp
public bool IsCaseSensitive(string path)
```

引擎名：`is_case_sensitive`

Returns `true` if the file system or directory use case sensitive file names.

**Note:** This method is implemented on macOS, Linux (for EXT4 and F2FS filesystems only) and Windows. On other platforms, it always returns `true`.

### IsEquivalent

```csharp
public bool IsEquivalent(string pathA, string pathB)
```

引擎名：`is_equivalent`

Returns `true` if paths `pathA` and `pathB` resolve to the same file system object. Returns `false` otherwise, even if the files are bit-for-bit identical (e.g., identical copies of the file that are not symbolic links).

### IsLink

```csharp
public bool IsLink(string path)
```

引擎名：`is_link`

Returns `true` if the file or directory is a symbolic link, directory junction, or other reparse point.

**Note:** This method is implemented on macOS, Linux, and Windows.

### ListDirBegin

```csharp
public Error ListDirBegin()
```

引擎名：`list_dir_begin`

Initializes the stream used to list all files and directories using the `GetNext()` function, closing the currently opened stream if needed. Once the stream has been processed, it should typically be closed with `ListDirEnd()`.

Affected by `IncludeHidden` and `IncludeNavigational`.

**Note:** The order of files and directories returned by this method is not deterministic, and can vary between operating systems. If you want a list of all files or folders sorted alphabetically, use `GetFiles()` or `GetDirectories()`.

### ListDirEnd

```csharp
public void ListDirEnd()
```

引擎名：`list_dir_end`

Closes the current stream opened with `ListDirBegin()` (whether it has been fully processed with `GetNext()` does not matter).

### MakeDir

```csharp
public Error MakeDir(string path)
```

引擎名：`make_dir`

Creates a directory. The argument can be relative to the current directory, or an absolute path. The target directory should be placed in an already existing directory (to create the full path recursively, see `MakeDirRecursive()`).

Returns one of the [`Error`](Error.md) code constants ([`Error.Ok`](Error.md) on success).

### MakeDirAbsolute

```csharp
public static Error MakeDirAbsolute(string path)
```

引擎名：`make_dir_absolute`

Static version of `MakeDir()`. Supports only absolute paths.

### MakeDirRecursive

```csharp
public Error MakeDirRecursive(string path)
```

引擎名：`make_dir_recursive`

Creates a target directory and all necessary intermediate directories in its path, by calling `MakeDir()` recursively. The argument can be relative to the current directory, or an absolute path.

Returns one of the [`Error`](Error.md) code constants ([`Error.Ok`](Error.md) on success).

### MakeDirRecursiveAbsolute

```csharp
public static Error MakeDirRecursiveAbsolute(string path)
```

引擎名：`make_dir_recursive_absolute`

Static version of `MakeDirRecursive()`. Supports only absolute paths.

### Open

```csharp
public static DirAccess Open(string path)
```

引擎名：`open`

Creates a new `DirAccess` object and opens an existing directory of the filesystem. The `path` argument can be within the project tree (`res://folder`), the user directory (`user://folder`) or an absolute path of the user filesystem (e.g. `/tmp/folder` or `C:\tmp\folder`).

Returns `null` if opening the directory failed. You can use `GetOpenError()` to check the error that occurred.

### ReadLink

```csharp
public string ReadLink(string path)
```

引擎名：`read_link`

Returns target of the symbolic link.

**Note:** This method is implemented on macOS, Linux, and Windows.

### Remove

```csharp
public Error Remove(string path)
```

引擎名：`remove`

Permanently deletes the target file or an empty directory. The argument can be relative to the current directory, or an absolute path. If the target directory is not empty, the operation will fail.

If you don't want to delete the file/directory permanently, use [`OS.MoveToTrash`](OS.md) instead.

Returns one of the [`Error`](Error.md) code constants ([`Error.Ok`](Error.md) on success).

### RemoveAbsolute

```csharp
public static Error RemoveAbsolute(string path)
```

引擎名：`remove_absolute`

Static version of `Remove()`. Supports only absolute paths.

### Rename

```csharp
public Error Rename(string from, string to)
```

引擎名：`rename`

Renames (move) the `from` file or directory to the `to` destination. Both arguments should be paths to files or directories, either relative or absolute. If the destination file or directory exists and is not access-protected, it will be overwritten.

Returns one of the [`Error`](Error.md) code constants ([`Error.Ok`](Error.md) on success).

### RenameAbsolute

```csharp
public static Error RenameAbsolute(string from, string to)
```

引擎名：`rename_absolute`

Static version of `Rename()`. Supports only absolute paths.

### SetIncludeHidden

```csharp
public void SetIncludeHidden(bool enable)
```

引擎名：`set_include_hidden`

### SetIncludeNavigational

```csharp
public void SetIncludeNavigational(bool enable)
```

引擎名：`set_include_navigational`
