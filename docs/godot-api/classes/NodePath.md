<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# NodePath

命名空间 `Godot`；class

A pre-parsed relative or absolute path in a scene tree,
for use with [`Node.GetNode`](Node.md) and similar functions.
It can reference a node, a resource within a node, or a property
of a node or resource.
For instance, `"Path2D/PathFollow2D/Sprite2D:texture:size"`
would refer to the `size` property of the `texture`
resource on the node named `"Sprite2D"` which is a child of
the other named nodes in the path.
You will usually just pass a string to [`Node.GetNode`](Node.md)
and it will be automatically converted, but you may occasionally
want to parse a path ahead of time with NodePath.
Exporting a NodePath variable will give you a node selection widget
in the properties panel of the editor, which can often be useful.
A NodePath is composed of a list of slash-separated node names
(like a filesystem path) and an optional colon-separated list of
"subnames" which can be resources or properties.

Note: In the editor, NodePath properties are automatically updated when moving,
renaming or deleting a node in the scene tree, but they are never updated at runtime.

## 构造函数

### NodePath

```csharp
public NodePath()
```

Constructs an empty `NodePath`.

### NodePath

```csharp
public NodePath(string path)
```

Constructs a `NodePath` from a string `path`,
e.g.: `"Path2D/PathFollow2D/Sprite2D:texture:size"`.
A path is absolute if it starts with a slash. Absolute paths
are only valid in the global scene tree, not within individual
scenes. In a relative path, `"."` and `".."` indicate
the current node and its parent.
The "subnames" optionally included after the path to the target
node can point to resources or properties, and can also be nested.

参数：
- `path`：A string that represents a path in a scene tree.

## 属性

### IsEmpty

```csharp
public bool IsEmpty { get; }
```

引擎名：`is_empty`

Returns `true` if the node path is empty.

返回：If the `NodePath` is empty.

## 方法

### Dispose

```csharp
public void Dispose()
```

Disposes of this `NodePath`.

### Dispose

```csharp
public void Dispose(bool disposing)
```

### Equals

```csharp
public bool Equals(NodePath other)
```

### Equals

```csharp
public virtual bool Equals(object obj)
```

### GetAsPropertyPath

```csharp
public NodePath GetAsPropertyPath()
```

引擎名：`get_as_property_path`

Returns a node path with a colon character (`:`) prepended,
transforming it to a pure property path with no node name (defaults
to resolving from the current node).

返回：The `NodePath` as a pure property path.

### GetConcatenatedNames

```csharp
public string GetConcatenatedNames()
```

引擎名：`get_concatenated_names`

Returns all names concatenated with a slash character (`/`).

返回：The names concatenated with `/`.

### GetConcatenatedSubNames

```csharp
public string GetConcatenatedSubNames()
```

Returns all subnames concatenated with a colon character (`:`)
as separator, i.e. the right side of the first colon in a node path.

返回：The subnames concatenated with `:`.

### GetHashCode

```csharp
public virtual int GetHashCode()
```

### GetName

```csharp
public string GetName(int idx)
```

引擎名：`get_name`

Gets the node name indicated by `idx` (0 to `GetNameCount()`).

参数：
- `idx`：The name index.

返回：The name at the given index `idx`.

### GetNameCount

```csharp
public int GetNameCount()
```

引擎名：`get_name_count`

Gets the number of node names which make up the path.
Subnames (see `GetSubNameCount()`) are not included.
For example, `"Path2D/PathFollow2D/Sprite2D"` has 3 names.

返回：The number of node names which make up the path.

### GetSubName

```csharp
public string GetSubName(int idx)
```

Gets the resource or property name indicated by `idx` (0 to `GetSubNameCount()`).

参数：
- `idx`：The subname index.

返回：The subname at the given index `idx`.

### GetSubNameCount

```csharp
public int GetSubNameCount()
```

Gets the number of resource or property names ("subnames") in the path.
Each subname is listed after a colon character (`:`) in the node path.
For example, `"Path2D/PathFollow2D/Sprite2D:texture:load_path"` has 2 subnames.

返回：The number of subnames in the path.

### IsAbsolute

```csharp
public bool IsAbsolute()
```

引擎名：`is_absolute`

Returns `true` if the node path is absolute (as opposed to relative),
which means that it starts with a slash character (`/`). Absolute node paths can
be used to access the root node (`"/root"`) or autoloads (e.g. `"/global"`
if a "global" autoload was registered).

返回：If the `NodePath` is an absolute path.

### ToString

```csharp
public virtual string ToString()
```

Converts this `NodePath` to a string.

返回：A string representation of this `NodePath`.

## 运算符

### op_Equality

```csharp
public static bool op_Equality(NodePath left, NodePath right)
```

### op_Implicit

```csharp
public static NodePath op_Implicit(string from)
```

Converts a string to a `NodePath`.

参数：
- `from`：The string to convert.

### op_Implicit

```csharp
public static string op_Implicit(NodePath from)
```

Converts this `NodePath` to a string.

参数：
- `from`：The `NodePath` to convert.

### op_Inequality

```csharp
public static bool op_Inequality(NodePath left, NodePath right)
```
