<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GodotObject

命名空间 `Godot`；class

An advanced [`Variant`](Variant.md) type. All classes in the engine inherit from Object. Each class may define new properties, methods or signals, which are available to all inheriting classes. For example, a [`Sprite2D`](Sprite2D.md) instance is able to call [`Node.AddChild`](Node.md) because it inherits from [`Node`](Node.md).

You can create new instances, using `Object.new()` in GDScript, or `new GodotObject` in C#.

To delete an Object instance, call `Free()`. This is necessary for most classes inheriting Object, because they do not manage memory on their own, and will otherwise cause memory leaks when no longer in use. There are a few classes that perform memory management. For example, [`RefCounted`](RefCounted.md) (and by extension [`Resource`](Resource.md)) deletes itself when no longer referenced, and [`Node`](Node.md) deletes its children when freed.

Objects can have a [`Script`](Script.md) attached to them. Once the [`Script`](Script.md) is instantiated, it effectively acts as an extension to the base class, allowing it to define and inherit new properties, methods and signals.

Inside a [`Script`](Script.md), `_GetPropertyList()` may be overridden to customize properties in several ways. This allows them to be available to the editor, display as lists of options, sub-divide into groups, save on disk, etc. Scripting languages offer easier ways to customize properties, such as with the [annotation @GDScript.@export] annotation.

Godot is very dynamic. An object's script, and therefore its properties, methods and signals, can be changed at run-time. Because of this, there can be occasions where, for example, a property required by a method may not exist. To prevent run-time errors, see methods such as `Set()`, `Get()`, `Call()`, `HasMethod()`, `HasSignal()`, etc. Note that these methods are **much** slower than direct references.

In GDScript, you can also check if a given property, method, or signal name exists in an object with the `in` operator:

```csharp
var node = Node.new()
print("name" in node)         # Prints true
print("get_parent" in node)   # Prints true
print("tree_entered" in node) # Prints true
print("unknown" in node)      # Prints false

```

Notifications are `System.Int32` constants commonly sent and received by objects. For example, on every rendered frame, the [`SceneTree`](SceneTree.md) notifies nodes inside the tree with a [`Node.NotificationProcess`](Node.md). The nodes receive it and may call [`Node._Process()`](Node.md) to update. To make use of notifications, see `Notification()` and `_Notification()`.

Lastly, every object can also contain metadata (data about data). `SetMeta()` can be useful to store information that the object itself does not depend on. To keep your code clean, making excessive use of metadata is discouraged.

**Note:** Unlike references to a [`RefCounted`](RefCounted.md), references to an object stored in a variable can become invalid without being set to `null`. To check if an object has been deleted, do *not* compare it against `null`. Instead, use `GodotObject.IsInstanceValid`. It's also recommended to inherit from [`RefCounted`](RefCounted.md) for classes storing data instead of `GodotObject`.

**Note:** The `script` is not exposed like most properties. To set or get an object's [`Script`](Script.md) in code, use `SetScript()` and `GetScript()`, respectively.

**Note:** In a boolean context, an `GodotObject` will evaluate to `false` if it is equal to `null` or it has been freed. Otherwise, an `GodotObject` will always evaluate to `true`. See also `GodotObject.IsInstanceValid`.

## 构造函数

### GodotObject

```csharp
public GodotObject()
```

Constructs a new `GodotObject`.

## 属性

### NativeInstance

```csharp
public IntPtr NativeInstance { get; }
```

The pointer to the native instance of this `GodotObject`.

## 方法

### AddUserSignal

```csharp
public void AddUserSignal(string signal, Array arguments = null)
```

Adds a user-defined signal named `signal`. Optional arguments for the signal can be added as an [`Collections.Array`](Collections.Array.md) of dictionaries, each defining a `name` `System.String` and a `type` `System.Int32` (see [`Variant.Type`](Variant.md)). See also `HasUserSignal()` and `RemoveUserSignal()`.

```csharp
AddUserSignal("Hurt",
[
new Godot.Collections.Dictionary()
{
{ "name", "damage" },
{ "type", (int)Variant.Type.Int },
},
new Godot.Collections.Dictionary()
{
{ "name", "source" },
{ "type", (int)Variant.Type.Object },
},
]);

```

### Call

```csharp
public Variant Call(StringName method, ReadOnlySpan<Variant> args)
```

Calls the `method` on the object and returns the result. This method supports a variable number of arguments, so parameters can be passed as a comma separated list.

```csharp
var node = new Node3D();
node.Call(Node3D.MethodName.Rotate, new Vector3(1f, 0f, 0f), 1.571f);

```

**Note:** In C#, `method` must be in snake_case when referring to built-in Godot methods. Prefer using the names exposed in the `MethodName` class to avoid allocating a new [`StringName`](StringName.md) on each call.

### Call

```csharp
public Variant Call(StringName method, params Variant[] args)
```

Calls the `method` on the object and returns the result. This method supports a variable number of arguments, so parameters can be passed as a comma separated list.

```csharp
var node = new Node3D();
node.Call(Node3D.MethodName.Rotate, new Vector3(1f, 0f, 0f), 1.571f);

```

**Note:** In C#, `method` must be in snake_case when referring to built-in Godot methods. Prefer using the names exposed in the `MethodName` class to avoid allocating a new [`StringName`](StringName.md) on each call.

### CallDeferred

```csharp
public Variant CallDeferred(StringName method, ReadOnlySpan<Variant> args)
```

Calls the `method` on the object during idle time. Always returns `null`, **not** the method's result.

Idle time happens mainly at the end of process and physics frames. In it, deferred calls will be run until there are none left, which means you can defer calls from other deferred calls and they'll still be run in the current idle time cycle. This means you should not call a method deferred from itself (or from a method called by it), as this causes infinite recursion the same way as if you had called the method directly.

