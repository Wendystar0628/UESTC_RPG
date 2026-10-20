<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRInteractionProfileMetadata

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

This class allows OpenXR core and extensions to register metadata relating to supported interaction devices such as controllers, trackers, haptic devices, etc. It is primarily used by the action map editor and to sanitize any action map by removing extension-dependent entries when applicable.

## 构造函数

### OpenXRInteractionProfileMetadata

```csharp
public OpenXRInteractionProfileMetadata()
```

## 方法

### RegisterIOPath

```csharp
public void RegisterIOPath(string interactionProfile, string displayName, string toplevelPath, string openxrPath, string openxrExtensionNames, OpenXRAction.ActionTypeEnum actionType)
```

引擎名：`register_io_path`

Registers an input/output path for the given `interactionProfile`. The profile should previously have been registered using `RegisterInteractionProfile()`. `displayName` is the description shown to the user. `toplevelPath` specifies the bind path this input/output can be bound to (e.g. `/user/hand/left` or `/user/hand/right`). `openxrPath` is the action input/output being registered (e.g. `/user/hand/left/input/aim/pose`). `openxrExtensionNames` restricts this input/output to an enabled/available extension, this doesn't need to repeat the extension on the profile but relates to overlapping extension (e.g. `XR_EXT_palm_pose` that introduces `…/input/palm_ext/pose` input paths). `actionType` defines the type of input or output provided by OpenXR.

### RegisterInteractionProfile

```csharp
public void RegisterInteractionProfile(string displayName, string openxrPath, string openxrExtensionNames)
```

引擎名：`register_interaction_profile`

Registers an interaction profile using its OpenXR designation (e.g. `/interaction_profiles/khr/simple_controller` is the profile for OpenXR's simple controller profile).

`displayName` is the description shown to the user. `openxrPath` is the interaction profile path being registered. `openxrExtensionNames` optionally restricts this profile to the given extension being enabled/available. If the extension is not available, the profile and all related entries used in an action map are filtered out.

### RegisterPathRename

```csharp
public void RegisterPathRename(string oldName, string newName)
```

引擎名：`register_path_rename`

Allows for renaming old input/output paths to new paths in order to load and process older action maps.

### RegisterProfileRename

```csharp
public void RegisterProfileRename(string oldName, string newName)
```

引擎名：`register_profile_rename`

Allows for renaming old interaction profile paths to new paths in order to load and process older action maps.

### RegisterTopLevelPath

```csharp
public void RegisterTopLevelPath(string displayName, string openxrPath, string openxrExtensionNames)
```

引擎名：`register_top_level_path`

Registers a top level path to which profiles can be bound. For instance `/user/hand/left` refers to the bind point for the player's left hand. Extensions can register additional top level paths, for instance a haptic vest extension might register `/user/body/vest`.

`displayName` is the name shown to the user. `openxrPath` is the top level path being registered. `openxrExtensionNames` is optional and ensures the top level path is only used if the specified extension is available/enabled.

When a top level path ends up being bound by OpenXR, an [`XRPositionalTracker`](XRPositionalTracker.md) is instantiated to manage the state of the device.
