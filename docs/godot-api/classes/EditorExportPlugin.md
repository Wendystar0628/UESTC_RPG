<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorExportPlugin

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

`EditorExportPlugin`s are automatically invoked whenever the user exports the project. They can be used to modify scenes and resources during project export based on what [Feature Tags]($DOCS_URL/tutorials/export/feature_tags.html) are set. For each plugin, `_ExportBegin()` is called at the beginning of the export process and then `_ExportFile()` is called for each exported file.

Register a `EditorExportPlugin` by creating a new [`EditorPlugin`](EditorPlugin.md) and calling its [`EditorPlugin.AddExportPlugin`](EditorPlugin.md) method.

## 构造函数

### EditorExportPlugin

```csharp
public EditorExportPlugin()
```

## 方法

### AddAppleEmbeddedPlatformBundleFile

```csharp
public void AddAppleEmbeddedPlatformBundleFile(string path)
```

引擎名：`add_apple_embedded_platform_bundle_file`

Adds an Apple embedded platform bundle file from the given `path` to the exported project.

### AddAppleEmbeddedPlatformCppCode

```csharp
public void AddAppleEmbeddedPlatformCppCode(string code)
```

引擎名：`add_apple_embedded_platform_cpp_code`

Adds C++ code to the Apple embedded platform export. The final code is created from the code appended by each active export plugin.

### AddAppleEmbeddedPlatformEmbeddedFramework

```csharp
public void AddAppleEmbeddedPlatformEmbeddedFramework(string path)
```

引擎名：`add_apple_embedded_platform_embedded_framework`

Adds a dynamic library (*.dylib, *.framework) to the Linking Phase in the Apple embedded platform's Xcode project and embeds it into the resulting binary.

**Note:** For static libraries (*.a), this works in the same way as `AddAppleEmbeddedPlatformFramework()`.

**Note:** This method should not be used for System libraries as they are already present on the device.

### AddAppleEmbeddedPlatformFramework

```csharp
public void AddAppleEmbeddedPlatformFramework(string path)
```

引擎名：`add_apple_embedded_platform_framework`

Adds a static library (*.a) or a dynamic library (*.dylib, *.framework) to the Linking Phase to the Apple embedded platform's Xcode project.

### AddAppleEmbeddedPlatformLinkerFlags

```csharp
public void AddAppleEmbeddedPlatformLinkerFlags(string flags)
```

引擎名：`add_apple_embedded_platform_linker_flags`

Adds linker flags for the Apple embedded platform export.

### AddAppleEmbeddedPlatformPlistContent

```csharp
public void AddAppleEmbeddedPlatformPlistContent(string plistContent)
```

引擎名：`add_apple_embedded_platform_plist_content`

Adds additional fields to the Apple embedded platform's project Info.plist file.

### AddAppleEmbeddedPlatformProjectStaticLib

```csharp
public void AddAppleEmbeddedPlatformProjectStaticLib(string path)
```

引擎名：`add_apple_embedded_platform_project_static_lib`

Adds a static library from the given `path` to the Apple embedded platform project.

### AddFile

```csharp
public void AddFile(string path, ReadOnlySpan<byte> file, bool remap)
```

引擎名：`add_file`

Adds a custom file to be exported. `path` is the virtual path that can be used to load the file, `file` is the binary data of the file.

When called inside `_ExportFile()` and `remap` is `true`, the current file will not be exported, but instead remapped to this custom file. `remap` is ignored when called in other places.

`file` will not be imported, so consider using `_CustomizeResource()` to remap imported resources.

### AddFile

```csharp
public void AddFile(string path, byte[] file, bool remap)
```

引擎名：`add_file`

Adds a custom file to be exported. `path` is the virtual path that can be used to load the file, `file` is the binary data of the file.

When called inside `_ExportFile()` and `remap` is `true`, the current file will not be exported, but instead remapped to this custom file. `remap` is ignored when called in other places.

`file` will not be imported, so consider using `_CustomizeResource()` to remap imported resources.

### AddIosBundleFile

```csharp
public void AddIosBundleFile(string path)
```

引擎名：`add_ios_bundle_file`

Adds an iOS bundle file from the given `path` to the exported project.

### AddIosCppCode

```csharp
public void AddIosCppCode(string code)
```

引擎名：`add_ios_cpp_code`

Adds C++ code to the iOS export. The final code is created from the code appended by each active export plugin.

### AddIosEmbeddedFramework

```csharp
public void AddIosEmbeddedFramework(string path)
```

引擎名：`add_ios_embedded_framework`

Adds a dynamic library (*.dylib, *.framework) to Linking Phase in iOS's Xcode project and embeds it into resulting binary.