This method supports a variable number of arguments, so parameters can be passed as a comma separated list.

```csharp
var node = new Node3D();
node.CallDeferred(Node3D.MethodName.Rotate, new Vector3(1f, 0f, 0f), 1.571f);

```

For methods that are deferred from the same thread, the order of execution at idle time is identical to the order in which `call_deferred` was called.

See also `Callable.CallDeferred`.

**Note:** In C#, `method` must be in snake_case when referring to built-in Godot methods. Prefer using the names exposed in the `MethodName` class to avoid allocating a new [`StringName`](StringName.md) on each call.

**Note:** If you're looking to delay the function call by a frame, refer to the [`SceneTree.ProcessFrame`](SceneTree.md) and [`SceneTree.PhysicsFrame`](SceneTree.md) signals.

```csharp
var node = Node3D.new()
# Make a Callable and bind the arguments to the node's rotate() call.
var callable = node.rotate.bind(Vector3(1.0, 0.0, 0.0), 1.571)
# Connect the callable to the process_frame signal, so it gets called in the next process frame.
# CONNECT_ONE_SHOT makes sure it only gets called once instead of every frame.
get_tree().process_frame.connect(callable, CONNECT_ONE_SHOT)

```

### CallDeferred

```csharp
public Variant CallDeferred(StringName method, params Variant[] args)
```

Calls the `method` on the object during idle time. Always returns `null`, **not** the method's result.

Idle time happens mainly at the end of process and physics frames. In it, deferred calls will be run until there are none left, which means you can defer calls from other deferred calls and they'll still be run in the current idle time cycle. This means you should not call a method deferred from itself (or from a method called by it), as this causes infinite recursion the same way as if you had called the method directly.

This method supports a variable number of arguments, so parameters can be passed as a comma separated list.

```csharp
var node = new Node3D();
node.CallDeferred(Node3D.MethodName.Rotate, new Vector3(1f, 0f, 0f), 1.571f);

```

For methods that are deferred from the same thread, the order of execution at idle time is identical to the order in which `call_deferred` was called.

See also `Callable.CallDeferred`.

**Note:** In C#, `method` must be in snake_case when referring to built-in Godot methods. Prefer using the names exposed in the `MethodName` class to avoid allocating a new [`StringName`](StringName.md) on each call.

**Note:** If you're looking to delay the function call by a frame, refer to the [`SceneTree.ProcessFrame`](SceneTree.md) and [`SceneTree.PhysicsFrame`](SceneTree.md) signals.

```csharp
var node = Node3D.new()
# Make a Callable and bind the arguments to the node's rotate() call.
var callable = node.rotate.bind(Vector3(1.0, 0.0, 0.0), 1.571)
# Connect the callable to the process_frame signal, so it gets called in the next process frame.
# CONNECT_ONE_SHOT makes sure it only gets called once instead of every frame.
get_tree().process_frame.connect(callable, CONNECT_ONE_SHOT)

```

### Callv

```csharp
public Variant Callv(StringName method, Array argArray)
```

Calls the `method` on the object and returns the result. Unlike `Call()`, this method expects all parameters to be contained inside `argArray`.

```csharp
var node = new Node3D();
node.Callv(Node3D.MethodName.Rotate, [new Vector3(1f, 0f, 0f), 1.571f]);

```

**Note:** In C#, `method` must be in snake_case when referring to built-in Godot methods. Prefer using the names exposed in the `MethodName` class to avoid allocating a new [`StringName`](StringName.md) on each call.

### CanTranslateMessages

```csharp
public bool CanTranslateMessages()
```

Returns `true` if the object is allowed to translate messages with `Tr()` and `TrN()`. See also `SetMessageTranslation()`.

### CancelFree

```csharp
public void CancelFree()
```

If this method is called during `NotificationPredelete`, this object will reject being freed and will remain allocated. This is mostly an internal function used for error handling to avoid the user from freeing objects when they are not intended to.

### Connect

```csharp
public Error Connect(StringName signal, Callable callable, uint flags = 0)
```

Connects a `signal` by name to a `callable`. Optional `flags` can be also added to configure the connection's behavior (see [`GodotObject.ConnectFlags`](GodotObject.md) constants).

A signal can only be connected once to the same [`Callable`](Callable.md). If the signal is already connected, this method returns [`Error.InvalidParameter`](Error.md) and generates an error, unless the signal is connected with [`GodotObject.ConnectFlags.ReferenceCounted`](GodotObject.md). To prevent this, use `IsConnected()` first to check for existing connections.

**Note:** If the `callable`'s object is freed, the connection will be lost.

**Note:** In GDScript, it is generally recommended to connect signals with `Signal.connect` instead.

**Note:** This method, and all other signal-related methods, are thread-safe.

### Disconnect

```csharp
public void Disconnect(StringName signal, Callable callable)
```

Disconnects a `signal` by name from a given `callable`. If the connection does not exist, generates an error. Use `IsConnected()` to make sure that the connection exists.

### Dispose

```csharp
public void Dispose()
```

Disposes of this `GodotObject`.

### EmitSignal

```csharp
public Error EmitSignal(StringName signal, ReadOnlySpan<Variant> args)
```

Emits the given `signal` by name. The signal must exist, so it should be a built-in signal of this class or one of its inherited classes, or a user-defined signal (see `AddUserSignal()`). This method supports a variable number of arguments, so parameters can be passed as a comma separated list.

Returns [`Error.Unavailable`](Error.md) if `signal` does not exist or the parameters are invalid.

```csharp
EmitSignal(SignalName.Hit, "sword", 100);
EmitSignal(SignalName.GameOver);

```

**Note:** In C#, `signal` must be in snake_case when referring to built-in Godot signals. Prefer using the names exposed in the `SignalName` class to avoid allocating a new [`StringName`](StringName.md) on each call.

