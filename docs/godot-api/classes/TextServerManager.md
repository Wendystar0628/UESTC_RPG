<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TextServerManager

命名空间 `Godot`；static class

`TextServerManager` is the API backend for loading, enumerating, and switching [`TextServer`](TextServer.md)s.

**Note:** Switching text server at runtime is possible, but will invalidate all fonts and text buffers. Make sure to unload all controls, fonts, and themes before doing so.

## 属性

### Singleton

```csharp
public static TextServerManagerInstance Singleton { get; }
```

## 方法

### AddInterface

```csharp
public static void AddInterface(TextServer interface)
```

引擎名：`add_interface`

Registers a [`TextServer`](TextServer.md) interface.

### FindInterface

```csharp
public static TextServer FindInterface(string name)
```

引擎名：`find_interface`

Finds an interface by its `name`.

### GetInterface

```csharp
public static TextServer GetInterface(int idx)
```

引擎名：`get_interface`

Returns the interface registered at a given index.

### GetInterfaceCount

```csharp
public static int GetInterfaceCount()
```

引擎名：`get_interface_count`

Returns the number of interfaces currently registered.

### GetInterfaces

```csharp
public static Array<Dictionary> GetInterfaces()
```

引擎名：`get_interfaces`

Returns a list of available interfaces, with the index and name of each interface.

### GetPrimaryInterface

```csharp
public static TextServer GetPrimaryInterface()
```

引擎名：`get_primary_interface`

Returns the primary [`TextServer`](TextServer.md) interface currently in use.

### RemoveInterface

```csharp
public static void RemoveInterface(TextServer interface)
```

引擎名：`remove_interface`

Removes an interface. All fonts and shaped text caches should be freed before removing an interface.

### SetPrimaryInterface

```csharp
public static void SetPrimaryInterface(TextServer index)
```

引擎名：`set_primary_interface`

Sets the primary [`TextServer`](TextServer.md) interface.

## 事件（信号）

### InterfaceAdded

```csharp
public event TextServerManager.InterfaceAddedEventHandler InterfaceAdded
```

引擎名：`interface_added`

Emitted when a new interface has been added.

### InterfaceRemoved

```csharp
public event TextServerManager.InterfaceRemovedEventHandler InterfaceRemoved
```

引擎名：`interface_removed`

Emitted when an interface is removed.

## InterfaceAddedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`TextServerManager.InterfaceAdded`](TextServerManager.md) event of a [`TextServerManager`](TextServerManager.md) class.

### 签名

#### Invoke

```csharp
delegate void InterfaceAddedEventHandler(StringName interfaceName)
```

## InterfaceRemovedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`TextServerManager.InterfaceRemoved`](TextServerManager.md) event of a [`TextServerManager`](TextServerManager.md) class.

### 签名

#### Invoke

```csharp
delegate void InterfaceRemovedEventHandler(StringName interfaceName)
```
