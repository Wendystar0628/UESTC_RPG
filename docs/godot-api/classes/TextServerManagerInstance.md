<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TextServerManagerInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

[`TextServerManager`](TextServerManager.md) is the API backend for loading, enumerating, and switching [`TextServer`](TextServer.md)s.

**Note:** Switching text server at runtime is possible, but will invalidate all fonts and text buffers. Make sure to unload all controls, fonts, and themes before doing so.

## 方法

### AddInterface

```csharp
public void AddInterface(TextServer interface)
```

Registers a [`TextServer`](TextServer.md) interface.

### FindInterface

```csharp
public TextServer FindInterface(string name)
```

Finds an interface by its `name`.

### GetInterface

```csharp
public TextServer GetInterface(int idx)
```

Returns the interface registered at a given index.

### GetInterfaceCount

```csharp
public int GetInterfaceCount()
```

Returns the number of interfaces currently registered.

### GetInterfaces

```csharp
public Array<Dictionary> GetInterfaces()
```

Returns a list of available interfaces, with the index and name of each interface.

### GetPrimaryInterface

```csharp
public TextServer GetPrimaryInterface()
```

Returns the primary [`TextServer`](TextServer.md) interface currently in use.

### RemoveInterface

```csharp
public void RemoveInterface(TextServer interface)
```

Removes an interface. All fonts and shaped text caches should be freed before removing an interface.

### SetPrimaryInterface

```csharp
public void SetPrimaryInterface(TextServer index)
```

Sets the primary [`TextServer`](TextServer.md) interface.

## 事件（信号）

### InterfaceAdded

```csharp
public event TextServerManagerInstance.InterfaceAddedEventHandler InterfaceAdded
```

Emitted when a new interface has been added.

### InterfaceRemoved

```csharp
public event TextServerManagerInstance.InterfaceRemovedEventHandler InterfaceRemoved
```

Emitted when an interface is removed.

## InterfaceAddedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`TextServerManagerInstance.InterfaceAdded`](TextServerManagerInstance.md) event of a [`TextServerManagerInstance`](TextServerManagerInstance.md) class.

### 签名

#### Invoke

```csharp
delegate void InterfaceAddedEventHandler(StringName interfaceName)
```

## InterfaceRemovedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`TextServerManagerInstance.InterfaceRemoved`](TextServerManagerInstance.md) event of a [`TextServerManagerInstance`](TextServerManagerInstance.md) class.

### 签名

#### Invoke

```csharp
delegate void InterfaceRemovedEventHandler(StringName interfaceName)
```
