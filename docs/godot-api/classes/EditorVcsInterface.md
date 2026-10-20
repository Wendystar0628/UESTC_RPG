<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorVcsInterface

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

Defines the API that the editor uses to extract information from the underlying VCS. The implementation of this API is included in VCS plugins, which are GDExtension plugins that inherit `EditorVcsInterface` and are attached (on demand) to the singleton instance of `EditorVcsInterface`. Instead of performing the task themselves, all the virtual functions listed below are calling the internally overridden functions in the VCS plugins to provide a plug-n-play experience. A custom VCS plugin is supposed to inherit from `EditorVcsInterface` and override each of these virtual functions.

## 构造函数

### EditorVcsInterface

```csharp
public EditorVcsInterface()
```

## 方法

### AddDiffHunksIntoDiffFile

```csharp
public Dictionary AddDiffHunksIntoDiffFile(Dictionary diffFile, Array<Dictionary> diffHunks)
```

Helper function to add an array of `diffHunks` into a `diffFile`.

### AddLineDiffsIntoDiffHunk

```csharp
public Dictionary AddLineDiffsIntoDiffHunk(Dictionary diffHunk, Array<Dictionary> lineDiffs)
```

Helper function to add an array of `lineDiffs` into a `diffHunk`.

### CreateCommit

```csharp
public Dictionary CreateCommit(string msg, string author, string id, long unixTimestamp, long offsetMinutes)
```

Helper function to create a commit [`Collections.Dictionary`](Collections.Dictionary.md) item. `msg` is the commit message of the commit. `author` is a single human-readable string containing all the author's details, e.g. the email and name configured in the VCS. `id` is the identifier of the commit, in whichever format your VCS may provide an identifier to commits. `unixTimestamp` is the UTC Unix timestamp of when the commit was created. `offsetMinutes` is the timezone offset in minutes, recorded from the system timezone where the commit was created.

### CreateDiffFile

```csharp
public Dictionary CreateDiffFile(string newFile, string oldFile)
```

Helper function to create a [`Collections.Dictionary`](Collections.Dictionary.md) for storing old and new diff file paths.

### CreateDiffHunk

```csharp
public Dictionary CreateDiffHunk(int oldStart, int newStart, int oldLines, int newLines)
```

Helper function to create a [`Collections.Dictionary`](Collections.Dictionary.md) for storing diff hunk data. `oldStart` is the starting line number in old file. `newStart` is the starting line number in new file. `oldLines` is the number of lines in the old file. `newLines` is the number of lines in the new file.

### CreateDiffLine

```csharp
public Dictionary CreateDiffLine(int newLineNo, int oldLineNo, string content, string status)
```

Helper function to create a [`Collections.Dictionary`](Collections.Dictionary.md) for storing a line diff. `newLineNo` is the line number in the new file (can be `-1` if the line is deleted). `oldLineNo` is the line number in the old file (can be `-1` if the line is added). `content` is the diff text. `status` is a single character string which stores the line origin.

### CreateStatusFile

```csharp
public Dictionary CreateStatusFile(string filePath, EditorVcsInterface.ChangeType changeType, EditorVcsInterface.TreeArea area)
```

Helper function to create a [`Collections.Dictionary`](Collections.Dictionary.md) used by editor to read the status of a file.

### PopupError

```csharp
public void PopupError(string msg)
```

Pops up an error message in the editor which is shown as coming from the underlying VCS. Use this to show VCS specific error messages.

### _AllowAmends

```csharp
public virtual bool _AllowAmends()
```

Returns whether or not the plugin allows commit amends.

### _CheckoutBranch

```csharp
public virtual bool _CheckoutBranch(string branchName)
```

Checks out a `branchName` in the VCS.

### _Commit

```csharp
public virtual void _Commit(string msg, bool amend)
```

Commits the currently staged changes and applies the commit `msg` to the resulting commit. If `amend` is `true` the commit will modify the most recent commit instead.

### _CreateBranch

```csharp
public virtual void _CreateBranch(string branchName)
```

Creates a new branch named `branchName` in the VCS.

### _CreateRemote

```csharp
public virtual void _CreateRemote(string remoteName, string remoteUrl)
```

Creates a new remote destination with name `remoteName` and points it to `remoteUrl`. This can be an HTTPS remote or an SSH remote.

### _DiscardFile

```csharp
public virtual void _DiscardFile(string filePath)
```

Discards the changes made in a file present at `filePath`.

### _Fetch

```csharp
public virtual void _Fetch(string remote)
```

Fetches new changes from the `remote`, but doesn't write changes to the current working directory. Equivalent to `git fetch`.

