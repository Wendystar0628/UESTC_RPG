<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ResourcePreloader

命名空间 `Godot`；class；继承 [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This node is used to preload sub-resources inside a scene, so when the scene is loaded, all the resources are ready to use and can be retrieved from the preloader. You can add the resources using the ResourcePreloader tab when the node is selected.

GDScript has a simplified `@GDScript.preload` built-in method which can be used in most situations, leaving the use of `ResourcePreloader` for more advanced scenarios.

## 构造函数

### ResourcePreloader

```csharp
public ResourcePreloader()
```

## 属性

### Resources

```csharp
public Array Resources { get; set; }
```

引擎名：`resources`

## 方法

### AddResource

```csharp
public void AddResource(StringName name, Resource resource)
```

引擎名：`add_resource`

Adds a resource to the preloader with the given `name`. If a resource with the given `name` already exists, the new resource will be renamed to "`name` N" where N is an incrementing number starting from 2.

### GetResource

```csharp
public Resource GetResource(StringName name)
```

引擎名：`get_resource`

Returns the resource associated to `name`.

### GetResourceList

```csharp
public string[] GetResourceList()
```

引擎名：`get_resource_list`

Returns the list of resources inside the preloader.

### HasResource

```csharp
public bool HasResource(StringName name)
```

引擎名：`has_resource`

Returns `true` if the preloader contains a resource associated to `name`.

### RemoveResource

```csharp
public void RemoveResource(StringName name)
```

引擎名：`remove_resource`

Removes the resource associated to `name` from the preloader.

### RenameResource

```csharp
public void RenameResource(StringName name, StringName newname)
```

引擎名：`rename_resource`

Renames a resource inside the preloader from `name` to `newname`.