### EmitSignal

```csharp
public Error EmitSignal(StringName signal, params Variant[] args)
```

Emits the given `signal` by name. The signal must exist, so it should be a built-in signal of this class or one of its inherited classes, or a user-defined signal (see `AddUserSignal()`). This method supports a variable number of arguments, so parameters can be passed as a comma separated list.

Returns [`Error.Unavailable`](Error.md) if `signal` does not exist or the parameters are invalid.

```csharp
EmitSignal(SignalName.Hit, "sword", 100);
EmitSignal(SignalName.GameOver);

```

**Note:** In C#, `signal` must be in snake_case when referring to built-in Godot signals. Prefer using the names exposed in the `SignalName` class to avoid allocating a new [`StringName`](StringName.md) on each call.

### Free

```csharp
public void Free()
```

Deletes the object from memory. Pre-existing references to the object become invalid, and any attempt to access them will result in a runtime error. Checking the references with `GodotObject.IsInstanceValid` will return `false`. This is equivalent to the `memdelete` function in GDExtension C++.

### Get

```csharp
public Variant Get(StringName property)
```

Returns the [`Variant`](Variant.md) value of the given `property`. If the `property` does not exist, this method returns `null`.

```csharp
var node = new Node2D();
node.Rotation = 1.5f;
var a = node.Get(Node2D.PropertyName.Rotation); // a is 1.5

```

**Note:** In C#, `property` must be in snake_case when referring to built-in Godot properties. Prefer using the names exposed in the `PropertyName` class to avoid allocating a new [`StringName`](StringName.md) on each call.

### GetClass

```csharp
public string GetClass()
```

Returns the object's built-in class name, as a `System.String`. See also `IsClass()`.

**Note:** This method ignores `class_name` declarations. If this object's script has defined a `class_name`, the base, built-in class name is returned instead.

### GetIncomingConnections

```csharp
public Array<Dictionary> GetIncomingConnections()
```

Returns an [`Collections.Array`](Collections.Array.md) of signal connections received by this object. Each connection is represented as a [`Collections.Dictionary`](Collections.Dictionary.md) that contains three entries:

- `signal` is a reference to the [`Signal`](Signal.md);

- `callable` is a reference to the [`Callable`](Callable.md);

- `flags` is a combination of [`GodotObject.ConnectFlags`](GodotObject.md).

### GetIndexed

```csharp
public Variant GetIndexed(NodePath propertyPath)
```

Gets the object's property indexed by the given `propertyPath`. The path should be a [`NodePath`](NodePath.md) relative to the current object and can use the colon character (`:`) to access nested properties.

**Examples:** `"position:x"` or `"material:next_pass:blend_mode"`.

```csharp
var node = new Node2D();
node.Position = new Vector2(5, -10);
var a = node.GetIndexed("position");   // a is Vector2(5, -10)
var b = node.GetIndexed("position:y"); // b is -10

```

**Note:** In C#, `propertyPath` must be in snake_case when referring to built-in Godot properties. Prefer using the names exposed in the `PropertyName` class to avoid allocating a new [`StringName`](StringName.md) on each call.

**Note:** This method does not support actual paths to nodes in the [`SceneTree`](SceneTree.md), only sub-property paths. In the context of nodes, use [`Node.GetNodeAndResource`](Node.md) instead.

### GetInstanceId

```csharp
public ulong GetInstanceId()
```

Returns the object's unique instance ID. This ID can be saved in [`EncodedObjectAsId`](EncodedObjectAsId.md), and can be used to retrieve this object instance with `GodotObject.InstanceFromId`.

**Note:** This ID is only useful during the current session. It won't correspond to a similar object if the ID is sent over a network, or loaded from a file at a later time.

### GetMeta

```csharp
public Variant GetMeta(StringName name, Variant default = default)
```

Returns the object's metadata value for the given entry `name`. If the entry does not exist, returns `default`. If `default` is `null`, an error is also generated.

**Note:** A metadata's name must be a valid identifier as per `StringName.is_valid_identifier` method.

**Note:** Metadata that has a name starting with an underscore (`_`) is considered editor-only. Editor-only metadata is not displayed in the Inspector and should not be edited, although it can still be found by this method.

### GetMetaList

```csharp
public Array<StringName> GetMetaList()
```

Returns the object's metadata entry names as an [`Collections.Array`](Collections.Array.md) of [`StringName`](StringName.md)s.

### GetMethodArgumentCount

```csharp
public int GetMethodArgumentCount(StringName method)
```

Returns the number of arguments of the given `method` by name.

**Note:** In C#, `method` must be in snake_case when referring to built-in Godot methods. Prefer using the names exposed in the `MethodName` class to avoid allocating a new [`StringName`](StringName.md) on each call.

### GetMethodList

```csharp
public Array<Dictionary> GetMethodList()
```

Returns this object's methods and their signatures as an [`Collections.Array`](Collections.Array.md) of dictionaries. Each [`Collections.Dictionary`](Collections.Dictionary.md) contains the following entries:

- `name` is the name of the method, as a `System.String`;

- `args` is an [`Collections.Array`](Collections.Array.md) of dictionaries representing the arguments;

- `default_args` is the default arguments as an [`Collections.Array`](Collections.Array.md) of variants;

- `flags` is a combination of [`MethodFlags`](MethodFlags.md);

- `id` is the method's internal identifier `System.Int32`;

- `return` is the returned value, as a [`Collections.Dictionary`](Collections.Dictionary.md);

**Note:** The dictionaries of `args` and `return` are formatted identically to the results of `GetPropertyList()`, although not all entries are used.

### GetPropertyList

```csharp
public Array<Dictionary> GetPropertyList()
```

Returns the object's property list as an [`Collections.Array`](Collections.Array.md) of dictionaries. Each [`Collections.Dictionary`](Collections.Dictionary.md) contains the following entries:

- `name` is the property's name, as a `System.String`;

- `class_name` is an empty [`StringName`](StringName.md), unless the property is [`Variant.Type.Object`](Variant.md) and it inherits from a class;

- `type` is the property's type, as an `System.Int32` (see [`Variant.Type`](Variant.md));

- `hint` is *how* the property is meant to be edited (see [`PropertyHint`](PropertyHint.md));

- `hint_string` depends on the hint (see [`PropertyHint`](PropertyHint.md));

- `usage` is a combination of [`PropertyUsageFlags`](PropertyUsageFlags.md).

**Note:** In GDScript, all class members are treated as properties. In C# and GDExtension, it may be necessary to explicitly mark class members as Godot properties using decorators or attributes.

### GetScript

```csharp
public Variant GetScript()
```

Returns the object's [`Script`](Script.md) instance, or `null` if no script is attached.

### GetSignalConnectionList

```csharp
public Array<Dictionary> GetSignalConnectionList(StringName signal)
```

Returns an [`Collections.Array`](Collections.Array.md) of connections for the given `signal` name. Each connection is represented as a [`Collections.Dictionary`](Collections.Dictionary.md) that contains three entries:

- `signal` is a reference to the [`Signal`](Signal.md);

- `callable` is a reference to the connected [`Callable`](Callable.md);

- `flags` is a combination of [`GodotObject.ConnectFlags`](GodotObject.md).

### GetSignalList

```csharp
public Array<Dictionary> GetSignalList()
```

Returns the list of existing signals as an [`Collections.Array`](Collections.Array.md) of dictionaries.

**Note:** Due to the implementation, each [`Collections.Dictionary`](Collections.Dictionary.md) is formatted very similarly to the returned values of `GetMethodList()`.

### GetTranslationDomain

```csharp
public StringName GetTranslationDomain()
```

Returns the name of the translation domain used by `Tr()` and `TrN()`. See also [`TranslationServer`](TranslationServer.md).

### HasConnections

```csharp
public bool HasConnections(StringName signal)
```

Returns `true` if any connection exists on the given `signal` name.

**Note:** In C#, `signal` must be in snake_case when referring to built-in Godot methods. Prefer using the names exposed in the `SignalName` class to avoid allocating a new [`StringName`](StringName.md) on each call.

### HasMeta

```csharp
public bool HasMeta(StringName name)
```

Returns `true` if a metadata entry is found with the given `name`. See also `GetMeta()`, `SetMeta()` and `RemoveMeta()`.

**Note:** A metadata's name must be a valid identifier as per `StringName.is_valid_identifier` method.

**Note:** Metadata that has a name starting with an underscore (`_`) is considered editor-only. Editor-only metadata is not displayed in the Inspector and should not be edited, although it can still be found by this method.

### HasMethod

```csharp
public bool HasMethod(StringName method)
```

Returns `true` if the given `method` name exists in the object.

**Note:** In C#, `method` must be in snake_case when referring to built-in Godot methods. Prefer using the names exposed in the `MethodName` class to avoid allocating a new [`StringName`](StringName.md) on each call.

### HasSignal

```csharp
public bool HasSignal(StringName signal)
```

Returns `true` if the given `signal` name exists in the object.

**Note:** In C#, `signal` must be in snake_case when referring to built-in Godot signals. Prefer using the names exposed in the `SignalName` class to avoid allocating a new [`StringName`](StringName.md) on each call.

### HasUserSignal

```csharp
public bool HasUserSignal(StringName signal)
```

Returns `true` if the given user-defined `signal` name exists. Only signals added with `AddUserSignal()` are included. See also `RemoveUserSignal()`.

### InstanceFromId

```csharp
public static GodotObject InstanceFromId(ulong instanceId)
```

Returns the `GodotObject` that corresponds to `instanceId`.
All Objects have a unique instance ID. See also `GetInstanceId()`.

参数：
- `instanceId`：Instance ID of the Object to retrieve.

返回：The `GodotObject` instance.

### IsBlockingSignals

```csharp
public bool IsBlockingSignals()
```

Returns `true` if the object is blocking its signals from being emitted. See `SetBlockSignals()`.

### IsClass

```csharp
public bool IsClass(StringName class)
```

Returns `true` if the object inherits from the given `class`. See also `GetClass()`.

```csharp
var sprite2D = new Sprite2D();
sprite2D.IsClass("Sprite2D"); // Returns true
sprite2D.IsClass("Node");     // Returns true
sprite2D.IsClass("Node3D");   // Returns false

```

**Note:** This method ignores `class_name` declarations in the object's script.

### IsClass

```csharp
public bool IsClass(string class)
```

Returns `true` if the object inherits from the given `class`. See also `GetClass()`.

```csharp
var sprite2D = new Sprite2D();
sprite2D.IsClass("Sprite2D"); // Returns true
sprite2D.IsClass("Node");     // Returns true
sprite2D.IsClass("Node3D");   // Returns false

```

**Note:** This method ignores `class_name` declarations in the object's script.

### IsConnected

```csharp
public bool IsConnected(StringName signal, Callable callable)
```

Returns `true` if a connection exists between the given `signal` name and `callable`.

**Note:** In C#, `signal` must be in snake_case when referring to built-in Godot signals. Prefer using the names exposed in the `SignalName` class to avoid allocating a new [`StringName`](StringName.md) on each call.

### IsInstanceIdValid

```csharp
public static bool IsInstanceIdValid(ulong id)
```

Returns `true` if the `GodotObject` that corresponds
to `id` is a valid object (e.g. has not been deleted from
memory). All Objects have a unique instance ID.

参数：
- `id`：The Object ID to check.

返回：If the instance with the given ID is a valid object.

### IsInstanceValid

```csharp
public static bool IsInstanceValid(GodotObject instance)
```