**Note:** For static libraries (*.a), this works the in same way as `AddAppleEmbeddedPlatformFramework()`.

**Note:** This method should not be used for System libraries as they are already present on the device.

### AddIosFramework

```csharp
public void AddIosFramework(string path)
```

引擎名：`add_ios_framework`

Adds a static library (*.a) or a dynamic library (*.dylib, *.framework) to the Linking Phase to the iOS Xcode project.

### AddIosLinkerFlags

```csharp
public void AddIosLinkerFlags(string flags)
```

引擎名：`add_ios_linker_flags`

Adds linker flags for the iOS export.

### AddIosPlistContent

```csharp
public void AddIosPlistContent(string plistContent)
```

引擎名：`add_ios_plist_content`

Adds additional fields to the iOS project Info.plist file.

### AddIosProjectStaticLib

```csharp
public void AddIosProjectStaticLib(string path)
```

引擎名：`add_ios_project_static_lib`

Adds a static library from the given `path` to the iOS project.

### AddMacOSPluginFile

```csharp
public void AddMacOSPluginFile(string path)
```

Adds file or directory matching `path` to `PlugIns` directory of macOS app bundle.

**Note:** This is useful only for macOS exports.

### AddSharedObject

```csharp
public void AddSharedObject(string path, ReadOnlySpan<string> tags, string target)
```

引擎名：`add_shared_object`

Adds a shared object or a directory containing only shared objects with the given `tags` and destination `path`.

**Note:** In case of macOS exports, those shared objects will be added to `Frameworks` directory of app bundle.

In case of a directory code-sign will error if you place non code object in directory.

### AddSharedObject

```csharp
public void AddSharedObject(string path, string[] tags, string target)
```

引擎名：`add_shared_object`

Adds a shared object or a directory containing only shared objects with the given `tags` and destination `path`.

**Note:** In case of macOS exports, those shared objects will be added to `Frameworks` directory of app bundle.

In case of a directory code-sign will error if you place non code object in directory.

### GetExportPlatform

```csharp
public EditorExportPlatform GetExportPlatform()
```

引擎名：`get_export_platform`

Returns currently used export platform.

### GetExportPreset

```csharp
public EditorExportPreset GetExportPreset()
```

引擎名：`get_export_preset`

Returns currently used export preset.

### GetOption

```csharp
public Variant GetOption(StringName name)
```

引擎名：`get_option`

Returns the current value of an export option supplied by `_GetExportOptions()`.

### Skip

```csharp
public void Skip()
```

引擎名：`skip`

To be called inside `_ExportFile()`. Skips the current file, so it's not included in the export.

### _BeginCustomizeResources

```csharp
public virtual bool _BeginCustomizeResources(EditorExportPlatform platform, string[] features)
```

引擎名：`_begin_customize_resources`

Return `true` if this plugin will customize resources based on the platform and features used.

When enabled, `_GetCustomizationConfigurationHash()` and `_CustomizeResource()` will be called and must be implemented.

### _BeginCustomizeScenes

```csharp
public virtual bool _BeginCustomizeScenes(EditorExportPlatform platform, string[] features)
```

引擎名：`_begin_customize_scenes`

Return `true` if this plugin will customize scenes based on the platform and features used.

When enabled, `_GetCustomizationConfigurationHash()` and `_CustomizeScene()` will be called and must be implemented.

**Note:** `_CustomizeScene()` will only be called for scenes that have been modified since the last export.

### _CustomizeResource

```csharp
public virtual Resource _CustomizeResource(Resource resource, string path)
```

引擎名：`_customize_resource`

Customize a resource. If changes are made to it, return the same or a new resource. Otherwise, return `null`. When a new resource is returned, `resource` will be replaced by a copy of the new resource.

The `path` argument is only used when customizing an actual file, otherwise this means that this resource is part of another one and it will be empty.

Implementing this method is required if `_BeginCustomizeResources()` returns `true`.

**Note:** When customizing any of the following types and returning another resource, the other resource should not be skipped using `Skip()` in `_ExportFile()`:

- [`AtlasTexture`](AtlasTexture.md)

- [`CompressedCubemap`](CompressedCubemap.md)

- [`CompressedCubemapArray`](CompressedCubemapArray.md)

- [`CompressedTexture2D`](CompressedTexture2D.md)

- [`CompressedTexture2DArray`](CompressedTexture2DArray.md)

- [`CompressedTexture3D`](CompressedTexture3D.md)

### _CustomizeScene

```csharp
public virtual Node _CustomizeScene(Node scene, string path)
```

引擎名：`_customize_scene`

Customize a scene. If changes are made to it, return the same or a new scene. Otherwise, return `null`. If a new scene is returned, it is up to you to dispose of the old one.

