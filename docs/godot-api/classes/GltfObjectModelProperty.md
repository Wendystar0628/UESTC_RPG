<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GltfObjectModelProperty

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

GLTFObjectModelProperty defines a mapping between a property in the glTF object model and a NodePath in the Godot scene tree. This can be used to animate properties in a glTF file using the `KHR_animation_pointer` extension, or to access them through an engine-agnostic script such as a behavior graph as defined by the `KHR_interactivity` extension.

The glTF property is identified by JSON pointer(s) stored in `JsonPointers`, while the Godot property it maps to is defined by `NodePaths`. In most cases `JsonPointers` and `NodePaths` will each only have one item, but in some cases a single glTF JSON pointer will map to multiple Godot properties, or a single Godot property will be mapped to multiple glTF JSON pointers, or it might be a many-to-many relationship.

[`Expression`](Expression.md) objects can be used to define conversions between the data, such as when glTF defines an angle in radians and Godot uses degrees. The `ObjectModelType` property defines the type of data stored in the glTF file as defined by the object model, see [`GltfObjectModelProperty.GltfObjectModelType`](GltfObjectModelProperty.md) for possible values.

## 构造函数

### GltfObjectModelProperty

```csharp
public GltfObjectModelProperty()
```

## 属性

### GltfToGodotExpression

```csharp
public Expression GltfToGodotExpression { get; set; }
```

If set, this [`Expression`](Expression.md) will be used to convert the property value from the glTF object model to the value expected by the Godot property. This is useful when the glTF object model uses a different unit system, or when the data needs to be transformed in some way. If `null`, the value will be copied as-is.

### GodotToGltfExpression

```csharp
public Expression GodotToGltfExpression { get; set; }
```

If set, this [`Expression`](Expression.md) will be used to convert the property value from the Godot property to the value expected by the glTF object model. This is useful when the glTF object model uses a different unit system, or when the data needs to be transformed in some way. If `null`, the value will be copied as-is.

### JsonPointers

```csharp
public Array<string[]> JsonPointers { get; set; }
```

The glTF object model JSON pointers used to identify the property in the glTF object model. In most cases, there will be only one item in this array, but specific cases may require multiple pointers. The items are themselves arrays which represent the JSON pointer split into its components.

### NodePaths

```csharp
public Array<NodePath> NodePaths { get; set; }
```

An array of [`NodePath`](NodePath.md)s that point to a property, or multiple properties, in the Godot scene tree. On import, this will either be set by [`GltfDocument`](GltfDocument.md), or by a [`GltfDocumentExtension`](GltfDocumentExtension.md) class. For simple cases, use `AppendPathToProperty()` to add properties to this array.

In most cases `NodePaths` will only have one item, but in some cases a single glTF JSON pointer will map to multiple Godot properties. For example, a [`GltfCamera`](GltfCamera.md) or [`GltfLight`](GltfLight.md) used on multiple glTF nodes will be represented by multiple Godot nodes.

### ObjectModelType

```csharp
public GltfObjectModelProperty.GltfObjectModelType ObjectModelType { get; set; }
```

The type of data stored in the glTF file as defined by the object model. This is a superset of the available accessor types, and determines the accessor type.

### VariantType

```csharp
public Variant.Type VariantType { get; set; }
```

The type of data stored in the Godot property. This is the type of the property that the `NodePaths` point to.

## 方法

### AppendNodePath

```csharp
public void AppendNodePath(NodePath nodePath)
```

Appends a [`NodePath`](NodePath.md) to `NodePaths`. This can be used by [`GltfDocumentExtension`](GltfDocumentExtension.md) classes to define how a glTF object model property maps to a Godot property, or multiple Godot properties. Prefer using `AppendPathToProperty()` for simple cases. Be sure to also call `SetTypes()` once (the order does not matter).

### AppendPathToProperty

```csharp
public void AppendPathToProperty(NodePath nodePath, StringName propName)
```

High-level wrapper over `AppendNodePath()` that handles the most common cases. It constructs a new [`NodePath`](NodePath.md) using `nodePath` as a base and appends `propName` to the subpath. Be sure to also call `SetTypes()` once (the order does not matter).

### GetAccessorType

```csharp
public GltfAccessor.GltfAccessorType GetAccessorType()
```

The GLTF accessor type associated with this property's `ObjectModelType`. See [`GltfAccessor.AccessorType`](GltfAccessor.md) for possible values, and see [`GltfObjectModelProperty.GltfObjectModelType`](GltfObjectModelProperty.md) for how the object model type maps to accessor types.

### GetGltfToGodotExpression

```csharp
public Expression GetGltfToGodotExpression()
```

### GetGodotToGltfExpression

```csharp
public Expression GetGodotToGltfExpression()
```

### GetJsonPointers

```csharp
public Array<string[]> GetJsonPointers()
```

### GetNodePaths

```csharp
public Array<NodePath> GetNodePaths()
```

### GetObjectModelType

```csharp
public GltfObjectModelProperty.GltfObjectModelType GetObjectModelType()
```

### GetVariantType

```csharp
public Variant.Type GetVariantType()
```

### HasJsonPointers

```csharp
public bool HasJsonPointers()
```

Returns `true` if `JsonPointers` is not empty. This is used during export to determine if a `GltfObjectModelProperty` can handle converting a Godot property to a glTF object model property.

### HasNodePaths

```csharp
public bool HasNodePaths()
```

Returns `true` if `NodePaths` is not empty. This is used during import to determine if a `GltfObjectModelProperty` can handle converting a glTF object model property to a Godot property.

### SetGltfToGodotExpression

```csharp
public void SetGltfToGodotExpression(Expression gltfToGodotExpr)
```

### SetGodotToGltfExpression

```csharp
public void SetGodotToGltfExpression(Expression godotToGltfExpr)
```

### SetJsonPointers

```csharp
public void SetJsonPointers(Array<string[]> jsonPointers)
```

### SetNodePaths

```csharp
public void SetNodePaths(Array<NodePath> nodePaths)
```

### SetObjectModelType

```csharp
public void SetObjectModelType(GltfObjectModelProperty.GltfObjectModelType type)
```

### SetTypes

```csharp
public void SetTypes(Variant.Type variantType, GltfObjectModelProperty.GltfObjectModelType objModelType)
```

Sets the `VariantType` and `ObjectModelType` properties. This is a convenience method to set both properties at once, since they are almost always known at the same time. This method should be called once. Calling it again with the same values will have no effect.

### SetVariantType

```csharp
public void SetVariantType(Variant.Type variantType)
```

## GltfObjectModelType

命名空间 `Godot`；enum

### 枚举值

- `Unknown = 0`：Unknown or not set object model type.
- `Bool = 1`：Object model type "bool".
- `Float = 2`：Object model type "float".
- `FloatArray = 3`：Object model type "float[lb][rb]".
- `Float2 = 4`：Object model type "float2".
- `Float3 = 5`：Object model type "float3".
- `Float4 = 6`：Object model type "float4".
- `Float2X2 = 7`：Object model type "float2x2".
- `Float3X3 = 8`：Object model type "float3x3".
- `Float4X4 = 9`：Object model type "float4x4".
- `Int = 10`：Object model type "int".