Returns `true` if `instance` is a
valid `GodotObject` (e.g. has not been deleted from memory).

参数：
- `instance`：The instance to check.

返回：If the instance is a valid object.

### IsQueuedForDeletion

```csharp
public bool IsQueuedForDeletion()
```

Returns `true` if the methods [`Node.QueueFree`](Node.md) or [`SceneTree.QueueDelete`](SceneTree.md) was called for the object.

**Note:** This method does not return `true` on children of the node that [`Node.QueueFree`](Node.md) has been called on, even though they will be freed together with the parent.

### Notification

```csharp
public void Notification(int what, bool reversed = false)
```

Sends the given `what` notification to all classes inherited by the object, triggering calls to `_Notification()`, starting from the highest ancestor (the `GodotObject` class) and going down to the object's script.

If `reversed` is `true`, the call order is reversed.

```csharp
var player = new Node2D();
player.SetScript(GD.Load("res://player.gd"));

player.Notification(NotificationEnterTree);
// The call order is GodotObject -> Node -> Node2D -> player.gd.

player.Notification(NotificationEnterTree, true);
// The call order is player.gd -> Node2D -> Node -> GodotObject.

```

### NotifyPropertyListChanged

```csharp
public void NotifyPropertyListChanged()
```

Emits the `PropertyListChanged` signal. This is mainly used to refresh the editor, so that the Inspector and editor plugins are properly updated.

### PropertyCanRevert

```csharp
public bool PropertyCanRevert(StringName property)
```

Returns `true` if the given `property` has a custom default value. Use `PropertyGetRevert()` to get the `property`'s default value.

**Note:** This method is used by the Inspector dock to display a revert icon. The object must implement `_PropertyCanRevert()` to customize the default value. If `_PropertyCanRevert()` is not implemented, this method returns `false`.

### PropertyGetRevert

```csharp
public Variant PropertyGetRevert(StringName property)
```

Returns the custom default value of the given `property`. Use `PropertyCanRevert()` to check if the `property` has a custom default value.

**Note:** This method is used by the Inspector dock to display a revert icon. The object must implement `_PropertyGetRevert()` to customize the default value. If `_PropertyGetRevert()` is not implemented, this method returns `null`.

### RemoveMeta

```csharp
public void RemoveMeta(StringName name)
```

Removes the given entry `name` from the object's metadata. See also `HasMeta()`, `GetMeta()` and `SetMeta()`.

**Note:** A metadata's name must be a valid identifier as per `StringName.is_valid_identifier` method.

**Note:** Metadata that has a name starting with an underscore (`_`) is considered editor-only. Editor-only metadata is not displayed in the Inspector and should not be edited, although it can still be found by this method.

### RemoveUserSignal

```csharp
public void RemoveUserSignal(StringName signal)
```

Removes the given user signal `signal` from the object. See also `AddUserSignal()` and `HasUserSignal()`.

### Set

```csharp
public void Set(StringName property, Variant value)
```

Assigns `value` to the given `property`. If the property does not exist or the given `value`'s type doesn't match, nothing happens.

```csharp
var node = new Node2D();
node.Set(Node2D.PropertyName.GlobalScale, new Vector2(8, 2.5f));
GD.Print(node.GlobalScale); // Prints (8, 2.5)

```

**Note:** In C#, `property` must be in snake_case when referring to built-in Godot properties. Prefer using the names exposed in the `PropertyName` class to avoid allocating a new [`StringName`](StringName.md) on each call.

### SetBlockSignals

```csharp
public void SetBlockSignals(bool enable)
```

If set to `true`, the object becomes unable to emit signals. As such, `EmitSignal()` and signal connections will not work, until it is set to `false`.

### SetDeferred

```csharp
public void SetDeferred(StringName property, Variant value)
```

Assigns `value` to the given `property`, at the end of the current frame. This is equivalent to calling `Set()` through `CallDeferred()`.

```csharp
var node = new Node2D();
node.Rotation = 1.5f;
node.SetDeferred(Node2D.PropertyName.Rotation, 3f);
GD.Print(node.Rotation); // Prints 1.5

await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
GD.Print(node.Rotation); // Prints 3.0

```

**Note:** In C#, `property` must be in snake_case when referring to built-in Godot properties. Prefer using the names exposed in the `PropertyName` class to avoid allocating a new [`StringName`](StringName.md) on each call.

### SetIndexed

```csharp
public void SetIndexed(NodePath propertyPath, Variant value)
```

Assigns a new `value` to the property identified by the `propertyPath`. The path should be a [`NodePath`](NodePath.md) relative to this object, and can use the colon character (`:`) to access nested properties.

```csharp
var node = new Node2D();
node.SetIndexed("position", new Vector2(42, 0));
node.SetIndexed("position:y", -10);
GD.Print(node.Position); // Prints (42, -10)

```

**Note:** In C#, `propertyPath` must be in snake_case when referring to built-in Godot properties. Prefer using the names exposed in the `PropertyName` class to avoid allocating a new [`StringName`](StringName.md) on each call.

### SetMessageTranslation

```csharp
public void SetMessageTranslation(bool enable)
```

If set to `true`, allows the object to translate messages with `Tr()` and `TrN()`. Enabled by default. See also `CanTranslateMessages()`.

### SetMeta

```csharp
public void SetMeta(StringName name, Variant value)
```

Adds or changes the entry `name` inside the object's metadata. The metadata `value` can be any [`Variant`](Variant.md), although some types cannot be serialized correctly.

If `value` is `null`, the entry is removed. This is the equivalent of using `RemoveMeta()`. See also `HasMeta()` and `GetMeta()`.

**Note:** A metadata's name must be a valid identifier as per `StringName.is_valid_identifier` method.

**Note:** Metadata that has a name starting with an underscore (`_`) is considered editor-only. Editor-only metadata is not displayed in the Inspector and should not be edited, although it can still be found by this method.