Implementing this method is required if `_BeginCustomizeScenes()` returns `true`.

**Note:** To change a variable in your scene, use the `@export` annotation when declaring it.

### _EndCustomizeResources

```csharp
public virtual void _EndCustomizeResources()
```

引擎名：`_end_customize_resources`

This is called when the customization process for resources ends.

### _EndCustomizeScenes

```csharp
public virtual void _EndCustomizeScenes()
```

引擎名：`_end_customize_scenes`

This is called when the customization process for scenes ends.

### _EndGenerateAppleEmbeddedProject

```csharp
public virtual void _EndGenerateAppleEmbeddedProject(string path, bool willBuildArchive)
```

引擎名：`_end_generate_apple_embedded_project`

This is called after Xcode project generation, but before it is built.

**Note:** Only supported on iOS and visionOS.

### _ExportBegin

```csharp
public virtual void _ExportBegin(string[] features, bool isDebug, string path, uint flags)
```

引擎名：`_export_begin`

Virtual method to be overridden by the user. It is called when the export starts and provides all information about the export. `features` is the list of features for the export, `isDebug` is `true` for debug builds, `path` is the target path for the exported project. `flags` is only used when running a runnable profile, e.g. when using native run on Android.

### _ExportEnd

```csharp
public virtual void _ExportEnd()
```

引擎名：`_export_end`

Virtual method to be overridden by the user. Called when the export is finished.

### _ExportFile

```csharp
public virtual void _ExportFile(string path, string type, string[] features)
```

引擎名：`_export_file`

Virtual method to be overridden by the user. Called for each exported file before `_CustomizeResource()` and `_CustomizeScene()`. The arguments can be used to identify the file. `path` is the path of the file, `type` is the [`Resource`](Resource.md) represented by the file (e.g. [`PackedScene`](PackedScene.md)), and `features` is the list of features for the export.

Calling `Skip()` inside this callback will make the file not included in the export.

### _GetAndroidDependencies

```csharp
public virtual string[] _GetAndroidDependencies(EditorExportPlatform platform, bool debug)
```

引擎名：`_get_android_dependencies`

Virtual method to be overridden by the user. This is called to retrieve the set of Android dependencies provided by this plugin. Each returned Android dependency should have the format of an Android remote binary dependency: `org.godot.example:my-plugin:0.0.0`

