<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorResourceConversionPlugin

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

`EditorResourceConversionPlugin` is invoked when the context menu is brought up for a resource in the editor inspector. Relevant conversion plugins will appear as menu options to convert the given resource to a target type.

Below shows an example of a basic plugin that will convert an [`ImageTexture`](ImageTexture.md) to a [`PortableCompressedTexture2D`](PortableCompressedTexture2D.md).

To use an `EditorResourceConversionPlugin`, register it using the [`EditorPlugin.AddResourceConversionPlugin`](EditorPlugin.md) method first.

## 构造函数

### EditorResourceConversionPlugin

```csharp
public EditorResourceConversionPlugin()
```

## 方法

### _Convert

```csharp
public virtual Resource _Convert(Resource resource)
```

引擎名：`_convert`

Takes an input [`Resource`](Resource.md) and converts it to the type given in `_ConvertsTo()`. The returned [`Resource`](Resource.md) is the result of the conversion, and the input [`Resource`](Resource.md) remains unchanged.

### _ConvertsTo

```csharp
public virtual string _ConvertsTo()
```

引擎名：`_converts_to`

Returns the class name of the target type of [`Resource`](Resource.md) that this plugin converts source resources to.

### _Handles

```csharp
public virtual bool _Handles(Resource resource)
```

引擎名：`_handles`

Called to determine whether a particular [`Resource`](Resource.md) can be converted to the target resource type by this plugin.