### SetScript

```csharp
public void SetScript(Variant script)
```

Attaches `script` to the object, and instantiates it. As a result, the script's `GodotObject` is called. A [`Script`](Script.md) is used to extend the object's functionality.

If a script already exists, its instance is detached, and its property values and state are lost. Built-in property values are still kept.

### SetTranslationDomain

```csharp
public void SetTranslationDomain(StringName domain)
```

Sets the name of the translation domain used by `Tr()` and `TrN()`. See also [`TranslationServer`](TranslationServer.md).

### ToSignal

```csharp
public SignalAwaiter ToSignal(GodotObject source, StringName signal)
```

Returns a new [`SignalAwaiter`](SignalAwaiter.md) awaiter configured to complete when the instance
`source` emits the signal specified by the `signal` parameter.

参数：
- `source`：The instance the awaiter will be listening to.
- `signal`：The signal the awaiter will be waiting for.

返回：A [`SignalAwaiter`](SignalAwaiter.md) that completes when
`source` emits the `signal`.

### ToString

```csharp
public virtual string ToString()
```

Converts this `GodotObject` to a string.

返回：A string representation of this object.

### Tr

```csharp
public string Tr(StringName message, StringName context = null)
```

Translates a `message`, using the translation catalogs configured in the Project Settings. Further `context` can be specified to help with the translation. Note that most [`Control`](Control.md) nodes automatically translate their strings, so this method is mostly useful for formatted strings or custom drawn text.

If `CanTranslateMessages()` is `false`, or no translation is available, this method returns the `message` without changes. See `SetMessageTranslation()`.

For detailed examples, see [Internationalizing games]($DOCS_URL/tutorials/i18n/internationalizing_games.html).

**Note:** This method can't be used without an `GodotObject` instance, as it requires the `CanTranslateMessages()` method. To translate strings in a static context, use [`TranslationServer.Translate`](TranslationServer.md).

### TrN

```csharp
public string TrN(StringName message, StringName pluralMessage, int n, StringName context = null)
```

Translates a `message` or `pluralMessage`, using the translation catalogs configured in the Project Settings. Further `context` can be specified to help with the translation.

If `CanTranslateMessages()` is `false`, or no translation is available, this method returns `message` or `pluralMessage`, without changes. See `SetMessageTranslation()`.

The `n` is the number, or amount, of the message's subject. It is used by the translation system to fetch the correct plural form for the current language.

For detailed examples, see [Localization using gettext]($DOCS_URL/tutorials/i18n/localization_using_gettext.html).

**Note:** Negative and `System.Single` numbers may not properly apply to some countable subjects. It's recommended to handle these cases with `Tr()`.

**Note:** This method can't be used without an `GodotObject` instance, as it requires the `CanTranslateMessages()` method. To translate strings in a static context, use [`TranslationServer.TranslatePlural`](TranslationServer.md).

### WeakRef

```csharp
public static WeakRef WeakRef(GodotObject obj)
```

Returns a weak reference to an object, or `null`
if the argument is invalid.
A weak reference to an object is not enough to keep the object alive:
when the only remaining references to a referent are weak references,
garbage collection is free to destroy the referent and reuse its memory
for something else. However, until the object is actually destroyed the
weak reference may return the object even if there are no strong references
to it.

参数：
- `obj`：The object.

返回：The [`WeakRef`](WeakRef.md) reference to the object or `null`.

### _Get

```csharp
public virtual Variant _Get(StringName property)
```

Override this method to customize the behavior of `Get()`. Should return the given `property`'s value, or `null` if the `property` should be handled normally.

Combined with `_Set()` and `_GetPropertyList()`, this method allows defining custom properties, which is particularly useful for editor plugins.

**Note:** This method is not called when getting built-in properties of an object, including properties defined with [annotation @GDScript.@export].

```csharp
public override Variant _Get(StringName property)
{
if (property == "FakeProperty")
{
GD.Print("Getting my property!");
return 4;
}
return default;
}

public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
{
return
[
new Godot.Collections.Dictionary()
{
{ "name", "FakeProperty" },
{ "type", (int)Variant.Type.Int },
},
];
}

```

**Note:** Unlike other virtual methods, this method is called automatically for every script that overrides it. This means that the base implementation should not be called via `super` in GDScript or its equivalents in other languages. The bottom-most sub-class will be called first, with subsequent calls ascending the class hierarchy. The call chain will stop on the first class that returns a non-`null` value.

**Warning:** This method must be [thread-safe]($DOCS_URL/tutorials/performance/thread_safe_apis.html) if overridden. Otherwise, the engine may crash when trying to save a resource containing the object.

### _GetPropertyList

```csharp
public virtual Array<Dictionary> _GetPropertyList()
```

Override this method to provide a custom list of additional properties to handle by the engine.

Should return a property list, as an [`Collections.Array`](Collections.Array.md) of dictionaries. The result is added to the array of `GetPropertyList()`, and should be formatted in the same way. Each [`Collections.Dictionary`](Collections.Dictionary.md) must at least contain the `name` and `type` entries.

You can use `_PropertyCanRevert()` and `_PropertyGetRevert()` to customize the default values of the properties added by this method.

The example below displays a list of numbers shown as words going from `ZERO` to `FIVE`, with `number_count` controlling the size of the list:

[`Collections.Dictionary`](Collections.Dictionary.md)