For more information see [Android documentation on dependencies](https://developer.android.com/build/dependencies?agpversion=4.1#dependency-types).

**Note:** Only supported on Android and requires `EditorExportPlatformAndroid.gradle_build/use_gradle_build` to be enabled.

### _GetAndroidDependenciesMavenRepos

```csharp
public virtual string[] _GetAndroidDependenciesMavenRepos(EditorExportPlatform platform, bool debug)
```

引擎名：`_get_android_dependencies_maven_repos`

Virtual method to be overridden by the user. This is called to retrieve the URLs of Maven repositories for the set of Android dependencies provided by this plugin.

For more information see [Gradle documentation on dependency management](https://docs.gradle.org/current/userguide/dependency_management.html#sec:maven_repo).

**Note:** Google's Maven repo and the Maven Central repo are already included by default.

**Note:** Only supported on Android and requires `EditorExportPlatformAndroid.gradle_build/use_gradle_build` to be enabled.

### _GetAndroidLibraries

```csharp
public virtual string[] _GetAndroidLibraries(EditorExportPlatform platform, bool debug)
```

引擎名：`_get_android_libraries`

Virtual method to be overridden by the user. This is called to retrieve the local paths of the Android libraries archive (AAR) files provided by this plugin.

**Note:** Relative paths **must** be relative to Godot's `res://addons/` directory. For example, an AAR file located under `res://addons/hello_world_plugin/HelloWorld.release.aar` can be returned as an absolute path using `res://addons/hello_world_plugin/HelloWorld.release.aar` or a relative path using `hello_world_plugin/HelloWorld.release.aar`.

**Note:** Only supported on Android and requires `EditorExportPlatformAndroid.gradle_build/use_gradle_build` to be enabled.

### _GetAndroidManifestActivityElementContents

```csharp
public virtual string _GetAndroidManifestActivityElementContents(EditorExportPlatform platform, bool debug)
```

引擎名：`_get_android_manifest_activity_element_contents`

Virtual method to be overridden by the user. This is used at export time to update the contents of the `activity` element in the generated Android manifest.

**Note:** Only supported on Android and requires `EditorExportPlatformAndroid.gradle_build/use_gradle_build` to be enabled.

### _GetAndroidManifestApplicationElementContents

```csharp
public virtual string _GetAndroidManifestApplicationElementContents(EditorExportPlatform platform, bool debug)
```

引擎名：`_get_android_manifest_application_element_contents`

Virtual method to be overridden by the user. This is used at export time to update the contents of the `application` element in the generated Android manifest.

**Note:** Only supported on Android and requires `EditorExportPlatformAndroid.gradle_build/use_gradle_build` to be enabled.

### _GetAndroidManifestElementContents

```csharp
public virtual string _GetAndroidManifestElementContents(EditorExportPlatform platform, bool debug)
```

引擎名：`_get_android_manifest_element_contents`

Virtual method to be overridden by the user. This is used at export time to update the contents of the `manifest` element in the generated Android manifest.

**Note:** Only supported on Android and requires `EditorExportPlatformAndroid.gradle_build/use_gradle_build` to be enabled.

### _GetCustomizationConfigurationHash

```csharp
public virtual ulong _GetCustomizationConfigurationHash()
```

引擎名：`_get_customization_configuration_hash`

Return a hash based on the configuration passed (for both scenes and resources). This helps keep separate caches for separate export configurations.

Implementing this method is required if `_BeginCustomizeResources()` returns `true`.

**Note:** `_CustomizeResource()` and `_CustomizeScene()` will not be called when the `EditorExportPlugin` script is modified unless this hash changes too.

### _GetExportFeatures

```csharp
public virtual string[] _GetExportFeatures(EditorExportPlatform platform, bool debug)
```

引擎名：`_get_export_features`

Return a `System.String`[] of additional features this preset, for the given `platform`, should have.

### _GetExportOptionVisibility

```csharp
public virtual bool _GetExportOptionVisibility(EditorExportPlatform platform, string option)
```

引擎名：`_get_export_option_visibility`

Validates `option` and returns the visibility for the specified `platform`. The default implementation returns `true` for all options.

### _GetExportOptionWarning

```csharp
public virtual string _GetExportOptionWarning(EditorExportPlatform platform, string option)
```

引擎名：`_get_export_option_warning`

Check the requirements for the given `option` and return a non-empty warning string if they are not met.

**Note:** Use `GetOption()` to check the value of the export options.

### _GetExportOptions

```csharp
public virtual Array<Dictionary> _GetExportOptions(EditorExportPlatform platform)
```

引擎名：`_get_export_options`

Return a list of export options that can be configured for this export plugin.

Each element in the return value is a [`Collections.Dictionary`](Collections.Dictionary.md) with the following keys:

- `option`: A dictionary with the structure documented by [`GodotObject.GetPropertyList`](GodotObject.md), but all keys are optional.

- `default_value`: The default value for this option.

- `update_visibility`: An optional boolean value. If set to `true`, the preset will emit [`GodotObject.PropertyListChanged`](GodotObject.md) when the option is changed.

### _GetExportOptionsOverrides

```csharp
public virtual Dictionary _GetExportOptionsOverrides(EditorExportPlatform platform)
```

引擎名：`_get_export_options_overrides`

Return a [`Collections.Dictionary`](Collections.Dictionary.md) of override values for export options, that will be used instead of user-provided values. Overridden options will be hidden from the user interface.

```csharp
class MyExportPlugin extends EditorExportPlugin:
func _get_name() -> String:
return "MyExportPlugin"

func _supports_platform(platform) -> bool:
if platform is EditorExportPlatformPC:
# Run on all desktop platforms including Windows, MacOS and Linux.
return true
return false

func _get_export_options_overrides(platform) -> Dictionary:
# Override "Embed PCK" to always be enabled.
return {
"binary_format/embed_pck": true,
}

```

### _GetName

```csharp
public virtual string _GetName()
```

引擎名：`_get_name`

Return the name identifier of this plugin (for future identification by the exporter). The plugins are sorted by name before exporting.

Implementing this method is required.

### _ShouldUpdateExportOptions

```csharp
public virtual bool _ShouldUpdateExportOptions(EditorExportPlatform platform)
```

引擎名：`_should_update_export_options`

Return `true` if the result of `_GetExportOptions()` has changed and the export options of the preset corresponding to `platform` should be updated.

### _SupportsPlatform

```csharp
public virtual bool _SupportsPlatform(EditorExportPlatform platform)
```

引擎名：`_supports_platform`

Return `true` if the plugin supports the given `platform`.

### _UpdateAndroidPrebuiltManifest

```csharp
public virtual byte[] _UpdateAndroidPrebuiltManifest(EditorExportPlatform platform, byte[] manifestData)
```

引擎名：`_update_android_prebuilt_manifest`

Provide access to the Android prebuilt manifest and allows the plugin to modify it if needed.

Implementers of this virtual method should take the binary manifest data from `manifestData`, copy it, modify it, and then return it with the modifications.

If no modifications are needed, then an empty `System.Byte`[] should be returned.