### _GetBranchList

```csharp
public virtual Array<string> _GetBranchList()
```

Gets an instance of an [`Collections.Array`](Collections.Array.md) of `System.String`s containing available branch names in the VCS.

### _GetCurrentBranchName

```csharp
public virtual string _GetCurrentBranchName()
```

Gets the current branch name defined in the VCS.

### _GetDiff

```csharp
public virtual Array<Dictionary> _GetDiff(string identifier, int area)
```

Returns an array of [`Collections.Dictionary`](Collections.Dictionary.md) items (see `CreateDiffFile()`, `CreateDiffHunk()`, `CreateDiffLine()`, `AddLineDiffsIntoDiffHunk()` and `AddDiffHunksIntoDiffFile()`), each containing information about a diff. If `identifier` is a file path, returns a file diff, and if it is a commit identifier, then returns a commit diff.

### _GetLineDiff

```csharp
public virtual Array<Dictionary> _GetLineDiff(string filePath, string text)
```

Returns an [`Collections.Array`](Collections.Array.md) of [`Collections.Dictionary`](Collections.Dictionary.md) items (see `CreateDiffHunk()`), each containing a line diff between a file at `filePath` and the `text` which is passed in.

### _GetModifiedFilesData

```csharp
public virtual Array<Dictionary> _GetModifiedFilesData()
```

Returns an [`Collections.Array`](Collections.Array.md) of [`Collections.Dictionary`](Collections.Dictionary.md) items (see `CreateStatusFile()`), each containing the status data of every modified file in the project folder.

### _GetPreviousCommits

```csharp
public virtual Array<Dictionary> _GetPreviousCommits(int maxCommits)
```

Returns an [`Collections.Array`](Collections.Array.md) of [`Collections.Dictionary`](Collections.Dictionary.md) items (see `CreateCommit()`), each containing the data for a past commit.

### _GetRemotes

```csharp
public virtual Array<string> _GetRemotes()
```

Returns an [`Collections.Array`](Collections.Array.md) of `System.String`s, each containing the name of a remote configured in the VCS.

### _GetVcsName

```csharp
public virtual string _GetVcsName()
```

Returns the name of the underlying VCS provider.

### _Initialize

```csharp
public virtual bool _Initialize(string projectPath)
```

Initializes the VCS plugin when called from the editor. Returns whether or not the plugin was successfully initialized. A VCS project is initialized at `projectPath`.

### _Pull

```csharp
public virtual void _Pull(string remote)
```

Pulls changes from the remote. This can give rise to merge conflicts.

### _Push

```csharp
public virtual void _Push(string remote, bool force)
```

Pushes changes to the `remote`. If `force` is `true`, a force push will override the change history already present on the remote.

### _RemoveBranch

```csharp
public virtual void _RemoveBranch(string branchName)
```

Remove a branch from the local VCS.

### _RemoveRemote

```csharp
public virtual void _RemoveRemote(string remoteName)
```

Remove a remote from the local VCS.

### _SetCredentials

```csharp
public virtual void _SetCredentials(string userName, string password, string sshPublicKeyPath, string sshPrivateKeyPath, string sshPassphrase)
```

Set user credentials in the underlying VCS. `userName` and `password` are used only during HTTPS authentication unless not already mentioned in the remote URL. `sshPublicKeyPath`, `sshPrivateKeyPath`, and `sshPassphrase` are only used during SSH authentication.

### _ShutDown

```csharp
public virtual bool _ShutDown()
```

Shuts down VCS plugin instance. Called when the user either closes the editor or shuts down the VCS plugin through the editor UI.

### _StageFile

```csharp
public virtual void _StageFile(string filePath)
```

Stages the file present at `filePath` to the staged area.

### _UnstageFile

```csharp
public virtual void _UnstageFile(string filePath)
```

Unstages the file present at `filePath` from the staged area to the unstaged area.

## ChangeType

命名空间 `Godot`；enum；仅编辑器（GodotSharpEditor）

### 枚举值

- `New = 0`：A new file has been added.
- `Modified = 1`：An earlier added file has been modified.
- `Renamed = 2`：An earlier added file has been renamed.
- `Deleted = 3`：An earlier added file has been deleted.
- `Typechange = 4`：An earlier added file has been typechanged.
- `Unmerged = 5`：A file is left unmerged.

## TreeArea

命名空间 `Godot`；enum；仅编辑器（GodotSharpEditor）

### 枚举值

- `Commit = 0`：A commit is encountered from the commit area.
- `Staged = 1`：A file is encountered from the staged area.
- `Unstaged = 2`：A file is encountered from the unstaged area.
