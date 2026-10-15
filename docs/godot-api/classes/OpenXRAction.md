<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRAction

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This resource defines an OpenXR action. Actions can be used both for inputs (buttons, joysticks, triggers, etc.) and outputs (haptics).

OpenXR performs automatic conversion between action type and input type whenever possible. An analog trigger bound to a boolean action will thus return `false` if the trigger is depressed and `true` if pressed fully.

Actions are not directly bound to specific devices, instead OpenXR recognizes a limited number of top level paths that identify devices by usage. We can restrict which devices an action can be bound to by these top level paths. For instance an action that should only be used for hand held controllers can have the top level paths "/user/hand/left" and "/user/hand/right" associated with them. See the [reserved path section in the OpenXR specification](https://www.khronos.org/registry/OpenXR/specs/1.0/html/xrspec.html#semantic-path-reserved) for more info on the top level paths.

Note that the name of the resource is used to register the action with.

## 构造函数

### OpenXRAction

```csharp
public OpenXRAction()
```

## 属性

### ActionType

```csharp
public OpenXRAction.ActionTypeEnum ActionType { get; set; }
```

引擎名：`action_type`

The type of action.

### LocalizedName

```csharp
public string LocalizedName { get; set; }
```

引擎名：`localized_name`

The localized description of this action.

### ToplevelPaths

```csharp
public string[] ToplevelPaths { get; set; }
```

引擎名：`toplevel_paths`

A collections of toplevel paths to which this action can be bound.

## 方法

### GetActionType

```csharp
public OpenXRAction.ActionTypeEnum GetActionType()
```

引擎名：`get_action_type`

### GetLocalizedName

```csharp
public string GetLocalizedName()
```

引擎名：`get_localized_name`

### GetToplevelPaths

```csharp
public string[] GetToplevelPaths()
```

引擎名：`get_toplevel_paths`

### SetActionType

```csharp
public void SetActionType(OpenXRAction.ActionTypeEnum actionType)
```

引擎名：`set_action_type`

### SetLocalizedName

```csharp
public void SetLocalizedName(string localizedName)
```

引擎名：`set_localized_name`

### SetToplevelPaths

```csharp
public void SetToplevelPaths(ReadOnlySpan<string> toplevelPaths)
```

引擎名：`set_toplevel_paths`

### SetToplevelPaths

```csharp
public void SetToplevelPaths(string[] toplevelPaths)
```

引擎名：`set_toplevel_paths`

## ActionTypeEnum

命名空间 `Godot`；enum

### 枚举值

- `Bool = 0`：This action provides a boolean value.
- `Float = 1`：This action provides a float value between `0.0` and `1.0` for any analog input such as triggers.
- `Vector2 = 2`：This action provides a [`Vector2`](Vector2.md) value and can be bound to embedded trackpads and joysticks.
- `Pose = 3`