```csharp
[Tool]
public partial class MyNode : Node
{
private int _numberCount;

[Export]
public int NumberCount
{
get => _numberCount;
set
{
_numberCount = value;
_numbers.Resize(_numberCount);
NotifyPropertyListChanged();
}
}

private Godot.Collections.Array<int> _numbers = [];

public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
{
Godot.Collections.Array<Godot.Collections.Dictionary> properties = [];

for (int i = 0; i < _numberCount; i++)
{
properties.Add(new Godot.Collections.Dictionary()
{
{ "name", $"number_{i}" },
{ "type", (int)Variant.Type.Int },
{ "hint", (int)PropertyHint.Enum },
{ "hint_string", "Zero,One,Two,Three,Four,Five" },
});
}

return properties;
}

public override Variant _Get(StringName property)
{
string propertyName = property.ToString();
if (propertyName.StartsWith("number_"))
{
int index = int.Parse(propertyName.Substring("number_".Length));
return _numbers[index];
}
return default;
}

public override bool _Set(StringName property, Variant value)
{
string propertyName = property.ToString();
if (propertyName.StartsWith("number_"))
{
int index = int.Parse(propertyName.Substring("number_".Length));
_numbers[index] = value.As<int>();
return true;
}
return false;
}
}

```

**Note:** This method is intended for advanced purposes. For most common use cases, the scripting languages offer easier ways to handle properties. See [annotation @GDScript.@export], [annotation @GDScript.@export_enum], [annotation @GDScript.@export_group], etc. If you want to customize exported properties, use `_ValidateProperty()`.

**Note:** If the object's script is not [annotation @GDScript.@tool], this method will not be called in the editor.

**Note:** Unlike other virtual methods, this method is called automatically for every script that overrides it. This means that the base implementation should not be called via `super` in GDScript or its equivalents in other languages. The bottom-most sub-class will be called first, with subsequent calls ascending the class hierarchy.

**Warning:** This method must be [thread-safe]($DOCS_URL/tutorials/performance/thread_safe_apis.html) if overridden. Otherwise, the engine may crash when trying to save a resource containing the object.

### _IterGet

```csharp
public virtual Variant _IterGet(Variant iter)
```

Returns the current iterable value. `iter` stores the iteration state, but unlike `_IterInit()` and `_IterNext()` the state is supposed to be read-only, so there is no [`Collections.Array`](Collections.Array.md) wrapper.

**Tip:** In GDScript, you can use a subtype of [`Variant`](Variant.md) as the return type for `_IterGet()`. The specified type will be used to set the type of the iterator variable in `for` loops, enhancing type safety.

### _IterInit

```csharp
public virtual bool _IterInit(Array iter)
```

Initializes the iterator. `iter` stores the iteration state. Since GDScript does not support passing arguments by reference, a single-element array is used as a wrapper. Returns `true` so long as the iterator has not reached the end.

```csharp
class MyRange:
var _from
var _to

func _init(from, to):
assert(from <= to)
_from = from
_to = to

func _iter_init(iter):
iter[0] = _from
return iter[0] < _to

func _iter_next(iter):
iter[0] += 1
return iter[0] < _to

func _iter_get(iter):
return iter

func _ready():
var my_range = MyRange.new(2, 5)
for x in my_range:
print(x) # Prints 2, 3, 4.

```

**Note:** Avoid storing iterator state in a member variable, use the `iter` parameter instead. Otherwise, you won't be able to reuse the same iterator instance in nested loops.

See also [online docs]($DOCS_URL/tutorials/scripting/gdscript/gdscript_advanced.html#custom-iterators).

### _IterNext

```csharp
public virtual bool _IterNext(Array iter)
```

Moves the iterator to the next iteration. `iter` stores the iteration state. Since GDScript does not support passing arguments by reference, a single-element array is used as a wrapper. Returns `true` so long as the iterator has not reached the end.

### _Notification

```csharp
public virtual void _Notification(int what)
```

Called when the object receives a notification, which can be identified in `what` by comparing it with a constant. See also `Notification()`.

```csharp
public override void _Notification(int what)
{
if (what == NotificationPredelete)
{
GD.Print("Goodbye!");
}
}

```

**Note:** The base `GodotObject` defines a few notifications (`NotificationPostinitialize` and `NotificationPredelete`). Inheriting classes such as [`Node`](Node.md) define a lot more notifications, which are also received by this method.

**Note:** Unlike other virtual methods, this method is called automatically for every script that overrides it. This means that the base implementation should not be called via `super` in GDScript or its equivalents in other languages. Call order depends on the `reversed` argument of `Notification()` and varies between different notifications. Most notifications are sent in the forward order (i.e. Object class first, most derived class last).

### _PropertyCanRevert

```csharp
public virtual bool _PropertyCanRevert(StringName property)
```

Override this method to customize the given `property`'s revert behavior. Should return `true` if the `property` has a custom default value and is revertible in the Inspector dock. Use `_PropertyGetRevert()` to specify the `property`'s default value.

**Note:** This method must return consistently, regardless of the current value of the `property`.

**Note:** Unlike other virtual methods, this method is called automatically for every script that overrides it. This means that the base implementation should not be called via `super` in GDScript or its equivalents in other languages. The bottom-most sub-class will be called first, with subsequent calls ascending the class hierarchy. The call chain will stop on the first class that returns `true`.

### _PropertyGetRevert

```csharp
public virtual Variant _PropertyGetRevert(StringName property)
```

Override this method to customize the given `property`'s revert behavior. Should return the default value for the `property`. If the default value differs from the `property`'s current value, a revert icon is displayed in the Inspector dock.

**Note:** `_PropertyCanRevert()` must also be overridden for this method to be called.

**Note:** Unlike other virtual methods, this method is called automatically for every script that overrides it. This means that the base implementation should not be called via `super` in GDScript or its equivalents in other languages. The bottom-most sub-class will be called first, with subsequent calls ascending the class hierarchy. The call chain will stop on the first class that returns a non-`null` value.

### _Set

```csharp
public virtual bool _Set(StringName property, Variant value)
```

Override this method to customize the behavior of `Set()`. Should set the `property` to `value` and return `true`, or `false` if the `property` should be handled normally. The *exact* way to set the `property` is up to this method's implementation.

Combined with `_Get()` and `_GetPropertyList()`, this method allows defining custom properties, which is particularly useful for editor plugins.

**Note:** This method is not called when setting built-in properties of an object, including properties defined with [annotation @GDScript.@export].

```csharp
private Godot.Collections.Dictionary _internalData = new Godot.Collections.Dictionary();

public override bool _Set(StringName property, Variant value)
{
if (property == "FakeProperty")
{
// Storing the value in the fake property.
_internalData["FakeProperty"] = value;
return true;
}

return false;
}

public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
{
return
[
new Godot.Collections.Dictionary()
{
{ "name", "FakeProperty" },
{ "type", (int)Variant.Type.Int },
},
];
}

```

**Note:** Unlike other virtual methods, this method is called automatically for every script that overrides it. This means that the base implementation should not be called via `super` in GDScript or its equivalents in other languages. The bottom-most sub-class will be called first, with subsequent calls ascending the class hierarchy. The call chain will stop on the first class that returns `true`.

### _ValidateProperty

```csharp
public virtual void _ValidateProperty(Dictionary property)
```

Override this method to customize existing properties. Every property info goes through this method, except properties added with `_GetPropertyList()`. The dictionary contents is the same as in `_GetPropertyList()`.

```csharp
[Tool]
public partial class MyNode : Node
{
private bool _isNumberEditable;

[Export]
public bool IsNumberEditable
{
get => _isNumberEditable;
set
{
_isNumberEditable = value;
NotifyPropertyListChanged();
}
}

[Export]
public int Number { get; set; }

public override void _ValidateProperty(Godot.Collections.Dictionary property)
{
if (property["name"].AsStringName() == PropertyName.Number && !IsNumberEditable)
{
var usage = property["usage"].As<PropertyUsageFlags>() | PropertyUsageFlags.ReadOnly;
property["usage"] = (int)usage;
}
}
}

```

## 事件（信号）

### PropertyListChanged

```csharp
public event Action PropertyListChanged
```

Emitted when `NotifyPropertyListChanged()` is called.

### ScriptChanged

```csharp
public event Action ScriptChanged
```

Emitted when the object's script is changed.

**Note:** When this signal is emitted, the new script is not initialized yet. If you need to access the new script, defer connections to this signal with [`GodotObject.ConnectFlags.Deferred`](GodotObject.md).

## 常量

### NotificationExtensionReloaded

```csharp
public const long NotificationExtensionReloaded = 2
```

Notification received when the object finishes hot reloading. This notification is only sent for extensions classes and derived.

### NotificationPostinitialize

```csharp
public const long NotificationPostinitialize = 0
```

Notification received when the object is initialized, before its script is attached. Used internally.

### NotificationPredelete

```csharp
public const long NotificationPredelete = 1
```

Notification received when the object is about to be deleted. Can be used like destructors in object-oriented programming languages.

This notification is sent in reversed order.

## ConnectFlags

命名空间 `Godot`；enum

### 枚举值

- `Deferred = 1`：Deferred connections trigger their [`Callable`](Callable.md)s on idle time (at the end of the frame), rather than instantly.
- `Persist = 2`：Persisting connections are stored when the object is serialized (such as when using [`PackedScene.Pack`](PackedScene.md)).
- `OneShot = 4`：One-shot connections disconnect themselves after emission.
- `ReferenceCounted = 8`：Reference-counted connections can be assigned to the same [`Callable`](Callable.md) multiple times.
- `AppendSourceObject = 16`：On signal emission, the source object is automatically appended after the original arguments of the signal, regardless of the connected [`Callable`](Callable.md)'s unbinds which affect only the original arguments of the …

## NativeConstructorNotFoundException

命名空间 `Godot`；class；继承 [`GodotObject.NativeMemberNotFoundException`](GodotObject.md)

### 构造函数

#### NativeConstructorNotFoundException

```csharp
public NativeConstructorNotFoundException()
```

#### NativeConstructorNotFoundException

```csharp
public NativeConstructorNotFoundException(string message, Exception innerException)
```

#### NativeConstructorNotFoundException

```csharp
public NativeConstructorNotFoundException(string message, string nativeClassName)
```

#### NativeConstructorNotFoundException

```csharp
public NativeConstructorNotFoundException(string message, string nativeClassName, Exception innerException)
```

#### NativeConstructorNotFoundException

```csharp
public NativeConstructorNotFoundException(string nativeClassName)
```

### 属性

#### Message

```csharp
public string Message { get; }
```

## NativeMemberNotFoundException

命名空间 `Godot`；class

### 构造函数

#### NativeMemberNotFoundException

```csharp
public NativeMemberNotFoundException()
```

#### NativeMemberNotFoundException

```csharp
public NativeMemberNotFoundException(string message)
```

#### NativeMemberNotFoundException

```csharp
public NativeMemberNotFoundException(string message, Exception innerException)
```

## NativeMethodBindNotFoundException

命名空间 `Godot`；class；继承 [`GodotObject.NativeMemberNotFoundException`](GodotObject.md)

### 构造函数

#### NativeMethodBindNotFoundException

```csharp
public NativeMethodBindNotFoundException()
```

#### NativeMethodBindNotFoundException

```csharp
public NativeMethodBindNotFoundException(string message, Exception innerException)
```

#### NativeMethodBindNotFoundException

```csharp
public NativeMethodBindNotFoundException(string message, string nativeMethodName)
```

#### NativeMethodBindNotFoundException

```csharp
public NativeMethodBindNotFoundException(string message, string nativeMethodName, Exception innerException)
```

#### NativeMethodBindNotFoundException

```csharp
public NativeMethodBindNotFoundException(string nativeMethodName)
```

### 属性

#### Message

```csharp
public string Message { get; }
```
